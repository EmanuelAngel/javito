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
            categorias.Add(new Categoria
            {
                Id = lector.GetInt32(0),
                Nombre = lector.GetString(1),
                Descripcion = lector.IsDBNull(2) ? null : lector.GetString(2),
                Activo = lector.GetBoolean(3)
            });
        }

        return categorias;
    }
}
