using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Javito.Api.Tests.Infraestructura;
using MySqlConnector;

namespace Javito.Api.Tests;

/// <summary>
/// S1…S4 de HIST-01: el historial se pagina en el servidor, se filtra por hábito y por rango de
/// fechas, sólo trae lo propio y cada fila lleva la meta con la que se cerró ese día. Las pruebas
/// escriben marcas en junio de 2001, donde el seed no sembró nada, y limpian lo suyo. Los hábitos que
/// usan son del seed: el 1 es sí/no, el 15 es contador con meta 10, el 17 está archivado y el 18 es de
/// otra cuenta.
/// </summary>
[Collection(ColeccionDeCategorias.Nombre)]
public class HistorialEndpointTests(FabricaDeApi fabrica) : IAsyncLifetime
{
    private static readonly DateOnly UnLunes = new(2001, 6, 4);

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
    public async Task El_historial_viene_paginado_desde_el_servidor()
    {
        // Siete marcas propias más lo que sembró el seed: la primera página no puede traerlas todas.
        for (var dia = 0; dia < 7; dia++)
        {
            await MarcarAsync(1, UnLunes.AddDays(dia), 1);
        }

        var primera = await PedirAsync("/api/historial?tamanioDePagina=6");
        var segunda = await PedirAsync("/api/historial?pagina=2&tamanioDePagina=6");

        Assert.Equal(6, Elementos(primera).Count);
        Assert.Equal(1, Numero(primera, "pagina"));
        Assert.Equal(6, Numero(primera, "tamanioDePagina"));

        var total = Numero(primera, "totalDeRegistros");
        Assert.True(total > 6, "El historial del seed tiene más registros que una página.");
        Assert.Equal((total + 5) / 6, Numero(primera, "totalDePaginas"));

        // Sin desempate estable la paginación repite o saltea filas entre dos requests.
        var clavesDeLaPrimera = Claves(primera);
        var clavesDeLaSegunda = Claves(segunda);
        Assert.NotEmpty(clavesDeLaSegunda);
        Assert.Empty(clavesDeLaPrimera.Intersect(clavesDeLaSegunda));
    }

    [Fact]
    public async Task Se_filtra_por_habito()
    {
        await MarcarAsync(1, UnLunes, 1);
        await MarcarAsync(15, UnLunes, 4);

        var delContador = await PedirAsync(
            $"/api/historial?habitoId=15&desde={UnLunes:yyyy-MM-dd}&hasta={UnLunes:yyyy-MM-dd}");
        var delSiNo = await PedirAsync(
            $"/api/historial?habitoId=1&desde={UnLunes:yyyy-MM-dd}&hasta={UnLunes:yyyy-MM-dd}");

        var contador = Assert.Single(Elementos(delContador));
        Assert.Equal(15, Habito(contador));
        Assert.Equal(4m, contador.GetProperty("valor").GetDecimal());

        var siNo = Assert.Single(Elementos(delSiNo));
        Assert.Equal(1, Habito(siNo));
        Assert.True(siNo.GetProperty("completado").GetBoolean());
    }

    [Fact]
    public async Task Se_filtra_por_rango_de_fechas_con_los_extremos_incluidos()
    {
        var lunes = UnLunes;
        var martes = UnLunes.AddDays(1);
        var miercoles = UnLunes.AddDays(2);
        await MarcarAsync(1, lunes, 1);
        await MarcarAsync(1, martes, 1);
        await MarcarAsync(1, miercoles, 1);

        var acotado = await PedirAsync(
            $"/api/historial?habitoId=1&desde={martes:yyyy-MM-dd}&hasta={miercoles:yyyy-MM-dd}");

        Assert.Equal(
            new[] { miercoles.ToString("yyyy-MM-dd"), martes.ToString("yyyy-MM-dd") },
            Fechas(acotado));

        // Un solo extremo también filtra, y el seed de este hábito vive cerca de hoy: acotar arriba
        // lo deja afuera, y acotar abajo lo deja adentro. Por eso se afirma sobre las fechas propias.
        var soloDesde = await PedirAsync($"/api/historial?habitoId=1&tamanioDePagina=50&desde={martes:yyyy-MM-dd}");
        Assert.Contains(martes.ToString("yyyy-MM-dd"), Fechas(soloDesde));
        Assert.Contains(miercoles.ToString("yyyy-MM-dd"), Fechas(soloDesde));
        Assert.DoesNotContain(lunes.ToString("yyyy-MM-dd"), Fechas(soloDesde));

        var soloHasta = await PedirAsync($"/api/historial?habitoId=1&tamanioDePagina=50&hasta={martes:yyyy-MM-dd}");
        Assert.Equal(new[] { martes.ToString("yyyy-MM-dd"), lunes.ToString("yyyy-MM-dd") },
            Fechas(soloHasta));
    }

    [Fact]
    public async Task Solo_se_ven_los_registros_propios()
    {
        // El seed marca los hábitos de la otra cuenta: si no hubiera nada que esconder, esto no probaría nada.
        Assert.True(await ContarRegistrosAsync(18) > 0);

        var ajeno = await PedirAsync("/api/historial?habitoId=18");
        var todo = await PedirAsync("/api/historial?tamanioDePagina=50");

        Assert.Empty(Elementos(ajeno));
        Assert.Equal(0, Numero(ajeno, "totalDeRegistros"));
        // 18, 19 y 20 son de otras cuentas (database.sql:, ver el seed).
        Assert.DoesNotContain(Elementos(todo), fila => Habito(fila) is 18 or 19 or 20);
    }

    [Fact]
    public async Task Cada_fila_lleva_la_meta_con_la_que_se_cerro_ese_dia()
    {
        // La meta del hábito cambia después de marcar: la fila del historial no se reescribe.
        await CambiarMetaDelContadorAsync(3m);

        try
        {
            await MarcarAsync(15, UnLunes, 2);
        }
        finally
        {
            await CambiarMetaDelContadorAsync(10m);
        }

        var pagina = await PedirAsync(
            $"/api/historial?habitoId=15&desde={UnLunes:yyyy-MM-dd}&hasta={UnLunes:yyyy-MM-dd}");

        var fila = Assert.Single(Elementos(pagina));
        Assert.Equal(UnLunes.ToString("yyyy-MM-dd"), fila.GetProperty("fecha").GetString());
        Assert.Equal(15, Habito(fila));
        Assert.Equal("Leer", fila.GetProperty("habito").GetString());
        Assert.Equal(2m, fila.GetProperty("valor").GetDecimal());
        Assert.Equal(3m, fila.GetProperty("metaObjetivo").GetDecimal());
        Assert.False(fila.GetProperty("completado").GetBoolean());
    }

    [Fact]
    public async Task Un_habito_archivado_sigue_en_el_historial()
    {
        // El 17 está archivado y el seed le dejó dos semanas de marcas (database.sql, semanales).
        var pagina = await PedirAsync("/api/historial?habitoId=17");

        Assert.NotEmpty(Elementos(pagina));
        Assert.All(Elementos(pagina), fila => Assert.Equal(17, Habito(fila)));
    }

    [Fact]
    public async Task Un_rango_invertido_devuelve_400_sin_devolver_filas()
    {
        await MarcarAsync(1, UnLunes, 1);

        var respuesta = await _cliente.GetAsync(
            $"/api/historial?desde={UnLunes.AddDays(2):yyyy-MM-dd}&hasta={UnLunes:yyyy-MM-dd}");
        var problema = await JsonAsync(respuesta);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("rango_de_fechas_invalido", problema.GetProperty("codigo").GetString());
        Assert.False(problema.TryGetProperty("elementos", out _));
    }

    [Fact]
    public async Task Una_fecha_mal_escrita_devuelve_400()
    {
        var respuesta = await _cliente.GetAsync("/api/historial?desde=2001-13-01");
        var problema = await JsonAsync(respuesta);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("fecha_invalida", problema.GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task Una_pagina_fuera_de_rango_devuelve_el_400_de_validacion()
    {
        var respuesta = await _cliente.GetAsync("/api/historial?pagina=0");
        var problema = await JsonAsync(respuesta);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.True(problema.GetProperty("errors").TryGetProperty("Pagina", out var mensajes));
        Assert.NotEmpty(mensajes.EnumerateArray());
    }

    private Task MarcarAsync(int habitoId, DateOnly fecha, decimal valor) =>
        _cliente.PutAsJsonAsync($"/api/habitos/{habitoId}/registros/{fecha:yyyy-MM-dd}", new { valor });

    /// <summary>La edición del hábito: el historial no tiene que reescribirse con la meta nueva.</summary>
    private async Task CambiarMetaDelContadorAsync(decimal meta)
    {
        var respuesta = await _cliente.PutAsJsonAsync(
            "/api/habitos/15",
            new
            {
                titulo = "Leer",
                descripcion = (string?)null,
                categoriaId = 2,
                tipo = "contador",
                meta,
                unidad = "páginas",
                frecuencia = new { tipo = "diaria", veces = (int?)null }
            });

        respuesta.EnsureSuccessStatusCode();
    }

    private async Task<JsonElement> PedirAsync(string ruta)
    {
        var respuesta = await _cliente.GetAsync(ruta);

        Assert.True(
            respuesta.IsSuccessStatusCode,
            $"GET {ruta} respondió {(int)respuesta.StatusCode}: {await respuesta.Content.ReadAsStringAsync()}");

        return await JsonAsync(respuesta);
    }

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage respuesta) =>
        await respuesta.Content.ReadFromJsonAsync<JsonElement>();

    private static int Numero(JsonElement pagina, string nombre) =>
        pagina.GetProperty(nombre).GetInt32();

    private static List<JsonElement> Elementos(JsonElement pagina) =>
        pagina.GetProperty("elementos").EnumerateArray().ToList();

    private static int Habito(JsonElement fila) => fila.GetProperty("habitoId").GetInt32();

    private static string[] Fechas(JsonElement pagina) =>
        Elementos(pagina).Select(fila => fila.GetProperty("fecha").GetString()!).ToArray();

    /// <summary>La identidad de una fila: un hábito se marca una sola vez por día.</summary>
    private static HashSet<string> Claves(JsonElement pagina) =>
        Elementos(pagina)
            .Select(fila => $"{fila.GetProperty("fecha").GetString()}/{Habito(fila)}")
            .ToHashSet();

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

    /// <summary>Borra sólo la ventana que escriben estas pruebas: el seed vive cerca de hoy.</summary>
    private static async Task LimpiarAsync()
    {
        await using var conexion = new MySqlConnection(BaseDeDatosDePrueba.CadenaDeConexion);
        await conexion.OpenAsync();

        await using var comando = new MySqlCommand(
            "DELETE FROM registro_habito WHERE fecha BETWEEN '2001-06-01' AND '2001-06-30'",
            conexion);
        await comando.ExecuteNonQueryAsync();
    }
}
