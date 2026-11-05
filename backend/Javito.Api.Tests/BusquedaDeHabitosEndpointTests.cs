using System.Net;
using System.Net.Http.Json;
using Javito.Api.Tests.Infraestructura;
using MySqlConnector;

namespace Javito.Api.Tests;

/// <summary>
/// HU-RUT-02, del lado del servidor: buscar el hábito por nombre mientras se escribe (S4) y que la
/// búsqueda sólo devuelva los activos de quien pregunta (S5). Lo que crea lleva el prefijo
/// <see cref="Prefijo"/> y la clase limpia lo suyo antes y después: los listados del seed afirman
/// sobre diecisiete hábitos.
/// </summary>
[Collection(ColeccionDeCategorias.Nombre)]
public class BusquedaDeHabitosEndpointTests(FabricaDeApi fabrica) : IAsyncLifetime
{
    private const string Prefijo = "Prueba ";

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

    // --- S4: buscar por pedazo del nombre, mientras se escribe --------------------------------

    [Theory]
    [InlineData("Zebr")]
    [InlineData("bracor")]
    [InlineData("cornio")]
    public async Task La_busqueda_encuentra_por_pedazo_del_titulo(string pedazo)
    {
        var creado = await CrearAsync(_usuario, $"{Prefijo}Zebracornio");

        var encontrados = await BuscarAsync(_usuario, pedazo);

        Assert.Equal(creado.Id, Assert.Single(encontrados).Id);
    }

    // --- S5: consulta el servidor y sólo devuelve mis hábitos activos --------------------------

    [Fact]
    public async Task La_busqueda_solo_devuelve_mis_habitos_activos()
    {
        var propio = await CrearAsync(_usuario, $"{Prefijo}Jirafa");
        var archivado = await CrearAsync(_usuario, $"{Prefijo}Jirafa vieja");
        await ArchivarAsync(archivado.Id);
        await CrearAsync(_admin, $"{Prefijo}Jirafa ajena");

        var encontrados = await BuscarAsync(_usuario, "Jirafa");

        var unico = Assert.Single(encontrados);
        Assert.Equal(propio.Id, unico.Id);
        Assert.True(unico.Activo);
    }

    [Fact]
    public async Task Un_comodin_de_like_se_busca_como_texto()
    {
        await CrearAsync(_usuario, $"{Prefijo}Agua");

        // Si el `%` viajara crudo a LIKE, este pedido devolvería la lista entera.
        Assert.Empty(await BuscarAsync(_usuario, "%"));
        Assert.Empty(await BuscarAsync(_usuario, "_"));
    }

    [Fact]
    public async Task Una_busqueda_en_blanco_no_filtra()
    {
        await CrearAsync(_usuario, $"{Prefijo}Ruido");

        var conQ = await BuscarAsync(_usuario, string.Empty);
        var sinQ = await _usuario.GetFromJsonAsync<List<HabitoDePrueba>>("/api/habitos");

        // Sin texto que buscar, el listado sigue trayendo exactamente lo mismo: es el contrato que
        // ya usaba la pantalla y el que afirma la prueba del listado del seed.
        Assert.NotNull(sinQ);
        Assert.Equal(sinQ.Count, conQ.Count);
    }

    private static async Task<HabitoDePrueba> CrearAsync(HttpClient cliente, string titulo)
    {
        var respuesta = await cliente.PostAsJsonAsync(
            "/api/habitos",
            new HabitoEntradaDePrueba(
                titulo,
                null,
                1,
                "si_no",
                null,
                null,
                new FrecuenciaDePrueba("diaria", null)));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);

        return await respuesta.Content.ReadFromJsonAsync<HabitoDePrueba>()
            ?? throw new InvalidOperationException("El alta no devolvió el hábito.");
    }

    private static Task<List<HabitoDePrueba>> BuscarAsync(HttpClient cliente, string q) =>
        cliente.GetFromJsonAsync<List<HabitoDePrueba>>($"/api/habitos?q={Uri.EscapeDataString(q)}")!;

    /// <summary>Baja lógica, como la haría el ABM: el hábito sigue existiendo pero ya no está activo.</summary>
    private static async Task ArchivarAsync(int habitoId)
    {
        await using var conexion = new MySqlConnection(BaseDeDatosDePrueba.CadenaDeConexion);
        await conexion.OpenAsync();

        await using var comando = new MySqlCommand(
            "UPDATE habito SET activo = FALSE WHERE id = @id",
            conexion);
        comando.Parameters.AddWithValue("@id", habitoId);

        await comando.ExecuteNonQueryAsync();
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
