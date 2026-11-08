using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Javito.Api.Tests.Infraestructura;
using MySqlConnector;

namespace Javito.Api.Tests;

/// <summary>
/// HU-ART-03: el Administrador retira un artículo sin borrarlo y lo puede reactivar. Una prueba por
/// regla (S24-S26 de odd/tasks/articulos.md); cada una mira la respuesta, lo que ve el lector por la
/// API y la fila cruda cuando hay que afirmar que nada más se tocó.
/// </summary>
[Collection(ColeccionDeArticulos.Nombre)]
public class ArticulosBajaEndpointTests(FabricaDeApi fabrica) : IAsyncLifetime
{
    /// <summary>Lo que crea cada prueba lleva este prefijo; la clase borra sólo lo suyo.</summary>
    private const string Prefijo = "Prueba baja ";

    /// <summary>Categoría activa del seed.</summary>
    private const int CategoriaActiva = 2;

    /// <summary>
    /// Las fechas de los artículos de prueba son futuras a propósito: así quedan siempre arriba de
    /// cualquier listado ordenado por fecha, sea cual sea el seed, y el orden se puede afirmar.
    /// </summary>
    private const string FechaBase = "2099-03-";

    private readonly HttpClient _admin = fabrica.CreateClient();
    private readonly HttpClient _usuario = fabrica.CreateClient();
    private readonly HttpClient _anonimo = fabrica.CreateClient();

    public async Task InitializeAsync()
    {
        await SesionDePrueba.ClienteAdministradorAsync(_admin);
        await SesionDePrueba.ClienteAutenticadoAsync(_usuario);
        await LimpiarAsync();
    }

    public async Task DisposeAsync()
    {
        await LimpiarAsync();
        _admin.Dispose();
        _usuario.Dispose();
        _anonimo.Dispose();
    }

    // --- S24: desactivar -----------------------------------------------------------------------

    [Fact]
    public async Task Desactivar_responde_el_articulo_inactivo_lo_saca_del_catalogo_y_del_detalle_sin_tocar_nada_mas()
    {
        var id = await CrearAsync("Para retirar", dia: 10);
        var antes = await FilaSinActivoAsync(id);

        var respuesta = await CambiarActivoAsync(_admin, id, "{\"activo\":false}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(id, cuerpo.GetProperty("id").GetInt32());
        Assert.False(cuerpo.GetProperty("activo").GetBoolean());

        // Desaparece del catálogo y no se abre por URL: para el lector no existe.
        Assert.DoesNotContain(id, await IdsDelCatalogoAsync());
        var detalle = await _usuario.GetAsync($"/api/articulos/{id}");
        Assert.Equal(HttpStatusCode.NotFound, detalle.StatusCode);
        Assert.Equal("no_encontrado", (await detalle.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("codigo").GetString());

        // La baja es lógica y toca sólo `activo`: la fila sigue, con la misma fecha y los mismos campos.
        Assert.Equal(antes, await FilaSinActivoAsync(id));
        Assert.Equal(0, await ActivoAsync(id));
    }

    // --- S25: reactivar ------------------------------------------------------------------------

    [Fact]
    public async Task Reactivar_devuelve_el_articulo_al_catalogo_con_su_fecha_de_publicacion_original()
    {
        var id = await CrearAsync("Para volver", dia: 11);
        await CambiarActivoAsync(_admin, id, "{\"activo\":false}");
        var antes = await FilaSinActivoAsync(id);

        var respuesta = await CambiarActivoAsync(_admin, id, "{\"activo\":true}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.True((await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("activo").GetBoolean());

        var enCatalogo = (await ElementosDelCatalogoAsync()).Single(e => e.GetProperty("id").GetInt32() == id);
        Assert.Equal($"{FechaBase}11", enCatalogo.GetProperty("fechaPublicacion").GetString());

        var detalle = await _usuario.GetAsync($"/api/articulos/{id}");
        Assert.Equal(HttpStatusCode.OK, detalle.StatusCode);
        Assert.Equal($"{FechaBase}11", (await detalle.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("fechaPublicacion").GetString());

        Assert.Equal(antes, await FilaSinActivoAsync(id));
    }

    [Fact]
    public async Task Cambiar_al_estado_en_que_ya_esta_es_idempotente_y_responde_200()
    {
        var id = await CrearAsync("Idempotente", dia: 12);

        // Activar uno activo.
        var yaActivo = await CambiarActivoAsync(_admin, id, "{\"activo\":true}");
        Assert.Equal(HttpStatusCode.OK, yaActivo.StatusCode);
        Assert.True((await yaActivo.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("activo").GetBoolean());
        Assert.Equal(1, await ActivoAsync(id));

        // Desactivar dos veces seguidas.
        await CambiarActivoAsync(_admin, id, "{\"activo\":false}");
        var yaInactivo = await CambiarActivoAsync(_admin, id, "{\"activo\":false}");
        Assert.Equal(HttpStatusCode.OK, yaInactivo.StatusCode);
        Assert.False((await yaInactivo.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("activo").GetBoolean());
        Assert.Equal(0, await ActivoAsync(id));
    }

    [Fact]
    public async Task Cambiar_el_estado_de_un_articulo_inexistente_devuelve_404_no_encontrado()
    {
        var respuesta = await CambiarActivoAsync(_admin, 999_999, "{\"activo\":false}");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("no_encontrado", problema.GetProperty("codigo").GetString());
        Assert.Equal(404, problema.GetProperty("status").GetInt32());
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"activo\":null}")]
    public async Task Cambiar_el_estado_sin_activo_devuelve_400_con_el_error_del_campo_y_no_toca_la_fila(string cuerpo)
    {
        var id = await CrearAsync("Sin estado", dia: 13);

        var respuesta = await CambiarActivoAsync(_admin, id, cuerpo);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.True((await respuesta.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("errors").TryGetProperty("Activo", out _));
        Assert.Equal(1, await ActivoAsync(id));
    }

    // --- S26: sólo Administración --------------------------------------------------------------

    [Fact]
    public async Task Cambiar_el_estado_sin_sesion_devuelve_401_y_como_usuario_devuelve_403_sin_tocar_la_fila()
    {
        var id = await CrearAsync("Sólo Administración", dia: 14);
        var antes = await FilaSinActivoAsync(id);

        var anonimo = await CambiarActivoAsync(_anonimo, id, "{\"activo\":false}");
        Assert.Equal(HttpStatusCode.Unauthorized, anonimo.StatusCode);
        Assert.Equal("no_autenticado", (await anonimo.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("codigo").GetString());

        var usuario = await CambiarActivoAsync(_usuario, id, "{\"activo\":false}");
        Assert.Equal(HttpStatusCode.Forbidden, usuario.StatusCode);
        Assert.Equal("sin_permiso", (await usuario.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("codigo").GetString());

        // Sigue activo y sigue en el catálogo.
        Assert.Equal(1, await ActivoAsync(id));
        Assert.Equal(antes, await FilaSinActivoAsync(id));
        Assert.Contains(id, await IdsDelCatalogoAsync());
    }

    [Fact]
    public async Task Reactivar_sin_sesion_devuelve_401_y_como_usuario_devuelve_403_y_el_articulo_sigue_retirado()
    {
        var id = await CrearAsync("Reactivar ajeno", dia: 15);
        await CambiarActivoAsync(_admin, id, "{\"activo\":false}");
        var antes = await FilaSinActivoAsync(id);

        var anonimo = await CambiarActivoAsync(_anonimo, id, "{\"activo\":true}");
        Assert.Equal(HttpStatusCode.Unauthorized, anonimo.StatusCode);
        Assert.Equal("no_autenticado", (await anonimo.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("codigo").GetString());

        var usuario = await CambiarActivoAsync(_usuario, id, "{\"activo\":true}");
        Assert.Equal(HttpStatusCode.Forbidden, usuario.StatusCode);
        Assert.Equal("sin_permiso", (await usuario.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("codigo").GetString());

        // Sigue retirado: ni en la fila, ni en el catálogo, ni por URL.
        Assert.Equal(0, await ActivoAsync(id));
        Assert.Equal(antes, await FilaSinActivoAsync(id));
        Assert.DoesNotContain(id, await IdsDelCatalogoAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await _usuario.GetAsync($"/api/articulos/{id}")).StatusCode);
    }

    // --- S25 + S4: el listado de administración ------------------------------------------------

    [Fact]
    public async Task El_listado_de_administracion_incluye_los_inactivos_con_su_estado_y_sin_el_cuerpo()
    {
        var activo = await CrearAsync("Listado activo", dia: 20);
        var retirado = await CrearAsync("Listado retirado", dia: 21);
        await CambiarActivoAsync(_admin, retirado, "{\"activo\":false}");

        var respuesta = await _admin.GetAsync("/api/articulos/administracion?tamanioDePagina=50");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var pagina = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        var elementos = pagina.GetProperty("elementos").EnumerateArray().ToList();

        var filaActiva = elementos.Single(e => e.GetProperty("id").GetInt32() == activo);
        var filaRetirada = elementos.Single(e => e.GetProperty("id").GetInt32() == retirado);

        Assert.True(filaActiva.GetProperty("activo").GetBoolean());
        Assert.False(filaRetirada.GetProperty("activo").GetBoolean());
        Assert.Equal($"{Prefijo}Listado retirado", filaRetirada.GetProperty("titulo").GetString());
        Assert.Equal($"{FechaBase}21", filaRetirada.GetProperty("fechaPublicacion").GetString());
        Assert.Equal(CategoriaActiva, filaRetirada.GetProperty("categoriaId").GetInt32());

        // Sin el Markdown: es un listado, no el detalle.
        Assert.All(elementos, e => Assert.False(e.TryGetProperty("contenidoMarkdown", out _)));

        // El orden es el del catálogo: lo más nuevo primero.
        Assert.True(
            elementos.FindIndex(e => e.GetProperty("id").GetInt32() == retirado)
            < elementos.FindIndex(e => e.GetProperty("id").GetInt32() == activo));
    }

    [Fact]
    public async Task El_listado_de_administracion_respeta_el_paginado_en_espanol()
    {
        var primero = await CrearAsync("Página uno", dia: 30);
        var segundo = await CrearAsync("Página dos", dia: 29);
        var tercero = await CrearAsync("Página tres", dia: 28);
        await CambiarActivoAsync(_admin, segundo, "{\"activo\":false}");

        var primera = await _admin.GetFromJsonAsync<JsonElement>("/api/articulos/administracion?pagina=1&tamanioDePagina=2");
        var segunda = await _admin.GetFromJsonAsync<JsonElement>("/api/articulos/administracion?pagina=2&tamanioDePagina=2");

        Assert.Equal(1, primera.GetProperty("pagina").GetInt32());
        Assert.Equal(2, primera.GetProperty("tamanioDePagina").GetInt32());
        Assert.Equal([primero, segundo], Ids(primera));
        Assert.Equal(2, segunda.GetProperty("pagina").GetInt32());
        Assert.Equal(tercero, Ids(segunda)[0]);

        // El total cuenta activos e inactivos: lo mismo que hay en la tabla.
        var total = await ContarAsync();
        Assert.Equal(total, primera.GetProperty("totalDeRegistros").GetInt32());
        Assert.Equal((total + 1) / 2, primera.GetProperty("totalDePaginas").GetInt32());

        // Una página pasada del final queda vacía y la metadata es la que se pidió.
        var pasada = await _admin.GetFromJsonAsync<JsonElement>("/api/articulos/administracion?pagina=999&tamanioDePagina=2");
        Assert.Empty(pasada.GetProperty("elementos").EnumerateArray());
        Assert.Equal(999, pasada.GetProperty("pagina").GetInt32());
        Assert.Equal(total, pasada.GetProperty("totalDeRegistros").GetInt32());
    }

    [Theory]
    [InlineData("tamanioDePagina=0")]
    [InlineData("tamanioDePagina=51")]
    [InlineData("pagina=0")]
    public async Task El_listado_de_administracion_rechaza_parametros_fuera_de_rango_con_400(string consulta)
    {
        var respuesta = await _admin.GetAsync($"/api/articulos/administracion?{consulta}");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.True((await respuesta.Content.ReadFromJsonAsync<JsonElement>()).TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task El_listado_de_administracion_sin_sesion_devuelve_401_y_como_usuario_devuelve_403()
    {
        var anonimo = await _anonimo.GetAsync("/api/articulos/administracion");
        Assert.Equal(HttpStatusCode.Unauthorized, anonimo.StatusCode);
        Assert.Equal("no_autenticado", (await anonimo.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("codigo").GetString());

        var usuario = await _usuario.GetAsync("/api/articulos/administracion");
        Assert.Equal(HttpStatusCode.Forbidden, usuario.StatusCode);
        Assert.Equal("sin_permiso", (await usuario.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("codigo").GetString());
    }

    // --- utilidades --------------------------------------------------------------------------

    /// <summary>Publica un artículo por la API y le pone la fecha del día indicado de marzo de 2099.</summary>
    private async Task<int> CrearAsync(string titulo, int dia)
    {
        var formulario = new MultipartFormDataContent
        {
            { new StringContent($"{Prefijo}{titulo}"), "titulo" },
            { new StringContent("Un resumen"), "resumen" },
            { new StringContent("# Nada"), "contenidoMarkdown" },
            { new StringContent(CategoriaActiva.ToString()), "categoriaId" }
        };

        var respuesta = await _admin.PostAsync("/api/articulos", formulario);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);

        var id = (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        await EjecutarAsync($"UPDATE articulo SET fecha_publicacion = '{FechaBase}{dia:00} 10:00:00' WHERE id = {id}");

        return id;
    }

    private static Task<HttpResponseMessage> CambiarActivoAsync(HttpClient cliente, int id, string json) =>
        cliente.PutAsync(
            $"/api/articulos/{id}/activo",
            new StringContent(json, Encoding.UTF8, "application/json"));

    private async Task<List<JsonElement>> ElementosDelCatalogoAsync()
    {
        var pagina = await _usuario.GetFromJsonAsync<JsonElement>(
            $"/api/articulos?categoriaId={CategoriaActiva}&tamanioDePagina=50");

        return pagina.GetProperty("elementos").EnumerateArray().ToList();
    }

    private async Task<List<int>> IdsDelCatalogoAsync() =>
        (await ElementosDelCatalogoAsync()).Select(e => e.GetProperty("id").GetInt32()).ToList();

    private static List<int> Ids(JsonElement pagina) =>
        pagina.GetProperty("elementos").EnumerateArray().Select(e => e.GetProperty("id").GetInt32()).ToList();

    /// <summary>Todo lo de la fila salvo `activo`: lo que una baja o una reactivación no puede tocar.</summary>
    private static async Task<string?> FilaSinActivoAsync(int id) => (string?)await EscalarAsync(
        $"""
         SELECT CONCAT_WS('|', titulo, resumen, contenido_markdown, categoria_id,
             IFNULL(imagen_portada_url, ''), fecha_publicacion)
         FROM articulo WHERE id = {id}
         """);

    private static async Task<long> ActivoAsync(int id) =>
        Convert.ToInt64(await EscalarAsync($"SELECT activo FROM articulo WHERE id = {id}"));

    private static async Task<int> ContarAsync() =>
        Convert.ToInt32(await EscalarAsync("SELECT COUNT(*) FROM articulo"));

    private static async Task<object?> EscalarAsync(string sql)
    {
        await using var conexion = new MySqlConnection(BaseDeDatosDePrueba.CadenaDeConexion);
        await conexion.OpenAsync();

        await using var comando = new MySqlCommand(sql, conexion);
        return await comando.ExecuteScalarAsync();
    }

    private static async Task EjecutarAsync(string sql)
    {
        await using var conexion = new MySqlConnection(BaseDeDatosDePrueba.CadenaDeConexion);
        await conexion.OpenAsync();

        await using var comando = new MySqlCommand(sql, conexion);
        await comando.ExecuteNonQueryAsync();
    }

    /// <summary>Borra sólo lo que crearon estas pruebas (DELETE real: la tabla es del seed).</summary>
    private static Task LimpiarAsync() => EjecutarAsync("DELETE FROM articulo WHERE titulo LIKE 'Prueba baja %'");
}
