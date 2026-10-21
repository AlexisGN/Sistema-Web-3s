using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Mvc;
using Sistema3S.Web.Controllers;
using Sistema3S.Web.Services.Ia;

namespace Sistema3S.Tests.Unit;

public sealed class IaPortabilidadTests
{
    private sealed class Entorno(string root):IWebHostEnvironment
    {
        public string EnvironmentName{get;set;}="Testing";
        public string ApplicationName{get;set;}="Sistema3S";
        public string ContentRootPath{get;set;}=root;
        public IFileProvider ContentRootFileProvider{get;set;}=new NullFileProvider();
        public string WebRootPath{get;set;}=Path.Combine(root,"wwwroot");
        public IFileProvider WebRootFileProvider{get;set;}=new NullFileProvider();
    }
    [Fact(DisplayName="PU-IA-005 | Un clon en otra carpeta resuelve Python y modelos respecto al proyecto")]
    public void ClonIndependiente(){
        var project=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"Colaborador carpeta con espacios","Sistema3S"));
        var env=new Entorno(Path.Combine(project,"Sistema3S.Web"));
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{
            ["Ia:Python"]="../IA/.venv/Scripts/python.exe",["Ia:DatosPrivados"]="../IA/modelos"}).Build();
        Assert.Equal(Path.Combine(project,"IA","modelos"),IaRutas.Modelos(config,env));
        Assert.Equal(Path.Combine(project,"IA",".venv","Scripts","python.exe"),IaRutas.Python(config,env));
        Assert.Equal(Path.Combine(project,"tools","ia","motor.py"),IaRutas.Motor(env));
        var empty=new ConfigurationBuilder().Build();
        Assert.Equal(Path.Combine(project,"IA","modelos"),IaRutas.Modelos(empty,env));
        Assert.Equal(Path.Combine(project,"IA",".venv",OperatingSystem.IsWindows()?"Scripts":"bin",OperatingSystem.IsWindows()?"python.exe":"python"),IaRutas.Python(empty,env));
    }
    [Fact(DisplayName="PU-IA-006 | Se conservan rutas absolutas explícitas para instalaciones de servidor")]
    public void ConfiguracionExplicita(){
        var dir=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"Sistema3S-Servidor"));
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{
            ["Ia:Python"]=Path.Combine(dir,"python.exe"),["Ia:DatosPrivados"]=Path.Combine(dir,"modelos")}).Build();
        var env=new Entorno(Path.Combine(dir,"Web"));
        Assert.Equal(Path.Combine(dir,"python.exe"),IaRutas.Python(config,env));
        Assert.Equal(Path.Combine(dir,"modelos"),IaRutas.Modelos(config,env));
    }
    [Fact(DisplayName="PU-IA-007 | El simulador retirado no expone un endpoint HTTP")]
    public void SinSimulador(){
        var endpoints=typeof(IaController).GetMethods().SelectMany(m=>m.GetCustomAttributes(typeof(HttpPostAttribute),true).Cast<HttpPostAttribute>());
        Assert.DoesNotContain(endpoints,a=>a.Template=="escenario");
        Assert.Null(typeof(IaService).GetMethod("Escenario"));
    }
}
