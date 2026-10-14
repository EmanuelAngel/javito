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

    [Fact]
    public async Task CORS_no_habilita_ningun_origen_cuando_la_lista_esta_vacia()
    {
        // Fuera de Development, appsettings.json no define `Cors:AllowedOrigins`, así que el
        // constructor de política queda sin ningún configurador: ni WithOrigins ni AllowAnyOrigin.
        // Es la rama que corre en cualquier entorno que no sea Development, y ninguna otra prueba
        // la ejercita. Si la política vacía fuese permisiva, esta prueba lo delata.
        using var fabrica = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        using var cliente = fabrica.CreateClient();

        var peticion = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        peticion.Headers.Add("Origin", OrigenSpa);

        var respuesta = await cliente.SendAsync(peticion);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.False(
            respuesta.Headers.Contains("Access-Control-Allow-Origin"),
            "Sin orígenes configurados la política CORS no debe habilitar ninguno");
    }
}

internal sealed record RespuestaDeSalud(string Estado, string Servicio);
