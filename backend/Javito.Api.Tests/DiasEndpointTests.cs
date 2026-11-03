using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Javito.Api.Tests.Infraestructura;
using MySqlConnector;

namespace Javito.Api.Tests;

/// <summary>
/// S1: el día se lee como recurso propio. Una prueba por regla de la proyección: qué hábitos entran,
/// qué semana se cuenta y qué informa cuando no hay registro.
/// Va en la colección de categorías porque afirma sobre filas exactas del seed y la escritura de
/// hábitos (T3) suma filas en la misma base.
/// </summary>
[Collection(ColeccionDeCategorias.Nombre)]
public class DiasEndpointTests(FabricaDeApi fabrica) : IAsyncLifetime
{
    private readonly HttpClient _cliente = fabrica.CreateClient();

    public async Task InitializeAsync() => await SesionDePrueba.ClienteAutenticadoAsync(_cliente);

    public Task DisposeAsync()
    {
        _cliente.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Sin_token_el_dia_devuelve_401()
    {
        using var anonimo = fabrica.CreateClient();

        // El formato va explícito: interpolar un DateOnly usa la cultura de la máquina («1/11/2026»)
        // y la ruta no encontraría ninguna acción.
        var respuesta = await anonimo.GetAsync($"/api/dias/{await HoyAsync():yyyy-MM-dd}");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task El_dia_trae_solo_mis_habitos_activos()
    {
        var dia = await DiaAsync(await HoyAsync());

        // El seed del usuario 2 tiene diecisiete hábitos: dieciséis activos y «Correr» (17) archivado.
        Assert.Equal(Enumerable.Range(1, 16), dia.Habitos.Select(habito => habito.Id).Order());
    }

    [Fact]
    public async Task El_dia_nunca_trae_habitos_de_otra_cuenta()
    {
        using var otraCuenta = fabrica.CreateClient();
        var token = await SesionDePrueba.IniciarAsync(otraCuenta, "otra@javito.local");
        otraCuenta.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dia = await otraCuenta.GetFromJsonAsync<DiaDePrueba>($"/api/dias/{await HoyAsync():yyyy-MM-dd}");

        Assert.NotNull(dia);
        Assert.Equal([18, 19], dia.Habitos.Select(habito => habito.Id).Order());
    }

    [Fact]
    public async Task El_objetivo_semanal_es_la_cuota_del_semanal_o_siete_en_el_diario()
    {
        var dia = await DiaAsync(await HoyAsync());

        var diario = Assert.Single(dia.Habitos, habito => habito.Id == 1);
        Assert.Equal(new FrecuenciaDePrueba("diaria", null), diario.Frecuencia);
        Assert.Equal(7, diario.Semana.Objetivo);

        var semanal = Assert.Single(dia.Habitos, habito => habito.Id == 12);
        Assert.Equal(new FrecuenciaDePrueba("semanal", 3), semanal.Frecuencia);
        Assert.Equal(3, semanal.Semana.Objetivo);
    }

    [Fact]
    public async Task La_semana_cuenta_de_lunes_a_domingo_y_solo_los_dias_completados()
    {
        var hoy = await HoyAsync();
        var dia = await DiaAsync(hoy);

        // El seed arma las semanas a mano para que cada una cuente una historia: «Ejercicio pesado»
        // va 2 de 3 esta semana y cumplió 3 la anterior; «Practicar dibujo» retoma con 1 de 2. La
        // semana en curso se siembra en lunes y martes, los días que ya transcurrieron sin importar
        // el día en que se corra, así que lo esperado se calcula: un lunes cuenta 1 y de martes en
        // adelante 2. Un conteo de «últimos 7 días» daría 3 o más en vez de 1 o 2.
        var diasTranscurridos = ((int)hoy.DayOfWeek + 6) % 7 + 1;
        var cumplidosEsperados = Math.Min(2, diasTranscurridos);

        var ejercicio = Assert.Single(dia.Habitos, habito => habito.Id == 12);
        Assert.Equal(cumplidosEsperados, ejercicio.Semana.Cumplidos);
        Assert.Equal(1, Assert.Single(dia.Habitos, habito => habito.Id == 13).Semana.Cumplidos);
        Assert.Equal(1, Assert.Single(dia.Habitos, habito => habito.Id == 14).Semana.Cumplidos);
    }

    [Fact]
    public async Task Sin_registro_el_dia_informa_null_y_con_registro_deriva_el_completado()
    {
        var dia = await DiaAsync(await HoyAsync());

        // El seed marca alrededor del 70% de los hábitos diarios: las dos ramas existen siempre.
        Assert.Contains(dia.Habitos, habito => habito.Registro is null);

        var marcados = dia.Habitos.Where(habito => habito.Registro is not null).ToList();
        Assert.NotEmpty(marcados);
        Assert.All(
            marcados,
            habito => Assert.Equal(
                habito.Registro!.Valor >= (habito.Meta ?? 1),
                habito.Registro.Completado));
    }

    [Fact]
    public async Task Un_dia_sin_registros_informa_todos_los_habitos_sin_marca()
    {
        var dia = await DiaAsync(new DateOnly(2000, 1, 1));

        Assert.Equal(16, dia.Habitos.Count);
        Assert.All(dia.Habitos, habito => Assert.Null(habito.Registro));
        Assert.All(dia.Habitos, habito => Assert.Equal(0, habito.Semana.Cumplidos));
    }

    [Fact]
    public async Task Una_fecha_que_no_es_fecha_devuelve_problem_details_400()
    {
        var respuesta = await _cliente.GetAsync("/api/dias/2026-13-01");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("fecha_invalida", problema.GetProperty("codigo").GetString());
    }

    private async Task<DiaDePrueba> DiaAsync(DateOnly fecha)
    {
        var dia = await _cliente.GetFromJsonAsync<DiaDePrueba>($"/api/dias/{fecha:yyyy-MM-dd}");

        Assert.NotNull(dia);
        Assert.Equal(fecha, dia.Fecha);

        return dia;
    }

    /// <summary>
    /// El día se pide con la fecha que conoce MySQL y no con la de .NET: el seed se armó con
    /// <c>CURDATE()</c>, así que sólo coinciden si se pregunta por la misma base.
    /// </summary>
    private static async Task<DateOnly> HoyAsync()
    {
        await using var conexion = new MySqlConnection(BaseDeDatosDePrueba.CadenaDeConexion);
        await conexion.OpenAsync();

        await using var comando = new MySqlCommand("SELECT CURDATE()", conexion);

        return DateOnly.FromDateTime((DateTime)(await comando.ExecuteScalarAsync())!);
    }
}

/// <summary>
/// El contrato JSON que consume el SPA, escrito acá a mano: si el endpoint cambia de forma, la prueba
/// falla antes que la pantalla.
/// </summary>
internal sealed record DiaDePrueba(DateOnly Fecha, List<HabitoDelDiaDePrueba> Habitos);

internal sealed record HabitoDelDiaDePrueba(
    int Id,
    string Titulo,
    string? Categoria,
    string Tipo,
    decimal? Meta,
    string? Unidad,
    FrecuenciaDePrueba Frecuencia,
    SemanaDePrueba Semana,
    RegistroDePrueba? Registro);

internal sealed record FrecuenciaDePrueba(string Tipo, int? Veces);

internal sealed record SemanaDePrueba(int Cumplidos, int Objetivo);

internal sealed record RegistroDePrueba(decimal Valor, bool Completado);
