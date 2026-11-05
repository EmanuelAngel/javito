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
    /// Los hábitos de la rutina, ordenados por el lugar que ocupan en ella. Si la rutina no es del
    /// usuario la lista sale vacía: el filtro por dueño viaja en el join.
    /// </summary>
    Task<IReadOnlyList<HabitoEnRutina>> ListarHabitosAsync(
        int rutinaId,
        int usuarioId,
        CancellationToken cancellationToken = default);
}
