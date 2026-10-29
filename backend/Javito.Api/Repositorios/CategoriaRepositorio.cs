using Javito.Api.Contratos;
using Javito.Api.Datos;
using MySqlConnector;

namespace Javito.Api.Repositorios;

public sealed class CategoriaRepositorio(IDbConnectionFactory fabrica)
{
    public async Task<IReadOnlyList<CategoriaDto>> ListarAsync(
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

        var categorias = new List<CategoriaDto>();
        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
        while (await lector.ReadAsync(cancellationToken))
        {
            categorias.Add(new CategoriaDto(
                lector.GetInt32(0),
                lector.GetString(1),
                lector.IsDBNull(2) ? null : lector.GetString(2),
                lector.GetBoolean(3)));
        }

        return categorias;
    }
}
