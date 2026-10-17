-- Migración BI aditiva. Ejecutar después de verificar respaldo y base destino.
-- No carga historial ni modifica operaciones comerciales. No cambia aislamiento de BD.
SET XACT_ABORT ON;
IF SCHEMA_ID('bi') IS NULL EXEC('CREATE SCHEMA bi AUTHORIZATION dbo');
GO
IF OBJECT_ID('bi.LoteReferencia') IS NULL
CREATE TABLE bi.LoteReferencia(
 IdLote uniqueidentifier NOT NULL PRIMARY KEY,
 Nombre nvarchar(120) NOT NULL UNIQUE, Inicio date NOT NULL, Fin date NOT NULL,
 IncorporadoUtc datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
 Generador nvarchar(100) NOT NULL, Estado varchar(20) NOT NULL CHECK(Estado IN('Preparado','Completo')));
IF OBJECT_ID('bi.EntidadReferencia') IS NULL
CREATE TABLE bi.EntidadReferencia(
 IdEntidad int NOT NULL CHECK(IdEntidad<0), Tipo varchar(10) NOT NULL CHECK(Tipo IN('cliente','proveedor')),
 Nombre nvarchar(200) NOT NULL, FechaAlta date NOT NULL, IdLote uniqueidentifier NOT NULL,
 PRIMARY KEY(Tipo,IdEntidad), FOREIGN KEY(IdLote) REFERENCES bi.LoteReferencia(IdLote));
IF OBJECT_ID('bi.OperacionReferencia') IS NULL
CREATE TABLE bi.OperacionReferencia(
 IdOperacion int IDENTITY NOT NULL PRIMARY KEY, Clave varchar(80) NOT NULL UNIQUE,
 IdLote uniqueidentifier NOT NULL REFERENCES bi.LoteReferencia(IdLote),
 Tipo varchar(12) NOT NULL CHECK(Tipo IN('venta','compra','cotizacion')),
 Fecha datetime2 NOT NULL, Estado nvarchar(50) NOT NULL,
 IdCliente int NULL, IdProveedor int NULL, IdCotizacionReferencia int NULL REFERENCES bi.OperacionReferencia(IdOperacion),
 Base decimal(18,2) NOT NULL CHECK(Base>=0), Igv decimal(18,2) NOT NULL CHECK(Igv>=0),
 Total decimal(18,2) NOT NULL CHECK(Total>=0), Moneda char(3) NOT NULL DEFAULT 'PEN' CHECK(Moneda='PEN'),
 CHECK(Total=Base+Igv));
IF OBJECT_ID('bi.DetalleReferencia') IS NULL
CREATE TABLE bi.DetalleReferencia(
 IdDetalle int IDENTITY NOT NULL PRIMARY KEY, IdOperacion int NOT NULL REFERENCES bi.OperacionReferencia(IdOperacion),
 IdElementoCatalogo int NOT NULL REFERENCES dbo.ElementoCatalogo(IdElementoCatalogo),
 Cantidad int NOT NULL CHECK(Cantidad>0), Importe decimal(18,2) NOT NULL CHECK(Importe>=0),
 UNIQUE(IdOperacion,IdElementoCatalogo));
IF OBJECT_ID('bi.MovimientoReferencia') IS NULL
CREATE TABLE bi.MovimientoReferencia(
 IdMovimiento int IDENTITY NOT NULL PRIMARY KEY, Clave varchar(80) NOT NULL UNIQUE,
 IdLote uniqueidentifier NOT NULL REFERENCES bi.LoteReferencia(IdLote),
 IdProducto int NOT NULL REFERENCES dbo.Producto(IdProducto), Fecha datetime2 NOT NULL,
 Tipo nvarchar(20) NOT NULL CHECK(Tipo IN('Entrada','Salida','Apertura')),
 Cantidad int NOT NULL CHECK(Cantidad>0), IdOperacion int NULL REFERENCES bi.OperacionReferencia(IdOperacion));
GO
CREATE OR ALTER VIEW bi.DimElemento AS
SELECT e.IdElementoCatalogo,e.Nombre,p.IdProducto,COALESCE(p.IdCategoria,0) IdCategoria,
 COALESCE(c.Nombre,N'Servicios') Categoria,CASE WHEN p.IdProducto IS NULL THEN N'Servicio' ELSE N'Producto' END TipoElemento,
 COALESCE(u.Abreviatura,N'Servicio') Unidad,e.Estado,e.FechaRegistro
FROM dbo.ElementoCatalogo e LEFT JOIN dbo.Producto p ON p.IdElementoCatalogo=e.IdElementoCatalogo
LEFT JOIN dbo.Categoria c ON c.IdCategoria=p.IdCategoria LEFT JOIN dbo.UnidadMedida u ON u.IdUnidadMedida=p.IdUnidadMedida;
GO
CREATE OR ALTER VIEW bi.DimCliente AS
SELECT c.IdCliente,COALESCE(ce.RazonSocial,CONCAT(cp.Nombres,N' ',cp.ApellidoPaterno,N' ',cp.ApellidoMaterno),N'Cliente') Nombre,
 c.FechaRegistro FechaAlta,CAST('operativo' AS varchar(12)) Procedencia
FROM dbo.Cliente c LEFT JOIN dbo.ClienteEmpresa ce ON ce.IdCliente=c.IdCliente
LEFT JOIN dbo.ClientePersonaNatural cp ON cp.IdCliente=c.IdCliente
UNION ALL SELECT r.IdEntidad,r.Nombre,CAST(r.FechaAlta AS datetime),CAST('referencia' AS varchar(12)) FROM bi.EntidadReferencia r
JOIN bi.LoteReferencia l ON l.IdLote=r.IdLote AND l.Estado='Completo' WHERE r.Tipo='cliente';
GO
CREATE OR ALTER VIEW bi.DimProveedor AS
SELECT IdProveedor,RazonSocial Nombre,CAST('operativo' AS varchar(12)) Procedencia FROM dbo.Proveedor
UNION ALL SELECT r.IdEntidad,r.Nombre,CAST('referencia' AS varchar(12)) FROM bi.EntidadReferencia r
JOIN bi.LoteReferencia l ON l.IdLote=r.IdLote AND l.Estado='Completo' WHERE r.Tipo='proveedor';
GO
CREATE OR ALTER VIEW bi.FactVenta AS
SELECT CAST('operativo' AS varchar(12)) Procedencia,v.IdVenta IdOperacion,v.FechaVenta Fecha,s.Nombre Estado,
 v.IdCliente IdCliente,CAST(NULL AS int) IdProveedor,d.IdElementoCatalogo IdElementoCatalogo,
 COALESCE(d.Cantidad,0) Cantidad,
 CAST(v.Total*CASE WHEN x.Importe>0 THEN d.Subtotal/x.Importe ELSE 1.0/CASE WHEN x.Filas=0 THEN 1 ELSE x.Filas END END AS decimal(28,8)) Total,
 CAST((v.Subtotal)*CASE WHEN x.Importe>0 THEN d.Subtotal/x.Importe ELSE 1.0/CASE WHEN x.Filas=0 THEN 1 ELSE x.Filas END END AS decimal(28,8)) Base,
 CAST(v.Igv*CASE WHEN x.Importe>0 THEN d.Subtotal/x.Importe ELSE 1.0/CASE WHEN x.Filas=0 THEN 1 ELSE x.Filas END END AS decimal(28,8)) Igv,
 0 Convertida,
 CAST(CASE WHEN x.Importe IS NULL OR x.Importe=0 THEN 1 ELSE 0 END AS bit) SinDetalleValido
FROM dbo.Venta v JOIN dbo.EstadoVenta s ON s.IdEstadoVenta=v.IdEstadoVenta
OUTER APPLY(SELECT SUM(CAST(Subtotal AS decimal(28,8))) Importe, COUNT_BIG(*) Filas FROM dbo.DetalleVenta z WHERE z.IdVenta=v.IdVenta) x
LEFT JOIN dbo.DetalleVenta d ON d.IdVenta=v.IdVenta 
UNION ALL
SELECT CAST('referencia' AS varchar(12)),r.IdOperacion,r.Fecha,r.Estado,r.IdCliente,r.IdProveedor,d.IdElementoCatalogo,d.Cantidad,
 CAST(d.Importe AS decimal(28,8)),CAST(d.Importe*r.Base/NULLIF(r.Total,0) AS decimal(28,8)),
 CAST(d.Importe*r.Igv/NULLIF(r.Total,0) AS decimal(28,8)),0,CAST(0 AS bit)
FROM bi.OperacionReferencia r JOIN bi.LoteReferencia l ON l.IdLote=r.IdLote AND l.Estado='Completo'
JOIN bi.DetalleReferencia d ON d.IdOperacion=r.IdOperacion WHERE r.Tipo='venta';
GO
CREATE OR ALTER VIEW bi.FactCompra AS
SELECT CAST('operativo' AS varchar(12)) Procedencia,v.IdCompra IdOperacion,v.FechaCompra Fecha,s.Nombre Estado,
 CAST(NULL AS int) IdCliente,v.IdProveedor IdProveedor,p.IdElementoCatalogo IdElementoCatalogo,
 COALESCE(d.Cantidad,0) Cantidad,
 CAST(v.Total*CASE WHEN x.Importe>0 THEN d.Subtotal/x.Importe ELSE 1.0/CASE WHEN x.Filas=0 THEN 1 ELSE x.Filas END END AS decimal(28,8)) Total,
 CAST((v.Subtotal)*CASE WHEN x.Importe>0 THEN d.Subtotal/x.Importe ELSE 1.0/CASE WHEN x.Filas=0 THEN 1 ELSE x.Filas END END AS decimal(28,8)) Base,
 CAST(v.Igv*CASE WHEN x.Importe>0 THEN d.Subtotal/x.Importe ELSE 1.0/CASE WHEN x.Filas=0 THEN 1 ELSE x.Filas END END AS decimal(28,8)) Igv,
 0 Convertida,
 CAST(CASE WHEN x.Importe IS NULL OR x.Importe=0 THEN 1 ELSE 0 END AS bit) SinDetalleValido
FROM dbo.Compra v JOIN dbo.EstadoCompra s ON s.IdEstadoCompra=v.IdEstadoCompra
OUTER APPLY(SELECT SUM(CAST(Subtotal AS decimal(28,8))) Importe, COUNT_BIG(*) Filas FROM dbo.DetalleCompra z WHERE z.IdCompra=v.IdCompra) x
LEFT JOIN dbo.DetalleCompra d ON d.IdCompra=v.IdCompra LEFT JOIN dbo.Producto p ON p.IdProducto=d.IdProducto
UNION ALL
SELECT CAST('referencia' AS varchar(12)),r.IdOperacion,r.Fecha,r.Estado,r.IdCliente,r.IdProveedor,d.IdElementoCatalogo,d.Cantidad,
 CAST(d.Importe AS decimal(28,8)),CAST(d.Importe*r.Base/NULLIF(r.Total,0) AS decimal(28,8)),
 CAST(d.Importe*r.Igv/NULLIF(r.Total,0) AS decimal(28,8)),0,CAST(0 AS bit)
FROM bi.OperacionReferencia r JOIN bi.LoteReferencia l ON l.IdLote=r.IdLote AND l.Estado='Completo'
JOIN bi.DetalleReferencia d ON d.IdOperacion=r.IdOperacion WHERE r.Tipo='compra';
GO
CREATE OR ALTER VIEW bi.FactCotizacion AS
SELECT CAST('operativo' AS varchar(12)) Procedencia,v.IdCotizacion IdOperacion,v.FechaCotizacion Fecha,s.Nombre Estado,
 v.IdCliente IdCliente,CAST(NULL AS int) IdProveedor,d.IdElementoCatalogo IdElementoCatalogo,
 COALESCE(d.Cantidad,0) Cantidad,
 CAST(v.Total*CASE WHEN x.Importe>0 THEN d.Subtotal/x.Importe ELSE 1.0/CASE WHEN x.Filas=0 THEN 1 ELSE x.Filas END END AS decimal(28,8)) Total,
 -- En cotizaciones el subtotal incluye IGV. La base analítica es total menos el IGV persistido.
 CAST((v.Total-v.Igv)*CASE WHEN x.Importe>0 THEN d.Subtotal/x.Importe ELSE 1.0/CASE WHEN x.Filas=0 THEN 1 ELSE x.Filas END END AS decimal(28,8)) Base,
 CAST(v.Igv*CASE WHEN x.Importe>0 THEN d.Subtotal/x.Importe ELSE 1.0/CASE WHEN x.Filas=0 THEN 1 ELSE x.Filas END END AS decimal(28,8)) Igv,
 CASE WHEN EXISTS(SELECT 1 FROM dbo.Venta cv JOIN dbo.EstadoVenta cs ON cs.IdEstadoVenta=cv.IdEstadoVenta WHERE cs.Nombre=N'Confirmada' AND (cv.IdCotizacion=v.IdCotizacion OR cv.IdVenta=v.IdVentaGenerada)) THEN 1 ELSE 0 END Convertida,
 CAST(CASE WHEN x.Importe IS NULL OR x.Importe=0 THEN 1 ELSE 0 END AS bit) SinDetalleValido
FROM dbo.Cotizacion v JOIN dbo.EstadoCotizacion s ON s.IdEstadoCotizacion=v.IdEstadoCotizacion
OUTER APPLY(SELECT SUM(CAST(Subtotal AS decimal(28,8))) Importe, COUNT_BIG(*) Filas FROM dbo.DetalleCotizacion z WHERE z.IdCotizacion=v.IdCotizacion) x
LEFT JOIN dbo.DetalleCotizacion d ON d.IdCotizacion=v.IdCotizacion 
UNION ALL
SELECT CAST('referencia' AS varchar(12)),r.IdOperacion,r.Fecha,r.Estado,r.IdCliente,r.IdProveedor,d.IdElementoCatalogo,d.Cantidad,
 CAST(d.Importe AS decimal(28,8)),CAST(d.Importe*r.Base/NULLIF(r.Total,0) AS decimal(28,8)),
 CAST(d.Importe*r.Igv/NULLIF(r.Total,0) AS decimal(28,8)),CASE WHEN EXISTS(SELECT 1 FROM bi.OperacionReferencia cv WHERE cv.IdCotizacionReferencia=r.IdOperacion AND cv.Tipo='venta' AND cv.Estado=N'Confirmada') THEN 1 ELSE 0 END,CAST(0 AS bit)
FROM bi.OperacionReferencia r JOIN bi.LoteReferencia l ON l.IdLote=r.IdLote AND l.Estado='Completo'
JOIN bi.DetalleReferencia d ON d.IdOperacion=r.IdOperacion WHERE r.Tipo='cotizacion';
GO
CREATE OR ALTER VIEW bi.FactMovimiento AS
SELECT CAST('operativo' AS varchar(12)) Procedencia,m.IdMovimientoStock IdMovimiento,m.IdProducto,m.FechaMovimiento Fecha,
 t.Nombre Tipo,m.Cantidad,m.IdVenta,m.IdCompra,
 CASE WHEN t.Nombre=N'Entrada' THEN m.Cantidad WHEN t.Nombre=N'Salida' THEN -m.Cantidad ELSE NULL END Delta
FROM dbo.MovimientoStock m JOIN dbo.TipoMovimientoStock t ON t.IdTipoMovimientoStock=m.IdTipoMovimientoStock
UNION ALL SELECT CAST('referencia' AS varchar(12)),r.IdMovimiento,r.IdProducto,r.Fecha,r.Tipo,r.Cantidad,
 CASE WHEN o.Tipo='venta' THEN o.IdOperacion END,CASE WHEN o.Tipo='compra' THEN o.IdOperacion END,
 CASE WHEN r.Tipo IN(N'Entrada',N'Apertura') THEN r.Cantidad ELSE -r.Cantidad END
FROM bi.MovimientoReferencia r JOIN bi.LoteReferencia l ON l.IdLote=r.IdLote AND l.Estado='Completo'
LEFT JOIN bi.OperacionReferencia o ON o.IdOperacion=r.IdOperacion;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Venta') AND name='IX_BI_Venta_Fecha') CREATE INDEX IX_BI_Venta_Fecha ON dbo.Venta(FechaVenta, IdEstadoVenta) INCLUDE(IdCliente,IdCotizacion,Subtotal,Igv,Total);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Compra') AND name='IX_BI_Compra_Fecha') CREATE INDEX IX_BI_Compra_Fecha ON dbo.Compra(FechaCompra,IdEstadoCompra) INCLUDE(IdProveedor,Subtotal,Igv,Total);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Cotizacion') AND name='IX_BI_Cotizacion_Fecha') CREATE INDEX IX_BI_Cotizacion_Fecha ON dbo.Cotizacion(FechaCotizacion,IdEstadoCotizacion) INCLUDE(IdCliente,IdVentaGenerada,Subtotal,Descuento,Igv,Total);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.DetalleVenta') AND name='IX_BI_DetalleVenta_Cabecera') CREATE INDEX IX_BI_DetalleVenta_Cabecera ON dbo.DetalleVenta(IdVenta,IdElementoCatalogo) INCLUDE(Cantidad,Subtotal);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.DetalleCompra') AND name='IX_BI_DetalleCompra_Cabecera') CREATE INDEX IX_BI_DetalleCompra_Cabecera ON dbo.DetalleCompra(IdCompra,IdProducto) INCLUDE(Cantidad,Subtotal);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.DetalleCotizacion') AND name='IX_BI_DetalleCotizacion_Cabecera') CREATE INDEX IX_BI_DetalleCotizacion_Cabecera ON dbo.DetalleCotizacion(IdCotizacion,IdElementoCatalogo) INCLUDE(Cantidad,Subtotal);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.MovimientoStock') AND name='IX_BI_Movimiento_Fecha') CREATE INDEX IX_BI_Movimiento_Fecha ON dbo.MovimientoStock(FechaMovimiento,IdProducto) INCLUDE(Cantidad,IdTipoMovimientoStock,IdVenta,IdCompra);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('bi.OperacionReferencia') AND name='IX_BI_Referencia_Fecha') CREATE INDEX IX_BI_Referencia_Fecha ON bi.OperacionReferencia(Tipo,Fecha,Estado) INCLUDE(IdCliente,IdProveedor,Base,Igv,Total,IdLote);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('bi.DetalleReferencia') AND name='IX_BI_Referencia_Cabecera') CREATE INDEX IX_BI_Referencia_Cabecera ON bi.DetalleReferencia(IdOperacion,IdElementoCatalogo) INCLUDE(Cantidad,Importe);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('bi.MovimientoReferencia') AND name='IX_BI_Referencia_Movimiento') CREATE INDEX IX_BI_Referencia_Movimiento ON bi.MovimientoReferencia(Fecha,IdProducto) INCLUDE(Tipo,Cantidad,IdOperacion,IdLote);
GO
-- Los permisos sólo se incorporan al catálogo. No concede BI a roles internos distintos del administrador.
INSERT dbo.Permiso(Nombre,Descripcion,Estado)
SELECT x.Nombre,x.Descripcion,1 FROM(VALUES
 (N'BI_VER',N'Consultar Inteligencia de Negocios.'),
 (N'BI_EXPORTAR',N'Exportar reportes de Inteligencia de Negocios.'),
 (N'BI_REFERENCIA',N'Consultar exclusivamente historial sintético de referencia.')) x(Nombre,Descripcion)
WHERE NOT EXISTS(SELECT 1 FROM dbo.Permiso p WHERE p.Nombre=x.Nombre);
GO
