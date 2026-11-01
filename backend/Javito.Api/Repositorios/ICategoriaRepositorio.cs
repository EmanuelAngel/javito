using Javito.Api.Modelos;

namespace Javito.Api.Repositorios;

public interface ICategoriaRepositorio
{
    Task<IReadOnlyList<Categoria>> ListarAsync(
        bool? activo,
        CancellationToken cancellationToken = default);

    Task<Categoria?> BuscarAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca por nombre exacto. La comparación la resuelve la collation de la base, así que no
    /// distingue mayúsculas ni acentos.
    /// </summary>
    Task<Categoria?> BuscarPorNombreAsync(string nombre, CancellationToken cancellationToken = default);

    /// <summary>Inserta la categoría y devuelve el id que le asignó la base.</summary>
    Task<int> CrearAsync(Categoria categoria, CancellationToken cancellationToken = default);

    /// <summary>Devuelve <c>false</c> cuando no hay ninguna fila con ese id.</summary>
    Task<bool> ActualizarAsync(
        int id,
        string nombre,
        string? descripcion,
        CancellationToken cancellationToken = default);

    /// <summary>Devuelve <c>false</c> cuando no hay ninguna fila con ese id.</summary>
    Task<bool> CambiarActivoAsync(int id, bool activo, CancellationToken cancellationToken = default);
}
