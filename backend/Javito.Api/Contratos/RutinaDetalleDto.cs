namespace Javito.Api.Contratos;

/// <summary>Rutina con sus hábitos, en el orden en que se hacen.</summary>
public sealed record RutinaDetalleDto(
    int Id,
    string Nombre,
    string? Descripcion,
    bool Activo,
    IReadOnlyList<HabitoEnRutinaDto> Habitos);
