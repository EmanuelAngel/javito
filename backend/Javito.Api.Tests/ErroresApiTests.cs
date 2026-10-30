using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Javito.Api.Tests.Infraestructura;

namespace Javito.Api.Tests;

/// <summary>S3: un error de la API siempre es ProblemDetails, también el 404 de una ruta inexistente.</summary>
public class ErroresApiTests(FabricaDeApi fabrica) : IClassFixture<FabricaDeApi>
{
    private readonly HttpClient _cliente = fabrica.CreateClient();

    [Fact]
    public async Task Una_ruta_inexistente_devuelve_problem_details_404()
    {
        var respuesta = await _cliente.GetAsync("/api/no-existe");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(404, problema.GetProperty("status").GetInt32());
    }
}
