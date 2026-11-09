using Javito.Api.Contratos;
using Javito.Api.Paginacion;
using Javito.Api.Repositorios;
using Javito.Api.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Javito.Api.Controllers;

/// <summary>
/// USU-01 (HU-USU-01): el listado de cuentas que ve el administrador. Todo el controlador es de
/// administración, así que la escritura y la lectura llevan el rol en la acción: la sesión sola no
/// alcanza. Un Usuario autenticado recibe 403 y la fila no se toca.
/// </summary>
[ApiController]
[Authorize]
[Route("api/usuarios")]
public sealed class UsuariosController(IUsuarioRepositorio usuarios) : ControllerBase
{
    /// <summary>
    /// Una página de cuentas, con el total para el paginador. Sin <paramref name="q"/> ni
    /// <paramref name="activo"/> trae todo: activos y desactivados, que es lo que la pantalla separa
    /// con el filtro de estado.
    /// </summary>
    /// <param name="q">
    /// Pedazo del nombre o del email. Los comodines de LIKE se buscan literales: «%» no trae todo.
    /// </param>
    [HttpGet]
    [Authorize(Roles = Roles.Administrador)]
    [ProducesResponseType<PaginaDto<UsuarioAdministradoDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListarAsync(
        [FromQuery] ParametrosDePagina parametros,
        [FromQuery] bool? activo,
        [FromQuery] string? q,
        CancellationToken cancellationToken)
    {
        var (elementos, total) = await usuarios.ListarAsync(
            activo,
            q,
            parametros.TamanioDePagina,
            parametros.Desplazamiento,
            cancellationToken);

        var dtos = elementos.Select(MapeoDeUsuarios.AAdministrado).ToList();

        return Ok(PaginaDto<UsuarioAdministradoDto>.De(dtos, parametros, total));
    }
}
