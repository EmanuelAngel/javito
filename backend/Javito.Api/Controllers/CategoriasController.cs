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
        var nombre = entrada.Nombre.Trim();

        if (await repositorio.BuscarPorNombreAsync(nombre, cancellationToken) is not null)
        {
            return NombreRepetido(nombre);
        }

        var categoria = new Categoria
        {
            Nombre = nombre,
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
        var actual = await repositorio.BuscarAsync(id, cancellationToken);

        if (actual is null)
        {
            return NoEncontrada(id);
        }

        var nombre = entrada.Nombre.Trim();
        var conEseNombre = await repositorio.BuscarPorNombreAsync(nombre, cancellationToken);

        // Guardar la categoría con el nombre que ya tiene es válido: el choque es con otra fila.
        if (conEseNombre is not null && conEseNombre.Id != id)
        {
            return NombreRepetido(nombre);
        }

        var descripcion = NormalizarDescripcion(entrada.Descripcion);

        if (!await repositorio.ActualizarAsync(id, nombre, descripcion, cancellationToken))
        {
            return NoEncontrada(id);
        }

        return Ok(MapeoDeCategorias.ADto(new Categoria
        {
            Id = id,
            Nombre = nombre,
            Descripcion = descripcion,
            Activo = actual.Activo
        }));
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
        var actual = await repositorio.BuscarAsync(id, cancellationToken);

        if (actual is null)
        {
            return NoEncontrada(id);
        }

        var activo = entrada.Activo!.Value;

        if (!await repositorio.CambiarActivoAsync(id, activo, cancellationToken))
        {
            return NoEncontrada(id);
        }

        return Ok(MapeoDeCategorias.ADto(new Categoria
        {
            Id = id,
            Nombre = actual.Nombre,
            Descripcion = actual.Descripcion,
            Activo = activo
        }));
    }

    /// <summary>
    /// Los errores previstos se devuelven, no se lanzan: el `codigo` estable es lo que lee el SPA.
    /// </summary>
    private static ObjectResult NombreRepetido(string nombre) => RespuestasDeError.De(new ErrorDeNegocio(
        "nombre_repetido",
        "Nombre repetido",
        $"Ya existe una categoría con el nombre «{nombre}».",
        StatusCodes.Status409Conflict));

    private static ObjectResult NoEncontrada(int id) => RespuestasDeError.De(new ErrorDeNegocio(
        "no_encontrado",
        "Categoría inexistente",
        $"No hay ninguna categoría con id {id}.",
        StatusCodes.Status404NotFound));

    private static string? NormalizarDescripcion(string? descripcion) =>
        string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim();
}
