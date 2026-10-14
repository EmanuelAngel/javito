using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Javito.Api.Tests;

/// <summary>
/// Cubre la regla de S8: el SPA obtiene datos del backend a través de la API con CORS
/// configurado por origen. Cada prueba verifica un efecto observable del contrato.
/// </summary>
public class HealthEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string OrigenSpa = "http://localhost:5173";

    private readonly HttpClient _cliente = factory
        .WithWebHostBuilder(builder => builder.UseEnvironment("Development"))
        .CreateClient();

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
}

internal sealed record RespuestaDeSalud(string Estado, string Servicio);
