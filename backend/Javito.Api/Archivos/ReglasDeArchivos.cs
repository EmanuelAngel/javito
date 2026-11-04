using Javito.Api.Contratos;

namespace Javito.Api.Archivos;

/// <summary>
/// Las reglas de una imagen subida y los errores con los que se rechaza. Viven acá y no en el
/// controlador porque el almacén las aplica, el controlador las devuelve y el manejador del cuerpo
/// demasiado grande repite la misma respuesta: tres lugares, un solo texto y un solo <c>codigo</c>.
/// </summary>
public static class ReglasDeArchivos
{
    /// <summary>Tope de la imagen: 5 MB, el mismo que usa el antecedente de la Inmobiliaria.</summary>
    public const long TamanoMaximoEnBytes = 5 * 1024 * 1024;

    /// <summary>
    /// Tope del cuerpo <c>multipart</c>, alineado con <c>MaxRequestBodySize</c> de Kestrel (30 MB, su
    /// valor por defecto). Así, para todo cuerpo que el servidor acepte, el rechazo por tamaño lo da
    /// la validación del almacén con su mensaje; por encima de eso responde Kestrel y la API ni se
    /// entera. Con el valor por defecto de <c>FormOptions</c> (128 MB) el corte caería en otro lado
    /// que el de Kestrel y el mensaje se volvería inalcanzable.
    /// </summary>
    public const long TamanoMaximoDelCuerpoEnBytes = 30 * 1024 * 1024;

    public static ErrorDeNegocio Faltante() => new(
        "archivo_requerido",
        "Falta el archivo",
        "Elegí una imagen para subir.",
        StatusCodes.Status400BadRequest);

    public static ErrorDeNegocio Vacio() => new(
        "archivo_vacio",
        "El archivo está vacío",
        "El archivo llegó sin contenido. Elegí otra imagen.",
        StatusCodes.Status400BadRequest);

    public static ErrorDeNegocio NoEsImagen() => new(
        "archivo_no_es_imagen",
        "El archivo no es una imagen",
        "Solo se aceptan imágenes JPG, PNG o WebP.",
        StatusCodes.Status400BadRequest);

    public static ErrorDeNegocio DemasiadoGrande() => new(
        "archivo_demasiado_grande",
        "La imagen es demasiado grande",
        $"La imagen no puede superar los {TamanoMaximoEnBytes / (1024 * 1024)} MB.",
        StatusCodes.Status413PayloadTooLarge);
}
