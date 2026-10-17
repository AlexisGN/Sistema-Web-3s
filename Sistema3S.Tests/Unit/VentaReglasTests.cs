using Sistema3S.Web.DTOs.Venta;
using Sistema3S.Web.Services.Implementations;

namespace Sistema3S.Tests.Unit;

[Trait("Category", "Unit")]
public class VentaReglasTests
{
    [Theory]
    [InlineData(true, "Factura")]
    [InlineData(true, "Boleta")]
    [InlineData(false, "Boleta")]
    [InlineData(false, "Nota de venta")]
    public void ComprobantesPermitidosConservanProductos(bool empresa, string comprobante)
    {
        VentaReglas.Validar(empresa, comprobante,
            [new() { IdElementoCatalogo = 1, Cantidad = 3 }], new HashSet<int> { 2 });
    }

    [Fact]
    public void EmpresaRechazaNotaVenta()
    {
        var error = Assert.Throws<InvalidOperationException>(() => VentaReglas.Validar(true, "Nota de venta", [], new HashSet<int>()));
        Assert.Contains("Factura o Boleta", error.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(10)]
    public void ServicioRealRechazaCantidadDistintaDeUnoAunqueElDtoDigaProducto(int cantidad)
    {
        var error = Assert.Throws<InvalidOperationException>(() => VentaReglas.Validar(false, "Boleta",
            [new() { IdElementoCatalogo = 2, Cantidad = cantidad, TipoElemento = "Producto" }], new HashSet<int> { 2 }));
        Assert.Contains("fija de 1", error.Message);
    }

    [Fact]
    public void VentaMixtaConservaCantidadesYPrecios()
    {
        List<VentaDetalleCrearDto> detalles = [
            new() { IdElementoCatalogo = 1, Cantidad = 5, PrecioUnitario = 10 },
            new() { IdElementoCatalogo = 2, Cantidad = 1, PrecioUnitario = 100, TipoElemento = null }
        ];
        VentaReglas.Validar(true, "Boleta", detalles, new HashSet<int> { 2 });
        Assert.Equal(150, detalles.Sum(d => d.Cantidad * d.PrecioUnitario));
        Assert.Equal(5, detalles[0].Cantidad); Assert.Equal(1, detalles[1].Cantidad);
    }
}
