namespace Javito.Api.Modelos;

/// <summary>
/// Hábito del usuario. Refleja la tabla 1:1 y no lleva DataAnnotations: las reglas de entrada se
/// validan en el DTO que llega por HTTP y los límites de longitud viven en database.sql.
/// </summary>
public class Habito
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    /// <summary>Puede ser <c>null</c>: un hábito archivado o viejo puede no tener categoría.</summary>
    public int? CategoriaId { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public string Tipo { get; set; } = TiposDeHabito.SiNo;

    /// <summary>Meta diaria; <c>null</c> en sí/no y en abstinencia, donde vale 1.</summary>
    public decimal? Meta { get; set; }

    public string? Unidad { get; set; }

    public string FrecuenciaTipo { get; set; } = TiposDeHabito.Diaria;

    /// <summary>Cuota semanal, de 1 a 6. Obligatoria en la frecuencia semanal.</summary>
    public int? FrecuenciaVeces { get; set; }

    public bool Activo { get; set; } = true;
}
