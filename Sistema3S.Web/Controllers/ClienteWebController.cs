using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Sistema3S.Web.DTOs.ClienteWeb;
using Sistema3S.Web.Services.Interfaces;

namespace Sistema3S.Web.Controllers
{
    [ApiController]
    [Route("api/cliente-web")]
    public class ClienteWebController : ControllerBase
    {
        private readonly IClienteWebService _clienteWebService;

        public ClienteWebController(IClienteWebService clienteWebService)
        {
            _clienteWebService = clienteWebService;
        }

        [AllowAnonymous]
        [HttpGet("consultar-documento")]
        public async Task<IActionResult> ConsultarDocumento(
            [FromQuery] string tipoDocumento,
            [FromQuery] string numeroDocumento
        )
        {
            try
            {
                var resultado = await _clienteWebService.ConsultarDocumentoAsync(
                    tipoDocumento,
                    numeroDocumento
                );

                return Ok(resultado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    mensaje = ex.Message
                });
            }
            catch (SqlException ex)
            {
                return BadRequest(new
                {
                    mensaje = ex.Message
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    mensaje = "No se pudo consultar el documento del cliente."
                });
            }
        }

        [AllowAnonymous]
        [HttpPost("registro")]
        public async Task<IActionResult> Registrar([FromBody] ClienteWebRegistroDto dto)
        {
            try
            {
                var resultado = await _clienteWebService.RegistrarAsync(dto);

                return Ok(resultado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    mensaje = ex.Message
                });
            }
            catch (SqlException ex)
            {
                return BadRequest(new
                {
                    mensaje = ex.Message
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    mensaje = "No se pudo registrar la cuenta del cliente."
                });
            }
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] ClienteWebLoginDto dto)
        {
            try
            {
                var resultado = await _clienteWebService.LoginAsync(dto);

                return Ok(resultado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    mensaje = ex.Message
                });
            }
            catch (SqlException ex)
            {
                return BadRequest(new
                {
                    mensaje = ex.Message
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    mensaje = "No se pudo iniciar sesión como cliente."
                });
            }
        }

        [Authorize]
        [HttpPost("cotizaciones")]
        public async Task<IActionResult> RegistrarCotizacion(
            [FromBody] ClienteWebCotizacionCrearDto dto
        )
        {
            try
            {
                var idCliente = ObtenerClaimEntero("idCliente");
                var idUsuario = ObtenerClaimEntero("idUsuario");

                if (idCliente <= 0 || idUsuario <= 0)
                {
                    return Unauthorized(new
                    {
                        mensaje = "Inicia sesión como cliente para enviar tu cotización."
                    });
                }

                var resultado = await _clienteWebService.RegistrarCotizacionAsync(
                    idCliente,
                    idUsuario,
                    dto
                );

                return Ok(resultado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    mensaje = ex.Message
                });
            }
            catch (SqlException ex)
            {
                return BadRequest(new
                {
                    mensaje = ex.Message
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    mensaje = "No se pudo enviar la cotización web."
                });
            }
        }

        [Authorize]
        [HttpGet("cotizaciones")]
        public async Task<IActionResult> ListarCotizaciones(
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanioPagina = 10
        )
        {
            try
            {
                var idCliente = ObtenerClaimEntero("idCliente");

                if (idCliente <= 0)
                {
                    return Unauthorized(new
                    {
                        mensaje = "Inicia sesión como cliente para ver tu historial."
                    });
                }

                var resultado = await _clienteWebService.ListarCotizacionesAsync(
                    idCliente,
                    pagina,
                    tamanioPagina
                );

                return Ok(resultado);
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    mensaje = "No se pudo cargar el historial de cotizaciones."
                });
            }
        }

        [Authorize]
        [HttpGet("cotizaciones/{idCotizacion:int}")]
        public async Task<IActionResult> ObtenerCotizacion(int idCotizacion)
        {
            try
            {
                var idCliente = ObtenerClaimEntero("idCliente");

                if (idCliente <= 0)
                {
                    return Unauthorized(new
                    {
                        mensaje = "Inicia sesión como cliente para ver el detalle."
                    });
                }

                var resultado = await _clienteWebService.ObtenerCotizacionAsync(
                    idCliente,
                    idCotizacion
                );

                if (resultado == null)
                {
                    return NotFound(new
                    {
                        mensaje = "Cotización no encontrada."
                    });
                }

                return Ok(resultado);
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    mensaje = "No se pudo cargar el detalle de la cotización."
                });
            }
        }

        private int ObtenerClaimEntero(string nombre)
        {
            var valor = User.FindFirst(nombre)?.Value;

            if (string.IsNullOrWhiteSpace(valor))
            {
                return 0;
            }

            return int.TryParse(valor, out var id)
                ? id
                : 0;
        }
    }
}