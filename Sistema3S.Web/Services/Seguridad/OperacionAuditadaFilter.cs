using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Sistema3S.Web.Data;
using Sistema3S.Web.DTOs.Cotizacion;
using Sistema3S.Web.DTOs.Auth;

namespace Sistema3S.Web.Services.Seguridad;

public sealed class OperacionAuditadaFilter(Bd3sContext db, ILogger<OperacionAuditadaFilter> logger) : IAsyncActionFilter
{
    private static readonly HashSet<string> Internos = new(StringComparer.OrdinalIgnoreCase) {
        "Caja", "Compra", "Venta", "Cotizacion", "Producto", "Servicio", "Cliente",
        "Proveedor", "Inventario", "Usuario", "Rol", "Catalogo", "Auditoria"
    };
    private static readonly HashSet<string> Responsables = new(StringComparer.OrdinalIgnoreCase) {
        "IdUsuarioRegistro", "IdUsuarioAtencion", "IdUsuarioApertura", "IdUsuarioCierre"
    };

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        var controlador = context.RouteData.Values["controller"]?.ToString() ?? "";
        var accion = context.RouteData.Values["action"]?.ToString() ?? "";
        var escritura = !HttpMethods.IsGet(http.Request.Method) && !HttpMethods.IsHead(http.Request.Method);
        var anterior = ContextoAuditoria.Actual.Value;
        var auditoria = new ContextoAuditoria { Accion = $"{controlador}.{accion}", Solicitud = http.TraceIdentifier,
            Origen = Internos.Contains(controlador) ? "Administración" : "Web" };
        ContextoAuditoria.Actual.Value = auditoria;
        try
        {
            var interno = Internos.Contains(controlador);
            var idTexto = http.User.FindFirst("idUsuario")?.Value;
            var tieneIdentidad = http.User.Identity?.IsAuthenticated == true && int.TryParse(idTexto, out _);
            var id = tieneIdentidad ? int.Parse(idTexto!) : 0;
            var usuario = tieneIdentidad ? await db.Usuario.AsNoTracking()
                .Where(u => u.IdUsuario == id && u.Estado && u.IdRolNavigation.Estado)
                .Select(u => new { u.IdUsuario, Rol = u.IdRolNavigation.Nombre }).FirstOrDefaultAsync() : null;
            auditoria.IdUsuario = usuario?.IdUsuario;
            var esCliente = http.User.HasClaim(c => c.Type == "idCliente") || usuario?.Rol.Trim().Equals("Cliente", StringComparison.OrdinalIgnoreCase) == true
                || usuario?.Rol.Trim().Equals("Cliente web", StringComparison.OrdinalIgnoreCase) == true;
            var admin = usuario != null && !esCliente && (usuario.Rol.Trim().Equals("Administrador", StringComparison.OrdinalIgnoreCase)
                || usuario.Rol.Trim().Equals("Admin", StringComparison.OrdinalIgnoreCase));

            // Las consultas públicas conservan sus rutas. Toda escritura administrativa exige sesión interna.
            if (interno && (escritura || controlador is "Auditoria" or "Usuario") && (usuario == null || esCliente))
            {
                var status = usuario == null ? 401 : 403;
                context.Result = new ObjectResult(new { mensaje = "Inicia sesión con un usuario interno autorizado." }) { StatusCode = status };
                await RegistrarSolicitud(auditoria, "DENEGADA", status);
                return;
            }
            var anular = accion.Contains("Anular", StringComparison.OrdinalIgnoreCase) || accion.Equals("Cancelar", StringComparison.OrdinalIgnoreCase);
            if (controlador == "Cotizacion" && context.ActionArguments.Values.OfType<CotizacionCambiarEstadoDto>().FirstOrDefault() is { } estado)
            {
                var nombre = estado.NuevoEstado;
                if (string.IsNullOrWhiteSpace(nombre) && estado.IdEstadoCotizacion.HasValue)
                    nombre = await db.EstadoCotizacion.Where(e => e.IdEstadoCotizacion == estado.IdEstadoCotizacion.Value).Select(e => e.Nombre).FirstOrDefaultAsync();
                anular |= nombre?.Trim().Equals("Cancelada", StringComparison.OrdinalIgnoreCase) == true;
            }
            if ((anular || controlador == "Auditoria") && !admin)
            {
                context.Result = new ObjectResult(new { mensaje = "Sólo el administrador puede realizar esta acción." }) { StatusCode = 403 };
                await RegistrarSolicitud(auditoria, "DENEGADA", 403);
                return;
            }
            if (interno && usuario != null)
            {
                foreach (var key in context.ActionArguments.Keys.ToArray())
                {
                    if (Responsables.Contains(key) || (controlador == "Caja" && key.Equals("idUsuario", StringComparison.OrdinalIgnoreCase)))
                        context.ActionArguments[key] = id;
                    else if (context.ActionArguments[key] is { } dto)
                        foreach (var property in dto.GetType().GetProperties().Where(p => p.CanWrite && Responsables.Contains(p.Name)))
                            property.SetValue(dto, id);
                }
                // Los cuerpos opcionales de cotización también deben llevar el responsable autenticado.
                if (controlador == "Cotizacion" && context.ActionArguments.TryGetValue("dto", out var valor) && valor == null)
                {
                    if (accion == "Cancelar") context.ActionArguments["dto"] = new CotizacionCambiarEstadoDto { IdUsuarioAtencion = id };
                    if (accion is "EnviarCorreo" or "EnviarWhatsApp") context.ActionArguments["dto"] = new CotizacionEnviarDto { IdUsuarioAtencion = id };
                }
            }
            var resultado = await next();
            if (controlador == "Auth" && accion == "Login" && resultado.Result is OkObjectResult { Value: LoginResultadoDto login })
                auditoria.IdUsuario = login.IdUsuario;
            if (escritura)
            {
                var status = resultado.Exception != null ? 500 : (resultado.Result as Microsoft.AspNetCore.Mvc.Infrastructure.IStatusCodeActionResult)?.StatusCode ?? http.Response.StatusCode;
                await RegistrarSolicitud(auditoria, resultado.Exception != null || status >= 400 ? "ERROR" : "SOLICITUD", status);
            }
        }
        finally { ContextoAuditoria.Actual.Value = anterior; }
    }

    private async Task RegistrarSolicitud(ContextoAuditoria contexto, string resultado, int status)
    {
        try
        {
            // Sin cuerpos, contraseñas, tokens ni direcciones de correo. Los triggers guardan los cambios confirmados.
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT dbo.AuditoriaLog (IdUsuario, Accion, TablaAfectada, Fecha, Descripcion, Solicitud, Origen) VALUES ({contexto.IdUsuario}, {resultado}, {"API"}, GETDATE(), {contexto.Accion + " HTTP " + status}, {contexto.Solicitud}, {contexto.Origen})");
        }
        catch (Exception ex) { logger.LogError(ex, "No se pudo registrar el resultado de la solicitud {Solicitud}", contexto.Solicitud); }
    }
}
