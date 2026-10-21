using Sistema3S.Tests.Fixtures;
using Sistema3S.Web.DTOs.Bi;

namespace Sistema3S.Tests.Integration;

[Trait("Category","Integration")]
public sealed class BiComparacionComercialTests
{
    private static async Task Preparar(QaDatabase db)
    {
        await BiTests.Migrar(db);
        await db.Exec("""
          INSERT UnidadMedida(Nombre,Abreviatura) VALUES(N'Litro',N'L');
          INSERT ElementoCatalogo(IdTipoElemento,Nombre,PrecioReferencial) VALUES
            (1,N'Producto B',100),(1,N'Producto litros',50),(2,N'Servicio A',300),(2,N'Servicio B',300),(1,N'Producto sin ventas',100);
          INSERT Producto(IdElementoCatalogo,IdCategoria,IdUnidadMedida,CodigoProducto) VALUES(2,1,1,N'QA-B'),(3,1,2,N'QA-L'),(6,1,1,N'QA-CERO');
          INSERT Servicio(IdElementoCatalogo) VALUES(4),(5);
          UPDATE ElementoCatalogo SET FechaRegistro='20260101';
          INSERT Cotizacion(IdCliente,IdUsuarioRegistro,IdEstadoCotizacion,FechaCotizacion,Subtotal,Descuento,Igv,Total) VALUES
            (1,1,1,'20260901',100,0,0,100),(1,1,1,'20260902',100,0,0,100),(1,1,4,'20260903',50,0,0,50);
          INSERT DetalleCotizacion(IdCotizacion,IdElementoCatalogo,Cantidad,PrecioUnitario,Subtotal) VALUES(1,2,1,100,100),(2,2,1,100,100),(3,1,1,50,50);
          INSERT Venta(IdCliente,IdCotizacion,IdUsuarioRegistro,IdEstadoVenta,FechaVenta,Subtotal,Igv,Total) VALUES
            (1,NULL,1,1,'20260905',600,0,600),(1,2,1,1,'20260906',100,0,100),(1,NULL,1,2,'20260907',900,0,900),
            (1,NULL,1,1,'20260908',300,0,300),(1,NULL,1,1,'20260909',600,0,600),(1,NULL,1,1,'20260910',500,0,500);
          INSERT DetalleVenta(IdVenta,IdElementoCatalogo,Cantidad,PrecioUnitario,Subtotal) VALUES
            (1,1,2,100,200),(1,2,4,100,400),(2,2,1,100,100),(3,1,9,100,900),(4,4,1,300,300),(5,5,2,300,600),(6,3,10,50,500);
          INSERT Compra(IdProveedor,IdUsuarioRegistro,IdEstadoCompra,FechaCompra,Subtotal,Igv,Total) VALUES
            (1,1,1,'20260901',354,0,354),(1,1,2,'20260902',472,0,472);
          INSERT DetalleCompra(IdCompra,IdProducto,Cantidad,PrecioCompra,Subtotal) VALUES(1,1,2,59,118),(1,2,4,59,236),(2,1,8,59,472);
          """);
    }
    private static BiFiltro Filtro(int a=1,int b=2)=>new(){Desde=new(2026,4,1),Hasta=new(2026,10,12),Elemento=a,CompararElemento=b};

    [Fact(DisplayName="PI-BI-CMP-001 | Dos elementos: totales SQL, diferencias, meses cero, filtros y ventas compartidas")]
    public async Task Comparacion()
    {
        await using var db=await QaDatabase.CreateAsync();await Preparar(db);var s=BiTests.Servicio(db);
        var r=await s.ConsultarAsync("productos",Filtro(),default);var items=r.Datos["comparacionElementos"];
        Assert.Equal(2,items.Count);Assert.Equal(200,Convert.ToDecimal(items[0]["Importe"]));Assert.Equal(500,Convert.ToDecimal(items[1]["Importe"]));
        Assert.Equal(1,Convert.ToInt32(items[0]["Operaciones"]));Assert.Equal(2,Convert.ToInt32(items[1]["Operaciones"]));
        Assert.Equal(2,Convert.ToInt64(r.Datos["kpis"].Single(x=>Equals(x["Tipo"],"venta"))["Operaciones"]));
        var metricas=r.Datos["comparacionMetricas"];Assert.Equal(300,Convert.ToDecimal(metricas[0]["DiferenciaAbsoluta"]));Assert.Equal(150,Convert.ToDecimal(metricas[0]["DiferenciaPorcentual"]));
        Assert.Equal(3,Convert.ToDecimal(metricas[2]["DiferenciaAbsoluta"]));Assert.Equal(150,Convert.ToDecimal(metricas[2]["DiferenciaPorcentual"]));
        Assert.Equal(14,r.Datos["productosEvolucion"].Count);Assert.Equal(700,r.Datos["productosEvolucion"].Sum(x=>Convert.ToDecimal(x["Importe"])));
        Assert.Equal(12,r.Datos["productosEvolucion"].Count(x=>Convert.ToDecimal(x["Importe"])==0));
        Assert.Contains(r.Hallazgos,h=>h.Interpretacion.Contains("Producto B"));
        var f=Filtro();f.Cliente=999;r=await s.ConsultarAsync("productos",f,default);
        Assert.All(r.Datos["comparacionElementos"],x=>Assert.Equal(0,Convert.ToDecimal(x["Importe"])));Assert.Null(r.Datos["comparacionMetricas"][0]["DiferenciaPorcentual"]);
        r=await s.ConsultarAsync("productos",Filtro(6,2),default);Assert.Null(r.Datos["comparacionMetricas"][0]["DiferenciaPorcentual"]);
        Assert.Single(r.Datos["sinVentas"]);Assert.Equal(6,Convert.ToInt32(r.Datos["sinVentas"][0]["Id"]));
        await Assert.ThrowsAsync<ArgumentException>(()=>s.ConsultarAsync("productos",Filtro(1,1),default));
        await Assert.ThrowsAsync<ArgumentException>(()=>s.ConsultarAsync("productos",Filtro(1,999),default));
    }
    [Fact(DisplayName="PI-BI-CMP-002 | Cantidades incompatibles no generan diferencias; servicios sí son comparables")]
    public async Task Unidades()
    {
        await using var db=await QaDatabase.CreateAsync();await Preparar(db);var s=BiTests.Servicio(db);
        var r=await s.ConsultarAsync("productos",Filtro(1,3),default);var cantidad=r.Datos["comparacionMetricas"][2];
        Assert.Null(cantidad["DiferenciaAbsoluta"]);Assert.Null(cantidad["DiferenciaPorcentual"]);Assert.Equal("Unidades diferentes",cantidad["Unidad"]);
        Assert.Contains(r.Hallazgos,h=>h.Titulo=="Unidades diferentes");
        r=await s.ConsultarAsync("productos",Filtro(4,5),default);Assert.All(r.Datos["comparacionElementos"],x=>Assert.Equal("Servicio",x["TipoElemento"]));
        Assert.Equal(1,Convert.ToDecimal(r.Datos["comparacionMetricas"][2]["DiferenciaAbsoluta"]));Assert.Equal(100,Convert.ToDecimal(r.Datos["comparacionMetricas"][2]["DiferenciaPorcentual"]));
        var filtro=Filtro(1,2);filtro.Procedencia="referencia";r=await s.ConsultarAsync("productos",filtro,default);Assert.All(r.Datos["comparacionElementos"],x=>Assert.Equal(0,Convert.ToDecimal(x["Importe"])));
    }
    [Fact(DisplayName="PI-BI-DES-001 | Abastecimiento y oportunidades concilian, anuladas excluidas y desglose relacionado")]
    public async Task Desgloses()
    {
        await using var db=await QaDatabase.CreateAsync();await Preparar(db);var s=BiTests.Servicio(db);var f=BiTests.Filtro();
        var compras=await s.ConsultarAsync("compras",f,default);Assert.Equal(2,compras.Datos["abastecimiento"].Count);
        Assert.Equal(354,compras.Datos["abastecimiento"].Sum(x=>Convert.ToDecimal(x["Importe"])));Assert.All(compras.Datos["abastecimiento"],x=>Assert.Equal(59,Convert.ToDecimal(x["CostoUnitario"])));
        Assert.Contains(compras.Datos["operaciones"],x=>Equals(x["Estado"],"Anulada"));
        var clientes=await s.ConsultarAsync("clientes",f,default);var oportunidad=Assert.Single(clientes.Datos["oportunidadesClientes"]);
        Assert.Equal(3,Convert.ToInt32(oportunidad["Cotizaciones"]));Assert.Equal(1,Convert.ToInt32(oportunidad["Convertidas"]));Assert.Equal(1,Convert.ToInt32(oportunidad["Oportunidades"]));Assert.Equal(100,Convert.ToDecimal(oportunidad["ImportePendiente"]));
        var lineas=await s.DetalleAsync("venta",1,default);Assert.Equal(2,lineas.Count);Assert.Contains(lineas,x=>Convert.ToInt32(x["Id"])==2&&Convert.ToDecimal(x["Importe"])==400);
        Assert.Equal(0,await db.Scalar("SELECT StockActual FROM Inventario WHERE IdProducto=1"));
    }
}
