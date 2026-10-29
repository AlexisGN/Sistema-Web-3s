-- Migración aditiva. Sólo metadatos IA; no modifica tablas comerciales.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @g int;EXEC @g=sys.sp_getapplock @Resource='Sistema3S.IA.esquema',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
IF @g<0 THROW 51000,'IA_MIGRACION_OCUPADA',1;
-- Ampliar únicamente el catálogo de estados IA conservando la restricción.
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('ia.Ejecucion') AND definition LIKE '%Sombra%')
BEGIN
 DECLARE @check sysname=(SELECT TOP(1) name FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('ia.Ejecucion') AND definition LIKE '%Estado%');
 IF @check IS NOT NULL BEGIN DECLARE @drop nvarchar(400)=N'ALTER TABLE ia.Ejecucion DROP CONSTRAINT '+QUOTENAME(@check);EXEC(@drop);END;
 ALTER TABLE ia.Ejecucion WITH CHECK ADD CONSTRAINT CK_IA_EjecucionEstadoV7 CHECK(Estado IN('EnCurso','Completada','Fallida','Sombra'));
END;
IF COL_LENGTH('ia.Modelo','EstadoModelo') IS NULL EXEC('ALTER TABLE ia.Modelo ADD EstadoModelo varchar(30) NULL');
IF COL_LENGTH('ia.Programacion','HuellaEntrenada') IS NULL EXEC('ALTER TABLE ia.Programacion ADD HuellaEntrenada char(64) NULL,HuellaCodigo char(64) NULL,OperacionesEntrenadas int NULL,UltimoEntrenamientoUTC datetime2 NULL');
IF COL_LENGTH('ia.Programacion','HuellaRegimen') IS NULL EXEC('ALTER TABLE ia.Programacion ADD HuellaRegimen char(64) NULL');
IF OBJECT_ID('ia.PrediccionMemoria','U') IS NULL CREATE TABLE ia.PrediccionMemoria(
 Id bigint IDENTITY PRIMARY KEY,IdEjecucion bigint NOT NULL REFERENCES ia.Ejecucion(Id),Modelo uniqueidentifier NOT NULL REFERENCES ia.Modelo(Id),
 Version varchar(30) NOT NULL,IdProducto int NOT NULL REFERENCES dbo.Producto(IdProducto),Unidad nvarchar(30) NOT NULL,
 GeneradaUTC datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),EsSombra bit NOT NULL DEFAULT 0,DiaEmision date NOT NULL,Corte date NOT NULL,Horizonte int NOT NULL CHECK(Horizonte IN(7,30,60,90)),
 Desde date NOT NULL,Hasta date NOT NULL,Cantidad decimal(18,6) NOT NULL CHECK(Cantidad>=0),
 UNIQUE(IdEjecucion,IdProducto,Horizonte),CHECK(Hasta>=Desde));
IF OBJECT_ID('ia.EvaluacionMemoria','U') IS NULL CREATE TABLE ia.EvaluacionMemoria(
 IdPrediccion bigint PRIMARY KEY REFERENCES ia.PrediccionMemoria(Id),EvaluadaUTC datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
 Real decimal(18,6) NOT NULL CHECK(Real>=0),ErrorAbsoluto decimal(18,6) NOT NULL,Sesgo decimal(18,6) NOT NULL,
 Wape decimal(28,10) NULL,Estado varchar(30) NOT NULL CHECK(Estado IN('Evaluada','SinDenominador')));
EXEC('CREATE OR ALTER TRIGGER ia.TR_PrediccionMemoria_Inmutable ON ia.PrediccionMemoria INSTEAD OF UPDATE,DELETE AS THROW 51000,''IA_PREDICCION_INMUTABLE'',1;');
EXEC('CREATE OR ALTER TRIGGER ia.TR_EvaluacionMemoria_Inmutable ON ia.EvaluacionMemoria INSTEAD OF UPDATE,DELETE AS THROW 51000,''IA_EVALUACION_INMUTABLE'',1;');
COMMIT;
