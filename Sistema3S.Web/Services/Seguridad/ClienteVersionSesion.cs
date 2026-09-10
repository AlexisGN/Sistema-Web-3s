using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Sistema3S.Web.Data;

namespace Sistema3S.Web.Services.Seguridad;

// No expone el hash de contraseña. Cambiarlo invalida los JWT emitidos previamente.
public static class ClienteVersionSesion
{
    public static string Crear(string hash, IConfiguration configuration) => Convert.ToHexString(
        HMACSHA256.HashData(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!), Encoding.UTF8.GetBytes("cliente:" + hash)));

    public static async Task ValidarAsync(TokenValidatedContext ctx)
    {
        var principal = ctx.Principal!;
        if (!principal.HasClaim(c => c.Type == "idCliente")) return;
        if (!int.TryParse(principal.FindFirst("idUsuario")?.Value, out var id) ||
            !int.TryParse(principal.FindFirst("idCliente")?.Value, out var clienteId)) { ctx.Fail("Sesión inválida."); return; }
        var db = ctx.HttpContext.RequestServices.GetRequiredService<Bd3sContext>();
        var config = ctx.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var hash = await db.Usuario.AsNoTracking().Where(u => u.IdUsuario == id && u.Estado &&
            u.IdRolNavigation.Estado && u.IdRolNavigation.Nombre.ToUpper() == "CLIENTE" &&
            u.Cliente != null && u.Cliente.Estado && u.Cliente.IdCliente == clienteId)
            .Select(u => u.ContrasenaHash).FirstOrDefaultAsync(ctx.HttpContext.RequestAborted);
        var stamp = principal.FindFirst("clienteVersion")?.Value;
        if (hash == null || stamp == null || !CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(stamp), Encoding.UTF8.GetBytes(Crear(hash, config))))
            ctx.Fail("Tu sesión cambió. Inicia sesión nuevamente.");
    }
}
