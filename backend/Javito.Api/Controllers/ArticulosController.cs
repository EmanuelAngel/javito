using Javito.Api.Archivos;
using Javito.Api.Contratos;
using Javito.Api.Modelos;
using Javito.Api.Paginacion;
using Javito.Api.Repositorios;
using Javito.Api.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Javito.Api.Controllers;

/// <summary>
/// Artículos del blog. Leer requiere sesión —la propuesta aprobada no tiene catálogo público— y
/// publicar y editar son exclusivos de Administración.
/// </summary>
[ApiController]
[Authorize]
[Route("api/articulos")]
public class ArticulosController(
    IArticuloRepositorio articulos,
    ICategoriaRepositorio categorias,
    IAlmacenDeArchivos almacen) : ControllerBase
{
    /// <summary>Subcarpeta del almacén. La elige el servidor, nunca el cliente.</summary>
    private const string SubcarpetaDePortadas = "articulos";

    /// <summary>
    /// Publica un artículo. La portada es opcional: sin archivo se publica igual. El orden de los
    /// pasos no es casual —validar la categoría primero deja el disco limpio cuando la entrada no
    /// sirve, y borrar la imagen si el INSERT falla evita que un alta fallida deje huérfanas.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = Roles.Administrador)]
    [ProducesResponseType<ArticuloDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    public async Task<IActionResult> CrearAsync(
        [FromForm] ArticuloEntradaDto entrada,
        [FromForm] IFormFile? imagenPortada,
        CancellationToken cancellationToken)
    {
        if (!ReclamosDeSesion.TryUsuarioId(User, out var autorId))
        {
            return RespuestasDeError.De(ErroresDeSesion.SesionIlegible());
        }

        // La categoría tiene que existir y estar activa: el desplegable sólo ofrece activas, y el
        // servidor no confía en que el cliente haya mandado una de ellas.
        var categoria = await categorias.BuscarAsync(entrada.CategoriaId, cancellationToken);

        if (categoria is null || !categoria.Activo)
        {
            return CategoriaInvalida(entrada.CategoriaId);
        }

        string? portada = null;

        if (imagenPortada is not null)
        {
            var guardado = await almacen.GuardarImagenAsync(imagenPortada, SubcarpetaDePortadas, cancellationToken);

            if (guardado.Error is not null)
            {
                return RespuestasDeError.De(guardado.Error);
            }

            portada = guardado.Ruta;
        }

        var articulo = new Articulo
        {
            UsuarioId = autorId,
            CategoriaId = categoria.Id,
            Titulo = entrada.Titulo!.Trim(),
            Resumen = entrada.Resumen!.Trim(),
            ContenidoMarkdown = entrada.ContenidoMarkdown!,
            ImagenPortadaUrl = portada,
            Activo = true
        };

        try
        {
            articulo.Id = await articulos.CrearAsync(articulo, cancellationToken);
        }
        catch
        {
            // El alta no entró: la portada que acabamos de guardar no le sirve a ninguna fila.
            almacen.Borrar(portada);
            throw;
        }

        // Se relee la fila en vez de armar el DTO con lo que mandó el cliente: así la respuesta trae
        // la fecha que puso la base y queda probado que lo publicado es lo que se lee.
        var releido = await articulos.BuscarAsync(articulo.Id, cancellationToken);

        if (releido is null)
        {
            throw new InvalidOperationException($"No se pudo releer el artículo {articulo.Id} recién creado.");
        }

        return Created($"/api/articulos/{articulo.Id}", MapeoDeArticulos.ADto(releido));
    }

    /// <summary>
    /// Corrige un artículo publicado (HU-ART-02) sin despublicarlo: la fecha de publicación y
    /// <c>activo</c> no cambian. La portada se conserva si no llega archivo, o se reemplaza; no se
    /// quita. El reemplazo es el del avatar: guardar la nueva, actualizar la fila y recién ahí borrar
    /// la anterior. Si la fila no acepta la nueva, se borra la nueva y queda la anterior.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Administrador)]
    [ProducesResponseType<ArticuloDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    public async Task<IActionResult> EditarAsync(
        int id,
        [FromForm] ArticuloEntradaDto entrada,
        [FromForm] IFormFile? imagenPortada,
        CancellationToken cancellationToken)
    {
        // Un artículo retirado no se corrige: primero se reactiva (HU-ART-03). Para la edición, igual
        // que para la lectura, no existe.
        var existente = await articulos.BuscarAsync(id, cancellationToken);

        if (existente is null || !existente.Activo)
        {
            return NoEncontrado(id);
        }

        // Conservar la categoría que ya tenía vale aunque se haya desactivado después; cambiar a otra
        // exige una activa. Se valida antes de tocar el disco, igual que en el alta.
        if (entrada.CategoriaId != existente.CategoriaId)
        {
            var categoria = await categorias.BuscarAsync(entrada.CategoriaId, cancellationToken);

            if (categoria is null || !categoria.Activo)
            {
                return CategoriaInvalida(entrada.CategoriaId);
            }
        }

        var anterior = existente.ImagenPortadaUrl;
        string? nueva = null;

        if (imagenPortada is not null)
        {
            var guardado = await almacen.GuardarImagenAsync(imagenPortada, SubcarpetaDePortadas, cancellationToken);

            if (guardado.Error is not null)
            {
                return RespuestasDeError.De(guardado.Error);
            }

            nueva = guardado.Ruta;
        }

        existente.CategoriaId = entrada.CategoriaId;
        existente.Titulo = entrada.Titulo!.Trim();
        existente.Resumen = entrada.Resumen!.Trim();
        existente.ContenidoMarkdown = entrada.ContenidoMarkdown!;
        existente.ImagenPortadaUrl = nueva ?? anterior;

        bool actualizado;

        try
        {
            actualizado = await articulos.ActualizarAsync(existente, cancellationToken);
        }
        catch
        {
            // La fila no aceptó la portada nueva: sigue apuntando a la anterior, que queda en disco.
            almacen.Borrar(nueva);
            throw;
        }

        if (!actualizado)
        {
            // Lo dieron de baja entre la lectura y el UPDATE: nada apunta a la portada nueva.
            almacen.Borrar(nueva);
            return NoEncontrado(id);
        }

        // Recién con la fila apuntando a la nueva, la anterior no le sirve a nadie.
        if (nueva is not null)
        {
            almacen.Borrar(anterior);
        }

        var releido = await articulos.BuscarAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"No se pudo releer el artículo {id} recién editado.");

        return Ok(MapeoDeArticulos.ADto(releido));
    }

    /// <summary>
    /// El catálogo: sólo artículos activos, del más nuevo al más viejo, paginado y opcionalmente
    /// filtrado por categoría. Una página pasada del final devuelve <c>elementos</c> vacío con la
    /// metadata tal como se pidió: corregirla en silencio escondería que el cliente se equivocó.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<PaginaDto<ArticuloDelCatalogoDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListarAsync(
        [FromQuery] ParametrosDePagina parametros,
        [FromQuery] int? categoriaId,
        CancellationToken cancellationToken)
    {
        var (elementos, total) = await articulos.ListarCatalogoAsync(
            categoriaId,
            parametros.TamanioDePagina,
            parametros.Desplazamiento,
            cancellationToken);

        var dtos = elementos.Select(MapeoDeArticulos.ADelCatalogo).ToList();

        return Ok(PaginaDto<ArticuloDelCatalogoDto>.De(dtos, parametros, total));
    }

    /// <summary>
    /// Un artículo por id. Un artículo retirado responde 404 y no 403: para el lector no existe, y
    /// así la baja lógica de HU-ART-03 no se puede saltear escribiendo la URL.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<ArticuloDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerAsync(int id, CancellationToken cancellationToken)
    {
        var articulo = await articulos.BuscarAsync(id, cancellationToken);

        if (articulo is null || !articulo.Activo)
        {
            return NoEncontrado(id);
        }

        return Ok(MapeoDeArticulos.ADto(articulo));
    }

    /// <summary>
    /// Los errores previstos se devuelven, no se lanzan: el `codigo` estable es lo que lee el SPA.
    /// </summary>
    private static ObjectResult CategoriaInvalida(int id) => RespuestasDeError.De(new ErrorDeNegocio(
        "categoria_invalida",
        "Categoría inválida",
        $"No hay ninguna categoría activa con id {id}.",
        StatusCodes.Status400BadRequest));

    private static ObjectResult NoEncontrado(int id) => RespuestasDeError.De(new ErrorDeNegocio(
        "no_encontrado",
        "Artículo inexistente",
        $"No hay ningún artículo con id {id}.",
        StatusCodes.Status404NotFound));
}
