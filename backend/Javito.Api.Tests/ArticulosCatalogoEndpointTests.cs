using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Javito.Api.Tests.Infraestructura;
using MySqlConnector;

namespace Javito.Api.Tests;

/// <summary>
/// ART-04, specs S4 y S5: el catálogo paginado en español. Una prueba por regla, y las respuestas se
/// leen como <see cref="JsonElement"/> porque lo que se verifica es el contrato que viaja: los nombres
/// de la metadata son el requisito, no un detalle de serialización.
/// </summary>
[Collection(ColeccionDeArticulos.Nombre)]
public class ArticulosCatalogoEndpointTests(FabricaDeApi fabrica) : IAsyncLifetime
{
    /// <summary>Lo que crea cada prueba lleva este prefijo; la clase borra sólo lo suyo.</summary>
    private const string Prefijo = "Prueba ";

    /// <summary>Categoría activa del seed (Mente) y la retirada (Espiritualidad).</summary>
    private const int CategoriaActiva = 2;

    private const int CategoriaInactiva = 5;

    /// <summary>Id de la categoría activa del seed que tiene un solo artículo (Cuerpo).</summary>
    private const int CategoriaDeUnSoloArticulo = 1;

    /// <summary>Artículo del seed que está dado de baja (HU-ART-03).</summary>
    private const int ArticuloInactivoDelSeed = 4;

    /// <summary>
    /// Los tres artículos activos del seed, del más nuevo al más viejo: sus fechas son 20, 12 y 5 días
    /// atrás, así que el orden esperado es el inverso de los ids.
    /// </summary>
    private static readonly int[] ActivosDelSeedDelMasNuevoAlMasViejo = [3, 2, 1];

    private readonly HttpClient _usuario = fabrica.CreateClient();
    private readonly HttpClient _anonimo = fabrica.CreateClient();

    public async Task InitializeAsync()
    {
        await SesionDePrueba.ClienteAutenticadoAsync(_usuario);
        await LimpiarAsync();
    }

    public async Task DisposeAsync()
    {
        await LimpiarAsync();
        _usuario.Dispose();
        _anonimo.Dispose();
    }

    // --- S4: el contrato de paginado ---------------------------------------------------------

    [Fact]
    public async Task Catalogo_por_defecto_devuelve_los_tres_activos_del_mas_nuevo_al_mas_viejo()
    {
        var pagina = await PedirAsync("/api/articulos");

        // Los cinco nombres de la metadata, en español y con el default de 6 por página.
        Assert.Equal(1, Numero(pagina, "pagina"));
        Assert.Equal(6, Numero(pagina, "tamanioDePagina"));
        Assert.Equal(3, Numero(pagina, "totalDeRegistros"));
        Assert.Equal(1, Numero(pagina, "totalDePaginas"));

        Assert.Equal(ActivosDelSeedDelMasNuevoAlMasViejo, Ids(pagina));

        // "Del más nuevo al más viejo": el primero es el de hace 5 días.
        var elementos = Elementos(pagina);
        var primero = elementos[0];
        Assert.Equal("Hábitos semanales sin culpa", primero.GetProperty("titulo").GetString());
        Assert.False(string.IsNullOrWhiteSpace(primero.GetProperty("resumen").GetString()));

        // S5: cada elemento trae portada, título, resumen, categoría y fecha, con la misma forma que
        // el detalle. Los del seed no tienen portada.
        Assert.Equal(CategoriaActiva, primero.GetProperty("categoriaId").GetInt32());
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", primero.GetProperty("fechaPublicacion").GetString());
        Assert.Equal(JsonValueKind.Null, primero.GetProperty("imagenPortadaUrl").ValueKind);
        Assert.True(primero.GetProperty("activo").GetBoolean());

        // Y el cuerpo NO viaja: una lista de tarjetas que arrastra el Markdown completo de cada
        // artículo es justo lo que HU-ART-04 pide evitar. El cuerpo se lee en GET /api/articulos/{id}.
        Assert.False(primero.TryGetProperty("contenidoMarkdown", out _));
    }

    [Fact]
    public async Task Catalogo_con_tamanio_2_reparte_los_tres_activos_en_dos_paginas()
    {
        var primera = await PedirAsync("/api/articulos?tamanioDePagina=2");

        Assert.Equal(1, Numero(primera, "pagina"));
        Assert.Equal(2, Numero(primera, "tamanioDePagina"));
        Assert.Equal(3, Numero(primera, "totalDeRegistros"));
        Assert.Equal(2, Numero(primera, "totalDePaginas"));
        Assert.Equal(new[] { 3, 2 }, Ids(primera));

        // La última página es corta: sobra un solo artículo, y los totales no cambian.
        var segunda = await PedirAsync("/api/articulos?tamanioDePagina=2&pagina=2");

        Assert.Equal(2, Numero(segunda, "pagina"));
        Assert.Equal(2, Numero(segunda, "tamanioDePagina"));
        Assert.Equal(3, Numero(segunda, "totalDeRegistros"));
        Assert.Equal(2, Numero(segunda, "totalDePaginas"));
        Assert.Equal(new[] { 1 }, Ids(segunda));
    }

    [Fact]
    public async Task Catalogo_con_una_pagina_pasada_del_final_devuelve_vacio_y_la_pagina_pedida()
    {
        var pagina = await PedirAsync("/api/articulos?pagina=9");

        // El servidor no corrige en silencio lo que el cliente pidió: la página se repite tal cual.
        Assert.Empty(Elementos(pagina));
        Assert.Equal(9, Numero(pagina, "pagina"));
        Assert.Equal(6, Numero(pagina, "tamanioDePagina"));
        Assert.Equal(3, Numero(pagina, "totalDeRegistros"));
        Assert.Equal(1, Numero(pagina, "totalDePaginas"));
    }

    [Theory]
    [InlineData("pagina=0", "Pagina")]
    [InlineData("tamanioDePagina=0", "TamanioDePagina")]
    [InlineData("tamanioDePagina=51", "TamanioDePagina")]
    public async Task Catalogo_con_paginacion_fuera_de_rango_devuelve_400_con_el_campo_en_errors(
        string consulta,
        string campo)
    {
        var respuesta = await _usuario.GetAsync($"/api/articulos?{consulta}");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);

        var problema = await JsonAsync(respuesta);
        Assert.Equal(400, problema.GetProperty("status").GetInt32());

        // El 400 es el de DataAnnotations: nombra el campo, no un texto suelto escrito a mano.
        Assert.True(
            problema.GetProperty("errors").TryGetProperty(campo, out var mensajes),
            $"El 400 tiene que nombrar el campo {campo} en errors.");
        Assert.NotEmpty(mensajes.EnumerateArray());
    }

    [Theory]
    [InlineData("tamanioDePagina=1", 1)]
    [InlineData("tamanioDePagina=50", 50)]
    public async Task Catalogo_acepta_los_dos_bordes_del_tamanio_de_pagina(string consulta, int esperado)
    {
        // Los bordes del rango aceptado: el tope de 50 no es un rechazo, y 1 no cae al default.
        var pagina = await PedirAsync($"/api/articulos?{consulta}");

        Assert.Equal(esperado, Numero(pagina, "tamanioDePagina"));
    }

    [Theory]
    [InlineData("pagina=abc")]
    [InlineData("pagina=2147483648")]
    public async Task Catalogo_con_una_pagina_que_no_es_un_entero_devuelve_400(string consulta)
    {
        var respuesta = await _usuario.GetAsync($"/api/articulos?{consulta}");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);

        var problema = await JsonAsync(respuesta);
        Assert.True(
            problema.GetProperty("errors").TryGetProperty("Pagina", out _),
            "Una página que no entra en un entero tiene que nombrar el campo Pagina.");
    }

    // --- S5: el catálogo ---------------------------------------------------------------------

    [Fact]
    public async Task Catalogo_no_devuelve_nunca_el_articulo_retirado()
    {
        // Se recorre página por página: "sólo activo = TRUE" tiene que valer en todas, no en la primera.
        var recorridos = new List<int>();

        for (var numero = 1; numero <= 3; numero++)
        {
            var pagina = await PedirAsync($"/api/articulos?pagina={numero}&tamanioDePagina=1");

            foreach (var elemento in Elementos(pagina))
            {
                Assert.True(elemento.GetProperty("activo").GetBoolean());
            }

            recorridos.AddRange(Ids(pagina));
        }

        Assert.Equal(ActivosDelSeedDelMasNuevoAlMasViejo, recorridos);
        Assert.DoesNotContain(ArticuloInactivoDelSeed, recorridos);
    }

    [Fact]
    public async Task Catalogo_con_categoria_filtra_y_los_totales_reflejan_el_filtro()
    {
        var deMente = await PedirAsync($"/api/articulos?categoriaId={CategoriaActiva}");

        Assert.Equal(new[] { 3, 1 }, Ids(deMente));
        Assert.Equal(2, Numero(deMente, "totalDeRegistros"));
        Assert.Equal(1, Numero(deMente, "totalDePaginas"));

        foreach (var elemento in Elementos(deMente))
        {
            Assert.Equal(CategoriaActiva, elemento.GetProperty("categoriaId").GetInt32());
        }

        // Otra categoría activa: el filtro acota de verdad y los totales siguen al filtro.
        var deCuerpo = await PedirAsync($"/api/articulos?categoriaId={CategoriaDeUnSoloArticulo}");

        Assert.Equal(new[] { 2 }, Ids(deCuerpo));
        Assert.Equal(1, Numero(deCuerpo, "totalDeRegistros"));
        Assert.Equal(1, Numero(deCuerpo, "totalDePaginas"));
    }

    [Theory]
    [InlineData(CategoriaInactiva)]
    [InlineData(999_999)]
    public async Task Catalogo_con_una_categoria_invalida_devuelve_una_pagina_vacia_y_no_400(int categoriaId)
    {
        // Una categoría retirada o inexistente no es un error de entrada: simplemente no hay nada.
        var respuesta = await _usuario.GetAsync($"/api/articulos?categoriaId={categoriaId}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var pagina = await JsonAsync(respuesta);
        Assert.Empty(Elementos(pagina));
        Assert.Equal(1, Numero(pagina, "pagina"));
        Assert.Equal(0, Numero(pagina, "totalDeRegistros"));
        Assert.Equal(0, Numero(pagina, "totalDePaginas"));
    }

    [Fact]
    public async Task Catalogo_desempata_por_id_descendente_cuando_dos_articulos_comparten_fecha()
    {
        var (primero, segundo) = await InsertarEmpateAsync();

        var pagina = await PedirAsync("/api/articulos?tamanioDePagina=2");

        // Los dos son más nuevos que el seed, así que encabezan la primera página.
        Assert.Equal(5, Numero(pagina, "totalDeRegistros"));
        Assert.Equal(new[] { segundo, primero }, Ids(pagina));

        // La fecha es la misma: lo único que puede decidir el orden es el id.
        var elementos = Elementos(pagina);
        Assert.Equal(
            elementos[0].GetProperty("fechaPublicacion").GetString(),
            elementos[1].GetProperty("fechaPublicacion").GetString());
    }

    [Fact]
    public async Task Catalogo_sin_sesion_devuelve_401()
    {
        var respuesta = await _anonimo.GetAsync("/api/articulos");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    // --- utilidades --------------------------------------------------------------------------

    /// <summary>Pide una página y exige 200; en RED la ruta no existe y el mensaje dice qué pasó.</summary>
    private async Task<JsonElement> PedirAsync(string url)
    {
        var respuesta = await _usuario.GetAsync(url);
        var cuerpo = await respuesta.Content.ReadAsStringAsync();

        Assert.True(
            respuesta.StatusCode == HttpStatusCode.OK,
            $"GET {url} falló con {respuesta.StatusCode}: {cuerpo}");

        return JsonDocument.Parse(cuerpo).RootElement.Clone();
    }

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage respuesta) =>
        await respuesta.Content.ReadFromJsonAsync<JsonElement>();

    private static int Numero(JsonElement pagina, string nombre) => pagina.GetProperty(nombre).GetInt32();

    private static List<JsonElement> Elementos(JsonElement pagina) =>
        pagina.GetProperty("elementos").EnumerateArray().ToList();

    private static int[] Ids(JsonElement pagina) =>
        Elementos(pagina).Select(elemento => elemento.GetProperty("id").GetInt32()).ToArray();

    /// <summary>
    /// Dos artículos con la misma <c>fecha_publicacion</c>. El seed no garantiza empates, así que el
    /// desempate se prueba con filas propias: las dos nacen con la fecha más nueva del catálogo para
    /// quedar en la primera página.
    /// </summary>
    private static async Task<(int Primero, int Segundo)> InsertarEmpateAsync()
    {
        var momento = DateTime.Now;

        var primero = await InsertarAsync($"{Prefijo}Empate primero", momento);
        var segundo = await InsertarAsync($"{Prefijo}Empate segundo", momento);

        return (primero, segundo);
    }

    private static async Task<int> InsertarAsync(string titulo, DateTime fechaPublicacion)
    {
        await using var conexion = new MySqlConnection(BaseDeDatosDePrueba.CadenaDeConexion);
        await conexion.OpenAsync();

        await using var comando = new MySqlCommand(
            """
            INSERT INTO articulo
                (usuario_id, categoria_id, titulo, resumen, contenido_markdown, fecha_publicacion, activo)
            VALUES (1, @categoriaId, @titulo, 'Un resumen', '# Empate', @fechaPublicacion, TRUE)
            """,
            conexion);
        comando.Parameters.AddWithValue("@categoriaId", CategoriaActiva);
        comando.Parameters.AddWithValue("@titulo", titulo);
        comando.Parameters.AddWithValue("@fechaPublicacion", fechaPublicacion);
        await comando.ExecuteNonQueryAsync();

        await using var id = new MySqlCommand(
            "SELECT id FROM articulo WHERE titulo = @titulo",
            conexion);
        id.Parameters.AddWithValue("@titulo", titulo);

        return Convert.ToInt32(await id.ExecuteScalarAsync());
    }

    /// <summary>
    /// Borra sólo lo que crearon estas pruebas. Es un DELETE de verdad y no la baja lógica de la
    /// aplicación: la tabla es del seed y el catálogo afirma sobre sus tres artículos activos.
    /// </summary>
    private static async Task LimpiarAsync()
    {
        await using var conexion = new MySqlConnection(BaseDeDatosDePrueba.CadenaDeConexion);
        await conexion.OpenAsync();

        await using var comando = new MySqlCommand(
            "DELETE FROM articulo WHERE titulo LIKE 'Prueba %'",
            conexion);
        await comando.ExecuteNonQueryAsync();
    }
}
