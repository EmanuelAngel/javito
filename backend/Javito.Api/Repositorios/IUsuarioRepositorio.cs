using Javito.Api.Modelos;

namespace Javito.Api.Repositorios;

public interface IUsuarioRepositorio
{
    /// <summary>
    /// Busca por email. La intercalación de la columna es insensible a mayúsculas, así que
    /// "Usuario@javito.local" encuentra la misma cuenta que "usuario@javito.local".
    /// </summary>
    Task<Usuario?> BuscarPorEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<Usuario?> BuscarPorIdAsync(int id, CancellationToken cancellationToken = default);
}
