using Javito.Api.Modelos;

namespace Javito.Api.Contratos;

/// <summary>
/// Proyección del día a lo que viaja por HTTP. El objetivo semanal sale de la frecuencia: siete días
/// en un hábito diario y la cuota elegida en uno semanal.
/// </summary>
public static class MapeoDeDias
{
    public static DiaDto ADto(DateOnly fecha, IReadOnlyList<HabitoDelDia> habitos) =>
        new(fecha, habitos.Select(ADto).ToList());

    private static HabitoDelDiaDto ADto(HabitoDelDia habito) => new(
        habito.Id,
        habito.Titulo,
        habito.Categoria,
        habito.Tipo,
        habito.Meta,
        habito.Unidad,
        new FrecuenciaDto(habito.FrecuenciaTipo, habito.FrecuenciaVeces),
        new SemanaDto(habito.CumplidosEnLaSemana, Objetivo(habito)),
        habito.Valor is null ? null : new RegistroDelDiaDto(habito.Valor.Value, habito.Completado));

    /// <summary>
    /// En un hábito diario el objetivo es la semana entera. En uno semanal es la cuota, y la columna
    /// es obligatoria en ese caso, así que el 7 es sólo una red por si la base trae una fila vieja.
    /// </summary>
    private static int Objetivo(HabitoDelDia habito) =>
        habito.FrecuenciaTipo == TiposDeHabito.Semanal ? habito.FrecuenciaVeces ?? 7 : 7;
}
