using System.Text.RegularExpressions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Sistema3S.Tests.Fixtures;
using Sistema3S.Web.Services.Bi;
using Sistema3S.Web.DTOs.Bi;

namespace Sistema3S.Tests.Integration;

public sealed class BiTests
{
    internal static async Task Migrar(QaDatabase db)
    {
        var path=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../Sistema3S.Web/Database/20261012_BI.sql"));
        foreach(var sql in Regex.Split(await File.ReadAllTextAsync(path),@"^GO\s*$",RegexOptions.Multiline|RegexOptions.IgnoreCase))if(!string.IsNullOrWhiteSpace(sql))await db.Exec(sql);
    }
    public static BiService Servicio(QaDatabase db)=>new(new(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"ConnectionStrings:BD_3S",db.Connection}}).Build()),NullLogger<BiService>.Instance);
    public static BiFiltro Filtro(string procedencia="operativo")=>new(){Desde=new(2026,4,1),Hasta=new(2026,10,12),Procedencia=procedencia};
    [Fact(DisplayName="PI-BI-001 | Migración idempotente, cero operaciones y seis análisis sin datos falsos")]
    public async Task Vacio()
    {
        await using var db=await QaDatabase.CreateAsync();await Migrar(db);await Migrar(db);
        var s=Servicio(db);
        foreach(var vista in new[]{"resumen","ventas","productos","clientes","compras","inventario"})
        {
            var r=await s.ConsultarAsync(vista,Filtro(),default);
            if(vista!="inventario")Assert.All(r.Datos["kpis"],k=>{Assert.Equal(0,Convert.ToDecimal(k["Importe"]));Assert.Null(k["VariacionPorcentual"]);});
            Assert.Equal("PEN",r.Moneda);
        }
        Assert.Equal(0,await db.Scalar("SELECT COUNT(*) FROM Venta"));Assert.Equal(0,await db.Scalar("SELECT COUNT(*) FROM bi.OperacionReferencia"));
    }
    [Fact(DisplayName="PI-BI-002 | Totales exactos, detalle sin duplicación, anulaciones y cohortes convertidas")]
    public async Task Matematica()
    {
        await using var db=await QaDatabase.CreateAsync();await Migrar(db);
        await db.Exec("""
          INSERT Cotizacion(IdCliente,IdUsuarioRegistro,IdEstadoCotizacion,FechaCotizacion,Subtotal,Descuento,Igv,Total) VALUES(1,1,1,'2026-09-01',236,0,36,236);
          INSERT DetalleCotizacion(IdCotizacion,IdElementoCatalogo,Cantidad,PrecioUnitario,Subtotal) VALUES(1,1,2,118,236);
          INSERT Venta(IdCliente,IdCotizacion,IdUsuarioRegistro,IdEstadoVenta,FechaVenta,Subtotal,Igv,Total) VALUES(1,1,1,1,'2026-09-05',200,36,236),(1,NULL,1,2,'2026-09-06',100,18,118);
          INSERT DetalleVenta(IdVenta,IdElementoCatalogo,Cantidad,PrecioUnitario,Subtotal) VALUES(1,1,1,118,118),(1,1,1,118,118),(2,1,1,118,118);
          INSERT Compra(IdProveedor,IdUsuarioRegistro,IdEstadoCompra,FechaCompra,Subtotal,Igv,Total) VALUES(1,1,1,'2026-09-01',100,18,118);
          INSERT DetalleCompra(IdCompra,IdProducto,Cantidad,PrecioCompra,Subtotal) VALUES(1,1,2,59,118);
          """);
        var r=await Servicio(db).ConsultarAsync("resumen",Filtro(),default);
        var ventas=r.Datos["kpis"].Single(d=>Equals(d["Tipo"],"venta"));
        Assert.Equal(236,Convert.ToDecimal(ventas["Importe"]));Assert.Equal(1,Convert.ToInt64(ventas["Operaciones"]));Assert.Equal(2,Convert.ToInt64(r.Datos["productosUnidades"][0]["Cantidad"]));
        var cot=r.Datos["kpis"].Single(d=>Equals(d["Tipo"],"cotizacion"));Assert.Equal(100,Convert.ToDecimal(cot["ConversionPorcentual"]));
        Assert.Equal(200,Convert.ToDecimal(cot["Base"]));Assert.Equal(Convert.ToDecimal(cot["Importe"]),Convert.ToDecimal(cot["Base"])+Convert.ToDecimal(cot["Igv"]));
        var compras=r.Datos["kpis"].Single(d=>Equals(d["Tipo"],"compra"));Assert.Equal(118,Convert.ToDecimal(compras["Importe"]));
        var f=Filtro();f.Cliente=999;var vacio=await Servicio(db).ConsultarAsync("ventas",f,default);Assert.Equal(0,Convert.ToDecimal(vacio.Datos["kpis"].Single(d=>Equals(d["Tipo"],"venta"))["Importe"]));
    }
    [Fact(DisplayName="PS-BI-001 | Endpoints y exportaciones niegan anónimo, cliente y rol sin permiso")]
    public async Task Seguridad()
    {
        await using var db=await QaDatabase.CreateAsync();await Migrar(db);await using var api=await ApiHost.Start(db);
        var url="/api/bi/resumen?desde=2026-04-01&hasta=2026-10-12";
        Assert.Equal(HttpStatusCode.Unauthorized,(await api.Http.GetAsync(url)).StatusCode);
        await db.Exec("UPDATE Cliente SET IdUsuario=3 WHERE IdCliente=1");
        var cliente=await ApiTests.Json(await api.Http.PostAsJsonAsync("/api/cliente-web/login",new{correo="cliente@example.test",contrasena=QaDatabase.Password}));
        api.Http.DefaultRequestHeaders.Authorization=new("Bearer",cliente.GetProperty("token").GetString());
        Assert.Equal(HttpStatusCode.Forbidden,(await api.Http.GetAsync(url)).StatusCode);
        await ApiTests.Login(api,"compras@example.test");Assert.Equal(HttpStatusCode.Forbidden,(await api.Http.GetAsync(url)).StatusCode);
        await db.Exec("INSERT RolPermiso(IdRol,IdPermiso) SELECT 2,IdPermiso FROM Permiso WHERE Nombre='BI_VER'");
        Assert.Equal(HttpStatusCode.OK,(await api.Http.GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await api.Http.GetAsync(url+"&procedencia=referencia")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await api.Http.GetAsync("/api/bi/resumen/excel?desde=2026-04-01&hasta=2026-10-12")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await api.Http.GetAsync("/api/bi/operaciones/venta/1")).StatusCode);
        await ApiTests.Login(api);var invalid=await api.Http.GetAsync("/api/bi/resumen?desde=2026-10-12&hasta=2026-04-01");Assert.Equal(HttpStatusCode.BadRequest,invalid.StatusCode);
        Assert.True(await db.Scalar("SELECT COUNT(*) FROM AuditoriaLog WHERE Descripcion LIKE 'Bi.%'")>0);
    }
    [Fact(DisplayName="PI-BI-003 | Referencia idempotente, siete meses, saldos y separación de operaciones")]
    public async Task Referencia()
    {
        await using var db=await QaDatabase.CreateAsync();await Migrar(db);await BiReferencia.PrepararAsync(db);
        var cantidad=await db.Scalar("SELECT COUNT(*) FROM bi.OperacionReferencia");
        await db.Exec(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory,"Fixtures/bi-referencia-qa.sql")));
        Assert.Equal(cantidad,await db.Scalar("SELECT COUNT(*) FROM bi.OperacionReferencia"));
        Assert.Equal(418,cantidad);Assert.Equal(0,await db.Scalar("SELECT SUM(StockActual) FROM Inventario"));Assert.Equal(0,await db.Scalar("SELECT COUNT(*) FROM Venta"));
        var s=Servicio(db);var r=await s.ConsultarAsync("resumen",Filtro("referencia"),default);
        var ventas=r.Datos["kpis"].Single(k=>Equals(k["Tipo"],"venta"));Assert.Equal(await db.Scalar("SELECT SUM(Total) FROM bi.OperacionReferencia WHERE Tipo='venta' AND Estado=N'Confirmada'"),Convert.ToDecimal(ventas["Importe"]));
        Assert.Equal(7,r.Datos["evolucion"].Where(k=>Equals(k["Tipo"],"venta")).Count());
        var f=Filtro("referencia");f.Elemento=5;var inventario=await s.ConsultarAsync("inventario",f,default);
        Assert.NotEmpty(inventario.Datos["saldosHistoricos"]);Assert.NotNull(inventario.Datos["rotacion"][0]["Rotacion"]);
        Assert.Null(inventario.Datos["estadoInventario"][0]["BajoMinimo"]);
        foreach(var elemento in Enumerable.Range(5,14)){f.Elemento=elemento;var saldo=await s.ConsultarAsync("inventario",f,default);Assert.NotEmpty(saldo.Datos["saldosHistoricos"]);Assert.All(saldo.Datos["saldosHistoricos"],d=>Assert.True(Convert.ToDecimal(d["Minimo"])>=0));}
        var op=await s.ConsultarAsync("resumen",Filtro(),default);Assert.All(op.Datos["kpis"],k=>Assert.Equal(0,Convert.ToDecimal(k["Importe"])));
    }
    [Fact(DisplayName="PI-BI-004 | Agrupaciones, comparación, filtros y periodos sin actividad cuadran con SQL")]
    public async Task FiltrosSeries()
    {
        await using var db=await QaDatabase.CreateAsync();await Migrar(db);await BiReferencia.PrepararAsync(db);
        var s=Servicio(db);foreach(var agrupacion in new[]{"dia","semana","mes","anio"})
        {
            var f=Filtro("referencia");f.Agrupacion=agrupacion;var r=await s.ConsultarAsync("ventas",f,default);
            Assert.Equal(171826m,r.Datos["evolucion"].Where(d=>Equals(d["Tipo"],"venta")).Sum(d=>Convert.ToDecimal(d["Importe"])));
        }
        var filtro=Filtro("referencia");filtro.Desde=new(2026,9,1);filtro.Hasta=new(2026,9,30);filtro.CompararDesde=new(2026,6,1);filtro.CompararHasta=new(2026,6,30);filtro.Elemento=5;
        var actual=await s.ConsultarAsync("productos",filtro,default);var k=actual.Datos["kpis"].Single(d=>Equals(d["Tipo"],"venta"));
        Assert.Equal(await db.Scalar("SELECT SUM(d.Importe) FROM bi.OperacionReferencia o JOIN bi.DetalleReferencia d ON d.IdOperacion=o.IdOperacion WHERE o.Tipo='venta' AND o.Estado=N'Confirmada' AND o.Fecha>='20260901' AND o.Fecha<'20261001' AND d.IdElementoCatalogo=5"),Convert.ToDecimal(k["Importe"]));
        Assert.Equal(BiReglas.Variacion(Convert.ToDecimal(k["Importe"]),Convert.ToDecimal(k["Anterior"])),k["VariacionPorcentual"]);
        filtro.Elemento=18;var sinVentas=await s.ConsultarAsync("productos",filtro,default);Assert.Equal(0,Convert.ToDecimal(sinVentas.Datos["kpis"].Single(d=>Equals(d["Tipo"],"venta"))["Importe"]));Assert.Single(sinVentas.Datos["sinVentas"]);
    }
    [Fact(DisplayName="PI-BI-005 | Exportaciones protegidas contienen KPI SQL y gráfico Excel válido")]
    public async Task Archivos()
    {
        await using var db=await QaDatabase.CreateAsync();await Migrar(db);await BiReferencia.PrepararAsync(db);await using var api=await ApiHost.Start(db);await ApiTests.Login(api);
        var query="?desde=2026-04-01&hasta=2026-10-12&procedencia=referencia";
        using var response=await api.Http.GetAsync("/api/bi/resumen/excel"+query);Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        var bytes=await response.Content.ReadAsByteArrayAsync();using var stream=new MemoryStream(bytes);using(var workbook=new ClosedXML.Excel.XLWorkbook(stream))
        {
            var indicadores=workbook.Worksheets.Single(w=>w.Name.Contains("Indicadores"));var venta=indicadores.RowsUsed().Single(row=>row.Cell(1).GetString()=="venta");Assert.Equal(171826m,venta.Cell(3).GetValue<decimal>());Assert.Contains("referencia",workbook.Worksheet("Informe").Cell(5,1).GetString());
        }
        using var open=DocumentFormat.OpenXml.Packaging.SpreadsheetDocument.Open(new MemoryStream(bytes),false);
        var drawings=open.WorkbookPart!.WorksheetParts.SelectMany(p=>p.DrawingsPart?.ChartParts??[]).ToArray();Assert.Single(drawings);
        Assert.Empty(new DocumentFormat.OpenXml.Validation.OpenXmlValidator().Validate(drawings[0].ChartSpace));
        Assert.Empty(new DocumentFormat.OpenXml.Validation.OpenXmlValidator().Validate(open));
        using var pdf=await api.Http.GetAsync("/api/bi/resumen/pdf"+query);Assert.Equal(HttpStatusCode.OK,pdf.StatusCode);Assert.StartsWith("%PDF",System.Text.Encoding.ASCII.GetString((await pdf.Content.ReadAsByteArrayAsync()).Take(4).ToArray()));
        var disabled=new BiFiltro{Desde=new(2026,9,1),Hasta=new(2026,9,30)};using var canceled=new CancellationTokenSource();canceled.Cancel();await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>Servicio(db).ConsultarAsync("ventas",disabled,canceled.Token));
    }
    [Fact(DisplayName="PI-BI-006 | Instalación comercialmente vacía permite login, catálogo vacío y seis reportes")]
    public async Task InstalacionLimpia()
    {
        await using var db=await QaDatabase.CreateAsync(false);var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../"));
        await db.Exec(await File.ReadAllTextAsync(Path.Combine(root,"tools/instalacion/02-catalogos.sql")));await Migrar(db);
        var columnasEsperadas=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory,"Fixtures/schema-columnas.json")));
        foreach(var columna in columnasEsperadas.RootElement.GetProperty("filas").EnumerateArray().Where(c=>c.GetProperty("tabla").GetString()!="sysdiagrams"))
        {
            var tabla=columna.GetProperty("tabla").GetString();var nombre=columna.GetProperty("columna").GetString();Assert.Equal(1,await db.Scalar($"SELECT COUNT(*) FROM sys.columns WHERE object_id=OBJECT_ID('dbo.{tabla}') AND name=N'{nombre}'"));
        }
        await using(var conn=new Microsoft.Data.SqlClient.SqlConnection(db.Connection)){await conn.OpenAsync();await using var cmd=new Microsoft.Data.SqlClient.SqlCommand("INSERT Usuario(IdRol,Correo,ContrasenaHash) SELECT IdRol,N'admin@example.test',@hash FROM Rol WHERE Nombre=N'Administrador'",conn);cmd.Parameters.AddWithValue("@hash",new Sistema3S.Web.Services.Seguridad.PasswordHashService().CrearHash(QaDatabase.Password));await cmd.ExecuteNonQueryAsync();}
        await using var api=await ApiHost.Start(db);await ApiTests.Login(api);
        Assert.Equal(HttpStatusCode.OK,(await api.Http.GetAsync("/api/publico/productos")).StatusCode);
        foreach(var view in new[]{"resumen","ventas","productos","clientes","compras","inventario"})Assert.Equal(HttpStatusCode.OK,(await api.Http.GetAsync($"/api/bi/{view}?desde=2026-04-01&hasta=2026-10-12")).StatusCode);
        foreach(var table in new[]{"Venta","Compra","Cotizacion","Cliente","Proveedor","ElementoCatalogo","MovimientoStock","Inventario","bi.OperacionReferencia"})Assert.Equal(0,await db.Scalar($"SELECT COUNT(*) FROM {table}"));
    }
    [Fact(DisplayName="PI-BI-007 | Fallo y bloqueo SQL del BI devuelven 503 controlado y el negocio sigue operativo")]
    public async Task FallosControlados()
    {
        await using var db=await QaDatabase.CreateAsync();await Migrar(db);await using var api=await ApiHost.Start(db);await ApiTests.Login(api);
        var url="/api/bi/resumen?desde=2026-04-01&hasta=2026-10-12";
        await db.Exec("DROP VIEW bi.FactVenta");using(var fallo=await api.Http.GetAsync(url)){Assert.Equal(HttpStatusCode.ServiceUnavailable,fallo.StatusCode);var texto=await fallo.Content.ReadAsStringAsync();Assert.Contains("mensaje",texto);Assert.DoesNotContain("SqlException",texto);Assert.DoesNotContain("SELECT",texto);}
        Assert.Equal(HttpStatusCode.OK,(await api.Http.GetAsync("/api/publico/productos")).StatusCode);await Migrar(db);
        await using(var conn=new Microsoft.Data.SqlClient.SqlConnection(db.Connection))
        {
            await conn.OpenAsync();await using var transaction=(Microsoft.Data.SqlClient.SqlTransaction)await conn.BeginTransactionAsync();
            await using(var command=new Microsoft.Data.SqlClient.SqlCommand("SELECT COUNT(*) FROM dbo.Venta WITH(TABLOCKX,HOLDLOCK)",conn,transaction))await command.ExecuteScalarAsync();
            using var bloqueo=await api.Http.GetAsync(url);Assert.Equal(HttpStatusCode.ServiceUnavailable,bloqueo.StatusCode);await transaction.RollbackAsync();
        }
        Assert.Equal(HttpStatusCode.OK,(await api.Http.GetAsync(url)).StatusCode);
        using var comercial=await api.Http.PostAsJsonAsync("/api/cotizacion",new{idCliente=1,origenCotizacion="Manual",detalles=new[]{new{idElementoCatalogo=1,cantidad=1,precioUnitario=118}}});Assert.Equal(HttpStatusCode.Created,comercial.StatusCode);
        var f=Filtro();f.Elemento=1;await db.Exec("UPDATE Inventario SET StockActual=10");var historico=await Servicio(db).ConsultarAsync("inventario",f,default);Assert.Empty(historico.Datos["saldosHistoricos"]);Assert.Null(historico.Datos["rotacion"][0]["Rotacion"]);
    }
}
