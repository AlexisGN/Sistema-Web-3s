namespace Sistema3S.Web.DTOs.ClienteWeb
{
    public class ClienteWebRegistroDto
    {
        public string TipoDocumento { get; set; } = string.Empty;
        public string NumeroDocumento { get; set; } = string.Empty;

        public string Correo { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string? Direccion { get; set; }

        public string Contrasena { get; set; } = string.Empty;
        public string ConfirmarContrasena { get; set; } = string.Empty;

        public string? Nombres { get; set; }
        public string? ApellidoPaterno { get; set; }
        public string? ApellidoMaterno { get; set; }

        public string? RazonSocial { get; set; }
        public string? NombreComercial { get; set; }
    }

    public class ClienteWebLoginDto
    {
        public string Correo { get; set; } = string.Empty;
        public string Contrasena { get; set; } = string.Empty;
    }

    public class ClienteWebSesionDto
    {
        public int IdCliente { get; set; }
        public int IdUsuario { get; set; }
        public int IdRol { get; set; }

        public string Correo { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;

        public string TipoDocumento { get; set; } = string.Empty;
        public string NumeroDocumento { get; set; } = string.Empty;
        public string TipoCliente { get; set; } = string.Empty;
        public string NombreCliente { get; set; } = string.Empty;

        public bool EsEmpresa { get; set; }

        public string Token { get; set; } = string.Empty;
        public DateTime Expira { get; set; }

        public string Mensaje { get; set; } = string.Empty;
    }
}
public class ClienteWebConsultaDocumentoDto
{
    public string TipoDocumento { get; set; } = string.Empty;
    public string NumeroDocumento { get; set; } = string.Empty;

    public bool Exitoso { get; set; }
    public bool ClienteYaExiste { get; set; }
    public bool CuentaWebVinculada { get; set; }

    public int? IdClienteExistente { get; set; }

    public string? CorreoExistente { get; set; }
    public string? TelefonoExistente { get; set; }
    public string? DireccionExistente { get; set; }

    public string? Nombres { get; set; }
    public string? ApellidoPaterno { get; set; }
    public string? ApellidoMaterno { get; set; }
    public string? NombreCompleto { get; set; }

    public string? RazonSocial { get; set; }
    public string? NombreComercial { get; set; }

    public string? EstadoSunat { get; set; }
    public string? CondicionSunat { get; set; }

    public string? CodigoUbigeo { get; set; }
    public int? IdUbigeo { get; set; }
    public string? Ubicacion { get; set; }

    public string Mensaje { get; set; } = string.Empty;
}
public class ClienteWebCotizacionCrearDto
{
    public string? ObservacionGeneral { get; set; }
    public List<ClienteWebCotizacionItemCrearDto> Items { get; set; } = new();
}

public class ClienteWebCotizacionItemCrearDto
{
    public int IdProducto { get; set; }
    public int Cantidad { get; set; }
    public string? Observacion { get; set; }
}

public class ClienteWebCotizacionRegistradaDto
{
    public int IdCotizacion { get; set; }
    public string CodigoCotizacion { get; set; } = string.Empty;
    public DateTime FechaCotizacion { get; set; }
    public string EstadoCotizacion { get; set; } = string.Empty;
    public string OrigenCotizacion { get; set; } = string.Empty;

    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal Igv { get; set; }
    public decimal Total { get; set; }

    public bool EsEmpresa { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}

public class ClienteWebCotizacionResumenDto
{
    public int IdCotizacion { get; set; }
    public string CodigoCotizacion { get; set; } = string.Empty;
    public DateTime FechaCotizacion { get; set; }
    public string EstadoCotizacion { get; set; } = string.Empty;
    public string OrigenCotizacion { get; set; } = string.Empty;
    public string? Observacion { get; set; }

    public int CantidadProductos { get; set; }

    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal Igv { get; set; }
    public decimal Total { get; set; }
}

public class ClienteWebCotizacionDetalleResponseDto
{
    public int IdCotizacion { get; set; }
    public string CodigoCotizacion { get; set; } = string.Empty;
    public DateTime FechaCotizacion { get; set; }
    public string EstadoCotizacion { get; set; } = string.Empty;
    public string OrigenCotizacion { get; set; } = string.Empty;
    public string? Observacion { get; set; }

    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal Igv { get; set; }
    public decimal Total { get; set; }

    public List<ClienteWebCotizacionDetalleItemDto> Detalles { get; set; } = new();
}

public class ClienteWebCotizacionDetalleItemDto
{
    public int IdDetalleCotizacion { get; set; }
    public int IdElementoCatalogo { get; set; }
    public int? IdProducto { get; set; }

    public string CodigoProducto { get; set; } = string.Empty;
    public string NombreProducto { get; set; } = string.Empty;
    public string? ImagenUrl { get; set; }

    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }

    public string? Observacion { get; set; }
}