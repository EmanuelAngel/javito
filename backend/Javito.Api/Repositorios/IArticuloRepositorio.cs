using Javito.Api.Modelos;

namespace Javito.Api.Repositorios;

public interface IArticuloRepositorio
{
    /// <summary>Inserta el artículo y devuelve el id que le asignó la base.</summary>
    Task<int> CrearAsync(Articulo articulo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Devuelve el artículo esté activo o no: si lo retirado se puede abrir es una decisión de la
    /// pantalla que lo pide, no del repositorio. El catálogo filtra por su cuenta.
    /// </summary>
    Task<Articulo?> BuscarAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Una página del catálogo con el total de filas que entran en el filtro. Van juntos y de una sola
    /// vez porque la pantalla necesita los dos para dibujar la paginación, y porque el conteo tiene que
    /// salir del mismo <c>WHERE</c> que la página: un total con otro criterio mentiría.
    /// </summary>
    /// <param name="categoriaId">
    /// Filtro opcional. Una categoría inexistente o inactiva no es un error de entrada: no hay filas.
    /// </param>
    /// <param name="limite">Cuántas filas trae la página.</param>
    /// <param name="desplazamiento">Cuántas filas se saltean antes de la página.</param>
    Task<(IReadOnlyList<Articulo> Elementos, int Total)> ListarCatalogoAsync(
        int? categoriaId,
        int limite,
        long desplazamiento,
        CancellationToken cancellationToken = default);
}
