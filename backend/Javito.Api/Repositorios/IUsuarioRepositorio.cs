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

    /// <summary>
    /// Una página de cuentas para el listado de administración, más el total que necesita el
    /// paginador. El orden es por id descendente: el id es <c>AUTO_INCREMENT</c>, así que ese orden es
    /// el de alta con el más nuevo primero, sin una columna de fecha que la tabla no tiene.
    /// </summary>
    /// <param name="activo">Estado a filtrar; <c>null</c> trae los dos.</param>
    /// <param name="q">
    /// Pedazo del nombre o del email, con la misma semántica que la búsqueda de hábitos: vacío o en
    /// blanco no filtra y `%`, `_` y la barra se buscan literales.
    /// </param>
    Task<(IReadOnlyList<Usuario> Elementos, int Total)> ListarAsync(
        bool? activo,
        string? q,
        int limite,
        long desplazamiento,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deja el avatar de la cuenta en la ruta indicada (o en <c>null</c>) y dice si tocó la fila. El
    /// archivo anterior lo borra quien llama: el repositorio no conoce el disco.
    /// </summary>
    Task<bool> ActualizarAvatarAsync(int id, string? avatarUrl, CancellationToken cancellationToken = default);
}
