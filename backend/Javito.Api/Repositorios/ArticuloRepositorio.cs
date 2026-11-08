using Javito.Api.Datos;
using Javito.Api.Modelos;
using MySqlConnector;

namespace Javito.Api.Repositorios;

public sealed class ArticuloRepositorio(IDbConnectionFactory fabrica) : IArticuloRepositorio
{
    /// <summary>Las columnas de la tabla, en el orden en que las lee <see cref="ADominio"/>.</summary>
    private const string Columnas = """
        id, usuario_id, categoria_id, titulo, resumen, contenido_markdown,
        imagen_portada_url, fecha_publicacion, activo
        """;

    public async Task<int> CrearAsync(Articulo articulo, CancellationToken cancellationToken = default)
    {
        // fecha_publicacion no se escribe: la pone la base con su default, así el artículo "nace con
        // fecha de publicación" aunque el cliente no la mande. Nace activo por la misma razón.
        const string sql = """
            INSERT INTO articulo
                (usuario_id, categoria_id, titulo, resumen, contenido_markdown, imagen_portada_url, activo)
            VALUES (@usuarioId, @categoriaId, @titulo, @resumen, @contenidoMarkdown, @imagenPortadaUrl, TRUE)
            """;

        await using var conexion = await fabrica.AbrirAsync(cancellationToken);
        await using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@usuarioId", articulo.UsuarioId);
        comando.Parameters.AddWithValue("@categoriaId", articulo.CategoriaId);
        comando.Parameters.AddWithValue("@titulo", articulo.Titulo);
        comando.Parameters.AddWithValue("@resumen", articulo.Resumen);
        comando.Parameters.AddWithValue("@contenidoMarkdown", articulo.ContenidoMarkdown);
        comando.Parameters.AddWithValue("@imagenPortadaUrl", (object?)articulo.ImagenPortadaUrl ?? DBNull.Value);

        await comando.ExecuteNonQueryAsync(cancellationToken);

        // La fila recién insertada se lee de esta misma conexión: LAST_INSERT_ID() es por sesión.
        await using var ultimoId = new MySqlCommand("SELECT LAST_INSERT_ID()", conexion);
        return Convert.ToInt32(await ultimoId.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<Articulo?> BuscarAsync(int id, CancellationToken cancellationToken = default)
    {
        var sql = $"SELECT {Columnas} FROM articulo WHERE id = @id";

        await using var conexion = await fabrica.AbrirAsync(cancellationToken);
        await using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@id", id);

        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
        return await lector.ReadAsync(cancellationToken) ? ADominio(lector) : null;
    }

    public async Task<bool> ActualizarAsync(Articulo articulo, CancellationToken cancellationToken = default)
    {
        // fecha_publicacion y activo quedan afuera del SET: corregir no es volver a publicar. El filtro
        // por activo hace que una baja cruzada con la edición no reviva el artículo por la espalda.
        const string sql = """
            UPDATE articulo
            SET categoria_id = @categoriaId, titulo = @titulo, resumen = @resumen,
                contenido_markdown = @contenidoMarkdown, imagen_portada_url = @imagenPortadaUrl
            WHERE id = @id AND activo = TRUE
            """;

        await using var conexion = await fabrica.AbrirAsync(cancellationToken);
        await using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@id", articulo.Id);
        comando.Parameters.AddWithValue("@categoriaId", articulo.CategoriaId);
        comando.Parameters.AddWithValue("@titulo", articulo.Titulo);
        comando.Parameters.AddWithValue("@resumen", articulo.Resumen);
        comando.Parameters.AddWithValue("@contenidoMarkdown", articulo.ContenidoMarkdown);
        comando.Parameters.AddWithValue("@imagenPortadaUrl", (object?)articulo.ImagenPortadaUrl ?? DBNull.Value);

        // MySqlConnector cuenta filas encontradas, no cambiadas: guardar sin cambios también es 1.
        return await comando.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    /// <summary>
    /// Las columnas que necesita una tarjeta del catálogo. El cuerpo (<c>MEDIUMTEXT</c>) queda afuera
    /// a propósito: traer seis artículos completos de la base para mostrar títulos y resúmenes es
    /// justo lo que HU-ART-04 pide evitar. El cuerpo lo lee <see cref="BuscarAsync"/>.
    /// </summary>
    private const string ColumnasDelCatalogo = """
        id, usuario_id, categoria_id, titulo, resumen, imagen_portada_url, fecha_publicacion, activo
        """;

    public async Task<(IReadOnlyList<Articulo> Elementos, int Total)> ListarCatalogoAsync(
        int? categoriaId,
        int limite,
        long desplazamiento,
        CancellationToken cancellationToken = default)
    {
        // El filtro vive en una sola constante porque lo usan las dos consultas: así no hay forma de
        // que el conteo y la página se refieran a conjuntos distintos.
        const string filtro = """
            FROM articulo
            WHERE activo = TRUE AND (@categoriaId IS NULL OR categoria_id = @categoriaId)
            """;

        // Desempate por id: dos artículos publicados el mismo día tienen que salir siempre igual, o la
        // paginación repite y saltea filas entre dos requests.
        var sql = $"""
            SELECT {ColumnasDelCatalogo} {filtro}
            ORDER BY fecha_publicacion DESC, id DESC
            LIMIT @limite OFFSET @desplazamiento
            """;

        await using var conexion = await fabrica.AbrirAsync(cancellationToken);

        var elementos = new List<Articulo>();

        await using (var comando = new MySqlCommand(sql, conexion))
        {
            comando.Parameters.AddWithValue("@categoriaId", (object?)categoriaId ?? DBNull.Value);
            comando.Parameters.AddWithValue("@limite", limite);
            comando.Parameters.AddWithValue("@desplazamiento", desplazamiento);

            await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
            while (await lector.ReadAsync(cancellationToken))
            {
                elementos.Add(ADominioDelCatalogo(lector));
            }
        }

        await using var conteo = new MySqlCommand($"SELECT COUNT(*) {filtro}", conexion);
        conteo.Parameters.AddWithValue("@categoriaId", (object?)categoriaId ?? DBNull.Value);

        return (elementos, Convert.ToInt32(await conteo.ExecuteScalarAsync(cancellationToken)));
    }

    /// <summary>
    /// El artículo sin su cuerpo: <c>ContenidoMarkdown</c> queda vacío porque la consulta del catálogo
    /// no lo pide. Nadie tiene que leer ese campo desde acá: el detalle se lee con <see cref="BuscarAsync"/>.
    /// </summary>
    private static Articulo ADominioDelCatalogo(MySqlDataReader lector) => new()
    {
        Id = lector.GetInt32(0),
        UsuarioId = lector.GetInt32(1),
        CategoriaId = lector.GetInt32(2),
        Titulo = lector.GetString(3),
        Resumen = lector.GetString(4),
        ImagenPortadaUrl = lector.IsDBNull(5) ? null : lector.GetString(5),
        FechaPublicacion = lector.GetDateTime(6),
        Activo = lector.GetBoolean(7)
    };

    private static Articulo ADominio(MySqlDataReader lector) => new()
    {
        Id = lector.GetInt32(0),
        UsuarioId = lector.GetInt32(1),
        CategoriaId = lector.GetInt32(2),
        Titulo = lector.GetString(3),
        Resumen = lector.GetString(4),
        ContenidoMarkdown = lector.GetString(5),
        ImagenPortadaUrl = lector.IsDBNull(6) ? null : lector.GetString(6),
        FechaPublicacion = lector.GetDateTime(7),
        Activo = lector.GetBoolean(8)
    };
}
