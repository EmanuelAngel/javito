using Javito.Api.Contratos;
using Javito.Api.Datos;
using Javito.Api.Modelos;
using MySqlConnector;

namespace Javito.Api.Repositorios;

public sealed class CategoriaRepositorio(IDbConnectionFactory fabrica) : ICategoriaRepositorio
{
    public async Task<IReadOnlyList<Categoria>> ListarAsync(
        bool? activo,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, nombre, descripcion, activo
            FROM categoria
            WHERE @activo IS NULL OR activo = @activo
            ORDER BY id
            """;

        await using var conexion = await fabrica.AbrirAsync(cancellationToken);
        await using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@activo", activo is null ? DBNull.Value : activo.Value);

        var categorias = new List<Categoria>();
        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
        while (await lector.ReadAsync(cancellationToken))
        {
            categorias.Add(ADominio(lector));
        }

        return categorias;
    }

    public async Task<Categoria?> BuscarAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, nombre, descripcion, activo
            FROM categoria
            WHERE id = @id
            """;

        await using var conexion = await fabrica.AbrirAsync(cancellationToken);
        await using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@id", id);

        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
        return await lector.ReadAsync(cancellationToken) ? ADominio(lector) : null;
    }

    public async Task<int> CrearAsync(Categoria categoria, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO categoria (nombre, descripcion, activo)
            VALUES (@nombre, @descripcion, TRUE)
            """;

        await using var conexion = await fabrica.AbrirAsync(cancellationToken);
        await using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@nombre", categoria.Nombre);
        comando.Parameters.AddWithValue("@descripcion", (object?)categoria.Descripcion ?? DBNull.Value);
        categoria.Activo = true;

        try
        {
            await comando.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (MySqlException excepcion) when (excepcion.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
        {
            throw NombreRepetido(categoria.Nombre);
        }

        // La fila recién insertada se lee de esta misma conexión: LAST_INSERT_ID() es por sesión.
        await using var ultimoId = new MySqlCommand("SELECT LAST_INSERT_ID()", conexion);
        return Convert.ToInt32(await ultimoId.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<bool> ActualizarAsync(
        int id,
        string nombre,
        string? descripcion,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE categoria
            SET nombre = @nombre, descripcion = @descripcion
            WHERE id = @id
            """;

        await using var conexion = await fabrica.AbrirAsync(cancellationToken);
        await using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@id", id);
        comando.Parameters.AddWithValue("@nombre", nombre);
        comando.Parameters.AddWithValue("@descripcion", (object?)descripcion ?? DBNull.Value);

        try
        {
            // MySqlConnector informa filas encontradas y no modificadas (UseAffectedRows es false por
            // defecto): guardar los mismos valores cuenta como 1 y no se confunde con un 404.
            return await comando.ExecuteNonQueryAsync(cancellationToken) > 0;
        }
        catch (MySqlException excepcion) when (excepcion.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
        {
            throw NombreRepetido(nombre);
        }
    }

    public async Task<bool> CambiarActivoAsync(
        int id,
        bool activo,
        CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE categoria SET activo = @activo WHERE id = @id";

        await using var conexion = await fabrica.AbrirAsync(cancellationToken);
        await using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@id", id);
        comando.Parameters.AddWithValue("@activo", activo);

        return await comando.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static Categoria ADominio(MySqlDataReader lector) => new()
    {
        Id = lector.GetInt32(0),
        Nombre = lector.GetString(1),
        Descripcion = lector.IsDBNull(2) ? null : lector.GetString(2),
        Activo = lector.GetBoolean(3)
    };

    /// <summary>
    /// La unicidad la impone el índice de MySQL: <c>nombre</c> es UNIQUE sobre una collation que no
    /// distingue mayúsculas ni acentos. El 1062 se traduce acá y no en el controlador porque es el
    /// repositorio el que ve la restricción; de paso cubre la carrera entre dos altas simultáneas,
    /// que una consulta previa dejaría pasar.
    /// </summary>
    private static ErrorDeNegocio NombreRepetido(string nombre) => new(
        "nombre_repetido",
        "Nombre repetido",
        $"Ya existe una categoría con el nombre «{nombre}».");
}
