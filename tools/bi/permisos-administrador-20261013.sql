SET NOCOUNT ON; SET XACT_ABORT ON; BEGIN TRAN;
IF CAST(SERVERPROPERTY('ServerName') AS nvarchar(128))<>N'Alexis' OR (DB_NAME()<>N'BD_3S' AND DB_NAME() NOT LIKE N'Sistema3S[_]QA[_]BI90[_]%') THROW 51000,N'Esta corrección SQL es exclusiva de la base de desarrollo o su copia QA.',1;
EXEC sys.sp_set_session_context @key=N'AccionAuditoria', @value=N'Completar permisos Administrador: solicitud autorizada 13/10/2026';
EXEC sys.sp_set_session_context @key=N'OrigenAuditoria', @value=N'Sistema';
EXEC sys.sp_set_session_context @key=N'SolicitudAuditoria', @value=N'BI90-ADMIN-20261013';
INSERT dbo.RolPermiso(IdRol,IdPermiso)
SELECT r.IdRol,p.IdPermiso FROM dbo.Rol r CROSS JOIN dbo.Permiso p
WHERE r.Estado=1 AND UPPER(LTRIM(RTRIM(r.Nombre))) IN('ADMINISTRADOR','ADMIN') AND p.Estado=1
AND NOT EXISTS(SELECT 1 FROM dbo.RolPermiso rp WHERE rp.IdRol=r.IdRol AND rp.IdPermiso=p.IdPermiso);
IF EXISTS(SELECT 1 FROM dbo.Rol r CROSS JOIN dbo.Permiso p
 WHERE r.Estado=1 AND UPPER(LTRIM(RTRIM(r.Nombre))) IN('ADMINISTRADOR','ADMIN') AND p.Estado=1
 AND NOT EXISTS(SELECT 1 FROM dbo.RolPermiso rp WHERE rp.IdRol=r.IdRol AND rp.IdPermiso=p.IdPermiso))
 THROW 51000,N'Administrador no tiene el catálogo completo.',1;
COMMIT;
