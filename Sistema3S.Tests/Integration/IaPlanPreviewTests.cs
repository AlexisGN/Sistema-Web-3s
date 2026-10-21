using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Sistema3S.Tests.Fixtures;
using Sistema3S.Web.Services.Ia;
namespace Sistema3S.Tests.Integration;
public sealed class IaPlanPreviewTests
{
 [Fact] public async Task VistaPreviaExigePermisoYCalculaSinGuardarNiCambiarInventario()
 {
  await using var db=await QaDatabase.CreateAsync();await IaTests.Migrar(db);var hoy=IaReglas.Hoy;
  var prediction=new{products=new[]{new{id=1,sufficient=true,first=hoy.AddDays(1).ToString("yyyy-MM-dd"),daily=Enumerable.Range(1,90).Select(i=>new{date=hoy.AddDays(i).ToString("yyyy-MM-dd"),quantity=1})}}};
  var json=JsonSerializer.Serialize(prediction).Replace("'","''");
  await db.Exec($"UPDATE Inventario SET StockActual=4 WHERE IdProducto=1; INSERT ia.Ejecucion(Procedencia,Corte,Huella,Estado,Finalizada,Resultado) VALUES('operativo','{hoy.AddDays(-1):yyyyMMdd}','{new string('d',64)}','Completada',SYSUTCDATETIME(),N'{json}');");
  await using var api=await ApiHost.Start(db,new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"Ia:Habilitada","false"},{"Ia:HistorialOperativoVerificado","true"}}).Build());
  var dto=new IaPolitica(1,1,10,30,0,1,1,0,0,null,"QA",hoy.AddDays(20));
  Assert.Equal(HttpStatusCode.Unauthorized,(await api.Http.PostAsJsonAsync("/api/ia/abastecimiento/vista-previa",dto)).StatusCode);
  await ApiTests.Login(api,"compras@example.test");await db.Exec("INSERT RolPermiso(IdRol,IdPermiso) SELECT 2,IdPermiso FROM Permiso WHERE Nombre='IA_VER'");
  Assert.Equal(HttpStatusCode.Forbidden,(await api.Http.PostAsJsonAsync("/api/ia/abastecimiento/vista-previa",dto)).StatusCode);
  await db.Exec("INSERT RolPermiso(IdRol,IdPermiso) SELECT 2,IdPermiso FROM Permiso WHERE Nombre='IA_CONFIGURAR_ABASTECIMIENTO'");
  for(var i=0;i<2;i++){
   var response=await api.Http.PostAsJsonAsync("/api/ia/abastecimiento/vista-previa",dto);Assert.Equal(HttpStatusCode.OK,response.StatusCode);
   var r=await response.Content.ReadFromJsonAsync<JsonElement>();Assert.False(r.GetProperty("guardado").GetBoolean());var risk=r.GetProperty("riesgo");Assert.Equal(36,risk.GetProperty("cantidad").GetDecimal());Assert.Equal(hoy.ToString("yyyy-MM-dd"),risk.GetProperty("fechaPedido").GetString());Assert.Equal(6,risk.GetProperty("retrasoDias").GetInt32());
  }
  Assert.Equal(4,await db.Scalar("SELECT StockActual FROM Inventario WHERE IdProducto=1"));
  foreach(var table in new[]{"ia.Abastecimiento","ia.Recomendacion","ia.SolicitudCompra","Compra","Venta","MovimientoStock"})Assert.Equal(0,await db.Scalar("SELECT COUNT(*) FROM "+table));
  await db.Exec("UPDATE Proveedor SET Estado=0 WHERE IdProveedor=1");Assert.Equal(HttpStatusCode.BadRequest,(await api.Http.PostAsJsonAsync("/api/ia/abastecimiento/vista-previa",dto)).StatusCode);
 }
}
