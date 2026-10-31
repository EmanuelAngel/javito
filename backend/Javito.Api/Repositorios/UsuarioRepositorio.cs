using Javito.Api.Datos;
using Javito.Api.Modelos;
using MySqlConnector;

namespace Javito.Api.Repositorios;

public sealed class UsuarioRepositorio(IDbConnectionFactory fabrica) : IUsuarioRepositorio
{
    private const string Columnas = "id, rol, nombre, email, password_hash, avatar_url, activo";

    public Task<Usuario?> BuscarPorEmailAsync(string email, CancellationToken cancellationToken = default) =>
        BuscarAsync("email = @valor", email, cancellationToken);

    public Task<Usuario?> BuscarPorIdAsync(int id, CancellationToken cancellationToken = default) =>
        BuscarAsync("id = @valor", id, cancellationToken);

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
