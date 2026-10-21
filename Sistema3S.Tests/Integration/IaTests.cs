using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Hosting;
using Sistema3S.Tests.Fixtures;
using Sistema3S.Web.Services.Bi;
using Sistema3S.Web.Services.Ia;
namespace Sistema3S.Tests.Integration;

public sealed class IaTests
{
 internal static async Task Migrar(QaDatabase db){await BiTests.Migrar(db);var path=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../Sistema3S.Web/Database/20261014_IA.sql"));foreach(var sql in Regex.Split(await File.ReadAllTextAsync(path),@"^GO\s*$",RegexOptions.Multiline|RegexOptions.IgnoreCase))if(!string.IsNullOrWhiteSpace(sql))await db.Exec(sql);}
 private static IConfiguration Config(QaDatabase db,bool verified=false)=>new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"ConnectionStrings:BD_3S",db.Connection},{"Ia:HistorialOperativoVerificado",verified.ToString()},{"Ia:ReferenciaTecnica","true"}}).Build();
 private static IaService Service(QaDatabase db,bool verified=false){var con=new BiConexion(Config(db));return new(con,new(con),Config(db,verified),new QaEnvironment());}
 [Fact(DisplayName="PI-IA-001 | Instalación idempotente y vacía no modifica comercio ni fabrica pronósticos")]
 public async Task Vacia(){await using var db=await QaDatabase.CreateAsync();await Migrar(db);await Migrar(db);Assert.Equal(8,await db.Scalar("SELECT COUNT(*) FROM Permiso WHERE Nombre LIKE 'IA[_]%'"));var s=Service(db);foreach(var view in new[]{"resumen","pronostico","proyeccion","inventario","compras","seguimiento"}){var r=JsonSerializer.SerializeToElement(await s.Consultar(view,"operativo",null,default),IaMotor.Json);Assert.Contains("verifique",r.GetProperty("mensaje").GetString());}Assert.Equal(0,await db.Scalar("SELECT COUNT(*) FROM ia.Pronostico"));Assert.Equal(0,await db.Scalar("SELECT COUNT(*) FROM Venta"));Assert.Equal(0,await db.Scalar("SELECT COUNT(*) FROM Compra"));Assert.Equal(0,await db.Scalar("SELECT SUM(StockActual) FROM Inventario"));Assert.Equal(0,await db.Scalar("SELECT COUNT(*) FROM bi.OperacionReferencia"));}
 [Fact(DisplayName="PS-IA-001 | JWT, permisos granulares, origen de referencia y autorización de borradores")]
 public async Task Seguridad(){await using var db=await QaDatabase.CreateAsync();await Migrar(db);await using var api=await ApiHost.Start(db,new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"Ia:HistorialOperativoVerificado","false"},{"Ia:Habilitada","false"}}).Build());Assert.Equal(HttpStatusCode.Unauthorized,(await api.Http.GetAsync("/api/ia/resumen")).StatusCode);await ApiTests.Login(api,"compras@example.test");Assert.Equal(HttpStatusCode.Forbidden,(await api.Http.GetAsync("/api/ia/resumen")).StatusCode);await db.Exec("INSERT RolPermiso(IdRol,IdPermiso) SELECT 2,IdPermiso FROM Permiso WHERE Nombre='IA_VER'");Assert.Equal(HttpStatusCode.OK,(await api.Http.GetAsync("/api/ia/resumen")).StatusCode);foreach(var path in new[]{"pronostico","proyeccion","inventario","compras","seguimiento","avisos"})Assert.Equal(HttpStatusCode.Forbidden,(await api.Http.GetAsync("/api/ia/"+path)).StatusCode);await db.Exec("INSERT RolPermiso(IdRol,IdPermiso) SELECT 2,IdPermiso FROM Permiso WHERE Nombre='IA_PRONOSTICOS_VER'");Assert.Equal(HttpStatusCode.OK,(await api.Http.GetAsync("/api/ia/pronostico")).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await api.Http.GetAsync("/api/ia/pronostico?procedencia=referencia")).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await api.Http.GetAsync("/api/ia/solicitudes/1")).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await api.Http.PostAsJsonAsync("/api/ia/entrenar",new{})).StatusCode);await ApiTests.Login(api);Assert.Equal(HttpStatusCode.OK,(await api.Http.GetAsync("/api/ia/seguimiento")).StatusCode);Assert.True(await db.Scalar("SELECT COUNT(*) FROM AuditoriaLog WHERE Descripcion LIKE 'Ia.%'")>0);}
 [Fact(DisplayName="PI-IA-002 | Avisos deduplicados, borrador sin stock y vínculo a compra existente")]
 public async Task Solicitud(){await using var db=await QaDatabase.CreateAsync();await Migrar(db);var s=Service(db,true);var today=IaReglas.Hoy;var p=new IaPolitica(1,1,3,7,2,1,1,0,0,null,"Verificación exclusivamente QA",today.AddDays(20));await s.GuardarPolitica(p,1,default);await s.ActualizarAvisos("operativo",default);var before=await db.Scalar("SELECT COUNT(*) FROM ia.Recomendacion");await s.ActualizarAvisos("operativo",default);Assert.Equal(before,await db.Scalar("SELECT COUNT(*) FROM ia.Recomendacion"));
  // Snapshot controlado exclusivamente en base QA; prueba transacciones, no precisión.
  await db.Exec($"INSERT ia.Ejecucion(Procedencia,Corte,Huella,Estado,Finalizada) VALUES('operativo','{today.AddDays(-1):yyyy-MM-dd}','{new string('a',64)}','Completada',SYSUTCDATETIME()); INSERT ia.Recomendacion(Procedencia,IdProducto,IdEjecucion,Prioridad,Cantidad,Mensaje,Firma,Estado) VALUES('operativo',1,1,'Urgente',4,N'Escenario de integración QA','{new string('b',64)}','Aceptada');");var id=(long)await db.Scalar("SELECT MAX(Id) FROM ia.Recomendacion");var purchases=new IaCompras(new BiConexion(Config(db,true)),Config(db,true));var request=await purchases.Crear(id,new(4,1,"Cantidad validada en QA"),1,default);Assert.Equal(request,await purchases.Crear(id,new(4,1,"Reintento QA"),1,default));Assert.Equal(0,await db.Scalar("SELECT COUNT(*) FROM Compra"));Assert.Equal(0,await db.Scalar("SELECT SUM(StockActual) FROM Inventario"));Assert.Equal(0,await db.Scalar("SELECT COUNT(*) FROM MovimientoStock"));await Assert.ThrowsAsync<ArgumentException>(()=>purchases.Vincular(request,999,1,default));await db.Exec("INSERT Compra(IdProveedor,IdUsuarioRegistro,IdEstadoCompra,FechaCompra,Subtotal,Igv,Total) VALUES(1,1,1,GETDATE(),100,18,118); INSERT DetalleCompra(IdCompra,IdProducto,Cantidad,PrecioCompra,Subtotal) VALUES(1,1,4,29.5,118);");await purchases.Vincular(request,1,1,default);await purchases.Vincular(request,1,1,default);Assert.Equal(1,await db.Scalar("SELECT IdCompra FROM ia.SolicitudCompra"));await s.Revisar(id,new("Atendida","Recepción comprobada en QA"),1,"operativo",default);Assert.Equal(1,await db.Scalar("SELECT COUNT(*) FROM ia.Recomendacion WHERE Estado='Atendida'"));}
 [Fact(DisplayName="PI-IA-003 | Historial sin anulaciones, sin futuros, stock operativo separado de referencia")]
 public async Task Datos(){await using var db=await QaDatabase.CreateAsync();await Migrar(db);await db.Exec("UPDATE ElementoCatalogo SET FechaRegistro='20260101'; INSERT Venta(IdCliente,IdUsuarioRegistro,IdEstadoVenta,FechaVenta,Subtotal,Igv,Total) VALUES(1,1,1,'2026-09-01',100,18,118),(1,1,2,'2026-09-02',100,18,118),(1,1,1,'2026-11-01',100,18,118);INSERT DetalleVenta(IdVenta,IdElementoCatalogo,Cantidad,PrecioUnitario,Subtotal)VALUES(1,1,2,59,118),(2,1,9,13.111,118),(3,1,8,14.75,118);UPDATE Inventario SET StockActual=7 WHERE IdProducto=1;");var data=new IaDatos(new BiConexion(Config(db)));var input=await data.Extraer("operativo",new(2026,10,13),default);Assert.Equal(2,input.Products.Sum(p=>p.Days.Sum(d=>d.Quantity)));Assert.All(input.Products.SelectMany(p=>p.Days),d=>Assert.True(DateOnly.Parse(d.Date)<=new DateOnly(2026,10,13)));Assert.Equal(7,(await data.Contexto("operativo",default)).Single(p=>p.IdProducto==1).Stock);Assert.Null((await data.Contexto("referencia",default)).Single(p=>p.IdProducto==1).Stock);}
 [Fact(DisplayName="PI-IA-VER-001 | El resumen distingue verificación del historial y ausencia de pronósticos")]
 public async Task VerificacionSinPronosticos()
 {
  await using var db=await QaDatabase.CreateAsync();await Migrar(db);
  foreach(var verified in new[]{false,true})
  {
   var reply=JsonSerializer.SerializeToElement(await Service(db,verified).Consultar("resumen","operativo",null,default),IaMotor.Json);
   Assert.Equal(verified,reply.GetProperty("historialOperativoVerificado").GetBoolean());
   Assert.Equal(0,reply.GetProperty("conEstimacion").GetInt32());
   if(verified)Assert.Contains("Aún no hay estimaciones",reply.GetProperty("mensaje").GetString());
   else Assert.Contains("verifique",reply.GetProperty("mensaje").GetString());
  }
  Assert.Equal(0,await db.Scalar("SELECT COUNT(*) FROM ia.Ejecucion"));
  Assert.Equal(0,await db.Scalar("SELECT COUNT(*) FROM Venta"));
 }
 private sealed class QaEnvironment:IWebHostEnvironment{public string EnvironmentName{get;set;}="Development";public string ApplicationName{get;set;}="Sistema3S";public string ContentRootPath{get;set;}=AppContext.BaseDirectory;public IFileProvider ContentRootFileProvider{get;set;}=new NullFileProvider();public string WebRootPath{get;set;}=Path.Combine(AppContext.BaseDirectory,"wwwroot");public IFileProvider WebRootFileProvider{get;set;}=new NullFileProvider();}
}
