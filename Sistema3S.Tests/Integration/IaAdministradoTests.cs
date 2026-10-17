using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Sistema3S.Tests.Fixtures;
using Sistema3S.Web.Services.Ia;
namespace Sistema3S.Tests.Integration;
public sealed class IaAdministradoTests
{
 [Fact(DisplayName="PI-IA-ADM | Reinicio carga artefacto QA validado sin entrenar; agenda persiste y falla de Python se aísla")]
 public async Task ReinicioYAgenda()
 {
  // Artefacto entrenado realmente por test_engine.py sobre serie constante explícitamente QA.
  // Jamás se copia a BD_3S ni se presenta como aceptación predictiva del negocio.
  var fixture=Environment.GetEnvironmentVariable("S3S_QA_MODEL")??throw new InvalidOperationException("Ejecuta IA/motor/test_engine.py y configura S3S_QA_MODEL con su carpeta temporal de artefactos QA.");
  var python=Environment.GetEnvironmentVariable("S3S_QA_PYTHON")??throw new InvalidOperationException("Falta S3S_QA_PYTHON para la prueba de proceso Python real.");
  var project=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../"));
  var root=Path.Combine(Path.GetTempPath(),"Sistema3S-IA-QA-"+Guid.NewGuid().ToString("N"));var model=Guid.NewGuid();var path=Path.Combine(root,model.ToString("N"));Directory.CreateDirectory(path);
  foreach(var f in new[]{"manifest.json","network.keras"})File.Copy(Path.Combine(fixture,f),Path.Combine(path,f));var manifest=JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(path,"manifest.json")))!.AsObject();Assert.True(IaPublicacion.Aceptable(manifest));
  var initial=System.Security.Cryptography.SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(path,"network.keras")));
  await using var db=await QaDatabase.CreateAsync();await IaTests.Migrar(db);await db.Exec(await File.ReadAllTextAsync(Path.Combine(project,"Sistema3S.Web/Database/20261016_IA_MotorAdministrado.sql")));
  var hoy=IaReglas.Hoy;await db.Exec($"""
   DECLARE @i int=0,@id int;WHILE @i<600 BEGIN
    INSERT Venta(IdCliente,IdUsuarioRegistro,IdEstadoVenta,FechaVenta,Subtotal,Igv,Total) VALUES(1,1,1,DATEADD(day,@i-600,'{hoy:yyyyMMdd}'),90,16.2,106.2);SET @id=SCOPE_IDENTITY();
    INSERT DetalleVenta(IdVenta,IdElementoCatalogo,Cantidad,PrecioUnitario,Subtotal) VALUES(@id,1,3,35.4,106.2);SET @i+=1;END;
   UPDATE ia.Programacion SET ProximaUTC=DATEADD(day,10,SYSUTCDATETIME()),Estado='Pendiente';
   UPDATE ia.Programacion SET ProximaUTC=DATEADD(minute,-1,SYSUTCDATETIME()) WHERE Tipo='Inferencia';
   """);
  var raw=await File.ReadAllTextAsync(Path.Combine(path,"manifest.json"));var hash=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();
  await db.Exec($"INSERT ia.Modelo(Id,Version,Alcance,Corte,Huella,Artefacto,Evaluacion,Elegible,Activo) VALUES('{model}','3s-ia-5','production','{manifest["cutoff"]!.GetValue<string>()}','{hash}','{model:N}',N'{raw.Replace("'","''")}',1,1)");
  // El servicio ancla modelos en DatosPrivados/administrado; ubicar esta carpeta allí.
  var parent=Path.Combine(root,"datos");Directory.CreateDirectory(parent);Directory.Move(path,Path.Combine(parent,model.ToString("N")));Directory.Move(parent,Path.Combine(root,"administrado"));
  IConfiguration Cfg(string exe)=>new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"Ia:Habilitada","true"},{"Ia:HistorialOperativoVerificado","true"},{"Ia:Python",exe},{"Ia:DatosPrivados",root},{"Ia:Automatizacion:RevisionSegundos","5"}}).Build();
  async Task Esperar(string state){for(var i=0;i<120;i++){if(await db.Scalar($"SELECT COUNT(*) FROM ia.Trabajo WHERE Tipo='Inferencia' AND Estado='{state}'")>0)return;await Task.Delay(500);}throw new TimeoutException("No terminó la tarea IA QA: "+state);}
  await using(var api=await ApiHost.Start(db,Cfg(python))){await Esperar("PronosticosGuardados");Assert.True(await db.Scalar("SELECT COUNT(*) FROM ia.Pronostico")>=3);Assert.True(await db.Scalar("SELECT COUNT(*) FROM ia.Pronostico7")>=1);await ApiTests.Login(api);Assert.True((await api.Http.GetAsync("/api/ia/pronostico")).IsSuccessStatusCode);}
  await db.Exec("UPDATE ia.Programacion SET ProximaUTC=DATEADD(second,-1,SYSUTCDATETIME()) WHERE Tipo='Inferencia'");
  await using(var api=await ApiHost.Start(db,Cfg(python))){await Esperar("CargadoSinEntrenar");Assert.Equal(0,await db.Scalar("SELECT COUNT(*) FROM ia.Trabajo WHERE Tipo='Entrenamiento'"));}
  Assert.Equal(initial,System.Security.Cryptography.SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(root,"administrado",model.ToString("N"),"network.keras"))));
  await db.Exec("UPDATE ia.Programacion SET ProximaUTC=DATEADD(second,-1,SYSUTCDATETIME()) WHERE Tipo='Inferencia'");
  await using(var api=await ApiHost.Start(db,Cfg(Path.Combine(root,"inexistente.exe")))){await Esperar("Fallida");await ApiTests.Login(api);Assert.True((await api.Http.GetAsync("/api/ia/pronostico")).IsSuccessStatusCode);Assert.True((await api.Http.GetAsync("/api/inventario")).IsSuccessStatusCode);}
  Assert.Equal(600,await db.Scalar("SELECT COUNT(*) FROM Venta"));Assert.Equal(0,await db.Scalar("SELECT COUNT(*) FROM Compra"));Assert.Equal(0,await db.Scalar("SELECT COUNT(*) FROM MovimientoStock"));
 }
 [Fact(DisplayName="PI-IA-ORIGEN | Etiquetas de procedencia no cambian ventas y se excluyen sólo al preparar evaluación")]
 public async Task Procedencia()
 {
  await using var db=await QaDatabase.CreateAsync();await IaTests.Migrar(db);var project=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../"));var ddl=await File.ReadAllTextAsync(Path.Combine(project,"Sistema3S.Web/Database/20261016_IA_MotorAdministrado.sql"));await db.Exec(ddl);await db.Exec(ddl);
  await db.Exec("INSERT Venta(IdCliente,IdUsuarioRegistro,IdEstadoVenta,FechaVenta,Subtotal,Igv,Total) VALUES(1,1,1,'20260901',100,18,118),(1,1,1,'20260902',100,18,118);INSERT DetalleVenta(IdVenta,IdElementoCatalogo,Cantidad,PrecioUnitario,Subtotal) VALUES(1,1,2,59,118),(2,1,10,11.8,118);INSERT ia.OrigenOperacion(Tipo,IdOperacion,Origen,Evidencia) VALUES('Venta',2,'DesarrolloAsistido','QA')");
  var cfg=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"ConnectionStrings:BD_3S",db.Connection}}).Build();var data=await new IaDatosAdministrados(new Sistema3S.Web.Services.Bi.BiConexion(cfg)).Extraer(new(2026,10,15),default);Assert.Equal(2,data.Datos.Products.Sum(p=>p.Days.Sum(d=>d.Quantity)));Assert.Equal(2,await db.Scalar("SELECT COUNT(*) FROM Venta"));
 }
}
