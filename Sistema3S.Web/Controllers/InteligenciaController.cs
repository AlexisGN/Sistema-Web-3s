using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sistema3S.Web.Services.Comercial;
namespace Sistema3S.Web.Controllers;

[Authorize,ApiController,Route("api/inteligencia")]
[Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("bi")]
public sealed class InteligenciaController(ComercialService service,ComercialExportacion exportacion,ILogger<InteligenciaController> logger):ControllerBase
{
 [HttpGet("{vista}")]public Task<IActionResult> Consultar(string vista,[FromQuery]ComercialFiltro filtro,CancellationToken ct)=>Ejecutar(async()=>Ok(await service.Consultar(vista,filtro,ct)));
 [HttpGet("detalle/{tipo}")]public Task<IActionResult> Detalle(string tipo,[FromQuery]ComercialFiltro filtro,[FromQuery]string? segmento,[FromQuery]int? pagina,CancellationToken ct)=>Ejecutar(async()=>Ok(await service.Detalle(tipo,segmento??"todos",filtro,pagina??1,ct)));
 [HttpGet("{vista}/excel")]public Task<IActionResult> Excel(string vista,[FromQuery]ComercialFiltro filtro,CancellationToken ct)=>Ejecutar(async()=>File(exportacion.Excel(await service.Consultar(vista,filtro,ct)),"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet","3S-Inteligencia-Comercial.xlsx"));
 [HttpGet("{vista}/pdf")]public Task<IActionResult> Pdf(string vista,[FromQuery]ComercialFiltro filtro,CancellationToken ct)=>Ejecutar(async()=>File(exportacion.Pdf(await service.Consultar(vista,filtro,ct)),"application/pdf","3S-Inteligencia-Comercial.pdf"));
 private async Task<IActionResult> Ejecutar(Func<Task<IActionResult>> action){try{return await action();}catch(ArgumentException e){return BadRequest(new{mensaje=e.Message});}catch(OperationCanceledException)when(HttpContext.RequestAborted.IsCancellationRequested){return StatusCode(499);}catch(Exception e){logger.LogError(e,"Lectura de Inteligencia Comercial no disponible");return StatusCode(503,new{mensaje="No pudimos consultar este análisis. Las operaciones comerciales siguen disponibles."});}}
}
