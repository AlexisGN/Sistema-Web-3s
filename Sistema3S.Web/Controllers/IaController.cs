using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Sistema3S.Web.Services.Ia;
namespace Sistema3S.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/ia")]
[Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("ia")]
public sealed class IaController(IaService service,IaCompras compras,IaDatos datos,ILogger<IaController> logger):ControllerBase
{
    private int Usuario=>int.Parse(User.FindFirst("idUsuario")!.Value);
    [HttpGet("resumen")] public Task<IActionResult> Resumen([FromQuery]string procedencia="operativo",[FromQuery]int? producto=null,CancellationToken ct=default,[FromQuery]DateOnly? fechaObjetivo=null)=>Leer("resumen",procedencia,producto,ct,fechaObjetivo);
    [HttpGet("pronostico")] public Task<IActionResult> Pronostico([FromQuery]string procedencia="operativo",[FromQuery]int? producto=null,CancellationToken ct=default,[FromQuery]DateOnly? fechaObjetivo=null)=>Leer("pronostico",procedencia,producto,ct,fechaObjetivo);
    [HttpGet("proyeccion")] public Task<IActionResult> Proyeccion([FromQuery]string procedencia="operativo",[FromQuery]int? producto=null,CancellationToken ct=default,[FromQuery]DateOnly? fechaObjetivo=null)=>Leer("proyeccion",procedencia,producto,ct,fechaObjetivo);
    [HttpGet("inventario")] public Task<IActionResult> Inventario([FromQuery]string procedencia="operativo",[FromQuery]int? producto=null,CancellationToken ct=default,[FromQuery]DateOnly? fechaObjetivo=null)=>Leer("inventario",procedencia,producto,ct,fechaObjetivo);
    [HttpGet("compras")] public Task<IActionResult> Compras([FromQuery]string procedencia="operativo",[FromQuery]int? producto=null,CancellationToken ct=default,[FromQuery]DateOnly? fechaObjetivo=null)=>Leer("compras",procedencia,producto,ct,fechaObjetivo);
    [HttpGet("seguimiento")] public Task<IActionResult> Seguimiento([FromQuery]string procedencia="operativo",[FromQuery]int? producto=null,CancellationToken ct=default,[FromQuery]DateOnly? fechaObjetivo=null)=>Leer("seguimiento",procedencia,producto,ct,fechaObjetivo);
    [HttpGet("avisos")] public Task<IActionResult> Avisos([FromQuery]string procedencia="operativo",CancellationToken ct=default)=>Ejecutar(async()=>Ok(await service.Avisos(ct,procedencia)));
    [HttpGet("productos")] public Task<IActionResult> Productos([FromQuery]string procedencia="operativo",CancellationToken ct=default)=>Ejecutar(async()=>Ok((await datos.Contexto(service.Procedencia(procedencia),ct)).Select(p=>new{p.IdProducto,p.Nombre,p.Unidad})));
    [HttpGet("proveedores")] public Task<IActionResult> Proveedores(CancellationToken ct)=>Ejecutar(async()=>Ok(await compras.Proveedores(ct)));
    [HttpPost("abastecimiento/vista-previa")] public Task<IActionResult> PrevisualizarAbastecimiento(IaPolitica dto,CancellationToken ct)=>Ejecutar(async()=>Ok(await service.PrevisualizarAbastecimiento(dto,ct)));
    [HttpPut("abastecimiento")] public Task<IActionResult> Abastecimiento(IaPolitica dto,CancellationToken ct)=>Ejecutar(async()=>{await service.GuardarPolitica(dto,Usuario,ct);return Ok(new{mensaje="Configuración de abastecimiento verificada."});});
    [HttpPut("recomendaciones/{id:long}")] public Task<IActionResult> Revisar(long id,IaRevision dto,[FromQuery]string procedencia="operativo",CancellationToken ct=default)=>Ejecutar(async()=>{await service.Revisar(id,dto,Usuario,procedencia,ct);return Ok(new{mensaje="Revisión registrada."});});
    [HttpPost("recomendaciones/{id:long}/solicitud")] public Task<IActionResult> Solicitud(long id,IaSolicitud dto,CancellationToken ct)=>Ejecutar(async()=>Ok(new{id=await compras.Crear(id,dto,Usuario,ct),mensaje="Solicitud preparada. Revisa y registra la compra en su formulario."}));
    [HttpGet("solicitudes/{id:long}")] public Task<IActionResult> ObtenerSolicitud(long id,CancellationToken ct)=>Ejecutar(async()=>Ok(await compras.Obtener(id,ct)));
    [HttpPut("solicitudes/{id:long}/compra")] public Task<IActionResult> Vincular(long id,IaVinculo dto,CancellationToken ct)=>Ejecutar(async()=>{await compras.Vincular(id,dto.IdCompra,Usuario,ct);return Ok(new{mensaje="Compra existente vinculada."});});
    private Task<IActionResult> Leer(string view,string source,int? product,CancellationToken ct,DateOnly? fechaObjetivo=null)=>Ejecutar(async()=>
    {
        IaPeriodo.ValidarFecha(fechaObjetivo,IaReglas.Hoy);
        return Ok(await service.Consultar(view,source,product,ct,fechaObjetivo));
    });
    private async Task<IActionResult> Ejecutar(Func<Task<IActionResult>> action)
    {
        try{return await action();}
        catch(ArgumentException e){return BadRequest(new{mensaje=e.Message});}
        catch(OperationCanceledException)when(HttpContext.RequestAborted.IsCancellationRequested){return StatusCode(499);}
        catch(Exception e){logger.LogError(e,"Consulta IA aislada fallida; referencia {Solicitud}",HttpContext.TraceIdentifier);return StatusCode(503,new{mensaje="El análisis no está disponible por el momento. Las operaciones comerciales continúan disponibles.",referencia=HttpContext.TraceIdentifier});}
    }
}
