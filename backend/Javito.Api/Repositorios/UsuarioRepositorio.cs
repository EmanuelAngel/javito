using Javito.Api.Datos;
using Javito.Api.Modelos;
using Javito.Api.Paginacion;
using MySqlConnector;

namespace Javito.Api.Repositorios;

public sealed class UsuarioRepositorio(IDbConnectionFactory fabrica) : IUsuarioRepositorio
{
    private const string Columnas = "id, rol, nombre, email, password_hash, avatar_url, activo";

    public Task<Usuario?> BuscarPorEmailAsync(string email, CancellationToken cancellationToken = default) =>
        BuscarAsync("email = @valor", email, cancellationToken);

    public Task<Usuario?> BuscarPorIdAsync(int id, CancellationToken cancellationToken = default) =>
        BuscarAsync("id = @valor", id, cancellationToken);

    public async Task<bool> ActualizarAvatarAsync(
        int id,
        string? avatarUrl,
        CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE usuario SET avatar_url = @avatar WHERE id = @id";

        await using var conexion = await fabrica.AbrirAsync(cancellationToken);
        await using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@avatar", avatarUrl);
        comando.Parameters.AddWithValue("@id", id);

        return await comando.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<(IReadOnlyList<Usuario> Elementos, int Total)> ListarAsync(
        bool? activo,
        string? q,
        int limite,
        long desplazamiento,
        CancellationToken cancellationToken = default)
    {
        // El filtro vive en una sola constante porque lo usan las dos consultas: así el conteo y la
        // página no pueden referirse a conjuntos distintos.
        const string filtro = """
            FROM usuario
            WHERE (@activo IS NULL OR activo = @activo)
              AND (@patron IS NULL
                   OR nombre LIKE @patron ESCAPE '\\'
                   OR email LIKE @patron ESCAPE '\\')
            """;

        // El id es AUTO_INCREMENT: ordenar por él descendente es el alta con el más nuevo primero, y
        // como es único, la paginación no repite ni saltea filas entre dos requests.
        var sql = $"""
            SELECT {Columnas} {filtro}
            ORDER BY id DESC
            LIMIT @limite OFFSET @desplazamiento
            """;

        await using var conexion = await fabrica.AbrirAsync(cancellationToken);

        var elementos = new List<Usuario>();

        await using (var comando = new MySqlCommand(sql, conexion))
        {
            comando.Parameters.AddWithValue("@activo", activo is null ? DBNull.Value : activo.Value);
            comando.Parameters.AddWithValue(
                "@patron",
                (object?)PatronDeBusqueda.APatron(q) ?? DBNull.Value);
            comando.Parameters.AddWithValue("@limite", limite);
            comando.Parameters.AddWithValue("@desplazamiento", desplazamiento);

            await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
            while (await lector.ReadAsync(cancellationToken))
            {
                elementos.Add(Leer(lector));
            }
        }

        await using var conteo = new MySqlCommand($"SELECT COUNT(*) {filtro}", conexion);
        conteo.Parameters.AddWithValue("@activo", activo is null ? DBNull.Value : activo.Value);
        conteo.Parameters.AddWithValue(
            "@patron",
            (object?)PatronDeBusqueda.APatron(q) ?? DBNull.Value);

        return (elementos, Convert.ToInt32(await conteo.ExecuteScalarAsync(cancellationToken)));
    }

    private async Task<Usuario?> BuscarAsync(
        string condicion,
        object valor,
        CancellationToken cancellationToken)
    {
        var sql = $"SELECT {Columnas} FROM usuario WHERE {condicion} LIMIT 1";

        await using var conexion = await fabrica.AbrirAsync(cancellationToken);
        await using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@valor", valor);

        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);

        return await lector.ReadAsync(cancellationToken) ? Leer(lector) : null;
    }

    private static Usuario Leer(MySqlDataReader lector) => new()
    {
        Id = lector.GetInt32(0),
        Rol = lector.GetString(1),
        Nombre = lector.GetString(2),
        Email = lector.GetString(3),
        PasswordHash = lector.GetString(4),
        AvatarUrl = lector.IsDBNull(5) ? null : lector.GetString(5),
        Activo = lector.GetBoolean(6)
    };
}
