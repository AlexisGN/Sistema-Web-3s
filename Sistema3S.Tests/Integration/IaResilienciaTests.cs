using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Hosting;
using Sistema3S.Tests.Fixtures;
using Sistema3S.Web.Services.Bi;
using Sistema3S.Web.Services.Ia;
namespace Sistema3S.Tests.Integration;

public sealed class IaResilienciaTests
{
 [Fact(DisplayName="PI-IA-004 | Instalación cliente vacía muestra estados reales sin referencias ni modelos")]
 public async Task ClienteVacio(){
  await using var db=await QaDatabase.CreateAsync(false);
  var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../"));
  await db.Exec(await File.ReadAllTextAsync(Path.Combine(root,"tools/instalacion/02-catalogos.sql")));await IaTests.Migrar(db);
  await using(var c=new Microsoft.Data.SqlClient.SqlConnection(db.Connection)){await c.OpenAsync();await using var cmd=c.CreateCommand();cmd.CommandText="INSERT Usuario(IdRol,Correo,ContrasenaHash) SELECT IdRol,N'admin@example.test',@h FROM Rol WHERE Nombre=N'Administrador'";cmd.Parameters.AddWithValue("@h",new Sistema3S.Web.Services.Seguridad.PasswordHashService().CrearHash(QaDatabase.Password));await cmd.ExecuteNonQueryAsync();}
  await using var api=await ApiHost.Start(db);await ApiTests.Login(api);
  foreach(var path in new[]{"resumen","pronostico","proyeccion","inventario","compras","seguimiento"})Assert.Equal(HttpStatusCode.OK,(await api.Http.GetAsync("/api/ia/"+path)).StatusCode);
  foreach(var table in new[]{"Venta","Compra","Cotizacion","Cliente","Proveedor","ElementoCatalogo","Inventario","MovimientoStock","bi.OperacionReferencia","ia.Pronostico","ia.SolicitudCompra"})Assert.Equal(0,await db.Scalar("SELECT COUNT(*) FROM "+table));
 }
 [Fact(DisplayName="PI-IA-005 | Fallo de motor recuperable libera el bloqueo sin afectar ventas ni BI")]
 public async Task Recuperacion(){
  await using var db=await QaDatabase.CreateAsync();await IaTests.Migrar(db);
  var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"ConnectionStrings:BD_3S",db.Connection},{"Ia:HistorialOperativoVerificado","true"}}).Build();
  var con=new BiConexion(config);var datos=new IaDatos(con);var motor=new IaMotor(config,new PrivateEnv(),NullLogger<IaMotor>.Instance);var process=new IaProceso(con,datos,motor,config);
  for(int i=0;i<2;i++)await Assert.ThrowsAsync<InvalidOperationException>(()=>process.Ejecutar("operativo",default));
  Assert.Equal(1,await db.Scalar("SELECT COUNT(*) FROM ia.Ejecucion"));Assert.Equal(1,await db.Scalar("SELECT COUNT(*) FROM ia.Ejecucion WHERE Estado='Fallida'"));Assert.Equal(0,await db.Scalar("SELECT COUNT(*) FROM ia.Pronostico"));
  await using var api=await ApiHost.Start(db);await ApiTests.Login(api);Assert.Equal(HttpStatusCode.OK,(await api.Http.GetAsync("/api/bi/resumen?desde=2026-04-01&hasta=2026-10-14")).StatusCode);
  Assert.Equal(HttpStatusCode.Created,(await api.Http.PostAsJsonAsync("/api/cotizacion",new{idCliente=1,origenCotizacion="Manual",detalles=new[]{new{idElementoCatalogo=1,cantidad=1,precioUnitario=118}}})).StatusCode);
 }
 [Fact(DisplayName="PI-IA-006 | Pronóstico original inmutable y evaluación posterior reconciliada sin anticipar ventanas")]
 public async Task Evaluacion(){
  await using var db=await QaDatabase.CreateAsync();await IaTests.Migrar(db);var today=IaReglas.Hoy;var start=today.AddDays(-40);var end=start.AddDays(29);
  await db.Exec($"INSERT ia.Ejecucion(Procedencia,Corte,Huella,Estado,Finalizada) VALUES('operativo','{start.AddDays(-2):yyyy-MM-dd}','{new string('c',64)}','Completada','{start.AddDays(-1):yyyy-MM-dd}T12:00:00'),('operativo','{today.AddDays(-1):yyyy-MM-dd}','{new string('d',64)}','Completada',SYSUTCDATETIME()); INSERT ia.Pronostico(IdEjecucion,IdProducto,Desde,Hasta,Horizonte,Cantidad) VALUES(1,1,'{start:yyyy-MM-dd}','{end:yyyy-MM-dd}',30,8),(2,1,'{today.AddDays(1):yyyy-MM-dd}','{today.AddDays(30):yyyy-MM-dd}',30,10); INSERT Venta(IdCliente,IdUsuarioRegistro,IdEstadoVenta,FechaVenta,Subtotal,Igv,Total) VALUES(1,1,1,'{start.AddDays(4):yyyy-MM-dd}',100,18,118);INSERT DetalleVenta(IdVenta,IdElementoCatalogo,Cantidad,PrecioUnitario,Subtotal)VALUES(1,1,4,29.5,118);");
  await using var c=new Microsoft.Data.SqlClient.SqlConnection(db.Connection);await c.OpenAsync();await IaProceso.Evaluar(c,"operativo",default);Assert.Equal(4,await db.Scalar("SELECT Real FROM ia.Pronostico WHERE Id=1"));Assert.Equal(8,await db.Scalar("SELECT Cantidad FROM ia.Pronostico WHERE Id=1"));Assert.Equal(1,await db.Scalar("SELECT COUNT(*) FROM ia.Pronostico WHERE Id=2 AND Real IS NULL AND Evaluada IS NULL"));
  await db.Exec("EXEC sys.sp_set_session_context @key=N'IdUsuarioAuditoria',@value=1; UPDATE Venta SET IdEstadoVenta=2 WHERE IdVenta=1");await IaProceso.Evaluar(c,"operativo",default);Assert.Equal(0,await db.Scalar("SELECT Real FROM ia.Pronostico WHERE Id=1"));Assert.Equal(8,await db.Scalar("SELECT Cantidad FROM ia.Pronostico WHERE Id=1"));
 }
 [Fact(DisplayName="PI-IA-008 | Proyección conserva snapshots y no inventa costo o utilidad neta")]
 public async Task Proyeccion(){
  await using var db=await QaDatabase.CreateAsync();await IaTests.Migrar(db);var today=IaReglas.Hoy;
  var nextMonth=new DateOnly(today.Year,today.Month,1).AddMonths(1);var monthQuantity=DateTime.DaysInMonth(nextMonth.Year,nextMonth.Month);
  var snapshot=System.Text.Json.JsonSerializer.Serialize(new{products=new[]{new{id=1,first=today.AddDays(1).ToString("yyyy-MM-dd"),nextMonth=monthQuantity,intervals=new Dictionary<string,object>{{"30",new{lower=20,upper=40,windows=5}}},totals=new Dictionary<string,int>{{"30",30},{"60",60},{"90",90}},daily=Enumerable.Range(1,90).Select(i=>new{date=today.AddDays(i).ToString("yyyy-MM-dd"),quantity=1}),history=Array.Empty<object>()}}});
  await using var c=new Microsoft.Data.SqlClient.SqlConnection(db.Connection);await c.OpenAsync();await using(var cmd=c.CreateCommand()){cmd.CommandText="INSERT ia.Ejecucion(Procedencia,Corte,Huella,Estado,Finalizada,Resultado) VALUES('operativo',@d,@h,'Completada',SYSUTCDATETIME(),@r)";cmd.Parameters.AddWithValue("@d",today.AddDays(-1).ToDateTime(TimeOnly.MinValue));cmd.Parameters.AddWithValue("@h",new string('e',64));cmd.Parameters.AddWithValue("@r",snapshot);await cmd.ExecuteNonQueryAsync();}
  var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"ConnectionStrings:BD_3S",db.Connection},{"Ia:HistorialOperativoVerificado","true"}}).Build();var con=new BiConexion(config);var s=new IaService(con,new(con),config,new PrivateEnv());
  var initial=System.Text.Json.JsonSerializer.SerializeToElement(await s.Consultar("proyeccion","operativo",1,default),IaMotor.Json).GetProperty("productos")[0];
  Assert.Equal(System.Text.Json.JsonValueKind.Null,initial.GetProperty("horizontes")[0].GetProperty("margenBruto").ValueKind);
  Assert.Equal(snapshot,(string)(await new Microsoft.Data.SqlClient.SqlCommand("SELECT Resultado FROM ia.Ejecucion",c).ExecuteScalarAsync())!);Assert.Equal(0,await db.Scalar("SELECT COUNT(*) FROM Compra"));Assert.Equal(0,await db.Scalar("SELECT COUNT(*) FROM Venta"));Assert.Equal(0,await db.Scalar("SELECT SUM(StockActual) FROM Inventario"));
  await db.Exec($"INSERT Venta(IdCliente,IdUsuarioRegistro,IdEstadoVenta,FechaVenta,Subtotal,Igv,Total) VALUES(1,1,1,'{today.AddDays(-1):yyyy-MM-dd}',100,18,118);INSERT DetalleVenta(IdVenta,IdElementoCatalogo,Cantidad,PrecioUnitario,Subtotal)VALUES(1,1,2,59,118);INSERT Compra(IdProveedor,IdUsuarioRegistro,IdEstadoCompra,FechaCompra,Subtotal,Igv,Total)VALUES(1,1,1,'{today.AddDays(-1):yyyy-MM-dd}',100,18,118);INSERT DetalleCompra(IdCompra,IdProducto,Cantidad,PrecioCompra,Subtotal)VALUES(1,1,1,118,118);");
  var product=System.Text.Json.JsonSerializer.SerializeToElement(await s.Consultar("proyeccion","operativo",1,default),IaMotor.Json).GetProperty("productos")[0];var projection=product.GetProperty("horizontes")[0];
  Assert.Equal(-1500,projection.GetProperty("margenBruto").GetDecimal());Assert.Equal(-100,projection.GetProperty("porcentajeMargen").GetDecimal());Assert.Equal("Margen negativo estimado",projection.GetProperty("estado").GetString());Assert.Contains("Revisa el precio",projection.GetProperty("recomendacion").GetString());
  var calendar=product.GetProperty("mesSiguienteFinanciero");Assert.Equal(nextMonth.ToString("yyyy-MM"),calendar.GetProperty("mes").GetString());Assert.Equal(monthQuantity*50,calendar.GetProperty("ingresoNeto").GetDecimal());Assert.Equal(-monthQuantity*50,calendar.GetProperty("margenBruto").GetDecimal());
  Assert.Equal(20,product.GetProperty("intervalos").GetProperty("30").GetProperty("lower").GetDecimal());Assert.Equal(100,product.GetProperty("historicoMensual")[0].GetProperty("ingresoNeto").GetDecimal());Assert.Equal(4500,product.GetProperty("estimadoMensual").EnumerateArray().Sum(m=>m.GetProperty("ingresoNeto").GetDecimal()));
 }
 private sealed class PrivateEnv:IWebHostEnvironment{public string EnvironmentName{get;set;}="Testing";public string ApplicationName{get;set;}="Sistema3S";public string ContentRootPath{get;set;}=AppContext.BaseDirectory;public IFileProvider ContentRootFileProvider{get;set;}=new NullFileProvider();public string WebRootPath{get;set;}=Path.Combine(AppContext.BaseDirectory,"wwwroot");public IFileProvider WebRootFileProvider{get;set;}=new NullFileProvider();}
}
