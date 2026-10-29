namespace Sistema3S.Web.DTOs.Usuario
{
    public class UsuarioListadoDto
    {
        public int TotalPermisos { get; set; }
        public bool EsCliente { get; set; }
        public string NombreCliente { get; set; } = string.Empty;
        public string NumeroDocumento { get; set; } = string.Empty;

        public int IdUsuario { get; set; }

        public int IdRol { get; set; }

        public string Rol { get; set; } = string.Empty;

        public string Correo { get; set; } = string.Empty;

        public bool Estado { get; set; }

        public DateTime FechaRegistro { get; set; }
    }
}