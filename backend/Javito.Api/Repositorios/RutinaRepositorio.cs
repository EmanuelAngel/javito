using Javito.Api.Datos;
using Javito.Api.Modelos;
using MySqlConnector;

namespace Javito.Api.Repositorios;

public sealed class RutinaRepositorio(IDbConnectionFactory fabrica) : IRutinaRepositorio
{
    public async Task<IReadOnlyList<Rutina>> ListarAsync(
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        // No filtra por activo: todavía no hay baja lógica (RUT-05 está fuera de alcance) y todas
        // nacen activas, así que esconderlas acá sería una regla que nadie pidió.
        const string sql = """
            SELECT id, usuario_id, nombre, descripcion, activo
            FROM rutina
            WHERE usuario_id = @usuarioId
            ORDER BY id
            """;

        await using var conexion = await fabrica.AbrirAsync(cancellationToken);
        await using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@usuarioId", usuarioId);

        var rutinas = new List<Rutina>();
        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
        while (await lector.ReadAsync(cancellationToken))
        {
            rutinas.Add(ADominio(lector));
        }

        return rutinas;
    }

    public async Task<Rutina?> BuscarAsync(
        int id,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, usuario_id, nombre, descripcion, activo
            FROM rutina
            WHERE id = @id AND usuario_id = @usuarioId
            """;

        await using var conexion = await fabrica.AbrirAsync(cancellationToken);
        await using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@id", id);
        comando.Parameters.AddWithValue("@usuarioId", usuarioId);

        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
        return await lector.ReadAsync(cancellationToken) ? ADominio(lector) : null;
    }

    public async Task<int> CrearAsync(Rutina rutina, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO rutina (usuario_id, nombre, descripcion, activo)
            VALUES (@usuarioId, @nombre, @descripcion, TRUE)
            """;

        await using var conexion = await fabrica.AbrirAsync(cancellationToken);
        await using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@usuarioId", rutina.UsuarioId);
        comando.Parameters.AddWithValue("@nombre", rutina.Nombre);
        comando.Parameters.AddWithValue("@descripcion", (object?)rutina.Descripcion ?? DBNull.Value);
        rutina.Activo = true;

        await comando.ExecuteNonQueryAsync(cancellationToken);

        // La fila recién insertada se lee de esta misma conexión: LAST_INSERT_ID() es por sesión.
        await using var ultimoId = new MySqlCommand("SELECT LAST_INSERT_ID()", conexion);
        return Convert.ToInt32(await ultimoId.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<IReadOnlyList<HabitoEnRutina>> ListarHabitosAsync(
        int rutinaId,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        // El dueño de la rutina viaja en el join: pedir los hábitos de una rutina ajena devuelve
        // vacío sin necesitar una consulta previa.
        const string sql = """
            SELECT h.id, h.titulo, rh.orden
            FROM rutina_habito rh
            JOIN rutina r ON r.id = rh.rutina_id
            JOIN habito h ON h.id = rh.habito_id
            WHERE rh.rutina_id = @rutinaId AND r.usuario_id = @usuarioId
            ORDER BY rh.orden, h.id
            """;

        await using var conexion = await fabrica.AbrirAsync(cancellationToken);
        await using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@rutinaId", rutinaId);
        comando.Parameters.AddWithValue("@usuarioId", usuarioId);

        var habitos = new List<HabitoEnRutina>();
        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
        while (await lector.ReadAsync(cancellationToken))
        {
            habitos.Add(new HabitoEnRutina
            {
                Id = lector.GetInt32(0),
                Titulo = lector.GetString(1),
                Orden = lector.GetInt32(2)
            });
        }

        return habitos;
    }

    public async Task<bool> ExisteHabitoAsync(
        int rutinaId,
        int habitoId,
        CancellationToken cancellationToken = default)
    {
        const string sql =
            "SELECT 1 FROM rutina_habito WHERE rutina_id = @rutinaId AND habito_id = @habitoId";

        await using var conexion = await fabrica.AbrirAsync(cancellationToken);
        await using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@rutinaId", rutinaId);
        comando.Parameters.AddWithValue("@habitoId", habitoId);

        return await comando.ExecuteScalarAsync(cancellationToken) is not null;
    }

    public async Task<int> AgregarHabitoAsync(
        int rutinaId,
        int habitoId,
        CancellationToken cancellationToken = default)
    {
        // INSERT … SELECT: el lugar se calcula dentro de la misma sentencia, así que el CHECK
        // (orden >= 1) y la numeración no dependen de lo que haya leído antes otro pedido.
        const string insertar = """
            INSERT INTO rutina_habito (rutina_id, habito_id, orden)
            SELECT @rutinaId, @habitoId, COALESCE(MAX(orden), 0) + 1
            FROM rutina_habito
            WHERE rutina_id = @rutinaId
            """;

        await using var conexion = await fabrica.AbrirAsync(cancellationToken);
        await using var comando = new MySqlCommand(insertar, conexion);
        comando.Parameters.AddWithValue("@rutinaId", rutinaId);
        comando.Parameters.AddWithValue("@habitoId", habitoId);

        await comando.ExecuteNonQueryAsync(cancellationToken);

        await using var orden = new MySqlCommand(
            "SELECT orden FROM rutina_habito WHERE rutina_id = @rutinaId AND habito_id = @habitoId",
            conexion);
        orden.Parameters.AddWithValue("@rutinaId", rutinaId);
        orden.Parameters.AddWithValue("@habitoId", habitoId);

        return Convert.ToInt32(await orden.ExecuteScalarAsync(cancellationToken));
    }

    private static Rutina ADominio(MySqlDataReader lector) => new()
    {
        Id = lector.GetInt32(0),
        UsuarioId = lector.GetInt32(1),
        Nombre = lector.GetString(2),
        Descripcion = lector.IsDBNull(3) ? null : lector.GetString(3),
        Activo = lector.GetBoolean(4)
    };
}
