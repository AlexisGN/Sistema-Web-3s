using System.Text.Json.Nodes;
using Sistema3S.Web.Services.Ia;
namespace Sistema3S.Tests.Unit;

public sealed class IaMotorV6Tests
{
 static JsonObject Validado(){
  var m=IaPublicacionTests.Validado();m["version"]="3s-ia-6";m["schema"]=2;m["source"]="operativo";
  var cutoff=DateOnly.Parse(m["cutoff"]!.GetValue<string>());var freeze=cutoff.AddDays(-273);m["selectionCutoff"]=freeze.ToString("yyyy-MM-dd");
  foreach(var h in new[]{7,30,60,90}){
   var p=m["policies"]!["1"]![h.ToString()]!;p["method"]="media28";var q=p["quality"]!;q["id"]=1;q["horizon"]=h;q["method"]="media28";
   foreach(var t in q["tests"]!.AsArray()){
    t!["method"]="media28";t["trainedUntil"]=t["origin"]!.DeepClone();t["selectionOrigins"]=new JsonArray(freeze.AddDays(-h-1).ToString("yyyy-MM-dd"));
    t["selectionWindows"]=new JsonArray(new JsonObject{["origin"]=freeze.AddDays(-h-1).ToString("yyyy-MM-dd"),["horizon"]=h});
   }
  }
  return m;
 }
 [Fact]public void AceptaElContratoNuevoYRechazaUnaPruebaFutura(){
  var m=Validado();Assert.True(IaPublicacion.Aceptable(m));m["policies"]!["1"]!["30"]!["quality"]!["tests"]![0]!["trainedUntil"]="2026-10-15";Assert.False(IaPublicacion.Aceptable(m));
 }
 [Fact]public void LaSeleccionQuedaFijadaAntesDeTodosLosBloquesReservados(){
  var m=Validado();m["policies"]!["1"]!["7"]!["quality"]!["tests"]![0]!["selectionWindows"]![0]!["origin"]="2026-09-01";Assert.False(IaPublicacion.Aceptable(m));
  m=Validado();m["policies"]!["1"]!["30"]!["method"]="otro";Assert.False(IaPublicacion.Aceptable(m));
 }
 [Fact]public void UnDiagnosticoNoOriginaCantidadesDeCompra(){
  var series=new IaSerie(1,"Producto","UND","2026-01-01",[new("2026-01-01",3)]);
  var manifest=new JsonObject{["policies"]=new JsonObject()};var r=IaDiagnostico.Crear(new("operativo","2026-01-01",[series]),manifest);
  var p=r["products"]![0]!;Assert.False(p["sufficient"]!.GetValue<bool>());Assert.Empty(p["daily"]!.AsArray());Assert.Null(p["totals"]!["30"]);
  Assert.False(IaPublicacion.Confiable(r,p));Assert.Contains("independientes",p["reason"]!.GetValue<string>());
 }
 [Fact]public void VersionDesconocidaNoSeUsaParaCompras(){Assert.False(IaPublicacion.Confiable(new JsonObject{["version"]="otra"},new JsonObject{["accepted"]=true}));}
}
