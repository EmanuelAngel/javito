using Javito.Api.Contratos;
using Javito.Api.Modelos;
using Javito.Api.Repositorios;
using Javito.Api.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Javito.Api.Controllers;

/// <summary>
/// Rutinas del usuario autenticado. El dueño sale siempre del token, nunca del cuerpo ni de la ruta:
/// una rutina ajena se comporta como inexistente.
/// </summary>
[ApiController]
[Authorize]
[Route("api/rutinas")]
public class RutinasController(IRutinaRepositorio repositorio) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<RutinaDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListarAsync(CancellationToken cancellationToken)
    {
        if (!ReclamosDeSesion.TryUsuarioId(User, out var usuarioId))
        {
            return RespuestasDeError.De(ErroresDeSesion.SesionIlegible());
        }

        var rutinas = await repositorio.ListarAsync(usuarioId, cancellationToken);

        return Ok(rutinas.Select(MapeoDeRutinas.ADto).ToList());
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<RutinaDetalleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DetalleAsync(int id, CancellationToken cancellationToken)
    {
        if (!ReclamosDeSesion.TryUsuarioId(User, out var usuarioId))
        {
            return RespuestasDeError.De(ErroresDeSesion.SesionIlegible());
        }

        var rutina = await repositorio.BuscarAsync(id, usuarioId, cancellationToken);

        if (rutina is null)
        {
            return NoEncontrada(id);
        }

        var habitos = await repositorio.ListarHabitosAsync(id, usuarioId, cancellationToken);

        return Ok(MapeoDeRutinas.ADetalleDto(rutina, habitos));
    }

    /// <summary>
    /// El alta no valida nombres repetidos: la historia pide poder tener varias rutinas, y agregar una
    /// unicidad que nadie pidió sería una regla inventada.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<RutinaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CrearAsync(
        [FromBody] RutinaEntradaDto entrada,
        CancellationToken cancellationToken)
    {
        if (!ReclamosDeSesion.TryUsuarioId(User, out var usuarioId))
        {
            return RespuestasDeError.De(ErroresDeSesion.SesionIlegible());
        }

        var rutina = new Rutina
        {
            UsuarioId = usuarioId,
            Nombre = entrada.Nombre.Trim(),
            Descripcion = NormalizarDescripcion(entrada.Descripcion)
        };

        rutina.Id = await repositorio.CrearAsync(rutina, cancellationToken);

        return Created($"/api/rutinas/{rutina.Id}", MapeoDeRutinas.ADto(rutina));
    }

    /// <summary>
    /// Los errores previstos se devuelven, no se lanzan: el `codigo` estable es lo que lee el SPA.
    /// </summary>
    private static ObjectResult NoEncontrada(int id) => RespuestasDeError.De(new ErrorDeNegocio(
        "no_encontrado",
        "Rutina inexistente",
        $"No hay ninguna rutina con id {id} en tu cuenta.",
        StatusCodes.Status404NotFound));

    private static string? NormalizarDescripcion(string? descripcion) =>
        string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim();
}
