-- Inspección de calidad: sólo SELECT. No corrige, borra ni reclasifica datos legítimos.
SELECT 'Venta total distinto de base e IGV' Regla,COUNT_BIG(*) Cantidad
FROM dbo.Venta WHERE ABS(Total-Subtotal-Igv)>0.02
UNION ALL SELECT 'Compra total distinto de base e IGV',COUNT_BIG(*) FROM dbo.Compra WHERE ABS(Total-Subtotal-Igv)>0.02
UNION ALL SELECT 'Cotización total distinto de subtotal menos descuento',COUNT_BIG(*) FROM dbo.Cotizacion WHERE ABS(Total-Subtotal+Descuento)>0.02
UNION ALL SELECT 'Cotización IGV fuera del total',COUNT_BIG(*) FROM dbo.Cotizacion WHERE Igv<0 OR Igv>Total
UNION ALL SELECT 'Venta sin detalle',COUNT_BIG(*) FROM dbo.Venta v WHERE NOT EXISTS(SELECT 1 FROM dbo.DetalleVenta d WHERE d.IdVenta=v.IdVenta)
UNION ALL SELECT 'Compra sin detalle',COUNT_BIG(*) FROM dbo.Compra v WHERE NOT EXISTS(SELECT 1 FROM dbo.DetalleCompra d WHERE d.IdCompra=v.IdCompra)
UNION ALL SELECT 'Stock negativo',COUNT_BIG(*) FROM dbo.Inventario WHERE StockActual<0
UNION ALL SELECT 'Ajuste sin signo estructurado',COUNT_BIG(*) FROM dbo.MovimientoStock m JOIN dbo.TipoMovimientoStock t ON t.IdTipoMovimientoStock=m.IdTipoMovimientoStock WHERE t.Nombre=N'Ajuste'
UNION ALL SELECT 'Movimiento sin producto',COUNT_BIG(*) FROM dbo.MovimientoStock m WHERE NOT EXISTS(SELECT 1 FROM dbo.Producto p WHERE p.IdProducto=m.IdProducto)
UNION ALL SELECT 'Venta sin cliente',COUNT_BIG(*) FROM dbo.Venta v WHERE NOT EXISTS(SELECT 1 FROM dbo.Cliente c WHERE c.IdCliente=v.IdCliente);

SELECT 'Operación referencia sin cliente válido' Regla,COUNT_BIG(*) Cantidad FROM bi.OperacionReferencia o
WHERE o.IdCliente IS NOT NULL AND NOT EXISTS(SELECT 1 FROM bi.EntidadReferencia e WHERE e.Tipo='cliente' AND e.IdEntidad=o.IdCliente AND e.IdLote=o.IdLote)
UNION ALL SELECT 'Referencia sin proveedor válido',COUNT_BIG(*) FROM bi.OperacionReferencia o WHERE o.IdProveedor IS NOT NULL AND NOT EXISTS(SELECT 1 FROM bi.EntidadReferencia e WHERE e.Tipo='proveedor' AND e.IdEntidad=o.IdProveedor AND e.IdLote=o.IdLote)
UNION ALL SELECT 'Total referencia inconsistente',COUNT_BIG(*) FROM bi.OperacionReferencia o WHERE ABS(o.Total-(SELECT SUM(d.Importe) FROM bi.DetalleReferencia d WHERE d.IdOperacion=o.IdOperacion))>0.02;
