using System.Threading.Channels;
using Sistema3S.Web.Services.Implementations;
namespace Sistema3S.Web.Services.Seguridad;

// La respuesta pública no depende de si existe la cuenta ni del tiempo que tarda SMTP.
public sealed class RecuperacionClienteQueue
{
    internal readonly Channel<string> Canal = Channel.CreateBounded<string>(new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.Wait });
    public bool Agregar(string correo) => Canal.Writer.TryWrite(correo.Trim().ToLowerInvariant());
}
public sealed class RecuperacionClienteWorker(RecuperacionClienteQueue queue, IServiceScopeFactory scopes,
    ILogger<RecuperacionClienteWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var correo in queue.Canal.Reader.ReadAllAsync(stoppingToken))
        {
            try { using var scope = scopes.CreateScope(); await scope.ServiceProvider.GetRequiredService<ClienteCuentaService>().EnviarRecuperacionAsync(correo, stoppingToken); }
            catch (OperationCanceledException) when(stoppingToken.IsCancellationRequested) { break; }
            catch { logger.LogError("No se pudo procesar un correo de recuperación. Revisar disponibilidad de SQL y SMTP. No se registran tokens ni destinatarios."); }
        }
    }
}
