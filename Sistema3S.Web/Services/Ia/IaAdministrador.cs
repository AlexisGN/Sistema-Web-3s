using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Data.SqlClient;
using Sistema3S.Web.Services.Bi;
namespace Sistema3S.Web.Services.Ia;

public sealed class IaAdministrador(BiConexion conexion,IaDatosAdministrados datos,IaMotorAdministrado motor,IaService consultas,IConfiguration config,IWebHostEnvironment env,ILogger<IaAdministrador> logger)
{
 static bool runtimeComprobado;
 static string Hash(string text)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
 static async Task<object?> Scalar(SqlConnection c,string sql,CancellationToken ct,params (string,object?)[] values){await using var cmd=c.CreateCommand();cmd.CommandTimeout=90;cmd.CommandText=sql;foreach(var (k,v) in values)cmd.Parameters.AddWithValue(k,v??DBNull.Value);return await cmd.ExecuteScalarAsync(ct);}
 public async Task Preparar(CancellationToken ct)
 {
  await using var c=await conexion.AbrirAsync(ct);
  var path=Path.Combine(env.ContentRootPath,"Database","20261016_IA_MotorAdministrado.sql");
  if(Convert.ToInt32(await Scalar(c,"SELECT CASE WHEN OBJECT_ID('ia.Programacion') IS NULL THEN 0 ELSE 1 END",ct))==0){if(!config.GetValue("Ia:Automatizacion:InstalarMetadatos",true)||!File.Exists(path))throw new InvalidOperationException("IA_MIGRACION_PENDIENTE");await Scalar(c,await File.ReadAllTextAsync(path,ct),ct);}
 }
 public async Task<string> Ciclo(string tipo,bool forzado,CancellationToken ct)
 {
  if(tipo is not ("Inferencia" or "Entrenamiento"))throw new ArgumentException("IA_TAREA_INVALIDA");
  if(!config.GetValue("Ia:Habilitada",false)||!config.GetValue("Ia:HistorialOperativoVerificado",false))return "Deshabilitada";
  await Preparar(ct);await using var c=await conexion.AbrirAsync(ct);
  var gate=Convert.ToInt32(await Scalar(c,"DECLARE @r int;EXEC @r=sys.sp_getapplock @Resource='Sistema3S.IA.administrado',@LockMode='Exclusive',@LockOwner='Session',@LockTimeout=0;SELECT @r;",ct));if(gate<0)return "Ocupada";
  long job=0;var cutoff=IaReglas.Hoy.AddDays(-1);string? fingerprint=null;
  try{
   // Poseer el bloqueo demuestra que no existe otro proceso Python administrado activo.
   await Scalar(c,"UPDATE ia.Trabajo SET Estado='Interrumpida',Finalizada=SYSUTCDATETIME(),Error='IA_INTERRUPCION_RECUPERADA' WHERE Estado='EnCurso'; UPDATE ia.Programacion SET Estado='Pendiente',ProximaUTC=SYSUTCDATETIME(),Error='IA_INTERRUPCION_RECUPERADA' WHERE Estado='EnCurso';",ct);
   await using var read=c.CreateCommand();read.CommandText="SELECT Tipo,ProximaUTC,UltimoCorte,UltimaHuella FROM ia.Programacion WHERE Tipo=@t";read.Parameters.AddWithValue("@t",tipo);var schedule=(await BiConexion.LeerAsync(read,["p"],ct))["p"].Single();
   if(!runtimeComprobado){await motor.Ejecutar("health",null,null,ct);var artifact=await Scalar(c,"SELECT Artefacto FROM ia.Modelo WHERE Activo=1",ct) as string;if(artifact!=null)await motor.Ejecutar("load",null,artifact,ct);runtimeComprobado=true;}
   if(!forzado&&(DateTime)schedule["ProximaUTC"]!>DateTime.UtcNow)return "NoVencida";
   if(tipo=="Entrenamiento"&&!forzado&&schedule["UltimoCorte"] is DateTime previous&&cutoff.DayNumber-DateOnly.FromDateTime(previous).DayNumber<Math.Clamp(config.GetValue("Ia:Automatizacion:MinimoDiasNuevos",7),1,90))return "SinInformacionNueva";
   job=Convert.ToInt64(await Scalar(c,"INSERT ia.Trabajo(Tipo,Corte,Estado) VALUES(@t,@d,'EnCurso');SELECT CAST(SCOPE_IDENTITY() AS bigint);UPDATE ia.Programacion SET Estado='EnCurso',IniciadaUTC=SYSUTCDATETIME(),Intentos=Intentos+1 WHERE Tipo=@t;",ct,("@t",tipo),("@d",cutoff.ToDateTime(TimeOnly.MinValue))));
   var dataset=await datos.Extraer(cutoff,ct);var serialized=JsonSerializer.Serialize(dataset.Datos,IaMotor.Json);var code=string.Concat(Directory.GetFiles(Path.GetDirectoryName(motor.Script)!,"*.py").Order().Select(File.ReadAllText))+await File.ReadAllTextAsync(IaRutas.Motor(env),ct);fingerprint=Hash(serialized+code+config.GetValue("Ia:Automatizacion:Epocas",30));
   string state;Guid? modelId=null;JsonObject detail=new(){["datos"]=dataset.Auditoria.DeepClone()};
   await using var modelRead=c.CreateCommand();modelRead.CommandText="SELECT Id,Artefacto,Huella,Evaluacion FROM ia.Modelo WHERE Activo=1";var active=(await BiConexion.LeerAsync(modelRead,["m"],ct))["m"].SingleOrDefault();
   if(tipo=="Entrenamiento"){
    if(schedule["UltimaHuella"] as string==fingerprint){state="SinCambios";}
    else{
     await motor.Ejecutar("health",null,null,ct);modelId=Guid.NewGuid();var artifact=modelId.Value.ToString("N");
     var training=JsonSerializer.SerializeToNode(dataset.Datos,IaMotor.Json)!.AsObject();if(active!=null)training["incumbent"]=JsonNode.Parse((string)active["Evaluacion"]!);
     var m=await motor.Ejecutar("train",new{data=training,scope="production",epochs=Math.Clamp(config.GetValue("Ia:Automatizacion:Epocas",30),1,60)},artifact,ct);
     var raw=await File.ReadAllTextAsync(Path.Combine(motor.Carpeta(artifact),"manifest.json"),ct);var valid=IaPublicacion.Aceptable(m);var incumbent=active==null?null:JsonNode.Parse((string)active["Evaluacion"]!)!.AsObject();
     var publish=valid&&IaPublicacion.Mejora(m,incumbent);if(publish){await motor.Ejecutar("load",null,artifact,ct);await motor.Ejecutar("infer",dataset.Datos,artifact,ct);}
     await Scalar(c,"SET XACT_ABORT ON;BEGIN TRANSACTION;IF @a=1 UPDATE ia.Modelo SET Activo=0 WHERE Activo=1;INSERT ia.Modelo(Id,Version,Alcance,Corte,Huella,Artefacto,Evaluacion,Elegible,Activo,Publicado) VALUES(@id,'3s-ia-5','production',@d,@h,@f,@m,@v,@a,CASE WHEN @a=1 THEN SYSUTCDATETIME() ELSE NULL END);COMMIT;",ct,("@a",publish),("@id",modelId),("@d",cutoff.ToDateTime(TimeOnly.MinValue)),("@h",Hash(raw)),("@f",artifact),("@m",m.ToJsonString()),("@v",valid));
     state=publish?"Publicado":valid?"ConservadoAnterior":"CandidatoRechazado";detail["aprobados"]=m["approved"]?.DeepClone();detail["publicado"]=publish;
    }
   }else if(active==null){state="SinModeloValidado";await consultas.ActualizarAvisos("operativo",ct);}
   else{
    modelId=(Guid)active["Id"]!;var artifact=(string)active["Artefacto"]!;var raw=await File.ReadAllTextAsync(Path.Combine(motor.Carpeta(artifact),"manifest.json"),ct);if(Hash(raw)!=(string)active["Huella"]!||!IaPublicacion.Aceptable(JsonNode.Parse(raw)!.AsObject()))throw new InvalidOperationException("IA_ARTEFACTO_ALTERADO");
    fingerprint=Hash(serialized+artifact);var exists=Convert.ToInt64(await Scalar(c,"SELECT COUNT(*) FROM ia.Ejecucion WHERE Huella=@h AND Corte=@d AND Procedencia='operativo' AND Estado='Completada'",ct,("@h",fingerprint),("@d",cutoff.ToDateTime(TimeOnly.MinValue))));
    if(exists>0){await motor.Ejecutar("load",null,artifact,ct);state="CargadoSinEntrenar";}
    else{var result=await motor.Ejecutar("infer",dataset.Datos,artifact,ct);await Persistir(c,result,fingerprint,cutoff,ct);state="PronosticosGuardados";}
    await IaProceso.Evaluar(c,"operativo",ct);await consultas.ActualizarAvisos("operativo",ct);
   }
   await Finalizar(c,job,tipo,state,cutoff,fingerprint,modelId,detail,null,ct);return state;
  }catch(OperationCanceledException) when(ct.IsCancellationRequested){if(job>0)await Finalizar(c,job,tipo,"Interrumpida",cutoff,null,null,null,"IA_CANCELADA",CancellationToken.None);throw;}
  catch(Exception ex){logger.LogWarning("IA {Tipo} aislada: {Clase}. Los módulos comerciales continúan.",tipo,ex.GetType().Name);if(job==0)job=Convert.ToInt64(await Scalar(c,"INSERT ia.Trabajo(Tipo,Corte,Estado) VALUES(@t,@d,'EnCurso');SELECT CAST(SCOPE_IDENTITY() AS bigint);",CancellationToken.None,("@t",tipo),("@d",cutoff.ToDateTime(TimeOnly.MinValue))));await Finalizar(c,job,tipo,"Fallida",cutoff,null,null,null,"IA_PROCESO_FALLIDO",CancellationToken.None);return "Fallida";}
  finally{await Scalar(c,"EXEC sys.sp_releaseapplock @Resource='Sistema3S.IA.administrado',@LockOwner='Session';",CancellationToken.None);}
 }
 async Task Finalizar(SqlConnection c,long job,string type,string state,DateOnly cutoff,string? hash,Guid? model,JsonObject? detail,string? error,CancellationToken ct)
 {
  var hours=error!=null?1:type=="Entrenamiento"?24*Math.Clamp(config.GetValue("Ia:Automatizacion:ReentrenamientoDias",7),1,90):Math.Clamp(config.GetValue("Ia:Automatizacion:InferenciaHoras",24),1,168);
  await Scalar(c,"UPDATE ia.Trabajo SET Estado=@s,Finalizada=SYSUTCDATETIME(),Modelo=@m,Detalle=@j,Error=@e WHERE Id=@id;UPDATE ia.Programacion SET Estado=@s,FinalizadaUTC=SYSUTCDATETIME(),ProximaUTC=DATEADD(hour,@hours,SYSUTCDATETIME()),Error=@e,UltimoCorte=CASE WHEN @e IS NULL THEN @d ELSE UltimoCorte END,UltimaHuella=CASE WHEN @h IS NULL THEN UltimaHuella ELSE @h END WHERE Tipo=@t;",ct,("@s",state),("@m",model),("@j",detail?.ToJsonString()),("@e",error),("@id",job),("@hours",hours),("@d",cutoff.ToDateTime(TimeOnly.MinValue)),("@h",hash),("@t",type));
 }
 static async Task Persistir(SqlConnection c,JsonObject result,string hash,DateOnly cutoff,CancellationToken ct)
 {
  await using var tx=(SqlTransaction)await c.BeginTransactionAsync(ct);
  async Task<object?> Q(string sql,params (string,object?)[] ps){await using var cmd=c.CreateCommand();cmd.Transaction=tx;cmd.CommandText=sql;foreach(var(k,v)in ps)cmd.Parameters.AddWithValue(k,v??DBNull.Value);return await cmd.ExecuteScalarAsync(ct);}
  var id=Convert.ToInt64(await Q("INSERT ia.Ejecucion(Procedencia,Corte,Huella,Estado,Resultado,Finalizada) VALUES('operativo',@d,@h,'Completada',@r,SYSUTCDATETIME());SELECT CAST(SCOPE_IDENTITY() AS bigint);",("@d",cutoff.ToDateTime(TimeOnly.MinValue)),("@h",hash),("@r",result.ToJsonString())));
  foreach(var p in result["products"]!.AsArray().Where(p=>p?["sufficient"]?.GetValue<bool>()==true))foreach(var h in new[]{7,30,60,90}){
   var first=DateOnly.Parse(p!["first"]!.GetValue<string>());var sql=h==7?"INSERT ia.Pronostico7(IdEjecucion,IdProducto,Desde,Hasta,Cantidad) VALUES(@id,@p,@a,@b,@q)":"INSERT ia.Pronostico(IdEjecucion,IdProducto,Desde,Hasta,Horizonte,Cantidad) VALUES(@id,@p,@a,@b,@h,@q)";
   await Q(sql,("@id",id),("@p",p["id"]!.GetValue<int>()),("@a",first.ToDateTime(TimeOnly.MinValue)),("@b",first.AddDays(h-1).ToDateTime(TimeOnly.MinValue)),("@h",h),("@q",p["totals"]![h.ToString()]!.GetValue<decimal>()));
  }
  await tx.CommitAsync(ct);
 }
}

public abstract class IaTareaWorker(IServiceScopeFactory scopes,IConfiguration config,ILogger logger,string tipo):BackgroundService
{
 protected override async Task ExecuteAsync(CancellationToken ct)
 {
  if(!config.GetValue("Ia:Habilitada",false))return;
  while(!ct.IsCancellationRequested){try{await using var scope=scopes.CreateAsyncScope();await scope.ServiceProvider.GetRequiredService<IaAdministrador>().Ciclo(tipo,false,ct);}catch(OperationCanceledException)when(ct.IsCancellationRequested){return;}catch(Exception ex){logger.LogWarning("IA {Tipo} no disponible ({Clase}); reintento diferido.",tipo,ex.GetType().Name);}try{await Task.Delay(TimeSpan.FromSeconds(Math.Clamp(config.GetValue("Ia:Automatizacion:RevisionSegundos",60),5,3600)),ct);}catch(OperationCanceledException){return;}}
 }
}
public sealed class IaInferenciaWorker(IServiceScopeFactory s,IConfiguration c,ILogger<IaInferenciaWorker> l):IaTareaWorker(s,c,l,"Inferencia");
public sealed class IaEntrenamientoWorker(IServiceScopeFactory s,IConfiguration c,ILogger<IaEntrenamientoWorker> l):IaTareaWorker(s,c,l,"Entrenamiento");
