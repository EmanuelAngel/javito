namespace Javito.Api.Modelos;

/// <summary>
/// Rutina del usuario: agrupa sus hábitos bajo un nombre para hacerlos en bloque. Es suya y de nadie
/// más, por eso el repositorio siempre filtra por <see cref="UsuarioId"/>.
/// </summary>
/// <remarks>
/// Refleja la tabla 1:1 y no lleva DataAnnotations: las reglas de entrada se validan en el DTO que
/// llega por HTTP. Los límites de longitud viven en database.sql.
/// </remarks>
public class Rutina
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public bool Activo { get; set; } = true;
}
