namespace Javito.Api.Modelos;

/// <summary>
/// Categoría del catálogo compartido: clasifica hábitos y artículos. Se archiva en lugar de borrarse,
/// así lo que ya la usaba la conserva.
/// </summary>
/// <remarks>
/// Refleja la tabla 1:1 y no lleva DataAnnotations: en una WebAPI el modelo es persistencia, y las
/// reglas de entrada se validan en el DTO que llega por HTTP, donde producen un 400 legible. Los
/// límites de longitud viven en database.sql.
/// </remarks>
public class Categoria
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public bool Activo { get; set; } = true;
}
