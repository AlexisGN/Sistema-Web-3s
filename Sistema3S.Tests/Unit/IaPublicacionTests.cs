using System.Text.Json.Nodes;
using Sistema3S.Web.Services.Ia;
namespace Sistema3S.Tests.Unit;
public sealed class IaPublicacionTests
{
 internal static JsonObject Validado(double predicted=100){var cutoff=new DateOnly(2026,10,15);var hs=new JsonObject();foreach(var h in new[]{7,30,60,90}){var tests=new JsonArray();foreach(var i in new[]{1,2,3}){var origin=cutoff.AddDays(-(h+1)*i);var selection=origin.AddDays(-h-1);tests.Add(new JsonObject{["origin"]=origin.ToString("yyyy-MM-dd"),["first"]=origin.AddDays(2).ToString("yyyy-MM-dd"),["last"]=origin.AddDays(h+1).ToString("yyyy-MM-dd"),["predicted"]=predicted,["actual"]=100,["selectionOrigins"]=new JsonArray(selection.ToString("yyyy-MM-dd")),["selectionWindows"]=new JsonArray(new JsonObject{["origin"]=selection.ToString("yyyy-MM-dd"),["horizon"]=h})});}hs[h.ToString()]=new JsonObject{["quality"]=new JsonObject{["tests"]=tests,["wape"]=Math.Abs(predicted-100)/100}};}
  return new JsonObject{["version"]="3s-ia-5",["scope"]="production",["publishable"]=true,["cutoff"]=cutoff.ToString("yyyy-MM-dd"),["approved"]=new JsonArray(1),["policies"]=new JsonObject{["1"]=hs}};
 }
 [Fact]public void RecalculaElErrorYRechazaUnaAprobacionDeclarada(){var m=Validado(50);Assert.False(IaPublicacion.Aceptable(m));Assert.True(IaPublicacion.Aceptable(Validado(85)));m=Validado();m["scope"]="development";Assert.False(IaPublicacion.Aceptable(m));}
 [Fact]public void NoPublicaConSeleccionFuturaOVentanasSuperpuestas(){var m=Validado();m["policies"]!["1"]!["30"]!["quality"]!["tests"]![0]!["selectionOrigins"]![0]="2026-10-15";Assert.False(IaPublicacion.Aceptable(m));m=Validado();m["policies"]!["1"]!["30"]!["quality"]!["tests"]![1]=m["policies"]!["1"]!["30"]!["quality"]!["tests"]![0]!.DeepClone();Assert.False(IaPublicacion.Aceptable(m));}
 [Fact]public void ConservaElAnteriorSiElCandidatoEmpeora(){Assert.False(IaPublicacion.Mejora(Validado(90),Validado(100)));Assert.True(IaPublicacion.Mejora(Validado(),Validado(90)));}
 [Fact]public void UnaEmisionV4NoOriginaComprasPorTenerPronostico(){Assert.False(IaPublicacion.Confiable(new JsonObject{["version"]="3s-ia-4"},new JsonObject{["sufficient"]=true}));Assert.False(IaPublicacion.Confiable(new JsonObject{["version"]="3s-ia-5",["published"]=false},new JsonObject{["accepted"]=true}));}
}
