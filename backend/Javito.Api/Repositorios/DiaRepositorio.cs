using Javito.Api.Datos;
using Javito.Api.Modelos;
using MySqlConnector;

namespace Javito.Api.Repositorios;

/// <summary>
/// Arma la proyección del día en una sola consulta: el LEFT JOIN trae la marca de la fecha pedida —o
/// nada, que es «sin marcar»— y la subconsulta cuenta los días completados de la semana en curso.
/// </summary>
public sealed class DiaRepositorio(IDbConnectionFactory fabrica) : IDiaRepositorio
{
    public async Task<IReadOnlyList<HabitoDelDia>> ListarAsync(
        int usuarioId,
        DateOnly fecha,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT h.id,
                   h.titulo,
                   c.nombre,
                   h.tipo_habito,
                   h.meta_valor,
                   h.unidad_medida,
                   h.frecuencia_tipo,
                   h.frecuencia_veces,
                   (SELECT COUNT(*)
                    FROM registro_habito cumplido
                    WHERE cumplido.habito_id = h.id
                      AND cumplido.completado = TRUE
                      AND cumplido.fecha BETWEEN @lunes AND @domingo) AS cumplidos,
                   r.valor_alcanzado,
                   r.completado
            FROM habito h
            LEFT JOIN categoria c ON c.id = h.categoria_id
            LEFT JOIN registro_habito r ON r.habito_id = h.id AND r.fecha = @fecha
            WHERE h.usuario_id = @usuarioId
              AND h.activo = TRUE
            ORDER BY h.id
            """;

        // La semana se cuenta de lunes a domingo, como el seed: WEEKDAY() de MySQL también usa el
        // lunes como primer día, y REG-04 dibuja esa misma semana.
        var desdeElLunes = ((int)fecha.DayOfWeek + 6) % 7;
        var lunes = fecha.AddDays(-desdeElLunes);
        var domingo = lunes.AddDays(6);

        await using var conexion = await fabrica.AbrirAsync(cancellationToken);
        await using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@usuarioId", usuarioId);
        comando.Parameters.AddWithValue("@fecha", fecha);
        comando.Parameters.AddWithValue("@lunes", lunes);
        comando.Parameters.AddWithValue("@domingo", domingo);

        var habitos = new List<HabitoDelDia>();
        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
        while (await lector.ReadAsync(cancellationToken))
        {
            habitos.Add(ADominio(lector));
        }

        return habitos;
    }

    private static HabitoDelDia ADominio(MySqlDataReader lector) => new()
    {
        Id = lector.GetInt32(0),
        Titulo = lector.GetString(1),
        Categoria = lector.IsDBNull(2) ? null : lector.GetString(2),
        Tipo = lector.GetString(3),
        Meta = lector.IsDBNull(4) ? null : lector.GetDecimal(4),
        Unidad = lector.IsDBNull(5) ? null : lector.GetString(5),
        FrecuenciaTipo = lector.GetString(6),
        FrecuenciaVeces = lector.IsDBNull(7) ? null : lector.GetInt32(7),
        CumplidosEnLaSemana = lector.GetInt32(8),
        Valor = lector.IsDBNull(9) ? null : lector.GetDecimal(9),
        // Sin fila de registro la columna viene NULL; el CHECK de la base garantiza que haya fila
        // siempre que el valor no lo sea, así que el valor manda.
        Completado = !lector.IsDBNull(10) && lector.GetBoolean(10)
    };
}
