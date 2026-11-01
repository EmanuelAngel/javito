using Javito.Api.Modelos;

namespace Javito.Api.Repositorios;

public interface IDiaRepositorio
{
    /// <summary>
    /// Hábitos activos del usuario con su registro de <paramref name="fecha"/> y los días
    /// completados de la semana en curso. Es de sólo lectura: el día no se escribe.
    /// </summary>
    Task<IReadOnlyList<HabitoDelDia>> ListarAsync(
        int usuarioId,
        DateOnly fecha,
        CancellationToken cancellationToken = default);
}
