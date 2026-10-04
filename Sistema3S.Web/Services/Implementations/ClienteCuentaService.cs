using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Sistema3S.Web.Data;
using Sistema3S.Web.DTOs.ClienteWeb;
using Sistema3S.Web.Models;
using Sistema3S.Web.Services.Interfaces;
using Sistema3S.Web.Services.Seguridad;

namespace Sistema3S.Web.Services.Implementations;

public sealed class ClienteCuentaService(Bd3sContext db, PasswordHashService passwords,
    IConfiguration config, IEmailService email)
{
    private IQueryable<Usuario> UsuariosCliente => db.Usuario.Where(u => u.Estado && u.IdRolNavigation.Estado &&
        u.IdRolNavigation.Nombre.ToUpper() == "CLIENTE" && u.Cliente != null && u.Cliente.Estado);

    public async Task<object> PerfilAsync(int usuario, int cliente)
    {
        var c = await db.Cliente.AsNoTracking().Include(c => c.ClienteEmpresa).Include(c => c.ClientePersonaNatural)
            .Include(c => c.IdTipoDocumentoNavigation).Include(c => c.IdUbigeoNavigation)
            .FirstOrDefaultAsync(c => c.IdCliente == cliente && c.IdUsuario == usuario && c.Estado)
            ?? throw new InvalidOperationException("No se encontró tu perfil.");
        var correo = await UsuariosCliente.Where(u => u.IdUsuario == usuario).Select(u => u.Correo).SingleAsync();
        return new { c.IdCliente, Correo = correo, TipoDocumento = c.IdTipoDocumentoNavigation.Nombre,
            c.NumeroDocumento, NombreCliente = c.ClienteEmpresa?.RazonSocial ??
                string.Join(" ", new[] { c.ClientePersonaNatural?.Nombres, c.ClientePersonaNatural?.ApellidoPaterno, c.ClientePersonaNatural?.ApellidoMaterno }.Where(s => !string.IsNullOrWhiteSpace(s))),
            EsEmpresa = c.ClienteEmpresa != null, c.Telefono, c.Direccion, c.IdUbigeo,
            NombreComercial = c.ClienteEmpresa?.NombreComercial,
            Ubicacion = c.IdUbigeoNavigation == null ? "" : $"{c.IdUbigeoNavigation.Departamento} / {c.IdUbigeoNavigation.Provincia} / {c.IdUbigeoNavigation.Distrito}" };
    }

    public async Task<object> EditarPerfilAsync(int usuario, int cliente, ClientePerfilEditarDto dto)
    {
        var c = await db.Cliente.Include(c => c.ClienteEmpresa).FirstOrDefaultAsync(c => c.IdCliente == cliente && c.IdUsuario == usuario && c.Estado)
            ?? throw new InvalidOperationException("No se encontró tu perfil.");
        if (dto.IdUbigeo.HasValue && !await db.Ubigeo.AnyAsync(u => u.IdUbigeo == dto.IdUbigeo))
            throw new InvalidOperationException("Selecciona una ubicación válida.");
        // Lista explícita: identidad, correo de acceso, usuario y estado nunca se editan desde aquí.
        c.Telefono = dto.Telefono.Trim(); c.Direccion = dto.Direccion?.Trim(); c.IdUbigeo = dto.IdUbigeo;
        if (c.ClienteEmpresa != null) c.ClienteEmpresa.NombreComercial = dto.NombreComercial?.Trim();
        await db.SaveChangesAsync();
        return await PerfilAsync(usuario, cliente);
    }

    public async Task<object> UbigeosAsync(string q) => await db.Ubigeo.AsNoTracking()
        .Where(u => q.Length >= 2 && (u.Distrito.Contains(q) || u.Provincia.Contains(q) || u.Departamento.Contains(q) || (u.CodigoUbigeo != null && u.CodigoUbigeo.Contains(q))))
        .OrderBy(u => u.Departamento).ThenBy(u => u.Provincia).ThenBy(u => u.Distrito).Take(20)
        .Select(u => new { u.IdUbigeo, Nombre = u.Departamento + " / " + u.Provincia + " / " + u.Distrito }).ToListAsync();

    public Uri UrlRecuperacion()
    {
        if (!Uri.TryCreate(config["ClienteWeb:UrlPublica"], UriKind.Absolute, out var url) ||
            (url.Scheme != "https" && !(url.IsLoopback && url.Scheme == "http")) || !string.IsNullOrEmpty(url.Query) || !string.IsNullOrEmpty(url.Fragment) || !string.IsNullOrEmpty(url.UserInfo))
            throw new InvalidOperationException("La recuperación no está disponible por el momento. Contacta al equipo 3S.");
        return new Uri(url.AbsoluteUri.TrimEnd('/') + "/cliente/restablecer");
    }

    public async Task VerificarRecuperacionAsync(CancellationToken ct)
    {
        UrlRecuperacion();
        // Comprobación global, independiente del correo: no permite enumerar cuentas.
        if (new[] { "Host", "User", "Password", "From" }.Any(key => string.IsNullOrWhiteSpace(config["Smtp:" + key])) ||
            !int.TryParse(config["Smtp:Port"], out var port) || port is < 1 or > 65535)
            throw new InvalidOperationException("Falta completar la configuración SMTP del servidor.");
        await using var con = new SqlConnection(db.Database.GetConnectionString());
        await con.OpenAsync(ct);
        await ConexionAuditada.PrepararAsync(con, ct);
        await using var cmd = new SqlCommand("SELECT CASE WHEN OBJECT_ID(N'dbo.ClienteRecuperacion', N'U') IS NULL THEN 0 ELSE 1 END", con);
        cmd.CommandTimeout = 5;
        if (Convert.ToInt32(await cmd.ExecuteScalarAsync(ct)) != 1)
            throw new InvalidOperationException("Falta ejecutar Database/20261001_ClienteRecuperacion.sql en la base del sistema.");
    }

    public async Task EnviarRecuperacionAsync(string correo, CancellationToken ct)
    {
        var url = UrlRecuperacion();
        var u = await UsuariosCliente.AsNoTracking().FirstOrDefaultAsync(u => u.Correo.ToLower() == correo, ct);
        if (u == null) return;
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var tokenHash = HashToken(token);
        var version = ClienteVersionSesion.Crear(u.ContrasenaHash, config);
        await using var con = new SqlConnection(db.Database.GetConnectionString()); await con.OpenAsync(ct);
        await ConexionAuditada.PrepararAsync(con, ct);
        // La exclusión por usuario también limita correos si hay varias instancias del servidor.
        await using (var command = new SqlCommand("""
            SET XACT_ABORT ON; BEGIN TRANSACTION;
            DECLARE @lock int;
            EXEC @lock = sys.sp_getapplock @Resource=@recurso, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=5000;
            IF @lock < 0 BEGIN ROLLBACK; THROW 50001, 'No se pudo reservar la solicitud.', 1; END;
            IF EXISTS (SELECT 1 FROM dbo.ClienteRecuperacion WHERE IdUsuario=@id AND CreadoUtc>DATEADD(MINUTE,-2,SYSUTCDATETIME()))
            BEGIN COMMIT; SELECT 0; RETURN; END;
            DELETE FROM dbo.ClienteRecuperacion WHERE IdUsuario=@id OR ExpiraUtc<DATEADD(DAY,-1,SYSUTCDATETIME());
            INSERT dbo.ClienteRecuperacion(TokenHash,IdUsuario,VersionClave,CreadoUtc,ExpiraUtc)
            VALUES(@token,@id,@version,SYSUTCDATETIME(),DATEADD(MINUTE,30,SYSUTCDATETIME()));
            COMMIT; SELECT 1;
            """, con))
        {
            command.Parameters.AddWithValue("@recurso", "RecuperacionCliente:" + u.IdUsuario);
            command.Parameters.AddWithValue("@id", u.IdUsuario); command.Parameters.AddWithValue("@token", tokenHash);
            command.Parameters.AddWithValue("@version", version);
            if (Convert.ToInt32(await command.ExecuteScalarAsync(ct)) != 1) return;
        }
        // El fragmento no se envía al servidor web ni en Referer. Sólo viaja al backend en el POST de confirmación.
        var enlace = url.AbsoluteUri + "#token=" + token;
        try
        {
            await email.EnviarConAdjuntoAsync(u.Correo, "Restablece tu contraseña | Sistema 3S",
                $"Hola,\n\nRecibimos una solicitud para restablecer tu contraseña de cliente 3S.\n\nAbre este enlace para crear una nueva contraseña:\n{enlace}\n\nEl enlace vence en 30 minutos y puede usarse una sola vez.\nSi no lo solicitaste, ignora este correo; tu contraseña no cambiará.\n\nEquipo 3S", null);
        }
        catch
        {
            await using var cleanup = new SqlCommand("DELETE dbo.ClienteRecuperacion WHERE TokenHash=@token", con);
            cleanup.Parameters.AddWithValue("@token", tokenHash); await cleanup.ExecuteNonQueryAsync(ct); throw;
        }
    }

    public async Task RestablecerAsync(ClienteRestablecerDto dto)
    {
        ValidarClave(dto.NuevaContrasena, dto.ConfirmarContrasena);
        var nuevoHash = passwords.CrearHash(dto.NuevaContrasena);
        await using var con = new SqlConnection(db.Database.GetConnectionString()); await con.OpenAsync();
        await ConexionAuditada.PrepararAsync(con);
        await using var tx = (SqlTransaction)await con.BeginTransactionAsync(IsolationLevel.Serializable);
        int id; string version; string actual;
        await using (var cmd = new SqlCommand("""
            SELECT r.IdUsuario,r.VersionClave,u.ContrasenaHash FROM dbo.ClienteRecuperacion r WITH(UPDLOCK,HOLDLOCK)
            JOIN dbo.Usuario u WITH(UPDLOCK,HOLDLOCK) ON u.IdUsuario=r.IdUsuario
            JOIN dbo.Rol rol ON rol.IdRol=u.IdRol JOIN dbo.Cliente c ON c.IdUsuario=u.IdUsuario
            WHERE r.TokenHash=@token AND r.ExpiraUtc>SYSUTCDATETIME() AND u.Estado=1 AND c.Estado=1
            AND rol.Estado=1 AND UPPER(rol.Nombre)='CLIENTE'
            """, con, tx))
        {
            cmd.Parameters.AddWithValue("@token", HashToken(dto.Token));
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) throw new InvalidOperationException("El enlace es inválido o ha vencido. Solicita uno nuevo.");
            id=reader.GetInt32(0); version=reader.GetString(1); actual=reader.GetString(2);
        }
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(version), Encoding.UTF8.GetBytes(ClienteVersionSesion.Crear(actual,config))))
            throw new InvalidOperationException("El enlace ya no es válido. Solicita uno nuevo.");
        await using var update = new SqlCommand("UPDATE dbo.Usuario SET ContrasenaHash=@hash WHERE IdUsuario=@id; DELETE dbo.ClienteRecuperacion WHERE IdUsuario=@id;",con,tx);
        update.Parameters.AddWithValue("@hash",nuevoHash); update.Parameters.AddWithValue("@id",id);
        await update.ExecuteNonQueryAsync(); await tx.CommitAsync();
    }

    public async Task CambiarClaveAsync(int usuario, ClienteCambiarClaveDto dto)
    {
        ValidarClave(dto.NuevaContrasena,dto.ConfirmarContrasena);
        var u = await UsuariosCliente.AsNoTracking().FirstOrDefaultAsync(u => u.IdUsuario == usuario)
            ?? throw new InvalidOperationException("Inicia sesión nuevamente.");
        if (!passwords.Verificar(dto.ContrasenaActual,u.ContrasenaHash)) throw new InvalidOperationException("La contraseña actual no es correcta.");
        var hash = passwords.CrearHash(dto.NuevaContrasena);
        // Compare-and-swap evita sobrescribir un cambio de contraseña simultáneo.
        var filas = await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.Usuario SET ContrasenaHash={hash} WHERE IdUsuario={usuario} AND ContrasenaHash={u.ContrasenaHash}");
        if (filas != 1) throw new InvalidOperationException("La cuenta cambió. Inicia sesión nuevamente.");
    }
    public static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token.ToUpperInvariant())));
    public static void ValidarClave(string nueva,string confirmar)
    {
        if (string.IsNullOrWhiteSpace(nueva) || nueva.Length is < 8 or > 128) throw new InvalidOperationException("Usa entre 8 y 128 caracteres.");
        if (nueva != confirmar) throw new InvalidOperationException("Las contraseñas no coinciden.");
    }
}
