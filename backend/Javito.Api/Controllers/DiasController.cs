using Javito.Api.Contratos;
using Javito.Api.Repositorios;
using Javito.Api.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Javito.Api.Controllers;

/// <summary>
/// El día del usuario: proyección de sólo lectura sobre sus hábitos activos, con la marca de la fecha
/// pedida y el progreso de la semana. La fecha la manda el cliente porque es el único que sabe qué día
/// es en su huso horario; el servidor corre en UTC.
/// </summary>
[ApiController]
[Authorize]
[Route("api/dias")]
public class DiasController(IDiaRepositorio repositorio) : ControllerBase
{
    [HttpGet("{fecha}")]
    [ProducesResponseType<DiaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DiaAsync(string fecha, CancellationToken cancellationToken)
    {
        if (!FechaIso.TryParsear(fecha, out var dia))
        {
            return RespuestasDeError.De(FechaIso.ErrorDeFormato(fecha));
        }

        if (!ReclamosDeSesion.TryUsuarioId(User, out var usuarioId))
        {
            return RespuestasDeError.De(new ErrorDeNegocio(
                "no_autenticado",
                "Sesión requerida",
                "El token no identifica a ningún usuario. Volvé a iniciar sesión.",
                StatusCodes.Status401Unauthorized));
        }

        var habitos = await repositorio.ListarAsync(usuarioId, dia, cancellationToken);

        return Ok(MapeoDeDias.ADto(dia, habitos));
    }
}
