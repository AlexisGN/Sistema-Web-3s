using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sistema3S.Tests.Fixtures;

namespace Sistema3S.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class SelectoresActivosTests
{
    [Fact(DisplayName="PF-SEL-001 | Selectores excluyen inactivos antes de buscar y paginar, sin cambiar administración")]
    public async Task SoloActivosAntesDePaginar()
    {
        await using var q=await QaDatabase.CreateAsync();
        await q.Exec("""
            DECLARE @id int;
            INSERT Cliente(IdTipoCliente,IdTipoDocumento,NumeroDocumento,Estado) VALUES(1,1,'80000002',1);
            SET @id=SCOPE_IDENTITY();
            INSERT ClientePersonaNatural(IdCliente,Nombres,ApellidoPaterno) VALUES(@id,N'ActivoFiltroCliente',N'QA');
            INSERT Cliente(IdTipoCliente,IdTipoDocumento,NumeroDocumento,Estado) VALUES(1,1,'80000003',0);
            SET @id=SCOPE_IDENTITY();
            INSERT ClientePersonaNatural(IdCliente,Nombres,ApellidoPaterno) VALUES(@id,N'InactivoFiltroCliente',N'QA');
            INSERT Cliente(IdTipoCliente,IdTipoDocumento,NumeroDocumento,Estado) VALUES(1,1,'80000004',0);
            SET @id=SCOPE_IDENTITY();
            INSERT ClientePersonaNatural(IdCliente,Nombres,ApellidoPaterno) VALUES(@id,N'InactivoFiltroCliente',N'QA');
            INSERT Cliente(IdTipoCliente,IdTipoDocumento,NumeroDocumento,Estado) VALUES(1,1,'80000005',0);
            SET @id=SCOPE_IDENTITY();
            INSERT ClientePersonaNatural(IdCliente,Nombres,ApellidoPaterno) VALUES(@id,N'InactivoFiltroCliente',N'QA');
            INSERT Proveedor(Ruc,RazonSocial,Estado) VALUES
              ('20000000002',N'ActivoFiltroProveedor',1),
              ('20000000003',N'InactivoFiltroProveedor',0),
              ('20000000004',N'InactivoFiltroProveedor',0),
              ('20000000005',N'InactivoFiltroProveedor',0);
            """);
        await using var api=await ApiHost.Start(q);
        await ApiTests.Login(api);
        foreach(var route in new[]{"cliente","proveedor"})
        {
            var id=route=="cliente"?"idCliente":"idProveedor";
            var page1=await ApiTests.Json(await api.Http.GetAsync($"/api/{route}?soloActivos=true&pagina=1&tamanioPagina=1"));
            var page2=await ApiTests.Json(await api.Http.GetAsync($"/api/{route}?soloActivos=true&pagina=2&tamanioPagina=1"));
            Assert.Equal(2,page1.GetProperty("totalRegistros").GetInt32());
            Assert.Equal(2,page1.GetProperty("items")[0].GetProperty(id).GetInt32());
            Assert.Equal(1,page2.GetProperty("items")[0].GetProperty(id).GetInt32());
            Assert.True(page1.GetProperty("items")[0].GetProperty("estado").GetBoolean());
            var search=await ApiTests.Json(await api.Http.GetAsync($"/api/{route}?soloActivos=true&buscar=Filtro"));
            Assert.Equal(1,search.GetProperty("totalRegistros").GetInt32());
            var inactive=await ApiTests.Json(await api.Http.GetAsync($"/api/{route}?soloActivos=true&buscar=Inactivo"));
            Assert.Equal(0,inactive.GetProperty("totalRegistros").GetInt32());
            Assert.Empty(inactive.GetProperty("items").EnumerateArray());
            foreach(var suffix in new[]{"","?soloActivos=false"})
            {
                var all=await ApiTests.Json(await api.Http.GetAsync($"/api/{route}{suffix}"));
                Assert.Equal(5,all.GetProperty("totalRegistros").GetInt32());
                Assert.False(all.GetProperty("items")[0].GetProperty("estado").GetBoolean());
            }
        }
    }

    [Fact(DisplayName="PF-SEL-002 | Desactivar terceros conserva operaciones históricas y rechaza nuevos registros")]
    public async Task HistoricoYValidacionExistente()
    {
        await using var q=await QaDatabase.CreateAsync();
        await NegocioTests.Abrir(q);
        await using var api=await ApiHost.Start(q);await ApiTests.Login(api);
        var purchase=await ApiTests.Json(await api.Http.PostAsJsonAsync("/api/compra",NegocioTests.Compra()));
        var sale=await ApiTests.Json(await api.Http.PostAsJsonAsync("/api/venta",NegocioTests.Venta()));
        var stock=await q.Scalar("SELECT StockActual FROM Inventario WHERE IdProducto=1");
        await q.Exec("UPDATE Cliente SET Estado=0 WHERE IdCliente=1; UPDATE Proveedor SET Estado=0 WHERE IdProveedor=1;");
        foreach(var route in new[]{"cliente","proveedor"})
        {
            var detail=await ApiTests.Json(await api.Http.GetAsync($"/api/{route}/1"));
            Assert.False(detail.GetProperty("estado").GetBoolean());
            var active=await ApiTests.Json(await api.Http.GetAsync($"/api/{route}?soloActivos=true"));
            Assert.Equal(0,active.GetProperty("totalRegistros").GetInt32());
        }
        Assert.Equal(HttpStatusCode.OK,(await api.Http.GetAsync($"/api/compra/{purchase.GetProperty("idCompra").GetInt32()}/detalle")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await api.Http.GetAsync($"/api/venta/{sale.GetProperty("idVenta").GetInt32()}/detalle")).StatusCode);
        var rejectedPurchase=await api.Http.PostAsJsonAsync("/api/compra",NegocioTests.Compra());
        var rejectedSale=await api.Http.PostAsJsonAsync("/api/venta",NegocioTests.Venta());
        Assert.Equal(HttpStatusCode.BadRequest,rejectedPurchase.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,rejectedSale.StatusCode);
        Assert.Contains("inactiv",await rejectedPurchase.Content.ReadAsStringAsync(),StringComparison.OrdinalIgnoreCase);
        Assert.Contains("inactiv",await rejectedSale.Content.ReadAsStringAsync(),StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1,await q.Scalar("SELECT COUNT(*) FROM Compra"));
        Assert.Equal(1,await q.Scalar("SELECT COUNT(*) FROM Venta"));
        Assert.Equal(stock,await q.Scalar("SELECT StockActual FROM Inventario WHERE IdProducto=1"));
    }
}
