using Javito.Api.Modelos;

namespace Javito.Api.Repositorios;

public interface ICategoriaRepositorio
{
    Task<IReadOnlyList<Categoria>> ListarAsync(
        bool? activo,
        CancellationToken cancellationToken = default);
}
