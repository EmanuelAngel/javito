using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Javito.Api.Contratos;
using Javito.Api.Tests.Infraestructura;
using MySqlConnector;

namespace Javito.Api.Tests;

/// <summary>
/// HU-RUT-02 del lado de la relación: S6 (el mismo hábito no entra dos veces), S7 (un hábito puede
/// estar en varias rutinas) y la numeración de `rutina_habito.orden`, que el schema exige aunque
/// reordenar (RUT-03) esté fuera de alcance.
/// </summary>
/// <remarks>
/// Usa los hábitos del seed (1…16 activos, 17 archivado) y no crea ninguno: los hábitos son de la
/// colección de hábitos, y crearlos acá le cambiaría el listado que esa colección afirma.
/// </remarks>
[Collection(ColeccionDeRutinas.Nombre)]
public class RutinasHabitosEndpointTests(FabricaDeApi fabrica) : IAsyncLifetime
{
    private const string Prefijo = "Prueba ";
    private const int HabitoActivo = 1;
    private const int OtroHabitoActivo = 2;
    private const int HabitoArchivado = 17;

    private readonly HttpClient _usuario = fabrica.CreateClient();
    private readonly HttpClient _admin = fabrica.CreateClient();

    public async Task InitializeAsync()
    {
        await SesionDePrueba.ClienteAutenticadoAsync(_usuario);
        await SesionDePrueba.ClienteAdministradorAsync(_admin);
        await LimpiarAsync();
    }

    public async Task DisposeAsync()
    {
        await LimpiarAsync();
        _usuario.Dispose();
        _admin.Dispose();
    }

    // --- numeración dentro de la rutina -------------------------------------------------------

    [Fact]
    public async Task Agregar_encola_el_habito_al_final_y_el_orden_sobrevive_al_detalle()
    {
        var rutina = await CrearRutinaAsync(_usuario, $"{Prefijo}Orden");

        var primero = await AgregarAsync(_usuario, rutina.Id, HabitoActivo);
        var segundo = await AgregarAsync(_usuario, rutina.Id, OtroHabitoActivo);

        Assert.Equal(HttpStatusCode.Created, primero.StatusCode);
        Assert.Equal(HttpStatusCode.Created, segundo.StatusCode);

        var uno = await primero.Content.ReadFromJsonAsync<HabitoEnRutinaDto>();
        var dos = await segundo.Content.ReadFromJsonAsync<HabitoEnRutinaDto>();

        Assert.NotNull(uno);
        Assert.NotNull(dos);
        Assert.Equal(1, uno.Orden);
        Assert.Equal(2, dos.Orden);
        Assert.Equal($"/api/rutinas/{rutina.Id}/habitos/{HabitoActivo}", primero.Headers.Location?.OriginalString);

        var detalle = await _usuario.GetFromJsonAsync<RutinaDetalleDto>($"/api/rutinas/{rutina.Id}");

        Assert.NotNull(detalle);
        Assert.Equal([HabitoActivo, OtroHabitoActivo], detalle.Habitos.Select(habito => habito.Id));
        Assert.Equal([1, 2], detalle.Habitos.Select(habito => habito.Orden));
    }

    // --- S6: el mismo hábito no se puede agregar dos veces -------------------------------------

    [Fact]
    public async Task El_mismo_habito_no_entra_dos_veces_en_la_misma_rutina()
    {
        var rutina = await CrearRutinaAsync(_usuario, $"{Prefijo}Repetido");

        await AgregarAsync(_usuario, rutina.Id, HabitoActivo);
        var repetido = await AgregarAsync(_usuario, rutina.Id, HabitoActivo);

        Assert.Equal(HttpStatusCode.Conflict, repetido.StatusCode);
        Assert.Equal("habito_ya_en_rutina", await CodigoAsync(repetido));

        var detalle = await _usuario.GetFromJsonAsync<RutinaDetalleDto>($"/api/rutinas/{rutina.Id}");

        Assert.NotNull(detalle);
        Assert.Equal(HabitoActivo, Assert.Single(detalle.Habitos).Id);
    }

    // --- S7: un hábito puede estar en varias rutinas a la vez ---------------------------------

    [Fact]
    public async Task Un_habito_puede_estar_en_varias_rutinas()
    {
        var mañana = await CrearRutinaAsync(_usuario, $"{Prefijo}Mañana");
        var noche = await CrearRutinaAsync(_usuario, $"{Prefijo}Noche");

        var enMañana = await AgregarAsync(_usuario, mañana.Id, HabitoActivo);
        var enNoche = await AgregarAsync(_usuario, noche.Id, HabitoActivo);

        Assert.Equal(HttpStatusCode.Created, enMañana.StatusCode);
        Assert.Equal(HttpStatusCode.Created, enNoche.StatusCode);

        var una = await _usuario.GetFromJsonAsync<RutinaDetalleDto>($"/api/rutinas/{mañana.Id}");
        var otra = await _usuario.GetFromJsonAsync<RutinaDetalleDto>($"/api/rutinas/{noche.Id}");

        Assert.Equal(HabitoActivo, Assert.Single(una!.Habitos).Id);
        Assert.Equal(HabitoActivo, Assert.Single(otra!.Habitos).Id);
    }

    // --- errores previstos: el hábito que falla es el del cuerpo del alta ----------------------

    [Fact]
    public async Task Un_habito_inexistente_o_de_otra_cuenta_devuelve_400_habito_inexistente()
    {
        var rutina = await CrearRutinaAsync(_usuario, $"{Prefijo}Inexistente");

        var inexistente = await AgregarAsync(_usuario, rutina.Id, 9999);

        Assert.Equal(HttpStatusCode.BadRequest, inexistente.StatusCode);
        Assert.Equal("habito_inexistente", await CodigoAsync(inexistente));

        // El hábito 1 es de la cuenta «usuario»: para la cuenta «admin» no existe.
        var delAdmin = await CrearRutinaAsync(_admin, $"{Prefijo}Del admin");
        var ajeno = await AgregarAsync(_admin, delAdmin.Id, HabitoActivo);

        Assert.Equal(HttpStatusCode.BadRequest, ajeno.StatusCode);
        Assert.Equal("habito_inexistente", await CodigoAsync(ajeno));
    }

    [Fact]
    public async Task Un_habito_archivado_no_se_puede_agregar()
    {
        var rutina = await CrearRutinaAsync(_usuario, $"{Prefijo}Archivado");

        var respuesta = await AgregarAsync(_usuario, rutina.Id, HabitoArchivado);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("habito_archivado", await CodigoAsync(respuesta));
    }

    [Fact]
    public async Task La_rutina_de_otra_cuenta_no_acepta_habitos()
    {
        var delAdmin = await CrearRutinaAsync(_admin, $"{Prefijo}Propia del admin");

        var respuesta = await AgregarAsync(_usuario, delAdmin.Id, HabitoActivo);

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal("no_encontrado", await CodigoAsync(respuesta));
    }

    // --- S8: quitar un hábito de la rutina sin archivarlo -------------------------------------

    [Fact]
    public async Task Quitar_el_habito_lo_deja_activo_y_fuera_de_la_rutina()
    {
        var rutina = await CrearRutinaAsync(_usuario, $"{Prefijo}Quitar");
        await AgregarAsync(_usuario, rutina.Id, HabitoActivo);

        var respuesta = await _usuario.DeleteAsync($"/api/rutinas/{rutina.Id}/habitos/{HabitoActivo}");

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);

        var detalle = await _usuario.GetFromJsonAsync<RutinaDetalleDto>($"/api/rutinas/{rutina.Id}");

        Assert.NotNull(detalle);
        Assert.Empty(detalle.Habitos);

        // Lo que pide la historia: se cae la relación, no el hábito.
        var habitos = await _usuario.GetFromJsonAsync<List<HabitoDePrueba>>("/api/habitos");
        var habito = Assert.Single(habitos!, candidato => candidato.Id == HabitoActivo);

        Assert.True(habito.Activo);
    }

    [Fact]
    public async Task Quitar_un_habito_que_no_esta_en_la_rutina_es_idempotente()
    {
        var rutina = await CrearRutinaAsync(_usuario, $"{Prefijo}Idempotente");

        var primera = await _usuario.DeleteAsync($"/api/rutinas/{rutina.Id}/habitos/{HabitoActivo}");
        var segunda = await _usuario.DeleteAsync($"/api/rutinas/{rutina.Id}/habitos/{HabitoActivo}");

        Assert.Equal(HttpStatusCode.NoContent, primera.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, segunda.StatusCode);
    }

    [Fact]
    public async Task No_se_pueden_quitar_habitos_de_una_rutina_ajena()
    {
        var delAdmin = await CrearRutinaAsync(_admin, $"{Prefijo}Quitar ajena");

        var respuesta = await _usuario.DeleteAsync($"/api/rutinas/{delAdmin.Id}/habitos/{HabitoActivo}");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal("no_encontrado", await CodigoAsync(respuesta));
    }

    private static async Task<RutinaDto> CrearRutinaAsync(HttpClient cliente, string nombre)
    {
        var respuesta = await cliente.PostAsJsonAsync("/api/rutinas", new { nombre });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);

        return await respuesta.Content.ReadFromJsonAsync<RutinaDto>()
            ?? throw new InvalidOperationException("El alta no devolvió la rutina.");
    }

    private static Task<HttpResponseMessage> AgregarAsync(HttpClient cliente, int rutinaId, int habitoId) =>
        cliente.PostAsJsonAsync($"/api/rutinas/{rutinaId}/habitos", new { habitoId });

    private static async Task<string?> CodigoAsync(HttpResponseMessage respuesta)
    {
        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();

        return problema.GetProperty("codigo").GetString();
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
