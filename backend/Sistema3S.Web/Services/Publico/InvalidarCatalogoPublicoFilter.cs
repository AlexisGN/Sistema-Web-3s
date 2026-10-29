using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.OutputCaching;

namespace Sistema3S.Web.Services.Publico;

public sealed class InvalidarCatalogoPublicoFilter(EstadoCatalogoPublico estado, IOutputCacheStore cache,
    ILogger<InvalidarCatalogoPublicoFilter> logger) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var result = await next();
        var request = context.HttpContext.Request;
        var controller = context.RouteData.Values["controller"]?.ToString();
        if (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method)
            || HttpMethods.IsOptions(request.Method)
            || controller is not ("Producto" or "Servicio" or "Catalogo")
            || result.Exception is not null) return;
        var status = (result.Result as IStatusCodeActionResult)?.StatusCode ?? context.HttpContext.Response.StatusCode;
        if(status is < 200 or >= 400) return;
        estado.Cambiar();
        try { await cache.EvictByTagAsync(EstadoCatalogoPublico.Etiqueta, CancellationToken.None); }
        catch(Exception ex) { logger.LogWarning(ex,"No se pudo retirar la generación anterior del catálogo público; su versión ya fue invalidada."); }
    }
}
