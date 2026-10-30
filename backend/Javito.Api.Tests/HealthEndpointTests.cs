using System.Net;
using System.Net.Http.Json;
using Javito.Api.Tests.Infraestructura;

namespace Javito.Api.Tests;

/// <summary>
/// El SPA obtiene datos por la API con CORS por origen; la fundación le suma el estado de la base.
/// Cada prueba verifica un efecto observable del contrato.
/// </summary>
public class HealthEndpointTests(FabricaDeApi fabrica) : IClassFixture<FabricaDeApi>
{
    private const string OrigenSpa = "http://localhost:5173";

    private readonly HttpClient _cliente = fabrica.CreateClient();

    [Fact]
    public async Task El_endpoint_de_salud_reporta_el_servicio_activo()
    {
        var respuesta = await _cliente.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var cuerpo = await respuesta.Content.ReadFromJsonAsync<RespuestaDeSalud>();
        Assert.NotNull(cuerpo);
        Assert.Equal("ok", cuerpo.Estado);
        Assert.Equal("Javito.Api", cuerpo.Servicio);
    }

    [Fact]
    public async Task El_endpoint_de_salud_informa_la_base_de_datos_disponible()
    {
        var cuerpo = await _cliente.GetFromJsonAsync<RespuestaDeSalud>("/api/health");

        Assert.NotNull(cuerpo);
        Assert.Equal("ok", cuerpo.BaseDeDatos);
    }

    [Fact]
    public async Task El_endpoint_de_salud_degrada_cuando_la_base_no_responde()
    {
        using var sinBase = FabricaDeApi.ConCadena("Server=localhost;Port=1;User ID=root;Database=javito");
        using var cliente = sinBase.CreateClient();

        var cuerpo = await cliente.GetFromJsonAsync<RespuestaDeSalud>("/api/health");

        Assert.NotNull(cuerpo);
        Assert.Equal("degradado", cuerpo.Estado);
        Assert.Equal("error", cuerpo.BaseDeDatos);
    }

    [Fact]
    public async Task CORS_habilita_el_origen_del_SPA_configurado()
    {
        var peticion = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        peticion.Headers.Add("Origin", OrigenSpa);

        var respuesta = await _cliente.SendAsync(peticion);

        Assert.True(
            respuesta.Headers.TryGetValues("Access-Control-Allow-Origin", out var origenes),
            "La respuesta no incluye la cabecera Access-Control-Allow-Origin");
        Assert.Equal(OrigenSpa, Assert.Single(origenes));
    }

    [Fact]
    public async Task CORS_no_habilita_ningun_origen_cuando_la_lista_esta_vacia()
    {
        // Fuera de Development, appsettings.json no define Cors:AllowedOrigins, así que el
        // constructor de política queda sin ningún configurador: ni WithOrigins ni AllowAnyOrigin.
        // Es la rama que corre en cualquier entorno que no sea Development, y ninguna otra prueba
        // la ejercita. Si la política vacía fuese permisiva, esta prueba lo delata.
        using var fabricaProduccion = FabricaDeApi.EnEntorno("Production");
        using var cliente = fabricaProduccion.CreateClient();

        var peticion = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        peticion.Headers.Add("Origin", OrigenSpa);

        var respuesta = await cliente.SendAsync(peticion);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.False(
            respuesta.Headers.Contains("Access-Control-Allow-Origin"),
            "Sin orígenes configurados la política CORS no debe habilitar ninguno");
    }
}

internal sealed record RespuestaDeSalud(string Estado, string Servicio, string BaseDeDatos);
