-- Sólo metadatos IA. No altera ni elimina operaciones comerciales o permisos.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @gate int;EXEC @gate=sys.sp_getapplock @Resource='Sistema3S.IA.esquema',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
IF @gate<0 THROW 51000,'IA_MIGRACION_OCUPADA',1;
IF SCHEMA_ID('ia') IS NULL EXEC('CREATE SCHEMA ia AUTHORIZATION dbo');
IF OBJECT_ID('ia.OrigenOperacion','U') IS NULL CREATE TABLE ia.OrigenOperacion(
 Tipo varchar(15) NOT NULL,IdOperacion int NOT NULL,Origen varchar(30) NOT NULL,
 Evidencia nvarchar(500) NOT NULL,Identificada datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
 PRIMARY KEY(Tipo,IdOperacion),CHECK(Tipo IN('Venta','Compra','Cotizacion')),CHECK(Origen IN('DesarrolloAsistido','PruebaQA')));
IF OBJECT_ID('ia.Modelo','U') IS NULL CREATE TABLE ia.Modelo(
 Id uniqueidentifier NOT NULL PRIMARY KEY,Version varchar(30) NOT NULL,Alcance varchar(20) NOT NULL,
 Corte date NOT NULL,Huella char(64) NOT NULL,Artefacto nvarchar(100) NOT NULL,
 Evaluacion nvarchar(max) NOT NULL CHECK(ISJSON(Evaluacion)=1),Elegible bit NOT NULL,Activo bit NOT NULL DEFAULT 0,
 Creado datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),Publicado datetime2 NULL,
 CHECK(Activo=0 OR (Elegible=1 AND Alcance='production')));
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('ia.Modelo') AND name='UQ_IA_ModeloActivo')
 CREATE UNIQUE INDEX UQ_IA_ModeloActivo ON ia.Modelo(Activo) WHERE Activo=1;
IF OBJECT_ID('ia.Programacion','U') IS NULL CREATE TABLE ia.Programacion(
 Tipo varchar(20) PRIMARY KEY CHECK(Tipo IN('Inferencia','Entrenamiento')),Estado varchar(30) NOT NULL DEFAULT 'Pendiente',
 ProximaUTC datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),UltimoCorte date NULL,UltimaHuella char(64) NULL,
 IniciadaUTC datetime2 NULL,FinalizadaUTC datetime2 NULL,Error varchar(80) NULL,Intentos int NOT NULL DEFAULT 0);
IF OBJECT_ID('ia.Trabajo','U') IS NULL CREATE TABLE ia.Trabajo(
 Id bigint IDENTITY PRIMARY KEY,Tipo varchar(20) NOT NULL,Corte date NOT NULL,Estado varchar(30) NOT NULL,
 Iniciada datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),Finalizada datetime2 NULL,Modelo uniqueidentifier NULL REFERENCES ia.Modelo(Id),
 Detalle nvarchar(max) NULL CHECK(Detalle IS NULL OR ISJSON(Detalle)=1),Error varchar(80) NULL);
IF OBJECT_ID('ia.Pronostico7','U') IS NULL CREATE TABLE ia.Pronostico7(
 Id bigint IDENTITY PRIMARY KEY,IdEjecucion bigint NOT NULL REFERENCES ia.Ejecucion(Id),IdProducto int NOT NULL REFERENCES dbo.Producto(IdProducto),
 Desde date NOT NULL,Hasta date NOT NULL,Cantidad decimal(18,6) NOT NULL CHECK(Cantidad>=0),Real decimal(18,6) NULL,Evaluada datetime2 NULL,
 UNIQUE(IdEjecucion,IdProducto));
IF NOT EXISTS(SELECT 1 FROM ia.Programacion WHERE Tipo='Inferencia') INSERT ia.Programacion(Tipo) VALUES('Inferencia');
IF NOT EXISTS(SELECT 1 FROM ia.Programacion WHERE Tipo='Entrenamiento') INSERT ia.Programacion(Tipo) VALUES('Entrenamiento');
COMMIT;
