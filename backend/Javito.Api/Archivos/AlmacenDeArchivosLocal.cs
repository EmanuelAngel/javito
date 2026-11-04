namespace Javito.Api.Archivos;

/// <summary>
/// Almacén en disco: los archivos viven en <c>{raíz}/{subcarpeta}</c> y se sirven como estáticos en
/// <c>/uploads</c> (ver <c>Program.cs</c>). El nombre lo genera el servidor con un <c>guid</c>: el
/// nombre que manda el cliente nunca toca el disco.
/// </summary>
public sealed class AlmacenDeArchivosLocal(OpcionesDeArchivos opciones) : IAlmacenDeArchivos
{
    /// <summary>Prefijo web bajo el que se monta la raíz del almacén.</summary>
    public const string PrefijoWeb = "/uploads";

    private readonly string _raiz = Path.TrimEndingDirectorySeparator(Path.GetFullPath(opciones.Ruta));

    public async Task<ResultadoDeGuardado> GuardarImagenAsync(
        IFormFile archivo,
        string subcarpeta,
        CancellationToken cancellationToken = default)
    {
        if (archivo.Length == 0)
        {
            return ResultadoDeGuardado.Rechazado(ReglasDeArchivos.Vacio());
        }

        if (archivo.Length > ReglasDeArchivos.TamanoMaximoEnBytes)
        {
            return ResultadoDeGuardado.Rechazado(ReglasDeArchivos.DemasiadoGrande());
        }

        var cabecera = await LeerCabeceraAsync(archivo, cancellationToken);
        var extension = FirmasDeImagen.ExtensionDe(cabecera);

        if (extension is null)
        {
            return ResultadoDeGuardado.Rechazado(ReglasDeArchivos.NoEsImagen());
        }

        var carpeta = RutaDeLaSubcarpeta(subcarpeta);
        Directory.CreateDirectory(carpeta);

        var nombre = $"{Guid.NewGuid():N}{extension}";
        var destino = Path.Combine(carpeta, nombre);

        try
        {
            await using var origen = archivo.OpenReadStream();
            await using var salida = new FileStream(destino, FileMode.CreateNew);

            await origen.CopyToAsync(salida, cancellationToken);
        }
        catch
        {
            // Copia a medias: se borra el resto y la excepción sigue su curso (es un fallo no previsto).
            File.Delete(destino);
            throw;
        }

        var relativa = Path.GetRelativePath(_raiz, carpeta).Replace('\\', '/');

        return ResultadoDeGuardado.Bien($"{PrefijoWeb}/{relativa}/{nombre}");
    }

    public void Borrar(string? rutaRelativa)
    {
        try
        {
            var ruta = RutaFisica(rutaRelativa);

            if (ruta is not null && File.Exists(ruta))
            {
                File.Delete(ruta);
            }
        }
        catch (Exception excepcion)
            when (excepcion is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            // Silencioso e idempotente: una falla de disco no puede tumbar el pedido que sí guardó la fila.
        }
    }

    private async Task<byte[]> LeerCabeceraAsync(IFormFile archivo, CancellationToken cancellationToken)
    {
        var cabecera = new byte[FirmasDeImagen.LargoDeCabecera];

        await using var origen = archivo.OpenReadStream();
        var leidos = await origen.ReadAtLeastAsync(cabecera, cabecera.Length, throwOnEndOfStream: false, cancellationToken);

        return leidos == cabecera.Length ? cabecera : [];
    }

    /// <summary>Carpeta destino, siempre dentro de la raíz: la subcarpeta la elige el servidor.</summary>
    private string RutaDeLaSubcarpeta(string subcarpeta)
    {
        var limpia = subcarpeta
            .Trim('/', '\\')
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        var ruta = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.Combine(_raiz, limpia)));

        if (!ruta.StartsWith(_raiz + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"La subcarpeta '{subcarpeta}' se sale de la raíz del almacén.");
        }

        return ruta;
    }

    /// <summary>Traduce la ruta web guardada en la base a una ruta de disco, con guarda de traversal.</summary>
    private string? RutaFisica(string? rutaRelativa)
    {
        if (string.IsNullOrWhiteSpace(rutaRelativa)
            || !rutaRelativa.StartsWith(PrefijoWeb + "/", StringComparison.Ordinal))
        {
            return null;
        }

        var relativa = rutaRelativa[(PrefijoWeb.Length + 1)..].Replace('/', Path.DirectorySeparatorChar);
        var ruta = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.Combine(_raiz, relativa)));

        return ruta.StartsWith(_raiz + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            ? ruta
            : null;
    }
}
