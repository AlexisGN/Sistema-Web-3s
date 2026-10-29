using Sistema3S.Web.DTOs.Bi;
namespace Sistema3S.Web.Services.Bi;

internal static class BiConsultas
{
    internal const string DescripcionFiltros="""
       SELECT COALESCE(c.Nombre,CASE WHEN @categoria=0 THEN N'Servicios' ELSE N'Todas las categorías' END) Categoria,
         COALESCE(e.Nombre,N'Todos los productos y servicios') Elemento,
         COALESCE(cl.Nombre,N'Todos los clientes') Cliente,COALESCE(p.Nombre,N'Todos los proveedores') Proveedor
       FROM (VALUES(1)) s(n) LEFT JOIN dbo.Categoria c ON c.IdCategoria=@categoria
       LEFT JOIN bi.DimElemento e ON e.IdElementoCatalogo=@elemento
       LEFT JOIN bi.DimCliente cl ON cl.IdCliente=@cliente AND cl.Procedencia=@procedencia
       LEFT JOIN bi.DimProveedor p ON p.IdProveedor=@proveedor AND p.Procedencia=@procedencia;
       """;
    internal static readonly string[] Vistas=["resumen","ventas","productos","clientes","compras","inventario"];
    internal static string Preparar(BiFiltro f)
    {
        // Cada operación se conserva una vez por elemento. La distribución monetaria vive en las vistas.
        var fuentes=new[]{("venta","Venta"),("compra","Compra"),("cotizacion","Cotizacion")};
        var union=string.Join(" UNION ALL ",fuentes.Select(t=>$"SELECT '{t.Item1}' Tipo,Procedencia,IdOperacion,Fecha,Estado,IdCliente,IdProveedor,IdElementoCatalogo,Cantidad,Total,Base,Igv,Convertida,SinDetalleValido FROM bi.Fact{t.Item2}"));
        var filtros="";
        if(f.Categoria.HasValue) filtros+=" AND e.IdCategoria=@categoria";
        if(f.Elemento.HasValue) filtros+=f.CompararElemento.HasValue?" AND x.IdElementoCatalogo IN(@elemento,@compararElemento)":" AND x.IdElementoCatalogo=@elemento";
        if(f.Cliente.HasValue) filtros+=" AND (x.Tipo='compra' OR x.IdCliente=@cliente)";
        if(f.Proveedor.HasValue) filtros+=" AND (x.Tipo<>'compra' OR x.IdProveedor=@proveedor)";
        if(!string.IsNullOrWhiteSpace(f.Estado)) filtros+=" AND x.Estado=@estado";
        return $"""
        SET NOCOUNT ON; SET LOCK_TIMEOUT 5000;
        SELECT x.Tipo,x.IdOperacion,x.Fecha,x.Estado,x.IdCliente,x.IdProveedor,x.IdElementoCatalogo,
          COALESCE(e.IdCategoria,-1) IdCategoria,COALESCE(e.Unidad,N'Sin unidad') Unidad,
          SUM(CAST(x.Cantidad AS bigint)) Cantidad,SUM(x.Total) Total,SUM(x.Base) Base,SUM(x.Igv) Igv,
          MAX(x.Convertida) Convertida,MAX(CONVERT(int,x.SinDetalleValido)) SinDetalleValido
        INTO #f FROM ({union}) x LEFT JOIN bi.DimElemento e ON e.IdElementoCatalogo=x.IdElementoCatalogo
        WHERE x.Procedencia=@procedencia AND ((x.Fecha>=@desde AND x.Fecha<@hasta) OR (@comparar=1 AND x.Fecha>=@compararDesde AND x.Fecha<@compararHasta)){filtros}
        GROUP BY x.Tipo,x.IdOperacion,x.Fecha,x.Estado,x.IdCliente,x.IdProveedor,x.IdElementoCatalogo,e.IdCategoria,e.Unidad;
        SELECT Tipo,IdOperacion,Fecha,Estado,IdCliente,IdProveedor,SUM(Total) Total,SUM(Base) Base,SUM(Igv) Igv,
          SUM(Cantidad) Cantidad,MAX(Convertida) Convertida,MAX(SinDetalleValido) SinDetalleValido
        INTO #o FROM #f GROUP BY Tipo,IdOperacion,Fecha,Estado,IdCliente,IdProveedor;
        """;
    }
    internal const string Vigente="((Tipo='venta' AND Estado=N'Confirmada') OR (Tipo='compra' AND Estado IN(N'Registrada',N'Confirmada')) OR Tipo='cotizacion')";
    internal static string Kpis => $"""
      SELECT Tipo,COUNT_BIG(*) Operaciones,CAST(SUM(Total) AS decimal(18,2)) Importe,
        CAST(SUM(Base) AS decimal(18,2)) Base,CAST(SUM(Igv) AS decimal(18,2)) Igv,
        COUNT(DISTINCT IdCliente) Clientes,COUNT(DISTINCT IdProveedor) Proveedores,SUM(Convertida) Convertidas,
        SUM(SinDetalleValido) OperacionesSinDetalleValido
      FROM #o WHERE Fecha>=@desde AND Fecha<@hasta AND {Vigente} GROUP BY Tipo;
      SELECT Tipo,COUNT_BIG(*) Operaciones,CAST(SUM(Total) AS decimal(18,2)) Importe
      FROM #o WHERE @comparar=1 AND Fecha>=@compararDesde AND Fecha<@compararHasta AND {Vigente} GROUP BY Tipo;
      SELECT Tipo,Unidad,SUM(Cantidad) Cantidad,COUNT(DISTINCT IdElementoCatalogo) Elementos
      FROM #f WHERE Fecha>=@desde AND Fecha<@hasta AND {Vigente} GROUP BY Tipo,Unidad;
      """;
    internal static string Series(BiFiltro f) => $"""
      WITH dias AS(SELECT CAST(@desde AS date) Fecha,0 Comparacion UNION ALL SELECT CAST(@compararDesde AS date),1 WHERE @comparar=1
        UNION ALL SELECT DATEADD(day,1,Fecha),Comparacion FROM dias WHERE Fecha<DATEADD(day,-1,CASE WHEN Comparacion=0 THEN @hasta ELSE @compararHasta END))
      SELECT DISTINCT Comparacion,{BiReglas.PeriodoSql(f.Agrupacion)} Periodo INTO #calendario FROM dias OPTION(MAXRECURSION 1830);
      WITH agregados AS(SELECT Tipo,{BiReglas.PeriodoSql(f.Agrupacion)} Periodo,CAST(SUM(Total) AS decimal(18,2)) Importe,COUNT_BIG(*) Operaciones,SUM(Convertida) Convertidas,COUNT(DISTINCT IdCliente) Clientes
        FROM #o WHERE Fecha>=@desde AND Fecha<@hasta AND {Vigente} GROUP BY Tipo,{BiReglas.PeriodoSql(f.Agrupacion)})
      SELECT t.Tipo,c.Periodo,COALESCE(a.Importe,0) Importe,COALESCE(a.Operaciones,0) Operaciones,COALESCE(a.Convertidas,0) Convertidas,COALESCE(a.Clientes,0) Clientes
        FROM #calendario c CROSS JOIN(VALUES('venta'),('compra'),('cotizacion')) t(Tipo) LEFT JOIN agregados a ON a.Tipo=t.Tipo AND a.Periodo=c.Periodo WHERE c.Comparacion=0 ORDER BY c.Periodo,t.Tipo;
      WITH agregados AS(SELECT Tipo,{BiReglas.PeriodoSql(f.Agrupacion)} Periodo,CAST(SUM(Total) AS decimal(18,2)) Importe,COUNT_BIG(*) Operaciones
        FROM #o WHERE Fecha>=@compararDesde AND Fecha<@compararHasta AND {Vigente} GROUP BY Tipo,{BiReglas.PeriodoSql(f.Agrupacion)})
      SELECT t.Tipo,c.Periodo,COALESCE(a.Importe,0) Importe,COALESCE(a.Operaciones,0) Operaciones
        FROM #calendario c CROSS JOIN(VALUES('venta'),('compra'),('cotizacion')) t(Tipo) LEFT JOIN agregados a ON a.Tipo=t.Tipo AND a.Periodo=c.Periodo WHERE c.Comparacion=1 ORDER BY c.Periodo,t.Tipo;
      SELECT Tipo,Estado,COUNT_BIG(*) Cantidad,CAST(SUM(Total) AS decimal(18,2)) Importe
      FROM #o WHERE Fecha>=@desde AND Fecha<@hasta GROUP BY Tipo,Estado ORDER BY Tipo,Estado;
      """;
    internal static string Rankings => """
      SELECT f.Tipo,f.IdCategoria Id,COALESCE(c.Nombre,CASE WHEN f.IdCategoria=0 THEN N'Servicios' ELSE N'Sin clasificación' END) Nombre,
       CAST(SUM(f.Total) AS decimal(18,2)) Importe,COUNT(DISTINCT f.IdOperacion) Operaciones
      FROM #f f LEFT JOIN dbo.Categoria c ON c.IdCategoria=f.IdCategoria
      WHERE f.Fecha>=@desde AND f.Fecha<@hasta AND ((f.Tipo='venta' AND f.Estado=N'Confirmada') OR (f.Tipo='compra' AND f.Estado IN(N'Registrada',N'Confirmada')))
      GROUP BY f.Tipo,f.IdCategoria,c.Nombre ORDER BY Importe DESC;
      SELECT TOP(10) f.IdElementoCatalogo Id,e.Nombre,e.TipoElemento,e.Unidad,
        SUM(f.Cantidad) Cantidad,CAST(SUM(f.Total) AS decimal(18,2)) Importe
      FROM #f f JOIN bi.DimElemento e ON e.IdElementoCatalogo=f.IdElementoCatalogo
      WHERE f.Tipo='venta' AND f.Estado=N'Confirmada' AND f.Fecha>=@desde AND f.Fecha<@hasta
      GROUP BY f.IdElementoCatalogo,e.Nombre,e.TipoElemento,e.Unidad ORDER BY Importe DESC,Id;
      SELECT TOP(10) f.IdElementoCatalogo Id,e.Nombre,e.TipoElemento,e.Unidad,
        SUM(f.Cantidad) Cantidad,CAST(SUM(f.Total) AS decimal(18,2)) Importe
      FROM #f f JOIN bi.DimElemento e ON e.IdElementoCatalogo=f.IdElementoCatalogo
      WHERE f.Tipo='venta' AND f.Estado=N'Confirmada' AND f.Fecha>=@desde AND f.Fecha<@hasta
      GROUP BY f.IdElementoCatalogo,e.Nombre,e.TipoElemento,e.Unidad ORDER BY Cantidad DESC,Id;
      SELECT TOP(10) f.IdElementoCatalogo Id,e.Nombre,e.TipoElemento,e.Unidad,
        SUM(f.Cantidad) Cantidad,CAST(SUM(f.Total) AS decimal(18,2)) Importe
      FROM #f f JOIN bi.DimElemento e ON e.IdElementoCatalogo=f.IdElementoCatalogo
      WHERE f.Tipo='venta' AND f.Estado=N'Confirmada' AND f.Fecha>=@desde AND f.Fecha<@hasta
      GROUP BY f.IdElementoCatalogo,e.Nombre,e.TipoElemento,e.Unidad HAVING SUM(f.Cantidad)>0 ORDER BY Cantidad,Id;
      SELECT TOP(10) o.IdCliente Id,c.Nombre,COUNT_BIG(*) Operaciones,CAST(SUM(o.Total) AS decimal(18,2)) Importe
      FROM #o o JOIN bi.DimCliente c ON c.IdCliente=o.IdCliente AND c.Procedencia=@procedencia
      WHERE o.Tipo='venta' AND o.Estado=N'Confirmada' AND o.Fecha>=@desde AND o.Fecha<@hasta
      GROUP BY o.IdCliente,c.Nombre ORDER BY Importe DESC,Id;
      SELECT TOP(10) o.IdCliente Id,c.Nombre,COUNT_BIG(*) Operaciones,CAST(SUM(o.Total) AS decimal(18,2)) Importe
      FROM #o o JOIN bi.DimCliente c ON c.IdCliente=o.IdCliente AND c.Procedencia=@procedencia
      WHERE o.Tipo='venta' AND o.Estado=N'Confirmada' AND o.Fecha>=@desde AND o.Fecha<@hasta
      GROUP BY o.IdCliente,c.Nombre ORDER BY Operaciones DESC,Id;
      SELECT TOP(10) o.IdProveedor Id,p.Nombre,COUNT_BIG(*) Operaciones,CAST(SUM(o.Total) AS decimal(18,2)) Importe
      FROM #o o JOIN bi.DimProveedor p ON p.IdProveedor=o.IdProveedor AND p.Procedencia=@procedencia
      WHERE o.Tipo='compra' AND o.Estado IN(N'Registrada',N'Confirmada') AND o.Fecha>=@desde AND o.Fecha<@hasta
      GROUP BY o.IdProveedor,p.Nombre ORDER BY Importe DESC,Id;
      SELECT TOP(10) f.IdElementoCatalogo Id,e.Nombre,e.Unidad,SUM(f.Cantidad) Cantidad,CAST(SUM(f.Total) AS decimal(18,2)) Importe
      FROM #f f JOIN bi.DimElemento e ON e.IdElementoCatalogo=f.IdElementoCatalogo
      WHERE f.Tipo='compra' AND f.Estado IN(N'Registrada',N'Confirmada') AND f.Fecha>=@desde AND f.Fecha<@hasta
      GROUP BY f.IdElementoCatalogo,e.Nombre,e.Unidad ORDER BY Importe DESC,Id;
      """;
    internal static string SinVenta(BiFiltro f) => $"""
      SELECT e.IdElementoCatalogo Id,e.Nombre,e.TipoElemento,e.Categoria,e.Unidad,COUNT_BIG(*) OVER() TotalFilas
      FROM bi.DimElemento e WHERE e.Estado=1 AND
       ((@procedencia='operativo' AND e.FechaRegistro<@hasta) OR (@procedencia='referencia' AND EXISTS(SELECT 1 FROM bi.FactMovimiento m WHERE m.Procedencia='referencia' AND m.IdProducto=e.IdProducto AND m.Fecha<@hasta)))
       {(f.Categoria.HasValue?"AND e.IdCategoria=@categoria":"")} {(f.Elemento.HasValue?"AND e.IdElementoCatalogo IN(@elemento,COALESCE(@compararElemento,@elemento))":"")}
       AND NOT EXISTS(SELECT 1 FROM #f x WHERE x.IdElementoCatalogo=e.IdElementoCatalogo AND x.Tipo='venta' AND x.Estado=N'Confirmada' AND x.Fecha>=@desde AND x.Fecha<@hasta)
      ORDER BY e.IdElementoCatalogo OFFSET @offset ROWS FETCH NEXT @tamanio ROWS ONLY;
      """;
    internal static string Clientes => """
      SELECT COUNT_BIG(*) AltasEnPeriodo FROM bi.DimCliente WHERE Procedencia=@procedencia AND FechaAlta>=@desde AND FechaAlta<@hasta;
      WITH compradores AS(SELECT IdCliente FROM #o WHERE Tipo='venta' AND Estado=N'Confirmada' AND Fecha>=@desde AND Fecha<@hasta GROUP BY IdCliente),
      segmentos AS(SELECT CASE WHEN EXISTS(SELECT 1 FROM bi.FactVenta v WHERE v.Procedencia=@procedencia AND v.IdCliente=c.IdCliente AND v.Estado=N'Confirmada' AND v.Fecha<@desde)
        THEN N'Recurrente' ELSE N'Primera compra' END Segmento FROM compradores c)
      SELECT Segmento,COUNT_BIG(*) Clientes FROM segmentos GROUP BY Segmento;
      """;
    internal static string EvolucionProductos(BiFiltro f) => $"""
      WITH agregados AS(SELECT f.IdElementoCatalogo,{BiReglas.PeriodoSql(f.Agrupacion,"f.Fecha")} Periodo,
        CAST(SUM(f.Total) AS decimal(18,2)) Importe,SUM(f.Cantidad) Cantidad
        FROM #f f WHERE f.Tipo='venta' AND f.Estado=N'Confirmada' AND f.Fecha>=@desde AND f.Fecha<@hasta
        GROUP BY f.IdElementoCatalogo,{BiReglas.PeriodoSql(f.Agrupacion,"f.Fecha")})
      SELECT e.IdElementoCatalogo Id,e.Nombre,
        CASE WHEN @compararElemento IS NULL THEN e.Nombre WHEN e.IdElementoCatalogo=@elemento THEN N'A · '+e.Nombre ELSE N'B · '+e.Nombre END Serie,
        c.Periodo,COALESCE(a.Importe,0) Importe,COALESCE(a.Cantidad,0) Cantidad,e.Unidad
      FROM bi.DimElemento e CROSS JOIN #calendario c LEFT JOIN agregados a ON a.IdElementoCatalogo=e.IdElementoCatalogo AND a.Periodo=c.Periodo
      WHERE c.Comparacion=0 AND @elemento IS NOT NULL AND e.IdElementoCatalogo IN(@elemento,COALESCE(@compararElemento,@elemento)) ORDER BY Periodo,Id;
      SELECT COUNT_BIG(*) Productos,SUM(CASE WHEN i.StockActual=0 THEN 1 ELSE 0 END) SinStock,
        SUM(CASE WHEN i.StockActual>0 AND i.StockActual<=i.StockMinimo THEN 1 ELSE 0 END) BajoMinimo
      FROM dbo.Inventario i JOIN dbo.Producto p ON p.IdProducto=i.IdProducto
      WHERE @procedencia='operativo' {(f.Categoria.HasValue?"AND p.IdCategoria=@categoria":"")} {(f.Elemento.HasValue?"AND p.IdElementoCatalogo=@elemento":"")};
      """;
    internal const string ComparacionElementos="""
      SELECT seleccion.Orden,e.IdElementoCatalogo Id,e.Nombre,e.TipoElemento,e.Unidad,
        COALESCE(a.Importe,0) Importe,COALESCE(a.Cantidad,0) Cantidad,COALESCE(a.Operaciones,0) Operaciones
      FROM (VALUES(1,@elemento),(2,@compararElemento)) seleccion(Orden,Id)
      JOIN bi.DimElemento e ON e.IdElementoCatalogo=seleccion.Id
      LEFT JOIN(SELECT IdElementoCatalogo,CAST(SUM(Total) AS decimal(18,2)) Importe,SUM(Cantidad) Cantidad,COUNT(DISTINCT IdOperacion) Operaciones
        FROM #f WHERE Tipo='venta' AND Estado=N'Confirmada' AND Fecha>=@desde AND Fecha<@hasta GROUP BY IdElementoCatalogo) a ON a.IdElementoCatalogo=e.IdElementoCatalogo
      WHERE @elemento IS NOT NULL AND @compararElemento IS NOT NULL ORDER BY seleccion.Orden;
      """;
    internal const string Desgloses="""
      SELECT f.Tipo,f.IdElementoCatalogo Id,e.Nombre,e.Unidad,SUM(f.Cantidad) Cantidad,CAST(SUM(f.Total) AS decimal(18,2)) Importe
      FROM #f f JOIN bi.DimElemento e ON e.IdElementoCatalogo=f.IdElementoCatalogo
      WHERE f.Fecha>=@desde AND f.Fecha<@hasta AND ((f.Tipo='venta' AND f.Estado=N'Confirmada') OR (f.Tipo='compra' AND f.Estado IN(N'Registrada',N'Confirmada')))
        AND f.Tipo=@tipoVista
      GROUP BY f.Tipo,f.IdElementoCatalogo,e.Nombre,e.Unidad ORDER BY f.Tipo,Importe DESC,Id;
      SELECT o.IdCliente Id,c.Nombre,COUNT_BIG(*) Cotizaciones,SUM(o.Convertida) Convertidas,
        SUM(CASE WHEN o.Estado IN(N'Pendiente',N'Respondida') AND o.Convertida=0 THEN 1 ELSE 0 END) Oportunidades,
        CAST(SUM(CASE WHEN o.Estado IN(N'Pendiente',N'Respondida') AND o.Convertida=0 THEN o.Total ELSE 0 END) AS decimal(18,2)) ImportePendiente
      FROM #o o JOIN bi.DimCliente c ON c.IdCliente=o.IdCliente AND c.Procedencia=@procedencia
      WHERE o.Tipo='cotizacion' AND o.Fecha>=@desde AND o.Fecha<@hasta
        AND @tipoVista='cotizacion'
      GROUP BY o.IdCliente,c.Nombre ORDER BY ImportePendiente DESC,Id;
      SELECT f.IdElementoCatalogo Id,e.Nombre,e.Unidad,SUM(f.Cantidad) Cantidad,CAST(SUM(f.Total) AS decimal(18,2)) Importe,
        CAST(SUM(f.Total)/NULLIF(SUM(CAST(f.Cantidad AS decimal(28,6))),0) AS decimal(18,2)) CostoUnitario,
        COUNT(DISTINCT f.IdProveedor) Proveedores
      FROM #f f JOIN bi.DimElemento e ON e.IdElementoCatalogo=f.IdElementoCatalogo
      WHERE f.Tipo='compra' AND f.Estado IN(N'Registrada',N'Confirmada') AND f.Fecha>=@desde AND f.Fecha<@hasta
        AND @tipoVista='compra'
      GROUP BY f.IdElementoCatalogo,e.Nombre,e.Unidad ORDER BY Importe DESC,Id;
      """;
    internal static string Sintesis(BiFiltro f)=>$"""
      SELECT e.* INTO #catalogo FROM bi.DimElemento e WHERE e.Estado=1 AND
       ((@procedencia='operativo' AND e.FechaRegistro<@hasta) OR (@procedencia='referencia' AND EXISTS(SELECT 1 FROM bi.FactMovimiento m WHERE m.Procedencia='referencia' AND m.IdProducto=e.IdProducto AND m.Fecha<@hasta)))
       {(f.Categoria.HasValue?"AND e.IdCategoria=@categoria":"")} {(f.Elemento.HasValue?"AND e.IdElementoCatalogo IN(@elemento,COALESCE(@compararElemento,@elemento))":"")};
      SELECT (SELECT COUNT_BIG(*) FROM #catalogo e WHERE NOT EXISTS(SELECT 1 FROM #f x WHERE x.IdElementoCatalogo=e.IdElementoCatalogo AND x.Tipo='venta' AND x.Estado=N'Confirmada' AND x.Fecha>=@desde AND x.Fecha<@hasta)) SinVentas,
       (SELECT COUNT_BIG(*) FROM #catalogo e WHERE e.IdProducto IS NOT NULL AND NOT EXISTS(SELECT 1 FROM bi.FactMovimiento m WHERE m.Procedencia=@procedencia AND m.IdProducto=e.IdProducto AND m.Fecha>=@desde AND m.Fecha<@hasta)) SinMovimiento,
       (SELECT COUNT_BIG(*) FROM (SELECT IdCliente FROM #o WHERE Tipo='venta' AND Estado=N'Confirmada' AND Fecha>=@desde AND Fecha<@hasta GROUP BY IdCliente HAVING COUNT(*)>1 OR EXISTS(SELECT 1 FROM bi.FactVenta v WHERE v.Procedencia=@procedencia AND v.IdCliente=#o.IdCliente AND v.Estado=N'Confirmada' AND v.Fecha<@desde)) compradores) ClientesRecurrentes;
      """;
    internal static string Operaciones => """
      SELECT o.Tipo,o.IdOperacion Id,o.Fecha,o.Estado,CAST(o.Total AS decimal(18,2)) Importe,
        COALESCE(c.Nombre,p.Nombre,N'Sin tercero') Tercero,COUNT_BIG(*) OVER() TotalFilas
      FROM #o o LEFT JOIN bi.DimCliente c ON c.IdCliente=o.IdCliente AND c.Procedencia=@procedencia
      LEFT JOIN bi.DimProveedor p ON p.IdProveedor=o.IdProveedor AND p.Procedencia=@procedencia
      WHERE o.Fecha>=@desde AND o.Fecha<@hasta AND (@tipoVista IS NULL OR o.Tipo=@tipoVista) ORDER BY o.Fecha DESC,o.Tipo,o.IdOperacion
      OFFSET @offset ROWS FETCH NEXT @tamanio ROWS ONLY;
      """;
}
