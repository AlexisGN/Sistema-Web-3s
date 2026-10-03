using System.ComponentModel.DataAnnotations;

namespace Sistema3S.Web.DTOs.ClienteWeb;

public sealed class ClientePerfilEditarDto
{
    [Required, RegularExpression(@"^\+51[0-9]{9}$", ErrorMessage = "Ingresa un teléfono peruano de 9 dígitos.")]
    public string Telefono { get; set; } = "";
    [StringLength(250)] public string? Direccion { get; set; }
    [StringLength(200)] public string? NombreComercial { get; set; }
    [Range(1, int.MaxValue)] public int? IdUbigeo { get; set; }
}
public sealed class ClienteRecuperarDto
{
    [Required, EmailAddress, StringLength(150)] public string Correo { get; set; } = "";
}
public class ClienteNuevaClaveDto
{
    [Required, StringLength(128, MinimumLength = 8)] public string NuevaContrasena { get; set; } = "";
    [Required, Compare(nameof(NuevaContrasena), ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmarContrasena { get; set; } = "";
}
public sealed class ClienteRestablecerDto : ClienteNuevaClaveDto
{
    [Required, RegularExpression("^[A-Fa-f0-9]{64}$")] public string Token { get; set; } = "";
}
public sealed class ClienteCambiarClaveDto : ClienteNuevaClaveDto
{
    [Required, StringLength(128)] public string ContrasenaActual { get; set; } = "";
}
