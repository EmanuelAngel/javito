using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Javito.Api.Contratos;
using Javito.Api.Modelos;
using Javito.Api.Seguridad;
using Javito.Api.Tests.Infraestructura;

namespace Javito.Api.Tests;

/// <summary>
/// S1 y S2: el listado de categorías exige token. Es la evidencia mínima de RT-07 dentro de las
/// pruebas: sin token 401 con código, con token 200.
/// </summary>
public class AccesoEndpointTests(FabricaDeApi fabrica) : IClassFixture<FabricaDeApi>
{
    private readonly HttpClient _cliente = fabrica.CreateClient();

    [Fact]
    public async Task El_listado_de_categorias_sin_token_devuelve_401_con_codigo()
    {
        var respuesta = await _cliente.GetAsync("/api/categorias");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("no_autenticado", problema.GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task El_listado_de_categorias_con_token_devuelve_200()
    {
        var token = await SesionDePrueba.IniciarAsync(_cliente);

        using var solicitud = new HttpRequestMessage(HttpMethod.Get, "/api/categorias");
        solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var respuesta = await _cliente.SendAsync(solicitud);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task Un_token_ajeno_a_la_clave_de_firma_devuelve_401()
    {
        using var solicitud = new HttpRequestMessage(HttpMethod.Get, "/api/categorias");
        solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "no.es.un.token");

        var respuesta = await _cliente.SendAsync(solicitud);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    /// <summary>
    /// S2: /yo devuelve la fila actual, no los claims congelados en el token. Se prueba con un token
    /// bien firmado cuyos claims mienten: si la respuesta trajera los claims, el nombre sería el falso.
    /// </summary>
    [Fact]
    public async Task Yo_devuelve_la_fila_actual_y_no_los_claims_del_token()
    {
        var emisor = new ServicioDeTokens(
            new OpcionesDeJwt(FabricaDeApi.ClaveDeJwtDePruebas, "javito", "javito-web", 8));

        var tokenQueMiente = emisor.Emitir(new Usuario
        {
            Id = 2,
            Rol = "Administrador",
            Nombre = "Nombre Falso",
            Email = "falso@javito.local",
            Activo = true
        });

        using var solicitud = new HttpRequestMessage(HttpMethod.Get, "/api/sesion/yo");
        solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenQueMiente);

        var respuesta = await _cliente.SendAsync(solicitud);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var usuario = await respuesta.Content.ReadFromJsonAsync<UsuarioDto>();
        Assert.NotNull(usuario);
        Assert.Equal(2, usuario.Id);
        Assert.Equal("Usuario de prueba", usuario.Nombre);
        Assert.Equal("usuario@javito.local", usuario.Email);
        Assert.Equal("Usuario", usuario.Rol);
    }

    [Fact]
    public async Task Yo_sin_token_devuelve_401_con_codigo()
    {
        var respuesta = await _cliente.GetAsync("/api/sesion/yo");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("no_autenticado", problema.GetProperty("codigo").GetString());
    }
}
