using Microsoft.Data.SqlClient;
using Sistema3S.Web.Services.Seguridad;
using System.Net.Mail;
// Aprovisiona únicamente el primer administrador de una instalación comercialmente vacía.
// No lee appsettings del desarrollador ni contiene contraseñas predeterminadas.
var connection=Environment.GetEnvironmentVariable("S3S_INSTALACION_CONEXION");
var correo=Environment.GetEnvironmentVariable("S3S_ADMIN_CORREO");
var password=Environment.GetEnvironmentVariable("S3S_ADMIN_CONTRASENA");
if(string.IsNullOrWhiteSpace(connection)||string.IsNullOrWhiteSpace(correo)||string.IsNullOrWhiteSpace(password))throw new InvalidOperationException("Configura S3S_INSTALACION_CONEXION, S3S_ADMIN_CORREO y S3S_ADMIN_CONTRASENA sólo para este proceso.");
if(!MailAddress.TryCreate(correo,out _)||password.Length<12||!password.Any(char.IsUpper)||!password.Any(char.IsLower)||!password.Any(char.IsDigit)||!password.Any(c=>!char.IsLetterOrDigit(c)))throw new InvalidOperationException("Usa correo válido y contraseña de al menos 12 caracteres con mayúscula, minúscula, número y símbolo.");
await using var conn=new SqlConnection(connection);await conn.OpenAsync();await using var transaction=(SqlTransaction)await conn.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
await using var cmd=new SqlCommand("""
 IF EXISTS(SELECT 1 FROM dbo.Usuario) OR EXISTS(SELECT 1 FROM dbo.Venta) OR EXISTS(SELECT 1 FROM dbo.Compra) OR EXISTS(SELECT 1 FROM dbo.Cotizacion) OR EXISTS(SELECT 1 FROM bi.OperacionReferencia)
   THROW 79003,N'El aprovisionamiento requiere una instalación nueva sin usuarios ni operaciones.',1;
 DECLARE @rol int=(SELECT TOP(1) IdRol FROM dbo.Rol WHERE Nombre=N'Administrador' AND Estado=1);
 IF @rol IS NULL THROW 79004,N'Inicializa primero los catálogos y la migración BI.',1;
 INSERT dbo.Usuario(IdRol,Correo,ContrasenaHash,Estado) VALUES(@rol,@correo,@hash,1);
 """,conn,transaction);
cmd.Parameters.AddWithValue("@correo",correo.Trim());cmd.Parameters.AddWithValue("@hash",new PasswordHashService().CrearHash(password));await cmd.ExecuteNonQueryAsync();await transaction.CommitAsync();
Console.WriteLine("Primer administrador creado. No se registraron datos comerciales. Retira las variables temporales de credenciales.");
