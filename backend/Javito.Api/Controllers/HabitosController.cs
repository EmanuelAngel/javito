using Javito.Api.Contratos;
using Javito.Api.Modelos;
using Javito.Api.Repositorios;
using Javito.Api.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Javito.Api.Controllers;

/// <summary>
/// Los hábitos del usuario y sus marcas. El registro es un recurso con identidad (hábito, fecha), así
/// que se escribe con PUT —crea o reemplaza, sin contar deltas— y se desmarca con DELETE, porque «sin
/// marcar» es la ausencia de la fila.
/// </summary>
[ApiController]
[Authorize]
[Route("api/habitos")]
public class HabitosController(IHabitoRepositorio repositorio) : ControllerBase
{
    /// <summary>
    /// El valor más alto que admite la columna: DECIMAL(8, 2).
    /// </summary>
    private const decimal ValorMaximo = 999999.99m;

    [HttpPut("{id:int}/registros/{fecha}")]
    [ProducesResponseType<RegistroDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarcarAsync(
        int id,
        string fecha,
        [FromBody] RegistroEntradaDto entrada,
        CancellationToken cancellationToken)
    {
        var (dia, habito, error) = await PrepararAsync(id, fecha, cancellationToken);

        if (error is not null)
        {
            return error;
        }

        var valor = entrada.Valor!.Value;

        if (valor < 0 || valor > ValorMaximo || (EsBinario(habito!.Tipo) && valor != 1))
        {
            return RespuestasDeError.De(ValorFueraDeRango(habito.Tipo));
        }

        // La meta se copia del hábito y el completado se deriva acá: el cliente manda un solo número.
        var metaObjetivo = habito.Meta ?? 1m;

        var registro = await repositorio.GuardarRegistroAsync(
            new Registro
            {
                HabitoId = id,
                Fecha = dia!.Value,
                ValorAlcanzado = valor,
                MetaObjetivo = metaObjetivo,
                Completado = valor >= metaObjetivo
            },
            cancellationToken);

        return Ok(MapeoDeRegistros.ADto(registro));
    }

    [HttpDelete("{id:int}/registros/{fecha}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DesmarcarAsync(
        int id,
        string fecha,
        CancellationToken cancellationToken)
    {
        var (dia, _, error) = await PrepararAsync(id, fecha, cancellationToken);

        if (error is not null)
        {
            return error;
        }

        await repositorio.BorrarRegistroAsync(id, dia!.Value, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Las tres condiciones que comparten marcar y desmarcar: fecha legible y en rango, hábito propio y
    /// hábito activo. Devuelve las tres cosas en una tupla porque no se puede usar <c>out</c> con async.
    /// </summary>
    private async Task<(DateOnly? Dia, Habito? Habito, IActionResult? Error)> PrepararAsync(
        int id,
        string fecha,
        CancellationToken cancellationToken)
    {
        if (!FechaIso.TryParsear(fecha, out var dia))
        {
            return (null, null, RespuestasDeError.De(FechaIso.ErrorDeFormato(fecha)));
        }

        if (!FechaIso.EstaEnRango(dia))
        {
            return (null, null, RespuestasDeError.De(FechaIso.ErrorDeRango(dia)));
        }

        if (!ReclamosDeSesion.TryUsuarioId(User, out var usuarioId))
        {
            return (null, null, RespuestasDeError.De(SesionIlegible()));
        }

        var habito = await repositorio.BuscarAsync(id, usuarioId, cancellationToken);

        if (habito is null)
        {
            return (null, null, NoEncontrado(id));
        }

        if (!habito.Activo)
        {
            return (null, null, RespuestasDeError.De(new ErrorDeNegocio(
                "habito_archivado",
                "Hábito archivado",
                $"«{habito.Titulo}» está archivado: reactivalo antes de marcarlo.",
                StatusCodes.Status400BadRequest)));
        }

        return (dia, habito, null);
    }

    private static bool EsBinario(string tipo) =>
        tipo is TiposDeHabito.SiNo or TiposDeHabito.Abstinencia;

    private static ErrorDeNegocio ValorFueraDeRango(string tipo) => new(
        "valor_fuera_de_rango",
        "Valor fuera de rango",
        EsBinario(tipo)
            ? $"Un hábito «{tipo}» se marca con valor 1: para desmarcarlo se borra la marca."
            : "El valor tiene que ir de 0 a 999999,99.",
        StatusCodes.Status400BadRequest);

    private static ErrorDeNegocio SesionIlegible() => new(
        "no_autenticado",
        "Sesión requerida",
        "El token no identifica a ningún usuario. Volvé a iniciar sesión.",
        StatusCodes.Status401Unauthorized);

    private static ObjectResult NoEncontrado(int id) => RespuestasDeError.De(new ErrorDeNegocio(
        "no_encontrado",
        "Hábito inexistente",
        $"No hay ningún hábito con id {id} en tu cuenta.",
        StatusCodes.Status404NotFound));
}
