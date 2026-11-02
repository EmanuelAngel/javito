using System.ComponentModel.DataAnnotations;

namespace Javito.Api.Contratos;

/// <summary>
/// Hábito tal como lo expone la API. La categoría viaja por id: el SPA ya tiene el catálogo para el
/// desplegable, así que no hace falta repetir el nombre en cada fila ni unirlos en el listado.
/// </summary>
public sealed record HabitoDto(
    int Id,
    string Titulo,
    string? Descripcion,
    int? CategoriaId,
    string Tipo,
    decimal? Meta,
    string? Unidad,
    FrecuenciaDto Frecuencia,
    bool Activo);

/// <summary>
/// El alta de un hábito. Los campos de meta y unidad se validan en el controlador y no con
/// anotaciones porque dependen del tipo: sólo un contador los usa.
/// </summary>
public sealed record HabitoEntradaDto(
    [StringLength(100, MinimumLength = 1, ErrorMessage = "El título admite entre 1 y 100 caracteres.")]
    string? Titulo,
    [StringLength(255, ErrorMessage = "La descripción admite hasta 255 caracteres.")]
    string? Descripcion,
    int? CategoriaId,
    [Required(ErrorMessage = "El tipo es obligatorio.")]
    string? Tipo,
    decimal? Meta,
    [StringLength(30, ErrorMessage = "La unidad admite hasta 30 caracteres.")]
    string? Unidad,
    [Required(ErrorMessage = "La frecuencia es obligatoria.")]
    FrecuenciaEntradaDto? Frecuencia);

/// <summary>
/// Frecuencia tal como llega: diaria o N veces por semana. `veces` se ignora cuando es diaria y es
/// obligatorio cuando es semanal.
/// </summary>
public sealed record FrecuenciaEntradaDto(
    [Required(ErrorMessage = "El tipo de frecuencia es obligatorio.")]
    string? Tipo,
    int? Veces);
