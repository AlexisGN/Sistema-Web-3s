using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
namespace Sistema3S.Web.Services.Ia;

public sealed class IaMotor(IConfiguration config,IWebHostEnvironment env,ILogger<IaMotor> logger)
{
    public static readonly JsonSerializerOptions Json=new(JsonSerializerDefaults.Web);
    public string HuellaVersion{get{var path=IaRutas.Motor(env);return File.Exists(path)?Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path))):"IA_RUNTIME_NO_CONFIGURADO";}}
    public async Task<JsonObject> Ejecutar(IaEntrada input,CancellationToken ct)
    {
        var python=IaRutas.Python(config,env);
        var script=IaRutas.Motor(env);
        var storage=IaRutas.Modelos(config,env);
        if(string.IsNullOrWhiteSpace(python)||!File.Exists(python)||!File.Exists(script))throw new InvalidOperationException("IA_RUNTIME_NO_CONFIGURADO");
        if(storage.StartsWith(Path.GetFullPath(env.WebRootPath??Path.Combine(env.ContentRootPath,"wwwroot")),StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("IA_ALMACEN_PUBLICO_DENEGADO");
        var info=new ProcessStartInfo(python){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true};
        info.ArgumentList.Add(script);info.ArgumentList.Add(Path.Combine(storage,input.Source=="referencia"?"referencia-local":input.Source));
        info.Environment["TF_CPP_MIN_LOG_LEVEL"]="2";
        using var p=Process.Start(info)??throw new InvalidOperationException("IA_MOTOR_NO_INICIADO");
        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(config.GetValue("Ia:TimeoutSegundos",600),30,1800)));
        var stdout=p.StandardOutput.ReadToEndAsync(timeout.Token);var stderr=p.StandardError.ReadToEndAsync(timeout.Token);
        try
        {
            await p.StandardInput.WriteAsync(JsonSerializer.Serialize(input,Json).AsMemory(),timeout.Token);p.StandardInput.Close();
            await p.WaitForExitAsync(timeout.Token);await Task.WhenAll(stdout,stderr);
            if(p.ExitCode!=0){logger.LogWarning("El motor IA terminó con código {Codigo}; salida técnica retenida fuera del panel",p.ExitCode);throw new InvalidOperationException("IA_MOTOR_FALLIDO");}
            if(stdout.Result.Length>40000000)throw new InvalidOperationException("IA_RESPUESTA_EXCESIVA");
            return JsonNode.Parse(stdout.Result)?.AsObject()??throw new InvalidOperationException("IA_RESPUESTA_INVALIDA");
        }
        catch {if(!p.HasExited){p.Kill(true);await p.WaitForExitAsync(CancellationToken.None);}throw;}
    }
}
