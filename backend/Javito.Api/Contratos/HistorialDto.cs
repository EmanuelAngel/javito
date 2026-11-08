using Javito.Api.Modelos;

namespace Javito.Api.Contratos;

/// <summary>
/// Una fila del historial tal como viaja por HTTP: la fecha, el hábito, el valor alcanzado, la meta
/// con la que se cerró ese día y si se completó (HU-HIS-01). Las notas no viajan todavía: las escribe
/// #74, que es quien cierra esa regla.
/// </summary>
public sealed record RegistroDeHistorialDto(
    DateOnly Fecha,
    int HabitoId,
    string Habito,
    decimal Valor,
    decimal MetaObjetivo,
    bool Completado);

/// <summary>
/// El detalle histórico de un hábito (HU-HIS-02): la ventana elegida, sus totales y el día por día.
/// <c>Cumplidos</c> y <c>Evaluables</c> siguen la unidad del hábito —días en un diario, semanas en un
/// semanal—, que es la misma del porcentaje (L4).
/// </summary>
public sealed record HistorialDeHabitoDto(
    int HabitoId,
    string Habito,
    string Tipo,
    FrecuenciaDto Frecuencia,
    int Dias,
    DateOnly Desde,
    DateOnly Hasta,
    int Cumplidos,
    int Evaluables,
    decimal Porcentaje,
    IReadOnlyList<DiaDelHistorialDto> Cuadricula);

/// <summary>Un día de la cuadrícula: la fecha y su estado, que es la única cosa que pinta el cliente.</summary>
public sealed record DiaDelHistorialDto(DateOnly Fecha, string Estado);

/// <summary>Proyección del historial a lo que viaja por HTTP.</summary>
public static class MapeoDeHistorial
{
    public static RegistroDeHistorialDto ADto(RegistroDeHistorial registro) => new(
        registro.Fecha,
        registro.HabitoId,
        registro.Habito,
        registro.Valor,
        registro.MetaObjetivo,
        registro.Completado);
}
