using Javito.Api.Modelos;

namespace Javito.Api.Repositorios;

public interface IHabitoRepositorio
{
    /// <summary>
    /// Busca el hábito entre los del usuario: uno ajeno no existe para él y la API responde 404. Es
    /// también la validación de pertenencia de todo lo que se cuelga de un hábito.
    /// </summary>
    Task<Habito?> BuscarAsync(int id, int usuarioId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Crea o reemplaza la marca del par (hábito, fecha) y devuelve la fila que quedó guardada. La
    /// identidad la impone el UNIQUE de la tabla: reintentar el mismo toque es un no-op.
    /// </summary>
    Task<Registro> GuardarRegistroAsync(
        Registro registro,
        CancellationToken cancellationToken = default);

    /// <summary>Devuelve <c>false</c> cuando no había ninguna marca: desmarcar es idempotente.</summary>
    Task<bool> BorrarRegistroAsync(
        int habitoId,
        DateOnly fecha,
        CancellationToken cancellationToken = default);
}
