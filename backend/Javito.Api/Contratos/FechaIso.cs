using System.Globalization;

namespace Javito.Api.Contratos;

/// <summary>
/// La fecha de los recursos del día viaja como aaaa-mm-dd y la manda el cliente: el servidor corre en
/// UTC y no sabe en qué huso horario vive quien marca.
/// </summary>
public static class FechaIso
{
    public const string Formato = "yyyy-MM-dd";

    /// <summary>Desde dónde se aceptan marcas. Antes no hay datos ni sentido.</summary>
    public static readonly DateOnly Desde = new(2000, 1, 1);

    public static bool TryParsear(string valor, out DateOnly fecha) =>
        DateOnly.TryParseExact(valor, Formato, CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha);

    /// <summary>
    /// Hasta mañana en UTC: hoy o ayer, y un día de margen para que un cliente en UTC+13 no quede
    /// afuera de su propio día. Marcar el futuro no es una corrección, es un dato que nadie midió.
    /// </summary>
    public static bool EstaEnRango(DateOnly fecha) =>
        fecha >= Desde && fecha <= DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);

    public static ErrorDeNegocio ErrorDeFormato(string valor) => new(
        "fecha_invalida",
        "Fecha inválida",
        $"«{valor}» no es una fecha con formato aaaa-mm-dd.",
        StatusCodes.Status400BadRequest);

    public static ErrorDeNegocio ErrorDeRango(DateOnly fecha) => new(
        "fecha_fuera_de_rango",
        "Fecha fuera de rango",
        $"No se puede marcar el {fecha:yyyy-MM-dd}: el rango va del {Desde:yyyy-MM-dd} a mañana.",
        StatusCodes.Status400BadRequest);
}
