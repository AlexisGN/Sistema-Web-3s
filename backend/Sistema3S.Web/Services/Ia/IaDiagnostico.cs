using System.Text.Json.Nodes;
using System.Text.Json;
namespace Sistema3S.Web.Services.Ia;

/// <summary>Explica un candidato rechazado sin convertirlo en un pronóstico publicado.</summary>
public static class IaDiagnostico
{
 public static JsonObject Crear(IaEntrada data,JsonObject manifest)
 {
  var first=DateOnly.Parse(data.Cutoff).AddDays(2);var products=new JsonArray();
  foreach(var s in data.Products){
   var policy=manifest["policies"]?[s.Id.ToString()];var quality=new JsonObject();
   foreach(var h in new[]{7,30,60,90})quality[h.ToString()]=policy?[h.ToString()]?["quality"]?.DeepClone();
   var tested=quality.Any(x=>x.Value?["tests"]?.AsArray().Count>0);
   var promising=manifest["temporalApproved"] is JsonArray ids&&ids.Any(id=>id?.GetValue<int>()==s.Id);
   var reason=manifest["publishable"]?.GetValue<bool>()==true?"La emisión anterior ya no corresponde a un modelo activo. Sus estimaciones se conservan como historial y no habilitan recomendaciones de compra.":promising?"El resultado experimental cumple el margen histórico; falta validación prospectiva para activar recomendaciones.":tested?"La evaluación histórica supera el margen de error permitido del 15 %. El candidato no se ha activado.":
    "Hay historial registrado, pero faltan períodos históricos independientes para comprobar un margen de error máximo del 15 %.";
   products.Add(new JsonObject{["id"]=s.Id,["name"]=s.Name,["unit"]=s.Unit,["sufficient"]=false,["accepted"]=false,
    ["reason"]=reason,["first"]=first.ToString("yyyy-MM-dd"),["daily"]=new JsonArray(),
    ["totals"]=new JsonObject{["7"]=null,["30"]=null,["60"]=null,["90"]=null},["nextMonth"]=null,
    ["history"]=JsonSerializer.SerializeToNode(s.Days.TakeLast(90),IaMotor.Json),["days"]=s.Days.Count,["events"]=s.Days.Count(d=>d.Quantity>0),
    ["quality"]=quality,["method"]=policy?["30"]?["method"]?.DeepClone(),["intervals"]=new JsonObject(),["regimeChanged"]=false});
  }
  return new JsonObject{["version"]=manifest["version"]?.GetValue<string>()??"3s-ia-6",["source"]="operativo",["cutoff"]=data.Cutoff,["published"]=false,["trained"]=false,["diagnostic"]=true,["products"]=products};
 }
}
