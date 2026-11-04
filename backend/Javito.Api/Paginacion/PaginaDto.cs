namespace Javito.Api.Paginacion;

/// <summary>
/// Una página de resultados con su metadata, en español como el resto del contrato de la API. El
/// nombre de cada propiedad es el nombre que viaja: <c>elementos</c>, <c>pagina</c>,
/// <c>tamanioDePagina</c>, <c>totalDeRegistros</c> y <c>totalDePaginas</c>. No hay alias en inglés.
/// </summary>
/// <typesparam name="T">Lo que va adentro de <c>elementos</c>: el DTO de cada catálogo.</typesparam>
public sealed record PaginaDto<T>(
    IReadOnlyList<T> Elementos,
    int Pagina,
    int TamanioDePagina,
    int TotalDeRegistros,
    int TotalDePaginas)
{
    /// <summary>
    /// Arma la página con la metadata de la request y el total que devolvió la base. Existe para que
    /// <c>totalDePaginas</c> se calcule en un solo lugar: repartido por controlador, cada uno lo
    /// redondea a su manera.
    /// </summary>
    /// <remarks>
    /// <c>pagina</c> y <c>tamanioDePagina</c> se repiten tal como se pidieron, aunque la página esté
    /// pasada del final: el servidor no corrige en silencio lo que el cliente pidió.
    /// </remarks>
    public static PaginaDto<T> De(
        IReadOnlyList<T> elementos,
        ParametrosDePagina parametros,
        int totalDeRegistros) => new(
            elementos,
            parametros.Pagina,
            parametros.TamanioDePagina,
            totalDeRegistros,
            // Techo de la división sin pasar por punto flotante: 0 filas son 0 páginas, no 1.
            (totalDeRegistros + parametros.TamanioDePagina - 1) / parametros.TamanioDePagina);
}
