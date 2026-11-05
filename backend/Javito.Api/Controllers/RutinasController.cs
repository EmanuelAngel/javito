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
public class RutinasController(IRutinaRepositorio repositorio, IHabitoRepositorio habitos) : ControllerBase
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
    /// Cuelga un hábito propio y activo al final de la rutina. El hábito se valida contra el dueño del
    /// token: uno ajeno responde igual que uno inexistente, y uno archivado se rechaza porque la
    /// historia pide elegirlo entre los activos.
    /// </summary>
    [HttpPost("{id:int}/habitos")]
    [ProducesResponseType<HabitoEnRutinaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AgregarHabitoAsync(
        int id,
        [FromBody] AgregarHabitoDto entrada,
        CancellationToken cancellationToken)
    {
        if (!ReclamosDeSesion.TryUsuarioId(User, out var usuarioId))
        {
            return RespuestasDeError.De(ErroresDeSesion.SesionIlegible());
        }

        if (await repositorio.BuscarAsync(id, usuarioId, cancellationToken) is null)
        {
            return NoEncontrada(id);
        }

        var habitoId = entrada.HabitoId!.Value;
        var habito = await habitos.BuscarAsync(habitoId, usuarioId, cancellationToken);

        if (habito is null)
        {
            return HabitoInexistente(habitoId);
        }

        if (!habito.Activo)
        {
            return HabitoArchivado(habito.Titulo);
        }

        if (await repositorio.ExisteHabitoAsync(id, habitoId, cancellationToken))
        {
            return HabitoRepetido(habito.Titulo);
        }

        var orden = await repositorio.AgregarHabitoAsync(id, habitoId, cancellationToken);

        return Created(
            $"/api/rutinas/{id}/habitos/{habitoId}",
            new HabitoEnRutinaDto(habito.Id, habito.Titulo, orden));
    }

    /// <summary>
    /// Saca el hábito de la rutina sin archivarlo (S8). Descolgar uno que no está es idempotente a
    /// propósito: deja el mismo estado y un doble toque no tiene por qué dar 404.
    /// </summary>
    [HttpDelete("{id:int}/habitos/{habitoId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> QuitarHabitoAsync(
        int id,
        int habitoId,
        CancellationToken cancellationToken)
    {
        if (!ReclamosDeSesion.TryUsuarioId(User, out var usuarioId))
        {
            return RespuestasDeError.De(ErroresDeSesion.SesionIlegible());
        }

        if (await repositorio.BuscarAsync(id, usuarioId, cancellationToken) is null)
        {
            return NoEncontrada(id);
        }

        await repositorio.QuitarHabitoAsync(id, habitoId, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Los errores previstos se devuelven, no se lanzan: el `codigo` estable es lo que lee el SPA.
    /// </summary>
    private static ObjectResult NoEncontrada(int id) => RespuestasDeError.De(new ErrorDeNegocio(
        "no_encontrado",
        "Rutina inexistente",
        $"No hay ninguna rutina con id {id} en tu cuenta.",
        StatusCodes.Status404NotFound));

    // El hábito que falla es el del cuerpo del alta, no una ruta inexistente: por eso 400, igual que
    // la categoría de un hábito nuevo.
    private static ObjectResult HabitoInexistente(int habitoId) => RespuestasDeError.De(new ErrorDeNegocio(
        "habito_inexistente",
        "Hábito inexistente",
        $"No hay ningún hábito con id {habitoId} en tu cuenta.",
        StatusCodes.Status400BadRequest));

    private static ObjectResult HabitoArchivado(string titulo) => RespuestasDeError.De(new ErrorDeNegocio(
        "habito_archivado",
        "Hábito archivado",
        $"«{titulo}» está archivado: reactivalo antes de agregarlo a una rutina.",
        StatusCodes.Status400BadRequest));

    private static ObjectResult HabitoRepetido(string titulo) => RespuestasDeError.De(new ErrorDeNegocio(
        "habito_ya_en_rutina",
        "Hábito repetido",
        $"«{titulo}» ya está en esta rutina.",
        StatusCodes.Status409Conflict));

    private static string? NormalizarDescripcion(string? descripcion) =>
        string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim();
}
