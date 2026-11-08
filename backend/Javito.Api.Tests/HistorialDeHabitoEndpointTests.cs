using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Javito.Api.Tests.Infraestructura;
using MySqlConnector;

namespace Javito.Api.Tests;

/// <summary>
/// S6-S8 de HIST-02: el detalle histórico de un hábito. Una prueba por regla del cálculo (L4-L7): qué
/// ventana dibuja cada período, qué días son cumplidos, vacíos o neutros, cómo se cuentan los
/// evaluables de un diario y de un semanal, cómo se cierra el porcentaje y qué rechaza el endpoint.
/// La ventana cae en junio de 2001, donde el seed no sembró nada. Los hábitos son propios y llevan el
/// prefijo «Prueba detalle »: el seed no se toca.
/// </summary>
[Collection(ColeccionDeCategorias.Nombre)]
public class HistorialDeHabitoEndpointTests(FabricaDeApi fabrica) : IAsyncLifetime
{
    /// <summary>El último día de la ventana que piden estas pruebas: 2001-06-30, un sábado.</summary>
    private static readonly DateOnly Hasta = new(2001, 6, 30);

    private const string Prefijo = "Prueba detalle ";

    private readonly HttpClient _cliente = fabrica.CreateClient();

    public async Task InitializeAsync()
    {
        await SesionDePrueba.ClienteAutenticadoAsync(_cliente);
        await LimpiarAsync();
    }

    public async Task DisposeAsync()
    {
        await LimpiarAsync();
        _cliente.Dispose();
    }

    [Fact]
    public async Task Los_tres_periodos_dibujan_la_ventana_completa()
    {
        var habito = await CrearAsync(UnDiario("Prueba detalle períodos"));

        foreach (var dias in new[] { 30, 60, 365 })
        {
            var detalle = await DetalleAsync(habito.Id, dias);

            Assert.Equal(habito.Id, detalle.HabitoId);
            Assert.Equal("Prueba detalle períodos", detalle.Habito);
            Assert.Equal("si_no", detalle.Tipo);
            Assert.Equal(new FrecuenciaDePrueba("diaria", null), detalle.Frecuencia);
            Assert.Equal(dias, detalle.Dias);
            Assert.Equal(dias, detalle.Cuadricula.Count);
            Assert.Equal(Hasta.AddDays(-(dias - 1)), detalle.Desde);
            Assert.Equal(Hasta, detalle.Hasta);
            // La cuadrícula es ascendente y ambos extremos de la ventana están incluidos.
            Assert.Equal(detalle.Desde, detalle.Cuadricula[0].Fecha);
            Assert.Equal(Hasta, detalle.Cuadricula[^1].Fecha);
            Assert.Equal(
                Enumerable.Range(0, dias).Select(desplazamiento => detalle.Desde.AddDays(desplazamiento)),
                detalle.Cuadricula.Select(dia => dia.Fecha));
        }
    }

    [Fact]
    public async Task Un_periodo_fuera_de_la_lista_devuelve_400_sin_devolver_la_cuadricula()
    {
        var habito = await CrearAsync(UnDiario("Prueba detalle período inválido"));
        await MarcarAsync(habito.Id, new DateOnly(2001, 6, 10), 1);
        var marcasAntes = await ContarRegistrosAsync(habito.Id);

        var respuesta = await _cliente.GetAsync(Ruta(habito.Id, 45));
        var problema = await JsonAsync(respuesta);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("dias_invalidos", Codigo(problema));
        Assert.Equal("Período inválido", problema.GetProperty("title").GetString());
        Assert.Equal(
            "El período tiene que ser 30, 60 o 365 días: «45» no es uno de ellos.",
            problema.GetProperty("detail").GetString());
        Assert.False(problema.TryGetProperty("cuadricula", out _));
        Assert.Equal(marcasAntes, await ContarRegistrosAsync(habito.Id));
    }

    [Fact]
    public async Task Un_habito_ajeno_o_inexistente_devuelve_el_404_de_habitos()
    {
        // El 18 es de otra cuenta y el seed le dejó marcas: si no hubiera nada que esconder, esto no
        // probaría nada.
        Assert.True(await ContarRegistrosAsync(18) > 0);

        foreach (var id in new[] { 18, 9999 })
        {
            var respuesta = await _cliente.GetAsync(Ruta(id));
            var problema = await JsonAsync(respuesta);

            Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
            Assert.Equal("no_encontrado", Codigo(problema));
            Assert.Equal("Hábito inexistente", problema.GetProperty("title").GetString());
            Assert.Equal(
                $"No hay ningún hábito con id {id} en tu cuenta.",
                problema.GetProperty("detail").GetString());
        }
    }

    [Fact]
    public async Task Un_diario_pinta_cumplido_el_dia_con_registro_y_vacio_el_que_no_lo_tiene()
    {
        // Un contador con meta 10: el registro de 4 no alcanza y el CHECK de la tabla lo deja sin
        // completar, que es el caso que el historial tiene que pintar vacío y no cumplido.
        var habito = await CrearAsync(UnContador("Prueba detalle diario"));
        await MarcarAsync(habito.Id, new DateOnly(2001, 6, 10), 10);
        await MarcarAsync(habito.Id, new DateOnly(2001, 6, 12), 4);

        var detalle = await DetalleAsync(habito.Id);

        Assert.Equal(EstadoCumplido, Estado(detalle, new DateOnly(2001, 6, 10)));
        Assert.Equal(EstadoVacio, Estado(detalle, new DateOnly(2001, 6, 12)));
        // Día pasado, dentro del rango evaluado y sin ninguna marca.
        Assert.Equal(EstadoVacio, Estado(detalle, new DateOnly(2001, 6, 13)));
        Assert.Equal(EstadoVacio, Estado(detalle, Hasta));
    }

    [Fact]
    public async Task Un_semanal_pinta_cumplido_el_dia_marcado_y_neutro_el_que_no_lo_esta()
    {
        // La cuota de un semanal no es diaria: un día sin marca no es una falta (L6).
        var habito = await CrearAsync(UnSemanal("Prueba detalle semanal", veces: 2));
        await MarcarAsync(habito.Id, new DateOnly(2001, 6, 26), 1);

        var detalle = await DetalleAsync(habito.Id);

        Assert.Equal(EstadoCumplido, Estado(detalle, new DateOnly(2001, 6, 26)));
        Assert.Equal(EstadoNeutro, Estado(detalle, new DateOnly(2001, 6, 27)));
        Assert.Equal(EstadoNeutro, Estado(detalle, new DateOnly(2001, 6, 28)));
    }

    [Fact]
    public async Task Los_dias_anteriores_al_primer_registro_son_neutros_y_no_cuentan()
    {
        var habito = await CrearAsync(UnDiario("Prueba detalle sin pasado"));
        await MarcarAsync(habito.Id, new DateOnly(2001, 6, 25), 1);
        await MarcarAsync(habito.Id, new DateOnly(2001, 6, 26), 1);

        var detalle = await DetalleAsync(habito.Id);

        // El período se evalúa desde max(desde, primer registro): seis días, no treinta (L5).
        Assert.Equal(6, detalle.Evaluables);
        Assert.Equal(2, detalle.Cumplidos);
        Assert.Equal(24, detalle.Cuadricula.Count(dia => dia.Estado == EstadoNeutro));
        Assert.All(
            detalle.Cuadricula.Where(dia => dia.Fecha < new DateOnly(2001, 6, 25)),
            dia => Assert.Equal(EstadoNeutro, dia.Estado));
    }

    [Fact]
    public async Task El_porcentaje_diario_redondea_a_un_decimal()
    {
        // Del 28 al 30 hay tres días evaluables y dos cumplidos: 66,6… se informa con un decimal.
        var habito = await CrearAsync(UnDiario("Prueba detalle porcentaje diario"));
        await MarcarAsync(habito.Id, new DateOnly(2001, 6, 28), 1);
        await MarcarAsync(habito.Id, new DateOnly(2001, 6, 30), 1);

        var detalle = await DetalleAsync(habito.Id);

        Assert.Equal(3, detalle.Evaluables);
        Assert.Equal(2, detalle.Cumplidos);
        Assert.Equal(66.7m, detalle.Porcentaje);
        Assert.Equal(EstadoVacio, Estado(detalle, new DateOnly(2001, 6, 29)));
    }

    [Fact]
    public async Task El_porcentaje_semanal_cuenta_semanas_con_la_cuota_cumplida()
    {
        // Primer registro el lunes 18: entran dos semanas —la del 18 y la del 25— y sólo la primera
        // alcanza la cuota de dos (L7).
        var habito = await CrearAsync(UnSemanal("Prueba detalle porcentaje semanal", veces: 2));
        await MarcarAsync(habito.Id, new DateOnly(2001, 6, 18), 1);
        await MarcarAsync(habito.Id, new DateOnly(2001, 6, 19), 1);
        await MarcarAsync(habito.Id, new DateOnly(2001, 6, 26), 1);

        var detalle = await DetalleAsync(habito.Id);

        Assert.Equal(2, detalle.Evaluables);
        Assert.Equal(1, detalle.Cumplidos);
        Assert.Equal(50m, detalle.Porcentaje);
        Assert.Equal(EstadoCumplido, Estado(detalle, new DateOnly(2001, 6, 19)));
        Assert.Equal(EstadoNeutro, Estado(detalle, new DateOnly(2001, 6, 27)));
    }

    [Fact]
    public async Task Un_habito_sin_registros_no_tiene_nada_evaluable()
    {
        var habito = await CrearAsync(UnDiario("Prueba detalle sin registros"));

        var detalle = await DetalleAsync(habito.Id);

        Assert.All(detalle.Cuadricula, dia => Assert.Equal(EstadoNeutro, dia.Estado));
        Assert.Equal(0, detalle.Evaluables);
        Assert.Equal(0, detalle.Cumplidos);
        Assert.Equal(0m, detalle.Porcentaje);
    }

    [Fact]
    public async Task Una_fecha_mal_escrita_devuelve_400_sin_devolver_la_cuadricula()
    {
        var habito = await CrearAsync(UnDiario("Prueba detalle fecha inválida"));
        await MarcarAsync(habito.Id, new DateOnly(2001, 6, 10), 1);
        var marcasAntes = await ContarRegistrosAsync(habito.Id);

        var respuesta = await _cliente.GetAsync(
            $"/api/habitos/{habito.Id}/historial?dias=30&hasta=2001-13-01");
        var problema = await JsonAsync(respuesta);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("fecha_invalida", Codigo(problema));
        Assert.Equal("Fecha inválida", problema.GetProperty("title").GetString());
        Assert.False(problema.TryGetProperty("cuadricula", out _));
        Assert.Equal(marcasAntes, await ContarRegistrosAsync(habito.Id));
    }

    /// <summary>
    /// El listado paginado de HIST-01 sigue respondiendo igual: el detalle comparte el controlador y el
    /// repositorio, así que el cambio no puede alterar su contrato.
    /// </summary>
    [Fact]
    public async Task El_listado_paginado_del_historial_sigue_igual()
    {
        var respuesta = await _cliente.GetAsync("/api/historial?tamanioDePagina=1");
        var pagina = await JsonAsync(respuesta);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Single(pagina.GetProperty("elementos").EnumerateArray());
        Assert.True(pagina.GetProperty("totalDeRegistros").GetInt32() > 1);
    }

    private const string EstadoCumplido = "cumplido";

    private const string EstadoVacio = "vacio";

    private const string EstadoNeutro = "neutro";

    private static string Ruta(int habitoId, int dias = 30, string hasta = "2001-06-30") =>
        $"/api/habitos/{habitoId}/historial?dias={dias}&hasta={hasta}";

    private async Task<DetalleDePrueba> DetalleAsync(int habitoId, int dias = 30)
    {
        var ruta = Ruta(habitoId, dias);
        var respuesta = await _cliente.GetAsync(ruta);

        Assert.True(
            respuesta.IsSuccessStatusCode,
            $"GET {ruta} respondió {(int)respuesta.StatusCode}: {await respuesta.Content.ReadAsStringAsync()}");

        var detalle = await respuesta.Content.ReadFromJsonAsync<DetalleDePrueba>();
        Assert.NotNull(detalle);

        return detalle;
    }

    private static string Estado(DetalleDePrueba detalle, DateOnly fecha) =>
        Assert.Single(detalle.Cuadricula, dia => dia.Fecha == fecha).Estado;

    private static string? Codigo(JsonElement problema) => problema.GetProperty("codigo").GetString();

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage respuesta) =>
        await respuesta.Content.ReadFromJsonAsync<JsonElement>();

    private Task MarcarAsync(int habitoId, DateOnly fecha, decimal valor) =>
        _cliente.PutAsJsonAsync($"/api/habitos/{habitoId}/registros/{fecha:yyyy-MM-dd}", new { valor });

    private static HabitoEntradaDePrueba UnDiario(string titulo) =>
        new(titulo, null, 1, "si_no", null, null, new FrecuenciaDePrueba("diaria", null));

    private static HabitoEntradaDePrueba UnContador(string titulo) =>
        new(titulo, null, 1, "contador", 10, "páginas", new FrecuenciaDePrueba("diaria", null));

    private static HabitoEntradaDePrueba UnSemanal(string titulo, int veces) =>
        new(titulo, null, 1, "si_no", null, null, new FrecuenciaDePrueba("semanal", veces));

    private async Task<HabitoDePrueba> CrearAsync(HabitoEntradaDePrueba entrada)
    {
        var respuesta = await _cliente.PostAsJsonAsync("/api/habitos", entrada);
        respuesta.EnsureSuccessStatusCode();

        return (await respuesta.Content.ReadFromJsonAsync<HabitoDePrueba>())!;
    }

    private static async Task<long> ContarRegistrosAsync(int habitoId)
    {
        await using var conexion = new MySqlConnection(BaseDeDatosDePrueba.CadenaDeConexion);
        await conexion.OpenAsync();

        await using var comando = new MySqlCommand(
            "SELECT COUNT(*) FROM registro_habito WHERE habito_id = @habitoId",
            conexion);
        comando.Parameters.AddWithValue("@habitoId", habitoId);

        return (long)(await comando.ExecuteScalarAsync())!;
    }

    /// <summary>Borra los hábitos que crean estas pruebas y sus marcas, si alguna quedó.</summary>
    private static async Task LimpiarAsync()
    {
        await using var conexion = new MySqlConnection(BaseDeDatosDePrueba.CadenaDeConexion);
        await conexion.OpenAsync();

        await using var registros = new MySqlCommand(
            "DELETE FROM registro_habito WHERE habito_id IN " +
            "(SELECT id FROM habito WHERE titulo LIKE 'Prueba detalle %')",
            conexion);
        await registros.ExecuteNonQueryAsync();

        await using var habitos = new MySqlCommand(
            "DELETE FROM habito WHERE titulo LIKE 'Prueba detalle %'",
            conexion);
        await habitos.ExecuteNonQueryAsync();
    }
}

/// <summary>
/// El contrato JSON del detalle, escrito a mano como en el resto de las pruebas: si el endpoint
/// cambia de forma, la prueba falla antes que la pantalla.
/// </summary>
internal sealed record DetalleDePrueba(
    int HabitoId,
    string Habito,
    string Tipo,
    FrecuenciaDePrueba Frecuencia,
    int Dias,
    DateOnly Desde,
    DateOnly Hasta,
    int Cumplidos,
    int Evaluables,
    decimal Porcentaje,
    List<DiaDeLaCuadricula> Cuadricula);

internal sealed record DiaDeLaCuadricula(DateOnly Fecha, string Estado);
