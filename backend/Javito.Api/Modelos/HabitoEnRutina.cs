namespace Javito.Api.Modelos;

/// <summary>
/// Un hábito dentro de una rutina, con el lugar que ocupa. No es una tabla propia: sale del join de
/// <c>rutina_habito</c> con <c>habito</c>, y el orden es el de la relación, no el del hábito.
/// </summary>
public class HabitoEnRutina
{
    /// <summary>Id del hábito, no el de la fila de <c>rutina_habito</c>: es lo que el SPA usa para quitarlo.</summary>
    public int Id { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public int Orden { get; set; }
}
