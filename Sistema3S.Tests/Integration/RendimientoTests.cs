using System.Diagnostics;
using System.Text.Json;
using Sistema3S.Tests.Fixtures;

namespace Sistema3S.Tests.Integration;
[Trait("Category","Performance")]
public class RendimientoTests
{
    [Fact(DisplayName="PC-C1-001 | Seis consultas HTTP locales: 20 mediciones y umbral 3000 ms")]
    public async Task Consultas(){await using var q=await QaDatabase.CreateAsync();await NegocioTests.Abrir(q);await using var api=await ApiHost.Start(q);await ApiTests.Login(api);var results=new List<object>();foreach(var ruta in new[]{"/api/publico/productos","/api/cliente","/api/venta","/api/compra","/api/inventario","/api/caja/resumen?idUsuario=1"}){for(int i=0;i<2;i++){var warm=await api.Http.GetAsync(ruta);Assert.True(warm.IsSuccessStatusCode,$"Warmup {ruta}: {warm.StatusCode}");}var tiempos=new List<double>();for(int i=0;i<20;i++){var watch=Stopwatch.StartNew();var r=await api.Http.GetAsync(ruta);await r.Content.ReadAsByteArrayAsync();watch.Stop();Assert.True(r.IsSuccessStatusCode,$"{ruta}: {r.StatusCode}");tiempos.Add(watch.Elapsed.TotalMilliseconds);}results.Add(new{ruta,n=20,min=tiempos.Min(),max=tiempos.Max(),promedio=tiempos.Average(),p95=tiempos.Order().ElementAt(18),umbralMs=3000,pass=tiempos.Max()<=3000,muestras=tiempos});Assert.True(tiempos.Max()<=3000,$"{ruta}: {tiempos.Max():F2} ms");}var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../docs/testing/evidencias"));Directory.CreateDirectory(root);await File.WriteAllTextAsync(Path.Combine(root,"c1.json"),JsonSerializer.Serialize(new{fechaUtc=DateTimeOffset.UtcNow,ambiente="LocalDB aislado, 1 cliente/1 producto, sin carga concurrente",calentamientos=2,resultados=results},new JsonSerializerOptions{WriteIndented=true}));}
}
