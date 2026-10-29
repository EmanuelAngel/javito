using System.ComponentModel.DataAnnotations;

namespace Javito.Api.Modelos;

/// <summary>
/// Categoría del catálogo compartido: clasifica hábitos y artículos. Se archiva en lugar de borrarse,
/// así lo que ya la usaba la conserva.
/// </summary>
public class Categoria
{
    [Display(Name = "Código")]
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(60, ErrorMessage = "El nombre no puede superar los 60 caracteres")]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(255, ErrorMessage = "La descripción no puede superar los 255 caracteres")]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [Display(Name = "Activa")]
    public bool Activo { get; set; } = true;
}
