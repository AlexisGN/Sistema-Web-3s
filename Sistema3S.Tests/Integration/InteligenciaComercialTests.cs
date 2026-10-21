using Sistema3S.Tests.Fixtures;
using Sistema3S.Web.Services.Comercial;
using Sistema3S.Web.Services.Bi;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http.Json;
namespace Sistema3S.Tests.Integration;

public sealed class InteligenciaComercialTests
{
 [Fact]public async Task GeneralSinComparacionYActivacionExplicita(){
  await using var db=await QaDatabase.CreateAsync();await Preparar(db);var s=Service(db);var f=Filtro();
  var normal=await s.Consultar("resumen",f,default);Assert.False(f.Comparar);Assert.Single(normal.Datos["periodos"]);Assert.Equal("A",normal.Datos["periodos"][0]["Periodo"]);Assert.All(normal.Datos["productos"],p=>Assert.False(p.ContainsKey("B")));Assert.All(normal.Datos["categorias"],p=>Assert.Equal("A",p["Periodo"]));
  QuestPDF.Settings.License=QuestPDF.Infrastructure.LicenseType.Community;Assert.True(new ComercialExportacion().Pdf(normal).Length>3000);
  f.Comparar=true;var compared=await s.Consultar("resumen",f,default);Assert.Equal(2,compared.Datos["periodos"].Count);Assert.Equal(300m,Convert.ToDecimal(compared.Datos["periodos"][1]["VentaNeta"]));
  f.Comparar=false;f.Escenario="categorias-caida";var categories=await s.Consultar("productos",f,default);Assert.Single(categories.Datos["periodos"]);Assert.Single(categories.Datos["categorias"]);Assert.Equal(100m,Convert.ToDecimal(categories.Datos["categorias"][0]["Importe"]));
  f.Escenario="todos";var returned=await s.Consultar("resumen",f,default);Assert.Single(returned.Datos["periodos"]);Assert.False(f.NecesitaBase);Assert.Equal(4,await db.Scalar("SELECT COUNT(*) FROM Venta"));
 }
 static ComercialFiltro Filtro()=>new(){Desde=new(2026,9,1),Hasta=new(2026,9,30),DesdeB=new(2026,8,1),HastaB=new(2026,8,31),VentanaConversion=10};
 static ComercialService Service(QaDatabase db)=>new(new BiConexion(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"ConnectionStrings:BD_3S",db.Connection}}).Build()));
 static async Task Preparar(QaDatabase db){await BiTests.Migrar(db);await db.Exec("""
 INSERT Cotizacion(IdCliente,IdUsuarioRegistro,IdEstadoCotizacion,FechaCotizacion,Subtotal,Descuento,Igv,Total) VALUES(1,1,1,'20260801',236,0,36,236),(1,1,1,'20260802',118,0,18,118);
 INSERT DetalleCotizacion(IdCotizacion,IdElementoCatalogo,Cantidad,PrecioUnitario,Subtotal) VALUES(1,1,2,118,236),(2,1,1,118,118);
 INSERT Venta(IdCliente,IdCotizacion,IdUsuarioRegistro,IdEstadoVenta,FechaVenta,Subtotal,Igv,Total) VALUES(1,1,1,1,'20260801',200,36,236),(1,NULL,1,1,'20260802',100,18,118),(1,2,1,1,'20260901',100,18,118),(1,NULL,1,2,'20260902',900,162,1062);
 INSERT DetalleVenta(IdVenta,IdElementoCatalogo,Cantidad,PrecioUnitario,Subtotal) VALUES(1,1,2,118,236),(2,1,1,118,118),(3,1,1,118,118),(4,1,9,118,1062);
 UPDATE Inventario SET StockActual=0;
 INSERT ElementoCatalogo(IdTipoElemento,Nombre,PrecioReferencial,Estado) VALUES(1,N'Inactivo QA',118,0);
 INSERT Producto(IdElementoCatalogo,IdCategoria,IdUnidadMedida,CodigoProducto,AplicaInventario) VALUES(2,1,1,N'QA-INACTIVO',1);
 INSERT Inventario(IdProducto,StockActual,StockMinimo) VALUES(2,0,5);
 """);}
 [Fact]public async Task NetosRecurrenciaConversionYUniversoActivo(){await using var db=await QaDatabase.CreateAsync();await Preparar(db);var f=Filtro();f.Desde=new(2026,8,1);f.Hasta=new(2026,8,31);f.DesdeB=new(2026,9,1);f.HastaB=new(2026,9,30);var r=await Service(db).Consultar("comparacion",f,default);var a=r.Datos["periodos"][0];var b=r.Datos["periodos"][1];Assert.Equal(300m,Convert.ToDecimal(a["VentaNeta"]));Assert.Equal(100m,Convert.ToDecimal(b["VentaNeta"]));Assert.Equal(2,Convert.ToInt32(a["Elegibles"]));Assert.Equal(1,Convert.ToInt32(a["Convertidas"]));Assert.Equal(50m,Convert.ToDecimal(a["Conversion"]));Assert.Equal(0m,Convert.ToDecimal(a["TasaRecurrentes"]));Assert.Equal(100m,Convert.ToDecimal(b["TasaRecurrentes"]));Assert.Equal(1,Convert.ToInt32(r.Datos["stock"][0]["Productos"]));Assert.Equal(100m,Convert.ToDecimal(r.Datos["stock"][0]["PorcentajeSinStock"]));Assert.Equal(100m,Convert.ToDecimal(r.Datos["stock"][0]["PorcentajeCritico"]));}
 [Fact]public async Task FiltrosCombinadosPreservanContextoYNoEscriben(){await using var db=await QaDatabase.CreateAsync();await Preparar(db);var s=Service(db);var f=Filtro();f.Escenario="clientes-caida";f.Umbral=50;var r=await s.Consultar("clientes",f,default);Assert.Single(r.Datos["clientes"]);Assert.Equal(-200m,Convert.ToDecimal(r.Datos["clientes"][0]["Diferencia"]));f.Umbral=80;r=await s.Consultar("clientes",f,default);Assert.Empty(r.Datos["clientes"]);Assert.Equal(100m,Convert.ToDecimal(r.Datos["periodos"][0]["VentaNeta"]));f.Escenario="criticos-ventas";r=await s.Consultar("inventario",f,default);Assert.Single(r.Datos["inventario"]);Assert.Equal(1,Convert.ToInt32(r.Datos["inventario"][0]["Id"]));Assert.Equal(4,await db.Scalar("SELECT COUNT(*) FROM Venta"));Assert.Equal(0,await db.Scalar("SELECT StockActual FROM Inventario WHERE IdProducto=1"));}
 [Fact]public async Task VacioYVistasNoInventanDatos(){await using var db=await QaDatabase.CreateAsync();await BiTests.Migrar(db);foreach(var vista in new[]{"resumen","ventas","productos","clientes","inventario","compras","comparacion"}){var r=await Service(db).Consultar(vista,Filtro(),default);Assert.All(r.Datos["periodos"],p=>{Assert.Equal(0m,Convert.ToDecimal(p["VentaNeta"]));Assert.Null(p["Ticket"]);Assert.Null(p["Conversion"]);Assert.Null(p["TasaRecurrentes"]);});}}
 [Fact]public async Task ApiPermisosYExportacionUsanMismaFuente(){await using var db=await QaDatabase.CreateAsync();await Preparar(db);await using var api=await ApiHost.Start(db);const string query="?desde=2026-09-01&hasta=2026-09-30&desdeB=2026-08-01&hastaB=2026-08-31&ventanaConversion=10";Assert.Equal(HttpStatusCode.Unauthorized,(await api.Http.GetAsync("/api/inteligencia/resumen"+query)).StatusCode);await ApiTests.Login(api,"compras@example.test");Assert.Equal(HttpStatusCode.Forbidden,(await api.Http.GetAsync("/api/inteligencia/resumen"+query)).StatusCode);await ApiTests.Login(api);Assert.Equal(HttpStatusCode.OK,(await api.Http.GetAsync("/api/inteligencia/resumen"+query)).StatusCode);using var response=await api.Http.GetAsync("/api/inteligencia/comparacion/excel"+query);Assert.Equal(HttpStatusCode.OK,response.StatusCode);using var wb=new ClosedXML.Excel.XLWorkbook(new MemoryStream(await response.Content.ReadAsByteArrayAsync()));var sheet=wb.Worksheet("periodos");var column=sheet.Row(1).CellsUsed().Single(c=>c.GetString()=="VentaNeta").Address.ColumnNumber;Assert.Equal(100m,sheet.Cell(2,column).GetValue<decimal>());Assert.Equal(300m,sheet.Cell(3,column).GetValue<decimal>());Assert.Equal(HttpStatusCode.BadRequest,(await api.Http.GetAsync("/api/inteligencia/resumen"+query+"&escenario=inventado")).StatusCode);}
 [Fact]public async Task ExportacionPdfLegibleYClienteSinAcceso(){
  await using var db=await QaDatabase.CreateAsync();await Preparar(db);await using var api=await ApiHost.Start(db);
  await ApiTests.Json(await api.Http.PostAsJsonAsync("/api/cliente-web/registro",new{tipoDocumento="DNI",numeroDocumento="00000002",correo="comercial-cliente@example.test",telefono="+51999999998",direccion="QA",contrasena=QaDatabase.Password,confirmarContrasena=QaDatabase.Password,nombres="Cliente",apellidoPaterno="Comercial",apellidoMaterno="QA"}));
  var login=await ApiTests.Json(await api.Http.PostAsJsonAsync("/api/cliente-web/login",new{correo="comercial-cliente@example.test",contrasena=QaDatabase.Password}));api.Http.DefaultRequestHeaders.Authorization=new("Bearer",login.GetProperty("token").GetString());Assert.Equal(HttpStatusCode.Forbidden,(await api.Http.GetAsync("/api/inteligencia/resumen")).StatusCode);
  await ApiTests.Login(api);using var pdf=await api.Http.GetAsync("/api/inteligencia/comparacion/pdf?desde=2026-09-01&hasta=2026-09-30&desdeB=2026-08-01&hastaB=2026-08-31");
  Assert.Equal(HttpStatusCode.OK,pdf.StatusCode);var bytes=await pdf.Content.ReadAsByteArrayAsync();Assert.Equal("%PDF-",System.Text.Encoding.ASCII.GetString(bytes,0,5));Assert.True(bytes.Length>3000);
 }
 [Fact]public async Task ExperimentalLeePersistidoSinPythonNiOrdenes(){
  await using var db=await QaDatabase.CreateAsync();await IaTests.Migrar(db);
  var project=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../"));foreach(var file in new[]{"20261016_IA_MotorAdministrado.sql","20261019_IA_Adaptativa.sql"})await db.Exec(await File.ReadAllTextAsync(Path.Combine(project,"Sistema3S.Web/Database",file)));
  await db.Exec("UPDATE ElementoCatalogo SET Nombre=N'Válvula eléctrica de prueba' WHERE IdElementoCatalogo=1");
  var id=Guid.NewGuid();var cutoff=Sistema3S.Web.Services.Ia.IaReglas.Hoy.AddDays(-1);var empty="{}";
  var result=System.Text.Json.JsonSerializer.Serialize(new{modelId=id,version="3s-ia-8",products=new[]{new{id=1,name="Producto QA",unit="UN",sufficient=true,accepted=false,first=cutoff.AddDays(2).ToString("yyyy-MM-dd"),totals=new Dictionary<string,int>{{"30",12}},daily=new[]{new{date=cutoff.AddDays(2).ToString("yyyy-MM-dd"),quantity=1}},history=Array.Empty<object>()}}});
  await db.Exec($"""
   INSERT ia.Modelo(Id,Version,Alcance,Corte,Huella,Artefacto,Evaluacion,Elegible,Activo) VALUES('{id}','3s-ia-8','production','{cutoff:yyyyMMdd}','{new string('1',64)}','qa-sin-python','{empty}',0,0);
   INSERT ia.Ejecucion(Procedencia,Corte,Huella,Estado,Resultado,Finalizada) VALUES('operativo','{cutoff:yyyyMMdd}','{new string('2',64)}','Sombra',N'{result.Replace("'","''")}',SYSUTCDATETIME());
  """);
  var cfg=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"Ia:Python","no-existe-python"},{"Ia:Habilitada","false"}}).Build();await using var api=await ApiHost.Start(db,cfg);
  await ApiTests.Login(api,"compras@example.test");Assert.Equal(HttpStatusCode.Forbidden,(await api.Http.GetAsync("/api/ia/experimental")).StatusCode);await ApiTests.Login(api);
  var r=await ApiTests.Json(await api.Http.GetAsync("/api/ia/experimental"));Assert.False(r.GetProperty("compraAutomatica").GetBoolean());Assert.False(r.GetProperty("productos")[0].GetProperty("validated").GetBoolean());Assert.Equal(12,r.GetProperty("productos")[0].GetProperty("totals").GetProperty("30").GetInt32());Assert.Equal("Válvula eléctrica de prueba",r.GetProperty("productos")[0].GetProperty("name").GetString());Assert.EndsWith("Z",r.GetProperty("actualizado").GetString());
  Assert.Equal(0,await db.Scalar("SELECT COUNT(*) FROM ia.Trabajo"));Assert.Equal(0,await db.Scalar("SELECT COUNT(*) FROM Compra"));Assert.Equal(1,await db.Scalar("SELECT COUNT(*) FROM ia.Ejecucion"));
 }

 [Fact]public async Task ComprasTienenTotalesPropiosYExportacionSinVentas(){
  await using var db=await QaDatabase.CreateAsync();await Preparar(db);
  await db.Exec("""
  INSERT Compra(IdProveedor,IdUsuarioRegistro,IdEstadoCompra,FechaCompra,Subtotal,Igv,Total) VALUES(1,1,1,'20260902',200,36,236),(1,1,1,'20260802',50,9,59);
  INSERT DetalleCompra(IdCompra,IdProducto,Cantidad,PrecioCompra,Subtotal) VALUES(1,1,1,118,118),(1,1,1,118,118),(2,1,1,59,59);
  """);
  var f=Filtro();f.Comparar=true;var r=await Service(db).Consultar("compras",f,default);var a=r.Datos["periodosCompras"][0];var b=r.Datos["periodosCompras"][1];
  Assert.Equal(200m,Convert.ToDecimal(a["CompraNeta"]));Assert.Equal(1,Convert.ToInt32(a["Compras"]));Assert.Equal(200m,Convert.ToDecimal(a["PromedioCompra"]));Assert.Equal(1,Convert.ToInt32(a["Proveedores"]));Assert.Equal(50m,Convert.ToDecimal(b["CompraNeta"]));Assert.Equal(2,r.Datos["evolucionCompras"].Count);Assert.Equal(100m,Convert.ToDecimal(r.Datos["periodos"][0]["VentaNeta"]));
  var exporter=new ComercialExportacion();using(var wb=new ClosedXML.Excel.XLWorkbook(new MemoryStream(exporter.Excel(r)))){Assert.False(wb.Worksheets.Any(w=>w.Name=="periodos"||w.Name=="stock"||w.Name=="clientes"));Assert.True(wb.Worksheets.Any(w=>w.Name=="periodosCompras"));Assert.Contains("Agosto",wb.Worksheet("evolucionCompras").Cell(2,2).GetString());}
  QuestPDF.Settings.License=QuestPDF.Infrastructure.LicenseType.Community;Assert.True(exporter.Pdf(r).Length>3000);
  r=await Service(db).Consultar("inventario",f,default);using(var wb=new ClosedXML.Excel.XLWorkbook(new MemoryStream(exporter.Excel(r))))Assert.Equal(new[]{"Criterios","stock","inventario"},wb.Worksheets.Select(w=>w.Name));Assert.True(exporter.Pdf(r).Length>3000);
  r=await Service(db).Consultar("ventas",f,default);using(var wb=new ClosedXML.Excel.XLWorkbook(new MemoryStream(exporter.Excel(r))))Assert.DoesNotContain(wb.Worksheet("periodos").Row(1).CellsUsed(),c=>c.GetString()=="Conversion");
  Assert.Equal(2,await db.Scalar("SELECT COUNT(*) FROM Compra"));Assert.Equal(0,await db.Scalar("SELECT StockActual FROM Inventario WHERE IdProducto=1"));
 }

 [Fact]public async Task DetalleStockCoincideConSegmentoImagenYPaginacion(){
  await using var db=await QaDatabase.CreateAsync();await Preparar(db);
  await db.Exec("""
   DECLARE @n int=1,@el int,@pr int;
   WHILE @n<=26 BEGIN
    INSERT ElementoCatalogo(IdTipoElemento,Nombre,PrecioReferencial,ImagenUrl) VALUES(1,CONCAT(N'Bajo QA ',FORMAT(@n,'00')),100,'/uploads/productos/qa.png');SET @el=SCOPE_IDENTITY();
    INSERT Producto(IdElementoCatalogo,IdCategoria,IdUnidadMedida,CodigoProducto,AplicaInventario) VALUES(@el,1,1,CONCAT('BAJO-QA-',@n),1);SET @pr=SCOPE_IDENTITY();
    INSERT Inventario(IdProducto,StockActual,StockMinimo) VALUES(@pr,2,5);SET @n+=1;
   END;
   INSERT ElementoCatalogo(IdTipoElemento,Nombre,PrecioReferencial) VALUES(1,N'Sobre QA',100);SET @el=SCOPE_IDENTITY();
   INSERT Producto(IdElementoCatalogo,IdCategoria,IdUnidadMedida,CodigoProducto,AplicaInventario) VALUES(@el,1,1,'SOBRE-QA',1);SET @pr=SCOPE_IDENTITY();
   INSERT Inventario(IdProducto,StockActual,StockMinimo) VALUES(@pr,9,5);
  """);
  var s=Service(db);var f=Filtro();var p1=await s.Detalle("stock","bajo-minimo",f,1,default);var p2=await s.Detalle("stock","bajo-minimo",f,2,default);
  Assert.Equal(26,p1.TotalRegistros);Assert.Equal(24,p1.Items.Count);Assert.Equal(2,p2.Items.Count);Assert.Empty(p1.Items.Select(x=>x["Id"]).Intersect(p2.Items.Select(x=>x["Id"])));
  Assert.All(p1.Items.Concat(p2.Items),x=>{Assert.Equal(2m,Convert.ToDecimal(x["StockActual"]));Assert.Equal("/uploads/productos/qa.png",x["ImagenUrl"]);});
  Assert.Single((await s.Detalle("stock","sin-stock",f,1,default)).Items);Assert.Single((await s.Detalle("stock","sobre-minimo",f,1,default)).Items);
  f.Elemento=Convert.ToInt32(p2.Items[0]["IdElementoCatalogo"]);Assert.Single((await s.Detalle("stock","bajo-minimo",f,1,default)).Items);
  Assert.Equal(4,await db.Scalar("SELECT COUNT(*) FROM Venta"));
 }
 [Fact]public async Task DetalleClientesConservaHistorialAnteriorYUniversoDelKpi(){
  await using var db=await QaDatabase.CreateAsync();await Preparar(db);
  await db.Exec("""
   INSERT Cliente(IdTipoCliente,IdTipoDocumento,NumeroDocumento,Estado) VALUES(1,1,'00000002',1),(1,1,'00000003',0);
   INSERT ClientePersonaNatural(IdCliente,Nombres,ApellidoPaterno) VALUES(2,N'Nuevo',N'QA'),(3,N'Recurrente inactivo',N'QA');
   INSERT Venta(IdCliente,IdUsuarioRegistro,IdEstadoVenta,FechaVenta,Subtotal,Igv,Total) VALUES(2,1,1,'20260903',100,18,118),(2,1,1,'20260904',100,18,118),(3,1,1,'20260801',100,18,118),(3,1,1,'20260902',100,18,118);
   INSERT DetalleVenta(IdVenta,IdElementoCatalogo,Cantidad,PrecioUnitario,Subtotal) VALUES(5,1,1,118,118),(6,1,1,118,118),(7,1,1,118,118),(8,1,1,118,118);
  """);
  var s=Service(db);var f=Filtro();var k=(await s.Consultar("resumen",f,default)).Datos["periodos"][0];var recurrentes=await s.Detalle("clientes","recurrentes",f,1,default);var nuevos=await s.Detalle("clientes","nuevos",f,1,default);
  Assert.Equal(Convert.ToInt32(k["Recurrentes"]),recurrentes.TotalRegistros);Assert.Equal(2,recurrentes.TotalRegistros);Assert.Contains(recurrentes.Items,x=>Convert.ToInt32(x["Id"])==3&&Convert.ToInt32(x["Activo"])==0);
  Assert.Single(nuevos.Items);Assert.Equal(2,Convert.ToInt32(nuevos.Items[0]["Id"]));Assert.Equal(2,Convert.ToInt32(nuevos.Items[0]["Operaciones"]));
  f.Cliente=2;Assert.Equal(0,(await s.Detalle("clientes","recurrentes",f,1,default)).TotalRegistros);Assert.Equal(1,(await s.Detalle("clientes","nuevos",f,1,default)).TotalRegistros);
 }
 [Fact]public async Task DetalleTienePermisosValidacionYMetadatosDeCategoria(){
  await using var db=await QaDatabase.CreateAsync();await Preparar(db);await using var api=await ApiHost.Start(db);
  const string path="/api/inteligencia/detalle/stock?desde=2026-09-01&hasta=2026-09-30&segmento=sin-stock";
  Assert.Equal(HttpStatusCode.Unauthorized,(await api.Http.GetAsync(path)).StatusCode);await ApiTests.Login(api,"compras@example.test");Assert.Equal(HttpStatusCode.Forbidden,(await api.Http.GetAsync(path)).StatusCode);await ApiTests.Login(api);var r=await ApiTests.Json(await api.Http.GetAsync(path));Assert.Equal(1,r.GetProperty("totalRegistros").GetInt32());
  Assert.Equal(HttpStatusCode.BadRequest,(await api.Http.GetAsync(path+"&pagina=0")).StatusCode);Assert.Equal(HttpStatusCode.BadRequest,(await api.Http.GetAsync(path.Replace("sin-stock","invalido"))).StatusCode);
  var choices=await BiTests.Servicio(db).OpcionesAsync("operativo","",default);Assert.All(choices["elementos"],e=>Assert.True(e.ContainsKey("IdCategoria")));
 }
 [Fact]public async Task ClientesDesactivadosQuedanAlFinalAntesDePaginar(){
  await using var db=await QaDatabase.CreateAsync();
  await db.Exec("""
   INSERT Cliente(IdTipoCliente,IdTipoDocumento,NumeroDocumento,Estado) VALUES(1,1,'00000002',1),(1,1,'00000003',0),(1,1,'00000004',0);
   INSERT ClientePersonaNatural(IdCliente,Nombres,ApellidoPaterno) VALUES(2,N'Segundo',N'QA'),(3,N'Tercero',N'QA'),(4,N'Cuarto',N'QA');
  """);
  await using var context=db.Open();var s=new Sistema3S.Web.Services.Implementations.ClienteService(context);var a=await s.ListarAsync(null,1,2);var b=await s.ListarAsync(null,2,2);
  Assert.All(a.Items,c=>Assert.True(c.Estado));Assert.All(b.Items,c=>Assert.False(c.Estado));Assert.Equal(new[]{2,1,4,3},a.Items.Concat(b.Items).Select(c=>c.IdCliente));Assert.Equal(4,a.TotalRegistros);
  var buscar=await s.ListarAsync("Tercero",1,5);Assert.Single(buscar.Items);Assert.False(buscar.Items[0].Estado);var activos=await s.ListarAsync(null,1,5,true);Assert.Equal(2,activos.TotalRegistros);Assert.All(activos.Items,c=>Assert.True(c.Estado));
 }
}
