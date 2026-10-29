-- Instalación aditiva. No incluye ventas, referencias, modelos ni predicciones.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF SCHEMA_ID(N'ia') IS NULL EXEC(N'CREATE SCHEMA ia AUTHORIZATION dbo');
IF OBJECT_ID(N'ia.Ejecucion',N'U') IS NULL
CREATE TABLE ia.Ejecucion(
 Id bigint IDENTITY PRIMARY KEY, Procedencia varchar(12) NOT NULL CHECK(Procedencia IN('operativo','referencia')),
 Corte date NOT NULL, Huella char(64) NOT NULL, Creada datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
 Estado varchar(20) NOT NULL CHECK(Estado IN('EnCurso','Completada','Fallida')), Finalizada datetime2 NULL,
 Resultado nvarchar(max) NULL CHECK(Resultado IS NULL OR ISJSON(Resultado)=1), Error varchar(80) NULL,
 CONSTRAINT UQ_IA_Ejecucion UNIQUE(Procedencia,Corte,Huella));
IF OBJECT_ID(N'ia.Pronostico',N'U') IS NULL
CREATE TABLE ia.Pronostico(
 Id bigint IDENTITY PRIMARY KEY, IdEjecucion bigint NOT NULL REFERENCES ia.Ejecucion(Id),
 IdProducto int NOT NULL REFERENCES dbo.Producto(IdProducto), Desde date NOT NULL, Hasta date NOT NULL,
 Horizonte smallint NOT NULL CHECK(Horizonte IN(30,60,90)), Cantidad decimal(18,6) NOT NULL CHECK(Cantidad>=0),
 Inferior decimal(18,6) NULL, Superior decimal(18,6) NULL,
 Real decimal(18,6) NULL, Evaluada datetime2 NULL, CONSTRAINT UQ_IA_Pronostico UNIQUE(IdEjecucion,IdProducto,Horizonte));
IF OBJECT_ID(N'ia.Abastecimiento',N'U') IS NULL
CREATE TABLE ia.Abastecimiento(
 IdProducto int PRIMARY KEY REFERENCES dbo.Producto(IdProducto), IdProveedor int NOT NULL REFERENCES dbo.Proveedor(IdProveedor),
 PlazoDias smallint NOT NULL CHECK(PlazoDias BETWEEN 1 AND 90), CoberturaDias smallint NOT NULL CHECK(CoberturaDias BETWEEN 1 AND 89),
 Seguridad decimal(18,4) NOT NULL CHECK(Seguridad>=0), Minimo int NOT NULL DEFAULT 1 CHECK(Minimo>=1),
 Multiplo int NOT NULL DEFAULT 1 CHECK(Multiplo>=1),
 Reservado decimal(18,4) NOT NULL DEFAULT 0 CHECK(Reservado>=0),
 EntradaConfirmada decimal(18,4) NOT NULL DEFAULT 0 CHECK(EntradaConfirmada>=0), FechaEntrada date NULL,
 Evidencia nvarchar(500) NOT NULL, VerificadaHasta date NOT NULL,
 Actualizada datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), IdUsuario int NOT NULL REFERENCES dbo.Usuario(IdUsuario),
 CHECK(PlazoDias+CoberturaDias<=90), CHECK(EntradaConfirmada=0 OR FechaEntrada IS NOT NULL));
-- Parámetros ficticios identificados, exclusivos de referencia de desarrollo.
IF OBJECT_ID(N'ia.AbastecimientoReferencia',N'U') IS NULL
CREATE TABLE ia.AbastecimientoReferencia(
 IdProducto int PRIMARY KEY REFERENCES dbo.Producto(IdProducto), IdLote uniqueidentifier NOT NULL REFERENCES bi.LoteReferencia(IdLote),
 IdProveedor int NOT NULL REFERENCES dbo.Proveedor(IdProveedor), PlazoDias smallint NOT NULL CHECK(PlazoDias BETWEEN 1 AND 90),
 CoberturaDias smallint NOT NULL CHECK(CoberturaDias>=1), Seguridad decimal(18,4) NOT NULL CHECK(Seguridad>=0),
 Minimo int NOT NULL CHECK(Minimo>=1),Multiplo int NOT NULL CHECK(Multiplo>=1),Reservado decimal(18,4) NOT NULL CHECK(Reservado>=0),
 EntradaConfirmada decimal(18,4) NOT NULL CHECK(EntradaConfirmada>=0),FechaEntrada date NULL,Evidencia nvarchar(500) NOT NULL,
 VerificadaHasta date NOT NULL,CHECK(PlazoDias+CoberturaDias<=90),CHECK(EntradaConfirmada=0 OR FechaEntrada IS NOT NULL));
IF OBJECT_ID(N'ia.Recomendacion',N'U') IS NULL
CREATE TABLE ia.Recomendacion(
 Id bigint IDENTITY PRIMARY KEY, Procedencia varchar(12) NOT NULL CHECK(Procedencia IN('operativo','referencia')),
 IdProducto int NOT NULL REFERENCES dbo.Producto(IdProducto), IdEjecucion bigint NULL REFERENCES ia.Ejecucion(Id),
 Prioridad varchar(40) NOT NULL, Cantidad decimal(18,4) NULL, FechaPedido date NULL,
 Mensaje nvarchar(1000) NOT NULL, Estado varchar(20) NOT NULL DEFAULT 'Pendiente'
 CHECK(Estado IN('Pendiente','Revisada','Aceptada','Descartada','Vinculada','Atendida')),
 Firma char(64) NOT NULL, Creada datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
 Revisada datetime2 NULL, IdUsuario int NULL REFERENCES dbo.Usuario(IdUsuario), Motivo nvarchar(500) NULL,
 CONSTRAINT UQ_IA_Alerta UNIQUE(Procedencia,IdProducto,Firma));
IF OBJECT_ID(N'ia.SolicitudCompra',N'U') IS NULL
CREATE TABLE ia.SolicitudCompra(
 Id bigint IDENTITY PRIMARY KEY, IdRecomendacion bigint NOT NULL UNIQUE REFERENCES ia.Recomendacion(Id),
 IdProducto int NOT NULL REFERENCES dbo.Producto(IdProducto), IdProveedor int NOT NULL REFERENCES dbo.Proveedor(IdProveedor),
 Cantidad int NOT NULL CHECK(Cantidad>0), PrecioConIgv decimal(18,4) NULL CHECK(PrecioConIgv IS NULL OR PrecioConIgv>0),
 Justificacion nvarchar(500) NOT NULL, IdUsuario int NOT NULL REFERENCES dbo.Usuario(IdUsuario),
 Creada datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), IdCompra int NULL REFERENCES dbo.Compra(IdCompra),
 Vinculada datetime2 NULL);
IF COL_LENGTH('ia.Recomendacion','Prioridad')<40
 ALTER TABLE ia.Recomendacion ALTER COLUMN Prioridad varchar(40) NOT NULL;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('ia.Ejecucion') AND name='IX_IA_Lectura')
 CREATE INDEX IX_IA_Lectura ON ia.Ejecucion(Procedencia,Estado,Corte DESC,Id DESC);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('ia.Pronostico') AND name='IX_IA_Evaluar')
 CREATE INDEX IX_IA_Evaluar ON ia.Pronostico(Hasta,Evaluada) INCLUDE(IdEjecucion,IdProducto,Cantidad,Desde,Horizonte);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('ia.Recomendacion') AND name='IX_IA_Pendientes')
 CREATE INDEX IX_IA_Pendientes ON ia.Recomendacion(Procedencia,Estado,IdProducto,Id DESC);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('ia.SolicitudCompra') AND name='UQ_IA_CompraVinculada')
 CREATE UNIQUE INDEX UQ_IA_CompraVinculada ON ia.SolicitudCompra(IdCompra) WHERE IdCompra IS NOT NULL;
DECLARE @p TABLE(Nombre nvarchar(100),Descripcion nvarchar(250));
INSERT @p VALUES
 (N'IA_VER',N'Acceder a Inteligencia Artificial y al resumen general.'),
 (N'IA_PRONOSTICOS_VER',N'Consultar estimaciones de ventas por producto.'),
 (N'IA_PROYECCIONES_VER',N'Consultar proyecciones comerciales y rangos estimados.'),
 (N'IA_ALERTAS_VER',N'Consultar riesgos de inventario y avisos internos.'),
 (N'IA_RECOMENDACIONES_REVISAR',N'Revisar, aceptar o descartar recomendaciones con justificación.'),
 (N'IA_COMPRAS_GESTIONAR',N'Consultar solicitudes de abastecimiento y su seguimiento.'),
 (N'IA_BORRADORES_CREAR',N'Preparar solicitudes para el formulario de Compras; exige COMPRAS_CREAR.'),
 (N'IA_CONFIGURAR_ABASTECIMIENTO',N'Verificar proveedor, plazo, cobertura, reservas y entregas de abastecimiento.');
INSERT dbo.Permiso(Nombre,Descripcion,Estado)
 SELECT p.Nombre,p.Descripcion,1 FROM @p p WHERE NOT EXISTS(SELECT 1 FROM dbo.Permiso x WHERE x.Nombre=p.Nombre);
-- Admin ya proyecta todo el catálogo; también se materializan las nuevas asignaciones.
INSERT dbo.RolPermiso(IdRol,IdPermiso)
 SELECT r.IdRol,p.IdPermiso FROM dbo.Rol r JOIN dbo.Permiso p ON p.Nombre IN(SELECT Nombre FROM @p)
 WHERE UPPER(LTRIM(RTRIM(r.Nombre))) IN('ADMIN','ADMINISTRADOR') AND r.Estado=1
 AND NOT EXISTS(SELECT 1 FROM dbo.RolPermiso rp WHERE rp.IdRol=r.IdRol AND rp.IdPermiso=p.IdPermiso);
COMMIT;

GO
CREATE OR ALTER TRIGGER ia.trg_Abastecimiento_Auditoria ON ia.[Abastecimiento] AFTER INSERT,UPDATE,DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.AuditoriaLog(IdUsuario,Accion,TablaAfectada,IdRegistro,Fecha,Descripcion,DatosAntes,DatosDespues,Solicitud,Origen)
 SELECT TRY_CONVERT(int,SESSION_CONTEXT(N'IdUsuarioAuditoria')),
 CASE WHEN d.[IdProducto] IS NULL THEN N'CREAR' WHEN i.[IdProducto] IS NULL THEN N'ELIMINAR' ELSE N'ACTUALIZAR' END,N'IA_Abastecimiento',
 COALESCE(i.[IdProducto],d.[IdProducto]),GETDATE(),COALESCE(CONVERT(nvarchar(500),SESSION_CONTEXT(N'AccionAuditoria')),N'Proceso de IA'),
 CASE WHEN d.[IdProducto] IS NOT NULL THEN (SELECT d.[IdProducto],d.[IdProveedor],d.[PlazoDias],d.[CoberturaDias],d.[Seguridad],d.[Minimo],d.[Multiplo],d.[Reservado],d.[EntradaConfirmada],d.[FechaEntrada],d.[Evidencia],d.[VerificadaHasta],d.[IdUsuario] FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES) END,
 CASE WHEN i.[IdProducto] IS NOT NULL THEN (SELECT i.[IdProducto],i.[IdProveedor],i.[PlazoDias],i.[CoberturaDias],i.[Seguridad],i.[Minimo],i.[Multiplo],i.[Reservado],i.[EntradaConfirmada],i.[FechaEntrada],i.[Evidencia],i.[VerificadaHasta],i.[IdUsuario] FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES) END,
 CONVERT(nvarchar(100),SESSION_CONTEXT(N'SolicitudAuditoria')),COALESCE(CONVERT(nvarchar(128),SESSION_CONTEXT(N'OrigenAuditoria')),N'Proceso IA')
 FROM inserted i FULL OUTER JOIN deleted d ON i.[IdProducto]=d.[IdProducto];
END;

GO
CREATE OR ALTER TRIGGER ia.trg_Recomendacion_Auditoria ON ia.[Recomendacion] AFTER INSERT,UPDATE,DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.AuditoriaLog(IdUsuario,Accion,TablaAfectada,IdRegistro,Fecha,Descripcion,DatosAntes,DatosDespues,Solicitud,Origen)
 SELECT TRY_CONVERT(int,SESSION_CONTEXT(N'IdUsuarioAuditoria')),
 CASE WHEN d.[Id] IS NULL THEN N'CREAR' WHEN i.[Id] IS NULL THEN N'ELIMINAR' ELSE N'ACTUALIZAR' END,N'IA_Recomendacion',
 TRY_CONVERT(int,COALESCE(i.[Id],d.[Id])),GETDATE(),COALESCE(CONVERT(nvarchar(500),SESSION_CONTEXT(N'AccionAuditoria')),N'Proceso de IA'),
 CASE WHEN d.[Id] IS NOT NULL THEN (SELECT d.[IdProducto],d.[Prioridad],d.[Cantidad],d.[FechaPedido],d.[Estado],d.[Motivo],d.[IdUsuario],d.[Procedencia] FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES) END,
 CASE WHEN i.[Id] IS NOT NULL THEN (SELECT i.[IdProducto],i.[Prioridad],i.[Cantidad],i.[FechaPedido],i.[Estado],i.[Motivo],i.[IdUsuario],i.[Procedencia] FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES) END,
 CONVERT(nvarchar(100),SESSION_CONTEXT(N'SolicitudAuditoria')),COALESCE(CONVERT(nvarchar(128),SESSION_CONTEXT(N'OrigenAuditoria')),N'Proceso IA')
 FROM inserted i FULL OUTER JOIN deleted d ON i.[Id]=d.[Id];
END;

GO
CREATE OR ALTER TRIGGER ia.trg_SolicitudCompra_Auditoria ON ia.[SolicitudCompra] AFTER INSERT,UPDATE,DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.AuditoriaLog(IdUsuario,Accion,TablaAfectada,IdRegistro,Fecha,Descripcion,DatosAntes,DatosDespues,Solicitud,Origen)
 SELECT TRY_CONVERT(int,SESSION_CONTEXT(N'IdUsuarioAuditoria')),
 CASE WHEN d.[Id] IS NULL THEN N'CREAR' WHEN i.[Id] IS NULL THEN N'ELIMINAR' ELSE N'ACTUALIZAR' END,N'IA_SolicitudCompra',
 TRY_CONVERT(int,COALESCE(i.[Id],d.[Id])),GETDATE(),COALESCE(CONVERT(nvarchar(500),SESSION_CONTEXT(N'AccionAuditoria')),N'Proceso de IA'),
 CASE WHEN d.[Id] IS NOT NULL THEN (SELECT d.[IdRecomendacion],d.[IdProducto],d.[IdProveedor],d.[Cantidad],d.[PrecioConIgv],d.[Justificacion],d.[IdUsuario],d.[IdCompra] FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES) END,
 CASE WHEN i.[Id] IS NOT NULL THEN (SELECT i.[IdRecomendacion],i.[IdProducto],i.[IdProveedor],i.[Cantidad],i.[PrecioConIgv],i.[Justificacion],i.[IdUsuario],i.[IdCompra] FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES) END,
 CONVERT(nvarchar(100),SESSION_CONTEXT(N'SolicitudAuditoria')),COALESCE(CONVERT(nvarchar(128),SESSION_CONTEXT(N'OrigenAuditoria')),N'Proceso IA')
 FROM inserted i FULL OUTER JOIN deleted d ON i.[Id]=d.[Id];
END;
