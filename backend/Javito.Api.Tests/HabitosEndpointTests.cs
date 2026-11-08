using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Javito.Api.Tests.Infraestructura;
using MySqlConnector;

namespace Javito.Api.Tests;

/// <summary>
/// S6, S7 y la edición (S1-S4): el alta de hábitos, el listado propio y la corrección del hábito sin
/// perder lo registrado. Una prueba por regla: qué filas ven cada cuenta, qué exige cada tipo y cada
/// frecuencia, qué se guarda cuando el tipo no usa meta y qué conserva una edición.
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

        var dia = await _cliente.GetFromJsonAsync<DiaDePrueba>($"/api/dias/{await BaseDeDatosDePrueba.HoyAsync():yyyy-MM-dd}");

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
    public async Task Un_habito_puede_quedar_sin_categoria_al_crearlo_y_al_editarlo()
    {
        // HU-HAB-05: «Un hábito puede quedar sin categoría». Vale en el alta y también para sacarle
        // la categoría a uno que la tenía.
        var alta = await _cliente.PostAsJsonAsync(
            "/api/habitos",
            UnSiNo("Prueba Sin categoría") with { CategoriaId = null });

        Assert.Equal(HttpStatusCode.Created, alta.StatusCode);
        var creado = await alta.Content.ReadFromJsonAsync<HabitoDePrueba>();
        Assert.NotNull(creado);
        Assert.Null(creado.CategoriaId);
        Assert.Null((await BuscarAsync(creado.Id)).CategoriaId);

        var conCategoria = await CrearAsync(UnSiNo("Prueba Le saco la categoría"));
        Assert.Equal(1, conCategoria.CategoriaId);

        var edicion = await _cliente.PutAsJsonAsync(
            $"/api/habitos/{conCategoria.Id}",
            UnSiNo("Prueba Le saco la categoría") with { CategoriaId = null });

        Assert.Equal(HttpStatusCode.OK, edicion.StatusCode);
        Assert.Null((await edicion.Content.ReadFromJsonAsync<HabitoDePrueba>())!.CategoriaId);
        Assert.Null((await BuscarAsync(conCategoria.Id)).CategoriaId);
    }

    [Fact]
    public async Task La_categoria_elegida_tiene_que_existir_y_estar_activa()
    {
        var inexistente = await _cliente.PostAsJsonAsync(
            "/api/habitos",
            UnSiNo("Prueba Categoría rara") with { CategoriaId = 9999 });
        // La 5 es «Espiritualidad», que el seed deja inactiva.
        var inactiva = await _cliente.PostAsJsonAsync(
            "/api/habitos",
            UnSiNo("Prueba Categoría vieja") with { CategoriaId = 5 });

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

    [Fact]
    public async Task Editar_cambia_todos_los_campos_y_lo_devuelve_en_el_listado()
    {
        var creado = await CrearAsync(UnSiNo("Prueba Editar"));

        var respuesta = await _cliente.PutAsJsonAsync(
            $"/api/habitos/{creado.Id}",
            new HabitoEntradaDePrueba(
                "  Prueba Editar corregido ",
                "Nueva descripción",
                3,
                "contador",
                15,
                "minutos",
                new FrecuenciaDePrueba("semanal", 4)));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var editado = await respuesta.Content.ReadFromJsonAsync<HabitoDePrueba>();
        Assert.NotNull(editado);
        Assert.Equal(creado.Id, editado.Id);
        Assert.Equal("Prueba Editar corregido", editado.Titulo);
        Assert.Equal("Nueva descripción", editado.Descripcion);
        Assert.Equal(3, editado.CategoriaId);
        Assert.Equal("contador", editado.Tipo);
        Assert.Equal(15m, editado.Meta);
        Assert.Equal("minutos", editado.Unidad);
        Assert.Equal(new FrecuenciaDePrueba("semanal", 4), editado.Frecuencia);
        Assert.True(editado.Activo);

        // El listado lo confirma: no quedó sólo en la respuesta del PUT.
        var listado = await _cliente.GetFromJsonAsync<List<HabitoDePrueba>>("/api/habitos");
        Assert.Equal(editado, Assert.Single(listado!, habito => habito.Id == creado.Id));
    }

    [Fact]
    public async Task Un_dia_ya_registrado_conserva_la_meta_con_la_que_se_cerro()
    {
        var creado = await CrearAsync(UnContador("Prueba Meta", meta: 3, unidad: "vasos"));
        var hoy = await BaseDeDatosDePrueba.HoyAsync();
        var ayer = hoy.AddDays(-1);

        var marca = await _cliente.PutAsJsonAsync(
            $"/api/habitos/{creado.Id}/registros/{ayer:yyyy-MM-dd}",
            new { valor = 3 });
        Assert.Equal(HttpStatusCode.OK, marca.StatusCode);

        var edicion = await _cliente.PutAsJsonAsync(
            $"/api/habitos/{creado.Id}",
            UnContador("Prueba Meta", meta: 5, unidad: "vasos"));
        Assert.Equal(HttpStatusCode.OK, edicion.StatusCode);

        // El día cerrado con meta 3 la conserva: el pasado no se reescribe.
        var delAyer = Assert.Single((await DiaAsync(ayer)).Habitos, habito => habito.Id == creado.Id);
        Assert.Equal(3m, delAyer.Meta);
        Assert.Equal(3m, delAyer.Registro!.Valor);
        Assert.True(delAyer.Registro.Completado);

        // La meta nueva rige desde el próximo registro: hoy, sin marca, se muestra la vigente.
        var deHoy = Assert.Single((await DiaAsync(hoy)).Habitos, habito => habito.Id == creado.Id);
        Assert.Equal(5m, deHoy.Meta);
        Assert.Null(deHoy.Registro);
    }

    [Fact]
    public async Task Cambiar_el_tipo_conserva_los_registros_anteriores()
    {
        var creado = await CrearAsync(UnContador("Prueba Cambio de tipo", meta: 3, unidad: "vasos"));
        var ayer = (await BaseDeDatosDePrueba.HoyAsync()).AddDays(-1);

        await _cliente.PutAsJsonAsync(
            $"/api/habitos/{creado.Id}/registros/{ayer:yyyy-MM-dd}",
            new { valor = 2 });

        var edicion = await _cliente.PutAsJsonAsync(
            $"/api/habitos/{creado.Id}",
            UnSiNo("Prueba Cambio de tipo"));

        Assert.Equal(HttpStatusCode.OK, edicion.StatusCode);

        // La marca del contador sigue ahí con su valor y su completado: cambiar el tipo no borra
        // lo ya registrado (S3).
        var delAyer = Assert.Single((await DiaAsync(ayer)).Habitos, habito => habito.Id == creado.Id);
        Assert.Equal("si_no", delAyer.Tipo);
        Assert.Null(delAyer.Meta);
        Assert.Equal(2m, delAyer.Registro!.Valor);
        Assert.False(delAyer.Registro.Completado);
    }

    [Fact]
    public async Task Editar_el_habito_ajeno_o_inexistente_da_404()
    {
        var ajeno = await CrearAsync(UnSiNo("Prueba Ajeno"));

        using var otraCuenta = fabrica.CreateClient();
        var token = await SesionDePrueba.IniciarAsync(otraCuenta, "otra@javito.local");
        otraCuenta.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var deOtraCuenta = await otraCuenta.PutAsJsonAsync(
            $"/api/habitos/{ajeno.Id}",
            UnSiNo("Prueba Robado"));
        var inexistente = await _cliente.PutAsJsonAsync("/api/habitos/9999", UnSiNo("Prueba Fantasma"));

        // Uno ajeno no existe para quien pregunta: mismo 404 y mismo código que un id inventado.
        Assert.Equal(HttpStatusCode.NotFound, deOtraCuenta.StatusCode);
        Assert.Equal("no_encontrado", await CodigoAsync(deOtraCuenta));
        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
        Assert.Equal("no_encontrado", await CodigoAsync(inexistente));

        Assert.Equal("Prueba Ajeno", (await BuscarAsync(ajeno.Id)).Titulo);
    }

    [Fact]
    public async Task La_edicion_conserva_la_categoria_inactiva_o_sin_categoria()
    {
        // L4: «el hábito conserva la categoría que eligió, aunque después se desactive». El seed
        // tiene «Meditar» (11) en la 5, inactiva, y «Té de hierbas consciente» (10) sin categoría.
        // Se los edita con sus mismos valores, así que la fila queda igual.
        var listado = await _cliente.GetFromJsonAsync<List<HabitoDePrueba>>("/api/habitos");
        Assert.NotNull(listado);

        var conInactiva = await _cliente.PutAsJsonAsync(
            "/api/habitos/11",
            DatosDe(listado.Single(habito => habito.Id == 11)));
        var sinCategoria = await _cliente.PutAsJsonAsync(
            "/api/habitos/10",
            DatosDe(listado.Single(habito => habito.Id == 10)));

        Assert.Equal(HttpStatusCode.OK, conInactiva.StatusCode);
        Assert.Equal(5, (await conInactiva.Content.ReadFromJsonAsync<HabitoDePrueba>())!.CategoriaId);
        Assert.Equal(HttpStatusCode.OK, sinCategoria.StatusCode);
        Assert.Null((await sinCategoria.Content.ReadFromJsonAsync<HabitoDePrueba>())!.CategoriaId);

        // Cambiar a otra categoría exige que exista y esté activa: pasar a la 5 se rechaza y el
        // hábito se queda con la que tenía.
        var creado = await CrearAsync(UnSiNo("Prueba Categoría activa"));
        var aLaInactiva = await _cliente.PutAsJsonAsync(
            $"/api/habitos/{creado.Id}",
            UnSiNo("Prueba Categoría activa") with { CategoriaId = 5 });

        Assert.Equal(HttpStatusCode.BadRequest, aLaInactiva.StatusCode);
        Assert.Equal("categoria_inactiva", await CodigoAsync(aLaInactiva));
        Assert.Equal(1, (await BuscarAsync(creado.Id)).CategoriaId);
    }

    [Fact]
    public async Task La_edicion_reutiliza_la_validacion_del_alta()
    {
        var creado = await CrearAsync(UnSiNo("Prueba Validación"));

        var sinTitulo = await _cliente.PutAsJsonAsync($"/api/habitos/{creado.Id}", UnSiNo("   "));
        // El tipo manda sobre meta y unidad también al editar: volverse contador exige meta.
        var sinMeta = await _cliente.PutAsJsonAsync(
            $"/api/habitos/{creado.Id}",
            UnSiNo("Prueba Validación") with { Tipo = "contador" });

        Assert.Equal(HttpStatusCode.BadRequest, sinTitulo.StatusCode);
        var problema = await sinTitulo.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(problema.GetProperty("errors").TryGetProperty("Titulo", out _));
        Assert.Equal("meta_requerida", await CodigoAsync(sinMeta));

        // Ninguno de los dos intentos se guardó.
        var sinCambios = await BuscarAsync(creado.Id);
        Assert.Equal("Prueba Validación", sinCambios.Titulo);
        Assert.Equal("si_no", sinCambios.Tipo);
    }

    [Fact]
    public async Task Un_habito_archivado_tambien_se_edita_y_sigue_archivado()
    {
        // L5: el archivo es de #70, así que la edición no lo deshace. «Correr» (17) viene archivado
        // del seed y se lo edita con sus mismos valores, así la fila queda igual.
        var listado = await _cliente.GetFromJsonAsync<List<HabitoDePrueba>>("/api/habitos");
        Assert.NotNull(listado);
        var archivado = listado.Single(habito => habito.Id == 17);
        Assert.False(archivado.Activo);

        var respuesta = await _cliente.PutAsJsonAsync("/api/habitos/17", DatosDe(archivado));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var editado = await respuesta.Content.ReadFromJsonAsync<HabitoDePrueba>();
        Assert.NotNull(editado);
        Assert.False(editado.Activo);
        Assert.Equal("Correr", editado.Titulo);
    }

    private static HabitoEntradaDePrueba UnSiNo(string titulo) =>
        new(titulo, null, 1, "si_no", null, null, new FrecuenciaDePrueba("diaria", null));

    private static HabitoEntradaDePrueba UnContador(string titulo, decimal? meta, string? unidad) =>
        new(titulo, null, 1, "contador", meta, unidad, new FrecuenciaDePrueba("diaria", null));

    /// <summary>Los datos de un hábito existente, para editarlo sin cambiarle nada.</summary>
    private static HabitoEntradaDePrueba DatosDe(HabitoDePrueba habito) =>
        new(
            habito.Titulo,
            habito.Descripcion,
            habito.CategoriaId,
            habito.Tipo,
            habito.Meta,
            habito.Unidad,
            habito.Frecuencia);

    private async Task<HabitoDePrueba> CrearAsync(HabitoEntradaDePrueba entrada)
    {
        var respuesta = await _cliente.PostAsJsonAsync("/api/habitos", entrada);
        respuesta.EnsureSuccessStatusCode();

        return (await respuesta.Content.ReadFromJsonAsync<HabitoDePrueba>())!;
    }

    private async Task<HabitoDePrueba> BuscarAsync(int id)
    {
        var habitos = await _cliente.GetFromJsonAsync<List<HabitoDePrueba>>("/api/habitos");

        return Assert.Single(habitos!, habito => habito.Id == id);
    }

    private async Task<DiaDePrueba> DiaAsync(DateOnly fecha)
    {
        var dia = await _cliente.GetFromJsonAsync<DiaDePrueba>($"/api/dias/{fecha:yyyy-MM-dd}");

        Assert.NotNull(dia);

        return dia;
    }

    private static async Task<string?> CodigoAsync(HttpResponseMessage respuesta)
    {
        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();

        return problema.GetProperty("codigo").GetString();
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
