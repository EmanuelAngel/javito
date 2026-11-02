namespace Javito.Api.Modelos;

/// <summary>
/// Marca de un hábito en un día. La identidad es el par (hábito, fecha): así lo impone el UNIQUE de
/// la tabla, y por eso «sin marcar» es la ausencia de fila.
/// </summary>
public class Registro
{
    public int Id { get; set; }

    public int HabitoId { get; set; }

    public DateOnly Fecha { get; set; }

    public decimal ValorAlcanzado { get; set; }

    /// <summary>
    /// La meta con la que se cerró el día. Se copia en el momento de marcar: si el hábito cambia de
    /// meta, el historial no se reescribe.
    /// </summary>
    public decimal MetaObjetivo { get; set; }

    public bool Completado { get; set; }

    public string? Notas { get; set; }
}
