using System.Text.Json;
using System.Text.Json.Nodes;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Sistema3S.Tests.Fixtures;
using Sistema3S.Web.Services.Bi;
using Sistema3S.Web.Services.Ia;
namespace Sistema3S.Tests.Integration;

public sealed class IaMotorV6Tests
{
 [Theory(DisplayName="PI-IA | Un modelo retirado no presta sus aprobaciones a consultas ni compras")]
 [InlineData("3s-ia-6")]
 [InlineData("3s-ia-7")]
 public async Task ModeloRetirado(string version){
  await using var db=await QaDatabase.CreateAsync();await IaTests.Migrar(db);
  var project=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../"));await db.Exec(await File.ReadAllTextAsync(Path.Combine(project,"Sistema3S.Web/Database/20261016_IA_MotorAdministrado.sql")));
  var cutoff=IaReglas.Hoy.AddDays(-1);var trained=cutoff.AddDays(-14);
  var result=JsonSerializer.Serialize(new{version,published=true,experimental=false,modelCutoff=trained.ToString("yyyy-MM-dd"),products=new[]{new{id=1,sufficient=true,accepted=true,first=IaReglas.Hoy.AddDays(1).ToString("yyyy-MM-dd"),totals=new Dictionary<string,int>{{"30",99}},days=100,events=4}}});
  var manifest=new JsonObject{["version"]=version,["publishable"]=true,["scope"]="production",["policies"]=new JsonObject()};
  await db.Exec($"""
   INSERT ia.Ejecucion(Procedencia,Corte,Huella,Estado,Resultado,Finalizada) VALUES('operativo','{cutoff:yyyyMMdd}','{new string('a',64)}','Completada',N'{result.Replace("'","''")}',SYSUTCDATETIME());
   INSERT ia.Modelo(Id,Version,Alcance,Corte,Huella,Artefacto,Evaluacion,Elegible,Activo)
   VALUES(NEWID(),'{version}','production','{trained:yyyyMMdd}','{new string('b',64)}','qa-validado',N'{manifest.ToJsonString().Replace("'","''")}',1,1);
   """);
  var cfg=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"ConnectionStrings:BD_3S",db.Connection},{"Ia:HistorialOperativoVerificado","true"},{"Ia:Habilitada","false"}}).Build();
  await using var api=await ApiHost.Start(db,cfg);await ApiTests.Login(api);
  var policy=new IaPolitica(1,1,3,7,2,1,1,0,0,null,"Condiciones sólo QA",IaReglas.Hoy.AddDays(20));
  var response=await api.Http.PutAsJsonAsync("/api/ia/abastecimiento",policy);
  // Usa el servicio público de condiciones; el host conserva sus permisos normales.
  if(!response.IsSuccessStatusCode)throw new InvalidOperationException(await response.Content.ReadAsStringAsync());
  // Guardar condiciones actualiza los avisos; el escenario controlado debe ser el más reciente.
  await db.Exec($"INSERT ia.Recomendacion(Procedencia,IdProducto,IdEjecucion,Prioridad,Cantidad,Mensaje,Firma,Estado) VALUES('operativo',1,1,'Urgente',4,N'Prueba aislada de retirada','{new string('c',64)}','Aceptada');");
  var recommendation=(long)await db.Scalar("SELECT MAX(Id) FROM ia.Recomendacion");
  var purchases=new IaCompras(new BiConexion(cfg),cfg);
  var request=await purchases.Crear(recommendation,new(4,1,"Prueba con modelo activo"),1,default);Assert.True(request>0);
  await db.Exec("UPDATE ia.Modelo SET Corte=DATEADD(day,1,Corte) WHERE Activo=1");
  var changed=await ApiTests.Json(await api.Http.GetAsync("/api/ia/pronostico"));Assert.False(changed.GetProperty("productos")[0].GetProperty("confiable").GetBoolean());
  await Assert.ThrowsAsync<ArgumentException>(()=>purchases.Crear(recommendation,new(4,1,"Emisión de otro modelo"),1,default));
  await db.Exec("UPDATE ia.Modelo SET Activo=0 WHERE Activo=1");
  var forecast=await ApiTests.Json(await api.Http.GetAsync("/api/ia/pronostico"));var p=forecast.GetProperty("productos")[0];
  Assert.False(p.GetProperty("suficiente").GetBoolean());Assert.False(p.GetProperty("confiable").GetBoolean());
  Assert.Equal(JsonValueKind.Null,p.GetProperty("cantidades").GetProperty("30").ValueKind);Assert.Contains("no corresponde a un modelo activo",p.GetProperty("motivo").GetString());
  await Assert.ThrowsAsync<ArgumentException>(()=>purchases.Crear(recommendation,new(4,1,"Reintento después de retirada"),1,default));
  Assert.Equal(1,await db.Scalar("SELECT COUNT(*) FROM ia.SolicitudCompra"));Assert.Equal(0,await db.Scalar("SELECT COUNT(*) FROM Compra"));
 }
 [Fact(DisplayName="PI-IA-V6 | Candidato rechazado explica el historial sin publicar cifras antiguas ni alterar operaciones")]
 public async Task DiagnosticoSinPublicacion(){
  await using var db=await QaDatabase.CreateAsync();await IaTests.Migrar(db);
  var project=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../"));await db.Exec(await File.ReadAllTextAsync(Path.Combine(project,"Sistema3S.Web/Database/20261016_IA_MotorAdministrado.sql")));
  var cutoff=IaReglas.Hoy.AddDays(-1);
  var previous=JsonSerializer.Serialize(new{version="3s-ia-4",products=new[]{new{id=1,sufficient=true,first=IaReglas.Hoy.AddDays(1).ToString("yyyy-MM-dd"),totals=new Dictionary<string,int>{{"30",999}},days=100,events=4}}});
  var manifest=new JsonObject{["version"]="3s-ia-6",["scope"]="production",["policies"]=new JsonObject()};
  await db.Exec($"""
   INSERT ia.Ejecucion(Procedencia,Corte,Huella,Estado,Resultado,Finalizada) VALUES('operativo','{cutoff:yyyyMMdd}','{new string('e',64)}','Completada',N'{previous.Replace("'","''")}',SYSUTCDATETIME());
   INSERT ia.Modelo(Id,Version,Alcance,Corte,Huella,Artefacto,Evaluacion,Elegible,Activo)
   VALUES(NEWID(),'3s-ia-6','production','{cutoff:yyyyMMdd}','{new string('f',64)}','qa-rechazado',N'{manifest.ToJsonString().Replace("'","''")}',0,0);
   INSERT Venta(IdCliente,IdUsuarioRegistro,IdEstadoVenta,FechaVenta,Subtotal,Igv,Total) VALUES(1,1,1,'{cutoff:yyyyMMdd}',100,18,118);
   INSERT DetalleVenta(IdVenta,IdElementoCatalogo,Cantidad,PrecioUnitario,Subtotal) VALUES(1,1,1,118,118);
   """);
  var cfg=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"Ia:HistorialOperativoVerificado","true"},{"Ia:Habilitada","false"}}).Build();
  await using var api=await ApiHost.Start(db,cfg);await ApiTests.Login(api);
  var forecast=await ApiTests.Json(await api.Http.GetAsync("/api/ia/pronostico"));var p=forecast.GetProperty("productos")[0];
  Assert.False(p.GetProperty("suficiente").GetBoolean());Assert.False(p.GetProperty("confiable").GetBoolean());
  Assert.Equal(JsonValueKind.Null,p.GetProperty("cantidades").GetProperty("30").ValueKind);
  Assert.Contains("independientes",p.GetProperty("motivo").GetString());Assert.Single(p.GetProperty("historico").EnumerateArray());
  Assert.Equal(0,await db.Scalar("SELECT COUNT(*) FROM ia.Modelo WHERE Activo=1"));Assert.Equal(0,await db.Scalar("SELECT COUNT(*) FROM ia.Pronostico"));
  Assert.Equal(1,await db.Scalar("SELECT COUNT(*) FROM Venta"));Assert.Equal(1,await db.Scalar("SELECT COUNT(*) FROM ia.Ejecucion"));
 }
}
