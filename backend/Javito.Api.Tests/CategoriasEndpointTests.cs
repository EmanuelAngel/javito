using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Javito.Api.Contratos;
using Javito.Api.Tests.Infraestructura;

namespace Javito.Api.Tests;

/// <summary>
/// S5: el día 1 entrega lectura real de MySQL. Cada prueba cubre una regla del listado.
/// Desde ACC-02 el listado exige token, así que la sesión se abre una vez para toda la clase.
/// Va en la colección de categorías porque cuenta filas del seed: la escritura de CAT-02…CAT-04 crea
/// y desactiva filas, y en paralelo esas cuentas dejarían de ser deterministas.
/// </summary>
[Collection(ColeccionDeCategorias.Nombre)]
public class CategoriasEndpointTests(FabricaDeApi fabrica) : IAsyncLifetime
{
    private readonly HttpClient _cliente = fabrica.CreateClient();

    public async Task InitializeAsync() => await SesionDePrueba.ClienteAutenticadoAsync(_cliente);

    public Task DisposeAsync()
    {
        _cliente.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Listar_sin_filtro_devuelve_las_cinco_categorias_del_seed()
    {
        var respuesta = await _cliente.GetAsync("/api/categorias");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var categorias = await respuesta.Content.ReadFromJsonAsync<List<CategoriaDto>>();
        Assert.NotNull(categorias);
        Assert.Equal(5, categorias.Count);
        Assert.Contains(categorias, c => c is { Nombre: "Cuerpo", Activo: true });
        Assert.Contains(categorias, c => c is { Nombre: "Espiritualidad", Activo: false });
    }

    [Fact]
    public async Task Listar_con_activo_verdadero_devuelve_solo_las_activas()
    {
        var categorias = await _cliente.GetFromJsonAsync<List<CategoriaDto>>("/api/categorias?activo=true");

        Assert.NotNull(categorias);
        Assert.Equal(4, categorias.Count);
        Assert.All(categorias, categoria => Assert.True(categoria.Activo));
    }

    [Fact]
    public async Task Listar_con_activo_falso_devuelve_solo_las_inactivas()
    {
        var categorias = await _cliente.GetFromJsonAsync<List<CategoriaDto>>("/api/categorias?activo=false");

        Assert.NotNull(categorias);
        var unica = Assert.Single(categorias);
        Assert.Equal("Espiritualidad", unica.Nombre);
    }

    [Fact]
    public async Task Un_activo_que_no_es_booleano_devuelve_problem_details_400()
    {
        var respuesta = await _cliente.GetAsync("/api/categorias?activo=basura");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(400, problema.GetProperty("status").GetInt32());
        Assert.True(problema.GetProperty("errors").TryGetProperty("activo", out var mensajes));
        Assert.NotEmpty(mensajes.EnumerateArray());
    }
}
