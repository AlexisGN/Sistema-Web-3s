using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Sistema3S.Tests.Fixtures;
using Sistema3S.Web.Services.Bi;
using Sistema3S.Web.Services.Ia;

namespace Sistema3S.Tests.Integration;

[Trait("Category","Integration")]
public class IaCalendarioTests
{
    [Fact]
    public async Task VentasConfirmadasAnterioresAlAltaCuentanYSeExcluyenAnuladasFuturasYSinDetalle()
    {
        await using var q=await QaDatabase.CreateAsync();await IaTests.Migrar(q);
        var cutoff=IaReglas.Hoy.AddDays(-1);
        await q.Exec($"""
            UPDATE ElementoCatalogo SET FechaRegistro=GETDATE();
            INSERT Venta(IdCliente,IdUsuarioRegistro,IdEstadoVenta,FechaVenta,Subtotal,Igv,Total) VALUES
            (1,1,1,'{cutoff.AddDays(-120):yyyyMMdd}',100,18,118),
            (1,1,1,'{cutoff.AddDays(-80):yyyyMMdd}',100,18,118),
            (1,1,1,'{cutoff.AddDays(-40):yyyyMMdd}',100,18,118),
            (1,1,1,'{cutoff.AddDays(-5):yyyyMMdd}',100,18,118),
            (1,1,2,'{cutoff.AddDays(-300):yyyyMMdd}',100,18,118),
            (1,1,1,'{cutoff.AddDays(40):yyyyMMdd}',100,18,118),
            (1,1,1,'{cutoff.AddDays(-200):yyyyMMdd}',100,18,118);
            INSERT DetalleVenta(IdVenta,IdElementoCatalogo,Cantidad,PrecioUnitario,Subtotal)VALUES
            (1,1,1,118,118),(2,1,1,118,118),(3,1,1,118,118),(4,1,1,118,118),(5,1,99,118,11682),(6,1,99,118,11682);
            """);
        var data=new IaDatos(new BiConexion(Config(q)));
        var input=await data.Extraer("operativo",cutoff,default);
        var product=Assert.Single(input.Products);
        Assert.Equal(cutoff.AddDays(-120).ToString("yyyy-MM-dd"),product.Start);
        Assert.Equal(121,product.Days.Count);Assert.Equal(4,product.Days.Count(d=>d.Quantity>0));
        Assert.Equal(4,product.Days.Sum(d=>d.Quantity));
        var tomorrow=await data.Extraer("operativo",cutoff.AddDays(1),default);
        Assert.Equal(product.Start,tomorrow.Products[0].Start);Assert.Equal(122,tomorrow.Products[0].Days.Count);
    }

    [Fact]
    public async Task LasConsultasPorFechaNoModificanSnapshotsNiRecomendaciones()
    {
        await using var q=await QaDatabase.CreateAsync();await IaTests.Migrar(q);
        var today=IaReglas.Hoy;
        var daily=Enumerable.Range(1,90).Select(i=>new{date=today.AddDays(i).ToString("yyyy-MM-dd"),quantity=1});
        var result=JsonSerializer.Serialize(new{products=new[]{new{id=1,sufficient=true,first=today.AddDays(1).ToString("yyyy-MM-dd"),days=120,events=4,
            daily,totals=new Dictionary<string,int>{{"30",30},{"60",60},{"90",90}},nextMonth=30,history=Array.Empty<object>()}}});
        await q.Exec($"""
            INSERT ia.Ejecucion(Procedencia,Corte,Huella,Estado,Finalizada,Resultado)
            VALUES('operativo','{today.AddDays(-1):yyyyMMdd}','{new string('c',64)}','Completada',SYSUTCDATETIME(),N'{result.Replace("'","''")}');
            INSERT ia.Recomendacion(Procedencia,IdProducto,IdEjecucion,Prioridad,Cantidad,FechaPedido,Mensaje,Firma) VALUES
            ('operativo',1,1,'Alta',3,'{today.AddDays(2):yyyyMMdd}',N'QA','{new string('a',64)}'),
            ('operativo',1,1,'Alta',4,'{today.AddDays(20):yyyyMMdd}',N'QA','{new string('b',64)}'),
            ('operativo',1,1,'Revisar stock',NULL,NULL,N'QA','{new string('d',64)}');
            """);
        var con=new BiConexion(Config(q));var service=new IaService(con,new IaDatos(con),Config(q),new QaEnvironment());
        foreach(var view in new[]{"resumen","pronostico","proyeccion","inventario","compras","seguimiento"})
        {
            var response=JsonSerializer.SerializeToElement(await service.Consultar(view,"operativo",null,default,today.AddDays(10)),IaMotor.Json);
            Assert.Equal(10,response.GetProperty("periodo").GetProperty("dias").GetInt32());
            if(view=="pronostico")
            {
                var p=response.GetProperty("productos")[0];Assert.Equal(10,p.GetProperty("periodoPersonalizado").GetProperty("cantidad").GetInt32());
                Assert.Equal(90,p.GetProperty("cantidades").GetProperty("90").GetInt32());Assert.Equal(10,p.GetProperty("estimado").GetArrayLength());
            }
            if(view is "compras" or "seguimiento")Assert.Equal(2,response.GetProperty("recomendaciones").GetArrayLength());
        }
        Assert.Equal(1,await q.Scalar("SELECT COUNT(*) FROM ia.Ejecucion"));
        Assert.Equal(3,await q.Scalar("SELECT COUNT(*) FROM ia.Recomendacion"));
        Assert.Equal(7,await q.Scalar("SELECT SUM(Cantidad) FROM ia.Recomendacion"));
        Assert.Equal(0,await q.Scalar("SELECT COUNT(*) FROM Compra"));
        Assert.Equal(0,await q.Scalar("SELECT COUNT(*) FROM ia.Pronostico"));
    }
    private static IConfiguration Config(QaDatabase q)=>new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{
        ["ConnectionStrings:BD_3S"]=q.Connection,["Ia:HistorialOperativoVerificado"]="true"}).Build();
    private sealed class QaEnvironment:IWebHostEnvironment
    {
        public string EnvironmentName{get;set;}="Development";public string ApplicationName{get;set;}="Sistema3S";
        public string ContentRootPath{get;set;}=AppContext.BaseDirectory;public string WebRootPath{get;set;}=AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider{get;set;}=new NullFileProvider();public IFileProvider WebRootFileProvider{get;set;}=new NullFileProvider();
    }
}
