using Microsoft.EntityFrameworkCore;
using Sistema3S.Tests.Fixtures;
using Sistema3S.Web.Services.Implementations;

namespace Sistema3S.Tests.Integration;

[Trait("Category", "Integration")]
public class VentaCatalogoTests
{
    [Fact]
    public async Task SelectoresCatalogoYCotizacionExcluyenInactivos()
    {
        await using var q = await QaDatabase.CreateAsync();
        await q.Exec("""
            INSERT ElementoCatalogo(IdTipoElemento,Nombre,PrecioReferencial,Estado) VALUES(2,N'Servicio QA',100,1);
            INSERT Servicio(IdElementoCatalogo) VALUES(SCOPE_IDENTITY());
            UPDATE Cliente SET Estado=0 WHERE IdCliente=1;
            UPDATE Proveedor SET Estado=0 WHERE IdProveedor=1;
            UPDATE ElementoCatalogo SET Estado=0;
            """);
        await using var api = await ApiHost.Start(q); await ApiTests.Login(api);
        foreach (var route in new[] {"cliente?soloActivos=true", "proveedor?soloActivos=true", "producto?estado=true", "servicio?estado=true"})
        {
            var result = await ApiTests.Json(await api.Http.GetAsync($"/api/{route}"));
            Assert.Equal(0, result.GetProperty("totalRegistros").GetInt32());
            Assert.Empty(result.GetProperty("items").EnumerateArray());
        }
        foreach (var route in new[] {"clientes", "elementos-cotizables"})
        {
            var result = await ApiTests.Json(await api.Http.GetAsync($"/api/cotizacion/{route}"));
            Assert.Empty(result.EnumerateArray());
        }
        var all = await ApiTests.Json(await api.Http.GetAsync("/api/producto"));
        Assert.Equal(1, all.GetProperty("totalRegistros").GetInt32());
        var allServices = await ApiTests.Json(await api.Http.GetAsync("/api/servicio"));
        Assert.Equal(1, allServices.GetProperty("totalRegistros").GetInt32());
    }

    [Fact]
    public async Task ServicioSeValidaConCatalogoRealYEmpresaNoAdmiteNotaVenta()
    {
        await using var q = await QaDatabase.CreateAsync(); await NegocioTests.Abrir(q);
        await q.Exec("""
            INSERT Cliente(IdTipoCliente,IdTipoDocumento,NumeroDocumento) VALUES(2,2,'20111111111');
            INSERT ClienteEmpresa(IdCliente,RazonSocial) VALUES(SCOPE_IDENTITY(),N'Empresa QA');
            INSERT ElementoCatalogo(IdTipoElemento,Nombre,PrecioReferencial) VALUES(2,N'Servicio QA',100);
            INSERT Servicio(IdElementoCatalogo) VALUES(SCOPE_IDENTITY());
            """);
        using var db = q.Open(); var service = new VentaService(db);
        var idServicio = await db.Servicio.Select(s => s.IdElementoCatalogo).SingleAsync();
        var venta = NegocioTests.Venta(2);
        venta.Detalles[0].IdElementoCatalogo = idServicio;
        venta.Detalles[0].TipoElemento = "Producto";
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegistrarVentaCompletaAsync(venta));
        Assert.Contains("fija de 1", error.Message);
        Assert.Equal(0, await q.Scalar("SELECT COUNT(*) FROM Venta"));
        Assert.Equal(0, await q.Scalar("SELECT COUNT(*) FROM MovimientoCaja"));
        venta.Detalles[0].Cantidad = 1; venta.MontoPagado = 118; venta.IdCliente = 2;
        venta.TipoComprobante = "Nota de venta";
        error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegistrarVentaCompletaAsync(venta));
        Assert.Contains("Factura o Boleta", error.Message);
        foreach (var tipo in new[] {"Factura", "Boleta"})
        {
            venta.TipoComprobante = tipo;
            var result = await service.RegistrarVentaCompletaAsync(venta);
            Assert.Equal(118, result.Total);
        }
        Assert.Equal(2, await q.Scalar("SELECT COUNT(*) FROM Venta"));
        Assert.Equal(0, await q.Scalar("SELECT COUNT(*) FROM MovimientoStock"));
    }
}
