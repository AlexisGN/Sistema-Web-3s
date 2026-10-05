using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Sistema3S.Web.DTOs.ClienteWeb;
using Sistema3S.Web.Services.Implementations;
using Sistema3S.Web.Services.Seguridad;

namespace Sistema3S.Web.Controllers;

[ApiController, Route("api/cliente-web"), Authorize, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ClienteCuentaController(ClienteCuentaService service, RecuperacionClienteQueue queue,
    ILogger<ClienteCuentaController> logger) : ControllerBase
{
    private int Usuario => int.TryParse(User.FindFirst("idUsuario")?.Value,out var id) ? id : 0;
    private int Cliente => int.TryParse(User.FindFirst("idCliente")?.Value,out var id) ? id : 0;
    [HttpGet("perfil")]
    public Task<IActionResult> Perfil() => Ejecutar(() => service.PerfilAsync(Usuario,Cliente));
    [HttpPut("perfil")]
    public Task<IActionResult> Editar(ClientePerfilEditarDto dto) => Ejecutar(() => service.EditarPerfilAsync(Usuario,Cliente,dto));
    [HttpGet("ubicaciones")]
    public Task<IActionResult> Ubicaciones([FromQuery] string? q="") => Ejecutar(() => service.UbigeosAsync((q ?? "").Trim()[..Math.Min((q ?? "").Trim().Length,80)]));
    [HttpPost("cambiar-contrasena"), EnableRateLimiting("cuenta-cliente")]
    public Task<IActionResult> Cambiar(ClienteCambiarClaveDto dto) => Ejecutar(async () => { await service.CambiarClaveAsync(Usuario,dto); return new { mensaje="Contraseña actualizada. Inicia sesión nuevamente." }; });

    [AllowAnonymous, HttpPost("recuperar-contrasena"), EnableRateLimiting("cuenta-cliente")]
    public async Task<IActionResult> Recuperar(ClienteRecuperarDto dto, CancellationToken ct)
    {
        try { await service.VerificarRecuperacionAsync(ct); }
        catch (InvalidOperationException e)
        {
            logger.LogError("Recuperación de clientes no configurada: {Motivo}", e.Message);
            return StatusCode(503,new { mensaje="La recuperación aún no está habilitada. Contacta al equipo 3S para que revise la configuración del servicio." });
        }
        catch (OperationCanceledException) when(ct.IsCancellationRequested) { throw; }
        catch
        {
            logger.LogError("Recuperación de clientes: no se pudo comprobar la base SQL. No se registran datos personales.");
            return StatusCode(503,new { mensaje="No pudimos conectar con el servicio de recuperación. Inténtalo nuevamente en unos minutos." });
        }
        if (!queue.Agregar(dto.Correo)) return StatusCode(503,new { mensaje="El servicio está ocupado. Inténtalo nuevamente en unos minutos." });
        return Accepted(new { mensaje="Si existe una cuenta asociada a ese correo, recibirás instrucciones. Revisa también tu carpeta de correo no deseado." });
    }
    [AllowAnonymous, HttpPost("restablecer-contrasena"), EnableRateLimiting("cuenta-cliente")]
    public async Task<IActionResult> Restablecer(ClienteRestablecerDto dto)
    {
        try { await service.RestablecerAsync(dto); return Ok(new { mensaje="Tu contraseña fue actualizada. Ya puedes iniciar sesión." }); }
        catch(InvalidOperationException e) { return BadRequest(new { mensaje=e.Message }); }
        catch { logger.LogError("Fallo al restablecer contraseña de cliente; revisar SQL."); return StatusCode(503,new { mensaje="No se pudo actualizar la contraseña. Inténtalo más tarde." }); }
    }
    private async Task<IActionResult> Ejecutar(Func<Task<object>> action)
    {
        if(Usuario<=0 || Cliente<=0) return Unauthorized(new { mensaje="Inicia sesión con tu cuenta de cliente." });
        try { return Ok(await action()); }
        catch(InvalidOperationException e) { return BadRequest(new { mensaje=e.Message }); }
        catch { logger.LogError("Fallo en una operación de cuenta de cliente."); return StatusCode(503,new { mensaje="No se pudo completar la operación. Inténtalo nuevamente." }); }
    }
}
