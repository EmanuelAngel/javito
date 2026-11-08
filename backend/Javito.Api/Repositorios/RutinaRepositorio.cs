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
        // No filtra por activo: la pantalla de rutinas muestra las inactivas aparte para poder
        // reactivarlas (L5). El filtro del día (RUT-04) es el que debe quedarse con las activas.
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

    public async Task<bool> ActualizarAsync(Rutina rutina, CancellationToken cancellationToken = default)
    {
        // Ni `activo` ni `rutina_habito`: editar no da de baja ni toca los hábitos ni su orden.
        const string sql = """
            UPDATE rutina
            SET nombre = @nombre, descripcion = @descripcion
            WHERE id = @id AND usuario_id = @usuarioId
            """;

        await using var conexion = await fabrica.AbrirAsync(cancellationToken);
        await using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@id", rutina.Id);
        comando.Parameters.AddWithValue("@usuarioId", rutina.UsuarioId);
        comando.Parameters.AddWithValue("@nombre", rutina.Nombre);
        comando.Parameters.AddWithValue("@descripcion", (object?)rutina.Descripcion ?? DBNull.Value);

        // Filas encontradas, no modificadas: guardar los mismos valores cuenta 1 y no es un 404.
        return await comando.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> CambiarActivoAsync(
        int id,
        int usuarioId,
        bool activo,
        CancellationToken cancellationToken = default)
    {
        // Baja lógica: `rutina_habito` queda como estaba, así que reactivar devuelve los hábitos en
        // su orden (S11).
        const string sql = "UPDATE rutina SET activo = @activo WHERE id = @id AND usuario_id = @usuarioId";

        await using var conexion = await fabrica.AbrirAsync(cancellationToken);
        await using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@id", id);
        comando.Parameters.AddWithValue("@usuarioId", usuarioId);
        comando.Parameters.AddWithValue("@activo", activo);

        return await comando.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<IReadOnlyList<HabitoEnRutina>> ListarHabitosAsync(
        int rutinaId,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        // El dueño de la rutina viaja en el join: pedir los hábitos de una rutina ajena devuelve
        // vacío sin necesitar una consulta previa. Un hábito archivado no se muestra, pero su fila de
        // `rutina_habito` queda: al reactivarlo vuelve a su lugar (S12).
        const string sql = """
            SELECT h.id, h.titulo, rh.orden
            FROM rutina_habito rh
            JOIN rutina r ON r.id = rh.rutina_id
            JOIN habito h ON h.id = rh.habito_id
            WHERE rh.rutina_id = @rutinaId AND r.usuario_id = @usuarioId AND h.activo = TRUE
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
        await using var conexion = await fabrica.AbrirAsync(cancellationToken);
        await using var transaccion = await conexion.BeginTransactionAsync(cancellationToken);

        // Se bloquea primero la fila de la rutina, que es la raíz del agregado: dos altas simultáneas
        // hacen fila acá. Calcular el `MAX(orden)` con un INSERT … SELECT sobre la tabla hija tomaba
        // locks de rango, y dos de esos se matan entre sí: InnoDB devuelve 1213 y el alta se perdía con
        // un 500 (lo pinta RutinasConcurrenciaEndpointTests).
        await using (var bloquear = new MySqlCommand(
            "SELECT id FROM rutina WHERE id = @rutinaId FOR UPDATE",
            conexion,
            transaccion))
        {
            bloquear.Parameters.AddWithValue("@rutinaId", rutinaId);
            await bloquear.ExecuteScalarAsync(cancellationToken);
        }

        // Con el lock tomado nadie más numera esta rutina, así que la lectura ya ve el último commit.
        int orden;
        await using (var ultimo = new MySqlCommand(
            "SELECT COALESCE(MAX(orden), 0) + 1 FROM rutina_habito WHERE rutina_id = @rutinaId",
            conexion,
            transaccion))
        {
            ultimo.Parameters.AddWithValue("@rutinaId", rutinaId);
            orden = Convert.ToInt32(await ultimo.ExecuteScalarAsync(cancellationToken));
        }

        await using (var insertar = new MySqlCommand(
            "INSERT INTO rutina_habito (rutina_id, habito_id, orden) " +
            "VALUES (@rutinaId, @habitoId, @orden)",
            conexion,
            transaccion))
        {
            insertar.Parameters.AddWithValue("@rutinaId", rutinaId);
            insertar.Parameters.AddWithValue("@habitoId", habitoId);
            insertar.Parameters.AddWithValue("@orden", orden);
            await insertar.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaccion.CommitAsync(cancellationToken);

        // El lugar se devuelve calculado, no releído: si otra transacción borra la fila justo después
        // del commit, una relectura devolvería null y el alta exitosa terminaría en 500.
        return orden;
    }

    public async Task<bool> QuitarHabitoAsync(
        int rutinaId,
        int habitoId,
        CancellationToken cancellationToken = default)
    {
        // Sólo cae la relación: la fila de habito no se toca, así que el hábito sigue activo y en sus
        // otras rutinas. Los huecos que quedan en `orden` los normaliza RUT-03 cuando llegue.
        const string sql =
            "DELETE FROM rutina_habito WHERE rutina_id = @rutinaId AND habito_id = @habitoId";

        await using var conexion = await fabrica.AbrirAsync(cancellationToken);
        await using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@rutinaId", rutinaId);
        comando.Parameters.AddWithValue("@habitoId", habitoId);

        return await comando.ExecuteNonQueryAsync(cancellationToken) > 0;
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
