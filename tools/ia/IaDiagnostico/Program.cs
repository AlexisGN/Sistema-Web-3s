using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using Sistema3S.Web.Services.Bi;
using Sistema3S.Web.Services.Ia;

// Herramienta privada, fuera de HTTP. Extraer es de sólo lectura; ejecutar escribe sólo ia.
var mode=args.ElementAtOrDefault(0)??"extraer";
var output=args.ElementAtOrDefault(1)??throw new ArgumentException("Indica el archivo de evidencia dentro del entorno de trabajo.");
var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../../Sistema3S.Web"));
var connection=Environment.GetEnvironmentVariable("S3S_IA_CONEXION")??throw new InvalidOperationException("Configura la conexión sólo en el proceso.");
var settings=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{
 ["ConnectionStrings:BD_3S"]=connection,["Ia:Python"]=Environment.GetEnvironmentVariable("S3S_IA_PYTHON"),
 ["Ia:DatosPrivados"]=Environment.GetEnvironmentVariable("S3S_IA_DATOS"),["Ia:ReferenciaTecnica"]="true",["Ia:HistorialOperativoVerificado"]="false"
}).Build();
var con=new BiConexion(settings);var data=new IaDatos(con);
if(mode=="extraer"){
 var input=await data.Extraer("referencia",IaReglas.Hoy.AddDays(-1),default);
 await File.WriteAllTextAsync(output,JsonSerializer.Serialize(input,IaMotor.Json));
 Console.WriteLine($"Agregados anónimos extraídos: {input.Products.Count} productos. Sin escrituras SQL.");
}else if(mode=="ejecutar"){
 var env=new PrivateEnvironment(root);var motor=new IaMotor(settings,env,NullLogger<IaMotor>.Instance);
 var process=new IaProceso(con,data,motor,settings);var service=new IaService(con,data,settings,env);
 await process.Ejecutar("referencia",default);await service.ActualizarAvisos("referencia",default);
 var result=new Dictionary<string,object>();foreach(var view in new[]{"resumen","pronostico","proyeccion","inventario","compras","seguimiento"})result[view]=await service.Consultar(view,"referencia",null,default);
 await File.WriteAllTextAsync(output,JsonSerializer.Serialize(result,IaMotor.Json));Console.WriteLine("Referencia técnica procesada. Las operaciones comerciales no fueron modificadas.");
}else throw new ArgumentException("Modo no disponible.");

sealed class PrivateEnvironment(string root):IWebHostEnvironment{
 public string EnvironmentName{get;set;}="Development";public string ApplicationName{get;set;}="Sistema3S";
 public string ContentRootPath{get;set;}=root;public IFileProvider ContentRootFileProvider{get;set;}=new NullFileProvider();
 public string WebRootPath{get;set;}=Path.Combine(root,"wwwroot");public IFileProvider WebRootFileProvider{get;set;}=new NullFileProvider();
}
