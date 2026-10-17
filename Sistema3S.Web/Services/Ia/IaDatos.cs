using System.Data;
using Sistema3S.Web.Services.Bi;
namespace Sistema3S.Web.Services.Ia;

public sealed class IaDatos(BiConexion conexion)
{
    public async Task<List<IaIngresoMes>> IngresosMensuales(string source,CancellationToken ct)
    {
        await using var c=await conexion.AbrirAsync(ct);await using var cmd=c.CreateCommand();cmd.CommandTimeout=45;
        cmd.CommandText="""
        SET NOCOUNT ON; SET LOCK_TIMEOUT 5000;
        SELECT e.IdProducto,CONVERT(char(7),v.Fecha,126) Mes,SUM(v.Base) IngresoNeto
        FROM bi.FactVenta v JOIN bi.DimElemento e ON e.IdElementoCatalogo=v.IdElementoCatalogo
        JOIN dbo.Producto p ON p.IdProducto=e.IdProducto
        WHERE v.Procedencia=@s AND v.Estado=N'Confirmada' AND v.SinDetalleValido=0 AND v.Cantidad>0
        AND e.Estado=1 AND p.AplicaInventario=1 AND v.Fecha>=@start AND v.Fecha<@end
        GROUP BY e.IdProducto,CONVERT(char(7),v.Fecha,126) ORDER BY e.IdProducto,Mes;
        """;
        var today=IaReglas.Hoy;BiConexion.Parametro(cmd,"@s",SqlDbType.VarChar,source,12);
        BiConexion.Parametro(cmd,"@start",SqlDbType.Date,new DateOnly(today.Year,today.Month,1).AddMonths(-5).ToDateTime(TimeOnly.MinValue));
        BiConexion.Parametro(cmd,"@end",SqlDbType.Date,today.ToDateTime(TimeOnly.MinValue));
        var rows=await BiConexion.LeerAsync(cmd,["meses"],ct);
        return rows["meses"].Select(r=>new IaIngresoMes(Convert.ToInt32(r["IdProducto"]),(string)r["Mes"]!,Convert.ToDecimal(r["IngresoNeto"]))).ToList();
    }
    public async Task<IaEntrada> Extraer(string source,DateOnly cutoff,CancellationToken ct)
    {
        await using var c=await conexion.AbrirAsync(ct);
        await using var cmd=c.CreateCommand();cmd.CommandTimeout=90;
        cmd.CommandText="""
        SET NOCOUNT ON; SET LOCK_TIMEOUT 5000;
        SELECT p.IdProducto,e.IdElementoCatalogo,e.Nombre,e.Unidad,e.FechaRegistro,
        (SELECT MIN(Fecha) FROM bi.FactVenta v WHERE v.Procedencia=@s AND v.IdElementoCatalogo=e.IdElementoCatalogo AND v.Estado=N'Confirmada' AND v.SinDetalleValido=0 AND v.Cantidad>0 AND v.Fecha<@end) PrimeraVenta,
        CASE WHEN @s='operativo' THEN i.StockActual ELSE (SELECT SUM(CAST(Delta AS bigint)) FROM bi.FactMovimiento m WHERE m.Procedencia=@s AND m.IdProducto=p.IdProducto) END Stock
        FROM bi.DimElemento e JOIN dbo.Producto p ON p.IdProducto=e.IdProducto
        LEFT JOIN dbo.Inventario i ON i.IdProducto=p.IdProducto WHERE e.Estado=1 AND p.AplicaInventario=1 ORDER BY p.IdProducto;
        SELECT e.IdProducto,CONVERT(date,v.Fecha) Dia,SUM(CAST(v.Cantidad AS bigint)) Cantidad,COUNT(DISTINCT v.IdCliente) Compradores
        FROM bi.FactVenta v JOIN bi.DimElemento e ON e.IdElementoCatalogo=v.IdElementoCatalogo
        WHERE v.Procedencia=@s AND v.Estado=N'Confirmada' AND v.SinDetalleValido=0 AND v.Cantidad>0 AND v.Fecha>=@start AND v.Fecha<@end
        AND e.IdProducto IS NOT NULL GROUP BY e.IdProducto,CONVERT(date,v.Fecha) ORDER BY e.IdProducto,Dia;
        SELECT IdProducto,Fecha,Delta FROM bi.FactMovimiento WHERE Procedencia=@s ORDER BY IdProducto,Fecha,IdMovimiento;
        """;
        BiConexion.Parametro(cmd,"@s",SqlDbType.VarChar,source,12);
        BiConexion.Parametro(cmd,"@start",SqlDbType.Date,cutoff.AddDays(-729).ToDateTime(TimeOnly.MinValue));
        BiConexion.Parametro(cmd,"@end",SqlDbType.Date,cutoff.AddDays(1).ToDateTime(TimeOnly.MinValue));
        var rows=await BiConexion.LeerAsync(cmd,["catalogo","ventas","movimientos"],ct);
        var sales=rows["ventas"].ToLookup(r=>Convert.ToInt32(r["IdProducto"]));
        var movements=rows["movimientos"].ToLookup(r=>Convert.ToInt32(r["IdProducto"]));
        var products=new List<IaSerie>();
        foreach(var p in rows["catalogo"])
        {
            var id=Convert.ToInt32(p["IdProducto"]);var mv=movements[id].ToList();
            var firstSale=p["PrimeraVenta"] is DateTime sale?DateOnly.FromDateTime(sale):(DateOnly?)null;
            if(firstSale==null || firstSale>cutoff)continue;
            var firstMovement=mv.Count>0?DateOnly.FromDateTime((DateTime)mv[0]["Fecha"]!):(DateOnly?)null;
            var start=IaHistoria.Inicio(firstSale.Value,cutoff,firstMovement,source=="referencia");
            var known=p["Stock"]!=null && mv.Count>0 && mv.All(m=>m["Delta"]!=null);
            double balance=known?Convert.ToDouble(p["Stock"])-mv.Sum(m=>Convert.ToDouble(m["Delta"])):0;
            if(balance<0)known=false;
            var changes=mv.GroupBy(m=>DateOnly.FromDateTime((DateTime)m["Fecha"]!)).ToDictionary(g=>g.Key,g=>g.Sum(m=>Convert.ToDouble(m["Delta"])));
            var byDay=sales[id].ToDictionary(r=>DateOnly.FromDateTime((DateTime)r["Dia"]!));
            var days=new List<IaDia>();
            // Reconstrucción desde cierre y todo el kardex; signo desconocido invalida disponibilidad.
            foreach(var prior in changes.Where(x=>x.Key<start))balance+=prior.Value;
            for(var day=start;day<=cutoff;day=day.AddDays(1))
            {
                balance+=changes.GetValueOrDefault(day);
                if(balance<0)known=false;
                byDay.TryGetValue(day,out var row);
                days.Add(new(day.ToString("yyyy-MM-dd"),row==null?0:Convert.ToDouble(row["Cantidad"]),row==null?0:Convert.ToInt32(row["Compradores"]),known?balance:null));
            }
            if(!known)days=days.Select(d=>d with{Availability=null}).ToList();
            products.Add(new(id,(string)p["Nombre"]!,(string)p["Unidad"]!,start.ToString("yyyy-MM-dd"),days));
        }
        return new(source,cutoff.ToString("yyyy-MM-dd"),products);
    }
    public async Task<List<IaContexto>> Contexto(string source,CancellationToken ct)
    {
        await using var c=await conexion.AbrirAsync(ct);await using var cmd=c.CreateCommand();cmd.CommandTimeout=45;
        // La nueva tabla es opcional hasta aplicar la migración aditiva; nunca interrumpe el módulo existente.
        await using var installed=c.CreateCommand();installed.CommandText="SELECT CASE WHEN OBJECT_ID('ia.AbastecimientoReferencia') IS NULL THEN 0 ELSE 1 END";
        var referencePolicies=Convert.ToInt32(await installed.ExecuteScalarAsync(ct))==1?
            "UNION ALL SELECT a.IdProducto,a.IdProveedor,a.PlazoDias,a.CoberturaDias,a.Seguridad,a.Minimo,a.Multiplo,a.Reservado,a.EntradaConfirmada,a.FechaEntrada,a.Evidencia,a.VerificadaHasta FROM ia.AbastecimientoReferencia a JOIN bi.LoteReferencia l ON l.IdLote=a.IdLote AND l.Estado='Completo' WHERE @s='referencia'":"";
        cmd.CommandText=$"""
        SET NOCOUNT ON; SET LOCK_TIMEOUT 5000;
        SELECT p.IdProducto,e.Nombre,e.Unidad,COALESCE(i.StockMinimo,0) StockMinimo,
        CASE WHEN @s='operativo' THEN i.StockActual ELSE ref.Saldo END Stock,
        pr.PrecioNeto,pr.Fecha FechaPrecio,COALESCE(co.CostoNeto,coHist.CostoNeto) CostoNeto,COALESCE(co.Fecha,coHist.Fecha) FechaCosto,prove.IdProveedor IdProveedorPolitica,COALESCE(prove.IdProveedor,hist.IdProveedor) IdProveedor,COALESCE(prove.RazonSocial,hist.Proveedor) Proveedor,
        a.PlazoDias,a.CoberturaDias,a.Seguridad,a.Minimo,a.Multiplo,a.Reservado,a.EntradaConfirmada,a.FechaEntrada,a.Evidencia,a.VerificadaHasta
        FROM dbo.Producto p JOIN bi.DimElemento e ON e.IdProducto=p.IdProducto
        LEFT JOIN dbo.Inventario i ON i.IdProducto=p.IdProducto
        LEFT JOIN (SELECT IdProducto,IdProveedor,PlazoDias,CoberturaDias,Seguridad,Minimo,Multiplo,Reservado,EntradaConfirmada,FechaEntrada,Evidencia,VerificadaHasta FROM ia.Abastecimiento WHERE @s='operativo'
        {referencePolicies}) a ON a.IdProducto=p.IdProducto
        LEFT JOIN dbo.Proveedor prove ON prove.IdProveedor=a.IdProveedor AND prove.Estado=1
        OUTER APPLY(SELECT TOP(1) v.IdProveedor,pv.RazonSocial Proveedor
        FROM bi.FactCompra v JOIN dbo.Proveedor pv ON pv.IdProveedor=v.IdProveedor AND pv.Estado=1
        WHERE @s='operativo' AND v.Procedencia=@s AND v.IdElementoCatalogo=e.IdElementoCatalogo
        AND v.Estado IN(N'Registrada',N'Confirmada') AND v.SinDetalleValido=0 AND v.Cantidad>0 AND v.Base>0
        AND v.Fecha<DATEADD(day,1,@today) ORDER BY v.Fecha DESC,v.IdOperacion DESC) hist
        OUTER APPLY(SELECT SUM(CAST(m.Delta AS bigint)) Saldo FROM bi.FactMovimiento m WHERE m.Procedencia='referencia' AND m.IdProducto=p.IdProducto) ref
        OUTER APPLY(SELECT SUM(v.Base)/NULLIF(SUM(CAST(v.Cantidad AS decimal(28,6))),0) PrecioNeto,MAX(CONVERT(date,v.Fecha)) Fecha
        FROM bi.FactVenta v WHERE v.Procedencia=@s AND v.IdElementoCatalogo=e.IdElementoCatalogo AND v.Estado=N'Confirmada' AND v.SinDetalleValido=0
        AND v.Base>0 AND v.Cantidad>0 AND v.Fecha>=DATEADD(day,-89,@today) AND v.Fecha<DATEADD(day,1,@today)) pr
        OUTER APPLY(SELECT SUM(v.Base)/NULLIF(SUM(CAST(v.Cantidad AS decimal(28,6))),0) CostoNeto,MAX(CONVERT(date,v.Fecha)) Fecha
        FROM bi.FactCompra v WHERE v.Procedencia=@s AND v.IdElementoCatalogo=e.IdElementoCatalogo AND v.Estado IN(N'Registrada',N'Confirmada') AND v.SinDetalleValido=0
        AND v.Base>0 AND v.Cantidad>0 AND v.Fecha>=DATEADD(day,-89,@today) AND v.Fecha<DATEADD(day,1,@today)) co
        OUTER APPLY(SELECT TOP(1) SUM(v.Base)/NULLIF(SUM(CAST(v.Cantidad AS decimal(28,6))),0) CostoNeto,MAX(CONVERT(date,v.Fecha)) Fecha
        FROM bi.FactCompra v WHERE co.CostoNeto IS NULL AND v.Procedencia=@s AND v.IdElementoCatalogo=e.IdElementoCatalogo
        AND v.Estado IN(N'Registrada',N'Confirmada') AND v.SinDetalleValido=0 AND v.Base>0 AND v.Cantidad>0
        AND v.Fecha<DATEADD(day,1,@today) GROUP BY v.IdOperacion,v.Fecha ORDER BY v.Fecha DESC,v.IdOperacion DESC) coHist
        WHERE e.Estado=1 AND p.AplicaInventario=1 ORDER BY p.IdProducto;
        """;
        BiConexion.Parametro(cmd,"@s",SqlDbType.VarChar,source,12);BiConexion.Parametro(cmd,"@today",SqlDbType.Date,IaReglas.Hoy.ToDateTime(TimeOnly.MinValue));
        var result=await BiConexion.LeerAsync(cmd,["productos"],ct);
        decimal? N(Dictionary<string,object?> r,string key)=>r[key]==null?null:Convert.ToDecimal(r[key]);
        DateOnly? D(Dictionary<string,object?> r,string key)=>r[key] is DateTime d?DateOnly.FromDateTime(d):null;
        return result["productos"].Select(r=>new IaContexto(Convert.ToInt32(r["IdProducto"]),(string)r["Nombre"]!,(string)r["Unidad"]!,N(r,"Stock"),N(r,"StockMinimo")??0,
            N(r,"PrecioNeto"),N(r,"CostoNeto"),D(r,"FechaPrecio"),D(r,"FechaCosto"),r["Proveedor"] as string,
            r["IdProveedorPolitica"]!=null?new IaPolitica(Convert.ToInt32(r["IdProducto"]),Convert.ToInt32(r["IdProveedorPolitica"]),Convert.ToInt32(r["PlazoDias"]),Convert.ToInt32(r["CoberturaDias"]),
                N(r,"Seguridad")!.Value,Convert.ToInt32(r["Minimo"]),Convert.ToInt32(r["Multiplo"]),N(r,"Reservado")!.Value,N(r,"EntradaConfirmada")!.Value,D(r,"FechaEntrada"),
                (string)r["Evidencia"]!,D(r,"VerificadaHasta")!.Value):null,r["IdProveedor"]==null?null:Convert.ToInt32(r["IdProveedor"]))).ToList();
    }
}
