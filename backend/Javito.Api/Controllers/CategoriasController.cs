using Javito.Api.Contratos;
using Javito.Api.Repositorios;
using Microsoft.AspNetCore.Mvc;

namespace Javito.Api.Controllers;

/// <summary>
/// Listado de categorías. Ruta de humo de la fundación: CAT-01 la hereda con su [Authorize].
/// </summary>
[ApiController]
[Route("api/categorias")]
public class CategoriasController(CategoriaRepositorio repositorio) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CategoriaDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarAsync(
        [FromQuery] bool? activo,
        CancellationToken cancellationToken)
    {
        return Ok(await repositorio.ListarAsync(activo, cancellationToken));
    }
}
