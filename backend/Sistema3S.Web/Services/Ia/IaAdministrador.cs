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
 static string? runtimeComprobado;
 static string Hash(string text)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
 bool Disponible(string artifact,string expected)
 {
  try{
   var folder=motor.Carpeta(artifact);var raw=File.ReadAllText(Path.Combine(folder,"manifest.json"));if(Hash(raw)!=expected)return false;
   var m=JsonNode.Parse(raw)!.AsObject();if(m["version"]?.GetValue<string>() is not ("3s-ia-6" or "3s-ia-7" or "3s-ia-8"))return false;
   bool FileOk(string name,string sha)=>Path.GetFileName(name)==name&&File.Exists(Path.Combine(folder,name))&&Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(folder,name)))).Equals(sha,StringComparison.OrdinalIgnoreCase);
   if(!FileOk(m["modelFile"]!.GetValue<string>(),m["weightsHash"]!.GetValue<string>()))return false;
   if(m["neuralFiles"] is JsonObject files)foreach(var entry in files)if(!FileOk(entry.Value!["file"]!.GetValue<string>(),entry.Value["sha256"]!.GetValue<string>()))return false;
   return true;
  }catch{return false;}
 }
 static async Task<object?> Scalar(SqlConnection c,string sql,CancellationToken ct,params (string,object?)[] values){await using var cmd=c.CreateCommand();cmd.CommandTimeout=90;cmd.CommandText=sql;foreach(var (k,v) in values)cmd.Parameters.AddWithValue(k,v??DBNull.Value);return await cmd.ExecuteScalarAsync(ct);}
 public async Task Preparar(CancellationToken ct)
 {
  await using var c=await conexion.AbrirAsync(ct);
  var path=Path.Combine(env.ContentRootPath,"Database","20261016_IA_MotorAdministrado.sql");
  if(Convert.ToInt32(await Scalar(c,"SELECT CASE WHEN OBJECT_ID('ia.Programacion') IS NULL THEN 0 ELSE 1 END",ct))==0){if(!config.GetValue("Ia:Automatizacion:InstalarMetadatos",true)||!File.Exists(path))throw new InvalidOperationException("IA_MIGRACION_PENDIENTE");await Scalar(c,await File.ReadAllTextAsync(path,ct),ct);}
  var v7=Path.Combine(env.ContentRootPath,"Database","20261019_IA_Adaptativa.sql");
  if(Convert.ToInt32(await Scalar(c,"SELECT CASE WHEN OBJECT_ID('ia.PrediccionMemoria') IS NULL OR COL_LENGTH('ia.Programacion','HuellaRegimen') IS NULL THEN 0 ELSE 1 END",ct))==0){if(!config.GetValue("Ia:Automatizacion:InstalarMetadatos",true)||!File.Exists(v7))throw new InvalidOperationException("IA_MIGRACION_V7_PENDIENTE");await Scalar(c,await File.ReadAllTextAsync(v7,ct),ct);}
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
   await using var read=c.CreateCommand();read.CommandText="SELECT Tipo,ProximaUTC,UltimoCorte,UltimaHuella,HuellaEntrenada,HuellaCodigo,HuellaRegimen,OperacionesEntrenadas,UltimoEntrenamientoUTC FROM ia.Programacion WHERE Tipo=@t";read.Parameters.AddWithValue("@t",tipo);var schedule=(await BiConexion.LeerAsync(read,["p"],ct))["p"].Single();
   var nuevoCodigo=tipo=="Entrenamiento"&&schedule["HuellaCodigo"] is string codigoAnterior&&codigoAnterior!=Hash(string.Concat(new[]{"data.py","network.py","engine.py","cli.py"}.Select(name=>File.ReadAllText(Path.Combine(Path.GetDirectoryName(motor.Script)!,name)))));
   if(!forzado&&!nuevoCodigo&&(DateTime)schedule["ProximaUTC"]!>DateTime.UtcNow)return "NoVencida";
   // La decisión adaptativa controla antigüedad y ventas nuevas después de comprobar código e historia.
   job=Convert.ToInt64(await Scalar(c,"INSERT ia.Trabajo(Tipo,Corte,Estado) VALUES(@t,@d,'EnCurso');SELECT CAST(SCOPE_IDENTITY() AS bigint);UPDATE ia.Programacion SET Estado='EnCurso',IniciadaUTC=SYSUTCDATETIME(),Intentos=Intentos+1 WHERE Tipo=@t;",ct,("@t",tipo),("@d",cutoff.ToDateTime(TimeOnly.MinValue))));
   var dataset=await datos.Extraer(cutoff,ct);var serialized=JsonSerializer.Serialize(dataset.Datos,IaMotor.Json);var code=string.Concat(new[]{"data.py","network.py","engine.py","cli.py"}.Select(name=>File.ReadAllText(Path.Combine(Path.GetDirectoryName(motor.Script)!,name))));fingerprint=Hash(serialized+code);var codeHash=Hash(code);var historyHash=dataset.Auditoria["HuellaHistoria"]!.GetValue<string>();var sales=dataset.Auditoria["OperacionesUtilizadas"]!.GetValue<int>();
   string state;Guid? modelId=null;JsonObject detail=new(){["datos"]=dataset.Auditoria.DeepClone()};
   await using var modelRead=c.CreateCommand();modelRead.CommandText="SELECT Id,Artefacto,Huella,Evaluacion FROM ia.Modelo WHERE Activo=1";var active=(await BiConexion.LeerAsync(modelRead,["m"],ct))["m"].SingleOrDefault();
   await IaMemoria.Evaluar(c,IaReglas.Hoy,ct);
   var degraded=await IaMemoria.Degradado(c,active==null?null:(Guid)active["Id"]!,ct);
   var missingActive=active!=null&&!Disponible((string)active["Artefacto"]!,(string)active["Huella"]!);
   if((degraded||missingActive)&&active!=null){await Scalar(c,"UPDATE ia.Modelo SET Activo=0,EstadoModelo='Obsoleto/degradado' WHERE Id=@id",ct,("@id",active["Id"]));detail["modeloDegradado"]=true;active=null;}
   await using var candidateRead=c.CreateCommand();candidateRead.CommandText="SELECT TOP(1) Id,Artefacto,Huella FROM ia.Modelo WHERE Version='3s-ia-8' ORDER BY Corte DESC,Creado DESC";
   var candidate=(await BiConexion.LeerAsync(candidateRead,["s"],ct))["s"].SingleOrDefault();
   var missingCandidate=candidate!=null&&!Disponible((string)candidate["Artefacto"]!,(string)candidate["Huella"]!);
   if(degraded||missingActive||(active==null&&missingCandidate)){
    await Scalar(c,"UPDATE ia.Programacion SET HuellaEntrenada=NULL,UltimoCorte=NULL,ProximaUTC=SYSUTCDATETIME() WHERE Tipo='Entrenamiento'",ct);
    schedule["HuellaEntrenada"]=null;detail["revisionPorDeterioro"]=degraded;detail["recuperacionArtefacto"]=missingActive||missingCandidate;
   }
   // Una instalación nueva sin ventas espera; no fabrica un candidato vacío ni necesita Python todavía.
   if(sales==0&&active==null&&candidate==null){
    detail["decision"]="SinInformacionNueva";detail["motivo"]="SinVentasUtilizables";
    await consultas.ActualizarAvisos("operativo",ct);
    await Finalizar(c,job,tipo,"SinInformacionNueva",cutoff,fingerprint,null,detail,null,ct);return "SinInformacionNueva";
   }
   var runtimeActual=IaRutas.Python(config,env)+"\n"+motor.Script;
   if(Volatile.Read(ref runtimeComprobado)!=runtimeActual){await motor.Ejecutar("health",null,null,ct);Volatile.Write(ref runtimeComprobado,runtimeActual);}
   if(tipo=="Entrenamiento"){
    var analysis=await motor.Ejecutar("analyze",dataset.Datos,null,ct);detail["cambiosDemanda"]=analysis;
    var regimeHash=Hash(string.Join(";",analysis["products"]!.AsArray().Where(p=>p?["changed"]?.GetValue<bool>()==true).Select(p=>p!["id"]!.ToJsonString()+":"+(p["sustainedNoSales"]?.ToJsonString()??"false"))));
    var newRegime=analysis["changed"]!.GetValue<bool>()&&schedule["HuellaRegimen"] is string priorRegime&&priorRegime!=regimeHash;
    if(schedule["HuellaRegimen"] is null)await Scalar(c,"UPDATE ia.Programacion SET HuellaRegimen=@h WHERE Tipo='Entrenamiento'",ct,("@h",regimeHash));
    var decision=IaAprendizaje.Decidir(historyHash,codeHash,schedule["HuellaEntrenada"] as string,schedule["HuellaCodigo"] as string,sales,schedule["OperacionesEntrenadas"] as int?,schedule["UltimoEntrenamientoUTC"] as DateTime?,analysis["changed"]!.GetValue<bool>(),degraded,Math.Clamp(config.GetValue("Ia:Automatizacion:MinimasVentasNuevas",7),1,10000),Math.Clamp(config.GetValue("Ia:Automatizacion:ReentrenamientoDias",7),1,90),newRegime);
    detail["decision"]=decision;
    if(decision!="Entrenar"){state=decision;}
    else{
     await motor.Ejecutar("health",null,null,ct);modelId=Guid.NewGuid();var artifact=modelId.Value.ToString("N");
     var training=JsonSerializer.SerializeToNode(dataset.Datos,IaMotor.Json)!.AsObject();if(active!=null)training["incumbent"]=JsonNode.Parse((string)active["Evaluacion"]!);
     var m=await motor.Ejecutar("train",new{data=training,scope="production"},artifact,ct);
     var raw=await File.ReadAllTextAsync(Path.Combine(motor.Carpeta(artifact),"manifest.json"),ct);var valid=IaPublicacion.Aceptable(m);var incumbent=active==null?null:JsonNode.Parse((string)active["Evaluacion"]!)!.AsObject();
     var publish=valid&&IaPublicacion.Mejora(m,incumbent);if(publish){await motor.Ejecutar("load",null,artifact,ct);await motor.Ejecutar("infer",dataset.Datos,artifact,ct);}
     await Scalar(c,"SET XACT_ABORT ON;BEGIN TRANSACTION;IF @a=1 UPDATE ia.Modelo SET Activo=0 WHERE Activo=1;INSERT ia.Modelo(Id,Version,Alcance,Corte,Huella,Artefacto,Evaluacion,Elegible,Activo,Publicado) VALUES(@id,@version,'production',@d,@h,@f,@m,@v,@a,CASE WHEN @a=1 THEN SYSUTCDATETIME() ELSE NULL END);COMMIT;",ct,("@a",publish),("@id",modelId),("@version",m["version"]!.GetValue<string>()),("@d",cutoff.ToDateTime(TimeOnly.MinValue)),("@h",Hash(raw)),("@f",artifact),("@m",m.ToJsonString()),("@v",valid));
     await Scalar(c,"UPDATE ia.Modelo SET EstadoModelo=@s WHERE Id=@id;UPDATE ia.Programacion SET HuellaEntrenada=@hist,HuellaCodigo=@code,HuellaRegimen=@regime,OperacionesEntrenadas=@n,UltimoEntrenamientoUTC=SYSUTCDATETIME() WHERE Tipo='Entrenamiento';",ct,("@s",publish?"Validado":m["state"]?.GetValue<string>()??"Rechazado"),("@id",modelId),("@hist",historyHash),("@code",codeHash),("@regime",regimeHash),("@n",sales));
     state=publish?"Publicado":valid?"ConservadoAnterior":"CandidatoRechazado";detail["aprobados"]=m["approved"]?.DeepClone();detail["publicado"]=publish;
    }
   }else if(active==null){
    state="SinModeloValidado";
    if(candidate!=null&&!missingCandidate){
     var shadowId=(Guid)candidate["Id"]!;var artifact=(string)candidate["Artefacto"]!;var shadowHash=Hash(serialized+artifact+"shadow");
     if(Hash(await File.ReadAllTextAsync(Path.Combine(motor.Carpeta(artifact),"manifest.json"),ct))!=(string)candidate["Huella"]!)throw new InvalidOperationException("IA_ARTEFACTO_ALTERADO");
     var exists=Convert.ToInt64(await Scalar(c,"SELECT COUNT(*) FROM ia.Ejecucion WHERE Huella=@h AND Corte=@d AND Estado='Sombra'",ct,("@h",shadowHash),("@d",cutoff.ToDateTime(TimeOnly.MinValue))));
     if(exists==0){var result=await motor.Ejecutar("shadow",dataset.Datos,artifact,ct);await Persistir(c,result,shadowHash,cutoff,shadowId,ct,true);}
     detail["evaluacionEnSombra"]=true;
    }
    await consultas.ActualizarAvisos("operativo",ct);
   }
   else{
    modelId=(Guid)active["Id"]!;var artifact=(string)active["Artefacto"]!;var raw=await File.ReadAllTextAsync(Path.Combine(motor.Carpeta(artifact),"manifest.json"),ct);if(Hash(raw)!=(string)active["Huella"]!||!IaPublicacion.Aceptable(JsonNode.Parse(raw)!.AsObject()))throw new InvalidOperationException("IA_ARTEFACTO_ALTERADO");
    fingerprint=Hash(serialized+artifact);var exists=Convert.ToInt64(await Scalar(c,"SELECT COUNT(*) FROM ia.Ejecucion WHERE Huella=@h AND Corte=@d AND Procedencia='operativo' AND Estado='Completada'",ct,("@h",fingerprint),("@d",cutoff.ToDateTime(TimeOnly.MinValue))));
    if(exists>0){await motor.Ejecutar("load",null,artifact,ct);state="CargadoSinEntrenar";}
    else{var result=await motor.Ejecutar("infer",dataset.Datos,artifact,ct);await Persistir(c,result,fingerprint,cutoff,modelId.Value,ct);state="PronosticosGuardados";}
    await IaProceso.Evaluar(c,"operativo",ct);await consultas.ActualizarAvisos("operativo",ct);
   }
   await Finalizar(c,job,tipo,state,cutoff,fingerprint,modelId,detail,null,ct);return state;
  }catch(OperationCanceledException) when(ct.IsCancellationRequested){if(job>0)await Finalizar(c,job,tipo,"Interrumpida",cutoff,null,null,null,"IA_CANCELADA",CancellationToken.None);throw;}
  catch(Exception ex){logger.LogWarning("IA {Tipo} aislada: {Clase}. Los módulos comerciales continúan.",tipo,ex.GetType().Name);if(job==0)job=Convert.ToInt64(await Scalar(c,"INSERT ia.Trabajo(Tipo,Corte,Estado) VALUES(@t,@d,'EnCurso');SELECT CAST(SCOPE_IDENTITY() AS bigint);",CancellationToken.None,("@t",tipo),("@d",cutoff.ToDateTime(TimeOnly.MinValue))));await Finalizar(c,job,tipo,"Fallida",cutoff,null,null,null,"IA_PROCESO_FALLIDO",CancellationToken.None);return "Fallida";}
  finally{await Scalar(c,"EXEC sys.sp_releaseapplock @Resource='Sistema3S.IA.administrado',@LockOwner='Session';",CancellationToken.None);}
 }
 async Task Finalizar(SqlConnection c,long job,string type,string state,DateOnly cutoff,string? hash,Guid? model,JsonObject? detail,string? error,CancellationToken ct)
 {
  var hours=IaAprendizaje.HorasHastaRevision(type,error!=null,config.GetValue("Ia:Automatizacion:InferenciaHoras",24));
  await Scalar(c,"UPDATE ia.Trabajo SET Estado=@s,Finalizada=SYSUTCDATETIME(),Modelo=@m,Detalle=@j,Error=@e WHERE Id=@id;UPDATE ia.Programacion SET Estado=@s,FinalizadaUTC=SYSUTCDATETIME(),ProximaUTC=DATEADD(hour,@hours,SYSUTCDATETIME()),Error=@e,UltimoCorte=CASE WHEN @e IS NULL THEN @d ELSE UltimoCorte END,UltimaHuella=CASE WHEN @h IS NULL THEN UltimaHuella ELSE @h END WHERE Tipo=@t;",ct,("@s",state),("@m",model),("@j",detail?.ToJsonString()),("@e",error),("@id",job),("@hours",hours),("@d",cutoff.ToDateTime(TimeOnly.MinValue)),("@h",hash),("@t",type));
 }
 static async Task Persistir(SqlConnection c,JsonObject result,string hash,DateOnly cutoff,Guid modelId,CancellationToken ct,bool shadow=false)
 {
  result["modelId"]=modelId.ToString();
  await using var tx=(SqlTransaction)await c.BeginTransactionAsync(ct);
  async Task<object?> Q(string sql,params (string,object?)[] ps){await using var cmd=c.CreateCommand();cmd.Transaction=tx;cmd.CommandText=sql;foreach(var(k,v)in ps)cmd.Parameters.AddWithValue(k,v??DBNull.Value);return await cmd.ExecuteScalarAsync(ct);}
  var id=Convert.ToInt64(await Q("INSERT ia.Ejecucion(Procedencia,Corte,Huella,Estado,Resultado,Finalizada) VALUES('operativo',@d,@h,@state,@r,SYSUTCDATETIME());SELECT CAST(SCOPE_IDENTITY() AS bigint);",("@d",cutoff.ToDateTime(TimeOnly.MinValue)),("@h",hash),("@state",shadow?"Sombra":"Completada"),("@r",result.ToJsonString())));
  foreach(var p in result["products"]!.AsArray().Where(p=>p?["sufficient"]?.GetValue<bool>()==true))foreach(var h in new[]{7,30,60,90}){
   var first=DateOnly.Parse(p!["first"]!.GetValue<string>());var sql=h==7?"INSERT ia.Pronostico7(IdEjecucion,IdProducto,Desde,Hasta,Cantidad) VALUES(@id,@p,@a,@b,@q)":"INSERT ia.Pronostico(IdEjecucion,IdProducto,Desde,Hasta,Horizonte,Cantidad) VALUES(@id,@p,@a,@b,@h,@q)";
   if(!shadow)await Q(sql,("@id",id),("@p",p["id"]!.GetValue<int>()),("@a",first.ToDateTime(TimeOnly.MinValue)),("@b",first.AddDays(h-1).ToDateTime(TimeOnly.MinValue)),("@h",h),("@q",p["totals"]![h.ToString()]!.GetValue<decimal>()));
   await Q("INSERT ia.PrediccionMemoria(IdEjecucion,Modelo,Version,IdProducto,Unidad,DiaEmision,Corte,Horizonte,Desde,Hasta,Cantidad,EsSombra) VALUES(@id,@model,@version,@p,@unit,@today,@cutoff,@h,@a,@b,@q,@shadow)",
    ("@id",id),("@model",modelId),("@version",result["version"]!.GetValue<string>()),("@p",p["id"]!.GetValue<int>()),("@unit",p["unit"]!.GetValue<string>()),("@today",IaReglas.Hoy.ToDateTime(TimeOnly.MinValue)),("@cutoff",cutoff.ToDateTime(TimeOnly.MinValue)),("@h",h),("@a",first.ToDateTime(TimeOnly.MinValue)),("@b",first.AddDays(h-1).ToDateTime(TimeOnly.MinValue)),("@q",p["totals"]![h.ToString()]!.GetValue<decimal>()),("@shadow",shadow));
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
