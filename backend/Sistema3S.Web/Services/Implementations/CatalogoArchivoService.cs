using Sistema3S.Web.Services.Interfaces;

namespace Sistema3S.Web.Services.Implementations
{
    public class CatalogoArchivoService : ICatalogoArchivoService
    {
        private const long TamanioMaximoImagen = 5 * 1024 * 1024;
        private const long TamanioMaximoPdf = 15 * 1024 * 1024;

        private static readonly HashSet<string> ExtensionesImagen =
            new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

        private readonly IWebHostEnvironment _environment;

        public CatalogoArchivoService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public Task<string> GuardarImagenProductoAsync(
            IFormFile archivo,
            CancellationToken cancellationToken = default
        ) => GuardarImagenAsync(archivo, "productos/imagenes", cancellationToken);

        public Task<string> GuardarFichaTecnicaProductoAsync(
            IFormFile archivo,
            CancellationToken cancellationToken = default
        ) => GuardarPdfAsync(archivo, "productos/fichas-tecnicas", cancellationToken);

        public Task<string> GuardarImagenServicioAsync(
            IFormFile archivo,
            CancellationToken cancellationToken = default
        ) => GuardarImagenAsync(archivo, "servicios/imagenes", cancellationToken);

        public Task EliminarArchivoLocalAsync(string? rutaPublica)
        {
            if (string.IsNullOrWhiteSpace(rutaPublica) ||
                !rutaPublica.StartsWith("/uploads/catalogo/", StringComparison.OrdinalIgnoreCase))
            {
                return Task.CompletedTask;
            }

            var webRoot = ObtenerWebRoot();
            var rutaRelativa = rutaPublica.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var rutaFisica = Path.GetFullPath(Path.Combine(webRoot, rutaRelativa));
            var raizPermitida = Path.GetFullPath(Path.Combine(webRoot, "uploads", "catalogo"))
                + Path.DirectorySeparatorChar;

            if (!rutaFisica.StartsWith(raizPermitida, StringComparison.OrdinalIgnoreCase))
            {
                return Task.CompletedTask;
            }

            try
            {
                if (File.Exists(rutaFisica))
                {
                    File.Delete(rutaFisica);
                }
            }
            catch (IOException)
            {
                // La operación principal ya fue guardada; el archivo puede limpiarse posteriormente.
            }
            catch (UnauthorizedAccessException)
            {
                // La operación principal ya fue guardada; el archivo puede limpiarse posteriormente.
            }

            return Task.CompletedTask;
        }

        private async Task<string> GuardarImagenAsync(
            IFormFile archivo,
            string subcarpeta,
            CancellationToken cancellationToken
        )
        {
            ValidarArchivoBasico(archivo, TamanioMaximoImagen, "La imagen", "5 MB");

            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();

            if (!ExtensionesImagen.Contains(extension))
            {
                throw new InvalidOperationException("La imagen debe estar en formato JPG, PNG o WebP.");
            }

            await using var origen = archivo.OpenReadStream();
            var cabecera = await LeerCabeceraAsync(origen, 12, cancellationToken);

            if (!EsImagenValida(extension, cabecera))
            {
                throw new InvalidOperationException("El contenido del archivo no corresponde a una imagen válida.");
            }

            origen.Position = 0;
            return await GuardarAsync(origen, extension, subcarpeta, cancellationToken);
        }

        private async Task<string> GuardarPdfAsync(
            IFormFile archivo,
            string subcarpeta,
            CancellationToken cancellationToken
        )
        {
            ValidarArchivoBasico(archivo, TamanioMaximoPdf, "La ficha técnica", "15 MB");

            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();

            if (!extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("La ficha técnica debe estar en formato PDF.");
            }

            await using var origen = archivo.OpenReadStream();
            var cabecera = await LeerCabeceraAsync(origen, 5, cancellationToken);

            if (cabecera.Length < 5 ||
                cabecera[0] != (byte)'%' ||
                cabecera[1] != (byte)'P' ||
                cabecera[2] != (byte)'D' ||
                cabecera[3] != (byte)'F' ||
                cabecera[4] != (byte)'-')
            {
                throw new InvalidOperationException("El contenido del archivo no corresponde a un PDF válido.");
            }

            origen.Position = 0;
            return await GuardarAsync(origen, extension, subcarpeta, cancellationToken);
        }

        private async Task<string> GuardarAsync(
            Stream origen,
            string extension,
            string subcarpeta,
            CancellationToken cancellationToken
        )
        {
            var webRoot = ObtenerWebRoot();
            var segmentos = subcarpeta.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var raizCatalogo = Path.Combine(webRoot, "uploads", "catalogo");
            var carpeta = segmentos.Aggregate(raizCatalogo, Path.Combine);
            Directory.CreateDirectory(carpeta);

            var nombreArchivo = $"{Guid.NewGuid():N}{extension}";
            var rutaFisica = Path.GetFullPath(Path.Combine(carpeta, nombreArchivo));
            var raizPermitida = Path.GetFullPath(carpeta) + Path.DirectorySeparatorChar;

            if (!rutaFisica.StartsWith(raizPermitida, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("No se pudo determinar una ruta segura para el archivo.");
            }

            try
            {
                await using var destino = new FileStream(
                    rutaFisica,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    81920,
                    useAsync: true
                );

                await origen.CopyToAsync(destino, cancellationToken);
            }
            catch
            {
                await EliminarArchivoLocalAsync($"/uploads/catalogo/{subcarpeta}/{nombreArchivo}");
                throw;
            }

            return $"/uploads/catalogo/{subcarpeta}/{nombreArchivo}";
        }

        private string ObtenerWebRoot()
        {
            return _environment.WebRootPath
                ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        }

        private static void ValidarArchivoBasico(
            IFormFile archivo,
            long tamanioMaximo,
            string etiqueta,
            string tamanioTexto
        )
        {
            if (archivo.Length <= 0)
            {
                throw new InvalidOperationException($"{etiqueta} está vacía.");
            }

            if (archivo.Length > tamanioMaximo)
            {
                throw new InvalidOperationException($"{etiqueta} no puede superar {tamanioTexto}.");
            }
        }

        private static async Task<byte[]> LeerCabeceraAsync(
            Stream stream,
            int cantidad,
            CancellationToken cancellationToken
        )
        {
            var buffer = new byte[cantidad];
            var leidos = 0;

            while (leidos < cantidad)
            {
                var actual = await stream.ReadAsync(
                    buffer.AsMemory(leidos, cantidad - leidos),
                    cancellationToken
                );

                if (actual == 0)
                {
                    break;
                }

                leidos += actual;
            }

            return leidos == cantidad ? buffer : buffer[..leidos];
        }

        private static bool EsImagenValida(string extension, byte[] cabecera)
        {
            return extension switch
            {
                ".jpg" or ".jpeg" =>
                    cabecera.Length >= 3 &&
                    cabecera[0] == 0xFF && cabecera[1] == 0xD8 && cabecera[2] == 0xFF,

                ".png" =>
                    cabecera.Length >= 8 &&
                    cabecera[0] == 0x89 && cabecera[1] == 0x50 &&
                    cabecera[2] == 0x4E && cabecera[3] == 0x47 &&
                    cabecera[4] == 0x0D && cabecera[5] == 0x0A &&
                    cabecera[6] == 0x1A && cabecera[7] == 0x0A,

                ".webp" =>
                    cabecera.Length >= 12 &&
                    cabecera[0] == (byte)'R' && cabecera[1] == (byte)'I' &&
                    cabecera[2] == (byte)'F' && cabecera[3] == (byte)'F' &&
                    cabecera[8] == (byte)'W' && cabecera[9] == (byte)'E' &&
                    cabecera[10] == (byte)'B' && cabecera[11] == (byte)'P',

                _ => false
            };
        }
    }
}
