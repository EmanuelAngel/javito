using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Javito.Api.Contratos;
using Javito.Api.Tests.Infraestructura;
using MySqlConnector;

namespace Javito.Api.Tests;

/// <summary>
/// HU-RUT-05: editar, desactivar y reactivar una rutina (S9-S12 de <c>odd/tasks/rutinas.md</c>, L5).
/// </summary>
[Collection(ColeccionDeRutinas.Nombre)]
public class RutinasEdicionEndpointTests(FabricaDeApi fabrica) : IAsyncLifetime
{
    private const string Prefijo = "Prueba ";

    // Los hábitos que crea esta clase no empiezan con «Prueba »: la limpieza de las pruebas de
    // hábitos corre en otra colección y los borraría a mitad de una prueba de acá.
    private const string PrefijoDeHabito = "Rutinas prueba ";

    private const int HabitoActivo = 1;
    private const int OtroHabitoActivo = 2;

    private readonly HttpClient _usuario = fabrica.CreateClient();
    private readonly HttpClient _admin = fabrica.CreateClient();

    public async Task InitializeAsync()
    {
        await LimpiarAsync();
        await SesionDePrueba.ClienteAutenticadoAsync(_usuario);
    }

    public async Task DisposeAsync()
    {
        await LimpiarAsync();
        _usuario.Dispose();
        _admin.Dispose();
    }

    [Fact]
    public async Task Editar_cambia_nombre_y_descripcion_y_conserva_sus_habitos()
    {
        var rutina = await CrearRutinaAsync(_usuario, $"{Prefijo}Mañana");
        await AgregarAsync(rutina.Id, HabitoActivo);

        var respuesta = await _usuario.PutAsJsonAsync(
            $"/api/rutinas/{rutina.Id}",
            new { nombre = $"  {Prefijo}Mañana temprano ", descripcion = "   " });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var editada = await respuesta.Content.ReadFromJsonAsync<RutinaDto>();
        Assert.Equal(new RutinaDto(rutina.Id, $"{Prefijo}Mañana temprano", null, true), editada);

        var detalle = await DetalleAsync(rutina.Id);
        Assert.Equal($"{Prefijo}Mañana temprano", detalle.Nombre);
        Assert.Null(detalle.Descripcion);
        Assert.Equal([HabitoActivo], detalle.Habitos.Select(habito => habito.Id));
    }

    [Fact]
    public async Task Editar_sin_nombre_da_400_y_una_rutina_ajena_o_inexistente_da_404()
    {
        var rutina = await CrearRutinaAsync(_usuario, $"{Prefijo}Validar");
        await SesionDePrueba.ClienteAdministradorAsync(_admin);
        var ajena = await CrearRutinaAsync(_admin, $"{Prefijo}Del admin");

        var sinNombre = await _usuario.PutAsJsonAsync($"/api/rutinas/{rutina.Id}", new { nombre = "   " });
        var deOtraCuenta = await _usuario.PutAsJsonAsync($"/api/rutinas/{ajena.Id}", new { nombre = $"{Prefijo}Robada" });
        var inexistente = await _usuario.PutAsJsonAsync("/api/rutinas/999999", new { nombre = $"{Prefijo}Fantasma" });

        Assert.Equal(HttpStatusCode.BadRequest, sinNombre.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deOtraCuenta.StatusCode);
        Assert.Equal("no_encontrado", await CodigoAsync(deOtraCuenta));
        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
        Assert.Equal("no_encontrado", await CodigoAsync(inexistente));

        Assert.Equal($"{Prefijo}Validar", (await DetalleAsync(rutina.Id)).Nombre);
        Assert.Equal($"{Prefijo}Del admin", (await _admin.GetFromJsonAsync<RutinaDetalleDto>($"/api/rutinas/{ajena.Id}"))!.Nombre);
    }

    [Fact]
    public async Task Desactivar_y_reactivar_conserva_la_rutina_sus_habitos_y_su_orden()
    {
        var rutina = await CrearRutinaAsync(_usuario, $"{Prefijo}Noche");
        await AgregarAsync(rutina.Id, OtroHabitoActivo);
        await AgregarAsync(rutina.Id, HabitoActivo);

        var baja = await _usuario.PutAsJsonAsync($"/api/rutinas/{rutina.Id}/activo", new { activo = false });
        // Repetir el estado que ya tiene no es un error, como en categorías y hábitos.
        var otraVez = await _usuario.PutAsJsonAsync($"/api/rutinas/{rutina.Id}/activo", new { activo = false });

        Assert.Equal(HttpStatusCode.OK, baja.StatusCode);
        Assert.False((await baja.Content.ReadFromJsonAsync<RutinaDto>())!.Activo);
        Assert.Equal(HttpStatusCode.OK, otraVez.StatusCode);

        // No se borra: sigue en el listado, marcada inactiva, y su detalle conserva los hábitos.
        var listado = await _usuario.GetFromJsonAsync<List<RutinaDto>>("/api/rutinas");
        Assert.False(Assert.Single(listado!, actual => actual.Id == rutina.Id).Activo);
        Assert.Equal([OtroHabitoActivo, HabitoActivo], (await DetalleAsync(rutina.Id)).Habitos.Select(habito => habito.Id));

        var alta = await _usuario.PutAsJsonAsync($"/api/rutinas/{rutina.Id}/activo", new { activo = true });

        Assert.Equal(HttpStatusCode.OK, alta.StatusCode);
        var reactivada = await DetalleAsync(rutina.Id);
        Assert.True(reactivada.Activo);
        Assert.Equal([OtroHabitoActivo, HabitoActivo], reactivada.Habitos.Select(habito => habito.Id));
    }

    [Fact]
    public async Task Cambiar_el_estado_sin_activo_da_400_y_de_una_rutina_ajena_da_404()
    {
        var rutina = await CrearRutinaAsync(_usuario, $"{Prefijo}Estado");
        await SesionDePrueba.ClienteAdministradorAsync(_admin);
        var ajena = await CrearRutinaAsync(_admin, $"{Prefijo}Del admin estado");

        var sinEstado = await _usuario.PutAsJsonAsync($"/api/rutinas/{rutina.Id}/activo", new { });
        var deOtraCuenta = await _usuario.PutAsJsonAsync($"/api/rutinas/{ajena.Id}/activo", new { activo = false });

        Assert.Equal(HttpStatusCode.BadRequest, sinEstado.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deOtraCuenta.StatusCode);
        Assert.Equal("no_encontrado", await CodigoAsync(deOtraCuenta));
        Assert.True((await DetalleAsync(rutina.Id)).Activo);
        Assert.True((await _admin.GetFromJsonAsync<RutinaDetalleDto>($"/api/rutinas/{ajena.Id}"))!.Activo);
    }

    [Fact]
    public async Task Archivar_un_habito_lo_quita_de_la_rutina_y_reactivarlo_lo_devuelve_a_su_lugar()
    {
        var rutina = await CrearRutinaAsync(_usuario, $"{Prefijo}Con archivado");
        var habito = await CrearHabitoAsync($"{PrefijoDeHabito}del medio");
        await AgregarAsync(rutina.Id, HabitoActivo);
        await AgregarAsync(rutina.Id, habito);
        await AgregarAsync(rutina.Id, OtroHabitoActivo);

        await _usuario.PutAsJsonAsync($"/api/habitos/{habito}/activo", new { activo = false });

        var conArchivado = await _usuario.GetAsync($"/api/rutinas/{rutina.Id}");
        Assert.Equal(HttpStatusCode.OK, conArchivado.StatusCode);
        var sinEl = await conArchivado.Content.ReadFromJsonAsync<RutinaDetalleDto>();
        Assert.Equal([HabitoActivo, OtroHabitoActivo], sinEl!.Habitos.Select(actual => actual.Id));

        await _usuario.PutAsJsonAsync($"/api/habitos/{habito}/activo", new { activo = true });

        Assert.Equal(
            [HabitoActivo, habito, OtroHabitoActivo],
            (await DetalleAsync(rutina.Id)).Habitos.Select(actual => actual.Id));
    }

    private async Task<RutinaDetalleDto> DetalleAsync(int id) =>
        await _usuario.GetFromJsonAsync<RutinaDetalleDto>($"/api/rutinas/{id}")
            ?? throw new InvalidOperationException("El detalle no devolvió la rutina.");

    private static async Task<RutinaDto> CrearRutinaAsync(HttpClient cliente, string nombre)
    {
        var respuesta = await cliente.PostAsJsonAsync("/api/rutinas", new { nombre });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);

        return await respuesta.Content.ReadFromJsonAsync<RutinaDto>()
            ?? throw new InvalidOperationException("El alta no devolvió la rutina.");
    }

    private async Task<int> CrearHabitoAsync(string titulo)
    {
        var respuesta = await _usuario.PostAsJsonAsync(
            "/api/habitos",
            new { titulo, categoriaId = 1, tipo = "si_no", frecuencia = new { tipo = "diaria" } });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);

        return (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    private async Task AgregarAsync(int rutinaId, int habitoId)
    {
        var respuesta = await _usuario.PostAsJsonAsync($"/api/rutinas/{rutinaId}/habitos", new { habitoId });

        Assert.True(respuesta.IsSuccessStatusCode, $"No se pudo agregar el hábito {habitoId}: {respuesta.StatusCode}");
    }

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

            DELETE rh FROM rutina_habito rh
            JOIN habito h ON h.id = rh.habito_id
            WHERE h.titulo LIKE 'Rutinas prueba %';

            DELETE r FROM registro_habito r
            JOIN habito h ON h.id = r.habito_id
            WHERE h.titulo LIKE 'Rutinas prueba %';

            DELETE FROM habito WHERE titulo LIKE 'Rutinas prueba %';
            """,
            conexion);

        await comando.ExecuteNonQueryAsync();
    }
}
