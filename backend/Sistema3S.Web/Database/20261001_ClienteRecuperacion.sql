-- Ejecutar una vez sobre la base de datos de Sistema3S, antes de iniciar esta versión.
-- Idempotente: no modifica usuarios, clientes, cotizaciones ni datos de auditoría.
IF OBJECT_ID(N'dbo.ClienteRecuperacion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ClienteRecuperacion (
        TokenHash varchar(64) NOT NULL CONSTRAINT PK_ClienteRecuperacion PRIMARY KEY,
        IdUsuario int NOT NULL CONSTRAINT FK_ClienteRecuperacion_Usuario REFERENCES dbo.Usuario(IdUsuario),
        VersionClave varchar(64) NOT NULL,
        CreadoUtc datetime2 NOT NULL,
        ExpiraUtc datetime2 NOT NULL
    );
    CREATE INDEX IX_ClienteRecuperacion_Usuario ON dbo.ClienteRecuperacion(IdUsuario, CreadoUtc);
END;
