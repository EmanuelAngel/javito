using System.Globalization;
using Javito.Api.Datos;
using Javito.Api.Modelos;
using MySqlConnector;

namespace Javito.Api.Repositorios;

/// <summary>
/// Arma la proyección del día en una sola consulta: el LEFT JOIN trae la marca de la fecha pedida —o
/// nada, que es «sin marcar»— y la subconsulta trae las fechas completadas de la semana en curso. Los
/// siete días se arman acá, cruzando el calendario con esas fechas, para que la regla del lunes viva en
/// un solo lugar.
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
                   (SELECT GROUP_CONCAT(DATE_FORMAT(cumplido.fecha, '%Y-%m-%d') ORDER BY cumplido.fecha)
                    FROM registro_habito cumplido
                    WHERE cumplido.habito_id = h.id
                      AND cumplido.completado = TRUE
                      AND cumplido.fecha BETWEEN @lunes AND @domingo) AS cumplidos,
                   r.valor_alcanzado,
                   r.completado,
                   r.meta_objetivo
            FROM habito h
            LEFT JOIN categoria c ON c.id = h.categoria_id
            LEFT JOIN registro_habito r ON r.habito_id = h.id AND r.fecha = @fecha
            WHERE h.usuario_id = @usuarioId
              AND h.activo = TRUE
            ORDER BY h.id
            """;

        // La semana se cuenta de lunes a domingo, como el seed: WEEKDAY() de MySQL también usa el
        // lunes como primer día, y REG-04 dibuja esa misma semana.
        var lunes = LunesDe(fecha);
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
            habitos.Add(ADominio(lector, lunes));
        }

        return habitos;
    }

    private static DateOnly LunesDe(DateOnly fecha) => fecha.AddDays(-(((int)fecha.DayOfWeek + 6) % 7));

    /// <summary>
    /// Desarma el <c>GROUP_CONCAT</c> de fechas. Viene NULL cuando el hábito no completó ningún día de
    /// la semana, que es el caso normal de un lunes para un hábito semanal.
    /// </summary>
    private static List<DateOnly> ADiasCumplidos(string? fechas) =>
        string.IsNullOrEmpty(fechas)
            ? []
            : fechas
                .Split(',')
                .Select(fecha => DateOnly.ParseExact(fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture))
                .ToList();

    private static HabitoDelDia ADominio(MySqlDataReader lector, DateOnly lunes)
    {
        var cumplidos = ADiasCumplidos(lector.IsDBNull(8) ? null : lector.GetString(8));
        var tipo = lector.GetString(3);

        // S2: un contador con marca muestra la meta con la que ese día se cerró, porque editarla
        // después no reescribe el pasado; sin marca vale la meta vigente del hábito. En sí/no y
        // abstinencia la meta sigue siendo la del hábito: ahí no hay nada que contar.
        var meta = tipo == TiposDeHabito.Contador && !lector.IsDBNull(11)
            ? lector.GetDecimal(11)
            : lector.IsDBNull(4) ? null : (decimal?)lector.GetDecimal(4);

        return new()
        {
            Id = lector.GetInt32(0),
            Titulo = lector.GetString(1),
            Categoria = lector.IsDBNull(2) ? null : lector.GetString(2),
            Tipo = tipo,
            Meta = meta,
            Unidad = lector.IsDBNull(5) ? null : lector.GetString(5),
            FrecuenciaTipo = lector.GetString(6),
            FrecuenciaVeces = lector.IsDBNull(7) ? null : lector.GetInt32(7),
            Semana = Enumerable
                .Range(0, 7)
                .Select(indice => lunes.AddDays(indice))
                .Select(fecha => new DiaDeLaSemana(fecha, cumplidos.Contains(fecha)))
                .ToList(),
            Valor = lector.IsDBNull(9) ? null : lector.GetDecimal(9),
            // Sin fila de registro la columna viene NULL; el CHECK de la base garantiza que haya fila
            // siempre que el valor no lo sea, así que el valor manda.
            Completado = !lector.IsDBNull(10) && lector.GetBoolean(10)
        };
    }
}
