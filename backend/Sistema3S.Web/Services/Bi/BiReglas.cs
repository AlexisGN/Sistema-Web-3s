namespace Sistema3S.Web.Services.Bi;

public static class BiReglas
{
    public static DateOnly HoyPeru() => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
        TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "SA Pacific Standard Time" : "America/Lima")));
    public static decimal? Variacion(decimal actual, decimal anterior) => anterior == 0 ? null : decimal.Round((actual-anterior)*100/anterior,2);
    public static decimal? Cociente(decimal numerador, decimal denominador) => denominador <= 0 ? null : decimal.Round(numerador/denominador,4);
    public static string PeriodoSql(string agrupacion, string campo="Fecha") => agrupacion switch {
        "dia" => $"CONVERT(date,{campo})",
        "semana" => $"DATEADD(day,-((DATEDIFF(day,'19000101',{campo})%7+7)%7),CONVERT(date,{campo}))",
        "anio" => $"DATEFROMPARTS(YEAR({campo}),1,1)",
        "mes" => $"DATEFROMPARTS(YEAR({campo}),MONTH({campo}),1)",
        _ => throw new ArgumentException("Agrupación no válida.")
    };
}
