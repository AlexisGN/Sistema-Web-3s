using Sistema3S.Web.Services.Seguridad;
﻿using System.Data;
using Microsoft.EntityFrameworkCore;
using Sistema3S.Web.Data;
using Sistema3S.Web.DTOs.Permiso;
using Sistema3S.Web.DTOs.Rol;
using Sistema3S.Web.Services.Interfaces;

namespace Sistema3S.Web.Services.Implementations
{
    public class RolService : IRolService
    {
        private readonly Bd3sContext _context;

        public RolService(Bd3sContext context)
        {
            _context = context;
        }

        public async Task<List<RolListadoDto>> ListarAsync(bool? soloActivos)
        {
            var roles = new List<RolListadoDto>();

            await using var connection = _context.Database.GetDbConnection();

            if (connection.State != ConnectionState.Open)
            {
                await connection.AbrirAuditadaAsync();
            }

            await using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT
                    r.IdRol,
                    r.Nombre,
                    r.Descripcion,
                    r.Estado,
                    CASE
                        WHEN r.Estado = 1 AND UPPER(LTRIM(RTRIM(r.Nombre))) IN ('ADMINISTRADOR', 'ADMIN')
                            THEN (SELECT COUNT(*) FROM dbo.Permiso WHERE Estado = 1)
                        ELSE COUNT(DISTINCT rp.IdPermiso)
                    END AS TotalPermisos,
                    COUNT(DISTINCT u.IdUsuario) AS TotalUsuarios
                FROM dbo.Rol r
                LEFT JOIN dbo.RolPermiso rp
                    ON rp.IdRol = r.IdRol
                LEFT JOIN dbo.Usuario u
                    ON u.IdRol = r.IdRol
                   AND u.Estado = 1
                WHERE
                    (@SoloActivos IS NULL OR r.Estado = @SoloActivos)
                GROUP BY
                    r.IdRol,
                    r.Nombre,
                    r.Descripcion,
                    r.Estado
                ORDER BY
                    CASE WHEN r.Nombre = 'Administrador' THEN 0 ELSE 1 END,
                    r.Nombre;
            ";

            var paramSoloActivos = command.CreateParameter();
            paramSoloActivos.ParameterName = "@SoloActivos";
            paramSoloActivos.Value = soloActivos.HasValue ? soloActivos.Value : DBNull.Value;
            command.Parameters.Add(paramSoloActivos);

            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                roles.Add(new RolListadoDto
                {
                    IdRol = Convert.ToInt32(reader["IdRol"]),
                    Nombre = Convert.ToString(reader["Nombre"]) ?? string.Empty,
                    Descripcion = reader["Descripcion"] == DBNull.Value
                        ? null
                        : Convert.ToString(reader["Descripcion"]),
                    Estado = Convert.ToBoolean(reader["Estado"]),
                    TotalPermisos = Convert.ToInt32(reader["TotalPermisos"]),
                    TotalUsuarios = Convert.ToInt32(reader["TotalUsuarios"])
                });
            }

            return roles;
        }
    }
}
