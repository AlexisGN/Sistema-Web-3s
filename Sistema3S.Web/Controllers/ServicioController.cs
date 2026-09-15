using Microsoft.AspNetCore.Mvc;
using Sistema3S.Web.DTOs.Servicio;
using Sistema3S.Web.Services.Interfaces;

namespace Sistema3S.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServicioController : ControllerBase
    {
        private readonly IServicioService _servicioService;
        private readonly ICatalogoArchivoService _archivoService;

        public ServicioController(
            IServicioService servicioService,
            ICatalogoArchivoService archivoService
        )
        {
            _servicioService = servicioService;
            _archivoService = archivoService;
        }

        [HttpGet]
        public async Task<IActionResult> Listar(
            [FromQuery] string? buscar,
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanioPagina = 5,
            [FromQuery] bool? estado = null
        )
        {
            try
            {
                var resultado = await _servicioService.ListarAsync(
                    buscar,
                    pagina,
                    tamanioPagina,
                    estado
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
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    mensaje = "No se pudieron cargar los servicios."
                });
            }
        }

        [HttpGet("{idServicio:int}")]
        public async Task<IActionResult> ObtenerPorId(int idServicio)
        {
            try
            {
                var servicio = await _servicioService.ObtenerPorIdAsync(idServicio);

                if (servicio == null)
                {
                    return NotFound(new
                    {
                        mensaje = "Servicio no encontrado."
                    });
                }

                return Ok(servicio);
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    mensaje = "No se pudo obtener el servicio."
                });
            }
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> Crear(
            [FromForm] ServicioCrearDto dto,
            CancellationToken cancellationToken
        )
        {
            string? imagenNueva = null;

            try
            {
                if (dto.ImagenArchivo == null)
                {
                    throw new InvalidOperationException("Selecciona una imagen para el servicio.");
                }

                imagenNueva = await _archivoService.GuardarImagenServicioAsync(
                    dto.ImagenArchivo,
                    cancellationToken
                );
                dto.ImagenUrl = imagenNueva;

                var servicio = await _servicioService.CrearAsync(dto);

                return CreatedAtAction(
                    nameof(ObtenerPorId),
                    new { idServicio = servicio.IdServicio },
                    servicio
                );
            }
            catch (InvalidOperationException ex)
            {
                await _archivoService.EliminarArchivoLocalAsync(imagenNueva);
                return BadRequest(new
                {
                    mensaje = ex.Message
                });
            }
            catch (Exception)
            {
                await _archivoService.EliminarArchivoLocalAsync(imagenNueva);
                return StatusCode(500, new
                {
                    mensaje = "No se pudo registrar el servicio."
                });
            }
        }

        [HttpPut("{idServicio:int}")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> Actualizar(
            int idServicio,
            [FromForm] ServicioActualizarDto dto,
            CancellationToken cancellationToken
        )
        {
            string? imagenNueva = null;

            try
            {
                var servicioActual = await _servicioService.ObtenerPorIdAsync(idServicio);

                if (servicioActual == null)
                {
                    return NotFound(new
                    {
                        mensaje = "Servicio no encontrado."
                    });
                }

                dto.ImagenUrl = servicioActual.ImagenUrl;

                if (dto.ImagenArchivo != null)
                {
                    imagenNueva = await _archivoService.GuardarImagenServicioAsync(
                        dto.ImagenArchivo,
                        cancellationToken
                    );
                    dto.ImagenUrl = imagenNueva;
                }

                var actualizado = await _servicioService.ActualizarAsync(idServicio, dto);

                if (!actualizado)
                {
                    await _archivoService.EliminarArchivoLocalAsync(imagenNueva);
                    return NotFound(new
                    {
                        mensaje = "Servicio no encontrado."
                    });
                }

                if (imagenNueva != null)
                {
                    await _archivoService.EliminarArchivoLocalAsync(servicioActual.ImagenUrl);
                }

                return Ok(new
                {
                    mensaje = "Servicio actualizado correctamente."
                });
            }
            catch (InvalidOperationException ex)
            {
                await _archivoService.EliminarArchivoLocalAsync(imagenNueva);
                return BadRequest(new
                {
                    mensaje = ex.Message
                });
            }
            catch (Exception)
            {
                await _archivoService.EliminarArchivoLocalAsync(imagenNueva);
                return StatusCode(500, new
                {
                    mensaje = "No se pudo actualizar el servicio."
                });
            }
        }




    }
}
