using System.Text.Json.Nodes;
using System.Text.Json;
namespace Sistema3S.Web.Services.Ia;

public static class IaPublicacion
{
 public static bool Aceptable(JsonObject m)
 {
  try{
   var version=m["version"]?.GetValue<string>();if(version=="3s-ia-8")return IaPublicacionV8.Aceptable(m);var v7=version=="3s-ia-7";var v6=version is "3s-ia-6" or "3s-ia-7";
   if(v7&&m["experimental"]?.GetValue<bool>()!=false)return false;
   if(version is not ("3s-ia-5" or "3s-ia-6" or "3s-ia-7")||m["scope"]?.GetValue<string>()!="production"||m["publishable"]?.GetValue<bool>()!=true)return false;
   var approved=m["approved"]!.AsArray().Select(x=>x!.GetValue<int>()).ToList();if(approved.Count==0||approved.Distinct().Count()!=approved.Count)return false;
   var cutoff=DateOnly.Parse(m["cutoff"]!.GetValue<string>());
   var selection=v6?DateOnly.Parse(m["selectionCutoff"]!.GetValue<string>()):cutoff;
   if(v6&&(m["schema"]?.GetValue<int>()!=(v7?3:2)||m["source"]?.GetValue<string>()!="operativo"||selection!=cutoff.AddDays(-273)))return false;
   foreach(var id in approved)foreach(var h in new[]{7,30,60,90}){
    var q=m["policies"]![id.ToString()]![h.ToString()]!["quality"]!;var tests=q["tests"]!.AsArray();if(tests.Count<3||(v6&&tests.Count!=3))return false;
    if(v6&&(q["id"]?.GetValue<int>()!=id||q["horizon"]?.GetValue<int>()!=h||q["method"]?.GetValue<string>()!=m["policies"]![id.ToString()]![h.ToString()]!["method"]?.GetValue<string>()))return false;
    double error=0,actual=0;var ranges=new List<(DateOnly first,DateOnly last)>();
    foreach(var t in tests){var origin=DateOnly.Parse(t!["origin"]!.GetValue<string>());var first=DateOnly.Parse(t["first"]!.GetValue<string>());var last=DateOnly.Parse(t["last"]!.GetValue<string>());if(first!=origin.AddDays(2)||last!=origin.AddDays(h+1)||last>cutoff||ranges.Any(r=>first<=r.last&&last>=r.first))return false;ranges.Add((first,last));
     if(v6&&(origin<selection||DateOnly.Parse(t["trainedUntil"]!.GetValue<string>())!=origin||t["method"]?.GetValue<string>()!=q["method"]?.GetValue<string>()))return false;
     var dates=t["selectionOrigins"]!.AsArray();if(dates.Count==0||dates.Any(d=>DateOnly.Parse(d!.GetValue<string>()).AddDays(h+1)>(v6?selection:origin)))return false;
     if(t["selectionWindows"]!.AsArray().Any(w=>DateOnly.Parse(w!["origin"]!.GetValue<string>()).AddDays(w["horizon"]!.GetValue<int>()+1)>(v6?selection:origin)))return false;
     var p=t["predicted"]!.Deserialize<double>();var a=t["actual"]!.Deserialize<double>();if(!double.IsFinite(p)||!double.IsFinite(a)||p<0||a<0)return false;error+=Math.Abs(p-a);actual+=a;
    }
    if(actual<=0||error/actual>.15+1e-12)return false;
   }
   return true;
  }catch{return false;}
 }
 public static bool Mejora(JsonObject candidate,JsonObject? active)
 {
  if(!Aceptable(candidate))return false;if(active==null)return true;
  if(candidate["version"]?.GetValue<string>()=="3s-ia-8")return false; // Conservar incumbente hasta una comparación pareada verificable.
  // No comparar resúmenes de períodos distintos ni un incumbente vuelto a ajustar.
  if(candidate["version"]?.GetValue<string>()=="3s-ia-7"&&candidate["pairedIncumbentVerified"]?.GetValue<bool>()!=true)return false;
  var next=candidate["approved"]!.AsArray().Select(x=>x!.GetValue<int>()).ToHashSet();var prior=active["approved"]!.AsArray().Select(x=>x!.GetValue<int>()).ToList();if(prior.Any(id=>!next.Contains(id)))return false;
  if(candidate["incumbentComparison"] is JsonArray comparisons && comparisons.Count>0){foreach(var id in prior)foreach(var h in new[]{7,30,60,90}){var x=comparisons.FirstOrDefault(x=>x?["id"]?.GetValue<int>()==id&&x?["horizon"]?.GetValue<int>()==h);if(x?["candidateWape"]==null||x["incumbent"]?["wape"]==null||x["candidateWape"]!.Deserialize<double>()>x["incumbent"]!["wape"]!.Deserialize<double>()+1e-12)return false;}return true;}
  foreach(var id in prior)foreach(var h in new[]{7,30,60,90})if(candidate["policies"]![id.ToString()]![h.ToString()]!["quality"]!["wape"]!.Deserialize<double>()>active["policies"]![id.ToString()]![h.ToString()]!["quality"]!["wape"]!.Deserialize<double>()+1e-12)return false;
  return true;
 }
 public static bool Confiable(JsonNode? root,JsonNode? product)
 {
  var version=root?["version"]?.GetValue<string>();
  // Los contratos anteriores sin versión se mantienen para integraciones históricas.
  // Las emisiones reales v4 y v5 siempre incluyen versión.
  return version is "3s-ia-5" or "3s-ia-6" or "3s-ia-7" or "3s-ia-8"?root?["published"]?.GetValue<bool>()==true&&(version is not ("3s-ia-7" or "3s-ia-8")||root?["experimental"]?.GetValue<bool>()==false)&&product?["accepted"]?.GetValue<bool>()==true:version is null or "3s-ia-3";
 }
}
