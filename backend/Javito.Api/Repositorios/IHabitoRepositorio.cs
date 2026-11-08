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
    /// Los hábitos del usuario: los activos primero y cada grupo por título. Sin filtro vienen los
    /// dos, que es lo que necesita la pantalla para separarlos sin pedir dos veces.
    /// </summary>
    /// <param name="q">
    /// Pedazo del título, tal como lo escribe la persona. Vacío o en blanco no filtra. `%`, `_` y la
    /// barra se buscan literales: son comodines de LIKE, no texto. Con texto, además, sólo entran los
    /// activos: la búsqueda existe para agregar un hábito a una rutina, y uno archivado no se agrega
    /// (HU-RUT-02), así que pedir `q` junto a `activo=false` devuelve vacío a propósito.
    /// </param>
    Task<IReadOnlyList<Habito>> ListarAsync(
        int usuarioId,
        bool? activo,
        string? q,
        CancellationToken cancellationToken = default);

    /// <summary>Inserta el hábito y devuelve el id que le asignó la base.</summary>
    Task<int> CrearAsync(Habito habito, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reescribe los campos del hábito —los mismos que pone el alta— sin tocar sus registros ni su
    /// estado de archivo. Devuelve <c>false</c> cuando no hay ninguna fila de ese usuario.
    /// </summary>
    Task<bool> ActualizarAsync(Habito habito, CancellationToken cancellationToken = default);

    /// <summary>
    /// Archiva (<c>false</c>) o reactiva (<c>true</c>) un hábito del usuario sin tocar sus registros.
    /// Devuelve <c>false</c> cuando no hay ninguna fila de ese usuario.
    /// </summary>
    Task<bool> CambiarActivoAsync(
        int id,
        int usuarioId,
        bool activo,
        CancellationToken cancellationToken = default);

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
