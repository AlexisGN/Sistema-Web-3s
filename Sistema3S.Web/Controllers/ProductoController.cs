using Microsoft.AspNetCore.Mvc;
using Sistema3S.Web.DTOs.Producto;
using Sistema3S.Web.Services.Interfaces;

namespace Sistema3S.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductoController : ControllerBase
    {
        private readonly IProductoService _productoService;
        private readonly ICatalogoArchivoService _archivoService;

        public ProductoController(
            IProductoService productoService,
            ICatalogoArchivoService archivoService
        )
        {
            _productoService = productoService;
            _archivoService = archivoService;
        }

        [HttpGet]
        public async Task<IActionResult> Listar(
            [FromQuery] string? buscar,
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanioPagina = 10,
            [FromQuery] bool? estado = null
        )
        {
            try
            {
                var resultado = await _productoService.ListarAsync(
                    buscar,
                    pagina,
                    tamanioPagina,
                    estado
                );

                return Ok(resultado);
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    mensaje = "No se pudieron cargar los productos."
                });
            }
        }

        [HttpGet("{idProducto:int}")]
        public async Task<IActionResult> ObtenerPorId(int idProducto)
        {
            try
            {
                var producto = await _productoService.ObtenerPorIdAsync(idProducto);

                if (producto == null)
                {
                    return NotFound(new
                    {
                        mensaje = "Producto no encontrado."
                    });
                }

                return Ok(producto);
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    mensaje = "No se pudo obtener el producto."
                });
            }
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(22 * 1024 * 1024)]
        public async Task<IActionResult> Crear(
            [FromForm] ProductoCrearDto dto,
            CancellationToken cancellationToken
        )
        {
            string? imagenNueva = null;
            string? fichaNueva = null;

            try
            {
                if (dto.ImagenArchivo == null)
                {
                    throw new InvalidOperationException("Selecciona una imagen para el producto.");
                }

                imagenNueva = await _archivoService.GuardarImagenProductoAsync(
                    dto.ImagenArchivo,
                    cancellationToken
                );
                dto.ImagenUrl = imagenNueva;

                if (dto.FichaTecnicaArchivo != null)
                {
                    fichaNueva = await _archivoService.GuardarFichaTecnicaProductoAsync(
                        dto.FichaTecnicaArchivo,
                        cancellationToken
                    );
                    dto.FichaTecnicaPdf = fichaNueva;
                }

                var producto = await _productoService.CrearAsync(dto);

                return CreatedAtAction(
                    nameof(ObtenerPorId),
                    new { idProducto = producto.IdProducto },
                    producto
                );
            }
            catch (InvalidOperationException ex)
            {
                await LimpiarArchivosNuevosAsync(imagenNueva, fichaNueva);
                return BadRequest(new
                {
                    mensaje = ex.Message
                });
            }
            catch (Exception)
            {
                await LimpiarArchivosNuevosAsync(imagenNueva, fichaNueva);
                return StatusCode(500, new
                {
                    mensaje = "No se pudo registrar el producto."
                });
            }
        }

        [HttpPut("{idProducto:int}")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(22 * 1024 * 1024)]
        public async Task<IActionResult> Actualizar(
            int idProducto,
            [FromForm] ProductoActualizarDto dto,
            CancellationToken cancellationToken
        )
        {
            string? imagenNueva = null;
            string? fichaNueva = null;

            try
            {
                var productoActual = await _productoService.ObtenerPorIdAsync(idProducto);

                if (productoActual == null)
                {
                    return NotFound(new
                    {
                        mensaje = "Producto no encontrado."
                    });
                }

                dto.ImagenUrl = productoActual.ImagenUrl;
                dto.FichaTecnicaPdf = productoActual.FichaTecnicaPdf;

                if (dto.ImagenArchivo != null)
                {
                    imagenNueva = await _archivoService.GuardarImagenProductoAsync(
                        dto.ImagenArchivo,
                        cancellationToken
                    );
                    dto.ImagenUrl = imagenNueva;
                }

                if (dto.FichaTecnicaArchivo != null)
                {
                    fichaNueva = await _archivoService.GuardarFichaTecnicaProductoAsync(
                        dto.FichaTecnicaArchivo,
                        cancellationToken
                    );
                    dto.FichaTecnicaPdf = fichaNueva;
                }

                var actualizado = await _productoService.ActualizarAsync(idProducto, dto);

                if (!actualizado)
                {
                    await LimpiarArchivosNuevosAsync(imagenNueva, fichaNueva);
                    return NotFound(new
                    {
                        mensaje = "Producto no encontrado."
                    });
                }

                if (imagenNueva != null)
                {
                    await _archivoService.EliminarArchivoLocalAsync(productoActual.ImagenUrl);
                }

                if (fichaNueva != null)
                {
                    await _archivoService.EliminarArchivoLocalAsync(productoActual.FichaTecnicaPdf);
                }

                return Ok(new
                {
                    mensaje = "Producto actualizado correctamente."
                });
            }
            catch (InvalidOperationException ex)
            {
                await LimpiarArchivosNuevosAsync(imagenNueva, fichaNueva);
                return BadRequest(new
                {
                    mensaje = ex.Message
                });
            }
            catch (Exception)
            {
                await LimpiarArchivosNuevosAsync(imagenNueva, fichaNueva);
                return StatusCode(500, new
                {
                    mensaje = "No se pudo actualizar el producto."
                });
            }
        }





        private async Task LimpiarArchivosNuevosAsync(
            string? imagenNueva,
            string? fichaNueva
        )
        {
            await _archivoService.EliminarArchivoLocalAsync(imagenNueva);
            await _archivoService.EliminarArchivoLocalAsync(fichaNueva);
        }
    }
}
