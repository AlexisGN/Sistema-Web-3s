using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Sistema3S.Web.Data;

namespace Sistema3S.Web.Services.Pdf
{
    public class PdfCotizacionService : IPdfCotizacionService
    {
        private readonly Bd3sContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IHttpClientFactory _httpClientFactory;

        private const string ColorRojo = "#d21927";
        private const string ColorNegro = "#141414";
        private const string ColorTexto = "#0f172a";
        private const string ColorSecundario = "#64748b";
        private const string ColorBorde = "#e2e8f0";
        private const string ColorFondo = "#f8fafc";

        public PdfCotizacionService(
            Bd3sContext context,
            IWebHostEnvironment environment,
            IHttpClientFactory httpClientFactory
        )
        {
            _context = context;
            _environment = environment;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<string> GenerarCotizacionAsync(int idCotizacion)
        {
            var modelo = await ConstruirModeloAsync(idCotizacion);

            var carpeta = Path.Combine(
                _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
                "uploads",
                "cotizaciones"
            );

            if (!Directory.Exists(carpeta))
            {
                Directory.CreateDirectory(carpeta);
            }

            var nombreArchivo = $"cotizacion-COT-{idCotizacion.ToString().PadLeft(5, '0')}-v2.pdf";
            var rutaFisica = Path.Combine(carpeta, nombreArchivo);
            var rutaRelativa = $"/uploads/cotizaciones/{nombreArchivo}";

            var logoPath = ObtenerRutaLogo();

            CrearDocumento(modelo, logoPath).GeneratePdf(rutaFisica);

            var cotizacion = await _context.Cotizacion
                .FirstOrDefaultAsync(c => c.IdCotizacion == idCotizacion);

            if (cotizacion == null)
            {
                throw new InvalidOperationException("No se pudo actualizar la ruta del PDF.");
            }

            cotizacion.ArchivoPdf = rutaRelativa;
            await _context.SaveChangesAsync();

            return rutaRelativa;
        }

        private IDocument CrearDocumento(CotizacionPdfModel modelo, string? logoPath)
        {
            return Document.Create(document =>
                {
                    document.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(32);
                        page.DefaultTextStyle(text => text.FontSize(9).FontColor(ColorTexto));

                        page.Header().Element(container => ComponerCabecera(container, modelo, logoPath));

                        page.Content().PaddingTop(14).Column(column =>
                        {
                            column.Spacing(12);



                            column.Item()
                                .Text("Propuesta comercial emitida para evaluación y aprobación del cliente.")
                                .FontSize(9)
                                .FontColor(ColorSecundario);

                            column.Item().Element(container => ComponerDatosCliente(container, modelo));
                            column.Item().Element(container => ComponerDetalle(container, modelo));
                            column.Item().Element(container => ComponerResumen(container, modelo));
                            if (!string.IsNullOrWhiteSpace(modelo.Observacion))
                                column.Item().Element(container => ComponerObservaciones(container, modelo));
                        });

                        page.Footer().Element(container => ComponerPiePagina(container));
                    });
                })
                ;
        }

        private async Task<CotizacionPdfModel> ConstruirModeloAsync(int idCotizacion)
        {
            var cotizacion = await _context.Cotizacion
                .FirstOrDefaultAsync(c => c.IdCotizacion == idCotizacion);

            if (cotizacion == null)
            {
                throw new InvalidOperationException("Cotización no encontrada.");
            }

            var estado = await _context.EstadoCotizacion
                .Where(e => e.IdEstadoCotizacion == cotizacion.IdEstadoCotizacion)
                .Select(e => e.Nombre)
                .FirstOrDefaultAsync() ?? "Sin estado";

            var empresa = await _context.Empresa
                .Where(e => e.Estado)
                .OrderBy(e => e.IdEmpresa)
                .Select(e => new EmpresaPdfModel
                {
                    NombreComercial = e.NombreComercial,
                    RazonSocial = e.RazonSocial,
                    Ruc = e.Ruc,
                    Rubro = e.Rubro,
                    Correo = e.Correo,
                    Telefono = e.Telefono,
                    Direccion = e.Direccion,
                    SitioWeb = e.SitioWeb
                })
                .FirstOrDefaultAsync();

            empresa ??= new EmpresaPdfModel
            {
                NombreComercial = "3S",
                RazonSocial = "3S (SERVICIO Y SOLUCIONES SUPERIORES S.A.C.)",
                Rubro = "Ingeniería y soluciones para la industria",
                Correo = "cevallosindustrial@gmail.com",
                Telefono = "+51 948 327 667",
                SitioWeb = "https://3s-omega.vercel.app/",
                Direccion = "Av. Los Pinos 960 Urb. El Ermitaño, Independencia - Lima - Lima"
            };

            var cliente = await ObtenerClienteAsync(cotizacion.IdCliente);

            var detalles = await (
    from d in _context.DetalleCotizacion
    join ec in _context.ElementoCatalogo on d.IdElementoCatalogo equals ec.IdElementoCatalogo
    join te in _context.TipoElemento on ec.IdTipoElemento equals te.IdTipoElemento
    where d.IdCotizacion == idCotizacion
    orderby d.IdDetalleCotizacion
    select new DetallePdfModel
    {
        TipoElemento = te.Nombre,
        Nombre = ec.Nombre,
        Descripcion = ec.Descripcion,

        ImagenUrl =
            _context.ImagenElementoCatalogo
                .Where(img =>
                    img.IdElementoCatalogo == ec.IdElementoCatalogo &&
                    img.Estado
                )
                .OrderByDescending(img => img.EsPrincipal)
                .ThenByDescending(img => img.IdImagenElementoCatalogo)
                .Select(img => img.UrlImagen)
                .FirstOrDefault()
            ?? ec.ImagenUrl,

        Cantidad = d.Cantidad,
        PrecioUnitario = d.PrecioUnitario,
        Subtotal = d.Subtotal,
        Observacion = d.Observacion
    }
).ToListAsync();

            if (detalles.Count == 0)
            {
                throw new InvalidOperationException("La cotización no tiene detalles para generar PDF.");
            }

            foreach (var detalle in detalles)
            {
                detalle.ImagenBytes = await ObtenerImagenAsync(detalle.ImagenUrl);
            }

            return new CotizacionPdfModel
            {
                IdCotizacion = cotizacion.IdCotizacion,
                FechaCotizacion = cotizacion.FechaCotizacion,
                EstadoCotizacion = estado,
                OrigenCotizacion = cotizacion.OrigenCotizacion,
                Subtotal = cotizacion.Subtotal,
                Descuento = cotizacion.Descuento,
                Igv = cotizacion.Igv,
                Total = cotizacion.Total,
                Observacion = cotizacion.Observacion,
                Empresa = empresa,
                Cliente = cliente,
                Detalles = detalles
            };
        }

        private async Task<ClientePdfModel> ObtenerClienteAsync(int idCliente)
        {
            var natural = await (
                from c in _context.Cliente
                join tc in _context.TipoCliente on c.IdTipoCliente equals tc.IdTipoCliente
                join td in _context.TipoDocumento on c.IdTipoDocumento equals td.IdTipoDocumento
                join pn in _context.ClientePersonaNatural on c.IdCliente equals pn.IdCliente
                where c.IdCliente == idCliente
                select new ClientePdfModel
                {
                    Nombre = pn.Nombres + " " + pn.ApellidoPaterno + " " + (pn.ApellidoMaterno ?? ""),
                    EsEmpresa = false,
                    TipoCliente = tc.Nombre,
                    TipoDocumento = td.Nombre,
                    NumeroDocumento = c.NumeroDocumento,
                    Correo = c.Correo,
                    Telefono = c.Telefono,
                    Direccion = c.Direccion
                }
            ).FirstOrDefaultAsync();

            if (natural != null)
            {
                return natural;
            }

            var empresa = await (
                from c in _context.Cliente
                join tc in _context.TipoCliente on c.IdTipoCliente equals tc.IdTipoCliente
                join td in _context.TipoDocumento on c.IdTipoDocumento equals td.IdTipoDocumento
                join ce in _context.ClienteEmpresa on c.IdCliente equals ce.IdCliente
                where c.IdCliente == idCliente
                select new ClientePdfModel
                {
                    Nombre = ce.RazonSocial,
                    NombreComercial = ce.NombreComercial,
                    EsEmpresa = true,
                    TipoCliente = tc.Nombre,
                    TipoDocumento = td.Nombre,
                    NumeroDocumento = c.NumeroDocumento,
                    Correo = c.Correo,
                    Telefono = c.Telefono,
                    Direccion = c.Direccion
                }
            ).FirstOrDefaultAsync();

            return empresa ?? new ClientePdfModel
            {
                Nombre = "Cliente no encontrado",
                EsEmpresa = false,
                TipoCliente = "Sin tipo",
                TipoDocumento = "Documento",
                NumeroDocumento = "-"
            };
        }

        private void ComponerCabecera(
            IContainer container,
            CotizacionPdfModel modelo,
            string? logoPath
        )
        {
            var contacto = string.Join("  |  ", new[] { modelo.Empresa.Correo, modelo.Empresa.Telefono, modelo.Empresa.SitioWeb }.Where(x => !string.IsNullOrWhiteSpace(x)));
            PdfEstilo3S.Encabezado(container, logoPath, "Cotización comercial", $"COT-{modelo.IdCotizacion:00000}",
                $"Fecha: {modelo.FechaCotizacion:dd/MM/yyyy}", modelo.Empresa.RazonSocial, contacto, modelo.Empresa.Direccion);
        }

        private void ComponerDatosCliente(IContainer container, CotizacionPdfModel modelo)
        {
            var campos = new List<(string Label, string Valor)>();

            if (modelo.Cliente.EsEmpresa)
            {
                campos.Add(("Razón social", modelo.Cliente.Nombre));

                if (!string.IsNullOrWhiteSpace(modelo.Cliente.NombreComercial))
                {
                    campos.Add(("Nombre comercial", modelo.Cliente.NombreComercial));
                }

                campos.Add(("RUC", modelo.Cliente.NumeroDocumento));
            }
            else
            {
                campos.Add(("Cliente", modelo.Cliente.Nombre));
                campos.Add(("Documento", $"{modelo.Cliente.TipoDocumento} {modelo.Cliente.NumeroDocumento}"));
            }

            if (!string.IsNullOrWhiteSpace(modelo.Cliente.Correo))
            {
                campos.Add(("Correo", modelo.Cliente.Correo));
            }

            if (!string.IsNullOrWhiteSpace(modelo.Cliente.Telefono))
            {
                campos.Add(("Teléfono", modelo.Cliente.Telefono));
            }

            if (!string.IsNullOrWhiteSpace(modelo.Cliente.Direccion))
            {
                campos.Add(("Dirección", modelo.Cliente.Direccion));
            }

            container
                .Border(1)
                .BorderColor(ColorBorde)
                .Background(ColorFondo)
                .Padding(12)
                .Column(column =>
                {
                    column.Spacing(8);

                    column.Item()
                        .Text("DATOS DEL CLIENTE")
                        .FontSize(11)
                        .Bold()
                        .FontColor(ColorNegro);

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        foreach (var campo in campos)
                        {
                            table.Cell()
                                .PaddingBottom(7)
                                .Element(c => Campo(c, campo.Label, campo.Valor));
                        }

                        if (campos.Count % 2 != 0)
                        {
                            table.Cell().Text("");
                        }
                    });
                });
        }

        private void ComponerDetalle(IContainer container, CotizacionPdfModel modelo)
        {
            container.Column(column =>
            {
                column.Spacing(8);

                column.Item()
                    .Text("DETALLE DE PRODUCTOS Y/O SERVICIOS COTIZADOS")
                    .FontSize(11)
                    .Bold()
                    .FontColor(ColorNegro);

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(62);
                        columns.RelativeColumn(3);
                        columns.ConstantColumn(44);
                        columns.ConstantColumn(76);
                        columns.ConstantColumn(82);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CeldaCabecera).Text("Imagen");
                        header.Cell().Element(CeldaCabecera).Text("Producto / Servicio");
                        header.Cell().Element(CeldaCabecera).AlignCenter().Text("Cant.");
                        header.Cell().Element(CeldaCabecera).AlignRight().Text("Precio c/IGV");
                        header.Cell().Element(CeldaCabecera).AlignRight().Text("Subtotal");
                    });

                    foreach (var item in modelo.Detalles)
                    {
                        table.Cell().Element(CeldaCuerpo).Height(58).Element(c => ComponerImagenDetalle(c, item));

                        table.Cell().Element(CeldaCuerpo).Column(c =>
                        {
                            c.Spacing(2);

                            c.Item().Text(item.Nombre).Bold().FontSize(9).FontColor(ColorTexto);
                            c.Item().Text(item.TipoElemento).FontSize(8).FontColor(ColorSecundario);

                            if (!string.IsNullOrWhiteSpace(item.Observacion))
                            {
                                c.Item().Text($"Observación: {item.Observacion}").FontSize(7).FontColor(ColorSecundario);
                            }
                        });

                        table.Cell().Element(CeldaCuerpo).AlignCenter().AlignMiddle().Text(item.Cantidad.ToString()).Bold();
                        table.Cell().Element(CeldaCuerpo).AlignRight().AlignMiddle().Text($"S/ {item.PrecioUnitario:0.00}");
                        table.Cell().Element(CeldaCuerpo).AlignRight().AlignMiddle().Text($"S/ {item.Subtotal:0.00}").Bold();
                    }
                });
            });
        }

        private void ComponerResumen(IContainer container, CotizacionPdfModel modelo)
        {
            var tieneDescuento = modelo.Descuento > 0;
            var valorVenta = Math.Round(modelo.Total / 1.18m, 2);

            container.Row(row =>
            {
                row.RelativeItem()
                    .Background(ColorFondo)
                    .Border(1)
                    .BorderColor(ColorBorde)
                    .Padding(12)
                    .Column(column =>
                    {
                        column.Spacing(5);

                        column.Item()
                            .Text("CONDICIONES COMERCIALES")
                            .FontSize(10)
                            .Bold()
                            .FontColor(ColorNegro);

                        column.Item()
                            .Text("Los precios indicados incluyen IGV. La propuesta queda sujeta a disponibilidad de stock, validación técnica o visita de evaluación cuando corresponda.")
                            .FontSize(8)
                            .FontColor(ColorSecundario);

                        column.Item()
                            .Text("La cotización tiene una vigencia referencial de 7 días calendario, salvo acuerdo distinto con el cliente.")
                            .FontSize(8)
                            .FontColor(ColorSecundario);

                        column.Item()
                            .Text("Esta cotización no representa comprobante de pago. La venta se formalizará con el comprobante correspondiente una vez aprobada la propuesta.")
                            .FontSize(8)
                            .FontColor(ColorSecundario);
                    });

                row.ConstantItem(18);

                row.ConstantItem(220)
                    .Border(1)
                    .BorderColor(ColorBorde)
                    .Padding(12)
                    .Column(column =>
                    {
                        column.Spacing(7);

                        FilaTotal(column, "Subtotal con IGV", modelo.Subtotal);

                        if (tieneDescuento)
                        {
                            FilaTotal(column, "Descuento", modelo.Descuento);
                        }

                        FilaTotal(column, "Valor venta", valorVenta);
                        FilaTotal(column, "IGV incluido 18%", modelo.Igv);

                        column.Item().LineHorizontal(1).LineColor(ColorBorde);

                        column.Item().PaddingVertical(9).Row(totalRow =>
                        {
                            totalRow.RelativeItem().Text("TOTAL REFERENCIAL").FontColor(PdfEstilo3S.Rojo).Bold().FontSize(10);
                            totalRow.AutoItem().Text($"S/ {modelo.Total:0.00}").FontColor(PdfEstilo3S.Rojo).Bold().FontSize(13);
                        });
                    });
            });
        }

        private void ComponerObservaciones(IContainer container, CotizacionPdfModel modelo)
        {
            container
                .Border(1)
                .BorderColor(ColorBorde)
                .Padding(12)
                .Column(column =>
                {
                    column.Spacing(5);

                    column.Item()
                        .Text("OBSERVACIONES PARA EL CLIENTE")
                        .FontSize(10)
                        .Bold()
                        .FontColor(ColorNegro);

                    column.Item()
                        .Text(string.IsNullOrWhiteSpace(modelo.Observacion)
                            ? "Sin observaciones adicionales."
                            : modelo.Observacion)
                        .FontSize(8)
                        .FontColor(ColorSecundario);
                });
        }

        private void ComponerPiePagina(IContainer container)
        {
            PdfEstilo3S.Pie(container, "Empresa 3S · Propuesta comercial para evaluación del cliente.");
        }

        private void ComponerImagenDetalle(IContainer container, DetallePdfModel item)
        {
            if (item.ImagenBytes != null &&
                item.ImagenBytes.Length > 0 &&
                EsImagenCompatible(item.ImagenBytes))
            {
                try
                {
                    container
                        .Padding(3)
                        .Image(item.ImagenBytes)
                        .FitArea();

                    return;
                }
                catch
                {
                    // Si QuestPDF no puede decodificar la imagen, se muestra el cuadro genérico.
                }
            }

            container
                .Border(1)
                .BorderColor(ColorBorde)
                .Background(ColorFondo)
                .AlignCenter()
                .AlignMiddle()
                .Text("Sin imagen")
                .FontSize(7)
                .FontColor(ColorSecundario);
        }

        private static IContainer CeldaCabecera(IContainer container)
        {
            return PdfEstilo3S.CabeceraTabla(container);
        }

        private static IContainer CeldaCuerpo(IContainer container)
        {
            return PdfEstilo3S.CuerpoTabla(container);
        }

        private void Campo(IContainer container, string label, string valor)
        {
            container.Column(column =>
            {
                column.Spacing(2);

                column.Item().Text(label).FontSize(7).FontColor(ColorSecundario);
                column.Item().Text(string.IsNullOrWhiteSpace(valor) ? "-" : valor).FontSize(9).Bold().FontColor(ColorTexto);
            });
        }

        private void FilaTotal(ColumnDescriptor column, string label, decimal valor)
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Text(label).FontSize(8).FontColor(ColorSecundario);
                row.AutoItem().Text($"S/ {valor:0.00}").FontSize(9).Bold().FontColor(ColorTexto);
            });
        }

        private async Task<byte[]?> ObtenerImagenAsync(string? imagenUrl)
        {
            if (string.IsNullOrWhiteSpace(imagenUrl))
            {
                return null;
            }

            var valor = imagenUrl.Trim();

            if (string.IsNullOrWhiteSpace(valor))
            {
                return null;
            }

            try
            {
                if (Uri.TryCreate(valor, UriKind.Absolute, out var uri) &&
                    (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                {
                    var imagenLocal = await ObtenerImagenLocalDesdeUrlAsync(uri);

                    if (imagenLocal != null)
                    {
                        return imagenLocal;
                    }

                    var client = _httpClientFactory.CreateClient();
                    client.Timeout = TimeSpan.FromSeconds(15);

                    using var request = new HttpRequestMessage(HttpMethod.Get, uri);

                    request.Headers.UserAgent.ParseAdd(
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"
                    );

                    request.Headers.Accept.ParseAdd("image/png");
                    request.Headers.Accept.ParseAdd("image/jpeg");
                    request.Headers.Accept.ParseAdd("*/*");

                    using var response = await client.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead
                    );

                    if (!response.IsSuccessStatusCode)
                    {
                        return null;
                    }

                    var bytes = await response.Content.ReadAsByteArrayAsync();

                    if (!EsImagenCompatible(bytes))
                    {
                        return null;
                    }

                    return bytes;
                }

                return await ObtenerImagenLocalAsync(valor);
            }
            catch
            {
                return null;
            }
        }
        private async Task<byte[]?> ObtenerImagenLocalDesdeUrlAsync(Uri uri)
        {
            if (!uri.IsLoopback &&
                !uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) &&
                !uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return await ObtenerImagenLocalAsync(uri.AbsolutePath);
        }

        private async Task<byte[]?> ObtenerImagenLocalAsync(string ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta))
            {
                return null;
            }

            var rutaLimpia = ruta
                .Split('?')[0]
                .Split('#')[0]
                .TrimStart('/')
                .Replace("/", Path.DirectorySeparatorChar.ToString());

            if (string.IsNullOrWhiteSpace(rutaLimpia))
            {
                return null;
            }

            var webRoot = _environment.WebRootPath ??
                          Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            var rutaFisica = Path.Combine(webRoot, rutaLimpia);

            if (!File.Exists(rutaFisica))
            {
                return null;
            }

            var archivoBytes = await File.ReadAllBytesAsync(rutaFisica);

            if (!EsImagenCompatible(archivoBytes))
            {
                return null;
            }

            return archivoBytes;
        }

        private bool EsContentTypeImagenCompatible(string? contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType))
            {
                return false;
            }

            contentType = contentType.ToLower();

            return contentType == "image/png" ||
                   contentType == "image/jpeg" ||
                   contentType == "image/jpg";
        }

        private bool EsImagenCompatible(byte[] bytes)
        {
            if (bytes.Length < 8)
            {
                return false;
            }

            var esPng =
                bytes[0] == 0x89 &&
                bytes[1] == 0x50 &&
                bytes[2] == 0x4E &&
                bytes[3] == 0x47 &&
                bytes[4] == 0x0D &&
                bytes[5] == 0x0A &&
                bytes[6] == 0x1A &&
                bytes[7] == 0x0A;

            var esJpg =
                bytes[0] == 0xFF &&
                bytes[1] == 0xD8;

            return esPng || esJpg;
        }

        private string? ObtenerRutaLogo()
        {
            var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            var rutas = new[]
            {
                Path.Combine(webRoot, "assets", "images", "logo-3s.png"),
                Path.Combine(webRoot, "images", "logo-3s.png"),
                Path.Combine(webRoot, "logo-3s.png")
            };

            return rutas.FirstOrDefault(File.Exists);
        }

        private class CotizacionPdfModel
        {
            public int IdCotizacion { get; set; }
            public DateTime FechaCotizacion { get; set; }
            public string EstadoCotizacion { get; set; } = string.Empty;
            public string OrigenCotizacion { get; set; } = string.Empty;
            public decimal Subtotal { get; set; }
            public decimal Descuento { get; set; }
            public decimal Igv { get; set; }
            public decimal Total { get; set; }
            public string? Observacion { get; set; }

            public EmpresaPdfModel Empresa { get; set; } = new();
            public ClientePdfModel Cliente { get; set; } = new();
            public List<DetallePdfModel> Detalles { get; set; } = new();
        }

        private class EmpresaPdfModel
        {
            public string NombreComercial { get; set; } = string.Empty;
            public string RazonSocial { get; set; } = string.Empty;
            public string? Ruc { get; set; }
            public string? Rubro { get; set; }
            public string? Correo { get; set; }
            public string? Telefono { get; set; }
            public string? Direccion { get; set; }
            public string? SitioWeb { get; set; }
        }

        private class ClientePdfModel
        {
            public string Nombre { get; set; } = string.Empty;
            public string? NombreComercial { get; set; }
            public bool EsEmpresa { get; set; }
            public string TipoCliente { get; set; } = string.Empty;
            public string TipoDocumento { get; set; } = string.Empty;
            public string NumeroDocumento { get; set; } = string.Empty;
            public string? Correo { get; set; }
            public string? Telefono { get; set; }
            public string? Direccion { get; set; }
        }

        private class DetallePdfModel
        {
            public string TipoElemento { get; set; } = string.Empty;
            public string Nombre { get; set; } = string.Empty;
            public string? Descripcion { get; set; }
            public string? ImagenUrl { get; set; }
            public byte[]? ImagenBytes { get; set; }
            public int Cantidad { get; set; }
            public decimal PrecioUnitario { get; set; }
            public decimal Subtotal { get; set; }
            public string? Observacion { get; set; }
        }
    }
}
