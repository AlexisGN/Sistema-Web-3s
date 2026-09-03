namespace Sistema3S.Web.Services.Seguridad;

public static class ClienteWebConfiguracion
{
    public static void AplicarValoresLocales(IConfiguration configuration, IHostEnvironment environment)
    {
        if (environment.IsDevelopment() && string.IsNullOrWhiteSpace(configuration["ClienteWeb:UrlPublica"]))
            configuration["ClienteWeb:UrlPublica"] = "http://localhost:4200";
    }
}
