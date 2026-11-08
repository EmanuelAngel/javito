using System.ComponentModel.DataAnnotations;

namespace Javito.Api.Contratos;

/// <summary>
/// Cuerpo del alta y de la edición de una rutina. Las anotaciones van sobre el parámetro del record, sin el prefijo
/// <c>[property: …]</c>: con el prefijo MVC las ignora y avisa al validar. Los límites son los de
/// <c>rutina</c> en database.sql.
/// </summary>
public sealed record RutinaEntradaDto(
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    // `.*\S.*` exige al menos un caracter que no sea espacio: `[Required]` acepta `"   "`. El
    // patrón cubre la cadena entera porque `RegularExpressionAttribute` no busca "contiene".
    [RegularExpression(@".*\S.*", ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(60, ErrorMessage = "El nombre no puede superar los 60 caracteres.")]
    string Nombre,
    [StringLength(255, ErrorMessage = "La descripción no puede superar los 255 caracteres.")]
    string? Descripcion);
