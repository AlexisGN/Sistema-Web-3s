using System.Text.Json;
using System.Text.Json.Nodes;
namespace Sistema3S.Web.Services.Ia;

public static class IaPublicacionV8
{
 public static bool Aceptable(JsonObject m)
 {
  try{
   if(m["schema"]?.GetValue<int>()!=4||m["version"]?.GetValue<string>()!="3s-ia-8"||m["source"]?.GetValue<string>()!="operativo"||m["scope"]?.GetValue<string>()!="production"||m["publishable"]?.GetValue<bool>()!=true||m["experimental"]?.GetValue<bool>()!=false)return false;
   var approved=m["approved"]!.AsArray().Select(x=>x!.GetValue<int>()).ToArray();if(approved.Length==0||approved.Distinct().Count()!=approved.Length)return false;
   var cutoff=DateOnly.Parse(m["cutoff"]!.GetValue<string>());
   foreach(var id in approved)foreach(var h in new[]{7,30,60,90}){
    var policy=m["policies"]![id.ToString()]![h.ToString()]!;var q=policy["quality"]!;
    if(policy["method"]?.GetValue<string>()!="v8_hurdle_tsb"||q["id"]?.GetValue<int>()!=id||q["horizon"]?.GetValue<int>()!=h)return false;
    var tests=q["tests"]!.AsArray();if(tests.Count!=3)return false;var ranges=new List<(DateOnly,DateOnly)>();double error=0,actual=0;
    foreach(var t in tests){var origin=DateOnly.Parse(t!["origin"]!.GetValue<string>());var first=DateOnly.Parse(t["first"]!.GetValue<string>());var last=DateOnly.Parse(t["last"]!.GetValue<string>());
     if(first!=origin.AddDays(2)||last!=origin.AddDays(h+1)||last>cutoff||DateOnly.Parse(t["trainedUntil"]!.GetValue<string>())!=origin||ranges.Any(r=>first<=r.Item2&&last>=r.Item1))return false;
     var windows=t["selectionWindows"]!.AsArray();if(windows.Count==0||windows.Any(w=>DateOnly.Parse(w!["origin"]!.GetValue<string>()).AddDays(w["horizon"]!.GetValue<int>()+1)>origin))return false;
     ranges.Add((first,last));var p=t["predicted"]!.Deserialize<double>();var a=t["actual"]!.Deserialize<double>();if(!double.IsFinite(p)||!double.IsFinite(a)||p<0||a<0)return false;actual+=a;error+=Math.Abs(p-a);
    }
    if(actual<=0||error/actual>.15+1e-12||q["wape"]==null||Math.Abs(q["wape"]!.Deserialize<double>()-error/actual)>1e-10)return false;
   }
   return true;
  }catch{return false;}
 }
}
