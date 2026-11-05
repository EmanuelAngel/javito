using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Javito.Api.Contratos;
using Javito.Api.Tests.Infraestructura;
using MySqlConnector;

namespace Javito.Api.Tests;

/// <summary>
/// HU-RUT-01: crear una rutina. Una prueba por regla de aceptación (S1, S2 y S3 de
/// <c>odd/tasks/rutinas.md</c>) más el aislamiento entre cuentas, que es lo que sostiene que una
/// rutina sea «mía».
/// </summary>
[Collection(ColeccionDeRutinas.Nombre)]
public class RutinasEndpointTests(FabricaDeApi fabrica) : IAsyncLifetime
{
    private const string Prefijo = "Prueba ";

    private readonly HttpClient _usuario = fabrica.CreateClient();
    private readonly HttpClient _admin = fabrica.CreateClient();

    public Task InitializeAsync() => LimpiarAsync();

    public async Task DisposeAsync()
    {
        await LimpiarAsync();
        _usuario.Dispose();
        _admin.Dispose();
    }

    // --- S1: nombre obligatorio y descripción opcional ---------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1234567890123456789012345678901234567890123456789012345678901")]
    public async Task Crear_sin_nombre_valido_devuelve_400_con_el_error_del_campo(string nombre)
    {
        await SesionDePrueba.ClienteAutenticadoAsync(_usuario);

        var respuesta = await _usuario.PostAsJsonAsync("/api/rutinas", new { nombre });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        var campos = problema.GetProperty("errors").EnumerateObject().ToList();
        var delNombre = Assert.Single(
            campos,
            campo => string.Equals(campo.Name, "Nombre", StringComparison.OrdinalIgnoreCase));

        Assert.NotEmpty(delNombre.Value.EnumerateArray());
    }

    [Fact]
    public async Task Crear_sin_descripcion_o_con_espacios_la_guarda_en_null()
    {
        await SesionDePrueba.ClienteAutenticadoAsync(_usuario);

        var sinDescripcion = await _usuario.PostAsJsonAsync(
            "/api/rutinas",
            new { nombre = $"{Prefijo}Sin descripción" });
        var soloEspacios = await _usuario.PostAsJsonAsync(
            "/api/rutinas",
            new { nombre = $"{Prefijo}Con espacios", descripcion = "   " });

        Assert.Equal(HttpStatusCode.Created, sinDescripcion.StatusCode);
        Assert.Equal(HttpStatusCode.Created, soloEspacios.StatusCode);

        var primera = await sinDescripcion.Content.ReadFromJsonAsync<RutinaDto>();
        var segunda = await soloEspacios.Content.ReadFromJsonAsync<RutinaDto>();

        Assert.NotNull(primera);
        Assert.NotNull(segunda);
        Assert.Null(primera.Descripcion);
        Assert.Null(segunda.Descripcion);
    }

    // --- S2: nace activa, vacía y sin hábitos -------------------------------------------------

    [Fact]
    public async Task La_rutina_nace_activa_vacia_y_sin_habitos()
    {
        await SesionDePrueba.ClienteAutenticadoAsync(_usuario);

        var respuesta = await _usuario.PostAsJsonAsync(
            "/api/rutinas",
            new { nombre = $"{Prefijo}Mañana", descripcion = "Bloque de la mañana" });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);

        var creada = await respuesta.Content.ReadFromJsonAsync<RutinaDto>();
        Assert.NotNull(creada);
        Assert.True(creada.Activo);
        Assert.Equal("Bloque de la mañana", creada.Descripcion);
        Assert.Equal($"/api/rutinas/{creada.Id}", respuesta.Headers.Location?.OriginalString);

        var detalle = await _usuario.GetFromJsonAsync<RutinaDetalleDto>($"/api/rutinas/{creada.Id}");

        Assert.NotNull(detalle);
        Assert.Equal(creada.Id, detalle.Id);
        Assert.Empty(detalle.Habitos);
    }

    // --- S3: varias rutinas en el mismo momento del día ---------------------------------------

    [Fact]
    public async Task Se_pueden_tener_varias_rutinas_con_el_mismo_nombre()
    {
        await SesionDePrueba.ClienteAutenticadoAsync(_usuario);

        var primera = await _usuario.PostAsJsonAsync("/api/rutinas", new { nombre = $"{Prefijo}Corte" });
        var segunda = await _usuario.PostAsJsonAsync("/api/rutinas", new { nombre = $"{Prefijo}Corte" });

        Assert.Equal(HttpStatusCode.Created, primera.StatusCode);
        Assert.Equal(HttpStatusCode.Created, segunda.StatusCode);

        var una = await primera.Content.ReadFromJsonAsync<RutinaDto>();
        var otra = await segunda.Content.ReadFromJsonAsync<RutinaDto>();

        Assert.NotNull(una);
        Assert.NotNull(otra);
        Assert.NotEqual(una.Id, otra.Id);

        var listado = await _usuario.GetFromJsonAsync<List<RutinaDto>>("/api/rutinas");

        Assert.NotNull(listado);
        Assert.Contains(listado, rutina => rutina.Id == una.Id);
        Assert.Contains(listado, rutina => rutina.Id == otra.Id);
    }

    // --- Aislamiento: la rutina es mía o no existe --------------------------------------------

    [Fact]
    public async Task La_rutina_de_otra_cuenta_no_aparece_ni_se_puede_abrir()
    {
        await SesionDePrueba.ClienteAdministradorAsync(_admin);
        await SesionDePrueba.ClienteAutenticadoAsync(_usuario);

        var delAdmin = await _admin.PostAsJsonAsync("/api/rutinas", new { nombre = $"{Prefijo}Del admin" });

        Assert.Equal(HttpStatusCode.Created, delAdmin.StatusCode);
        var ajena = await delAdmin.Content.ReadFromJsonAsync<RutinaDto>();
        Assert.NotNull(ajena);

        var listado = await _usuario.GetFromJsonAsync<List<RutinaDto>>("/api/rutinas");

        Assert.NotNull(listado);
        Assert.DoesNotContain(listado, rutina => rutina.Id == ajena.Id);

        var detalle = await _usuario.GetAsync($"/api/rutinas/{ajena.Id}");

        Assert.Equal(HttpStatusCode.NotFound, detalle.StatusCode);

        var problema = await detalle.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("no_encontrado", problema.GetProperty("codigo").GetString());
    }

    /// <summary>Solo borra lo que crean estas pruebas: la base de test es la única que se toca.</summary>
    private static async Task LimpiarAsync()
    {
        await using var conexion = new MySqlConnection(BaseDeDatosDePrueba.CadenaDeConexion);
        await conexion.OpenAsync();

        await using var comando = new MySqlCommand(
            """
            DELETE rh FROM rutina_habito rh
            JOIN rutina r ON r.id = rh.rutina_id
            WHERE r.nombre LIKE 'Prueba %';

            DELETE FROM rutina WHERE nombre LIKE 'Prueba %';
            """,
            conexion);

        await comando.ExecuteNonQueryAsync();
    }
}
