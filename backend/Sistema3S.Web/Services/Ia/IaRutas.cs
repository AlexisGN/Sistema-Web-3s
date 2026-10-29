namespace Sistema3S.Web.Services.Ia;

/// <summary>Rutas ancladas al contenido de la API, tanto en fuentes como en una publicación estándar.</summary>
public static class IaRutas
{
    public static string Resolver(string value,IWebHostEnvironment env) =>
        Path.GetFullPath(value,Path.GetFullPath(env.ContentRootPath));

    public static string Directorio(IWebHostEnvironment env) =>
        File.Exists(Resolver("IA/motor/v8/cli.py",env)) ? Resolver("IA",env) : Resolver("../IA",env);

    public static string Motor(IWebHostEnvironment env) => Path.Combine(Directorio(env),"compatibilidad","motor.py");

    public static string Python(IConfiguration config,IWebHostEnvironment env) =>
        !string.IsNullOrWhiteSpace(config["Ia:Python"]) ? Resolver(config["Ia:Python"]!,env)
        : Path.Combine(Directorio(env),".venv",OperatingSystem.IsWindows()?"Scripts":"bin",OperatingSystem.IsWindows()?"python.exe":"python");

    public static string Modelos(IConfiguration config,IWebHostEnvironment env) =>
        !string.IsNullOrWhiteSpace(config["Ia:DatosPrivados"]) ? Resolver(config["Ia:DatosPrivados"]!,env)
        : Path.Combine(Directorio(env),"modelos");
}
