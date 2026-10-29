using Sistema3S.Web.DTOs.Bi;
namespace Sistema3S.Web.Services.Bi;

internal static class BiInventarioConsulta
{
    internal static string Sql(BiFiltro f) => $"""
      SET NOCOUNT ON; SET LOCK_TIMEOUT 5000;
      SELECT e.IdElementoCatalogo,e.IdProducto,e.Nombre,e.Unidad,e.Categoria,e.IdCategoria,
        CASE WHEN @procedencia='operativo' THEN i.StockActual ELSE r.Saldo END StockActual,
        CASE WHEN @procedencia='operativo' THEN i.StockMinimo ELSE NULL END StockMinimo,
        i.FechaActualizacion
      INTO #inventario FROM bi.DimElemento e LEFT JOIN dbo.Inventario i ON i.IdProducto=e.IdProducto
      LEFT JOIN(SELECT IdProducto,SUM(CAST(Delta AS bigint)) Saldo FROM bi.FactMovimiento WHERE Procedencia='referencia' GROUP BY IdProducto) r ON r.IdProducto=e.IdProducto
      WHERE e.IdProducto IS NOT NULL {(f.Categoria.HasValue?"AND e.IdCategoria=@categoria":"")} {(f.Elemento.HasValue?"AND e.IdElementoCatalogo=@elemento":"")};
      SELECT COUNT_BIG(*) Productos,SUM(CASE WHEN StockActual=0 THEN 1 ELSE 0 END) SinStock,
        CASE WHEN @procedencia='referencia' THEN NULL ELSE SUM(CASE WHEN StockActual>0 AND StockActual<=StockMinimo THEN 1 ELSE 0 END) END BajoMinimo,
        SUM(CASE WHEN StockActual IS NULL THEN 1 ELSE 0 END) SinSaldo
      FROM #inventario;
      SELECT i.IdElementoCatalogo Id,i.Nombre,i.Unidad,i.Categoria,i.StockActual,i.StockMinimo,i.FechaActualizacion,
        COALESCE(m.Entradas,0) Entradas,COALESCE(m.Salidas,0) Salidas,COALESCE(m.Ajustes,0) Ajustes,
        COALESCE(m.Movimientos,0) Movimientos,COUNT_BIG(*) OVER() TotalFilas
      FROM #inventario i LEFT JOIN(SELECT IdProducto,COUNT_BIG(*) Movimientos,
        SUM(CASE WHEN Tipo IN(N'Entrada',N'Apertura') THEN CAST(Cantidad AS bigint) ELSE 0 END) Entradas,
        SUM(CASE WHEN Tipo=N'Salida' THEN CAST(Cantidad AS bigint) ELSE 0 END) Salidas,
        SUM(CASE WHEN Tipo=N'Ajuste' THEN 1 ELSE 0 END) Ajustes
        FROM bi.FactMovimiento WHERE Procedencia=@procedencia AND Fecha>=@desde AND Fecha<@hasta GROUP BY IdProducto) m ON m.IdProducto=i.IdProducto
      ORDER BY COALESCE(m.Movimientos,0) DESC,i.IdProducto OFFSET @offset ROWS FETCH NEXT @tamanio ROWS ONLY;
      SELECT TOP(10) i.IdElementoCatalogo Id,i.Nombre,i.Unidad,COUNT_BIG(*) Movimientos,SUM(CAST(m.Cantidad AS bigint)) Cantidad
      FROM bi.FactMovimiento m JOIN #inventario i ON i.IdProducto=m.IdProducto
      WHERE m.Procedencia=@procedencia AND m.Fecha>=@desde AND m.Fecha<@hasta
      GROUP BY i.IdElementoCatalogo,i.Nombre,i.Unidad ORDER BY Movimientos DESC,Id;
      SELECT {BiReglas.PeriodoSql(f.Agrupacion,"m.Fecha")} Periodo,m.Tipo,i.Unidad,SUM(CAST(m.Cantidad AS bigint)) Cantidad,COUNT_BIG(*) Movimientos
      FROM bi.FactMovimiento m JOIN #inventario i ON i.IdProducto=m.IdProducto
      WHERE m.Procedencia=@procedencia AND m.Fecha>=@desde AND m.Fecha<@hasta
      GROUP BY {BiReglas.PeriodoSql(f.Agrupacion,"m.Fecha")},m.Tipo,i.Unidad ORDER BY Periodo;
      SELECT TOP(10) i.IdElementoCatalogo Id,i.Nombre,i.Unidad,COUNT(m.IdMovimiento) Movimientos
      FROM #inventario i LEFT JOIN bi.FactMovimiento m ON m.IdProducto=i.IdProducto AND m.Procedencia=@procedencia AND m.Fecha>=@desde AND m.Fecha<@hasta
      GROUP BY i.IdElementoCatalogo,i.Nombre,i.Unidad ORDER BY Movimientos,Id;
      SELECT m.IdMovimiento Id,m.Fecha,m.Tipo,m.Cantidad,i.Nombre,i.Unidad,
        CASE WHEN m.IdVenta IS NOT NULL THEN N'Venta / reversión' WHEN m.IdCompra IS NOT NULL THEN N'Compra / reversión' ELSE N'Manual / apertura' END Origen,
        COUNT_BIG(*) OVER() TotalFilas
      FROM bi.FactMovimiento m JOIN #inventario i ON i.IdProducto=m.IdProducto
      WHERE m.Procedencia=@procedencia AND m.Fecha>=@desde AND m.Fecha<@hasta
      ORDER BY m.Fecha DESC,m.IdMovimiento DESC OFFSET @offset ROWS FETCH NEXT @tamanio ROWS ONLY;
      -- La curva y rotación necesitan un SKU y un saldo reconstruible. No se mezclan unidades.
      IF @elemento IS NOT NULL AND EXISTS(SELECT 1 FROM #inventario WHERE StockActual IS NOT NULL)
       AND (@procedencia='referencia' OR EXISTS(SELECT 1 FROM bi.DimElemento e WHERE e.IdElementoCatalogo=@elemento AND e.FechaRegistro<=@desde))
       AND NOT EXISTS(SELECT 1 FROM bi.FactMovimiento m JOIN #inventario i ON i.IdProducto=m.IdProducto WHERE m.Procedencia=@procedencia AND m.Fecha>=@desde AND m.Delta IS NULL)
      BEGIN
        DECLARE @saldo decimal(28,4)=(SELECT TOP(1) StockActual FROM #inventario);
        DECLARE @despues decimal(28,4)=COALESCE((SELECT SUM(CAST(m.Delta AS bigint)) FROM bi.FactMovimiento m JOIN #inventario i ON i.IdProducto=m.IdProducto WHERE m.Procedencia=@procedencia AND m.Fecha>=@hasta),0);
        DECLARE @periodo decimal(28,4)=COALESCE((SELECT SUM(CAST(m.Delta AS bigint)) FROM bi.FactMovimiento m JOIN #inventario i ON i.IdProducto=m.IdProducto WHERE m.Procedencia=@procedencia AND m.Fecha>=@desde AND m.Fecha<@hasta),0);
        WITH dias AS(SELECT CAST(@desde AS date) Dia UNION ALL SELECT DATEADD(day,1,Dia) FROM dias WHERE Dia<DATEADD(day,-1,@hasta)),
        cambios AS(SELECT CONVERT(date,m.Fecha) Dia,SUM(CAST(m.Delta AS bigint)) Delta FROM bi.FactMovimiento m JOIN #inventario i ON i.IdProducto=m.IdProducto
          WHERE m.Procedencia=@procedencia AND m.Fecha>=@desde AND m.Fecha<@hasta GROUP BY CONVERT(date,m.Fecha))
        SELECT d.Dia,@saldo-@despues-@periodo+SUM(COALESCE(c.Delta,0)) OVER(ORDER BY d.Dia) Saldo
        INTO #saldos FROM dias d LEFT JOIN cambios c ON c.Dia=d.Dia OPTION(MAXRECURSION 1830);
        IF NOT EXISTS(SELECT 1 FROM #saldos WHERE Saldo<0) AND @saldo-@despues-@periodo>=0
        BEGIN
          SELECT {BiReglas.PeriodoSql(f.Agrupacion,"Dia")} Periodo,MIN(Saldo) Minimo,MAX(Saldo) Maximo,AVG(Saldo) Promedio
          FROM #saldos GROUP BY {BiReglas.PeriodoSql(f.Agrupacion,"Dia")} ORDER BY Periodo;
          DECLARE @promedio decimal(28,4)=(SELECT AVG(Saldo) FROM #saldos);
          DECLARE @vendidas decimal(28,4)=COALESCE((SELECT SUM(CAST(v.Cantidad AS bigint)) FROM bi.FactVenta v WHERE v.Procedencia=@procedencia AND v.IdElementoCatalogo=@elemento AND v.Estado=N'Confirmada' AND v.Fecha>=@desde AND v.Fecha<@hasta),0);
          SELECT @promedio StockPromedio,@vendidas UnidadesVendidas,
           CASE WHEN @promedio>0 THEN @vendidas/@promedio END Rotacion,
           CASE WHEN @vendidas>0 THEN (@saldo-@despues)*DATEDIFF(day,@desde,@hasta)/@vendidas END CoberturaDias,
           N'Reconstruido desde saldo de cierre y movimientos registrados; promedio de cierres diarios. Sin valorización: no existe costo histórico fiable.' Metodo;
        END
        ELSE BEGIN SELECT CAST(NULL AS date) Periodo,CAST(NULL AS decimal(28,4)) Minimo,CAST(NULL AS decimal(28,4)) Maximo,CAST(NULL AS decimal(28,4)) Promedio WHERE 1=0;
          SELECT CAST(NULL AS decimal(28,4)) StockPromedio,CAST(NULL AS decimal(28,4)) Rotacion,CAST(NULL AS decimal(28,4)) CoberturaDias,N'No calculable: el saldo reconstruido presenta inconsistencias.' Metodo; END
      END
      ELSE BEGIN SELECT CAST(NULL AS date) Periodo,CAST(NULL AS decimal(28,4)) Minimo,CAST(NULL AS decimal(28,4)) Maximo,CAST(NULL AS decimal(28,4)) Promedio WHERE 1=0;
        SELECT CAST(NULL AS decimal(28,4)) StockPromedio,CAST(NULL AS decimal(28,4)) Rotacion,CAST(NULL AS decimal(28,4)) CoberturaDias,
          N'Selecciona un producto y un periodo posterior a su registro. Los ajustes de signo desconocido o la ausencia de saldo impiden reconstruir una rotación fiable.' Metodo; END
      SELECT COUNT_BIG(*) Productos,
       SUM(CASE WHEN m.Registros=0 THEN 1 ELSE 0 END) SinMovimiento
      FROM #inventario i OUTER APPLY(SELECT COUNT_BIG(*) Registros FROM bi.FactMovimiento x WHERE x.Procedencia=@procedencia AND x.IdProducto=i.IdProducto AND x.Fecha>=@desde AND x.Fecha<@hasta) m;
      """;
}
