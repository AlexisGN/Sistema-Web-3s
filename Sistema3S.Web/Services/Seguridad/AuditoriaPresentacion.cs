using System.Globalization;
using System.Text.Json;

namespace Sistema3S.Web.Services.Seguridad;

public sealed record FilaAuditoria(int IdLog, int? IdUsuario, string Responsable, string Accion,
    string Tabla, string? Registro, DateTime Fecha, string Descripcion, string? Antes, string? Despues,
    string Grupo, string Origen, string Modulo);
public sealed record CambioAuditoria(string Campo, string Antes, string Despues);
public sealed record DetalleAuditoria(string Registro, string Accion, IReadOnlyList<CambioAuditoria> Cambios);
public sealed record ActividadAuditoria(int Id, DateTime Fecha, string Responsable, string Modulo,
    string Titulo, string Resultado, string Resumen, IReadOnlyList<DetalleAuditoria> Detalles);

/// <summary>Presenta la bitácora sin alterar las filas originales ni exponer sus datos técnicos.</summary>
public static class AuditoriaPresentacion
{
    public static readonly string[] Modulos = ["Accesos", "Caja", "Clientes", "Compras", "Cotizaciones",
        "Inventario", "Productos", "Proveedores", "Servicios", "Usuarios y roles", "Ventas", "Otros"];
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-PE");
    private static readonly Dictionary<string, string> Entidades = new(StringComparer.OrdinalIgnoreCase) {
        ["Compra"]="Compra", ["DetalleCompra"]="Producto comprado", ["PagoCompra"]="Pago de compra",
        ["Venta"]="Venta", ["DetalleVenta"]="Detalle de venta", ["PagoVenta"]="Cobro de venta",
        ["Caja"]="Caja", ["MovimientoCaja"]="Movimiento de dinero", ["Inventario"]="Inventario",
        ["MovimientoStock"]="Movimiento de existencias", ["AlertaStock"]="Alerta de existencias",
        ["Cotizacion"]="Cotización", ["DetalleCotizacion"]="Detalle de cotización", ["EnvioCorreo"]="Envío de correo",
        ["Producto"]="Producto", ["ElementoCatalogo"]="Artículo del catálogo", ["Servicio"]="Servicio",
        ["ImagenElementoCatalogo"]="Imagen del catálogo", ["Cliente"]="Cliente",
        ["ClientePersonaNatural"]="Datos personales del cliente", ["ClienteEmpresa"]="Empresa cliente",
        ["ContactoCliente"]="Contacto del cliente", ["Proveedor"]="Proveedor", ["ContactoProveedor"]="Contacto del proveedor",
        ["Usuario"]="Cuenta de usuario", ["PersonalInterno"]="Personal interno", ["Rol"]="Rol de acceso",
        ["RolPermiso"]="Permiso del rol", ["Comprobante"]="Comprobante", ["SerieComprobante"]="Serie de comprobantes",
        ["Categoria"]="Categoría", ["Marca"]="Marca", ["UnidadMedida"]="Unidad de medida",
        ["DocumentoLegal"]="Documento legal", ["AceptacionDocumentoLegal"]="Aceptación de condiciones",
        ["Empresa"]="Datos de la empresa", ["ConsultaExterna"]="Consulta de documento"
    };
    private static readonly Dictionary<string, string> Campos = new(StringComparer.OrdinalIgnoreCase) {
        ["Nombre"]="Nombre", ["Nombres"]="Nombres", ["ApellidoPaterno"]="Apellido paterno", ["ApellidoMaterno"]="Apellido materno",
        ["RazonSocial"]="Razón social", ["NombreComercial"]="Nombre comercial", ["Correo"]="Correo", ["Telefono"]="Teléfono",
        ["Direccion"]="Dirección", ["NumeroDocumento"]="Documento", ["Ruc"]="RUC", ["Codigo"]="Código",
        ["Estado"]="Acceso / disponibilidad", ["Observacion"]="Observación", ["Observaciones"]="Observaciones",
        ["Motivo"]="Motivo", ["MotivoAnulacion"]="Motivo de anulación", ["Cargo"]="Cargo", ["Descripcion"]="Descripción",
        ["Monto"]="Importe", ["Total"]="Total", ["TotalReferencial"]="Total referencial", ["Subtotal"]="Subtotal",
        ["Descuento"]="Descuento", ["Igv"]="IGV", ["PrecioReferencial"]="Precio referencial", ["PrecioUnitario"]="Precio unitario",
        ["CostoUnitario"]="Costo unitario", ["SaldoInicial"]="Saldo inicial", ["SaldoEsperado"]="Saldo esperado",
        ["SaldoFinal"]="Saldo final", ["SaldoContado"]="Saldo contado", ["Diferencia"]="Diferencia",
        ["TotalIngresos"]="Total de ingresos", ["TotalEgresos"]="Total de egresos", ["MontoPagado"]="Importe pagado",
        ["SaldoPendiente"]="Saldo pendiente", ["Cantidad"]="Cantidad", ["StockActual"]="Existencias", ["StockMinimo"]="Mínimo de existencias",
        ["StockAnterior"]="Existencias anteriores", ["StockNuevo"]="Existencias resultantes", ["Serie"]="Serie", ["Numero"]="Número",
        ["MetodoPago"]="Método de pago", ["EstadoPago"]="Estado del pago", ["EstadoCobro"]="Estado del cobro",
        ["FechaApertura"]="Apertura", ["FechaCierre"]="Cierre", ["FechaVencimiento"]="Vencimiento",
        ["FechaMovimiento"]="Fecha del movimiento", ["FechaPago"]="Fecha del pago", ["FechaAnulacion"]="Fecha de anulación",
        ["ImagenUrl"]="Imagen", ["UrlImagen"]="Imagen", ["FichaTecnicaUrl"]="Ficha técnica", ["ArchivoPdf"]="Documento PDF",
        ["CorreoEnviado"]="Correo enviado", ["OrigenCotizacion"]="Origen", ["RequiereVisitaTecnica"]="Requiere visita técnica",
        ["IdEstadoCompra"]="Estado de compra", ["IdEstadoVenta"]="Estado de venta", ["IdEstadoCaja"]="Estado de caja",
        ["IdEstadoCotizacion"]="Estado de cotización", ["IdEstadoComprobante"]="Estado del comprobante",
        ["IdRol"]="Rol", ["IdPermiso"]="Permiso", ["IdCategoria"]="Categoría", ["IdMarca"]="Marca",
        ["IdUnidadMedida"]="Unidad de medida", ["IdTipoComprobante"]="Tipo de comprobante",
        ["IdTipoMovimientoCaja"]="Movimiento de dinero", ["IdTipoMovimientoStock"]="Movimiento de existencias",
        ["IdProducto"]="Producto", ["IdServicio"]="Servicio", ["IdCliente"]="Cliente", ["IdProveedor"]="Proveedor",
        ["IdCompra"]="Compra", ["IdVenta"]="Venta", ["IdCotizacion"]="Cotización", ["IdCaja"]="Caja",
        ["IdElementoCatalogo"]="Artículo", ["CodigoProducto"]="Código del producto", ["FichaTecnicaPdf"]="Ficha técnica",
        ["SaldoSistema"]="Saldo esperado", ["TipoPago"]="Condición de pago", ["NumeroCuotas"]="Número de cuotas",
        ["NumeroComprobante"]="Número de comprobante", ["SerieComprobante"]="Serie del comprobante",
        ["TipoComprobanteProveedor"]="Comprobante del proveedor", ["MontoTotal"]="Importe total",
        ["MontoInicial"]="Pago inicial", ["EsIngreso"]="Es un ingreso", ["FechaEmisionComprobante"]="Emisión del comprobante"
    };
    private static readonly HashSet<string> Dinero = new(StringComparer.OrdinalIgnoreCase) {
        "Monto","Total","TotalReferencial","Subtotal","Descuento","Igv","PrecioReferencial","PrecioUnitario","CostoUnitario",
        "SaldoInicial","SaldoEsperado","SaldoFinal","SaldoContado","SaldoSistema","MontoTotal","MontoInicial","Diferencia","TotalIngresos","TotalEgresos","MontoPagado","SaldoPendiente"
    };

    public static ActividadAuditoria Crear(IReadOnlyList<FilaAuditoria> filas, IReadOnlyDictionary<string,string> referencias)
    {
        var principal = filas.FirstOrDefault(f => f.Tabla == "API") ?? filas[0];
        var resultado = filas.Any(f => f.Accion == "DENEGADA") ? "denegado"
            : filas.Any(f => f.Accion == "ERROR") ? "error" : "completado";
        var operacion = principal.Descripcion.Split(' ')[0];
        var accion = operacion.Contains('.') ? operacion[(operacion.IndexOf('.') + 1)..] : principal.Accion;
        var modulo = principal.Modulo;
        var objeto = modulo switch { "Compras"=>"una compra", "Ventas"=>"una venta", "Cotizaciones"=>"una cotización",
            "Productos"=>"un producto", "Servicios"=>"un servicio", "Clientes"=>"un cliente", "Proveedores"=>"un proveedor",
            "Usuarios y roles"=>"los accesos del personal", "Inventario"=>"el inventario", "Caja"=>"la caja", _=>"información del sistema" };
        var (pasado, infinitivo) = accion.ToLowerInvariant() switch {
            "login" => ("Inició sesión", "iniciar sesión"), "registrarcliente" or "registrarse" => ("Registró una cuenta de cliente", "registrar una cuenta"),
            "abrircaja" => ("Abrió la caja", "abrir la caja"), "cerrarcaja" => ("Cerró la caja", "cerrar la caja"),
            "registrarmovimientomanual" => ("Registró un movimiento de dinero", "registrar un movimiento de dinero"),
            "registrarpago" => (modulo == "Ventas" ? "Registró un cobro de venta" : "Registró un pago de compra", "registrar un pago"),
            "anular" or "cancelar" => ($"Anuló {objeto}", $"anular {objeto}"),
            "enviarcorreo" => ("Envió la cotización por correo", "enviar la cotización por correo"),
            "enviarwhatsapp" => ("Preparó la cotización para WhatsApp", "preparar la cotización para WhatsApp"),
            "cambiarcontrasena" => ("Cambió una contraseña", "cambiar una contraseña"),
            "asignarpermisos" => ("Cambió los permisos de un rol", "cambiar los permisos de un rol"),
            "desactivar" or "eliminar" => ($"Dio de baja {objeto}", $"dar de baja {objeto}"),
            "activar" => ($"Activó {objeto}", $"activar {objeto}"),
            "crear" or "registrar" or "registrarcotizacion" => ($"Registró {objeto}", $"registrar {objeto}"),
            "responder" or "atender" => ("Respondió una cotización", "responder una cotización"),
            "actualizar" or "cambiarestado" or "actualizarstockminimo" => ($"Actualizó {objeto}", $"actualizar {objeto}"),
            _ => ($"Realizó una operación en {modulo.ToLowerInvariant()}", $"realizar una operación en {modulo.ToLowerInvariant()}")
        };
        var detalles = filas.Where(f => f.Tabla != "API").Select(f => Detalle(f, referencias)).ToArray();
        var responsable = filas.Select(f => f.Responsable).FirstOrDefault(r => !string.IsNullOrWhiteSpace(r))
            ?? (principal.Origen == "SQL" ? "Actividad directa en la base de datos" : "Usuario sin identificar");
        var resumen = resultado switch {
            "denegado" => "El sistema no autorizó esta acción.",
            "error" => detalles.Length > 0 ? "La operación informó un problema. Revisa los cambios que sí quedaron registrados." : "La operación no se completó. No hay cambios registrados para este intento.",
            _ => detalles.Length > 0 ? $"{detalles.Length} {(detalles.Length == 1 ? "registro con cambios" : "registros con cambios")}. Puedes revisar el detalle." : "La acción se completó; no se registraron cambios adicionales."
        };
        return new(filas.Max(f=>f.IdLog), filas.Max(f=>f.Fecha), responsable, modulo,
            resultado == "completado" ? pasado : (resultado == "denegado" ? "Acceso denegado: " : "No se completó: ") + infinitivo,
            resultado, resumen, detalles);
    }

    private static DetalleAuditoria Detalle(FilaAuditoria fila, IReadOnlyDictionary<string,string> refs)
    {
        var antes = Leer(fila.Antes); var despues = Leer(fila.Despues);
        var valores = despues.Count > 0 ? despues : antes;
        var nombre = new[] { "Nombre", "RazonSocial", "Correo", "Codigo", "CodigoProducto" }.Select(k=>valores.GetValueOrDefault(k)).FirstOrDefault(v=>!string.IsNullOrWhiteSpace(v));
        if (nombre == null && fila.Registro != null) refs.TryGetValue("Id"+fila.Tabla+":"+fila.Registro, out nombre);
        var etiqueta = Entidades.GetValueOrDefault(fila.Tabla, "Registro del sistema");
        var registro = etiqueta + (nombre != null ? $": {nombre}" : fila.Registro != null ? $" n.º {fila.Registro}" : "");
        var cambios = new List<CambioAuditoria>();
        foreach (var campo in antes.Keys.Union(despues.Keys, StringComparer.OrdinalIgnoreCase))
        {
            if (!Campos.TryGetValue(campo, out var label) || campo.Equals("Id"+fila.Tabla, StringComparison.OrdinalIgnoreCase)) continue;
            var a=antes.GetValueOrDefault(campo); var d=despues.GetValueOrDefault(campo);
            if (a == d) continue;
            if (campo is "ImagenUrl" or "UrlImagen" or "FichaTecnicaUrl" or "FichaTecnicaPdf" or "ArchivoPdf")
                cambios.Add(new(label, string.IsNullOrWhiteSpace(a)?"Sin archivo":"Archivo anterior", string.IsNullOrWhiteSpace(d)?"Sin archivo":"Archivo registrado o reemplazado"));
            else cambios.Add(new(label, Valor(campo, a, refs), Valor(campo, d, refs)));
        }
        return new(registro, fila.Accion switch { "CREAR"=>"Registrado", "ELIMINAR"=>"Retirado", _=>"Actualizado" }, cambios);
    }

    private static Dictionary<string,string?> Leer(string? json)
    {
        var valores=new Dictionary<string,string?>(StringComparer.OrdinalIgnoreCase);
        try {
            using var doc=JsonDocument.Parse(json ?? "{}");
            if(doc.RootElement.ValueKind != JsonValueKind.Object) return valores;
            foreach(var p in doc.RootElement.EnumerateObject())
                valores[p.Name]=p.Value.ValueKind == JsonValueKind.Null ? null : p.Value.ToString();
        } catch(JsonException) { /* Los registros antiguos conservan su resumen. */ }
        return valores;
    }

    private static string Valor(string campo, string? valor, IReadOnlyDictionary<string,string> refs)
    {
        if (string.IsNullOrWhiteSpace(valor)) return "Sin dato";
        if (campo.StartsWith("Id",StringComparison.OrdinalIgnoreCase)) {
            if(refs.TryGetValue(campo+":"+valor,out var texto)) return texto;
            if(campo is "IdCompra" or "IdVenta" or "IdCotizacion" or "IdCaja") return "N.º " + valor;
            return (Campos.GetValueOrDefault(campo,"Registro")) + " n.º " + valor;
        }
        if(campo is "ImagenUrl" or "UrlImagen" or "FichaTecnicaUrl" or "ArchivoPdf") return "Archivo registrado";
        if(campo == "Estado") return valor.ToLowerInvariant() is "true" or "1" ? "Activo" : "Inactivo";
        if(bool.TryParse(valor,out var booleano)) return booleano ? "Sí" : "No";
        if(Dinero.Contains(campo) && decimal.TryParse(valor,NumberStyles.Number,CultureInfo.InvariantCulture,out var monto)) return "S/ " + monto.ToString("N2",Cultura);
        if(campo.StartsWith("Fecha") && DateTime.TryParse(valor,CultureInfo.InvariantCulture,DateTimeStyles.None,out var fecha)) return fecha.ToString("dd/MM/yyyy HH:mm",Cultura);
        return valor;
    }
}
