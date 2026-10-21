using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Sistema3S.Tests.Fixtures;
using Sistema3S.Web.Controllers;

namespace Sistema3S.Tests.Integration;

[Trait("Category", "Integration")]
public class AuditoriaTests
{
    private static JsonElement Value(IActionResult result) => JsonSerializer.SerializeToElement(
        Assert.IsType<OkObjectResult>(result).Value,
        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

    [Fact(DisplayName = "PI-AUD-001 | Paginación y filtros conservan grupos internos completos sin actividad pública")]
    public async Task GruposInternos()
    {
        await using var q = await QaDatabase.CreateAsync();
        await q.Exec("""
            INSERT Rol(Nombre) VALUES(N'  CLIENTE WEB  ');
            INSERT Usuario(IdRol,Correo,ContrasenaHash,Estado) VALUES
                (2,'inactivo@example.test','qa',0),(4,'portal@example.test','qa',1),(2,'solo-publico@example.test','qa',1);
            INSERT PersonalInterno(IdUsuario,Nombres,ApellidoPaterno) VALUES(1,N'Administrador',N'Prueba');
            UPDATE Cliente SET IdUsuario=2 WHERE IdCliente=1;
            DELETE AuditoriaLog;
            INSERT AuditoriaLog(IdUsuario,Accion,TablaAfectada,IdRegistro,Fecha,Descripcion,DatosAntes,DatosDespues,Solicitud,Origen) VALUES
                (1,'CREAR','Compra',10,'20261022 00:01',N'Compra.Registrar',NULL,N'{"IdEstadoCompra":1}','compra',N'Administración'),
                (NULL,'ERROR','MovimientoCaja',22,'20261022 00:02',N'Compra.Registrar',NULL,N'{"Monto":118}','compra',N'Administración'),
                (1,'ACTUALIZAR','Caja',9,'20261022 00:03',N'Compra.Registrar',N'{"SaldoSistema":0}',N'{"SaldoSistema":118}','compra',N'Administración'),
                (1,'SOLICITUD','API',NULL,'20261021 23:59',N'Compra.Registrar HTTP 200',NULL,NULL,'compra',N'Administración'),
                (4,'SOLICITUD','API',NULL,'20261022 12:00',N'Producto.Actualizar HTTP 200',NULL,NULL,'inactivo',N'Administración'),
                (1,'SOLICITUD','API',NULL,'20261023',N'Auth.Login HTTP 200',NULL,NULL,NULL,N'Web'),
                (1,'SOLICITUD','API',NULL,'20261023',N'Auth.Login HTTP 200',NULL,NULL,'  ',N'Web'),
                (1,'SOLICITUD','API',NULL,'20261023',N'Auth.Login HTTP 200',NULL,NULL,'login',N'Web'),
                (3,'SOLICITUD','API',NULL,'20261023',N'Cotizacion.Crear HTTP 200',NULL,NULL,'cliente',N'Administración'),
                (5,'SOLICITUD','API',NULL,'20261023',N'Cotizacion.Crear HTTP 200',NULL,NULL,'cliente-web',N'Administración'),
                (6,'SOLICITUD','API',NULL,'20261023',N'Catalogo.Consultar HTTP 200',NULL,NULL,'empleado-web',N'Web'),
                (1,'CREAR','Cotizacion',10,'20261023',N'Cotizacion.Crear',NULL,NULL,'mixto-web',N'Administración'),
                (NULL,'SOLICITUD','API',NULL,'20261023',N'ClienteWeb.Cotizar HTTP 200',NULL,NULL,'mixto-web',N'Web'),
                (1,'ACTUALIZAR','Producto',1,'20261023',N'Producto.Actualizar',NULL,NULL,'mixto-cliente',N'Administración'),
                (3,'SOLICITUD','API',NULL,'20261023',N'Producto.Actualizar HTTP 200',NULL,NULL,'mixto-cliente',N'Administración'),
                (NULL,'CREAR','Producto',1,'20261023',N'Operación SQL',NULL,NULL,NULL,N'SQL');
            ;WITH N AS (SELECT TOP(20) ROW_NUMBER() OVER(ORDER BY object_id) n FROM sys.objects)
            INSERT AuditoriaLog(IdUsuario,Accion,TablaAfectada,Fecha,Descripcion,Solicitud,Origen)
                SELECT 2,'SOLICITUD','API',DATEADD(second,n,'20261020'),N'Producto.Actualizar HTTP 200',CONCAT('pagina-',n),N'Administración' FROM N;
            """);
        var before = await q.Scalar("SELECT COUNT(*) FROM AuditoriaLog");
        await using var db = q.Open();
        var c = new AuditoriaController(db);
        var first = Value(await c.Listar(null, null, null, null, null));
        var second = Value(await c.Listar(null, null, null, null, null, 2));
        Assert.Equal(25, first.GetProperty("total").GetInt32());
        Assert.Equal(20, first.GetProperty("items").GetArrayLength());
        Assert.Equal(5, second.GetProperty("items").GetArrayLength());
        var ids = first.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("id").GetInt32());
        Assert.Empty(ids.Intersect(second.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("id").GetInt32())));
        var empty = Value(await c.Listar(null, null, null, null, null, 999));
        Assert.Equal(25, empty.GetProperty("total").GetInt32());
        Assert.Empty(empty.GetProperty("items").EnumerateArray());

        var options = Value(await c.Opciones());
        Assert.Equal(new[] { 1, 2, 4 }, options.GetProperty("responsables").EnumerateArray().Select(x => x.GetProperty("id").GetInt32()).Order().ToArray());
        foreach (var excluded in new[] { 3, 5, 6 })
            Assert.Equal(0, Value(await c.Listar(null, excluded, null, null, null)).GetProperty("total").GetInt32());
        Assert.Equal(20, Value(await c.Listar(null, 2, null, null, null)).GetProperty("total").GetInt32());
        Assert.Equal(3, Value(await c.Listar("Accesos", null, null, null, null)).GetProperty("total").GetInt32());
        Assert.Equal(0, Value(await c.Listar("Cotizaciones", null, null, null, null)).GetProperty("total").GetInt32());

        var purchase = Value(await c.Listar("Compras", 1, new(2026, 10, 22), new(2026, 10, 22), "error"));
        Assert.Equal(1, purchase.GetProperty("total").GetInt32());
        var activity = purchase.GetProperty("items")[0];
        Assert.Equal("Administrador Prueba", activity.GetProperty("responsable").GetString());
        Assert.Equal("error", activity.GetProperty("resultado").GetString());
        Assert.Equal(3, activity.GetProperty("detalles").GetArrayLength());
        Assert.Contains("Registrada", activity.ToString());
        Assert.Equal(0, Value(await c.Listar(null, null, new(2026, 10, 21), new(2026, 10, 21), null)).GetProperty("total").GetInt32());
        Assert.Equal(before, await q.Scalar("SELECT COUNT(*) FROM AuditoriaLog"));
    }

    [Fact(DisplayName = "PI-AUD-002 | Historial amplio devuelve sólo la página solicitada y oculta campos sensibles")]
    public async Task HistorialAmplio()
    {
        await using var q = await QaDatabase.CreateAsync();
        await q.Exec("""
            DELETE AuditoriaLog;
            ;WITH N AS (SELECT TOP(8000) ROW_NUMBER() OVER(ORDER BY (SELECT NULL)) n FROM sys.all_objects a CROSS JOIN sys.all_objects b)
            INSERT AuditoriaLog(IdUsuario,Accion,TablaAfectada,IdRegistro,Fecha,Descripcion,DatosDespues,Solicitud,Origen)
            SELECT 1,r.Accion,r.Tabla,CASE WHEN r.Tabla='Producto' THEN 1 END,DATEADD(second,n,'20261001'),
                N'Producto.Actualizar HTTP 200',CASE WHEN r.Tabla='Producto' THEN
                CONCAT(N'{"StockActual":',n,N',"ContrasenaHash":"no-exponer-qa","CampoNoVisible":"',REPLICATE(N'x',1500),N'"}') END,
                CONCAT('historia-',n),N'Administración'
            FROM N CROSS JOIN (VALUES('SOLICITUD','API'),('ACTUALIZAR','Producto')) r(Accion,Tabla);
            """);
        await using var db = q.Open();
        var c = new AuditoriaController(db);
        var page = Value(await c.Listar("Productos", null, null, null, "completado", 2));
        Assert.Equal(8000, page.GetProperty("total").GetInt32());
        Assert.Equal(20, page.GetProperty("items").GetArrayLength());
        Assert.All(page.GetProperty("items").EnumerateArray(), x => Assert.Single(x.GetProperty("detalles").EnumerateArray()));
        Assert.DoesNotContain("no-exponer-qa", page.ToString());
        Assert.DoesNotContain("CampoNoVisible", page.ToString());
        Assert.True(JsonSerializer.SerializeToUtf8Bytes(page).Length < 100_000);
        Assert.Equal(16000, await q.Scalar("SELECT COUNT(*) FROM AuditoriaLog"));
    }

    [Fact(DisplayName = "PI-AUD-003 | Consulta vacía, validaciones y cancelación no alteran la bitácora")]
    public async Task VacioValidacionCancelacion()
    {
        await using var q = await QaDatabase.CreateAsync();
        await q.Exec("DELETE AuditoriaLog");
        await using var db = q.Open();
        var c = new AuditoriaController(db);
        var page = Value(await c.Listar(null, null, null, null, null));
        Assert.Equal(0, page.GetProperty("total").GetInt32());
        Assert.Empty(page.GetProperty("items").EnumerateArray());
        Assert.Empty(Value(await c.Opciones()).GetProperty("responsables").EnumerateArray());
        Assert.IsType<BadRequestObjectResult>(await c.Listar(null, null, null, null, null, 0));
        Assert.IsType<BadRequestObjectResult>(await c.Listar("inexistente", null, null, null, null));
        Assert.IsType<BadRequestObjectResult>(await c.Listar(null, null, new(2026, 10, 23), new(2026, 10, 1), null));
        using var cancel = new CancellationTokenSource();
        cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => c.Listar(null, null, null, null, null, cancellationToken: cancel.Token));
        Assert.Equal(0, await q.Scalar("SELECT COUNT(*) FROM AuditoriaLog"));
    }

    [Fact(DisplayName = "PS-AUD-001 | Listado y opciones siguen siendo exclusivos del administrador")]
    public async Task Seguridad()
    {
        await using var q = await QaDatabase.CreateAsync();
        await q.Exec("UPDATE Cliente SET IdUsuario=3 WHERE IdCliente=1");
        await using var api = await ApiHost.Start(q);
        foreach (var route in new[] { "/api/auditoria", "/api/auditoria/opciones" })
            Assert.Equal(HttpStatusCode.Unauthorized, (await api.Http.GetAsync(route)).StatusCode);
        await ApiTests.Login(api, "compras@example.test");
        foreach (var route in new[] { "/api/auditoria", "/api/auditoria/opciones" })
            Assert.Equal(HttpStatusCode.Forbidden, (await api.Http.GetAsync(route)).StatusCode);
        var client = await ApiTests.Json(await api.Http.PostAsJsonAsync("/api/cliente-web/login", new { correo = "cliente@example.test", contrasena = QaDatabase.Password }));
        api.Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", client.GetProperty("token").GetString());
        foreach (var route in new[] { "/api/auditoria", "/api/auditoria/opciones" })
            Assert.Equal(HttpStatusCode.Forbidden, (await api.Http.GetAsync(route)).StatusCode);
        await ApiTests.Login(api);
        foreach (var route in new[] { "/api/auditoria", "/api/auditoria/opciones" })
            Assert.Equal(HttpStatusCode.OK, (await api.Http.GetAsync(route)).StatusCode);
    }
}
