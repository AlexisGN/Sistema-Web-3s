using System.ComponentModel.DataAnnotations;

namespace Sistema3S.Web.DTOs.Catalogo;

public sealed class CategoriaGuardarDto
{
    [Required(ErrorMessage = "Escribe el nombre de la categoría.")]
    [StringLength(100, ErrorMessage = "El nombre admite hasta 100 caracteres.")]
    public string Nombre { get; set; } = "";

    [StringLength(300, ErrorMessage = "La descripción admite hasta 300 caracteres.")]
    public string? Descripcion { get; set; }
}

public sealed class CategoriaEstadoDto
{
    [Required(ErrorMessage = "Indica si la categoría debe estar activa.")]
    public bool? Estado { get; set; }
}

public sealed class CategoriaGestionDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public bool Estado { get; set; }
    public int ProductosAsignados { get; set; }
    public int ProductosActivos { get; set; }
}
