using Javito.Api.Modelos;

namespace Javito.Api.Repositorios;

public interface IRutinaRepositorio
{
    /// <summary>Las rutinas del usuario, en el orden en que las creó.</summary>
    Task<IReadOnlyList<Rutina>> ListarAsync(int usuarioId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Una rutina del usuario. Una rutina ajena no existe para él, así que devuelve <c>null</c> y el
    /// controlador responde 404.
    /// </summary>
    Task<Rutina?> BuscarAsync(int id, int usuarioId, CancellationToken cancellationToken = default);

    /// <summary>Inserta la rutina activa y vacía, y devuelve el id que le asignó la base.</summary>
    Task<int> CrearAsync(Rutina rutina, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reescribe nombre y descripción sin tocar el estado ni los hábitos. Devuelve <c>false</c> cuando
    /// no hay ninguna rutina de ese usuario.
    /// </summary>
    Task<bool> ActualizarAsync(Rutina rutina, CancellationToken cancellationToken = default);

    /// <summary>
    /// Desactiva (<c>false</c>) o reactiva (<c>true</c>) la rutina sin tocar sus hábitos. Devuelve
    /// <c>false</c> cuando no hay ninguna rutina de ese usuario.
    /// </summary>
    Task<bool> CambiarActivoAsync(
        int id,
        int usuarioId,
        bool activo,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Los hábitos de la rutina, ordenados por el lugar que ocupan en ella. Si la rutina no es del
    /// usuario la lista sale vacía: el filtro por dueño viaja en el join.
    /// </summary>
    Task<IReadOnlyList<HabitoEnRutina>> ListarHabitosAsync(
        int rutinaId,
        int usuarioId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Si el hábito ya está en la rutina. Es lo que responde 409 con un `codigo` legible; el índice
    /// UNIQUE de la tabla sigue siendo la última línea por si dos altas se cruzan.
    /// </summary>
    Task<bool> ExisteHabitoAsync(
        int rutinaId,
        int habitoId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cuelga el hábito al final de la rutina y devuelve el lugar que le tocó. El `MAX(orden) + 1` se
    /// resuelve en la misma sentencia que inserta: como SELECT y después INSERT, dos altas simultáneas
    /// se pelearían el mismo lugar.
    /// </summary>
    Task<int> AgregarHabitoAsync(
        int rutinaId,
        int habitoId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saca el hábito de la rutina sin tocarlo: el hábito sigue activo y en sus otras rutinas. Devuelve
    /// <c>false</c> cuando no estaba, que deja el mismo estado igual.
    /// </summary>
    Task<bool> QuitarHabitoAsync(
        int rutinaId,
        int habitoId,
        CancellationToken cancellationToken = default);
}
