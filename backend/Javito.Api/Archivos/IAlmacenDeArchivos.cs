using Javito.Api.Contratos;

namespace Javito.Api.Archivos;

/// <summary>
/// Guarda y borra los archivos que suben los usuarios. Es genérico a propósito: el avatar es el
/// primer consumidor y la portada del artículo (<c>ART-01</c>) es el segundo. La base guarda la ruta
/// relativa web que devuelve <see cref="GuardarImagenAsync"/>, nunca el archivo.
/// </summary>
public interface IAlmacenDeArchivos
{
    /// <summary>
    /// Valida y guarda una imagen en <c>{raíz}/{subcarpeta}/{guid}.{ext}</c>.
    /// </summary>
    /// <param name="archivo">Archivo recibido por <c>multipart/form-data</c>.</param>
    /// <param name="subcarpeta">Carpeta destino, elegida por el servidor (por ejemplo «avatares»).</param>
    /// <returns>
    /// La ruta relativa web (<c>/uploads/avatares/{guid}.png</c>) o el rechazo con su código, que el
    /// controlador devuelve tal cual: acá no se lanza para los errores previstos.
    /// </returns>
    Task<ResultadoDeGuardado> GuardarImagenAsync(
        IFormFile archivo,
        string subcarpeta,
        CancellationToken cancellationToken = default);

    /// <summary>Borra el archivo de una ruta relativa web. Idempotente y silencioso.</summary>
    void Borrar(string? rutaRelativa);
}

/// <summary>Lo que devuelve el almacén: la ruta guardada o el error que explica el rechazo.</summary>
public sealed record ResultadoDeGuardado(string? Ruta, ErrorDeNegocio? Error)
{
    public static ResultadoDeGuardado Bien(string ruta) => new(ruta, null);

    public static ResultadoDeGuardado Rechazado(ErrorDeNegocio error) => new(null, error);
}
