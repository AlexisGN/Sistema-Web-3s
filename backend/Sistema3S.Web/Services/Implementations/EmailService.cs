using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using Sistema3S.Web.Services.Interfaces;

namespace Sistema3S.Web.Services.Implementations
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task EnviarConAdjuntoAsync(
            string destinatario,
            string asunto,
            string cuerpo,
            string? rutaArchivoAdjunto
        )
        {
            var host = _configuration["Smtp:Host"]?.Trim();
            var portText = _configuration["Smtp:Port"];
            var user = _configuration["Smtp:User"]?.Trim();
            var password = NormalizarPassword(host, _configuration["Smtp:Password"]);
            var from = _configuration["Smtp:From"]?.Trim();
            var fromName = _configuration["Smtp:FromName"]?.Trim();

            if (string.IsNullOrWhiteSpace(fromName))
            {
                fromName = "Empresa 3S";
            }

            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(portText) ||
                string.IsNullOrWhiteSpace(user) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(from))
            {
                throw new InvalidOperationException(
                    "La configuración SMTP no está completa. Revisa appsettings.json."
                );
            }

            if (!int.TryParse(portText, out var port))
            {
                throw new InvalidOperationException("El puerto SMTP no es válido.");
            }

            if (!string.IsNullOrWhiteSpace(rutaArchivoAdjunto))
            {
                if (!File.Exists(rutaArchivoAdjunto))
                {
                    throw new FileNotFoundException(
                        "No se encontró el PDF que debe adjuntarse a la cotización.",
                        rutaArchivoAdjunto
                    );
                }

                if (new FileInfo(rutaArchivoAdjunto).Length == 0)
                {
                    throw new InvalidOperationException(
                        "El PDF de la cotización está vacío y no se puede adjuntar."
                    );
                }
            }

            using var message = new MailMessage
            {
                From = new MailAddress(from, fromName),
                Subject = asunto,
                Body = cuerpo,
                IsBodyHtml = false,
                SubjectEncoding = Encoding.UTF8,
                BodyEncoding = Encoding.UTF8
            };

            message.To.Add(new MailAddress(destinatario.Trim()));

            if (!string.IsNullOrWhiteSpace(rutaArchivoAdjunto))
            {
                var adjunto = new Attachment(
                    rutaArchivoAdjunto,
                    MediaTypeNames.Application.Pdf
                )
                {
                    Name = Path.GetFileName(rutaArchivoAdjunto),
                    NameEncoding = Encoding.UTF8
                };

                message.Attachments.Add(adjunto);
            }

            var enableSsl = !bool.TryParse(
                _configuration["Smtp:EnableSsl"],
                out var sslConfigurado
            ) || sslConfigurado;

            using var client = new SmtpClient(host, port)
            {
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(user, password),
                EnableSsl = enableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 30000
            };

            try
            {
                using var limite = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                await client.SendMailAsync(message, limite.Token);
            }
            catch (OperationCanceledException ex)
            {
                throw new InvalidOperationException("El servidor de correo no respondió en 30 segundos. Inténtalo nuevamente.", ex);
            }
            catch (SmtpException ex)
            {
                throw new InvalidOperationException(CrearMensajeErrorSmtp(ex), ex);
            }
        }

        private static string? NormalizarPassword(string? host, string? password)
        {
            var passwordLimpio = password?.Trim();

            // Google muestra las contraseñas de aplicación separadas en grupos,
            // pero el servidor SMTP requiere los 16 caracteres continuos.
            if (!string.IsNullOrWhiteSpace(passwordLimpio) &&
                host?.Contains("gmail.com", StringComparison.OrdinalIgnoreCase) == true)
            {
                passwordLimpio = string.Concat(
                    passwordLimpio.Where(c => !char.IsWhiteSpace(c))
                );
            }

            return passwordLimpio;
        }

        private static string CrearMensajeErrorSmtp(SmtpException exception)
        {
            if (exception.StatusCode == SmtpStatusCode.MustIssueStartTlsFirst)
            {
                return "El servidor SMTP exige una conexión segura. Revisa Smtp:EnableSsl y el puerto configurado.";
            }

            var detalle = exception.Message;

            if (detalle.Contains("authentication", StringComparison.OrdinalIgnoreCase) ||
                detalle.Contains("authenticate", StringComparison.OrdinalIgnoreCase) ||
                detalle.Contains("5.7.8", StringComparison.OrdinalIgnoreCase) ||
                detalle.Contains("credentials", StringComparison.OrdinalIgnoreCase))
            {
                return "Gmail rechazó la autenticación SMTP. Verifica el correo y la contraseña de aplicación de Google.";
            }

            return $"El servidor SMTP rechazó el envío ({exception.StatusCode}). {detalle}";
        }
    }
}
