using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Sistema3S.Web.Services.Pdf;

/// <summary>Identidad común de cotizaciones, comprobantes y reportes comerciales.</summary>
public static class PdfEstilo3S
{
    public const string Rojo = "#CF1827";
    public const string Texto = "#172235";
    public const string Secundario = "#64748B";
    public const string Borde = "#E1E7EF";
    public const string Fondo = "#F4F6F9";

    public static void Encabezado(IContainer container, string? logo, string titulo, string numero,
        string fecha, string? empresa = null, string? contacto = null, string? direccion = null)
    {
        container.Background("#FFFFFF").Column(column =>
        {
            column.Spacing(4);
            column.Item().AlignCenter().Width(72).Height(44).Element(mark =>
            {
                if (!string.IsNullOrWhiteSpace(logo) && File.Exists(logo)) mark.Image(logo).FitArea();
                else mark.AlignCenter().AlignMiddle().Text("3S").FontSize(28).Bold().FontColor(Rojo);
            });
            column.Item().AlignCenter().Text(string.IsNullOrWhiteSpace(empresa)
                ? "3S (SERVICIO Y SOLUCIONES SUPERIORES S.A.C.)" : empresa)
                .FontSize(11).SemiBold().FontColor(Texto);
            column.Item().AlignCenter().Text(contacto ?? "cevallosindustrial@gmail.com  |  +51 948 327 667  |  3s-omega.vercel.app")
                .FontSize(7.5f).FontColor(Secundario);
            column.Item().AlignCenter().Text(direccion ?? "Av. Los Pinos 960 Urb. El Ermitaño, Independencia - Lima - Lima")
                .FontSize(7.5f).FontColor(Secundario);
            column.Item().PaddingTop(8).LineHorizontal(1.5f).LineColor(Rojo);
            column.Item().PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Text(titulo.ToUpperInvariant()).FontSize(12).SemiBold().FontColor(Texto);
                row.RelativeItem().AlignRight().Column(info =>
                {
                    info.Item().AlignRight().Text(numero).FontSize(12).SemiBold().FontColor(Texto);
                    info.Item().PaddingTop(3).AlignRight().Text(fecha).FontSize(8).FontColor(Secundario);
                });
            });
        });
    }

    public static void Pie(IContainer container, string texto)
    {
        container.PaddingTop(10).BorderTop(0.5f).BorderColor(Borde).PaddingTop(7).Row(row =>
        {
            row.RelativeItem().PaddingRight(12).Text(texto).FontSize(7).FontColor(Secundario);
            row.AutoItem().Text(text =>
            {
                text.DefaultTextStyle(style => style.FontSize(7).FontColor(Secundario));
                text.Span("Página "); text.CurrentPageNumber(); text.Span(" de "); text.TotalPages();
            });
        });
    }

    public static IContainer CabeceraTabla(IContainer container) => container
        .Background(Fondo).BorderBottom(1).BorderColor(Borde).PaddingVertical(8).PaddingHorizontal(6)
        .DefaultTextStyle(style => style.FontSize(8).SemiBold().FontColor(Texto));

    public static IContainer CuerpoTabla(IContainer container) => container
        .BorderBottom(0.5f).BorderColor(Borde).PaddingVertical(8).PaddingHorizontal(6);
}
