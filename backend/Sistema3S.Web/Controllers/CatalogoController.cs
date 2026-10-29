using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema3S.Web.Data;
using Sistema3S.Web.DTOs.Catalogo;
using Sistema3S.Web.Models;
using Microsoft.Data.SqlClient;

namespace Sistema3S.Web.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CatalogoController : ControllerBase
    {
        private readonly Bd3sContext _context;

        public CatalogoController(Bd3sContext context)
        {
            _context = context;
        }

        // La gestión está reservada al administrador por OperacionAuditadaFilter.
        // La consulta activa que utilizan los demás módulos conserva su contrato.
        [HttpGet("categorias/gestion")]
        public async Task<IActionResult> GestionarCategorias(CancellationToken ct)
        {
            var categorias = await _context.Categoria.AsNoTracking()
                .OrderByDescending(c => c.Estado).ThenBy(c => c.Nombre)
                .Select(c => new CategoriaGestionDto {
                    Id = c.IdCategoria, Nombre = c.Nombre, Descripcion = c.Descripcion,
                    Estado = c.Estado, ProductosAsignados = c.Producto.Count,
                    ProductosActivos = c.Producto.Count(p => p.IdElementoCatalogoNavigation.Estado)
                }).ToListAsync(ct);
            return Ok(categorias);
        }

        [HttpPost("categorias")]
        public async Task<IActionResult> CrearCategoria(CategoriaGuardarDto dto, CancellationToken ct)
        {
            var nombre = dto.Nombre.Trim();
            if (nombre.Length == 0) return BadRequest(new { mensaje = "Escribe el nombre de la categoría." });
            if (await NombreDuplicado(nombre, null, ct)) return CategoriaDuplicada();
            var categoria = new Categoria { Nombre = nombre, Descripcion = Descripcion(dto.Descripcion), Estado = true };
            _context.Categoria.Add(categoria);
            try { await _context.SaveChangesAsync(ct); }
            catch (DbUpdateException ex) when (EsDuplicado(ex)) { return CategoriaDuplicada(); }
            return StatusCode(201, new { id = categoria.IdCategoria, mensaje = "Categoría creada." });
        }

        [HttpPut("categorias/{id:int}")]
        public async Task<IActionResult> ActualizarCategoria(int id, CategoriaGuardarDto dto, CancellationToken ct)
        {
            var nombre = dto.Nombre.Trim();
            if (nombre.Length == 0) return BadRequest(new { mensaje = "Escribe el nombre de la categoría." });
            var categoria = await _context.Categoria.FindAsync([id], ct);
            if (categoria == null) return CategoriaNoEncontrada();
            if (await NombreDuplicado(nombre, id, ct)) return CategoriaDuplicada();
            categoria.Nombre = nombre;
            categoria.Descripcion = Descripcion(dto.Descripcion);
            try { await _context.SaveChangesAsync(ct); }
            catch (DbUpdateException ex) when (EsDuplicado(ex)) { return CategoriaDuplicada(); }
            return Ok(new { mensaje = "Categoría actualizada." });
        }

        [HttpPatch("categorias/{id:int}/estado")]
        public async Task<IActionResult> CambiarEstadoCategoria(int id, CategoriaEstadoDto dto, CancellationToken ct)
        {
            if (!dto.Estado.HasValue) return BadRequest(new { mensaje = "Indica el estado de la categoría." });
            await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            var categoria = await _context.Categoria.FindAsync([id], ct);
            if (categoria == null) return CategoriaNoEncontrada();
            if (!dto.Estado.Value)
            {
                var activos = await _context.Producto.CountAsync(p => p.IdCategoria == id && p.IdElementoCatalogoNavigation.Estado, ct);
                if (activos > 0) return Conflict(new { mensaje = $"No se puede desactivar: {(activos == 1 ? "hay un producto activo" : $"hay {activos} productos activos")} en esta categoría. Reasigna o desactiva esos productos primero.", productosActivos = activos });
            }
            categoria.Estado = dto.Estado.Value;
            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Ok(new { mensaje = categoria.Estado ? "Categoría activada." : "Categoría desactivada." });
        }

        [HttpDelete("categorias/{id:int}")]
        public async Task<IActionResult> EliminarCategoria(int id, CancellationToken ct)
        {
            var categoria = await _context.Categoria.FindAsync([id], ct);
            if (categoria == null) return CategoriaNoEncontrada();
            if (await _context.Producto.AnyAsync(p => p.IdCategoria == id, ct))
                return Conflict(new { mensaje = "Esta categoría tiene productos asignados y no se puede eliminar. Sólo podrás desactivarla cuando no tenga productos activos." });
            _context.Categoria.Remove(categoria);
            try { await _context.SaveChangesAsync(ct); }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 })
            { return Conflict(new { mensaje = "La categoría está en uso y no se puede eliminar. Actualiza la lista para revisar sus productos asignados." }); }
            return Ok(new { mensaje = "Categoría eliminada." });
        }

        private Task<bool> NombreDuplicado(string nombre, int? id, CancellationToken ct) =>
            _context.Categoria.AnyAsync(c => (!id.HasValue || c.IdCategoria != id.Value)
                && c.Nombre.ToUpper() == nombre.ToUpper(), ct);
        private static string? Descripcion(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
        private static bool EsDuplicado(DbUpdateException ex) => ex.InnerException is SqlException { Number: 2601 or 2627 };
        private ConflictObjectResult CategoriaDuplicada() => Conflict(new { mensaje = "Ya existe una categoría con ese nombre. Revisa también las inactivas." });
        private NotFoundObjectResult CategoriaNoEncontrada() => NotFound(new { mensaje = "La categoría ya no está disponible. Actualiza la lista." });

        [HttpGet("categorias")]
        public async Task<IActionResult> ListarCategorias()
        {
            var data = await _context.Categoria
                .Where(c => c.Estado)
                .OrderBy(c => c.Nombre)
                .Select(c => new CatalogoItemDto
                {
                    Id = c.IdCategoria,
                    Nombre = c.Nombre
                })
                .ToListAsync();

            return Ok(data);
        }

        [HttpGet("marcas")]
        public async Task<IActionResult> ListarMarcas()
        {
            var data = await _context.Marca
                .Where(m => m.Estado)
                .OrderBy(m => m.Nombre)
                .Select(m => new CatalogoItemDto
                {
                    Id = m.IdMarca,
                    Nombre = m.Nombre
                })
                .ToListAsync();

            return Ok(data);
        }

        [HttpGet("unidades-medida")]
        public async Task<IActionResult> ListarUnidadesMedida()
        {
            var data = await _context.UnidadMedida
                .Where(u => u.Estado)
                .OrderBy(u => u.Nombre)
                .Select(u => new CatalogoItemDto
                {
                    Id = u.IdUnidadMedida,
                    Nombre = u.Nombre
                })
                .ToListAsync();

            return Ok(data);
        }
    }
}
