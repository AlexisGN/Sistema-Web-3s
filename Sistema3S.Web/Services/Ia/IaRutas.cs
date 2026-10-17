namespace Sistema3S.Web.Services.Ia;

/// <summary>Las rutas relativas se anclan a Sistema3S.Web, nunca al directorio de la terminal.</summary>
public static class IaRutas
{
    public static string Resolver(string value,IWebHostEnvironment env) =>
        Path.GetFullPath(value,Path.GetFullPath(env.ContentRootPath));
    public static string Motor(IWebHostEnvironment env) => Resolver("../tools/ia/motor.py",env);
    public static string Python(IConfiguration config,IWebHostEnvironment env) => Resolver(
        string.IsNullOrWhiteSpace(config["Ia:Python"])?
            (OperatingSystem.IsWindows()?"../IA/.venv/Scripts/python.exe":"../IA/.venv/bin/python"):
            config["Ia:Python"]!,env);
    public static string Modelos(IConfiguration config,IWebHostEnvironment env) => Resolver(
        string.IsNullOrWhiteSpace(config["Ia:DatosPrivados"])?"../IA/modelos":config["Ia:DatosPrivados"]!,env);
}
