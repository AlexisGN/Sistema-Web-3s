using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
namespace Sistema3S.Web.Services.Ia;

public sealed class IaMotorAdministrado(IConfiguration config,IWebHostEnvironment env)
{
 public string Raiz=>Path.Combine(IaRutas.Modelos(config,env),"administrado");
 public string Script=>Path.Combine(IaRutas.Directorio(env),"motor","v8","cli.py");
 public string Carpeta(string id){if(!Guid.TryParseExact(id,"N",out _))throw new ArgumentException("IA_ID_ARTEFACTO_INVALIDO");return Path.Combine(Raiz,id);}
 public async Task<JsonObject> Ejecutar(string action,object? payload,string? artifact,CancellationToken ct)
 {
  if(action is not ("analyze" or "health" or "train" or "load" or "infer" or "shadow"))throw new ArgumentException("IA_COMANDO_INVALIDO");
  var python=IaRutas.Python(config,env);if(!File.Exists(python)||!File.Exists(Script))throw new InvalidOperationException("IA_RUNTIME_NO_CONFIGURADO");
  var web=Path.GetFullPath(env.WebRootPath??Path.Combine(env.ContentRootPath,"wwwroot"))+Path.DirectorySeparatorChar;
  if(Path.GetFullPath(Raiz).StartsWith(web,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("IA_ALMACEN_PUBLICO_DENEGADO");
  var info=new ProcessStartInfo(python){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true};info.ArgumentList.Add(Script);info.ArgumentList.Add(action);if(artifact!=null)info.ArgumentList.Add(Carpeta(artifact));
  var utf8=new System.Text.UTF8Encoding(false);info.StandardInputEncoding=utf8;info.StandardOutputEncoding=utf8;info.StandardErrorEncoding=utf8;
  info.Environment["PYTHONUTF8"]="1";info.Environment["PYTHONIOENCODING"]="utf-8";
  info.Environment["OPENBLAS_NUM_THREADS"]="2";info.Environment["OMP_NUM_THREADS"]="2";info.Environment["TF_CPP_MIN_LOG_LEVEL"]="2";info.Environment["TF_NUM_INTRAOP_THREADS"]="2";info.Environment["TF_NUM_INTEROP_THREADS"]="2";
  using var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct);timeout.CancelAfter(TimeSpan.FromSeconds(action=="train"?Math.Clamp(config.GetValue("Ia:Automatizacion:EntrenamientoTimeoutSegundos",1800),60,7200):Math.Clamp(config.GetValue("Ia:TimeoutSegundos",600),30,1800)));
  using var p=Process.Start(info)??throw new InvalidOperationException("IA_PROCESO_NO_INICIADO");var stdout=p.StandardOutput.ReadToEndAsync(timeout.Token);var stderr=p.StandardError.ReadToEndAsync(timeout.Token);
  try{if(payload!=null)await p.StandardInput.WriteAsync(JsonSerializer.Serialize(payload,IaMotor.Json).AsMemory(),timeout.Token);p.StandardInput.Close();await p.WaitForExitAsync(timeout.Token);await Task.WhenAll(stdout,stderr);if(p.ExitCode!=0){try{var logs=Path.Combine(Raiz,"diagnostico");Directory.CreateDirectory(logs);await File.WriteAllTextAsync(Path.Combine(logs,DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff")+"-"+action+".log"),stderr.Result[..Math.Min(stderr.Result.Length,16000)],ct);}catch{}throw new InvalidOperationException("IA_PROCESO_FALLIDO");}if(stdout.Result.Length>40000000)throw new InvalidOperationException("IA_RESPUESTA_EXCESIVA");return JsonNode.Parse(stdout.Result)?.AsObject()??throw new InvalidOperationException("IA_RESPUESTA_INVALIDA");}
  catch{if(!p.HasExited){p.Kill(true);await p.WaitForExitAsync(CancellationToken.None);}throw;}
 }
}
