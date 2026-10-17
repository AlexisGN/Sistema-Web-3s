using Sistema3S.Web.DTOs.Bi;
namespace Sistema3S.Web.Services.Bi;

public sealed record BiResultado(string Vista, BiFiltro Filtros, DateOnly? CompararDesde, DateOnly? CompararHasta,
    DateTimeOffset GeneradoUtc, string Moneda, string ZonaHoraria,
    IReadOnlyDictionary<string,List<Dictionary<string,object?>>> Datos, IReadOnlyList<string> Notas)
{
    public IReadOnlyList<BiIndicador> Indicadores { get; init; } = [];
    public IReadOnlyList<BiHallazgo> Hallazgos { get; init; } = [];
}
public sealed record BiIndicador(string Etiqueta,decimal? Valor,string Formato,string Descripcion);
public sealed record BiHallazgo(string Titulo,string Evidencia,string Interpretacion,string AccionSugerida,string ImpactoEsperado);

public interface IBiService
{
    Task<BiResultado> ConsultarAsync(string vista, BiFiltro filtro, CancellationToken ct);
    Task<Dictionary<string,List<Dictionary<string,object?>>>> OpcionesAsync(string procedencia,string? buscar,CancellationToken ct);
    Task<List<Dictionary<string,object?>>> DetalleAsync(string tipo,int id,CancellationToken ct);
}
