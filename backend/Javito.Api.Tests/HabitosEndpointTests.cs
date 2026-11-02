using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Javito.Api.Tests.Infraestructura;
using MySqlConnector;

namespace Javito.Api.Tests;

/// <summary>
/// S6 y S7: el alta de hábitos y el listado propio. Una prueba por regla: qué filas ven cada cuenta,
/// qué exige cada tipo y cada frecuencia, y qué se guarda cuando el tipo no usa meta.
/// Lo que crea lleva el prefijo «Prueba » y la clase limpia lo suyo: el listado afirma filas del seed.
/// </summary>
[Collection(ColeccionDeCategorias.Nombre)]
public class HabitosEndpointTests(FabricaDeApi fabrica) : IAsyncLifetime
{
    private const string Prefijo = "Prueba ";

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
    public async Task El_listado_trae_mis_diecisiete_habitos_incluido_el_archivado()
    {
        var habitos = await _cliente.GetFromJsonAsync<List<HabitoDePrueba>>("/api/habitos");

        Assert.NotNull(habitos);
        Assert.Equal(Enumerable.Range(1, 17), habitos.Select(habito => habito.Id).Order());
        Assert.Contains(habitos, habito => habito is { Id: 17, Activo: false });
    }

    [Fact]
    public async Task El_listado_filtra_por_activo()
    {
        var activos = await _cliente.GetFromJsonAsync<List<HabitoDePrueba>>("/api/habitos?activo=true");
        var archivados = await _cliente.GetFromJsonAsync<List<HabitoDePrueba>>("/api/habitos?activo=false");

        Assert.Equal(16, activos!.Count);
        Assert.All(activos, habito => Assert.True(habito.Activo));
        Assert.Equal(17, Assert.Single(archivados!).Id);
    }

    [Fact]
    public async Task El_listado_informa_categoria_y_frecuencia()
    {
        var habitos = await _cliente.GetFromJsonAsync<List<HabitoDePrueba>>("/api/habitos");

        var semanal = Assert.Single(habitos!, habito => habito.Id == 12);
        Assert.Equal(1, semanal.CategoriaId);
        Assert.Equal(new FrecuenciaDePrueba("semanal", 3), semanal.Frecuencia);

        // «Té de hierbas consciente» viene sin categoría del seed (HU-HAB-05).
        Assert.Null(Assert.Single(habitos!, habito => habito.Id == 10).CategoriaId);
    }

    [Fact]
    public async Task Crear_guarda_el_habito_y_lo_devuelve()
    {
        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/habitos",
            UnContador("Prueba Leer", meta: 10, unidad: "páginas"));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);

        var creado = await respuesta.Content.ReadFromJsonAsync<HabitoDePrueba>();
        Assert.NotNull(creado);
        Assert.True(creado.Id > 0);
        Assert.Equal("Prueba Leer", creado.Titulo);
        Assert.Equal("contador", creado.Tipo);
        Assert.Equal(10m, creado.Meta);
        Assert.Equal("páginas", creado.Unidad);
        Assert.Equal(new FrecuenciaDePrueba("diaria", null), creado.Frecuencia);
        Assert.True(creado.Activo);
    }

    [Fact]
    public async Task Un_habito_creado_aparece_en_el_dia()
    {
        await _cliente.PostAsJsonAsync("/api/habitos", UnSiNo("Prueba Caminar"));

        var dia = await _cliente.GetFromJsonAsync<DiaDePrueba>($"/api/dias/{await HoyAsync():yyyy-MM-dd}");

        Assert.NotNull(dia);
        var creado = Assert.Single(dia.Habitos, habito => habito.Titulo == "Prueba Caminar");
        Assert.Null(creado.Registro);
        Assert.Equal(7, creado.Semana.Objetivo);
    }

    [Fact]
    public async Task El_titulo_no_puede_quedar_vacio()
    {
        var respuesta = await _cliente.PostAsJsonAsync("/api/habitos", UnSiNo("   "));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(problema.GetProperty("errors").TryGetProperty("Titulo", out var mensajes));
        Assert.NotEmpty(mensajes.EnumerateArray());
    }

    [Fact]
    public async Task Un_contador_exige_meta_y_unidad()
    {
        var sinMeta = await _cliente.PostAsJsonAsync(
            "/api/habitos",
            UnContador("Prueba Sin meta", meta: null, unidad: "páginas"));
        var sinUnidad = await _cliente.PostAsJsonAsync(
            "/api/habitos",
            UnContador("Prueba Sin unidad", meta: 5, unidad: null));

        Assert.Equal(HttpStatusCode.BadRequest, sinMeta.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, sinUnidad.StatusCode);
        Assert.Equal("meta_requerida", await CodigoAsync(sinMeta));
        Assert.Equal("unidad_requerida", await CodigoAsync(sinUnidad));
    }

    [Fact]
    public async Task La_categoria_es_obligatoria_y_tiene_que_estar_activa()
    {
        var sinCategoria = await _cliente.PostAsJsonAsync(
            "/api/habitos",
            UnSiNo("Prueba Sin categoría") with { CategoriaId = null });
        var inexistente = await _cliente.PostAsJsonAsync(
            "/api/habitos",
            UnSiNo("Prueba Categoría rara") with { CategoriaId = 9999 });
        // La 5 es «Espiritualidad», que el seed deja inactiva.
        var inactiva = await _cliente.PostAsJsonAsync(
            "/api/habitos",
            UnSiNo("Prueba Categoría vieja") with { CategoriaId = 5 });

        Assert.Equal("categoria_requerida", await CodigoAsync(sinCategoria));
        Assert.Equal("categoria_inexistente", await CodigoAsync(inexistente));
        Assert.Equal("categoria_inactiva", await CodigoAsync(inactiva));
    }

    [Fact]
    public async Task La_frecuencia_semanal_exige_entre_uno_y_seis()
    {
        var sinCuota = await _cliente.PostAsJsonAsync(
            "/api/habitos",
            UnSiNo("Prueba Sin cuota") with { Frecuencia = new FrecuenciaDePrueba("semanal", null) });
        var fueraDeRango = await _cliente.PostAsJsonAsync(
            "/api/habitos",
            UnSiNo("Prueba Siete veces") with { Frecuencia = new FrecuenciaDePrueba("semanal", 7) });

        Assert.Equal("frecuencia_fuera_de_rango", await CodigoAsync(sinCuota));
        Assert.Equal("frecuencia_fuera_de_rango", await CodigoAsync(fueraDeRango));
    }

    [Fact]
    public async Task Un_si_no_guarda_meta_y_unidad_nulas()
    {
        // La interfaz no las pide para ese tipo; si igual viajan, no se guardan.
        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/habitos",
            UnSiNo("Prueba Estirar") with { Meta = 5, Unidad = "x" });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);

        var creado = await respuesta.Content.ReadFromJsonAsync<HabitoDePrueba>();
        Assert.Null(creado!.Meta);
        Assert.Null(creado.Unidad);
    }

    private static HabitoEntradaDePrueba UnSiNo(string titulo) =>
        new(titulo, null, 1, "si_no", null, null, new FrecuenciaDePrueba("diaria", null));

    private static HabitoEntradaDePrueba UnContador(string titulo, decimal? meta, string? unidad) =>
        new(titulo, null, 1, "contador", meta, unidad, new FrecuenciaDePrueba("diaria", null));

    private static async Task<string?> CodigoAsync(HttpResponseMessage respuesta)
    {
        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();

        return problema.GetProperty("codigo").GetString();
    }

    private static async Task<DateOnly> HoyAsync()
    {
        await using var conexion = new MySqlConnection(BaseDeDatosDePrueba.CadenaDeConexion);
        await conexion.OpenAsync();

        await using var comando = new MySqlCommand("SELECT CURDATE()", conexion);

        return DateOnly.FromDateTime((DateTime)(await comando.ExecuteScalarAsync())!);
    }

    /// <summary>Borra los hábitos que crean estas pruebas y sus marcas, si alguna quedó.</summary>
    private static async Task LimpiarAsync()
    {
        await using var conexion = new MySqlConnection(BaseDeDatosDePrueba.CadenaDeConexion);
        await conexion.OpenAsync();

        await using var registros = new MySqlCommand(
            "DELETE FROM registro_habito WHERE habito_id IN " +
            "(SELECT id FROM habito WHERE titulo LIKE 'Prueba %')",
            conexion);
        await registros.ExecuteNonQueryAsync();

        await using var habitos = new MySqlCommand(
            "DELETE FROM habito WHERE titulo LIKE 'Prueba %'",
            conexion);
        await habitos.ExecuteNonQueryAsync();
    }
}

/// <summary>El hábito tal como lo expone la API: la categoría viaja por id, no por nombre.</summary>
internal sealed record HabitoDePrueba(
    int Id,
    string Titulo,
    string? Descripcion,
    int? CategoriaId,
    string Tipo,
    decimal? Meta,
    string? Unidad,
    FrecuenciaDePrueba Frecuencia,
    bool Activo);

/// <summary>El cuerpo del alta, tal como lo manda el formulario del SPA.</summary>
internal sealed record HabitoEntradaDePrueba(
    string Titulo,
    string? Descripcion,
    int? CategoriaId,
    string Tipo,
    decimal? Meta,
    string? Unidad,
    FrecuenciaDePrueba Frecuencia);
