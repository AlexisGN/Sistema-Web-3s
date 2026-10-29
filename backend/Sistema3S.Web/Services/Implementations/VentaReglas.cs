using Sistema3S.Web.DTOs.Venta;

namespace Sistema3S.Web.Services.Implementations;

public static class VentaReglas
{
    public static void Validar(bool esEmpresa, string tipoComprobante,
        IReadOnlyCollection<VentaDetalleCrearDto> detalles, IReadOnlySet<int> idsServicios)
    {
        if (esEmpresa && tipoComprobante.Trim() is not ("Factura" or "Boleta"))
            throw new InvalidOperationException("Para clientes empresa solo se permite Factura o Boleta.");

        if (detalles.Any(d => idsServicios.Contains(d.IdElementoCatalogo) && d.Cantidad != 1))
            throw new InvalidOperationException("Los servicios se registran con cantidad fija de 1.");
    }
}
