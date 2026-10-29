using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema3S.Web.Data;
using Sistema3S.Web.DTOs.Auth;
using Sistema3S.Web.Services.Interfaces;

namespace Sistema3S.Web.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly Bd3sContext _db;

        public AuthController(IAuthService authService, Bd3sContext db)
        {
            _authService = authService;
            _db = db;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                var resultado = await _authService.LoginAsync(dto);

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
                    mensaje = "No se pudo iniciar sesión."
                });
            }
        }

        [AllowAnonymous]
        [HttpPost("cambiar-contrasena-inicial")]
        public async Task<IActionResult> CambiarContrasenaInicial(
            [FromBody] CambiarContrasenaInicialDto dto
        )
        {
            try
            {
                var resultado = await _authService.CambiarContrasenaInicialAsync(dto);

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
                    mensaje = "No se pudo actualizar la contraseña inicial."
                });
            }
        }

        [Authorize]
        [HttpGet("perfil")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Perfil()
        {
            if (!int.TryParse(User.FindFirst("idUsuario")?.Value, out var idUsuario) || User.HasClaim(c => c.Type == "idCliente"))
                return Unauthorized();
            var usuario = await _db.Usuario.AsNoTracking().Where(u => u.IdUsuario == idUsuario && u.Estado && u.IdRolNavigation.Estado)
                .Select(u => new { u.IdUsuario, u.IdRol, u.Correo, Rol = u.IdRolNavigation.Nombre }).FirstOrDefaultAsync();
            if (usuario == null) return Unauthorized();
            if (usuario.Rol.Trim().Equals("Cliente", StringComparison.OrdinalIgnoreCase) || usuario.Rol.Trim().Equals("Cliente web", StringComparison.OrdinalIgnoreCase))
                return Forbid();
            var detalle = await _authService.ObtenerPermisosUsuarioAsync(idUsuario);
            // Misma sesión/JWT; los permisos se vuelven a consultar en la BD vigente.
            return Ok(new { idUsuario = usuario.IdUsuario, idRol = usuario.IdRol, correo = usuario.Correo, rol = usuario.Rol,
                permisos = detalle.Where(p => p.Asignado).Select(p => p.Nombre).ToList(), permisosDetalle = detalle });
        }
    }
}
