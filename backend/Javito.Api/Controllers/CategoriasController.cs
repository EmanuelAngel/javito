using Javito.Api.Contratos;
using Javito.Api.Modelos;
using Javito.Api.Repositorios;
using Javito.Api.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Javito.Api.Controllers;

/// <summary>
/// Catálogo compartido de categorías. La lectura queda abierta a cualquier usuario autenticado (el
/// selector de hábitos la necesita); escribir en el catálogo es cosa de Administración.
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

    [HttpPost]
    [Authorize(Roles = Roles.Administrador)]
    [ProducesResponseType<CategoriaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CrearAsync(
        [FromBody] CategoriaEntradaDto entrada,
        CancellationToken cancellationToken)
    {
        if (NombreInvalido(entrada.Nombre) is { } invalido)
        {
            return invalido;
        }

        var categoria = new Categoria
        {
            Nombre = entrada.Nombre.Trim(),
            Descripcion = NormalizarDescripcion(entrada.Descripcion)
        };

        categoria.Id = await repositorio.CrearAsync(categoria, cancellationToken);

        return Created($"/api/categorias/{categoria.Id}", MapeoDeCategorias.ADto(categoria));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Administrador)]
    [ProducesResponseType<CategoriaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> EditarAsync(
        int id,
        [FromBody] CategoriaEntradaDto entrada,
        CancellationToken cancellationToken)
    {
        if (NombreInvalido(entrada.Nombre) is { } invalido)
        {
            return invalido;
        }

        var actualizada = await repositorio.ActualizarAsync(
            id,
            entrada.Nombre.Trim(),
            NormalizarDescripcion(entrada.Descripcion),
            cancellationToken);

        if (!actualizada)
        {
            throw NoEncontrada(id);
        }

        return Ok(MapeoDeCategorias.ADto(await BuscarAsync(id, cancellationToken)));
    }

    /// <summary>
    /// Baja lógica y reactivación: la fila nunca se borra, así los hábitos y artículos que la usan
    /// conservan su clasificación. Es idempotente a propósito: repetir el estado que ya tiene devuelve
    /// 200 y no 409, que queda reservado para el nombre repetido.
    /// </summary>
    [HttpPut("{id:int}/activo")]
    [Authorize(Roles = Roles.Administrador)]
    [ProducesResponseType<CategoriaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CambiarActivoAsync(
        int id,
        [FromBody] CambioDeActivoDto entrada,
        CancellationToken cancellationToken)
    {
        var cambiada = await repositorio.CambiarActivoAsync(
            id,
            entrada.Activo!.Value,
            cancellationToken);

        if (!cambiada)
        {
            throw NoEncontrada(id);
        }

        return Ok(MapeoDeCategorias.ADto(await BuscarAsync(id, cancellationToken)));
    }

    private async Task<Categoria> BuscarAsync(int id, CancellationToken cancellationToken) =>
        await repositorio.BuscarAsync(id, cancellationToken) ?? throw NoEncontrada(id);

    /// <summary>
    /// El nombre llega recortado a la base, así que uno de sólo espacios no es un nombre: se rechaza
    /// con el mismo 400 por campo que produce la validación del DTO.
    /// </summary>
    private IActionResult? NombreInvalido(string nombre)
    {
        if (!string.IsNullOrWhiteSpace(nombre))
        {
            return null;
        }

        ModelState.AddModelError(nameof(CategoriaEntradaDto.Nombre), "El nombre es obligatorio.");
        return ValidationProblem(ModelState);
    }

    private static string? NormalizarDescripcion(string? descripcion) =>
        string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim();

    private static ErrorDeNegocio NoEncontrada(int id) => new(
        "no_encontrado",
        "Categoría inexistente",
        $"No hay ninguna categoría con id {id}.",
        StatusCodes.Status404NotFound);
}
