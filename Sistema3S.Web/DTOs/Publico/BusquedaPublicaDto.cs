namespace Sistema3S.Web.DTOs.Publico
{
    public class BusquedaPublicaDto
    {
        public string Query { get; set; } = string.Empty;

        public List<ProductoPublicoDto> Productos { get; set; } = new();

        public List<ServicioPublicoDto> Servicios { get; set; } = new();

        public List<CategoriaPublicaDto> Categorias { get; set; } = new();

        public List<MarcaPublicaDto> Marcas { get; set; } = new();

        public int TotalResultados { get; set; }
    }
}