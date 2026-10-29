namespace Javito.Api.Contratos;

/// <summary>Categoría tal como la expone la API.</summary>
public sealed record CategoriaDto(int Id, string Nombre, string? Descripcion, bool Activo);
