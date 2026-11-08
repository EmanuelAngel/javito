using Javito.Api.Contratos;
using Javito.Api.Historial;
using Javito.Api.Paginacion;
using Javito.Api.Repositorios;
using Javito.Api.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Javito.Api.Controllers;

/// <summary>
/// El historial del usuario: sus registros paginados en el servidor, filtrables por hábito y por
/// rango de fechas, para revisar un período concreto sin traer años de datos (HU-HIS-01), y el detalle
/// histórico de un hábito, con sus totales y su cuadrícula (HU-HIS-02). Es de sólo lectura; las marcas
/// se escriben en el recurso del hábito.
/// </summary>
[ApiController]
[Authorize]
[Route("api/historial")]
public class HistorialController(
    IHistorialRepositorio historial,
    IHabitoRepositorio habitos) : ControllerBase
{
    /// <summary>Los únicos períodos del detalle: la pantalla elige entre el mes, el bimestre y el año.</summary>
    private static readonly int[] PeriodosDelDetalle = [30, 60, 365];

    /// <summary>
    /// Una página del historial, del día más nuevo al más viejo. Los filtros ausentes o vacíos no
    /// filtran: sin <c>desde</c> no hay tope inferior. Una página pasada del final devuelve
    /// <c>elementos</c> vacío con la metadata tal como se pidió.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<PaginaDto<RegistroDeHistorialDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListarAsync(
        [FromQuery] ParametrosDePagina parametros,
        [FromQuery] int? habitoId,
        [FromQuery] string? desde,
        [FromQuery] string? hasta,
        CancellationToken cancellationToken)
    {
        // Las fechas llegan como texto para que el 400 tenga el mismo `codigo` que el resto de la API
        // en vez del error de binding, que no lo lleva.
        if (!TryLeerFecha(desde, out var fechaDesde))
        {
            return RespuestasDeError.De(FechaIso.ErrorDeFormato(desde!));
        }

        if (!TryLeerFecha(hasta, out var fechaHasta))
        {
            return RespuestasDeError.De(FechaIso.ErrorDeFormato(hasta!));
        }

        if (fechaDesde > fechaHasta)
        {
            return RespuestasDeError.De(RangoInvertido(fechaDesde!.Value, fechaHasta!.Value));
        }

        if (!ReclamosDeSesion.TryUsuarioId(User, out var usuarioId))
        {
            return RespuestasDeError.De(ErroresDeSesion.SesionIlegible());
        }

        var (elementos, total) = await historial.ListarAsync(
            usuarioId,
            habitoId,
            fechaDesde,
            fechaHasta,
            parametros.TamanioDePagina,
            parametros.Desplazamiento,
            cancellationToken);

        var dtos = elementos.Select(MapeoDeHistorial.ADto).ToList();

        return Ok(PaginaDto<RegistroDeHistorialDto>.De(dtos, parametros, total));
    }

    /// <summary>
    /// El detalle histórico de un hábito (HU-HIS-02, L8): la ventana de 30, 60 o 365 días que termina
    /// en <c>hasta</c>, con los totales del período y el estado de cada día.
    /// </summary>
    /// <remarks>
    /// La ruta es absoluta a propósito. El módulo del historial vive en este controlador y el recurso
    /// cuelga del hábito, así que sin el <c>~/</c> la acción quedaría colgada de <c>api/historial</c> y
    /// no de <c>api/habitos/{id}</c>, que es el recurso que pide el SPA.
    /// </remarks>
    /// <param name="dias">Los únicos períodos válidos: 30, 60 o 365. Cualquier otro devuelve 400.</param>
    /// <param name="hasta">
    /// Último día de la ventana, ambos extremos incluidos. Ausente o vacío es hoy en UTC: el día del
    /// cliente viaja explícito porque el servidor no sabe en qué huso horario vive quien mira.
    /// </param>
    [HttpGet("~/api/habitos/{id:int}/historial")]
    [ProducesResponseType<HistorialDeHabitoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DetalleAsync(
        int id,
        [FromQuery] int dias = 30,
        [FromQuery] string? hasta = null,
        CancellationToken cancellationToken = default)
    {
        // El período se valida primero: sin uno conocido no hay ventana que calcular.
        if (!PeriodosDelDetalle.Contains(dias))
        {
            return RespuestasDeError.De(PeriodoInvalido(dias));
        }

        if (!TryLeerHasta(hasta, out var fechaHasta))
        {
            return RespuestasDeError.De(FechaIso.ErrorDeFormato(hasta!));
        }

        if (!ReclamosDeSesion.TryUsuarioId(User, out var usuarioId))
        {
            return RespuestasDeError.De(ErroresDeSesion.SesionIlegible());
        }

        // La pertenencia se pregunta antes de leer: un hábito ajeno no existe para quien pregunta.
        var habito = await habitos.BuscarAsync(id, usuarioId, cancellationToken);

        if (habito is null)
        {
            return NoEncontrado(id);
        }

        var desde = fechaHasta.AddDays(-(dias - 1));
        var primerRegistro = await historial.PrimerRegistroAsync(id, usuarioId, cancellationToken);
        var filas = await historial.ListarFilasAsync(id, usuarioId, desde, fechaHasta, cancellationToken);

        return Ok(CuadriculaDelHistorial.Componer(habito, desde, fechaHasta, primerRegistro, filas));
    }

    /// <summary>Sin valor no hay filtro: el parámetro vacío de la query es la ausencia, no un cero.</summary>
    private static bool TryLeerFecha(string? valor, out DateOnly? fecha)
    {
        fecha = null;

        if (string.IsNullOrWhiteSpace(valor))
        {
            return true;
        }

        if (!FechaIso.TryParsear(valor, out var leida))
        {
            return false;
        }

        fecha = leida;
        return true;
    }

    private static ErrorDeNegocio RangoInvertido(DateOnly desde, DateOnly hasta) => new(
        "rango_de_fechas_invalido",
        "Rango de fechas inválido",
        $"El historial no puede empezar el {desde:yyyy-MM-dd} y terminar antes, el {hasta:yyyy-MM-dd}.",
        StatusCodes.Status400BadRequest);

    /// <summary>Sólo se aceptan los tres períodos de la pantalla; los demás se rechazan con su código.</summary>
    private static ErrorDeNegocio PeriodoInvalido(int dias) => new(
        "dias_invalidos",
        "Período inválido",
        $"El período tiene que ser {string.Join(", ", PeriodosDelDetalle[..^1])} o {PeriodosDelDetalle[^1]} " +
        $"días: «{dias}» no es uno de ellos.",
        StatusCodes.Status400BadRequest);

    /// <summary>Vacío o ausente es hoy en UTC, como en <c>/api/dias</c>: el día lo manda el cliente.</summary>
    private static bool TryLeerHasta(string? valor, out DateOnly fecha)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            fecha = DateOnly.FromDateTime(DateTime.UtcNow);
            return true;
        }

        return FechaIso.TryParsear(valor, out fecha);
    }

    /// <summary>El mismo 404 que usa <c>HabitosController</c>: «no existe» y «no es tuyo» se responden igual.</summary>
    private static ObjectResult NoEncontrado(int id) => RespuestasDeError.De(new ErrorDeNegocio(
        "no_encontrado",
        "Hábito inexistente",
        $"No hay ningún hábito con id {id} en tu cuenta.",
        StatusCodes.Status404NotFound));
}
