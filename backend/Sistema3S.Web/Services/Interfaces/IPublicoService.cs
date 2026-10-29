using Sistema3S.Web.DTOs.Publico;

namespace Sistema3S.Web.Services.Interfaces
{
    public interface IPublicoService
    {
        Task<InicioPublicoDto> ObtenerInicioAsync();

        Task<List<CategoriaPublicaDto>> ObtenerCategoriasPublicasAsync();

        Task<List<MarcaPublicaDto>> ObtenerMarcasPublicasAsync();

        Task<ProductoPublicoListadoDto> ObtenerProductosAsync(
            string? busqueda,
            int? idCategoria,
            int? idMarca,
            int pagina,
            int tamanioPagina
        );

        Task<ProductoDetallePublicoDto?> ObtenerProductoDetalleAsync(int idProducto);

        Task<List<ServicioPublicoDto>> ObtenerServiciosPublicosAsync();

        Task<ServicioDetallePublicoDto?> ObtenerServicioDetalleAsync(int idServicio);

        Task<BusquedaPublicaDto> BuscarAsync(string? busqueda, int limitePorTipo = 8);

        Task<NosotrosPublicoDto> ObtenerNosotrosAsync();
    }
}