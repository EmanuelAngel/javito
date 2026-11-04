namespace Javito.Api.Modelos;

/// <summary>
/// Artículo del blog: lo que escribe Administración sobre hábitos. Se retira en lugar de borrarse,
/// así el catálogo deja de mostrarlo sin perder lo ya publicado.
/// </summary>
/// <remarks>
/// Refleja la tabla 1:1 y no lleva DataAnnotations: en una WebAPI el modelo es persistencia, y las
/// reglas de entrada se validan en el DTO que llega por HTTP, donde producen un 400 legible. Los
/// límites de longitud viven en database.sql. La categoría viaja por id, como en <c>Habito</c>: el
/// SPA ya tiene el catálogo y no hace falta unir el nombre en cada fila.
/// </remarks>
public class Articulo
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    public int CategoriaId { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string Resumen { get; set; } = string.Empty;

    public string ContenidoMarkdown { get; set; } = string.Empty;

    /// <summary>Ruta relativa web de la portada, o <c>null</c>: sin portada se publica igual.</summary>
    public string? ImagenPortadaUrl { get; set; }

    /// <summary>La pone la base al insertar; el cliente no la manda nunca.</summary>
    public DateTime FechaPublicacion { get; set; }

    public bool Activo { get; set; } = true;
}
