using Javito.Api.Modelos;

namespace Javito.Api.Repositorios;

/// <summary>
/// El historial de registros del usuario: lectura paginada, filtrable por hábito y por rango de
/// fechas. Es de sólo lectura; las marcas se escriben en el recurso del hábito.
/// </summary>
public interface IHistorialRepositorio
{
    /// <summary>
    /// Una página de los registros propios, del más nuevo al más viejo, con el total para paginar.
    /// </summary>
    /// <remarks>
    /// El <paramref name="usuarioId"/> filtra en el <c>WHERE</c>: un hábito ajeno no entra nunca, ni
    /// siquiera pidiéndolo por <paramref name="habitoId"/> —en ese caso la lista vuelve vacía—.
    /// Los hábitos archivados sí entran: su historial se conserva y sigue siendo consultable
    /// (HU-HAB-04).
    /// </remarks>
    /// <param name="desde">Primer día incluido, o <c>null</c> sin tope inferior.</param>
    /// <param name="hasta">Último día incluido, o <c>null</c> sin tope superior.</param>
    Task<(IReadOnlyList<RegistroDeHistorial> Elementos, int Total)> ListarAsync(
        int usuarioId,
        int? habitoId,
        DateOnly? desde,
        DateOnly? hasta,
        int limite,
        long desplazamiento,
        CancellationToken cancellationToken = default);
}
