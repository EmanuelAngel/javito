using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Javito.Api.Contratos;
using Javito.Api.Tests.Infraestructura;

namespace Javito.Api.Tests;

/// <summary>
/// S1: las tres reglas del login que la historia HU-ACC-02 promete. El mensaje de fallo es el mismo
/// para email inexistente, contraseña incorrecta y cuenta desactivada: no se puede enumerar cuentas.
/// </summary>
public class SesionEndpointTests(FabricaDeApi fabrica) : IClassFixture<FabricaDeApi>
{
    private readonly HttpClient _cliente = fabrica.CreateClient();

    [Fact]
    public async Task Login_con_credenciales_del_seed_devuelve_el_token_y_el_usuario_sin_el_hash()
    {
        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/sesion",
            new { email = "usuario@javito.local", password = "Javito2026" });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var sesion = await respuesta.Content.ReadFromJsonAsync<SesionDto>();
        Assert.NotNull(sesion);
        Assert.False(string.IsNullOrWhiteSpace(sesion.Token));
        Assert.Equal("usuario@javito.local", sesion.Usuario.Email);
        Assert.Equal("Usuario de prueba", sesion.Usuario.Nombre);
        Assert.Equal("Usuario", sesion.Usuario.Rol);

        Assert.DoesNotContain("$2b$", await respuesta.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Login_con_email_inexistente_y_con_contrasena_incorrecta_devuelve_el_mismo_401()
    {
        var emailInexistente = await _cliente.PostAsJsonAsync(
            "/api/sesion",
            new { email = "nadie@javito.local", password = "Javito2026" });

        var contrasenaIncorrecta = await _cliente.PostAsJsonAsync(
            "/api/sesion",
            new { email = "usuario@javito.local", password = "no-es-la-clave" });

        Assert.Equal(HttpStatusCode.Unauthorized, emailInexistente.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, contrasenaIncorrecta.StatusCode);
        Assert.Equal("application/problem+json", emailInexistente.Content.Headers.ContentType?.MediaType);

        var sinCuenta = await emailInexistente.Content.ReadFromJsonAsync<JsonElement>();
        var sinClave = await contrasenaIncorrecta.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("credenciales_invalidas", sinCuenta.GetProperty("codigo").GetString());
        Assert.Equal("credenciales_invalidas", sinClave.GetProperty("codigo").GetString());
        Assert.Equal(
            sinCuenta.GetProperty("detail").GetString(),
            sinClave.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Un_usuario_desactivado_no_entra_aunque_la_contrasena_sea_correcta()
    {
        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/sesion",
            new { email = "desactivado@javito.local", password = "Javito2026" });

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("credenciales_invalidas", problema.GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task Login_sin_email_devuelve_problem_details_400_con_el_campo_en_errors()
    {
        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/sesion",
            new { email = string.Empty, password = "Javito2026" });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(problema.GetProperty("errors").TryGetProperty("Email", out var mensajes));
        Assert.NotEmpty(mensajes.EnumerateArray());
    }
}
