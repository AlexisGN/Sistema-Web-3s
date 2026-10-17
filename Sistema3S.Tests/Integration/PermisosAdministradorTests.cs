using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sistema3S.Tests.Fixtures;
using Sistema3S.Web.DTOs.Permiso;
using Sistema3S.Web.DTOs.Rol;

namespace Sistema3S.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class PermisosAdministradorTests
{
    [Fact(DisplayName = "PS-ADMIN-001 | Administrador muestra todo el catálogo activo, incluido BI y permisos futuros, sin ampliar otros roles")]
    public async Task CatalogoCompleto()
    {
        await using var db = await QaDatabase.CreateAsync();
        await BiTests.Migrar(db);
        await db.Exec("INSERT dbo.Permiso(Nombre,Descripcion,Estado) VALUES(N'FUTURO_VER',N'Módulo de prueba QA',1),(N'INACTIVO_VER',N'Permiso inactivo QA',0)");
        var asignaciones = await db.Scalar("SELECT COUNT(*) FROM dbo.RolPermiso WHERE IdRol=1");
        var activos = await db.Scalar("SELECT COUNT(*) FROM dbo.Permiso WHERE Estado=1");
        await using var api = await ApiHost.Start(db);
        await ApiTests.Login(api);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var admin = JsonSerializer.Deserialize<List<PermisoDto>>(await api.Http.GetStringAsync("/api/rol/1/permisos"), options)!;
        Assert.Equal((int)activos, admin.Count);
        Assert.All(admin, p => Assert.True(p.Estado && p.Asignado));
        foreach (var nombre in new[] { "BI_VER", "BI_EXPORTAR", "BI_REFERENCIA", "FUTURO_VER" })
            Assert.Contains(admin, p => p.Nombre == nombre && p.Asignado);
        Assert.DoesNotContain(admin, p => p.Nombre == "INACTIVO_VER");
        var roles = JsonSerializer.Deserialize<List<RolListadoDto>>(await api.Http.GetStringAsync("/api/rol"), options)!;
        Assert.Equal((int)activos, roles.Single(r => r.IdRol == 1).TotalPermisos);
        Assert.Equal(asignaciones, await db.Scalar("SELECT COUNT(*) FROM dbo.RolPermiso WHERE IdRol=1"));
        Assert.Equal(HttpStatusCode.BadRequest,
            (await api.Http.PutAsJsonAsync("/api/rol/1/permisos", new { idsPermisos = Array.Empty<int>() })).StatusCode);
        var otro = JsonSerializer.Deserialize<List<PermisoDto>>(await api.Http.GetStringAsync("/api/rol/2/permisos"), options)!;
        Assert.Equal(await db.Scalar("SELECT COUNT(*) FROM dbo.RolPermiso rp JOIN dbo.Permiso p ON p.IdPermiso=rp.IdPermiso WHERE rp.IdRol=2 AND p.Estado=1"), otro.Count(p => p.Asignado));
        Assert.All(otro.Where(p => p.Nombre.StartsWith("BI_") || p.Nombre == "FUTURO_VER"), p => Assert.False(p.Asignado));
        await ApiTests.Login(api, "compras@example.test");
        Assert.Equal(HttpStatusCode.Forbidden,
            (await api.Http.GetAsync("/api/bi/resumen?desde=2026-04-13&hasta=2026-10-13&procedencia=referencia")).StatusCode);
    }
}
