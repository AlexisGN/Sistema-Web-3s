using System.Net;
using System.Text.Json;
using Sistema3S.Tests.Fixtures;

namespace Sistema3S.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class BiBusquedaTests
{
    private static async Task Preparar(QaDatabase db)
    {
        await BiTests.Migrar(db);
        await db.Exec("""
            UPDATE ClientePersonaNatural SET Nombres=N'Juan Alexis',ApellidoPaterno=N'Guevara',ApellidoMaterno=N'Nolasco' WHERE IdCliente=1;
            UPDATE Proveedor SET RazonSocial=N'Soluciones Industriales del Norte' WHERE IdProveedor=1;
            UPDATE ElementoCatalogo SET Nombre=N'Válvula 100% [QA]_segura' WHERE IdElementoCatalogo=1;
            INSERT bi.LoteReferencia(IdLote,Nombre,Inicio,Fin,Generador,Estado) VALUES('70c9c6e8-0a0b-4ac9-b267-120a26000090',N'Búsqueda QA','20260413','20261013',N'Test aislado','Completo');
            INSERT bi.EntidadReferencia(IdEntidad,Tipo,Nombre,FechaAlta,IdLote) VALUES
              (-901,'cliente',N'Juan Alexis Guevara Nolasco · referencia','20260413','70c9c6e8-0a0b-4ac9-b267-120a26000090'),
              (-902,'proveedor',N'Soluciones Industriales del Norte · referencia','20260413','70c9c6e8-0a0b-4ac9-b267-120a26000090');
            """);
    }

    [Fact(DisplayName="PI-BUS-001 | Búsqueda por palabras separadas, orden libre, tildes y comodines literales")]
    public async Task Palabras()
    {
        await using var db=await QaDatabase.CreateAsync();await Preparar(db);var service=BiTests.Servicio(db);
        foreach(var text in new[]{"Juan Guevara","Juan Nolasco","Nolasco Juan","   juan \t Guevara  ","JUÁN guev","Juan Juan Guevara"})
            Assert.Contains((await service.OpcionesAsync("operativo",text,default))["clientes"],r=>Convert.ToInt32(r["Id"])==1);
        Assert.Empty((await service.OpcionesAsync("operativo","Juan Inexistente",default))["clientes"]);
        Assert.Contains((await service.OpcionesAsync("operativo","Norte Soluciones",default))["proveedores"],r=>Convert.ToInt32(r["Id"])==1);
        Assert.Contains((await service.OpcionesAsync("operativo","valvula segura",default))["elementos"],r=>Convert.ToInt32(r["Id"])==1);
        foreach(var literal in new[]{"100%","[QA]","_"})Assert.Single((await service.OpcionesAsync("operativo",literal,default))["elementos"]);
        var injected=await service.OpcionesAsync("operativo","' OR 1=1 --",default);Assert.All(injected.Values,r=>Assert.Empty(r));
        Assert.Single((await service.OpcionesAsync("operativo"," \t ",default))["clientes"]);
        Assert.Contains((await service.OpcionesAsync("referencia","Juan Guevara",default))["clientes"],r=>Convert.ToInt32(r["Id"])==-901);
        Assert.DoesNotContain((await service.OpcionesAsync("operativo","Juan Guevara",default))["clientes"],r=>Convert.ToInt32(r["Id"])<0);
        Assert.Equal(1,await db.Scalar("SELECT COUNT(*) FROM Cliente"));Assert.Equal(0,await db.Scalar("SELECT StockActual FROM Inventario WHERE IdProducto=1"));
        await Assert.ThrowsAsync<ArgumentException>(()=>service.OpcionesAsync("operativo",new string('a',101),default));
    }

    [Fact(DisplayName="PI-BUS-002 | Endpoint conserva permisos, separa procedencias y devuelve el nombre completo encontrado")]
    public async Task Endpoint()
    {
        await using var db=await QaDatabase.CreateAsync();await Preparar(db);await using var api=await ApiHost.Start(db);
        const string path="/api/bi/opciones?procedencia=operativo&buscar=Juan%20Guevara";
        Assert.Equal(HttpStatusCode.Unauthorized,(await api.Http.GetAsync(path)).StatusCode);
        await ApiTests.Login(api,"compras@example.test");Assert.Equal(HttpStatusCode.Forbidden,(await api.Http.GetAsync(path)).StatusCode);
        await ApiTests.Login(api);using var result=JsonDocument.Parse(await api.Http.GetStringAsync(path));
        var cliente=Assert.Single(result.RootElement.GetProperty("clientes").EnumerateArray());
        Assert.Equal(1,cliente.GetProperty("Id").GetInt32());Assert.Equal("Juan Alexis Guevara Nolasco",cliente.GetProperty("Nombre").GetString());
        using var reference=JsonDocument.Parse(await api.Http.GetStringAsync(path.Replace("operativo","referencia")));
        Assert.Equal(-901,Assert.Single(reference.RootElement.GetProperty("clientes").EnumerateArray()).GetProperty("Id").GetInt32());
        Assert.Equal(HttpStatusCode.BadRequest,(await api.Http.GetAsync("/api/bi/opciones?procedencia=desconocida&buscar=Juan")).StatusCode);
    }
}
