using Microsoft.Extensions.Configuration;
using Sistema3S.Tests.Fixtures;
using Sistema3S.Web.Services.Bi;
using Sistema3S.Web.Services.Ia;
namespace Sistema3S.Tests.Integration;
[Trait("Category","Integration")]
public class IaProveedorTests
{
 [Fact] public async Task ProveedorYUltimoCostoHistoricoSeReconocenSinCrearPolitica()
 {
  await using var db=await QaDatabase.CreateAsync();await IaTests.Migrar(db);var today=IaReglas.Hoy;
  await db.Exec($"""
   INSERT Compra(IdProveedor,IdUsuarioRegistro,IdEstadoCompra,FechaCompra,Subtotal,Igv,Total) VALUES
   (1,1,1,'{today.AddDays(-120):yyyyMMdd}',100,18,118),
   (1,1,2,'{today:yyyyMMdd}',999,18,1017),
   (1,1,1,'{today.AddDays(1):yyyyMMdd}',999,18,1017);
   INSERT DetalleCompra(IdCompra,IdProducto,Cantidad,PrecioCompra,Subtotal) VALUES(1,1,1,118,118),(2,1,1,1017,1017),(3,1,1,1017,1017);
   """);
  var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"ConnectionStrings:BD_3S",db.Connection}}).Build();
  var data=new IaDatos(new BiConexion(config));var p=Assert.Single(await data.Contexto("operativo",default));
  Assert.Equal(1,p.IdProveedor);Assert.False(string.IsNullOrWhiteSpace(p.Proveedor));Assert.Null(p.Politica);Assert.Equal(100,p.CostoNeto);Assert.Equal(today.AddDays(-120),p.FechaCosto);
  Assert.Equal(0,await db.Scalar("SELECT COUNT(*) FROM ia.Abastecimiento"));
  await db.Exec("UPDATE Proveedor SET Estado=0 WHERE IdProveedor=1");p=Assert.Single(await data.Contexto("operativo",default));Assert.Null(p.IdProveedor);Assert.Null(p.Proveedor);
 }
}
