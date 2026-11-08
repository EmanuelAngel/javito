using Javito.Api.Historial;
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

    /// <summary>
    /// La fecha del primer registro del hábito, o <c>null</c> si nunca se marcó. Es la única evidencia
    /// de cuándo empezó: el esquema no guarda la fecha de alta del hábito (L5).
    /// </summary>
    /// <remarks>Un hábito ajeno se comporta como uno sin marcas: el <c>WHERE</c> filtra por dueño.</remarks>
    Task<DateOnly?> PrimerRegistroAsync(
        int habitoId,
        int usuarioId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Los registros del hábito en <c>[desde, hasta]</c>, con el día y si se cumplió: lo que necesita el
    /// detalle histórico para armar su cuadrícula.
    /// </summary>
    /// <remarks>
    /// Igual que el listado paginado, el <c>JOIN</c> con <c>habito</c> es lo que hace cumplir la
    /// propiedad —<c>registro_habito</c> no guarda el dueño— y no filtra por <c>activo</c>: el
    /// historial de un hábito archivado se consulta igual.
    /// </remarks>
    Task<IReadOnlyList<FilaDelHistorial>> ListarFilasAsync(
        int habitoId,
        int usuarioId,
        DateOnly desde,
        DateOnly hasta,
        CancellationToken cancellationToken = default);
}
