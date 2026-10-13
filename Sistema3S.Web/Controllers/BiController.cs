using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Sistema3S.Web.DTOs.Bi;
using Sistema3S.Web.Services.Bi;

namespace Sistema3S.Web.Controllers;

[Authorize]
[Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("bi")]
[ApiController]
[Route("api/bi")]
public sealed class BiController(IBiService service,BiExportacion exportacion,ILogger<BiController> logger) : ControllerBase
{
    [HttpGet("{vista}")]
    public Task<IActionResult> Consultar(string vista,[FromQuery] BiFiltro filtro,CancellationToken ct)
        => Ejecutar(async()=>Ok(await service.ConsultarAsync(vista,filtro,ct)));
    [HttpGet("opciones")]
    public Task<IActionResult> Opciones([FromQuery]string procedencia="operativo",[FromQuery]string? buscar=null,CancellationToken ct=default)
        => Ejecutar(async()=>Ok(await service.OpcionesAsync(procedencia,buscar,ct)));
    [HttpGet("operaciones/{tipo}/{id:int}")]
    public Task<IActionResult> Detalle(string tipo,int id,CancellationToken ct)
        => Ejecutar(async()=>Ok(await service.DetalleAsync(tipo,id,ct)));
    private async Task<IActionResult> Ejecutar(Func<Task<IActionResult>> action)
    {
        try {return await action();}
        catch(ArgumentException e){return BadRequest(new{mensaje=e.Message});}
        catch(OperationCanceledException) when(HttpContext.RequestAborted.IsCancellationRequested){return StatusCode(499);}
        catch(SqlException e){logger.LogError(e,"BI no disponible. Correlación {Solicitud}",HttpContext.TraceIdentifier);return StatusCode(503,new{mensaje="No se pudo consultar el análisis. Intenta nuevamente en unos momentos.",referencia=HttpContext.TraceIdentifier});}
        catch(Exception e){logger.LogError(e,"Error BI. Correlación {Solicitud}",HttpContext.TraceIdentifier);return StatusCode(500,new{mensaje="No se pudo generar el análisis o el archivo. Intenta nuevamente.",referencia=HttpContext.TraceIdentifier});}
    }
}
