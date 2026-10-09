using System.Data.Common;
using System.Text.RegularExpressions;
using Sistema3S.Web.DTOs.Permiso;

namespace Sistema3S.Web.Services.Seguridad;

// Reglas de configuración del rol; no concede permisos ni sustituye la autorización existente.
public static class ReglasSeleccionPermisos
{
    public const string MinimoModuloVisible = "El rol debe tener al menos un módulo habilitado para visualización.";

    public static string? BaseRequerida(string nombre)
    {
        var codigo = nombre.Trim().ToUpperInvariant();
        var granular = Regex.Match(codigo, @"^([A-Z][A-Z0-9]*)_([A-Z][A-Z0-9_]*)$");
        if (granular.Success)
            return granular.Groups[2].Value == "VER" ? null : granular.Groups[1].Value + "_VER";

        // Compatibilidad con los permisos 'Gestionar ...' que ya existen en el catálogo.
        var heredado = Regex.Match(codigo, @"^GESTIONAR ([A-Z][A-Z0-9]*)$");
        return heredado.Success ? heredado.Groups[1].Value + "_VER" : null;
    }

    public static bool EsVisualizacionFuncional(string nombre)
    {
        var codigo = nombre.Trim().ToUpperInvariant();
        return codigo != "INICIO_VER" && Regex.IsMatch(codigo, @"^[A-Z][A-Z0-9]*_VER$");
    }

    public static void Validar(IEnumerable<PermisoDto> catalogo, IEnumerable<int> seleccion)
    {
        var disponibles = catalogo.Where(p => p.Estado).ToDictionary(p => p.IdPermiso);
        var ids = seleccion.ToHashSet();
        if (ids.Any(id => !disponibles.ContainsKey(id)))
            throw new InvalidOperationException("Uno o más permisos seleccionados no existen o están inactivos.");

        var nombres = ids.Select(id => disponibles[id].Nombre.Trim().ToUpperInvariant()).ToHashSet();
        foreach (var nombre in nombres)
        {
            var requerido = BaseRequerida(nombre);
            if (requerido != null && !nombres.Contains(requerido))
                throw new InvalidOperationException($"Debe activar {requerido} antes de asignar otros permisos del módulo.");
        }
        if (!nombres.Any(EsVisualizacionFuncional))
            throw new InvalidOperationException(MinimoModuloVisible);
    }

    public static async Task<List<PermisoDto>> LeerCatalogoAsync(DbConnection connection, DbTransaction transaction)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT IdPermiso, Nombre, Estado FROM dbo.Permiso WHERE Estado=1;";
        var permisos = new List<PermisoDto>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            permisos.Add(new PermisoDto { IdPermiso = reader.GetInt32(0), Nombre = reader.GetString(1), Estado = reader.GetBoolean(2) });
        return permisos;
    }

    public static async Task ValidarRolAsignableAsync(DbConnection connection, DbTransaction transaction, int idRol)
    {
        var catalogo = await LeerCatalogoAsync(connection, transaction);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT IdPermiso FROM dbo.RolPermiso WHERE IdRol=@IdRol;";
        var parameter = command.CreateParameter(); parameter.ParameterName = "@IdRol"; parameter.Value = idRol;
        command.Parameters.Add(parameter);
        var ids = new List<int>();
        await using (var reader = await command.ExecuteReaderAsync())
            while (await reader.ReadAsync()) ids.Add(reader.GetInt32(0));
        // Los permisos inactivos ya no dan acceso; se valida la configuración efectiva actual.
        Validar(catalogo, ids.Where(id => catalogo.Any(p => p.IdPermiso == id)));
    }
}
