using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Sistema3S.Web.Data;
using Sistema3S.Web.DTOs.Cotizacion;
using Sistema3S.Web.DTOs.Auth;
using Sistema3S.Web.Services.Interfaces;

namespace Sistema3S.Web.Services.Seguridad;

public sealed class OperacionAuditadaFilter(Bd3sContext db, IAuthService auth, ILogger<OperacionAuditadaFilter> logger) : IAsyncActionFilter, IOrderedFilter
{
    public int Order => -3000; // Autorizar antes de la respuesta automática por ModelState.
    // Permisos existentes; una lista permite cualquiera de sus permisos sólo en catálogos auxiliares.
    // No se expanden permisos antiguos "Gestionar ..." ni se infieren privilegios del nombre de un rol.
    internal static readonly IReadOnlyDictionary<string, string[]> PermisosPorOperacion = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) {
        ["Producto.Listar"] = ["PRODUCTOS_VER"], ["Producto.ObtenerPorId"] = ["PRODUCTOS_VER"], ["Producto.ContarActivos"] = ["PRODUCTOS_VER"],
        ["Producto.Crear"] = ["PRODUCTOS_CREAR"], ["Producto.Actualizar"] = ["PRODUCTOS_EDITAR"], ["Producto.EliminarLogico"] = ["PRODUCTOS_ELIMINAR"],
        ["Servicio.Listar"] = ["SERVICIOS_VER"], ["Servicio.ObtenerPorId"] = ["SERVICIOS_VER"], ["Servicio.ContarActivos"] = ["SERVICIOS_VER"],
        ["Servicio.Crear"] = ["SERVICIOS_CREAR"], ["Servicio.Actualizar"] = ["SERVICIOS_EDITAR"], ["Servicio.Eliminar"] = ["SERVICIOS_ELIMINAR"],
        ["Cliente.Listar"] = ["CLIENTES_VER"], ["Cliente.ObtenerPorId"] = ["CLIENTES_VER"], ["Cliente.ContarActivos"] = ["CLIENTES_VER"],
        ["Cliente.Crear"] = ["CLIENTES_CREAR"], ["Cliente.Actualizar"] = ["CLIENTES_EDITAR"], ["Cliente.Eliminar"] = ["CLIENTES_DESACTIVAR"],
        ["Cliente.ListarTiposCliente"] = ["CLIENTES_VER", "CLIENTES_CREAR", "CLIENTES_EDITAR"],
        ["Cliente.ListarTiposDocumento"] = ["CLIENTES_VER", "CLIENTES_CREAR", "CLIENTES_EDITAR"],
        ["Cliente.ListarUbigeos"] = ["CLIENTES_VER", "CLIENTES_CREAR", "CLIENTES_EDITAR", "PROVEEDORES_CREAR", "PROVEEDORES_EDITAR"],
        ["Cliente.ConsultarDni"] = ["CLIENTES_CREAR", "CLIENTES_EDITAR"], ["Cliente.ConsultarRuc"] = ["CLIENTES_CREAR", "CLIENTES_EDITAR"],
        ["Proveedor.Listar"] = ["PROVEEDORES_VER"], ["Proveedor.ObtenerPorId"] = ["PROVEEDORES_VER"], ["Proveedor.ContarActivos"] = ["PROVEEDORES_VER"],
        ["Proveedor.Crear"] = ["PROVEEDORES_CREAR"], ["Proveedor.Actualizar"] = ["PROVEEDORES_EDITAR"], ["Proveedor.EliminarLogico"] = ["PROVEEDORES_DESACTIVAR"],
        ["Proveedor.ConsultarRuc"] = ["PROVEEDORES_CREAR", "PROVEEDORES_EDITAR"],
        ["Inventario.Listar"] = ["INVENTARIO_VER"], ["Inventario.ObtenerPorProducto"] = ["INVENTARIO_VER"], ["Inventario.ObtenerResumen"] = ["INVENTARIO_VER"],
        ["Inventario.ListarMovimientosPorProducto"] = ["INVENTARIO_REPORTE"], ["Inventario.ListarMovimientosRecientes"] = ["INVENTARIO_REPORTE"],
        ["Inventario.ActualizarStockMinimo"] = ["INVENTARIO_AJUSTAR"], ["Inventario.RegistrarMovimientoManual"] = ["INVENTARIO_AJUSTAR"],
        ["Compra.Listar"] = ["COMPRAS_VER"], ["Compra.ObtenerDetalle"] = ["COMPRAS_VER"], ["Compra.GenerarPdf"] = ["COMPRAS_VER"],
        ["Compra.GenerarReportePdf"] = ["COMPRAS_REPORTE"], ["Compra.GenerarReporteExcel"] = ["COMPRAS_REPORTE"],
        ["Compra.Registrar"] = ["COMPRAS_CREAR"], ["Compra.RegistrarPago"] = ["COMPRAS_PAGAR"], ["Compra.Anular"] = ["COMPRAS_ANULAR"],
        ["Venta.Listar"] = ["VENTAS_VER"], ["Venta.ObtenerDetalle"] = ["VENTAS_VER"], ["Venta.GenerarPdfVenta"] = ["VENTAS_VER"],
        ["Venta.GenerarReportePdf"] = ["VENTAS_REPORTE"], ["Venta.GenerarReporteExcel"] = ["VENTAS_REPORTE"],
        ["Venta.ObtenerSiguienteComprobante"] = ["VENTAS_CREAR"], ["Venta.Registrar"] = ["VENTAS_CREAR"],
        ["Venta.RegistrarPago"] = ["VENTAS_COBRAR"], ["Venta.Anular"] = ["VENTAS_ANULAR"],
        ["Cotizacion.Listar"] = ["COTIZACIONES_VER"], ["Cotizacion.ObtenerPorId"] = ["COTIZACIONES_VER"], ["Cotizacion.ContarPendientes"] = ["COTIZACIONES_VER"],
        ["Cotizacion.Crear"] = ["COTIZACIONES_CREAR"], ["Cotizacion.Cancelar"] = ["COTIZACIONES_CANCELAR"],
        ["Cotizacion.CambiarEstado"] = ["COTIZACIONES_EDITAR"], ["Cotizacion.MarcarRespondida"] = ["COTIZACIONES_EDITAR"],
        ["Cotizacion.GenerarPdf"] = ["COTIZACIONES_VER"], ["Cotizacion.EnviarCorreo"] = ["COTIZACIONES_ENVIAR_CORREO"],
        ["Cotizacion.EnviarWhatsApp"] = ["COTIZACIONES_ENVIAR_WHATSAPP"], ["Cotizacion.ObtenerWhatsApp"] = ["COTIZACIONES_ENVIAR_WHATSAPP"],
        ["Cotizacion.PrepararParaVenta"] = ["COTIZACIONES_CONVERTIR_VENTA"], ["Cotizacion.MarcarConvertidaVenta"] = ["COTIZACIONES_CONVERTIR_VENTA"],
        ["Cotizacion.ListarClientes"] = ["COTIZACIONES_CREAR", "VENTAS_CREAR"],
        ["Cotizacion.ListarElementosCotizables"] = ["COTIZACIONES_CREAR", "VENTAS_CREAR"],
        ["Cotizacion.ListarEstados"] = ["COTIZACIONES_VER", "COTIZACIONES_EDITAR"],
        ["Caja.ObtenerCajaActiva"] = ["CAJA_VER"], ["Caja.ObtenerResumen"] = ["CAJA_VER"], ["Caja.ListarMovimientos"] = ["CAJA_VER"],
        ["Caja.AbrirCaja"] = ["CAJA_ABRIR"], ["Caja.CerrarCaja"] = ["CAJA_CERRAR"],
        ["Caja.RegistrarMovimientoManual"] = ["CAJA_MOVIMIENTO_MANUAL"], ["Caja.ObtenerReporte"] = ["CAJA_REPORTE"],
        ["Usuario.Listar"] = ["USUARIOS_VER"], ["Usuario.Crear"] = ["USUARIOS_CREAR"], ["Usuario.Actualizar"] = ["USUARIOS_EDITAR"],
        ["Usuario.Activar"] = ["USUARIOS_EDITAR"], ["Usuario.Desactivar"] = ["USUARIOS_DESACTIVAR"], ["Usuario.CambiarContrasena"] = ["USUARIOS_CAMBIAR_CONTRASENA"],
        ["Rol.Listar"] = ["ROLES_VER", "USUARIOS_VER", "USUARIOS_CREAR", "USUARIOS_EDITAR", "ROLES_GESTIONAR_PERMISOS"],
        ["Rol.Crear"] = ["ROLES_CREAR"], ["Rol.Actualizar"] = ["ROLES_EDITAR"], ["Rol.Desactivar"] = ["ROLES_DESACTIVAR"],
        ["Rol.ListarPermisos"] = ["ROLES_VER", "ROLES_GESTIONAR_PERMISOS"], ["Rol.ObtenerPermisosPorRol"] = ["ROLES_VER", "ROLES_GESTIONAR_PERMISOS"],
        ["Rol.AsignarPermisos"] = ["ROLES_GESTIONAR_PERMISOS"],
        ["Catalogo.ListarCategorias"] = ["PRODUCTOS_VER", "PRODUCTOS_CREAR", "PRODUCTOS_EDITAR"],
        ["Catalogo.ListarMarcas"] = ["PRODUCTOS_VER", "PRODUCTOS_CREAR", "PRODUCTOS_EDITAR"],
        ["Catalogo.ListarUnidadesMedida"] = ["PRODUCTOS_VER", "PRODUCTOS_CREAR", "PRODUCTOS_EDITAR"],
        // Sólo estado técnico de la conexión para la cabecera; no expone registros comerciales.
        ["Prueba.Conexion"] = []
    };
    private static readonly HashSet<string> Internos = new(StringComparer.OrdinalIgnoreCase) {
        "Caja", "Compra", "Venta", "Cotizacion", "Producto", "Servicio", "Cliente",
        "Proveedor", "Inventario", "Usuario", "Rol", "Catalogo", "Auditoria", "Prueba"
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

            // El catálogo y la cuenta pública usan Publico/ClienteWeb. Todo el panel exige sesión interna.
            if (interno && (usuario == null || esCliente))
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
            if (interno && usuario != null && !admin)
            {
                var clave = $"{controlador}.{accion}";
                PermisosPorOperacion.TryGetValue(clave, out var requeridos);
                if (controlador == "Usuario" && accion == "Listar" && context.ActionArguments.TryGetValue("clientes", out var clientes) && clientes is true)
                    requeridos = ["CLIENTES_VER"];
                if (controlador == "Cotizacion" && accion == "CambiarEstado" && context.ActionArguments.Values.OfType<CotizacionCambiarEstadoDto>().FirstOrDefault() is { } cambio)
                {
                    var nombre = cambio.NuevoEstado;
                    if (string.IsNullOrWhiteSpace(nombre) && cambio.IdEstadoCotizacion.HasValue)
                        nombre = await db.EstadoCotizacion.Where(e => e.IdEstadoCotizacion == cambio.IdEstadoCotizacion.Value).Select(e => e.Nombre).FirstOrDefaultAsync();
                    if (nombre?.Trim().Equals("Aprobada", StringComparison.OrdinalIgnoreCase) == true) requeridos = ["COTIZACIONES_APROBAR"];
                    if (nombre?.Trim().StartsWith("Convertida", StringComparison.OrdinalIgnoreCase) == true) requeridos = ["COTIZACIONES_CONVERTIR_VENTA"];
                }
                var permisos = (await auth.ObtenerPermisosUsuarioAsync(id)).Where(p => p.Asignado).Select(p => p.Nombre.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
                if (requeridos == null || (requeridos.Length > 0 && !requeridos.Any(permisos.Contains)))
                {
                    context.Result = new ObjectResult(new { mensaje = "No tienes permiso para realizar esta acción." }) { StatusCode = 403 };
                    await RegistrarSolicitud(auditoria, "DENEGADA", 403);
                    return;
                }
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
