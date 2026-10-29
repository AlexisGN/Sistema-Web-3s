using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Sistema3S.Web.Services.Seguridad;

// Una instancia por solicitud; nunca se obtiene el responsable del cuerpo enviado por el cliente.
public sealed class ContextoAuditoria
{
    public static readonly AsyncLocal<ContextoAuditoria?> Actual = new();
    public int? IdUsuario { get; set; }
    public string Accion { get; set; } = "Operación SQL";
    public string Solicitud { get; set; } = Guid.NewGuid().ToString();
    public string Origen { get; set; } = "Sistema";
}

public static class ConexionAuditada
{
    public static async Task AbrirAuditadaAsync(this DbConnection connection)
    {
        await connection.OpenAsync();
        await PrepararAsync(connection);
    }

    public static async Task PrepararAsync(DbConnection connection, CancellationToken token = default)
    {
        await using var command = CrearComando(connection);
        await command.ExecuteNonQueryAsync(token);
    }

    public static void Preparar(DbConnection connection)
    {
        using var command = CrearComando(connection);
        command.ExecuteNonQuery();
    }

    private static DbCommand CrearComando(DbConnection connection)
    {
        var actual = ContextoAuditoria.Actual.Value;
        var command = connection.CreateCommand();
        command.CommandText = """
            EXEC sys.sp_set_session_context @key=N'IdUsuarioAuditoria', @value=@usuario;
            EXEC sys.sp_set_session_context @key=N'AccionAuditoria', @value=@accion;
            EXEC sys.sp_set_session_context @key=N'SolicitudAuditoria', @value=@solicitud;
            EXEC sys.sp_set_session_context @key=N'OrigenAuditoria', @value=@origen;
            """;
        foreach (var (nombre, valor) in new (string, object?)[] {
            ("@usuario", actual?.IdUsuario), ("@accion", actual?.Accion),
            ("@solicitud", actual?.Solicitud), ("@origen", actual?.Origen) })
        {
            var parametro = command.CreateParameter();
            parametro.ParameterName = nombre;
            parametro.Value = valor ?? DBNull.Value;
            command.Parameters.Add(parametro);
        }
        return command;
    }
}

public sealed class ConexionAuditoriaInterceptor : DbConnectionInterceptor
{
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
        => ConexionAuditada.Preparar(connection);
    public override Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
        => ConexionAuditada.PrepararAsync(connection, cancellationToken);
}
