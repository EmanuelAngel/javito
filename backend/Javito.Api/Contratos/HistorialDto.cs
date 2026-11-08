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
