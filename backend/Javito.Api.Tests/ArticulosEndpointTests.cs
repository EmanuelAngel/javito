using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Javito.Api.Tests.Infraestructura;
using MySqlConnector;

namespace Javito.Api.Tests;

/// <summary>
/// HU-ART-01: el Administrador publica un artículo con portada opcional, y HU-ART-03 en su mitad de
/// lectura: lo retirado no se abre por URL. Una prueba por regla. Las respuestas se leen como
/// <see cref="JsonElement"/> y no con el DTO a propósito: el contrato se verifica por lo que viaja.
/// </summary>
[Collection(ColeccionDeArticulos.Nombre)]
public class ArticulosEndpointTests(FabricaDeApi fabrica) : IAsyncLifetime
{
    /// <summary>PNG 1x1 real: la validación mira la firma, así que un encabezado inventado no sirve.</summary>
    private static readonly byte[] PngValido = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAIAAACQd1PeAAAADElEQVR4nGPQqw4CAAHWAPz6Qz2AAAAAAElFTkSuQmCC");

    /// <summary>HTML con nombre de imagen: lo que la validación por firma tiene que rechazar.</summary>
    private static readonly byte[] NoEsImagen = Encoding.UTF8.GetBytes("<html><body>no soy una imagen</body></html>");

    /// <summary>Lo que crea cada prueba lleva este prefijo; la clase borra sólo lo suyo.</summary>
    private const string Prefijo = "Prueba ";

    /// <summary>Categoría activa del seed (Mente) y la retirada (Espiritualidad).</summary>
    private const int CategoriaActiva = 2;

    private const int CategoriaInactiva = 5;

    /// <summary>El tope de la portada, el mismo que aplica el almacén.</summary>
    private const long TamanoMaximoDeLaPortada = 5 * 1024 * 1024;

    /// <summary>Artículo del seed que está dado de baja (HU-ART-03).</summary>
    private const int ArticuloInactivoDelSeed = 4;

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

    // --- S1: el alta -------------------------------------------------------------------------

    [Fact]
    public async Task Crear_sin_portada_devuelve_201_con_el_articulo_activo_y_su_location()
    {
        var respuesta = await _admin.PostAsync(
            "/api/articulos",
            Formulario($"{Prefijo}Sin portada", "Un resumen", "# Contenido\n\nCon **negritas**."));

        Assert.True(
            respuesta.StatusCode == HttpStatusCode.Created,
            $"El alta falló con {respuesta.StatusCode}: {await respuesta.Content.ReadAsStringAsync()}");

        var creado = await respuesta.Content.ReadFromJsonAsync<JsonElement>();

        // La fila es nueva: el seed tiene cuatro artículos.
        Assert.True(creado.GetProperty("id").GetInt32() > 4, "El artículo tiene que ser una fila nueva.");
        Assert.Equal($"{Prefijo}Sin portada", creado.GetProperty("titulo").GetString());
        Assert.Equal("Un resumen", creado.GetProperty("resumen").GetString());
        Assert.Equal("# Contenido\n\nCon **negritas**.", creado.GetProperty("contenidoMarkdown").GetString());
        Assert.Equal(CategoriaActiva, creado.GetProperty("categoriaId").GetInt32());

        // "Un artículo sin portada se puede publicar igual".
        Assert.Equal(JsonValueKind.Null, creado.GetProperty("imagenPortadaUrl").ValueKind);

        // "El artículo nace activo y con fecha de publicación".
        Assert.True(creado.GetProperty("activo").GetBoolean());
        var publicacion = DateTime.Parse(creado.GetProperty("fechaPublicacion").GetString()!);
        Assert.InRange(publicacion, DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1));

        // Y lo que se publicó se puede leer después por su URL.
        var id = creado.GetProperty("id").GetInt32();
        Assert.Equal($"/api/articulos/{id}", respuesta.Headers.Location?.OriginalString);

        var leido = await _usuario.GetAsync($"/api/articulos/{id}");
        Assert.Equal(HttpStatusCode.OK, leido.StatusCode);
        Assert.Equal(id, (await leido.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task Crear_con_portada_la_guarda_y_la_sirve_por_uploads()
    {
        var respuesta = await _admin.PostAsync(
            "/api/articulos",
            Formulario($"{Prefijo}Con portada", "Un resumen", "# Con portada", portada: PngValido));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);

        var creado = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        var portada = creado.GetProperty("imagenPortadaUrl").GetString();

        // Ruta relativa, subcarpeta propia y nombre generado por el servidor: nada del nombre del cliente.
        Assert.Matches(@"^/uploads/articulos/[0-9a-f]{32}\.png$", portada);
        Assert.DoesNotContain("portada.png", portada, StringComparison.Ordinal);

        var servido = await _usuario.GetAsync(portada);
        Assert.Equal(HttpStatusCode.OK, servido.StatusCode);
        Assert.Equal("image/png", servido.Content.Headers.ContentType?.MediaType);
        Assert.Equal(PngValido, await servido.Content.ReadAsByteArrayAsync());
        Assert.Equal("nosniff", servido.Headers.GetValues("X-Content-Type-Options").Single());
    }

    [Fact]
    public async Task Crear_con_algo_que_no_es_imagen_devuelve_400_y_no_deja_archivo_en_disco()
    {
        var antes = ArchivosDePortada(fabrica);

        var respuesta = await _admin.PostAsync(
            "/api/articulos",
            Formulario($"{Prefijo}Portada falsa", "Un resumen", "# Nada", portada: NoEsImagen));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("archivo_no_es_imagen", problema.GetProperty("codigo").GetString());
        Assert.Equal(400, problema.GetProperty("status").GetInt32());

        // El rechazo ocurre antes de escribir: la subcarpeta queda como estaba, sin archivo nuevo.
        Assert.Equal(antes, ArchivosDePortada(fabrica));
    }

    [Fact]
    public async Task Crear_con_una_portada_de_mas_de_5_MB_devuelve_413_y_no_guarda_el_archivo()
    {
        var antes = ArchivosDePortada(fabrica);

        var respuesta = await _admin.PostAsync(
            "/api/articulos",
            Formulario(
                $"{Prefijo}Portada enorme",
                "Un resumen",
                "# Nada",
                portada: ImagenDeTope(TamanoMaximoDeLaPortada + 1)));

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("archivo_demasiado_grande", problema.GetProperty("codigo").GetString());
        Assert.Contains("5 MB", problema.GetProperty("detail").GetString(), StringComparison.Ordinal);

        Assert.Equal(antes, ArchivosDePortada(fabrica));
        Assert.Equal(0, await ContarAsync($"{Prefijo}Portada enorme"));
    }

    [Fact]
    public async Task Crear_sin_sesion_devuelve_401_y_como_usuario_devuelve_403_sin_permiso()
    {
        var antes = ArchivosDePortada(fabrica);

        var sinSesion = await _anonimo.PostAsync(
            "/api/articulos",
            Formulario($"{Prefijo}Sin sesión", "Un resumen", "# Nada"));

        var comoUsuario = await _usuario.PostAsync(
            "/api/articulos",
            Formulario($"{Prefijo}Como usuario", "Un resumen", "# Nada", portada: PngValido));

        Assert.Equal(HttpStatusCode.Unauthorized, sinSesion.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, comoUsuario.StatusCode);

        var problema = await comoUsuario.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("sin_permiso", problema.GetProperty("codigo").GetString());

        // Un rechazo no deja rastro: ni fila ni imagen.
        Assert.Equal(antes, ArchivosDePortada(fabrica));
        Assert.Equal(0, await ContarAsync($"{Prefijo}Como usuario"));
    }

    [Theory]
    [InlineData("titulo", "")]
    [InlineData("titulo", "   ")]
    [InlineData("resumen", "")]
    [InlineData("contenidoMarkdown", "   ")]
    public async Task Crear_con_un_campo_vacio_devuelve_400_con_el_error_de_ese_campo(
        string campo,
        string valor)
    {
        var respuesta = await _admin.PostAsync(
            "/api/articulos",
            FormularioCon(campo, valor));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        var campoEnElError = campo[..1].ToUpperInvariant() + campo[1..];

        Assert.True(
            problema.GetProperty("errors").TryGetProperty(campoEnElError, out var mensajes),
            $"El 400 tiene que nombrar el campo {campoEnElError} en errors.");
        Assert.NotEmpty(mensajes.EnumerateArray());
    }

    [Fact]
    public async Task Crear_con_un_titulo_de_151_caracteres_devuelve_400()
    {
        var respuesta = await _admin.PostAsync(
            "/api/articulos",
            Formulario($"{Prefijo}{new string('a', 144)}", "Un resumen", "# Nada"));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task Crear_con_un_resumen_de_301_caracteres_devuelve_400()
    {
        var respuesta = await _admin.PostAsync(
            "/api/articulos",
            Formulario($"{Prefijo}Resumen largo", new string('a', 301), "# Nada"));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task Crear_sin_categoria_devuelve_400()
    {
        var formulario = new MultipartFormDataContent
        {
            { new StringContent($"{Prefijo}Sin categoría"), "titulo" },
            { new StringContent("Un resumen"), "resumen" },
            { new StringContent("# Nada"), "contenidoMarkdown" }
        };

        var respuesta = await _admin.PostAsync("/api/articulos", formulario);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task Crear_con_una_categoria_inactiva_devuelve_400_categoria_invalida()
    {
        var respuesta = await _admin.PostAsync(
            "/api/articulos",
            Formulario($"{Prefijo}Categoría retirada", "Un resumen", "# Nada", CategoriaInactiva));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("categoria_invalida", problema.GetProperty("codigo").GetString());
        Assert.Equal(0, await ContarAsync($"{Prefijo}Categoría retirada"));
    }

    [Fact]
    public async Task Crear_con_una_categoria_inexistente_devuelve_400_categoria_invalida()
    {
        var respuesta = await _admin.PostAsync(
            "/api/articulos",
            Formulario($"{Prefijo}Categoría fantasma", "Un resumen", "# Nada", 999_999));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("categoria_invalida", problema.GetProperty("codigo").GetString());
        Assert.Equal(0, await ContarAsync($"{Prefijo}Categoría fantasma"));
    }

    [Fact]
    public async Task La_categoria_invalida_se_rechaza_antes_de_guardar_la_portada()
    {
        var antes = ArchivosDePortada(fabrica);

        // El orden importa: validar primero deja el disco limpio cuando la entrada no sirve.
        var respuesta = await _admin.PostAsync(
            "/api/articulos",
            Formulario($"{Prefijo}Orden", "Un resumen", "# Nada", CategoriaInactiva, portada: PngValido));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(antes, ArchivosDePortada(fabrica));
    }

    // --- S3: la lectura ----------------------------------------------------------------------

    [Fact]
    public async Task Obtener_devuelve_el_articulo_del_seed()
    {
        var respuesta = await _usuario.GetAsync("/api/articulos/1");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var articulo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, articulo.GetProperty("id").GetInt32());
        Assert.Equal("Por qué no hay racha en Javito", articulo.GetProperty("titulo").GetString());
        Assert.Equal(2, articulo.GetProperty("categoriaId").GetInt32());
        Assert.True(articulo.GetProperty("activo").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(articulo.GetProperty("contenidoMarkdown").GetString()));
    }

    [Fact]
    public async Task Obtener_un_articulo_inactivo_devuelve_404_no_encontrado()
    {
        var respuesta = await _usuario.GetAsync($"/api/articulos/{ArticuloInactivoDelSeed}");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("no_encontrado", problema.GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task Obtener_un_id_inexistente_devuelve_404_no_encontrado()
    {
        var respuesta = await _usuario.GetAsync("/api/articulos/999999");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("no_encontrado", problema.GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task Obtener_sin_sesion_devuelve_401()
    {
        var respuesta = await _anonimo.GetAsync("/api/articulos/1");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    // --- utilidades --------------------------------------------------------------------------

    private static MultipartFormDataContent Formulario(
        string titulo,
        string resumen,
        string contenido,
        int categoriaId = CategoriaActiva,
        byte[]? portada = null)
    {
        var formulario = new MultipartFormDataContent
        {
            { new StringContent(titulo), "titulo" },
            { new StringContent(resumen), "resumen" },
            { new StringContent(contenido), "contenidoMarkdown" },
            { new StringContent(categoriaId.ToString()), "categoriaId" }
        };

        if (portada is not null)
        {
            var archivo = new ByteArrayContent(portada);
            archivo.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            formulario.Add(archivo, "imagenPortada", "portada.png");
        }

        return formulario;
    }

    /// <summary>Formulario válido salvo el campo indicado.</summary>
    private static MultipartFormDataContent FormularioCon(string campo, string valor)
    {
        var valores = new Dictionary<string, string>
        {
            ["titulo"] = $"{Prefijo}Campo inválido",
            ["resumen"] = "Un resumen",
            ["contenidoMarkdown"] = "# Nada",
            ["categoriaId"] = CategoriaActiva.ToString()
        };

        valores[campo] = valor;

        var formulario = new MultipartFormDataContent();

        foreach (var (nombre, contenido) in valores)
        {
            formulario.Add(new StringContent(contenido), nombre);
        }

        return formulario;
    }

    /// <summary>
    /// Una imagen de <paramref name="bytes"/> bytes: la firma de PNG al principio y relleno detrás, así
    /// lo único que puede rechazarla es el tamaño y no el tipo.
    /// </summary>
    private static byte[] ImagenDeTope(long bytes)
    {
        var imagen = new byte[bytes];
        PngValido.CopyTo(imagen, 0);

        return imagen;
    }

    /// <summary>
    /// Los archivos que quedaron en la subcarpeta de portadas. Es el disco del producto en un
    /// temporal por fábrica: la única forma de comprobar que un rechazo no dejó huérfanas.
    /// </summary>
    private static string[] ArchivosDePortada(FabricaDeApi fabrica)
    {
        var carpeta = Path.Combine(fabrica.RutaDeArchivos, "articulos");

        return Directory.Exists(carpeta) ? Directory.GetFiles(carpeta) : [];
    }

    private static async Task<int> ContarAsync(string titulo)
    {
        await using var conexion = new MySqlConnection(BaseDeDatosDePrueba.CadenaDeConexion);
        await conexion.OpenAsync();

        await using var comando = new MySqlCommand(
            "SELECT COUNT(*) FROM articulo WHERE titulo = @titulo",
            conexion);
        comando.Parameters.AddWithValue("@titulo", titulo);

        return Convert.ToInt32(await comando.ExecuteScalarAsync());
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
