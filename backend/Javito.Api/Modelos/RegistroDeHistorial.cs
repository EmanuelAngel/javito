namespace Javito.Api.Modelos;

/// <summary>
/// Una fila del historial: el registro con el título del hábito al que pertenece. No es una tabla
/// sino una proyección de lectura sobre <c>registro_habito</c> y <c>habito</c>, y por eso la meta que
/// lleva es <c>MetaObjetivo</c> —la del día en que se cerró— y no la meta vigente del hábito.
/// </summary>
public class RegistroDeHistorial
{
    public DateOnly Fecha { get; set; }

    public int HabitoId { get; set; }

    public string Habito { get; set; } = string.Empty;

    public decimal Valor { get; set; }

    /// <summary>La meta con la que se cerró ese día: editar el hábito no reescribe el pasado.</summary>
    public decimal MetaObjetivo { get; set; }

    public bool Completado { get; set; }
}
