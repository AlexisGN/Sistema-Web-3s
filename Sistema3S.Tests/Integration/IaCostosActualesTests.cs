using Microsoft.Extensions.Configuration;
using Sistema3S.Tests.Fixtures;
using Sistema3S.Web.Services.Bi;
using Sistema3S.Web.Services.Ia;
namespace Sistema3S.Tests.Integration;
[Trait("Category","Integration")]
public class IaCostosActualesTests
{
 [Fact] public async Task CostoYPrecioIncluyenHoyPeroNoAnulacionesFuturosOComprasAntiguas()
 {
  await using var q=await QaDatabase.CreateAsync();await IaTests.Migrar(q);var today=IaReglas.Hoy;
  await q.Exec($"""
   INSERT Compra(IdProveedor,IdUsuarioRegistro,IdEstadoCompra,FechaCompra,Subtotal,Igv,Total) VALUES
   (1,1,1,'{today.AddDays(-100):yyyyMMdd}',1000,180,1180),
   (1,1,1,'{today:yyyyMMdd}',80,20,100),
   (1,1,2,'{today:yyyyMMdd}',1000,180,1180),
   (1,1,1,'{today.AddDays(1):yyyyMMdd}',1000,180,1180);
   INSERT DetalleCompra(IdCompra,IdProducto,Cantidad,PrecioCompra,Subtotal) VALUES
   (1,1,1,1180,1180),(2,1,2,50,100),(3,1,1,1180,1180),(4,1,1,1180,1180);
   INSERT Venta(IdCliente,IdUsuarioRegistro,IdEstadoVenta,FechaVenta,Subtotal,Igv,Total) VALUES
   (1,1,1,'{today:yyyyMMdd}',100,18,118),(1,1,2,'{today:yyyyMMdd}',999,18,1017);
   INSERT DetalleVenta(IdVenta,IdElementoCatalogo,Cantidad,PrecioUnitario,Subtotal) VALUES(1,1,1,118,118),(2,1,1,1017,1017);
   """);
  var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"ConnectionStrings:BD_3S",q.Connection}}).Build();
  var data=new IaDatos(new BiConexion(config));var p=Assert.Single(await data.Contexto("operativo",default));
  Assert.Equal(40,p.CostoNeto);Assert.Equal(100,p.PrecioNeto);Assert.Equal(today,p.FechaCosto);Assert.Equal(today,p.FechaPrecio);
  var history=await data.Extraer("operativo",today.AddDays(-1),default);Assert.Empty(history.Products);
  Assert.Equal(4,await q.Scalar("SELECT COUNT(*) FROM Compra"));Assert.Equal(2,await q.Scalar("SELECT COUNT(*) FROM Venta"));
 }
}
