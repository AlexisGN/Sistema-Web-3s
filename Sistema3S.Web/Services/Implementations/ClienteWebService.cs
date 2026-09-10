using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Sistema3S.Web.Data;
using Sistema3S.Web.DTOs.ClienteWeb;
using Sistema3S.Web.DTOs.Comun;
using Sistema3S.Web.Services.Interfaces;
using Sistema3S.Web.Services.Seguridad;

namespace Sistema3S.Web.Services.Implementations
{
    public class ClienteWebService : IClienteWebService
    {
        private readonly Bd3sContext _context;
        private readonly PasswordHashService _passwordHashService;
        private readonly IConfiguration _configuration;
        private readonly IConsultaDocumentoService _consultaDocumentoService;

        public ClienteWebService(
            Bd3sContext context,
            PasswordHashService passwordHashService,
            IConfiguration configuration,
            IConsultaDocumentoService consultaDocumentoService
        )
        {
            _context = context;
            _passwordHashService = passwordHashService;
            _configuration = configuration;
            _consultaDocumentoService = consultaDocumentoService;
        }

        public async Task<ClienteWebConsultaDocumentoDto> ConsultarDocumentoAsync(
            string tipoDocumento,
            string numeroDocumento
        )
        {
            var tipo = NormalizarTipoDocumento(tipoDocumento);
            var numero = NormalizarNumero(numeroDocumento);

            if (tipo != "DNI" && tipo != "RUC")
            {
                return new ClienteWebConsultaDocumentoDto
                {
                    TipoDocumento = tipo,
                    NumeroDocumento = numero,
                    Exitoso = false,
                    Mensaje = "Selecciona DNI o RUC para consultar."
                };
            }

            if (tipo == "DNI" && numero.Length != 8)
            {
                return new ClienteWebConsultaDocumentoDto
                {
                    TipoDocumento = tipo,
                    NumeroDocumento = numero,
                    Exitoso = false,
                    Mensaje = "El DNI debe tener 8 dígitos."
                };
            }

            if (tipo == "RUC" && numero.Length != 11)
            {
                return new ClienteWebConsultaDocumentoDto
                {
                    TipoDocumento = tipo,
                    NumeroDocumento = numero,
                    Exitoso = false,
                    Mensaje = "El RUC debe tener 11 dígitos."
                };
            }

            if (tipo == "DNI")
            {
                var resultado = await _consultaDocumentoService.ConsultarDniAsync(numero);

                var respuesta = new ClienteWebConsultaDocumentoDto
                {
                    TipoDocumento = "DNI",
                    NumeroDocumento = resultado.NumeroDocumento,
                    Exitoso = resultado.Exitoso,
                    ClienteYaExiste = resultado.ClienteYaExiste,
                    IdClienteExistente = resultado.IdClienteExistente,

                    Nombres = resultado.Nombres,
                    ApellidoPaterno = resultado.ApellidoPaterno,
                    ApellidoMaterno = resultado.ApellidoMaterno,
                    NombreCompleto = resultado.NombreCompleto,

                    Mensaje = resultado.Mensaje
                };

                await CompletarDatosClienteExistenteAsync(respuesta);

                return respuesta;
            }

            var resultadoRuc = await _consultaDocumentoService.ConsultarRucAsync(numero);

            var respuestaRuc = new ClienteWebConsultaDocumentoDto
            {
                TipoDocumento = "RUC",
                NumeroDocumento = resultadoRuc.NumeroDocumento,
                Exitoso = resultadoRuc.Exitoso,
                ClienteYaExiste = resultadoRuc.ClienteYaExiste,
                IdClienteExistente = resultadoRuc.IdClienteExistente,

                RazonSocial = resultadoRuc.RazonSocial,
                NombreComercial = resultadoRuc.NombreComercial,
                EstadoSunat = resultadoRuc.EstadoSunat,
                CondicionSunat = resultadoRuc.CondicionSunat,

                DireccionExistente = resultadoRuc.Direccion,
                CodigoUbigeo = resultadoRuc.CodigoUbigeo,
                IdUbigeo = resultadoRuc.IdUbigeo,
                Ubicacion = resultadoRuc.Ubicacion,

                Mensaje = resultadoRuc.Mensaje
            };

            await CompletarDatosClienteExistenteAsync(respuestaRuc);

            return respuestaRuc;
        }

        public async Task<ClienteWebSesionDto> RegistrarAsync(ClienteWebRegistroDto dto)
        {
            ValidarRegistro(dto);

            var tipoDocumento = NormalizarTipoDocumento(dto.TipoDocumento);
            var numeroDocumento = NormalizarNumero(dto.NumeroDocumento);
            var correo = NormalizarCorreo(dto.Correo);
            var telefono = NormalizarTexto(dto.Telefono);
            var direccion = NormalizarTexto(dto.Direccion);

            var nombres = NormalizarTexto(dto.Nombres);
            var apellidoPaterno = NormalizarTexto(dto.ApellidoPaterno);
            var apellidoMaterno = NormalizarTexto(dto.ApellidoMaterno);

            var razonSocial = NormalizarTexto(dto.RazonSocial);
            var nombreComercial = NormalizarTexto(dto.NombreComercial);

            var contrasenaHash = _passwordHashService.CrearHash(dto.Contrasena);

            await using var connection = CrearConexionClienteWeb();
            await connection.AbrirAuditadaAsync();

            await using var command = new SqlCommand("dbo.sp_RegistrarClienteWeb", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@TipoDocumento", tipoDocumento);
            command.Parameters.AddWithValue("@NumeroDocumento", numeroDocumento);
            command.Parameters.AddWithValue("@Correo", correo);
            command.Parameters.AddWithValue("@Telefono", ValorDb(telefono));
            command.Parameters.AddWithValue("@Direccion", ValorDb(direccion));
            command.Parameters.AddWithValue("@ContrasenaHash", contrasenaHash);

            command.Parameters.AddWithValue("@Nombres", ValorDb(nombres));
            command.Parameters.AddWithValue("@ApellidoPaterno", ValorDb(apellidoPaterno));
            command.Parameters.AddWithValue("@ApellidoMaterno", ValorDb(apellidoMaterno));

            command.Parameters.AddWithValue("@RazonSocial", ValorDb(razonSocial));
            command.Parameters.AddWithValue("@NombreComercial", ValorDb(nombreComercial));

            await using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                throw new InvalidOperationException("No se pudo registrar la cuenta del cliente.");
            }

            var sesion = LeerSesionDesdeReader(reader);
            CompletarToken(sesion, contrasenaHash);

            return sesion;
        }

        public async Task<ClienteWebSesionDto> LoginAsync(ClienteWebLoginDto dto)
        {
            var correo = NormalizarCorreo(dto.Correo);
            var contrasena = dto.Contrasena ?? string.Empty;

            if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(contrasena))
            {
                throw new InvalidOperationException("Ingresa correo y contraseña.");
            }

            var usuario = await _context.Usuario
                .AsNoTracking()
                .Include(u => u.IdRolNavigation)
                .Include(u => u.Cliente)
                    .ThenInclude(c => c!.IdTipoDocumentoNavigation)
                .Include(u => u.Cliente)
                    .ThenInclude(c => c!.IdTipoClienteNavigation)
                .Include(u => u.Cliente)
                    .ThenInclude(c => c!.ClientePersonaNatural)
                .Include(u => u.Cliente)
                    .ThenInclude(c => c!.ClienteEmpresa)
                .FirstOrDefaultAsync(u =>
                    u.Correo.ToLower() == correo &&
                    u.Estado &&
                    u.IdRolNavigation.Estado &&
                    u.IdRolNavigation.Nombre.ToUpper() == "CLIENTE"
                );

            if (usuario == null || usuario.Cliente == null)
            {
                throw new InvalidOperationException("Credenciales incorrectas.");
            }

            if (!usuario.Cliente.Estado)
            {
                throw new InvalidOperationException("La cuenta del cliente se encuentra inactiva.");
            }

            var passwordOk = _passwordHashService.Verificar(contrasena, usuario.ContrasenaHash);

            if (!passwordOk)
            {
                throw new InvalidOperationException("Credenciales incorrectas.");
            }

            var cliente = usuario.Cliente;
            var esEmpresa = cliente.IdTipoDocumentoNavigation.Nombre.ToUpper() == "RUC";

            var nombreCliente = esEmpresa
                ? cliente.ClienteEmpresa?.RazonSocial ?? "Cliente empresa"
                : ConstruirNombrePersona(
                    cliente.ClientePersonaNatural?.Nombres,
                    cliente.ClientePersonaNatural?.ApellidoPaterno,
                    cliente.ClientePersonaNatural?.ApellidoMaterno
                );

            var sesion = new ClienteWebSesionDto
            {
                IdCliente = cliente.IdCliente,
                IdUsuario = usuario.IdUsuario,
                IdRol = usuario.IdRol,
                Correo = usuario.Correo,
                Rol = usuario.IdRolNavigation.Nombre,
                TipoDocumento = cliente.IdTipoDocumentoNavigation.Nombre,
                NumeroDocumento = cliente.NumeroDocumento,
                TipoCliente = cliente.IdTipoClienteNavigation.Nombre,
                NombreCliente = nombreCliente,
                EsEmpresa = esEmpresa,
                Mensaje = "Inicio de sesión correcto."
            };

            CompletarToken(sesion, usuario.ContrasenaHash);

            return sesion;
        }

        private async Task CompletarDatosClienteExistenteAsync(ClienteWebConsultaDocumentoDto respuesta)
        {
            if (respuesta.IdClienteExistente == null)
            {
                return;
            }

            var cliente = await _context.Cliente
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.IdCliente == respuesta.IdClienteExistente.Value);

            if (cliente == null)
            {
                return;
            }

            respuesta.ClienteYaExiste = true;
            respuesta.CuentaWebVinculada = cliente.IdUsuario != null;
            respuesta.CorreoExistente = cliente.Correo;
            respuesta.TelefonoExistente = cliente.Telefono;

            if (string.IsNullOrWhiteSpace(respuesta.DireccionExistente))
            {
                respuesta.DireccionExistente = cliente.Direccion;
            }

            if (respuesta.CuentaWebVinculada)
            {
                respuesta.Mensaje = "Este documento ya tiene una cuenta web registrada. Inicia sesión para continuar.";
                return;
            }

            respuesta.Mensaje = "Datos encontrados correctamente. Revisa la información antes de crear tu cuenta.";
        }

        private void ValidarRegistro(ClienteWebRegistroDto dto)
        {
            var tipoDocumento = NormalizarTipoDocumento(dto.TipoDocumento);
            var numeroDocumento = NormalizarNumero(dto.NumeroDocumento);
            var correo = NormalizarCorreo(dto.Correo);
            var telefono = NormalizarTexto(dto.Telefono);

            if (tipoDocumento != "DNI" && tipoDocumento != "RUC")
            {
                throw new InvalidOperationException("Selecciona DNI o RUC.");
            }

            if (tipoDocumento == "DNI" && numeroDocumento.Length != 8)
            {
                throw new InvalidOperationException("El DNI debe tener 8 dígitos.");
            }

            if (tipoDocumento == "RUC" && numeroDocumento.Length != 11)
            {
                throw new InvalidOperationException("El RUC debe tener 11 dígitos.");
            }

            if (numeroDocumento.Any(c => !char.IsDigit(c)))
            {
                throw new InvalidOperationException("El número de documento solo debe contener dígitos.");
            }

            if (string.IsNullOrWhiteSpace(correo) || !correo.Contains("@") || !correo.Contains("."))
            {
                throw new InvalidOperationException("Ingresa un correo válido.");
            }

            if (string.IsNullOrWhiteSpace(telefono))
            {
                throw new InvalidOperationException("Ingresa un número de contacto.");
            }

            if (string.IsNullOrWhiteSpace(dto.Contrasena) || dto.Contrasena.Length < 8)
            {
                throw new InvalidOperationException("La contraseña debe tener como mínimo 8 caracteres.");
            }

            if (dto.Contrasena != dto.ConfirmarContrasena)
            {
                throw new InvalidOperationException("Las contraseñas no coinciden.");
            }

            if (tipoDocumento == "DNI")
            {
                if (string.IsNullOrWhiteSpace(dto.Nombres) || string.IsNullOrWhiteSpace(dto.ApellidoPaterno))
                {
                    throw new InvalidOperationException("Ingresa nombres y apellido paterno.");
                }
            }

            if (tipoDocumento == "RUC")
            {
                if (string.IsNullOrWhiteSpace(dto.RazonSocial))
                {
                    throw new InvalidOperationException("Ingresa la razón social de la empresa.");
                }
            }
        }

        private ClienteWebSesionDto LeerSesionDesdeReader(SqlDataReader reader)
        {
            return new ClienteWebSesionDto
            {
                IdCliente = Convert.ToInt32(reader["IdCliente"]),
                IdUsuario = Convert.ToInt32(reader["IdUsuario"]),
                IdRol = Convert.ToInt32(reader["IdRol"]),
                Correo = Convert.ToString(reader["Correo"]) ?? string.Empty,
                Rol = Convert.ToString(reader["Rol"]) ?? "Cliente",
                TipoDocumento = Convert.ToString(reader["TipoDocumento"]) ?? string.Empty,
                NumeroDocumento = Convert.ToString(reader["NumeroDocumento"]) ?? string.Empty,
                TipoCliente = Convert.ToString(reader["TipoCliente"]) ?? string.Empty,
                NombreCliente = Convert.ToString(reader["NombreCliente"]) ?? string.Empty,
                EsEmpresa = Convert.ToBoolean(reader["EsEmpresa"]),
                Mensaje = Convert.ToString(reader["Mensaje"]) ?? string.Empty
            };
        }

        private void CompletarToken(ClienteWebSesionDto sesion, string contrasenaHash)
        {
            var jwtKey = _configuration["Jwt:Key"];

            if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
            {
                throw new InvalidOperationException("La clave JWT no está configurada correctamente.");
            }

            var issuer = _configuration["Jwt:Issuer"] ?? "Sistema3S";
            var audience = _configuration["Jwt:Audience"] ?? "Sistema3SAdmin";

            var expireMinutesText = _configuration["Jwt:ExpireMinutes"];
            var expireMinutes = int.TryParse(expireMinutesText, out var minutes)
                ? minutes
                : 480;

            var expira = DateTime.UtcNow.AddMinutes(expireMinutes);

            var claims = new List<Claim>
            {
                new("idUsuario", sesion.IdUsuario.ToString()),
                new("idCliente", sesion.IdCliente.ToString()),
                new("clienteVersion", ClienteVersionSesion.Crear(contrasenaHash, _configuration)),
                new("correo", sesion.Correo),
                new("rol", sesion.Rol),
                new("tipoDocumento", sesion.TipoDocumento),
                new("numeroDocumento", sesion.NumeroDocumento),
                new("tipoCliente", sesion.TipoCliente),
                new("esEmpresa", sesion.EsEmpresa ? "true" : "false"),
                new(ClaimTypes.Role, sesion.Rol)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expira,
                signingCredentials: credentials
            );

            sesion.Token = new JwtSecurityTokenHandler().WriteToken(token);
            sesion.Expira = expira;
        }

        private SqlConnection CrearConexionClienteWeb()
        {
            var connectionString = _context.Database.GetConnectionString();

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("No se encontró la cadena de conexión.");
            }

            return new SqlConnection(connectionString);
        }

        private static string ConstruirNombrePersona(
            string? nombres,
            string? apellidoPaterno,
            string? apellidoMaterno
        )
        {
            var partes = new[]
            {
                nombres,
                apellidoPaterno,
                apellidoMaterno
            }
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p!.Trim());

            var nombreCompleto = string.Join(" ", partes);

            return string.IsNullOrWhiteSpace(nombreCompleto)
                ? "Cliente"
                : nombreCompleto;
        }

        private static string NormalizarTipoDocumento(string? valor)
        {
            return (valor ?? string.Empty).Trim().ToUpper();
        }

        private static string NormalizarCorreo(string? valor)
        {
            return (valor ?? string.Empty).Trim().ToLower();
        }

        private static string? NormalizarTexto(string? valor)
        {
            var texto = (valor ?? string.Empty).Trim();

            return string.IsNullOrWhiteSpace(texto)
                ? null
                : texto;
        }

        private static string NormalizarNumero(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return string.Empty;
            }

            return new string(valor.Where(char.IsDigit).ToArray());
        }

        private static object ValorDb(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor)
                ? DBNull.Value
                : valor.Trim();
        }

        private static object ValorDb(int? valor)
        {
            return valor.HasValue
                ? valor.Value
                : DBNull.Value;
        }
    }
}