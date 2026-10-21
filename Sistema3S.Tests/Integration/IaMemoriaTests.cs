using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Hosting;
using Sistema3S.Web.Services.Ia;
using Sistema3S.Web.Services.Bi;
using Sistema3S.Tests.Fixtures;
namespace Sistema3S.Tests.Integration;
public sealed class IaMemoriaTests
{
 static string Project=>Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../"));
 static async Task Preparar(QaDatabase db){await IaTests.Migrar(db);foreach(var f in new[]{"20261016_IA_MotorAdministrado.sql","20261019_IA_Adaptativa.sql","20261019_IA_Adaptativa.sql"})await db.Exec(await File.ReadAllTextAsync(Path.Combine(Project,"Sistema3S.Web/Database",f)));}
 [Fact(DisplayName="PI-IA-V7 | Memoria inmutable, evaluación completa e idempotente, denominador cero y backfill")]
 public async Task PrediccionesOriginales(){
  await using var db=await QaDatabase.CreateAsync();await Preparar(db);var today=IaReglas.Hoy;var model=Guid.NewGuid();var empty="{}";
  await db.Exec($"""
   INSERT ia.Modelo(Id,Version,Alcance,Corte,Huella,Artefacto,Evaluacion,Elegible,Activo) VALUES('{model}','3s-ia-7','production','{today.AddDays(-20):yyyyMMdd}','{new string('a',64)}','qa','{empty}',0,0);
   INSERT ia.Ejecucion(Procedencia,Corte,Huella,Estado,Resultado) VALUES('operativo','{today.AddDays(-20):yyyyMMdd}','{new string('b',64)}','Sombra','{empty}');
   INSERT ia.Ejecucion(Procedencia,Corte,Huella,Estado,Resultado) VALUES('operativo','{today.AddDays(-30):yyyyMMdd}','{new string('c',64)}','Sombra','{empty}');
   INSERT ia.PrediccionMemoria(IdEjecucion,Modelo,Version,IdProducto,Unidad,DiaEmision,Corte,Horizonte,Desde,Hasta,Cantidad,EsSombra) VALUES
    (1,'{model}','3s-ia-7',1,'UND','{today.AddDays(-10):yyyyMMdd}','{today.AddDays(-11):yyyyMMdd}',7,'{today.AddDays(-9):yyyyMMdd}','{today.AddDays(-3):yyyyMMdd}',6,1),
    (2,'{model}','3s-ia-7',1,'UND','{today.AddDays(-20):yyyyMMdd}','{today.AddDays(-21):yyyyMMdd}',7,'{today.AddDays(-19):yyyyMMdd}','{today.AddDays(-13):yyyyMMdd}',0,1),
    (1,'{model}','3s-ia-7',1,'UND','{today.AddDays(-2):yyyyMMdd}','{today.AddDays(-3):yyyyMMdd}',30,'{today.AddDays(-1):yyyyMMdd}','{today.AddDays(28):yyyyMMdd}',20,1);
   INSERT Venta(IdCliente,IdUsuarioRegistro,IdEstadoVenta,FechaVenta,Subtotal,Igv,Total) VALUES(1,1,1,'{today.AddDays(-6):yyyyMMdd}',100,18,118);
   INSERT DetalleVenta(IdVenta,IdElementoCatalogo,Cantidad,PrecioUnitario,Subtotal) VALUES(1,1,5,23.6,118);
   """);
  await using var c=new SqlConnection(db.Connection);await c.OpenAsync();await IaMemoria.Evaluar(c,today,default);await IaMemoria.Evaluar(c,today,default);
  Assert.Equal(2,await db.Scalar("SELECT COUNT(*) FROM ia.EvaluacionMemoria"));Assert.Equal(5,await db.Scalar("SELECT Real FROM ia.EvaluacionMemoria WHERE IdPrediccion=1"));Assert.Equal(1,await db.Scalar("SELECT ErrorAbsoluto FROM ia.EvaluacionMemoria WHERE IdPrediccion=1"));Assert.Equal(1,await db.Scalar("SELECT COUNT(*) FROM ia.EvaluacionMemoria WHERE IdPrediccion=2 AND Wape IS NULL AND Estado='SinDenominador'"));
  await Assert.ThrowsAsync<SqlException>(()=>db.Exec("UPDATE ia.PrediccionMemoria SET Cantidad=5 WHERE Id=1"));
  await Assert.ThrowsAsync<SqlException>(()=>db.Exec("DELETE ia.EvaluacionMemoria WHERE IdPrediccion=1"));
  await db.Exec("UPDATE DetalleVenta SET Cantidad=10 WHERE IdVenta=1");await IaMemoria.Evaluar(c,today,default);Assert.Equal(5,await db.Scalar("SELECT Real FROM ia.EvaluacionMemoria WHERE IdPrediccion=1"));Assert.Equal(6,await db.Scalar("SELECT Cantidad FROM ia.PrediccionMemoria WHERE Id=1"));
 }
 [Fact(DisplayName="PI-IA-V7 | Bloqueo SQL evita segundo administrador y no crea trabajos duplicados")]
 public async Task Exclusividad(){
  await using var db=await QaDatabase.CreateAsync();await Preparar(db);await using var c=new SqlConnection(db.Connection);await c.OpenAsync();await using var gate=c.CreateCommand();gate.CommandText="EXEC sys.sp_getapplock @Resource='Sistema3S.IA.administrado',@LockMode='Exclusive',@LockOwner='Session',@LockTimeout=0";await gate.ExecuteNonQueryAsync();
  var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"ConnectionStrings:BD_3S",db.Connection},{"Ia:Habilitada","true"},{"Ia:HistorialOperativoVerificado","true"}}).Build();var env=new Entorno(Path.Combine(Project,"Sistema3S.Web"));var con=new BiConexion(config);var datos=new IaDatos(con);var admin=new IaAdministrador(con,new IaDatosAdministrados(con),new IaMotorAdministrado(config,env),new IaService(con,datos,config,env),config,env,NullLogger<IaAdministrador>.Instance);
  Assert.Equal("Ocupada",await admin.Ciclo("Entrenamiento",true,default));Assert.Equal(0,await db.Scalar("SELECT COUNT(*) FROM ia.Trabajo"));
 }
 [Fact(DisplayName="PI-IA-V7 | Deterioro prospectivo retira el modelo y persiste una revisión aunque no lleguen ventas nuevas")]
 public async Task DeterioroProgramaRevision(){
  await using var db=await QaDatabase.CreateAsync();await Preparar(db);var today=IaReglas.Hoy;var model=Guid.NewGuid();
  var folder=Path.Combine(Path.GetTempPath(),"Sistema3S-v7-QA-"+Guid.NewGuid().ToString("N"));var artifact=Path.Combine(folder,"administrado",model.ToString("N"));Directory.CreateDirectory(artifact);
  // Artefacto mínimo de estado QA: no se infiere ni se presenta como un modelo entrenado.
  static string Hash(string text)=>Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
  var manifest=new System.Text.Json.Nodes.JsonObject{["version"]="3s-ia-6",["publishable"]=true,["modelFile"]="model.json",["weightsHash"]=Hash("{}")}.ToJsonString();
  await File.WriteAllTextAsync(Path.Combine(artifact,"model.json"),"{}");await File.WriteAllTextAsync(Path.Combine(artifact,"manifest.json"),manifest);
  await db.Exec($"""
   INSERT ia.Modelo(Id,Version,Alcance,Corte,Huella,Artefacto,Evaluacion,Elegible,Activo)
   VALUES('{model}','3s-ia-6','production','{today.AddDays(-50):yyyyMMdd}','{Hash(manifest)}','{model:N}',N'{manifest}',1,1);
   DECLARE @i int=0,@v int,@execution bigint,@first date;WHILE @i<3 BEGIN
    SET @first=DATEADD(day,-40+@i*10,'{today:yyyyMMdd}');
    INSERT ia.Ejecucion(Procedencia,Corte,Huella,Estado) VALUES('operativo',DATEADD(day,-2,@first),'{new string('a',64)}','Completada');SET @execution=SCOPE_IDENTITY();
    INSERT ia.PrediccionMemoria(IdEjecucion,Modelo,Version,IdProducto,Unidad,DiaEmision,Corte,Horizonte,Desde,Hasta,Cantidad,EsSombra)
    VALUES(@execution,'{model}','3s-ia-6',1,'UND',DATEADD(day,-1,@first),DATEADD(day,-2,@first),7,@first,DATEADD(day,6,@first),9,0);
    INSERT Venta(IdCliente,IdUsuarioRegistro,IdEstadoVenta,FechaVenta,Subtotal,Igv,Total) VALUES(1,1,1,@first,100,18,118);SET @v=SCOPE_IDENTITY();
    INSERT DetalleVenta(IdVenta,IdElementoCatalogo,Cantidad,PrecioUnitario,Subtotal) VALUES(@v,1,1,118,118);SET @i+=1;END;
   UPDATE ia.Programacion SET HuellaEntrenada='{new string('a',64)}',UltimoCorte='{today.AddDays(-1):yyyyMMdd}',ProximaUTC=DATEADD(day,7,SYSUTCDATETIME()) WHERE Tipo='Entrenamiento';
   """);
  var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"ConnectionStrings:BD_3S",db.Connection},{"Ia:Habilitada","true"},{"Ia:HistorialOperativoVerificado","true"},{"Ia:Python",Environment.GetEnvironmentVariable("S3S_QA_PYTHON")},{"Ia:DatosPrivados",folder}}).Build();
  var env=new Entorno(Path.Combine(Project,"Sistema3S.Web"));var con=new BiConexion(config);var admin=new IaAdministrador(con,new IaDatosAdministrados(con),new IaMotorAdministrado(config,env),new IaService(con,new IaDatos(con),config,env),config,env,NullLogger<IaAdministrador>.Instance);
  Assert.Equal("SinModeloValidado",await admin.Ciclo("Inferencia",true,default));
  Assert.Equal(3,await db.Scalar("SELECT COUNT(*) FROM ia.EvaluacionMemoria"));Assert.Equal(1,await db.Scalar("SELECT COUNT(*) FROM ia.Modelo WHERE Activo=0 AND EstadoModelo='Obsoleto/degradado'"));
  Assert.Equal(1,await db.Scalar("SELECT COUNT(*) FROM ia.Programacion WHERE Tipo='Entrenamiento' AND HuellaEntrenada IS NULL AND UltimoCorte IS NULL AND ProximaUTC<=SYSUTCDATETIME()"));
  Assert.Equal(3,await db.Scalar("SELECT COUNT(*) FROM Venta"));Assert.Equal(0,await db.Scalar("SELECT COUNT(*) FROM ia.SolicitudCompra"));
 }
 [Fact(DisplayName="PI-IA-V7 | Nuevas ventas generan candidato, sin cambios no entrena y sombra nunca alimenta compras")]
 public async Task AprendizajeRealAdministrado(){
  await using var db=await QaDatabase.CreateAsync();await Preparar(db);var today=IaReglas.Hoy;var python=Environment.GetEnvironmentVariable("S3S_QA_PYTHON")??throw new InvalidOperationException("Falta Python QA");
  await db.Exec($"""
   UPDATE UnidadMedida SET Abreviatura=N'UND' WHERE IdUnidadMedida=1;
   DECLARE @i int=0,@v int;WHILE @i<190 BEGIN
    INSERT Venta(IdCliente,IdUsuarioRegistro,IdEstadoVenta,FechaVenta,Subtotal,Igv,Total) VALUES(1,1,1,DATEADD(day,@i-190,'{today:yyyyMMdd}'),100,18,118);SET @v=SCOPE_IDENTITY();
    INSERT DetalleVenta(IdVenta,IdElementoCatalogo,Cantidad,PrecioUnitario,Subtotal) VALUES(@v,1,3,39.333,118);SET @i+=1;END;
   """);
  var folder=Path.Combine(Path.GetTempPath(),"Sistema3S-v7-QA-"+Guid.NewGuid().ToString("N"));
  var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"ConnectionStrings:BD_3S",db.Connection},{"Ia:Habilitada","true"},{"Ia:HistorialOperativoVerificado","true"},{"Ia:Python",python},{"Ia:DatosPrivados",folder}}).Build();var env=new Entorno(Path.Combine(Project,"Sistema3S.Web"));var con=new BiConexion(config);var datos=new IaDatos(con);var admin=new IaAdministrador(con,new IaDatosAdministrados(con),new IaMotorAdministrado(config,env),new IaService(con,datos,config,env),config,env,NullLogger<IaAdministrador>.Instance);
  Assert.Equal("CandidatoRechazado",await admin.Ciclo("Entrenamiento",true,default));Assert.Equal(1,await db.Scalar("SELECT COUNT(*) FROM ia.Modelo"));
  Assert.Equal("SinCambios",await admin.Ciclo("Entrenamiento",true,default));Assert.Equal(1,await db.Scalar("SELECT COUNT(*) FROM ia.Modelo"));
  await db.Exec($"""
   DECLARE @i int=0,@v int;WHILE @i<7 BEGIN
    INSERT Venta(IdCliente,IdUsuarioRegistro,IdEstadoVenta,FechaVenta,Subtotal,Igv,Total) VALUES(1,1,1,'{today.AddDays(-1):yyyyMMdd}',100,18,118);SET @v=SCOPE_IDENTITY();
    INSERT DetalleVenta(IdVenta,IdElementoCatalogo,Cantidad,PrecioUnitario,Subtotal) VALUES(@v,1,2,59,118);SET @i+=1;END;
   UPDATE ia.Programacion SET UltimoEntrenamientoUTC=DATEADD(day,-8,SYSUTCDATETIME()) WHERE Tipo='Entrenamiento';
   """);
  Assert.Equal("CandidatoRechazado",await admin.Ciclo("Entrenamiento",true,default));Assert.Equal(2,await db.Scalar("SELECT COUNT(*) FROM ia.Modelo"));Assert.Equal(0,await db.Scalar("SELECT COUNT(*) FROM ia.Modelo WHERE Activo=1"));
  Assert.Equal("SinModeloValidado",await admin.Ciclo("Inferencia",true,default));Assert.Equal("SinModeloValidado",await admin.Ciclo("Inferencia",true,default));
  Assert.Equal(1,await db.Scalar("SELECT COUNT(*) FROM ia.Ejecucion WHERE Estado='Sombra'"));Assert.True(await db.Scalar("SELECT COUNT(*) FROM ia.PrediccionMemoria WHERE EsSombra=1")>=4);Assert.Equal(0,await db.Scalar("SELECT COUNT(*) FROM ia.Pronostico"));Assert.Equal(197,await db.Scalar("SELECT COUNT(*) FROM Venta"));
  // Una BD restaurada puede conservar la agenda sin los pesos privados del equipo anterior.
  foreach(var file in Directory.GetFiles(Path.Combine(folder,"administrado"),"model.json",SearchOption.AllDirectories))File.Delete(file);
  Assert.Equal("SinModeloValidado",await admin.Ciclo("Inferencia",true,default));Assert.Equal(1,await db.Scalar("SELECT COUNT(*) FROM ia.Programacion WHERE Tipo='Entrenamiento' AND HuellaEntrenada IS NULL AND UltimoCorte IS NULL"));
  Assert.Equal("CandidatoRechazado",await admin.Ciclo("Entrenamiento",true,default));Assert.Equal(3,await db.Scalar("SELECT COUNT(*) FROM ia.Modelo"));Assert.Equal(197,await db.Scalar("SELECT COUNT(*) FROM Venta"));
 }
 sealed class Entorno(string root):IWebHostEnvironment {public string EnvironmentName{get;set;}="Testing";public string ApplicationName{get;set;}="Sistema3S";public string ContentRootPath{get;set;}=root;public string WebRootPath{get;set;}=Path.Combine(root,"wwwroot");public IFileProvider ContentRootFileProvider{get;set;}=new NullFileProvider();public IFileProvider WebRootFileProvider{get;set;}=new NullFileProvider();}
}
