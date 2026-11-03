namespace Javito.Api.Modelos;

/// <summary>
/// Proyección de lectura del día: un hábito activo del usuario con su registro de la fecha pedida y
/// el progreso de la semana en curso. No es una tabla; sale de habito + categoria + registro_habito,
/// y por eso es de sólo lectura: las marcas se escriben en su propio recurso.
/// </summary>
public class HabitoDelDia
{
    public int Id { get; set; }

    public string Titulo { get; set; } = string.Empty;

    /// <summary>Nombre de la categoría, o <c>null</c> si el hábito no tiene (HU-HAB-05).</summary>
    public string? Categoria { get; set; }

    public string Tipo { get; set; } = string.Empty;

    /// <summary>Meta diaria del hábito. Es <c>null</c> en sí/no y en abstinencia: valen 1.</summary>
    public decimal? Meta { get; set; }

    public string? Unidad { get; set; }

    public string FrecuenciaTipo { get; set; } = string.Empty;

    public int? FrecuenciaVeces { get; set; }

    /// <summary>
    /// Los siete días de la semana en curso (lunes a domingo) con su marca. Incluye los días que
    /// todavía no llegaron, que vienen sin completar.
    /// </summary>
    public List<DiaDeLaSemana> Semana { get; set; } = [];

    /// <summary>Valor alcanzado en la fecha pedida; <c>null</c> cuando no hay marca.</summary>
    public decimal? Valor { get; set; }

    public bool Completado { get; set; }
}
