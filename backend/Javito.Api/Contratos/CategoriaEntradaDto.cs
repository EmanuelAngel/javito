using System.ComponentModel.DataAnnotations;

namespace Javito.Api.Contratos;

/// <summary>
/// Cuerpo del alta y de la edición de una categoría. Las anotaciones van sobre el parámetro del
/// record, sin el prefijo <c>[property: …]</c>: con el prefijo MVC las ignora y avisa al validar. Los
/// límites son los de database.sql.
/// </summary>
public sealed record CategoriaEntradaDto(
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(60, ErrorMessage = "El nombre no puede superar los 60 caracteres.")]
    string Nombre,
    [StringLength(255, ErrorMessage = "La descripción no puede superar los 255 caracteres.")]
    string? Descripcion);
