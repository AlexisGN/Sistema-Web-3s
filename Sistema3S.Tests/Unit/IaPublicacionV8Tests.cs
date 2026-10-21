using System.Text.Json.Nodes;
using Sistema3S.Web.Services.Ia;
namespace Sistema3S.Tests.Unit;
public sealed class IaPublicacionV8Tests
{
 static JsonObject Control(){
  var cutoff=new DateOnly(2026,10,18);var ps=new JsonObject();
  foreach(var h in new[]{7,30,60,90}){var tests=new JsonArray();foreach(var n in new[]{3,2,1}){var origin=cutoff.AddDays(-91*n);tests.Add(new JsonObject{["origin"]=origin.ToString("yyyy-MM-dd"),["first"]=origin.AddDays(2).ToString("yyyy-MM-dd"),["last"]=origin.AddDays(h+1).ToString("yyyy-MM-dd"),["trainedUntil"]=origin.ToString("yyyy-MM-dd"),["predicted"]=11,["actual"]=10,["selectionWindows"]=new JsonArray(new JsonObject{["origin"]=origin.AddDays(-100).ToString("yyyy-MM-dd"),["horizon"]=90})});}
   ps[h.ToString()]=new JsonObject{["method"]="v8_hurdle_tsb",["quality"]=new JsonObject{["id"]=1,["horizon"]=h,["wape"]=.1,["tests"]=tests}};
  }
  return new JsonObject{["schema"]=4,["version"]="3s-ia-8",["source"]="operativo",["scope"]="production",["publishable"]=true,["experimental"]=false,["cutoff"]=cutoff.ToString("yyyy-MM-dd"),["approved"]=new JsonArray(1),["policies"]=new JsonObject{["1"]=ps}};
 }
 [Fact]public void CertificacionRecalculaErrorYNoConfiaEnBooleanos(){var m=Control();Assert.True(IaPublicacionV8.Aceptable(m));m["policies"]!["1"]!["90"]!["quality"]!["tests"]![0]!["predicted"]=100;Assert.False(IaPublicacionV8.Aceptable(m));}
 [Fact]public void NoPublicaPruebasSolapadasOFuturo(){var m=Control();var t=m["policies"]!["1"]!["90"]!["quality"]!["tests"]!.AsArray();t[1]=t[0]!.DeepClone();Assert.False(IaPublicacionV8.Aceptable(m));m=Control();m["policies"]!["1"]!["7"]!["quality"]!["tests"]![0]!["selectionWindows"]![0]!["origin"]="2099-01-01";Assert.False(IaPublicacionV8.Aceptable(m));}
 [Fact]public void ExperimentalNuncaCertificado(){var m=Control();m["experimental"]=true;Assert.False(IaPublicacionV8.Aceptable(m));m=Control();m["scope"]="development";Assert.False(IaPublicacionV8.Aceptable(m));}
}
