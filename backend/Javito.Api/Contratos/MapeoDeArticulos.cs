using System.Globalization;
using Javito.Api.Modelos;

namespace Javito.Api.Contratos;

/// <summary>Proyección del artículo a lo que viaja por HTTP.</summary>
public static class MapeoDeArticulos
{
    public static ArticuloDto ADto(Articulo articulo) => new(
        articulo.Id,
        articulo.CategoriaId,
        articulo.Titulo,
        articulo.Resumen,
        articulo.ContenidoMarkdown,
        articulo.ImagenPortadaUrl,
        // Invariante: el formato del contrato no depende del idioma de la máquina que lo serializa.
        articulo.FechaPublicacion.ToString(FechaIso.Formato, CultureInfo.InvariantCulture),
        articulo.Activo);

    /// <summary>La misma proyección sin el cuerpo, para las tarjetas del catálogo.</summary>
    public static ArticuloDelCatalogoDto ADelCatalogo(Articulo articulo) => new(
        articulo.Id,
        articulo.CategoriaId,
        articulo.Titulo,
        articulo.Resumen,
        articulo.ImagenPortadaUrl,
        articulo.FechaPublicacion.ToString(FechaIso.Formato, CultureInfo.InvariantCulture),
        articulo.Activo);
}
