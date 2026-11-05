using System.Net;
using System.Net.Http.Json;
using Javito.Api.Contratos;
using Javito.Api.Tests.Infraestructura;
using MySqlConnector;

namespace Javito.Api.Tests;

/// <summary>
/// Dos altas simultáneas de hábitos en la misma rutina no pueden perder ninguna respuesta con 5xx ni
/// repetir el lugar. El `MAX(orden) + 1` tiene que resolverse sin pelear locks de rango en la tabla
/// hija: lo serializa el lock de la fila de `rutina`.
/// </summary>
/// <remarks>
/// El fallo que pinta esta prueba es un deadlock de InnoDB (error 1213), no un dato corrupto: cuando
/// aparece, la fila no se inserta y la API responde 500. Un doble submit o dos pestañas lo alcanzan.
/// </remarks>
[Collection(ColeccionDeRutinas.Nombre)]
public class RutinasConcurrenciaEndpointTests(FabricaDeApi fabrica) : IAsyncLifetime
{
    private const string Prefijo = "Prueba ";
    private const int Rondas = 10;

    private readonly HttpClient _usuario = fabrica.CreateClient();

    public async Task InitializeAsync()
    {
        await SesionDePrueba.ClienteAutenticadoAsync(_usuario);
        await LimpiarAsync();
    }

    public async Task DisposeAsync()
    {
        await LimpiarAsync();
        _usuario.Dispose();
    }

    [Fact]
    public async Task Dos_altas_simultaneas_no_devuelven_500()
    {
        var fallos = 0;

        for (var ronda = 0; ronda < Rondas; ronda++)
        {
            var rutina = await CrearRutinaAsync($"dos {ronda}");
            var respuestas = await Task.WhenAll(
                AgregarAsync(rutina.Id, 1),
                AgregarAsync(rutina.Id, 2));

            fallos += respuestas.Count(respuesta => (int)respuesta.StatusCode >= 500);
        }

        Assert.True(fallos == 0, $"Se perdieron {fallos}/{Rondas * 2} altas con 5xx.");
    }

    [Fact]
    public async Task Ocho_altas_simultaneas_entran_todas_con_un_orden_cada_una()
    {
        var rutina = await CrearRutinaAsync("ocho");

        await Task.WhenAll(Enumerable.Range(1, 8).Select(habitoId => AgregarAsync(rutina.Id, habitoId)));

        var detalle = await _usuario.GetFromJsonAsync<RutinaDetalleDto>($"/api/rutinas/{rutina.Id}");

        Assert.NotNull(detalle);
        Assert.Equal(8, detalle.Habitos.Count);
        Assert.Equal(8, detalle.Habitos.Select(habito => habito.Orden).Distinct().Count());
    }

    private Task<HttpResponseMessage> AgregarAsync(int rutinaId, int habitoId) =>
        _usuario.PostAsJsonAsync($"/api/rutinas/{rutinaId}/habitos", new { habitoId });

    private async Task<RutinaDto> CrearRutinaAsync(string sufijo)
    {
        var respuesta = await _usuario.PostAsJsonAsync(
            "/api/rutinas",
            new { nombre = $"{Prefijo}Concurrencia {sufijo}" });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);

        return await respuesta.Content.ReadFromJsonAsync<RutinaDto>()
            ?? throw new InvalidOperationException("El alta no devolvió la rutina.");
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
