using Sistema3S.Web.DTOs.Auth;

namespace Sistema3S.Web.Services.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResultadoDto> LoginAsync(LoginDto dto);

        Task<List<PermisoSesionDto>> ObtenerPermisosUsuarioAsync(int idUsuario);

        Task<CambiarContrasenaResultadoDto> CambiarContrasenaInicialAsync(
            CambiarContrasenaInicialDto dto
        );
    }
}
