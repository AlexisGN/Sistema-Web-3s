using Microsoft.Data.SqlClient;
namespace Sistema3S.Web.Services.Ia;

/// <summary>Emisiones originales y evaluación prospectiva append-only, separada del backtest.</summary>
public static class IaMemoria
{
 public static async Task Evaluar(SqlConnection c,DateOnly today,CancellationToken ct)
 {
  await using var cmd=c.CreateCommand();cmd.CommandTimeout=90;cmd.CommandText="""
  INSERT ia.EvaluacionMemoria(IdPrediccion,Real,ErrorAbsoluto,Sesgo,Wape,Estado)
  SELECT p.Id,COALESCE(a.Q,0),ABS(p.Cantidad-COALESCE(a.Q,0)),p.Cantidad-COALESCE(a.Q,0),
   CASE WHEN a.Q>0 THEN ABS(p.Cantidad-a.Q)/a.Q END,CASE WHEN a.Q>0 THEN 'Evaluada' ELSE 'SinDenominador' END
  FROM ia.PrediccionMemoria p JOIN dbo.Producto prod ON prod.IdProducto=p.IdProducto
  OUTER APPLY(SELECT SUM(CAST(v.Cantidad AS decimal(18,6))) Q FROM bi.FactVenta v
   WHERE v.Procedencia='operativo' AND v.IdElementoCatalogo=prod.IdElementoCatalogo AND v.Estado=N'Confirmada'
    AND v.SinDetalleValido=0 AND v.Cantidad>0 AND v.Fecha>=p.Desde AND v.Fecha<DATEADD(day,1,p.Hasta)) a
  WHERE p.Hasta<@today AND p.DiaEmision<p.Desde
   AND NOT EXISTS(SELECT 1 FROM ia.EvaluacionMemoria e WHERE e.IdPrediccion=p.Id);
  """;cmd.Parameters.AddWithValue("@today",today.ToDateTime(TimeOnly.MinValue));await cmd.ExecuteNonQueryAsync(ct);
 }
 public static async Task<bool> Degradado(SqlConnection c,Guid? model,CancellationToken ct)
 {
  if(model==null)return false;
  await using var cmd=c.CreateCommand();cmd.CommandTimeout=60;
  // Ventanas separadas por horizonte. No sumar UND/L ni solapar emisiones diarias.
  cmd.CommandText="""
  WITH bloques AS(SELECT p.IdProducto,p.Unidad,p.Horizonte,DATEDIFF(day,'20000101',p.Desde)/(p.Horizonte+1) Bloque,
   e.Real,e.ErrorAbsoluto,p.Desde,p.Hasta,ROW_NUMBER() OVER(PARTITION BY p.IdProducto,p.Horizonte,DATEDIFF(day,'20000101',p.Desde)/(p.Horizonte+1) ORDER BY p.Desde) rn
   FROM ia.PrediccionMemoria p JOIN ia.EvaluacionMemoria e ON e.IdPrediccion=p.Id WHERE p.Modelo=@m),
  elegidas AS(SELECT *,ROW_NUMBER() OVER(PARTITION BY IdProducto,Horizonte ORDER BY Desde DESC) n FROM bloques b
   WHERE rn=1 AND NOT EXISTS(SELECT 1 FROM bloques anterior WHERE anterior.rn=1 AND anterior.IdProducto=b.IdProducto AND anterior.Horizonte=b.Horizonte AND anterior.Desde<b.Desde AND anterior.Hasta>=b.Desde))
  SELECT COUNT(*) FROM(SELECT IdProducto,Unidad,Horizonte FROM elegidas WHERE n<=3 GROUP BY IdProducto,Unidad,Horizonte
   HAVING COUNT(*)=3 AND SUM(Real)>0 AND SUM(ErrorAbsoluto)/SUM(Real)>.15 AND SUM(CASE WHEN Real>0 AND ErrorAbsoluto/Real>.15 THEN 1 ELSE 0 END)>=2) q;
  """;cmd.Parameters.AddWithValue("@m",model);return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct))>0;
 }
}
