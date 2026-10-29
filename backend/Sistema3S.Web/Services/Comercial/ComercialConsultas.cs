namespace Sistema3S.Web.Services.Comercial;

internal static class ComercialConsultas
{
 internal const string Preparar="""
 SET NOCOUNT ON;SET LOCK_TIMEOUT 5000;
 SELECT v.IdOperacion,v.IdCliente,v.IdElementoCatalogo,v.Fecha,v.Base,v.Cantidad,e.Nombre,e.IdCategoria,e.Unidad
 INTO #v FROM bi.FactVenta v JOIN bi.DimElemento e ON e.IdElementoCatalogo=v.IdElementoCatalogo
 WHERE v.Procedencia='operativo' AND v.Estado=N'Confirmada' AND v.SinDetalleValido=0
 AND ((v.Fecha>=@a AND v.Fecha<@z) OR (@base=1 AND v.Fecha>=@b AND v.Fecha<@y))
 AND (@categoria IS NULL OR e.IdCategoria=@categoria) AND (@elemento IS NULL OR v.IdElementoCatalogo=@elemento) AND (@cliente IS NULL OR v.IdCliente=@cliente);
 SELECT IdOperacion,IdCliente,Fecha,SUM(Base) Base INTO #o FROM #v GROUP BY IdOperacion,IdCliente,Fecha;
 SELECT 'A' Periodo,@a Desde,@z FinExclusivo INTO #periodos UNION ALL SELECT 'B',@b,@y WHERE @comparar=1;
 """;
 internal const string Indicadores="""
 SELECT p.Periodo,p.Desde,DATEADD(day,-1,p.FinExclusivo) Hasta,DATEDIFF(day,p.Desde,p.FinExclusivo) Dias,
 COALESCE(v.Base,0) VentaNeta,COALESCE(v.Operaciones,0) Operaciones,COALESCE(v.Compradores,0) Compradores,
 CASE WHEN v.Operaciones>0 THEN v.Base/v.Operaciones END Ticket,
 COALESCE(r.Recurrentes,0) Recurrentes,CASE WHEN v.Compradores>0 THEN 100.0*r.Recurrentes/v.Compradores END TasaRecurrentes,
 COALESCE(q.Elegibles,0) Elegibles,COALESCE(q.Convertidas,0) Convertidas,COALESCE(q.Emitidas,0) Emitidas,
 CASE WHEN q.Elegibles>0 THEN 100.0*q.Convertidas/q.Elegibles END Conversion,
 CASE WHEN p.FinExclusivo>@hoy THEN 1 ELSE 0 END DiaParcial
 FROM #periodos p
 OUTER APPLY(SELECT SUM(Base) Base,COUNT(*) Operaciones,COUNT(DISTINCT IdCliente) Compradores FROM #o WHERE Fecha>=p.Desde AND Fecha<p.FinExclusivo)v
 OUTER APPLY(SELECT COUNT(*) Recurrentes FROM(SELECT DISTINCT IdCliente FROM #o WHERE Fecha>=p.Desde AND Fecha<p.FinExclusivo)c
 WHERE EXISTS(SELECT 1 FROM dbo.Venta anterior JOIN dbo.EstadoVenta ev ON ev.IdEstadoVenta=anterior.IdEstadoVenta WHERE anterior.IdCliente=c.IdCliente AND anterior.FechaVenta<p.Desde AND ev.Nombre=N'Confirmada'))r
 OUTER APPLY(SELECT COUNT(*) Emitidas,
 SUM(CASE WHEN DATEADD(day,@d,CONVERT(date,c.FechaCotizacion))<@hoy THEN 1 ELSE 0 END) Elegibles,
 SUM(CASE WHEN DATEADD(day,@d,CONVERT(date,c.FechaCotizacion))<@hoy AND EXISTS_PLACEHOLDER=1 THEN 1 ELSE 0 END) Convertidas
 FROM (SELECT c.*,CASE WHEN EXISTS(SELECT 1 FROM dbo.Venta v JOIN dbo.EstadoVenta ev ON ev.IdEstadoVenta=v.IdEstadoVenta
 WHERE (v.IdCotizacion=c.IdCotizacion OR v.IdVenta=c.IdVentaGenerada) AND ev.Nombre=N'Confirmada' AND v.FechaVenta>=c.FechaCotizacion
 AND v.FechaVenta<DATEADD(day,@d+1,CONVERT(date,c.FechaCotizacion))) THEN 1 ELSE 0 END EXISTS_PLACEHOLDER
 FROM dbo.Cotizacion c WHERE c.FechaCotizacion>=p.Desde AND c.FechaCotizacion<p.FinExclusivo AND (@cliente IS NULL OR c.IdCliente=@cliente)
 AND ((@elemento IS NULL AND @categoria IS NULL) OR EXISTS(SELECT 1 FROM dbo.DetalleCotizacion dc JOIN bi.DimElemento e ON e.IdElementoCatalogo=dc.IdElementoCatalogo WHERE dc.IdCotizacion=c.IdCotizacion AND (@elemento IS NULL OR dc.IdElementoCatalogo=@elemento) AND (@categoria IS NULL OR e.IdCategoria=@categoria))))c)q
 ORDER BY p.Periodo;
 SELECT COUNT(*) Productos,SUM(CASE WHEN i.StockActual=0 THEN 1 ELSE 0 END) SinStock,
 SUM(CASE WHEN i.StockActual>=0 AND i.StockActual<=i.StockMinimo THEN 1 ELSE 0 END) Criticos,
 SUM(CASE WHEN i.StockActual IS NULL OR i.StockActual<0 THEN 1 ELSE 0 END) SaldoNoValido,
 100.0*SUM(CASE WHEN i.StockActual=0 THEN 1 ELSE 0 END)/NULLIF(COUNT(*),0) PorcentajeSinStock,
 100.0*SUM(CASE WHEN i.StockActual>=0 AND i.StockActual<=i.StockMinimo THEN 1 ELSE 0 END)/NULLIF(COUNT(*),0) PorcentajeCritico
 FROM dbo.Producto p JOIN dbo.ElementoCatalogo e ON e.IdElementoCatalogo=p.IdElementoCatalogo LEFT JOIN dbo.Inventario i ON i.IdProducto=p.IdProducto
 WHERE e.Estado=1 AND p.AplicaInventario=1 AND (@categoria IS NULL OR p.IdCategoria=@categoria) AND (@elemento IS NULL OR e.IdElementoCatalogo=@elemento);
 """;
 internal const string Series="""
 SELECT p.Periodo,CONVERT(char(7),v.Fecha,126) Nombre,CAST(SUM(v.Base) AS decimal(18,2)) Importe FROM #v v JOIN #periodos p ON v.Fecha>=p.Desde AND v.Fecha<p.FinExclusivo GROUP BY p.Periodo,CONVERT(char(7),v.Fecha,126) ORDER BY Nombre,Periodo;
 """;
 internal const string Productos="""
 WITH t AS(SELECT v.IdElementoCatalogo Id,MAX(v.Nombre) Nombre,MAX(v.Unidad) Unidad,
 SUM(CASE WHEN v.Fecha>=@a AND v.Fecha<@z THEN v.Base ELSE 0 END) A,SUM(CASE WHEN v.Fecha>=@b AND v.Fecha<@y THEN v.Base ELSE 0 END) B,
 SUM(CASE WHEN v.Fecha>=@a AND v.Fecha<@z THEN v.Cantidad ELSE 0 END) Cantidad,
 COUNT(DISTINCT CASE WHEN v.Fecha>=@b AND v.Fecha<@y THEN v.IdOperacion END) OperacionesB
 FROM #v v GROUP BY v.IdElementoCatalogo)
 SELECT TOP(100) *,A-B Diferencia,100*(A-B)/NULLIF(B,0) Variacion FROM t
 WHERE @escenario<>'productos-caida' OR (B>0 AND A<B AND 100*(A-B)/B<=-@umbral AND OperacionesB>=@minOps)
 ORDER BY CASE WHEN @escenario='productos-caida' THEN A-B ELSE -A END,Id;
 WITH t AS(SELECT COALESCE(c.Nombre,N'Servicios / sin categoría') Nombre,
 SUM(CASE WHEN v.Fecha>=@a AND v.Fecha<@z THEN v.Base ELSE 0 END) A,
 SUM(CASE WHEN @base=1 AND v.Fecha>=@b AND v.Fecha<@y THEN v.Base ELSE 0 END) B,
 COUNT(DISTINCT CASE WHEN @base=1 AND v.Fecha>=@b AND v.Fecha<@y THEN v.IdOperacion END) OperacionesB
 FROM #v v LEFT JOIN dbo.Categoria c ON c.IdCategoria=v.IdCategoria GROUP BY c.Nombre),
 elegible AS(SELECT * FROM t WHERE @escenario<>'categorias-caida' OR (B>0 AND A<B AND OperacionesB>=@minOps AND 100*(A-B)/B<=-@umbral))
 SELECT Nombre,'A' Periodo,A Importe FROM elegible WHERE A<>0 OR @escenario='categorias-caida'
 UNION ALL SELECT Nombre,'B',B FROM elegible WHERE @comparar=1 ORDER BY Nombre,Periodo;
 """;
 internal const string Clientes="""
 WITH t AS(SELECT IdCliente Id,SUM(CASE WHEN Fecha>=@a AND Fecha<@z THEN Base ELSE 0 END) A,SUM(CASE WHEN Fecha>=@b AND Fecha<@y THEN Base ELSE 0 END) B,
 COUNT(CASE WHEN Fecha>=@a AND Fecha<@z THEN 1 END) OperacionesA,COUNT(CASE WHEN Fecha>=@b AND Fecha<@y THEN 1 END) OperacionesB
 FROM #o GROUP BY IdCliente)
 SELECT TOP(100) t.*,d.Nombre,A-B Diferencia,100*(A-B)/NULLIF(B,0) Variacion
 FROM t JOIN bi.DimCliente d ON d.IdCliente=t.Id AND d.Procedencia='operativo'
 WHERE @escenario<>'clientes-caida' OR (B>0 AND B>=@importe AND A<B AND EXISTS(SELECT 1 FROM dbo.Venta prev JOIN dbo.EstadoVenta est ON est.IdEstadoVenta=prev.IdEstadoVenta WHERE prev.IdCliente=t.Id AND prev.FechaVenta<@a AND est.Nombre=N'Confirmada') AND 100*(A-B)/B<=-@umbral AND OperacionesB>=@minOps)
 ORDER BY CASE WHEN @escenario='clientes-caida' THEN A-B ELSE -A END,t.Id;
 """;
 internal const string Cotizaciones="""
 SELECT TOP(100) q.IdCotizacion Id,c.Nombre,q.FechaCotizacion Fecha,q.Total Importe,e.Nombre Estado,DATEDIFF(day,q.FechaCotizacion,@hoy) Antiguedad
 FROM dbo.Cotizacion q JOIN dbo.EstadoCotizacion e ON e.IdEstadoCotizacion=q.IdEstadoCotizacion JOIN bi.DimCliente c ON c.IdCliente=q.IdCliente AND c.Procedencia='operativo'
 WHERE e.Nombre IN(N'Pendiente',N'Respondida',N'Aprobada') AND q.FechaCotizacion>=@a AND q.FechaCotizacion<@z AND (@cliente IS NULL OR q.IdCliente=@cliente)
 AND NOT EXISTS(SELECT 1 FROM dbo.Venta v JOIN dbo.EstadoVenta ev ON ev.IdEstadoVenta=v.IdEstadoVenta WHERE (v.IdCotizacion=q.IdCotizacion OR v.IdVenta=q.IdVentaGenerada) AND ev.Nombre=N'Confirmada')
 AND ((@elemento IS NULL AND @categoria IS NULL) OR EXISTS(SELECT 1 FROM dbo.DetalleCotizacion dc JOIN bi.DimElemento de ON de.IdElementoCatalogo=dc.IdElementoCatalogo WHERE dc.IdCotizacion=q.IdCotizacion AND (@elemento IS NULL OR dc.IdElementoCatalogo=@elemento) AND (@categoria IS NULL OR de.IdCategoria=@categoria)))
 AND (@escenario<>'cotizaciones-seguimiento' OR (DATEDIFF(day,q.FechaCotizacion,@hoy)>=@inactividad AND q.Total>=@importe))
 ORDER BY q.FechaCotizacion,q.IdCotizacion;
 """;
 internal const string Inventario="""
 SELECT p.IdProducto Id,e.IdElementoCatalogo,e.Nombre,u.Abreviatura Unidad,i.StockActual,i.StockMinimo,
 COALESCE(v.Cantidad,0) Vendido,ult.UltimaVenta,DATEDIFF(day,ult.UltimaVenta,@hoy) DiasSinVenta,
 CASE WHEN v.Cantidad>0 THEN i.StockActual*1.0*DATEDIFF(day,@a,@z)/v.Cantidad END CoberturaHistorica,
 CASE WHEN i.StockActual IS NULL OR i.StockActual<0 THEN N'Saldo por revisar' WHEN i.StockActual=0 THEN N'Sin stock' WHEN i.StockActual<=i.StockMinimo THEN N'Stock crítico' ELSE N'Disponible' END Estado
 FROM dbo.Producto p JOIN dbo.ElementoCatalogo e ON e.IdElementoCatalogo=p.IdElementoCatalogo JOIN dbo.UnidadMedida u ON u.IdUnidadMedida=p.IdUnidadMedida
 LEFT JOIN dbo.Inventario i ON i.IdProducto=p.IdProducto
 OUTER APPLY(SELECT SUM(Cantidad) Cantidad FROM #v WHERE IdElementoCatalogo=e.IdElementoCatalogo AND Fecha>=@a AND Fecha<@z)v
 OUTER APPLY(SELECT MAX(f.Fecha) UltimaVenta FROM bi.FactVenta f WHERE f.Procedencia='operativo' AND f.Estado=N'Confirmada' AND f.IdElementoCatalogo=e.IdElementoCatalogo AND f.Fecha<@hoy)ult
 WHERE e.Estado=1 AND p.AplicaInventario=1 AND (@categoria IS NULL OR p.IdCategoria=@categoria) AND (@elemento IS NULL OR e.IdElementoCatalogo=@elemento)
 AND (@escenario<>'criticos-ventas' OR (i.StockActual>=0 AND i.StockActual<=i.StockMinimo AND v.Cantidad>0))
 AND (@escenario<>'stock-inactivo' OR (i.StockActual>0 AND e.FechaRegistro<=DATEADD(day,-@inactividad,@hoy) AND NOT EXISTS(SELECT 1 FROM bi.FactVenta f WHERE f.Procedencia='operativo' AND f.Estado=N'Confirmada' AND f.IdElementoCatalogo=e.IdElementoCatalogo AND f.Fecha>=DATEADD(day,-@inactividad,@hoy) AND f.Fecha<@hoy)))
 ORDER BY CASE WHEN i.StockActual=0 THEN 0 WHEN i.StockActual<=i.StockMinimo THEN 1 ELSE 2 END,e.Nombre;
 """;
 internal const string Compras="""
 WITH t AS(SELECT f.IdProveedor Id,SUM(f.Base) Importe,COUNT(DISTINCT f.IdOperacion) Operaciones FROM bi.FactCompra f JOIN bi.DimElemento e ON e.IdElementoCatalogo=f.IdElementoCatalogo
 WHERE f.Procedencia='operativo' AND f.Estado IN(N'Registrada',N'Confirmada') AND f.Fecha>=@a AND f.Fecha<@z
 AND (@categoria IS NULL OR e.IdCategoria=@categoria) AND (@elemento IS NULL OR f.IdElementoCatalogo=@elemento) AND (@proveedor IS NULL OR f.IdProveedor=@proveedor) GROUP BY f.IdProveedor),
 total AS(SELECT *,100*Importe/NULLIF(SUM(Importe) OVER(),0) Participacion FROM t)
 SELECT TOP(100) t.*,p.Nombre FROM total t JOIN bi.DimProveedor p ON p.IdProveedor=t.Id AND p.Procedencia='operativo'
 WHERE @escenario<>'proveedor-concentrado' OR (t.Participacion>=@umbral AND t.Operaciones>=@minOps) ORDER BY Importe DESC,t.Id;
 """;
 internal const string ComprasIndicadores="""
 SELECT f.IdOperacion,f.IdProveedor,f.Fecha,f.Base INTO #compras
 FROM bi.FactCompra f JOIN bi.DimElemento e ON e.IdElementoCatalogo=f.IdElementoCatalogo
 WHERE f.Procedencia='operativo' AND f.Estado IN(N'Registrada',N'Confirmada')
 AND ((f.Fecha>=@a AND f.Fecha<@z) OR (@comparar=1 AND f.Fecha>=@b AND f.Fecha<@y))
 AND (@categoria IS NULL OR e.IdCategoria=@categoria) AND (@elemento IS NULL OR f.IdElementoCatalogo=@elemento)
 AND (@proveedor IS NULL OR f.IdProveedor=@proveedor);
 SELECT p.Periodo,p.Desde,DATEADD(day,-1,p.FinExclusivo) Hasta,DATEDIFF(day,p.Desde,p.FinExclusivo) Dias,
 COALESCE(c.Importe,0) CompraNeta,c.Operaciones Compras,c.Proveedores,
 CASE WHEN c.Operaciones>0 THEN c.Importe/c.Operaciones END PromedioCompra,
 CASE WHEN p.FinExclusivo>@hoy THEN 1 ELSE 0 END DiaParcial
 FROM #periodos p OUTER APPLY(SELECT SUM(Base) Importe,COUNT(DISTINCT IdOperacion) Operaciones,
 COUNT(DISTINCT IdProveedor) Proveedores FROM #compras WHERE Fecha>=p.Desde AND Fecha<p.FinExclusivo)c ORDER BY p.Periodo;
 SELECT p.Periodo,CONVERT(char(7),c.Fecha,126) Nombre,CAST(SUM(c.Base) AS decimal(18,2)) Importe
 FROM #compras c JOIN #periodos p ON c.Fecha>=p.Desde AND c.Fecha<p.FinExclusivo
 GROUP BY p.Periodo,CONVERT(char(7),c.Fecha,126) ORDER BY Nombre,Periodo;
 """;
}
