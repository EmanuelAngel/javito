namespace Javito.Api.Archivos;

/// <summary>
/// Reconoce el formato de una imagen por su firma y no por el nombre ni por el <c>Content-Type</c>
/// que manda el cliente: los dos los controla quien sube. La extensión con la que se guarda el
/// archivo sale de acá, así que un HTML con nombre <c>.png</c> nunca llega al disco.
/// </summary>
public static class FirmasDeImagen
{
    /// <summary>Bytes que hay que leer para distinguir los tres formatos aceptados.</summary>
    public const int LargoDeCabecera = 12;

    private static readonly byte[] FirmaPng = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static readonly byte[] FirmaJpeg = [0xFF, 0xD8, 0xFF];

    /// <summary>La extensión canónica del formato, o <c>null</c> si la cabecera no es de una imagen.</summary>
    public static string? ExtensionDe(ReadOnlySpan<byte> cabecera)
    {
        // Una cabecera incompleta no identifica nada: con menos bytes no hay imagen real detrás.
        if (cabecera.Length < LargoDeCabecera)
        {
            return null;
        }

        if (cabecera.StartsWith(FirmaPng))
        {
            return ".png";
        }

        if (cabecera.StartsWith(FirmaJpeg))
        {
            return ".jpg";
        }

        // WebP es un contenedor RIFF: "RIFF" + tamaño + "WEBP".
        return cabecera[..4].SequenceEqual("RIFF"u8) && cabecera[8..12].SequenceEqual("WEBP"u8)
            ? ".webp"
            : null;
    }
}
