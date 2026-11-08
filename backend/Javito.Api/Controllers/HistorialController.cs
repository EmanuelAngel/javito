using Javito.Api.Contratos;
using Javito.Api.Paginacion;
using Javito.Api.Repositorios;
using Javito.Api.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Javito.Api.Controllers;

/// <summary>
/// El historial del usuario: sus registros paginados en el servidor, filtrables por hábito y por
/// rango de fechas, para revisar un período concreto sin traer años de datos (HU-HIS-01). Es de sólo
/// lectura; las marcas se escriben en el recurso del hábito.
/// </summary>
[ApiController]
[Authorize]
[Route("api/historial")]
public class HistorialController(IHistorialRepositorio historial) : ControllerBase
{
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
}
