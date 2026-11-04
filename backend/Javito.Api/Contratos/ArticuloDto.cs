using System.ComponentModel.DataAnnotations;

namespace Javito.Api.Contratos;

/// <summary>
/// Artículo tal como lo expone la API. La categoría viaja por id, igual que en <see cref="HabitoDto"/>
/// y por la misma razón: el SPA ya tiene el catálogo para el desplegable.
/// </summary>
/// <param name="FechaPublicacion">
/// aaaa-mm-dd, el mismo formato de fecha del resto de la API. La hora de publicación no se muestra
/// en ninguna pantalla, así que mostrarla sería ruido.
/// </param>
public sealed record ArticuloDto(
    int Id,
    int CategoriaId,
    string Titulo,
    string Resumen,
    string ContenidoMarkdown,
    string? ImagenPortadaUrl,
    string FechaPublicacion,
    bool Activo);

/// <summary>
/// Un artículo tal como lo ve el catálogo: <b>sin el cuerpo</b>. El punto de HU-ART-04 es no traer
/// todo el blog de una vez, y mandar seis artículos con su Markdown completo en una lista de tarjetas
/// sería exactamente eso. La tarjeta muestra portada, título, resumen, categoría y fecha.
/// </summary>
/// <remarks>
/// <c>activo</c> viaja aunque el catálogo sólo devuelve activos: es la regla afirmada en el contrato,
/// no un dato que el cliente tenga que recalcular.
/// </remarks>
public sealed record ArticuloDelCatalogoDto(
    int Id,
    int CategoriaId,
    string Titulo,
    string Resumen,
    string? ImagenPortadaUrl,
    string FechaPublicacion,
    bool Activo);

/// <summary>
/// El alta de un artículo. Las anotaciones van sobre el parámetro del record, sin el prefijo
/// <c>[property: …]</c>: con el prefijo MVC las ignora y avisa al validar. Los límites de título y
/// resumen son los de database.sql; el del contenido es más chico que el <c>MEDIUMTEXT</c> de la
/// columna porque un cuerpo de 16 MB no es un artículo.
/// </summary>
/// <remarks>
/// La portada no viaja acá: es un archivo del formulario y el controlador la recibe aparte, así este
/// contrato sirve igual si algún día el alta deja de ser multipart.
/// </remarks>
/// <remarks>
/// El prefijo <c>(?s)</c> no es decorativo: <c>RegularExpressionAttribute</c> exige que el patrón
/// cubra la cadena entera y, sin ese flag, <c>.</c> no matchea saltos de línea — con lo cual cualquier
/// contenido Markdown de más de un renglón se rechazaba como si estuviera vacío.
/// </remarks>
public sealed record ArticuloEntradaDto(
    [Required(ErrorMessage = "El título es obligatorio.")]
    // `.*\S.*` exige al menos un caracter que no sea espacio: `[Required]` acepta `"   "`.
    [RegularExpression(@"(?s).*\S.*", ErrorMessage = "El título es obligatorio.")]
    [StringLength(150, ErrorMessage = "El título no puede superar los 150 caracteres.")]
    string? Titulo,
    [Required(ErrorMessage = "El resumen es obligatorio.")]
    [RegularExpression(@"(?s).*\S.*", ErrorMessage = "El resumen es obligatorio.")]
    [StringLength(300, ErrorMessage = "El resumen no puede superar los 300 caracteres.")]
    string? Resumen,
    [Required(ErrorMessage = "El contenido es obligatorio.")]
    [RegularExpression(@"(?s).*\S.*", ErrorMessage = "El contenido es obligatorio.")]
    [StringLength(50_000, ErrorMessage = "El contenido no puede superar los 50 000 caracteres.")]
    string? ContenidoMarkdown,
    // Sin categoría el campo llega en 0: la base lo exige NOT NULL y el rango lo dice antes de ir.
    [Range(1, int.MaxValue, ErrorMessage = "La categoría es obligatoria.")]
    int CategoriaId);
