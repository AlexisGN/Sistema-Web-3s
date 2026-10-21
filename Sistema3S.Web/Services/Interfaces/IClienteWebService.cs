using Sistema3S.Web.DTOs.ClienteWeb;
using Sistema3S.Web.DTOs.Comun;

namespace Sistema3S.Web.Services.Interfaces
{
    public interface IClienteWebService
    {
        Task<ClienteWebConsultaDocumentoDto> ConsultarDocumentoAsync(
            string tipoDocumento,
            string numeroDocumento
        );

        Task<ClienteWebSesionDto> RegistrarAsync(ClienteWebRegistroDto dto);

        Task<ClienteWebSesionDto> LoginAsync(ClienteWebLoginDto dto);

        Task<ClienteWebCotizacionRegistradaDto> RegistrarCotizacionAsync(
            int idCliente,
            int idUsuario,
            ClienteWebCotizacionCrearDto dto
        );

        Task<ResultadoPaginadoDto<ClienteWebCotizacionResumenDto>> ListarCotizacionesAsync(
            int idCliente,
            int pagina,
            int tamanioPagina
        );

        Task<ClienteWebCotizacionDetalleResponseDto?> ObtenerCotizacionAsync(
            int idCliente,
            int idCotizacion
        );
    }
}