namespace Javito.Api.Paginacion;

/// <summary>
/// El patrón de LIKE de una búsqueda de texto libre. Vive en <c>Paginacion/</c>, junto al resto de lo
/// que comparten los listados, porque lo usan más de un repositorio: hábitos busca por título y
/// usuarios busca por nombre o por email, y el escape de los comodines tiene que ser el mismo en los
/// dos. Una copia por repositorio es una copia que se arregla en uno solo.
/// </summary>
public static class PatronDeBusqueda
{
    /// <summary>
    /// Envuelve el texto del usuario en comodines, neutralizando los que ya son comodines: buscar
    /// «50%» tiene que encontrar ese título literal y no traer todo. La barra va primero porque es el
    /// carácter de escape de LIKE. Vacío o en blanco no filtra: devuelve <c>null</c>.
    /// </summary>
    public static string? APatron(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return null;
        }

        var escapado = texto.Trim()
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_");

        return $"%{escapado}%";
    }
}
