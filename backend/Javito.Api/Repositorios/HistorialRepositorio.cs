using Javito.Api.Datos;
using Javito.Api.Historial;
using Javito.Api.Modelos;
using MySqlConnector;

namespace Javito.Api.Repositorios;

public sealed class HistorialRepositorio(IDbConnectionFactory fabrica) : IHistorialRepositorio
{
    /// <summary>
    /// El filtro vive en una sola constante porque lo usan las dos consultas: así el conteo y la
    /// página no pueden referirse a conjuntos distintos. El <c>JOIN</c> con <c>habito</c> es lo que
    /// hace cumplir la propiedad: <c>registro_habito</c> no guarda el dueño, lo guarda su hábito.
    /// No filtra por <c>activo</c>: el historial de un hábito archivado sigue siendo consultable.
    /// </summary>
    private const string Filtro = """
        FROM registro_habito r
        JOIN habito h ON h.id = r.habito_id
        WHERE h.usuario_id = @usuarioId
          AND (@habitoId IS NULL OR r.habito_id = @habitoId)
          AND (@desde IS NULL OR r.fecha >= @desde)
          AND (@hasta IS NULL OR r.fecha <= @hasta)
        """;

    public async Task<(IReadOnlyList<RegistroDeHistorial> Elementos, int Total)> ListarAsync(
        int usuarioId,
        int? habitoId,
        DateOnly? desde,
        DateOnly? hasta,
        int limite,
        long desplazamiento,
        CancellationToken cancellationToken = default)
    {
        // Desempate por hábito: dos marcas del mismo día tienen que salir siempre en el mismo orden,
        // o la paginación repite y saltea filas entre dos requests.
        var sql = $"""
            SELECT r.fecha, r.habito_id, h.titulo, r.valor_alcanzado, r.meta_objetivo, r.completado
            {Filtro}
            ORDER BY r.fecha DESC, r.habito_id DESC
            LIMIT @limite OFFSET @desplazamiento
            """;

        await using var conexion = await fabrica.AbrirAsync(cancellationToken);

        var elementos = new List<RegistroDeHistorial>();

        await using (var comando = new MySqlCommand(sql, conexion))
        {
            AgregarFiltros(comando, usuarioId, habitoId, desde, hasta);
            comando.Parameters.AddWithValue("@limite", limite);
            comando.Parameters.AddWithValue("@desplazamiento", desplazamiento);

            await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
            while (await lector.ReadAsync(cancellationToken))
            {
                elementos.Add(ADominio(lector));
            }
        }

        await using var conteo = new MySqlCommand($"SELECT COUNT(*) {Filtro}", conexion);
        AgregarFiltros(conteo, usuarioId, habitoId, desde, hasta);

        return (elementos, Convert.ToInt32(await conteo.ExecuteScalarAsync(cancellationToken)));
    }

    public async Task<DateOnly?> PrimerRegistroAsync(
        int habitoId,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        // MIN sobre el WHERE del dueño: un hábito ajeno no tiene marcas para quien pregunta.
        var sql = """
            SELECT MIN(r.fecha)
            FROM registro_habito r
            JOIN habito h ON h.id = r.habito_id
            WHERE h.usuario_id = @usuarioId
              AND r.habito_id = @habitoId
            """;

        await using var conexion = await fabrica.AbrirAsync(cancellationToken);
        await using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@usuarioId", usuarioId);
        comando.Parameters.AddWithValue("@habitoId", habitoId);

        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);

        return await lector.ReadAsync(cancellationToken) && !lector.IsDBNull(0)
            ? lector.GetFieldValue<DateOnly>(0)
            : null;
    }

    public async Task<IReadOnlyList<FilaDelHistorial>> ListarFilasAsync(
        int habitoId,
        int usuarioId,
        DateOnly desde,
        DateOnly hasta,
        CancellationToken cancellationToken = default)
    {
        var sql = """
            SELECT r.fecha, r.completado
            FROM registro_habito r
            JOIN habito h ON h.id = r.habito_id
            WHERE h.usuario_id = @usuarioId
              AND r.habito_id = @habitoId
              AND r.fecha >= @desde
              AND r.fecha <= @hasta
            ORDER BY r.fecha
            """;

        await using var conexion = await fabrica.AbrirAsync(cancellationToken);
        await using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@usuarioId", usuarioId);
        comando.Parameters.AddWithValue("@habitoId", habitoId);
        comando.Parameters.AddWithValue("@desde", desde);
        comando.Parameters.AddWithValue("@hasta", hasta);

        var filas = new List<FilaDelHistorial>();

        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
        while (await lector.ReadAsync(cancellationToken))
        {
            filas.Add(new FilaDelHistorial(
                lector.GetFieldValue<DateOnly>(0),
                lector.GetBoolean(1)));
        }

        return filas;
    }

    private static void AgregarFiltros(
        MySqlCommand comando,
        int usuarioId,
        int? habitoId,
        DateOnly? desde,
        DateOnly? hasta)
    {
        comando.Parameters.AddWithValue("@usuarioId", usuarioId);
        comando.Parameters.AddWithValue("@habitoId", (object?)habitoId ?? DBNull.Value);
        comando.Parameters.AddWithValue("@desde", (object?)desde ?? DBNull.Value);
        comando.Parameters.AddWithValue("@hasta", (object?)hasta ?? DBNull.Value);
    }

    private static RegistroDeHistorial ADominio(MySqlDataReader lector) => new()
    {
        Fecha = lector.GetFieldValue<DateOnly>(0),
        HabitoId = lector.GetInt32(1),
        Habito = lector.GetString(2),
        Valor = lector.GetDecimal(3),
        MetaObjetivo = lector.GetDecimal(4),
        Completado = lector.GetBoolean(5)
    };
}
