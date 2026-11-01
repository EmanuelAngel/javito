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

    public async Task<Categoria?> BuscarPorNombreAsync(
        string nombre,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, nombre, descripcion, activo
            FROM categoria
            WHERE nombre = @nombre
            """;

        await using var conexion = await fabrica.AbrirAsync(cancellationToken);
        await using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@nombre", nombre);

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

        await comando.ExecuteNonQueryAsync(cancellationToken);

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

        // MySqlConnector informa filas encontradas y no modificadas (UseAffectedRows es false por
        // defecto): guardar los mismos valores cuenta como 1 y no se confunde con un 404.
        return await comando.ExecuteNonQueryAsync(cancellationToken) > 0;
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
}
