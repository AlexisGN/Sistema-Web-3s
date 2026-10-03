using Sistema3S.Web.Services.Pdf;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Sistema3S.Web.DTOs.Venta;
using Sistema3S.Web.Services.Interfaces;

namespace Sistema3S.Web.Services.Implementations
{
    public class PdfVentaService : IPdfVentaService
    {
        private readonly IVentaService _ventaService;
        private readonly IWebHostEnvironment _environment;

        public PdfVentaService(
            IVentaService ventaService,
            IWebHostEnvironment environment
        )
        {
            _ventaService = ventaService;
            _environment = environment;
        }

        public async Task<byte[]> GenerarPdfVentaAsync(int idVenta)
        {
            var venta = await _ventaService.ObtenerDetalleAsync(idVenta);

            if (venta == null)
            {
                throw new InvalidOperationException("No se encontró la venta seleccionada.");
            }

            var logoPath = ObtenerRutaLogo();

            using var stream = new MemoryStream();

            Document.Create(document =>
            {
                // =========================================================
                // HOJA 1: COMPROBANTE INDIVIDUAL PARA EL CLIENTE
                // =========================================================
                document.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(32);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(container =>
                    {
                        DibujarEncabezadoComprobante(container, venta, logoPath);
                    });

                    page.Content().PaddingTop(14).Column(column =>
                    {
                        column.Spacing(14);



                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Element(container =>
                            {
                                DibujarCajaDatos(
                                    container,
                                    "Datos del cliente",
                                    new List<(string Label, string Value)>
                                    {
                                        ("Cliente", ValorTexto(venta.Cliente)),
                                        ("Tipo documento", ValorTexto(venta.TipoDocumentoCliente)),
                                        ("Documento", ValorTexto(venta.DocumentoCliente)),
                                        ("Teléfono", ValorTexto(venta.TelefonoCliente)),
                                        ("Correo", ValorTexto(venta.CorreoCliente)),
                                        ("Dirección", ValorTexto(venta.DireccionCliente))
                                    }
                                );
                            });

                            row.ConstantItem(12);

                            row.RelativeItem().Element(container =>
                            {
                                DibujarCajaDatos(
                                    container,
                                    "Datos del comprobante",
                                    new List<(string Label, string Value)>
                                    {
                                        ("Operación N°", venta.IdVenta.ToString()),
                                        ("Comprobante", ValorTexto(venta.TipoComprobante)),
                                        ("Documento", ValorTexto(venta.DocumentoCompleto)),
                                        ("Fecha emisión", venta.FechaEmision.ToString("dd/MM/yyyy")),
                                        ("Fecha venta", venta.FechaVenta.ToString("dd/MM/yyyy HH:mm")),
                                        ("Estado de pago", FormatearEstadoPagoCliente(venta.EstadoPago))
                                    }
                                );
                            });
                        });

                        column.Item().Element(container =>
                        {
                            DibujarDetalleVenta(container, venta);
                        });

                        column.Item().Element(container =>
                        {
                            DibujarTotalesCliente(container, venta);
                        });

                        if (!string.IsNullOrWhiteSpace(venta.Observacion))
                        {
                            column.Item().Element(container =>
                            {
                                container
                                    .Border(1)
                                    .BorderColor("#D7DEE7")
                                    .Padding(10)
                                    .Column(col =>
                                    {
                                        col.Item().Text("Observaciones")
                                            .Bold()
                                            .FontSize(11)
                                            .FontColor("#07111B");

                                        col.Item().PaddingTop(4)
                                            .Text(venta.Observacion!)
                                            .FontSize(10)
                                            .FontColor("#334155");
                                    });
                            });
                        }
                    });

                    page.Footer().Element(c => PdfEstilo3S.Pie(c, "Empresa 3S · Documento de venta y control comercial."));
                });

                // =========================================================
                // HOJA 2: SOLO SI HAY CUOTAS, PARCIAL O SALDO PENDIENTE
                // =========================================================
                if (DebeGenerarHojaPagos(venta))
                {
                    document.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(32);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(9));

                        page.Header().Element(container =>
                        {
                            DibujarEncabezadoComprobanteCompacto(container, venta, logoPath);
                        });

                        page.Content().PaddingTop(10).Column(column =>
                        {
                            column.Spacing(10);

                            column.Item().Text("Detalle de pagos y cronograma")
                                .FontSize(16)
                                .Bold()
                                .FontColor("#07111B");

                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Element(container =>
                                {
                                    DibujarResumenPagoCard(
                                        container,
                                        "Total",
                                        FormatearMoneda(venta.Total),
                                        false
                                    );
                                });

                                row.ConstantItem(8);

                                row.RelativeItem().Element(container =>
                                {
                                    DibujarResumenPagoCard(
                                        container,
                                        "Pagado",
                                        FormatearMoneda(venta.TotalPagado),
                                        false
                                    );
                                });

                                row.ConstantItem(8);

                                row.RelativeItem().Element(container =>
                                {
                                    DibujarResumenPagoCard(
                                        container,
                                        "Saldo pendiente",
                                        FormatearMoneda(venta.SaldoPendiente),
                                        venta.SaldoPendiente > 0
                                    );
                                });
                            });

                            column.Item().Element(container =>
                            {
                                container
                                    .Border(1)
                                    .BorderColor("#D7DEE7")
                                    .Padding(8)
                                    .Column(col =>
                                    {
                                        col.Spacing(2);

                                        col.Item().Text($"Cliente: {venta.Cliente}")
                                            .FontSize(9)
                                            .Bold();

                                        col.Item().Text($"Comprobante: {venta.DocumentoCompleto}")
                                            .FontSize(9);

                                        col.Item().Text($"Estado de pago: {FormatearEstadoPagoCliente(venta.EstadoPago)}")
                                            .FontSize(9)
                                            .FontColor("#D81920")
                                            .Bold();
                                    });
                            });

                            if (venta.Pagos.Count > 0)
                            {
                                column.Item().Text("Pagos realizados")
                                    .FontSize(12)
                                    .Bold()
                                    .FontColor("#07111B");

                                column.Item().Element(container =>
                                {
                                    DibujarTablaPagos(container, venta);
                                });
                            }

                            if (venta.Cuotas.Count > 0)
                            {
                                column.Item().Text("Cuotas programadas")
                                    .FontSize(12)
                                    .Bold()
                                    .FontColor("#07111B");

                                column.Item().Element(container =>
                                {
                                    DibujarTablaCuotas(container, venta);
                                });
                            }
                            else if (venta.SaldoPendiente > 0)
                            {
                                column.Item().Element(container =>
                                {
                                    container
                                        .Border(1)
                                        .BorderColor("#D7DEE7")
                                        .Padding(8)
                                        .Column(col =>
                                        {
                                            col.Item().Text("Saldo pendiente por cancelar")
                                                .Bold()
                                                .FontSize(10)
                                                .FontColor("#07111B");

                                            col.Item().PaddingTop(4)
                                                .Text($"El cliente mantiene un saldo pendiente de {FormatearMoneda(venta.SaldoPendiente)}.")
                                                .FontSize(9)
                                                .FontColor("#334155");
                                        });
                                });
                            }
                        });

                        page.Footer().Element(c => PdfEstilo3S.Pie(c, "Empresa 3S · Documento de venta y control comercial."));
                    });
                }
            }).GeneratePdf(stream);

            return stream.ToArray();
        }

        // =========================================================
        // REPORTE PDF DE VENTAS - SE MANTIENE COMO REPORTE INTERNO
        // =========================================================
        public byte[] GenerarPdfReporteVentas(
            IEnumerable<ReporteVentaDto> ventas,
            DateTime? fechaInicio,
            DateTime? fechaFin
        )
        {
            var lista = ventas.ToList();
            var logoPath = ObtenerRutaLogo();

            using var memoryStream = new MemoryStream();

            Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(28);
                    page.DefaultTextStyle(style => style.FontSize(8));

                    page.Header().Element(c => PdfEstilo3S.Encabezado(c, logoPath, "Reporte de ventas", $"{lista.Count} registros", ObtenerTextoRango(fechaInicio, fechaFin)));

                    page.Content().PaddingTop(14).Column(col =>
                    {
                        col.Spacing(10);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(34);
                                columns.ConstantColumn(80);
                                columns.ConstantColumn(85);
                                columns.RelativeColumn();
                                columns.ConstantColumn(80);
                                columns.ConstantColumn(70);
                                columns.ConstantColumn(70);
                                columns.ConstantColumn(70);
                                columns.ConstantColumn(70);
                                columns.ConstantColumn(70);
                                columns.ConstantColumn(72);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderCell).Text("ID");
                                header.Cell().Element(HeaderCell).Text("Fecha");
                                header.Cell().Element(HeaderCell).Text("Documento");
                                header.Cell().Element(HeaderCell).Text("Cliente");
                                header.Cell().Element(HeaderCell).Text("Origen");
                                header.Cell().Element(HeaderCell).AlignRight().Text("Subtotal sin IGV");
                                header.Cell().Element(HeaderCell).AlignRight().Text("IGV");
                                header.Cell().Element(HeaderCell).AlignRight().Text("Total");
                                header.Cell().Element(HeaderCell).AlignRight().Text("Pagado");
                                header.Cell().Element(HeaderCell).AlignRight().Text("Saldo");
                                header.Cell().Element(HeaderCell).Text("Estado");
                            });

                            foreach (var item in lista)
                            {
                                table.Cell().Element(BodyCell).Text(item.IdVenta.ToString());
                                table.Cell().Element(BodyCell).Text(item.FechaVenta.ToString("dd/MM/yyyy"));
                                table.Cell().Element(BodyCell).Text(item.DocumentoCompleto);
                                table.Cell().Element(BodyCell).Text(item.Cliente);
                                table.Cell().Element(BodyCell).Text(item.OrigenVenta);
                                table.Cell().Element(BodyCell).AlignRight().Text($"S/ {item.Subtotal:N2}");
                                table.Cell().Element(BodyCell).AlignRight().Text($"S/ {item.Igv:N2}");
                                table.Cell().Element(BodyCell).AlignRight().Text($"S/ {item.Total:N2}");
                                table.Cell().Element(BodyCell).AlignRight().Text($"S/ {item.TotalPagado:N2}");
                                table.Cell().Element(BodyCell).AlignRight().Text($"S/ {item.SaldoPendiente:N2}");
                                table.Cell().Element(BodyCell).Text(item.EstadoPago);
                            }
                        });

                        col.Item().Row(row =>
                        {
                            row.RelativeItem();

                            row.ConstantItem(300).Column(totals =>
                            {
                                AgregarLineaTotal(totals, "Total ventas", lista.Sum(x => x.Total), true);
                                AgregarLineaTotal(totals, "Total pagado", lista.Sum(x => x.TotalPagado), false);
                                AgregarLineaTotal(totals, "Saldo pendiente", lista.Sum(x => x.SaldoPendiente), false);
                            });
                        });
                    });

                    page.Footer().Element(c => PdfEstilo3S.Pie(c, "Empresa 3S · Documento de venta y control comercial."));
                });
            }).GeneratePdf(memoryStream);

            return memoryStream.ToArray();
        }

        // =========================================================
        // REGLAS
        // =========================================================
        private static bool DebeGenerarHojaPagos(VentaDetalleCompletoDto venta)
        {
            var estadoPago = venta.EstadoPago?.Trim().ToLower() ?? string.Empty;

            return venta.Cuotas.Count > 0
                   || venta.SaldoPendiente > 0
                   || estadoPago.Contains("cuota")
                   || estadoPago.Contains("parcial");
        }

        // =========================================================
        // ENCABEZADOS
        // =========================================================
        private void DibujarEncabezadoComprobante(
            IContainer container,
            VentaDetalleCompletoDto venta,
            string? logoPath
        )
        {
            PdfEstilo3S.Encabezado(container, logoPath, venta.TipoComprobante ?? "Comprobante de venta", venta.DocumentoCompleto, $"Fecha: {venta.FechaEmision:dd/MM/yyyy}");
        }

        private void DibujarEncabezadoComprobanteCompacto(
            IContainer container,
            VentaDetalleCompletoDto venta,
            string? logoPath
        )
        {
            PdfEstilo3S.Encabezado(container, logoPath, "Detalle de pagos", venta.DocumentoCompleto, $"Fecha: {venta.FechaEmision:dd/MM/yyyy}");
        }

        // =========================================================
        // BLOQUES COMPROBANTE CLIENTE
        // =========================================================
        private static void DibujarCajaDatos(
    IContainer container,
    string titulo,
    List<(string Label, string Value)> datos
)
        {
            container
                .Border(1)
                .BorderColor("#D7DEE7")
                .Padding(10)
                .Column(column =>
                {
                    column.Spacing(4);

                    column.Item().Text(titulo)
                        .Bold()
                        .FontSize(12)
                        .FontColor("#07111B");

                    foreach (var dato in datos)
                    {
                        column.Item().Text(text =>
                        {
                            text.Span($"{dato.Label}: ")
                                .Bold()
                                .FontSize(9.5f)
                                .FontColor("#111827");

                            text.Span(dato.Value)
                                .FontSize(9.5f)
                                .FontColor("#111827");
                        });
                    }
                });
        }

        private static void DibujarResumenPagoCard(
            IContainer container,
            string titulo,
            string valor,
            bool destacado
        )
        {
            container
                .Border(1)
                .BorderColor(destacado ? "#D81920" : "#D7DEE7")
                .Background(destacado ? "#FFF5F5" : "#F8FAFC")
                .Padding(8)
                .Column(column =>
                {
                    column.Item().Text(titulo)
                        .FontSize(8)
                        .Bold()
                        .FontColor(destacado ? "#D81920" : "#64748B");

                    column.Item().Text(valor)
                        .FontSize(11)
                        .Bold()
                        .FontColor(destacado ? "#D81920" : "#07111B");
                });
        }

        private void DibujarDetalleVenta(
            IContainer container,
            VentaDetalleCompletoDto venta
        )
        {
            container.Column(column =>
            {
                column.Spacing(8);

                column.Item().Text("Detalle de productos y servicios")
                    .Bold()
                    .FontSize(12)
                    .FontColor("#07111B");

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(70);
                        columns.RelativeColumn(2.4f);
                        columns.ConstantColumn(70);
                        columns.ConstantColumn(50);
                        columns.ConstantColumn(90);
                        columns.ConstantColumn(95);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCell).Text("Código");
                        header.Cell().Element(HeaderCell).Text("Descripción");
                        header.Cell().Element(HeaderCell).Text("Tipo");
                        header.Cell().Element(HeaderCell).AlignCenter().Text("Cant.");
                        header.Cell().Element(HeaderCell).AlignRight().Text("P. Unit.");
                        header.Cell().Element(HeaderCell).AlignRight().Text("Importe");
                    });

                    foreach (var item in venta.Detalles)
                    {
                        var codigo = item.CodigoProducto;

                        if (string.IsNullOrWhiteSpace(codigo) && item.IdServicio.HasValue)
                        {
                            codigo = $"SV-{item.IdServicio.Value}";
                        }

                        table.Cell().Element(BodyCell).Text(codigo ?? "-");
                        table.Cell().Element(BodyCell).Text(item.Elemento);
                        table.Cell().Element(BodyCell).Text(item.TipoElemento);
                        table.Cell().Element(BodyCell).AlignCenter().Text(item.Cantidad.ToString());
                        table.Cell().Element(BodyCell).AlignRight().Text(FormatearMoneda(item.PrecioUnitario));
                        table.Cell().Element(BodyCell).AlignRight().Text(FormatearMoneda(item.Subtotal));
                    }
                });
            });
        }

        private void DibujarTotalesCliente(
            IContainer container,
            VentaDetalleCompletoDto venta
        )
        {
            container.AlignRight().Width(300).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.ConstantColumn(110);
                });

                table.Cell().Element(BodyCellNoBorder).Text("Subtotal sin IGV");
                table.Cell().Element(BodyCellNoBorder).AlignRight().Text(FormatearMoneda(venta.Subtotal));

                table.Cell().Element(BodyCellNoBorder).Text("IGV 18%");
                table.Cell().Element(BodyCellNoBorder).AlignRight().Text(FormatearMoneda(venta.Igv));

                table.Cell().Element(BodyCellTotalLabel).Text("Total");
                table.Cell().Element(BodyCellTotalValue).AlignRight().Text(FormatearMoneda(venta.Total));

                table.Cell().Element(BodyCellNoBorder).Text("Pagado");
                table.Cell().Element(BodyCellNoBorder).AlignRight().Text(FormatearMoneda(venta.TotalPagado));

                table.Cell().Element(BodyCellNoBorder).Text("Saldo pendiente");
                table.Cell().Element(BodyCellNoBorder).AlignRight().Text(FormatearMoneda(venta.SaldoPendiente));
            });
        }

        private void DibujarTablaPagos(
            IContainer container,
            VentaDetalleCompletoDto venta
        )
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.ConstantColumn(100);
                    columns.ConstantColumn(100);
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("Método");
                    header.Cell().Element(HeaderCell).Text("Fecha");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Monto");
                });

                foreach (var pago in venta.Pagos.OrderBy(x => x.FechaPago))
                {
                    table.Cell().Element(BodyCell).Text(pago.MetodoPago);
                    table.Cell().Element(BodyCell).Text(pago.FechaPago.ToString("dd/MM/yyyy"));
                    table.Cell().Element(BodyCell).AlignRight().Text(FormatearMoneda(pago.MontoPagado));
                }
            });
        }

        private void DibujarTablaCuotas(
            IContainer container,
            VentaDetalleCompletoDto venta
        )
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(55);
                    columns.ConstantColumn(90);
                    columns.ConstantColumn(90);
                    columns.ConstantColumn(90);
                    columns.ConstantColumn(90);
                    columns.ConstantColumn(80);
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("Cuota");
                    header.Cell().Element(HeaderCell).Text("Vencimiento");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Monto");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Pagado");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Saldo");
                    header.Cell().Element(HeaderCell).Text("Estado");
                });

                foreach (var cuota in venta.Cuotas.OrderBy(x => x.NumeroCuota))
                {
                    var saldoCuota = Math.Max(0, cuota.MontoCuota - cuota.MontoPagado);

                    table.Cell().Element(BodyCell).Text(cuota.NumeroCuota.ToString());
                    table.Cell().Element(BodyCell).Text(cuota.FechaVencimiento.ToString("dd/MM/yyyy"));
                    table.Cell().Element(BodyCell).AlignRight().Text(FormatearMoneda(cuota.MontoCuota));
                    table.Cell().Element(BodyCell).AlignRight().Text(FormatearMoneda(cuota.MontoPagado));
                    table.Cell().Element(BodyCell).AlignRight().Text(FormatearMoneda(saldoCuota));
                    table.Cell().Element(BodyCell).Text(FormatearEstadoPagoCliente(cuota.EstadoCuota));
                }
            });
        }

        // =========================================================
        // HELPERS
        // =========================================================
        private string? ObtenerRutaLogo()
        {
            var posiblesRutas = new List<string>();

            if (!string.IsNullOrWhiteSpace(_environment.WebRootPath))
            {
                posiblesRutas.Add(Path.Combine(_environment.WebRootPath, "assets", "images", "logo-3s.png"));
                posiblesRutas.Add(Path.Combine(_environment.WebRootPath, "images", "logo-3s.png"));
            }

            if (!string.IsNullOrWhiteSpace(_environment.ContentRootPath))
            {
                posiblesRutas.Add(Path.Combine(_environment.ContentRootPath, "wwwroot", "assets", "images", "logo-3s.png"));
                posiblesRutas.Add(Path.Combine(_environment.ContentRootPath, "wwwroot", "images", "logo-3s.png"));
            }

            posiblesRutas.Add(Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "assets",
                "images",
                "logo-3s.png"
            ));

            posiblesRutas.Add(Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "images",
                "logo-3s.png"
            ));

            return posiblesRutas.FirstOrDefault(File.Exists);
        }

        private static string FormatearMoneda(decimal valor)
        {
            return $"S/ {valor:N2}";
        }

        private static string ValorTexto(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? "-" : valor.Trim();
        }

        private static string FormatearEstadoPagoCliente(string? estadoPago)
        {
            if (string.IsNullOrWhiteSpace(estadoPago))
            {
                return "-";
            }

            var estado = estadoPago.Trim();

            if (estado.Equals("Pagada", StringComparison.OrdinalIgnoreCase))
            {
                return "Pagado";
            }

            if (estado.Equals("Parcial", StringComparison.OrdinalIgnoreCase))
            {
                return "Parcial";
            }

            if (estado.Equals("Pendiente", StringComparison.OrdinalIgnoreCase))
            {
                return "Pendiente";
            }

            if (estado.Equals("En cuotas", StringComparison.OrdinalIgnoreCase))
            {
                return "En cuotas";
            }

            if (estado.Equals("Anulada", StringComparison.OrdinalIgnoreCase))
            {
                return "Anulada";
            }

            return estado;
        }

        private static string ObtenerTextoRango(DateTime? fechaInicio, DateTime? fechaFin)
        {
            if (fechaInicio.HasValue && fechaFin.HasValue)
            {
                return $"Del {fechaInicio.Value:dd/MM/yyyy} al {fechaFin.Value:dd/MM/yyyy}";
            }

            if (fechaInicio.HasValue)
            {
                return $"Desde {fechaInicio.Value:dd/MM/yyyy}";
            }

            if (fechaFin.HasValue)
            {
                return $"Hasta {fechaFin.Value:dd/MM/yyyy}";
            }

            return "Todas las ventas registradas";
        }

        private static void AgregarLineaTotal(
            ColumnDescriptor totals,
            string label,
            decimal value,
            bool destacado
        )
        {
            totals.Item()
                .BorderBottom(1)
                .BorderColor("#E5E7EB")
                .PaddingVertical(5)
                .Row(row =>
                {
                    row.RelativeItem().Text(text =>
                    {
                        var textoLabel = text.Span(label)
                            .FontSize(destacado ? 13 : 9)
                            .FontColor(destacado ? "#D81920" : "#111827");

                        if (destacado)
                        {
                            textoLabel.Bold();
                        }
                    });

                    row.AutoItem().Text(text =>
                    {
                        text.Span($"S/ {value:N2}")
                            .FontSize(destacado ? 13 : 9)
                            .Bold()
                            .FontColor(destacado ? "#D81920" : "#111827");
                    });
                });
        }

        private static IContainer HeaderCell(IContainer container)
        {
            return PdfEstilo3S.CabeceraTabla(container);
        }

        private static IContainer BodyCell(IContainer container)
        {
            return PdfEstilo3S.CuerpoTabla(container);
        }

        private static IContainer BodyCellNoBorder(IContainer container)
        {
            return container
                .PaddingVertical(4)
                .PaddingHorizontal(6);
        }

        private static IContainer BodyCellTotalLabel(IContainer container)
        {
            return container
                .BorderTop(1)
                .BorderColor("#D7DEE7")
                .PaddingVertical(6)
                .PaddingHorizontal(6);
        }

        private static IContainer BodyCellTotalValue(IContainer container)
        {
            return container
                .BorderTop(1)
                .BorderColor("#D7DEE7")
                .PaddingVertical(6)
                .PaddingHorizontal(6)
                .DefaultTextStyle(x => x.Bold().FontColor("#D81920").FontSize(11));
        }
    }
}