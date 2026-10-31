using Javito.Api.Contratos;
using Javito.Api.Repositorios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Javito.Api.Controllers;

/// <summary>
/// Listado de categorías. Exige token desde ACC-02; CAT-01 le suma la restricción por rol.
/// </summary>
[ApiController]
[Authorize]
[Route("api/categorias")]
public class CategoriasController(ICategoriaRepositorio repositorio) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CategoriaDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarAsync(
        [FromQuery] bool? activo,
        CancellationToken cancellationToken)
    {
        var categorias = await repositorio.ListarAsync(activo, cancellationToken);

        return Ok(categorias.Select(MapeoDeCategorias.ADto).ToList());
    }
}
