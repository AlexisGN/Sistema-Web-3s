using System.Text.Json.Nodes;
using Sistema3S.Web.Services.Bi;
using Sistema3S.Web.Services.Ia;
namespace Sistema3S.Web.Services.Comercial;

public sealed class ComercialPredictivo(BiConexion conexion)
{
 public async Task<object> Consultar(int? producto,CancellationToken ct)
 {
  if(producto<=0)throw new ArgumentException("Selecciona un producto válido.");
  await using var c=await conexion.AbrirAsync(ct);await using var cmd=c.CreateCommand();cmd.CommandTimeout=15;
  cmd.CommandText="""
  SELECT TOP(1) e.Id,e.Corte,e.Finalizada,e.Resultado,m.Activo,m.Elegible,m.EstadoModelo,m.Creado,m.Evaluacion,m.Id Modelo
  FROM ia.Ejecucion e JOIN ia.Modelo m ON m.Id=TRY_CONVERT(uniqueidentifier,JSON_VALUE(e.Resultado,'$.modelId'))
  WHERE m.Version='3s-ia-8' AND e.Procedencia='operativo' AND e.Estado IN('Sombra','Completada') ORDER BY e.Corte DESC,e.Id DESC;
  SELECT e.IdProducto,e.Nombre,e.Unidad,i.StockActual,i.StockMinimo FROM bi.DimElemento e JOIN dbo.Producto p ON p.IdProducto=e.IdProducto
  LEFT JOIN dbo.Inventario i ON i.IdProducto=e.IdProducto WHERE e.Estado=1 AND p.AplicaInventario=1 AND (@p IS NULL OR e.IdProducto=@p) ORDER BY e.Nombre;
  SELECT TOP(100) p.IdProducto,p.Unidad,p.Horizonte,p.DiaEmision,p.Desde,p.Hasta,p.Cantidad,p.EsSombra,e.Real,e.ErrorAbsoluto,e.Sesgo,e.Wape,e.Estado
  FROM ia.PrediccionMemoria p LEFT JOIN ia.EvaluacionMemoria e ON e.IdPrediccion=p.Id
  WHERE p.Version='3s-ia-8' AND (@p IS NULL OR p.IdProducto=@p) ORDER BY p.Id DESC;
  """;cmd.Parameters.AddWithValue("@p",(object?)producto??DBNull.Value);
  var rows=await BiConexion.LeerAsync(cmd,["emision","stock","memoria"],ct);var emission=rows["emision"].FirstOrDefault();
  var result=emission?["Resultado"] is string text?JsonNode.Parse(text)?.AsObject():null;
  var active=emission?["Activo"] is bool enabled&&enabled;
  var modelValid=active&&emission?["Evaluacion"] is string evaluation&&IaPublicacionV8.Aceptable(JsonNode.Parse(evaluation)!.AsObject());
  var stocks=rows["stock"].ToDictionary(x=>Convert.ToInt32(x["IdProducto"]));
  var products=result?["products"]?.AsArray().Where(p=>p!=null&&stocks.ContainsKey(p["id"]!.GetValue<int>())).Select(p=>{var copy=p!.DeepClone().AsObject();var stock=stocks[p["id"]!.GetValue<int>()];copy["name"]=Convert.ToString(stock["Nombre"]);copy["stock"]=stock["StockActual"]==null?null:JsonValue.Create(Convert.ToInt32(stock["StockActual"]));copy["stockMinimo"]=stock["StockMinimo"]==null?null:JsonValue.Create(Convert.ToInt32(stock["StockMinimo"]));copy["validated"]=modelValid&&p["accepted"]?.GetValue<bool>()==true;return copy;}).ToList()??[];
  return new{version="3s-ia-8",estado=modelValid?"Modelo validado":"Predicción experimental",actualizado=emission?["Finalizada"] is DateTime date?DateTime.SpecifyKind(date,DateTimeKind.Utc):(DateTime?)null,corte=emission?["Corte"],modelo=emission?["Modelo"],productos=products,memoria=rows["memoria"],mensaje=result==null?"La preparación del primer modelo está pendiente. El análisis histórico sigue disponible.":"Estimación calculada con ventas registradas. Consulta el error histórico en el detalle; la evaluación posterior conserva cada emisión original.",stockActual=true,compraAutomatica=false};
 }
}
