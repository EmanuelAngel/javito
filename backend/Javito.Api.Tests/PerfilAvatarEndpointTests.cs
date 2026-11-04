using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Javito.Api.Contratos;
using Javito.Api.Tests.Infraestructura;
using Microsoft.AspNetCore.Http;

namespace Javito.Api.Tests;

/// <summary>
/// HU-PER-02: el avatar y, con él, la infraestructura de archivos que después usa la portada de
/// ART-01. Una prueba por regla de la historia. El PNG del seed de pruebas es un 1x1 real: la firma
/// del archivo es lo que se valida, así que un encabezado inventado no alcanza para probar el camino
/// feliz. Los archivos van a un temporal por fábrica y no al <c>wwwroot</c> del repositorio.
/// </summary>
public class PerfilAvatarEndpointTests(FabricaDeApi fabrica) : IClassFixture<FabricaDeApi>, IAsyncLifetime
{
    /// <summary>PNG 1x1 real (69 bytes): firma, IHDR, IDAT e IEND válidos.</summary>
    private static readonly byte[] PngValido = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAIAAACQd1PeAAAADElEQVR4nGPQqw4CAAHWAPz6Qz2AAAAAAElFTkSuQmCC");

    /// <summary>Cuenta del seed que ninguna otra prueba usa, para no pisarle el avatar a nadie.</summary>
    private const string Email = "otra@javito.local";

    private const long TopeEnBytes = 5 * 1024 * 1024;

    private readonly HttpClient _cliente = fabrica.CreateClient();

    public async Task InitializeAsync()
    {
        _cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await SesionDePrueba.IniciarAsync(_cliente, Email));
    }

    public Task DisposeAsync()
    {
        _cliente.Dispose();
        return Task.CompletedTask;
    }

    // --- S1 y S5: subida válida --------------------------------------------------------------

    [Fact]
    public async Task Subir_una_imagen_valida_devuelve_el_usuario_con_su_avatar_y_la_sirve_por_uploads()
    {
        var avatar = await SubirAsync(PngValido, "foto.png");

        Assert.Equal(HttpStatusCode.OK, avatar.StatusCode);
        Assert.Equal("application/json", avatar.Content.Headers.ContentType?.MediaType);

        var usuario = await avatar.Content.ReadFromJsonAsync<UsuarioDto>();
        Assert.NotNull(usuario);
        Assert.Equal(Email, usuario.Email);

        // La ruta es relativa y con nombre generado por el servidor: nada del nombre que mandó el cliente.
        Assert.Matches(@"^/uploads/avatares/[0-9a-f]{32}\.png$", usuario.AvatarUrl);
        Assert.DoesNotContain("foto", usuario.AvatarUrl, StringComparison.Ordinal);

        // La URL que guardó la fila se sirve de verdad, con el tipo que corresponde.
        var servido = await _cliente.GetAsync(usuario.AvatarUrl);
        Assert.Equal(HttpStatusCode.OK, servido.StatusCode);
        Assert.Equal("image/png", servido.Content.Headers.ContentType?.MediaType);
        Assert.Equal(PngValido, await servido.Content.ReadAsByteArrayAsync());

        // Y es la misma que ve el SPA al revalidar la sesión.
        Assert.Equal(usuario.AvatarUrl, await AvatarEnDiscoAsync(_cliente));
    }

    // --- S1 y S8: sólo imágenes, validadas por el servidor -----------------------------------

    [Fact]
    public async Task Un_archivo_que_no_es_imagen_se_rechaza_aunque_se_llame_png_y_no_guarda_nada()
    {
        var antes = ArchivosEnDisco();
        var avatarAntes = await AvatarEnDiscoAsync(_cliente);

        var html = Encoding.UTF8.GetBytes("<html><script>alert('no soy una imagen')</script></html>");
        var respuesta = await SubirAsync(html, "disfraz.png");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("archivo_no_es_imagen", problema.GetProperty("codigo").GetString());
        Assert.Equal(StatusCodes.Status400BadRequest, problema.GetProperty("status").GetInt32());

        Assert.Equal(antes, ArchivosEnDisco());
        Assert.Equal(avatarAntes, await AvatarEnDiscoAsync(_cliente));
    }

    // --- S2 y S8: el tope de tamaño -----------------------------------------------------------

    [Fact]
    public async Task Un_archivo_demasiado_grande_se_rechaza_con_el_limite_en_el_mensaje_y_no_guarda_nada()
    {
        var antes = ArchivosEnDisco();
        var avatarAntes = await AvatarEnDiscoAsync(_cliente);

        var respuesta = await SubirAsync(ImagenDeTope(TopeEnBytes + 1), "grande.png");

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("archivo_demasiado_grande", problema.GetProperty("codigo").GetString());
        Assert.Contains("5 MB", problema.GetProperty("detail").GetString(), StringComparison.Ordinal);

        Assert.Equal(antes, ArchivosEnDisco());
        Assert.Equal(avatarAntes, await AvatarEnDiscoAsync(_cliente));
    }

    // --- S3: el avatar anterior se reemplaza ---------------------------------------------------

    [Fact]
    public async Task Subir_otro_avatar_reemplaza_el_anterior_y_deja_un_solo_archivo()
    {
        var primero = await SubirAsync(PngValido, "uno.png");
        var urlAnterior = (await primero.Content.ReadFromJsonAsync<UsuarioDto>())?.AvatarUrl;
        Assert.NotNull(urlAnterior);

        var segundo = await SubirAsync(PngValido, "dos.png");
        var urlNueva = (await segundo.Content.ReadFromJsonAsync<UsuarioDto>())?.AvatarUrl;

        Assert.NotNull(urlNueva);
        Assert.NotEqual(urlAnterior, urlNueva);

        // El viejo dejó de existir: ni en la carpeta ni por HTTP.
        Assert.Single(ArchivosEnDisco());
        Assert.Equal(HttpStatusCode.NotFound, (await _cliente.GetAsync(urlAnterior)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _cliente.GetAsync(urlNueva)).StatusCode);
        Assert.Equal(urlNueva, await AvatarEnDiscoAsync(_cliente));
    }

    // --- S7 y S8: sin sesión y sin archivo no se escribe nada -----------------------------------

    [Fact]
    public async Task Sin_token_la_subida_es_401_y_no_guarda_nada()
    {
        var antes = ArchivosEnDisco();

        using var anonimo = fabrica.CreateClient();
        var respuesta = await anonimo.PostAsync("/api/perfil/avatar", Formulario(PngValido, "foto.png"));

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("no_autenticado", problema.GetProperty("codigo").GetString());

        Assert.Equal(antes, ArchivosEnDisco());
    }

    [Fact]
    public async Task Un_formulario_sin_archivo_devuelve_400_archivo_requerido()
    {
        var antes = ArchivosEnDisco();
        using var formulario = new MultipartFormDataContent { { new StringContent("si"), "archivo" } };

        var respuesta = await _cliente.PostAsync("/api/perfil/avatar", formulario);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("archivo_requerido", problema.GetProperty("codigo").GetString());

        Assert.Equal(antes, ArchivosEnDisco());
    }

    // --- Apoyo ---------------------------------------------------------------------------------

    private Task<HttpResponseMessage> SubirAsync(byte[] bytes, string nombre) =>
        _cliente.PostAsync("/api/perfil/avatar", Formulario(bytes, nombre));

    private static MultipartFormDataContent Formulario(byte[] bytes, string nombre)
    {
        var archivo = new ByteArrayContent(bytes);
        archivo.Headers.ContentType = new MediaTypeHeaderValue("image/png");

        return new MultipartFormDataContent { { archivo, "archivo", nombre } };
    }

    /// <summary>Copia la firma de un PNG real y rellena hasta el tamaño pedido.</summary>
    private static byte[] ImagenDeTope(long tamano)
    {
        var bytes = new byte[tamano];
        PngValido.CopyTo(bytes, 0);

        return bytes;
    }

    private static async Task<string?> AvatarEnDiscoAsync(HttpClient cliente)
    {
        var respuesta = await cliente.GetAsync("/api/sesion/yo");
        respuesta.EnsureSuccessStatusCode();

        var usuario = await respuesta.Content.ReadFromJsonAsync<UsuarioDto>();

        return usuario?.AvatarUrl;
    }

    /// <summary>Los archivos que hay hoy en la carpeta de avatares, ordenados para poder comparar.</summary>
    private string[] ArchivosEnDisco()
    {
        var carpeta = Path.Combine(fabrica.RutaDeArchivos, "avatares");

        return Directory.Exists(carpeta)
            ? [.. Directory.GetFiles(carpeta).Order(StringComparer.Ordinal)]
            : [];
    }
}
