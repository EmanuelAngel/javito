using Javito.Api.Datos;
using Javito.Api.Modelos;
using MySqlConnector;

namespace Javito.Api.Repositorios;

public sealed class HabitoRepositorio(IDbConnectionFactory fabrica) : IHabitoRepositorio
{
    public async Task<Habito?> BuscarAsync(
        int id,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, usuario_id, categoria_id, titulo, descripcion, tipo_habito, meta_valor,
                   unidad_medida, frecuencia_tipo, frecuencia_veces, activo
            FROM habito
            WHERE id = @id AND usuario_id = @usuarioId
            """;

        await using var conexion = await fabrica.AbrirAsync(cancellationToken);
        await using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@id", id);
        comando.Parameters.AddWithValue("@usuarioId", usuarioId);

        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
        if (!await lector.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new Habito
        {
            Id = lector.GetInt32(0),
            UsuarioId = lector.GetInt32(1),
            CategoriaId = lector.IsDBNull(2) ? null : lector.GetInt32(2),
            Titulo = lector.GetString(3),
            Descripcion = lector.IsDBNull(4) ? null : lector.GetString(4),
            Tipo = lector.GetString(5),
            Meta = lector.IsDBNull(6) ? null : lector.GetDecimal(6),
            Unidad = lector.IsDBNull(7) ? null : lector.GetString(7),
            FrecuenciaTipo = lector.GetString(8),
            FrecuenciaVeces = lector.IsDBNull(9) ? null : lector.GetInt32(9),
            Activo = lector.GetBoolean(10)
        };
    }

    public async Task<IReadOnlyList<Habito>> ListarAsync(
        int usuarioId,
        bool? activo,
        string? q,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, usuario_id, categoria_id, titulo, descripcion, tipo_habito, meta_valor,
                   unidad_medida, frecuencia_tipo, frecuencia_veces, activo
            FROM habito
            WHERE usuario_id = @usuarioId
              AND (@activo IS NULL OR activo = @activo)
              AND (@patron IS NULL OR (activo = TRUE AND titulo LIKE @patron ESCAPE '\\'))
            ORDER BY activo DESC, titulo
            """;

        await using var conexion = await fabrica.AbrirAsync(cancellationToken);
        await using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@usuarioId", usuarioId);
        comando.Parameters.AddWithValue("@activo", activo is null ? DBNull.Value : activo.Value);
        comando.Parameters.AddWithValue("@patron", (object?)APatron(q) ?? DBNull.Value);

        var habitos = new List<Habito>();
        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
        while (await lector.ReadAsync(cancellationToken))
        {
            habitos.Add(ADominio(lector));
        }

        return habitos;
    }

    public async Task<int> CrearAsync(Habito habito, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO habito (usuario_id, categoria_id, titulo, descripcion, tipo_habito, meta_valor,
                                unidad_medida, frecuencia_tipo, frecuencia_veces, activo)
            VALUES (@usuarioId, @categoriaId, @titulo, @descripcion, @tipo, @meta, @unidad,
                    @frecuenciaTipo, @frecuenciaVeces, TRUE)
            """;

        await using var conexion = await fabrica.AbrirAsync(cancellationToken);
        await using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@usuarioId", habito.UsuarioId);
        comando.Parameters.AddWithValue("@categoriaId", (object?)habito.CategoriaId ?? DBNull.Value);
        comando.Parameters.AddWithValue("@titulo", habito.Titulo);
        comando.Parameters.AddWithValue("@descripcion", (object?)habito.Descripcion ?? DBNull.Value);
        comando.Parameters.AddWithValue("@tipo", habito.Tipo);
        comando.Parameters.AddWithValue("@meta", (object?)habito.Meta ?? DBNull.Value);
        comando.Parameters.AddWithValue("@unidad", (object?)habito.Unidad ?? DBNull.Value);
        comando.Parameters.AddWithValue("@frecuenciaTipo", habito.FrecuenciaTipo);
        comando.Parameters.AddWithValue("@frecuenciaVeces", (object?)habito.FrecuenciaVeces ?? DBNull.Value);

        await comando.ExecuteNonQueryAsync(cancellationToken);
        habito.Activo = true;

        // La fila recién insertada se lee de esta misma conexión: LAST_INSERT_ID() es por sesión.
        await using var ultimoId = new MySqlCommand("SELECT LAST_INSERT_ID()", conexion);
        return Convert.ToInt32(await ultimoId.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<Registro> GuardarRegistroAsync(
        Registro registro,
        CancellationToken cancellationToken = default)
    {
        // ON DUPLICATE KEY sobre uq_registro_habito_fecha: el par ya existe o se crea, y en los dos
        // casos queda con el valor que manda el cliente.
        const string sql = """
            INSERT INTO registro_habito (habito_id, fecha, valor_alcanzado, meta_objetivo, completado, notas)
            VALUES (@habitoId, @fecha, @valor, @meta, @completado, @notas)
            ON DUPLICATE KEY UPDATE
                valor_alcanzado = @valor,
                meta_objetivo = @meta,
                completado = @completado,
                notas = @notas
            """;

        const string lectura = """
            SELECT id, habito_id, fecha, valor_alcanzado, meta_objetivo, completado, notas
            FROM registro_habito
            WHERE habito_id = @habitoId AND fecha = @fecha
            """;

        await using var conexion = await fabrica.AbrirAsync(cancellationToken);

        await using (var comando = new MySqlCommand(sql, conexion))
        {
            comando.Parameters.AddWithValue("@habitoId", registro.HabitoId);
            comando.Parameters.AddWithValue("@fecha", registro.Fecha);
            comando.Parameters.AddWithValue("@valor", registro.ValorAlcanzado);
            comando.Parameters.AddWithValue("@meta", registro.MetaObjetivo);
            comando.Parameters.AddWithValue("@completado", registro.Completado);
            comando.Parameters.AddWithValue("@notas", (object?)registro.Notas ?? DBNull.Value);

            await comando.ExecuteNonQueryAsync(cancellationToken);
        }

        // La fila se relee de la misma conexión: el id no es el del INSERT cuando la sentencia pasó
        // por la rama del UPDATE.
        await using var confirmacion = new MySqlCommand(lectura, conexion);
        confirmacion.Parameters.AddWithValue("@habitoId", registro.HabitoId);
        confirmacion.Parameters.AddWithValue("@fecha", registro.Fecha);

        await using var lector = await confirmacion.ExecuteReaderAsync(cancellationToken);
        await lector.ReadAsync(cancellationToken);

        return new Registro
        {
            Id = lector.GetInt32(0),
            HabitoId = lector.GetInt32(1),
            Fecha = lector.GetFieldValue<DateOnly>(2),
            ValorAlcanzado = lector.GetDecimal(3),
            MetaObjetivo = lector.GetDecimal(4),
            Completado = lector.GetBoolean(5),
            Notas = lector.IsDBNull(6) ? null : lector.GetString(6)
        };
    }

    public async Task<bool> BorrarRegistroAsync(
        int habitoId,
        DateOnly fecha,
        CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM registro_habito WHERE habito_id = @habitoId AND fecha = @fecha";

        await using var conexion = await fabrica.AbrirAsync(cancellationToken);
        await using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@habitoId", habitoId);
        comando.Parameters.AddWithValue("@fecha", fecha);

        return await comando.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    /// <summary>
    /// Envuelve el texto del usuario en comodines, neutralizando los que ya son comodines: buscar
    /// «50%» tiene que encontrar ese título literal y no traer todo. La barra va primero porque es el
    /// carácter de escape de LIKE.
    /// </summary>
    private static string? APatron(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return null;
        }

        var escapado = texto.Trim()
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_");

        return $"%{escapado}%";
    }

    private static Habito ADominio(MySqlDataReader lector) => new()
    {
        Id = lector.GetInt32(0),
        UsuarioId = lector.GetInt32(1),
        CategoriaId = lector.IsDBNull(2) ? null : lector.GetInt32(2),
        Titulo = lector.GetString(3),
        Descripcion = lector.IsDBNull(4) ? null : lector.GetString(4),
        Tipo = lector.GetString(5),
        Meta = lector.IsDBNull(6) ? null : lector.GetDecimal(6),
        Unidad = lector.IsDBNull(7) ? null : lector.GetString(7),
        FrecuenciaTipo = lector.GetString(8),
        FrecuenciaVeces = lector.IsDBNull(9) ? null : lector.GetInt32(9),
        Activo = lector.GetBoolean(10)
    };
}
