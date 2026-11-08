using Javito.Api.Contratos;
using Javito.Api.Modelos;

namespace Javito.Api.Historial;

/// <summary>
/// Una fila de <c>registro_habito</c> reducida a lo que entra al cálculo: el día y si se cumplió. El
/// valor y la meta no hacen falta porque el historial lee las filas, no las escribe.
/// </summary>
public sealed record FilaDelHistorial(DateOnly Fecha, bool Completado);

/// <summary>
/// Los tres estados de la cuadrícula. Viajan al JSON tal cual porque el SPA decide por ellos: para que
/// cumplido, vacío y neutro se distingan sin depender del color (S8), la palabra es el contrato.
/// </summary>
public static class EstadosDelHistorial
{
    public const string Cumplido = "cumplido";

    public const string Vacio = "vacio";

    public const string Neutro = "neutro";
}

/// <summary>
/// El cálculo del detalle histórico de un hábito (HU-HIS-02, L4-L7). Es una función pura: no conoce la
/// base ni el <c>HttpContext</c>, así que la regla se puede leer entera en un lugar y el controlador
/// queda para autorizar y pedir los datos.
/// </summary>
public static class CuadriculaDelHistorial
{
    /// <summary>
    /// Arma el detalle de la ventana <c>[desde, hasta]</c>. Sin primer registro no hay período
    /// evaluable: todo neutro, cero evaluables y porcentaje cero (L5).
    /// </summary>
    public static HistorialDeHabitoDto Componer(
        Habito habito,
        DateOnly desde,
        DateOnly hasta,
        DateOnly? primerRegistro,
        IReadOnlyList<FilaDelHistorial> filas)
    {
        var cumplidas = filas.Where(fila => fila.Completado).Select(fila => fila.Fecha).ToHashSet();
        var esSemanal = habito.FrecuenciaTipo == TiposDeHabito.Semanal;

        var cuadricula = new List<DiaDelHistorialDto>();

        for (var fecha = desde; fecha <= hasta; fecha = fecha.AddDays(1))
        {
            cuadricula.Add(new DiaDelHistorialDto(
                fecha,
                Estado(fecha, cumplidas, primerRegistro, esSemanal)));
        }

        // El período se evalúa desde la primera marca: antes el hábito no existía para la cuenta.
        var (cumplidos, evaluables) = primerRegistro is { } primero
            ? Contar(primero > desde ? primero : desde, hasta, habito, cumplidas)
            : (0, 0);

        return new HistorialDeHabitoDto(
            habito.Id,
            habito.Titulo,
            habito.Tipo,
            new FrecuenciaDto(habito.FrecuenciaTipo, habito.FrecuenciaVeces),
            hasta.DayNumber - desde.DayNumber + 1,
            desde,
            hasta,
            cumplidos,
            evaluables,
            Porcentaje(cumplidos, evaluables),
            cuadricula);
    }

    /// <summary>
    /// L6: cumplido si su registro está completado; vacío si es un día evaluable de un diario sin
    /// registro completado; neutro si es anterior al primer registro o si es un día sin marca de un
    /// semanal, cuya cuota no es diaria. El futuro no entra: la ventana termina en <c>hasta</c>, que es
    /// el día que manda el cliente.
    /// </summary>
    private static string Estado(
        DateOnly fecha,
        HashSet<DateOnly> cumplidas,
        DateOnly? primerRegistro,
        bool esSemanal)
    {
        if (cumplidas.Contains(fecha))
        {
            return EstadosDelHistorial.Cumplido;
        }

        if (primerRegistro is null || fecha < primerRegistro)
        {
            return EstadosDelHistorial.Neutro;
        }

        return esSemanal ? EstadosDelHistorial.Neutro : EstadosDelHistorial.Vacio;
    }

    /// <summary>La unidad de los totales es la del hábito: días en un diario, semanas en un semanal.</summary>
    private static (int Cumplidos, int Evaluables) Contar(
        DateOnly desde,
        DateOnly hasta,
        Habito habito,
        HashSet<DateOnly> cumplidas) =>
        habito.FrecuenciaTipo == TiposDeHabito.Semanal
            ? ContarSemanas(desde, hasta, habito.FrecuenciaVeces ?? 1, cumplidas)
            : ContarDias(desde, hasta, cumplidas);

    private static (int Cumplidos, int Evaluables) ContarDias(
        DateOnly desde,
        DateOnly hasta,
        HashSet<DateOnly> cumplidas)
    {
        var cumplidos = 0;
        var evaluables = 0;

        for (var fecha = desde; fecha <= hasta; fecha = fecha.AddDays(1))
        {
            evaluables++;

            if (cumplidas.Contains(fecha))
            {
                cumplidos++;
            }
        }

        return (cumplidos, evaluables);
    }

    /// <summary>
    /// L7: todas las semanas tocadas por el período entran al denominador, con la semana que empieza el
    /// lunes. Una semana cuenta como cumplida si sus días dentro del período evaluado alcanzan la
    /// cuota, así que una semana del borde se evalúa con los días que le tocan.
    /// </summary>
    private static (int Cumplidos, int Evaluables) ContarSemanas(
        DateOnly desde,
        DateOnly hasta,
        int cuota,
        HashSet<DateOnly> cumplidas)
    {
        var cumplidos = 0;
        var evaluables = 0;

        for (var lunes = LunesDe(desde); lunes <= hasta; lunes = lunes.AddDays(7))
        {
            evaluables++;

            var marcas = 0;

            for (var fecha = lunes; fecha <= hasta && fecha < lunes.AddDays(7); fecha = fecha.AddDays(1))
            {
                if (fecha >= desde && cumplidas.Contains(fecha))
                {
                    marcas++;
                }
            }

            if (marcas >= cuota)
            {
                cumplidos++;
            }
        }

        return (cumplidos, evaluables);
    }

    /// <summary>El lunes de la semana de una fecha: el domingo se cuenta hacia atrás, no hacia adelante.</summary>
    private static DateOnly LunesDe(DateOnly fecha) =>
        fecha.AddDays(-(((int)fecha.DayOfWeek + 6) % 7));

    private static decimal Porcentaje(int cumplidos, int evaluables) =>
        evaluables == 0
            ? 0m
            : Math.Round(100m * cumplidos / evaluables, 1, MidpointRounding.AwayFromZero);
}
