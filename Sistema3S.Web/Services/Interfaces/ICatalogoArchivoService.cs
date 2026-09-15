namespace Sistema3S.Web.Services.Interfaces
{
    public interface ICatalogoArchivoService
    {
        Task<string> GuardarImagenProductoAsync(
            IFormFile archivo,
            CancellationToken cancellationToken = default
        );

        Task<string> GuardarFichaTecnicaProductoAsync(
            IFormFile archivo,
            CancellationToken cancellationToken = default
        );

        Task<string> GuardarImagenServicioAsync(
            IFormFile archivo,
            CancellationToken cancellationToken = default
        );

        Task EliminarArchivoLocalAsync(string? rutaPublica);
    }
}
