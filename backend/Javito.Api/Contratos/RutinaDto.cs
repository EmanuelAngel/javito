namespace Javito.Api.Contratos;

/// <summary>Rutina tal como la expone el listado de la API.</summary>
public sealed record RutinaDto(int Id, string Nombre, string? Descripcion, bool Activo);
