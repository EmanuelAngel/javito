using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Javito.Api.Modelos;
using Javito.Api.Repositorios;
using Javito.Api.Tests.Infraestructura;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;

namespace Javito.Api.Tests;

/// <summary>
/// HU-ART-02: el Administrador corrige un artículo publicado sin despublicarlo. Una prueba por regla
/// (S19-S23 de odd/tasks/articulos.md); cada una mira la respuesta, la fila releída por la API y,
/// cuando hay portada en juego, el disco.
/// </summary>
[Collection(ColeccionDeArticulos.Nombre)]
public class ArticulosEdicionEndpointTests(FabricaDeApi fabrica) : IAsyncLifetime
{
    /// <summary>PNG 1x1 real: la validación mira la firma, así que un encabezado inventado no sirve.</summary>
    private static readonly byte[] PngValido = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAIAAACQd1PeAAAADElEQVR4nGPQqw4CAAHWAPz6Qz2AAAAAAElFTkSuQmCC");

    private static readonly byte[] NoEsImagen = Encoding.UTF8.GetBytes("<html><body>no soy una imagen</body></html>");

    /// <summary>Lo que crea cada prueba lleva este prefijo; la clase borra sólo lo suyo.</summary>
    private const string Prefijo = "Prueba edición ";

    /// <summary>Categorías del seed: Mente y Cuerpo activas, Espiritualidad retirada.</summary>
    private const int CategoriaActiva = 2;

    private const int OtraCategoriaActiva = 1;

    private const int CategoriaInactiva = 5;

    /// <summary>Artículo del seed que está dado de baja (HU-ART-03).</summary>
    private const int ArticuloInactivoDelSeed = 4;

    /// <summary>
    /// La fecha con la que se "publicó" cada artículo de prueba: vieja a propósito, para que una
    /// edición que la pise con la de hoy no pase inadvertida.
    /// </summary>
    private const string FechaOriginal = "2026-01-15";

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

    // --- S19 + S21: editar los campos y conservar la fecha -----------------------------------

    [Fact]
    public async Task Editar_sin_portada_nueva_cambia_los_campos_y_conserva_portada_fecha_y_activo()
    {
        var original = await CrearAsync("Original", portada: PngValido);
        var id = original.GetProperty("id").GetInt32();
        var portada = original.GetProperty("imagenPortadaUrl").GetString();

        var respuesta = await _admin.PutAsync(
            $"/api/articulos/{id}",
            Formulario($"{Prefijo}Corregido", "Resumen corregido", "# Corregido\n\nCon **otra** negrita.",
                OtraCategoriaActiva));

        Assert.True(
            respuesta.StatusCode == HttpStatusCode.OK,
            $"La edición falló con {respuesta.StatusCode}: {await respuesta.Content.ReadAsStringAsync()}");

        // La respuesta y la lectura posterior dicen lo mismo: lo guardado es lo que se lee.
        foreach (var editado in new[]
                 {
                     await respuesta.Content.ReadFromJsonAsync<JsonElement>(),
                     await LeerAsync(id)
                 })
        {
            Assert.Equal(id, editado.GetProperty("id").GetInt32());
            Assert.Equal($"{Prefijo}Corregido", editado.GetProperty("titulo").GetString());
            Assert.Equal("Resumen corregido", editado.GetProperty("resumen").GetString());
            Assert.Equal("# Corregido\n\nCon **otra** negrita.", editado.GetProperty("contenidoMarkdown").GetString());
            Assert.Equal(OtraCategoriaActiva, editado.GetProperty("categoriaId").GetInt32());

            // Sin archivo nuevo la portada se conserva: no hay forma de quitarla.
            Assert.Equal(portada, editado.GetProperty("imagenPortadaUrl").GetString());

            // "La fecha de publicación original no cambia al editar", y editar no despublica.
            Assert.Equal(FechaOriginal, editado.GetProperty("fechaPublicacion").GetString());
            Assert.True(editado.GetProperty("activo").GetBoolean());
        }

        // La portada sigue en disco y sirviéndose.
        Assert.Equal(HttpStatusCode.OK, (await _usuario.GetAsync(portada)).StatusCode);
    }

    // --- S20: reemplazar la portada sin dejar huérfanas ----------------------------------------

    [Fact]
    public async Task Editar_con_portada_nueva_la_reemplaza_y_borra_la_anterior_del_disco()
    {
        var original = await CrearAsync("Con portada", portada: PngValido);
        var id = original.GetProperty("id").GetInt32();
        var anterior = original.GetProperty("imagenPortadaUrl").GetString()!;

        var respuesta = await _admin.PutAsync(
            $"/api/articulos/{id}",
            Formulario($"{Prefijo}Con portada", "Un resumen", "# Nada", portada: PngValido));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var nueva = (await respuesta.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("imagenPortadaUrl").GetString()!;

        Assert.Matches(@"^/uploads/articulos/[0-9a-f]{32}\.png$", nueva);
        Assert.NotEqual(anterior, nueva);
        Assert.Equal(nueva, (await LeerAsync(id)).GetProperty("imagenPortadaUrl").GetString());

        // La nueva está en disco y se sirve; la anterior ya no existe: no quedó huérfana.
        Assert.True(File.Exists(EnDisco(nueva)), "La portada nueva tiene que estar en disco.");
        Assert.False(File.Exists(EnDisco(anterior)), "La portada anterior tenía que borrarse.");
        Assert.Equal(HttpStatusCode.OK, (await _usuario.GetAsync(nueva)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _usuario.GetAsync(anterior)).StatusCode);
    }

    [Fact]
    public async Task Editar_con_algo_que_no_es_imagen_devuelve_400_y_deja_el_articulo_y_el_disco_como_estaban()
    {
        var original = await CrearAsync("Portada falsa", portada: PngValido);
        var id = original.GetProperty("id").GetInt32();
        var antes = ArchivosDePortada();

        var respuesta = await _admin.PutAsync(
            $"/api/articulos/{id}",
            Formulario($"{Prefijo}No debería guardarse", "Otro", "# Otro", portada: NoEsImagen));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("archivo_no_es_imagen", (await respuesta.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("codigo").GetString());

        AssertSinCambios(original, await LeerAsync(id));
        Assert.Equal(antes, ArchivosDePortada());
    }

    [Fact]
    public async Task Si_la_fila_no_se_actualiza_se_borra_la_portada_nueva_y_queda_la_anterior()
    {
        var original = await CrearAsync("Falla al guardar", portada: PngValido);
        var id = original.GetProperty("id").GetInt32();
        var antes = ArchivosDePortada();

        // La misma API con un repositorio que falla justo al actualizar: es la única forma de llegar
        // al camino en que la imagen nueva ya está en disco y la fila no la aceptó.
        await using var conFalla = fabrica.WithWebHostBuilder(builder => builder.ConfigureTestServices(servicios =>
            servicios.AddScoped<IArticuloRepositorio>(proveedor => new RepositorioQueFallaAlActualizar(
                ActivatorUtilities.CreateInstance<ArticuloRepositorio>(proveedor)))));

        var admin = conFalla.CreateClient();
        await SesionDePrueba.ClienteAdministradorAsync(admin);

        var respuesta = await admin.PutAsync(
            $"/api/articulos/{id}",
            Formulario($"{Prefijo}No debería guardarse", "Otro", "# Otro", portada: PngValido));

        Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);

        // La fila sigue con la portada anterior, que sigue en disco, y la nueva no quedó huérfana.
        AssertSinCambios(original, await LeerAsync(id));
        Assert.Equal(antes, ArchivosDePortada());
    }

    // --- S22: la categoría ---------------------------------------------------------------------

    [Fact]
    public async Task Editar_conservando_una_categoria_que_se_desactivo_despues_esta_permitido()
    {
        var original = await CrearAsync("Categoría retirada");
        var id = original.GetProperty("id").GetInt32();

        // Publicado cuando su categoría estaba activa; la categoría se retiró después.
        await EjecutarAsync($"UPDATE articulo SET categoria_id = {CategoriaInactiva} WHERE id = {id}");

        var respuesta = await _admin.PutAsync(
            $"/api/articulos/{id}",
            Formulario($"{Prefijo}Categoría retirada", "Resumen corregido", "# Nada", CategoriaInactiva));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var leido = await LeerAsync(id);
        Assert.Equal(CategoriaInactiva, leido.GetProperty("categoriaId").GetInt32());
        Assert.Equal("Resumen corregido", leido.GetProperty("resumen").GetString());
    }

    [Theory]
    [InlineData(CategoriaInactiva)]
    [InlineData(999_999)]
    public async Task Cambiar_a_una_categoria_inactiva_o_inexistente_devuelve_400_sin_guardar_ni_subir_nada(
        int categoriaId)
    {
        var original = await CrearAsync("Cambio de categoría");
        var id = original.GetProperty("id").GetInt32();
        var antes = ArchivosDePortada();

        var respuesta = await _admin.PutAsync(
            $"/api/articulos/{id}",
            Formulario($"{Prefijo}No debería guardarse", "Otro", "# Otro", categoriaId, PngValido));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("categoria_invalida", problema.GetProperty("codigo").GetString());
        Assert.Equal(400, problema.GetProperty("status").GetInt32());

        // La categoría se valida antes de guardar la portada: el disco queda como estaba.
        AssertSinCambios(original, await LeerAsync(id));
        Assert.Equal(antes, ArchivosDePortada());
    }

    [Fact]
    public async Task Editar_con_un_titulo_vacio_devuelve_400_con_el_error_del_campo()
    {
        var original = await CrearAsync("Título vacío");
        var id = original.GetProperty("id").GetInt32();

        var respuesta = await _admin.PutAsync($"/api/articulos/{id}", Formulario("   ", "Otro", "# Otro"));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.True((await respuesta.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("errors").TryGetProperty("Titulo", out _));
        AssertSinCambios(original, await LeerAsync(id));
    }

    // --- S23: quién y sobre qué ----------------------------------------------------------------

    [Fact]
    public async Task Editar_sin_sesion_devuelve_401_y_como_usuario_devuelve_403_sin_tocar_la_fila()
    {
        var original = await CrearAsync("Sólo Administración");
        var id = original.GetProperty("id").GetInt32();

        var anonimo = await _anonimo.PutAsync($"/api/articulos/{id}", Formulario($"{Prefijo}Intruso", "x", "# x"));
        Assert.Equal(HttpStatusCode.Unauthorized, anonimo.StatusCode);
        Assert.Equal("no_autenticado", (await anonimo.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("codigo").GetString());

        var usuario = await _usuario.PutAsync($"/api/articulos/{id}", Formulario($"{Prefijo}Intruso", "x", "# x"));
        Assert.Equal(HttpStatusCode.Forbidden, usuario.StatusCode);
        Assert.Equal("sin_permiso", (await usuario.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("codigo").GetString());

        AssertSinCambios(original, await LeerAsync(id));
    }

    [Theory]
    [InlineData(ArticuloInactivoDelSeed)]
    [InlineData(999_999)]
    public async Task Editar_un_articulo_inactivo_o_inexistente_devuelve_404_sin_guardar_ni_subir_nada(int id)
    {
        var antes = ArchivosDePortada();
        var filaAntes = await FilaAsync(id);

        var respuesta = await _admin.PutAsync(
            $"/api/articulos/{id}",
            Formulario($"{Prefijo}No debería guardarse", "Otro", "# Otro", portada: PngValido));

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal("no_encontrado", (await respuesta.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("codigo").GetString());

        Assert.Equal(filaAntes, await FilaAsync(id));
        Assert.Equal(antes, ArchivosDePortada());
    }

    // --- utilidades --------------------------------------------------------------------------

    /// <summary>Publica un artículo por la API y le pone <see cref="FechaOriginal"/> como fecha.</summary>
    private async Task<JsonElement> CrearAsync(string titulo, byte[]? portada = null)
    {
        var respuesta = await _admin.PostAsync(
            "/api/articulos",
            Formulario($"{Prefijo}{titulo}", "Un resumen", "# Nada", portada: portada));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);

        var id = (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        await EjecutarAsync($"UPDATE articulo SET fecha_publicacion = '{FechaOriginal} 10:00:00' WHERE id = {id}");

        return await LeerAsync(id);
    }

    private async Task<JsonElement> LeerAsync(int id)
    {
        var respuesta = await _usuario.GetAsync($"/api/articulos/{id}");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        return await respuesta.Content.ReadFromJsonAsync<JsonElement>();
    }

    /// <summary>El artículo releído es igual, campo por campo, al de antes del intento.</summary>
    private static void AssertSinCambios(JsonElement antes, JsonElement despues) =>
        Assert.Equal(antes.GetRawText(), despues.GetRawText());

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

    private string EnDisco(string rutaWeb) =>
        Path.Combine(fabrica.RutaDeArchivos, "articulos", Path.GetFileName(rutaWeb));

    private string[] ArchivosDePortada()
    {
        var carpeta = Path.Combine(fabrica.RutaDeArchivos, "articulos");

        return Directory.Exists(carpeta) ? Directory.GetFiles(carpeta).Order().ToArray() : [];
    }

    /// <summary>La fila cruda, para afirmar sobre artículos que la API no deja leer (inactivos).</summary>
    private static async Task<string?> FilaAsync(int id)
    {
        await using var conexion = new MySqlConnection(BaseDeDatosDePrueba.CadenaDeConexion);
        await conexion.OpenAsync();

        await using var comando = new MySqlCommand(
            """
            SELECT CONCAT_WS('|', titulo, resumen, contenido_markdown, categoria_id,
                IFNULL(imagen_portada_url, ''), fecha_publicacion, activo)
            FROM articulo WHERE id = @id
            """,
            conexion);
        comando.Parameters.AddWithValue("@id", id);

        return (string?)await comando.ExecuteScalarAsync();
    }

    private static async Task EjecutarAsync(string sql)
    {
        await using var conexion = new MySqlConnection(BaseDeDatosDePrueba.CadenaDeConexion);
        await conexion.OpenAsync();

        await using var comando = new MySqlCommand(sql, conexion);
        await comando.ExecuteNonQueryAsync();
    }

    /// <summary>Borra sólo lo que crearon estas pruebas (DELETE real: la tabla es del seed).</summary>
    private static Task LimpiarAsync() => EjecutarAsync("DELETE FROM articulo WHERE titulo LIKE 'Prueba edición %'");

    /// <summary>Delega todo en el repositorio real salvo la actualización, que falla como lo haría la base.</summary>
    private sealed class RepositorioQueFallaAlActualizar(IArticuloRepositorio real) : IArticuloRepositorio
    {
        public Task<int> CrearAsync(Articulo articulo, CancellationToken cancellationToken = default) =>
            real.CrearAsync(articulo, cancellationToken);

        public Task<Articulo?> BuscarAsync(int id, CancellationToken cancellationToken = default) =>
            real.BuscarAsync(id, cancellationToken);

        public Task<(IReadOnlyList<Articulo> Elementos, int Total)> ListarCatalogoAsync(
            int? categoriaId,
            int limite,
            long desplazamiento,
            CancellationToken cancellationToken = default) =>
            real.ListarCatalogoAsync(categoriaId, limite, desplazamiento, cancellationToken);

        public Task<bool> CambiarActivoAsync(int id, bool activo, CancellationToken cancellationToken = default) =>
            real.CambiarActivoAsync(id, activo, cancellationToken);

        public Task<(IReadOnlyList<Articulo> Elementos, int Total)> ListarTodosAsync(
            int limite,
            long desplazamiento,
            CancellationToken cancellationToken = default) =>
            real.ListarTodosAsync(limite, desplazamiento, cancellationToken);

        public Task<bool> ActualizarAsync(Articulo articulo, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("La base rechazó la actualización.");
    }
}
