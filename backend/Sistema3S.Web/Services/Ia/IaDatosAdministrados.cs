using System.Text.Json.Nodes;
using Sistema3S.Web.Services.Bi;
namespace Sistema3S.Web.Services.Ia;

public sealed record IaConjunto(IaEntrada Datos,JsonObject Auditoria);
public sealed class IaDatosAdministrados(BiConexion conexion)
{
 public async Task<IaConjunto> Extraer(DateOnly cutoff,CancellationToken ct)
 {
  await using var c=await conexion.AbrirAsync(ct);await using var cmd=c.CreateCommand();cmd.CommandTimeout=90;
  cmd.CommandText="""
  SET NOCOUNT ON;SET LOCK_TIMEOUT 5000;
  SELECT e.IdProducto,e.Nombre,e.Unidad FROM bi.DimElemento e JOIN dbo.Producto p ON p.IdProducto=e.IdProducto WHERE e.Estado=1 AND p.AplicaInventario=1 ORDER BY e.IdProducto;
  SELECT e.IdProducto,CONVERT(date,v.Fecha) Dia,SUM(CAST(v.Cantidad AS decimal(28,6))) Cantidad,COUNT(DISTINCT v.IdCliente) Compradores
  FROM bi.FactVenta v JOIN bi.DimElemento e ON e.IdElementoCatalogo=v.IdElementoCatalogo
  WHERE v.Procedencia='operativo' AND v.Estado=N'Confirmada' AND v.SinDetalleValido=0 AND v.Cantidad>0 AND e.IdProducto IS NOT NULL
  AND v.Fecha>=@start AND v.Fecha<@end
  GROUP BY e.IdProducto,CONVERT(date,v.Fecha) ORDER BY e.IdProducto,Dia;
  SELECT COUNT(DISTINCT v.IdOperacion) Operaciones,COUNT(*) Lineas,SUM(CASE WHEN v.Estado=N'Anulada' THEN 1 ELSE 0 END) LineasAnuladas,
  SUM(CASE WHEN o.IdOperacion IS NOT NULL THEN 1 ELSE 0 END) LineasDesarrollo,
  SUM(CASE WHEN cli.Nombre LIKE 'QA[_]%' THEN 1 ELSE 0 END) LineasQA,
  MIN(v.Fecha) PrimeraFecha,MAX(v.Fecha) UltimaFecha
  FROM bi.FactVenta v LEFT JOIN ia.OrigenOperacion o ON o.Tipo='Venta' AND o.IdOperacion=v.IdOperacion
  LEFT JOIN bi.DimCliente cli ON cli.IdCliente=v.IdCliente AND cli.Procedencia='operativo'
  WHERE v.Procedencia='operativo' AND v.Fecha>=@start AND v.Fecha<@end;
  SELECT COUNT(DISTINCT v.IdOperacion) OperacionesUtilizadas,COUNT(*) LineasUtilizadas,
  CONVERT(varchar(64),HASHBYTES('SHA2_256',COALESCE(NULLIF((SELECT f.IdOperacion,f.IdCliente,f.IdElementoCatalogo,f.Fecha,f.Cantidad,e.Unidad
   FROM bi.FactVenta f JOIN bi.DimElemento e ON e.IdElementoCatalogo=f.IdElementoCatalogo JOIN dbo.Producto p ON p.IdProducto=e.IdProducto
   WHERE f.Procedencia='operativo' AND f.Estado=N'Confirmada' AND f.SinDetalleValido=0 AND f.Cantidad>0 AND p.AplicaInventario=1
    AND f.Fecha>=@start AND f.Fecha<@end ORDER BY f.IdOperacion,f.IdElementoCatalogo,f.Cantidad,f.Fecha FOR JSON PATH),N''),N'[]')),2) HuellaHistoria
  FROM bi.FactVenta v JOIN bi.DimElemento e ON e.IdElementoCatalogo=v.IdElementoCatalogo JOIN dbo.Producto p ON p.IdProducto=e.IdProducto
  WHERE v.Procedencia='operativo' AND v.Estado=N'Confirmada' AND v.SinDetalleValido=0 AND v.Cantidad>0 AND p.AplicaInventario=1 AND v.Fecha>=@start AND v.Fecha<@end;
  SELECT COUNT(*) GruposParaRevisar FROM(SELECT v.IdOperacion,v.IdElementoCatalogo,v.Cantidad FROM bi.FactVenta v
   WHERE v.Procedencia='operativo' AND v.Estado=N'Confirmada' AND v.SinDetalleValido=0 AND v.Fecha<@end
   GROUP BY v.IdOperacion,v.IdElementoCatalogo,v.Cantidad HAVING COUNT(*)>1) duplicadas;
  """;
  cmd.Parameters.AddWithValue("@start",new DateTime(1900,1,1));cmd.Parameters.AddWithValue("@end",cutoff.AddDays(1).ToDateTime(TimeOnly.MinValue));
  var rows=await BiConexion.LeerAsync(cmd,["catalogo","ventas","auditoria","uso","duplicados"],ct);var lookup=rows["ventas"].ToLookup(x=>Convert.ToInt32(x["IdProducto"]));var products=new List<IaSerie>();
  foreach(var p in rows["catalogo"]){var id=Convert.ToInt32(p["IdProducto"]);var sales=lookup[id].ToList();var start=sales.Count>0?DateOnly.FromDateTime((DateTime)sales[0]["Dia"]!):cutoff;var byDate=sales.ToDictionary(x=>DateOnly.FromDateTime((DateTime)x["Dia"]!));var days=new List<IaDia>();
   for(var day=start;day<=cutoff;day=day.AddDays(1)){byDate.TryGetValue(day,out var sale);days.Add(new(day.ToString("yyyy-MM-dd"),sale==null?0:Convert.ToDouble(sale["Cantidad"]),sale==null?0:Convert.ToInt32(sale["Compradores"]),null));}
   products.Add(new(id,(string)p["Nombre"]!,(string)p["Unidad"]!,start.ToString("yyyy-MM-dd"),days));
  }
  var audit=System.Text.Json.JsonSerializer.SerializeToNode(rows["auditoria"].FirstOrDefault(),IaMotor.Json)?.AsObject()??new();audit["productosCatalogo"]=products.Count;audit["productosConVentas"]=products.Count(p=>p.Days.Any(d=>d.Quantity>0));audit["diasConVentas"]=products.SelectMany(p=>p.Days).Where(d=>d.Quantity>0).Select(d=>d.Date).Distinct().Count();
  foreach(var entry in rows["uso"].Single())audit[entry.Key]=System.Text.Json.JsonSerializer.SerializeToNode(entry.Value,IaMotor.Json);
  audit["gruposParaRevisarDuplicados"]=Convert.ToInt32(rows["duplicados"].Single()["GruposParaRevisar"]);
  audit["experimental"]=true;
  audit["alcance"]="Historial unificado experimental: todas las ventas válidas, incluida procedencia identificada. Ceros sin inventario histórico conocido no prueban ausencia de demanda. Fechas de carga retrospectiva no certifican disponibilidad contemporánea.";
  return new(new("operativo",cutoff.ToString("yyyy-MM-dd"),products,new JsonObject{["experimental"]=true,["historyHash"]=audit["HuellaHistoria"]?.DeepClone(),["evaluationScope"]="development-unified"}),audit);
 }
}
