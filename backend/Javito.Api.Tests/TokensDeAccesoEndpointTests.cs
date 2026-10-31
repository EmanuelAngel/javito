using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Javito.Api.Modelos;
using Javito.Api.Seguridad;
using Javito.Api.Tests.Infraestructura;
using Microsoft.IdentityModel.Tokens;

namespace Javito.Api.Tests;

/// <summary>
/// S3 y S14: el challenge valida no sólo la firma, sino también el emisor, la audiencia y la vida del
/// token. Cada prueba firma con la misma clave de la fábrica pero rompe una sola validación: un token
/// "casi bueno" tiene que salir 401 con <c>codigo: no_autenticado</c>. La cuarta prueba protege el otro
/// borde: el login sigue siendo público y su 401 no es del challenge, es <c>credenciales_invalidas</c>.
/// </summary>
public class TokensDeAccesoEndpointTests(FabricaDeApi fabrica) : IClassFixture<FabricaDeApi>
{
    private const string EmisorReal = "javito";

    private const string AudienciaReal = "javito-web";

    private readonly HttpClient _cliente = fabrica.CreateClient();

    [Fact]
    public async Task Un_token_con_emisor_ajeno_y_clave_valida_devuelve_401_con_codigo()
    {
        var emisorAjeno = new ServicioDeTokens(
            new OpcionesDeJwt(FabricaDeApi.ClaveDeJwtDePruebas, "otro-emisor", AudienciaReal, 8));

        var respuesta = await ConTokenAsync(emisorAjeno.Emitir(UsuarioValido()));

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("no_autenticado", problema.GetProperty("codigo").GetString());
        Assert.Equal(401, problema.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Un_token_con_audiencia_ajena_y_clave_valida_devuelve_401_con_codigo()
    {
        var audienciaAjena = new ServicioDeTokens(
            new OpcionesDeJwt(FabricaDeApi.ClaveDeJwtDePruebas, EmisorReal, "otro-cliente", 8));

        var respuesta = await ConTokenAsync(audienciaAjena.Emitir(UsuarioValido()));

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("no_autenticado", problema.GetProperty("codigo").GetString());
        Assert.Equal(401, problema.GetProperty("status").GetInt32());
    }

    /// <summary>
    /// El vencimiento no puede venir de ServicioDeTokens: siempre emite tokens vigentes. Acá se arma el
    /// JWT a mano con la misma clave y <c>expires</c> cinco minutos atrás; el sesgo de reloj de la API
    /// es de 30 segundos, así que un token vencido hace rato no tiene clemencia.
    /// </summary>
    [Fact]
    public async Task Un_token_vencido_devuelve_401_con_codigo()
    {
        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(FabricaDeApi.ClaveDeJwtDePruebas)),
            SecurityAlgorithms.HmacSha256);

        var vencido = new JwtSecurityToken(
            issuer: EmisorReal,
            audience: AudienciaReal,
            claims: [new Claim(JwtRegisteredClaimNames.Sub, "2")],
            notBefore: DateTime.UtcNow.AddMinutes(-10),
            expires: DateTime.UtcNow.AddMinutes(-5),
            signingCredentials: credenciales);

        var token = new JwtSecurityTokenHandler().WriteToken(vencido);

        var respuesta = await ConTokenAsync(token);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("no_autenticado", problema.GetProperty("codigo").GetString());
        Assert.Equal(401, problema.GetProperty("status").GetInt32());
    }

    /// <summary>
    /// El login es público: su 401 no sale del challenge de JwtBearer, sale de la regla de credenciales
    /// inválidas del controlador. Que el emisor y la audiencia exijan token no puede arrastrar a este
    /// endpoint.
    /// </summary>
    [Fact]
    public async Task El_login_con_contrasena_incorrecta_devuelve_401_de_credenciales_y_no_del_challenge()
    {
        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/sesion",
            new { email = "usuario@javito.local", password = "no-es-la-clave" });

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("credenciales_invalidas", problema.GetProperty("codigo").GetString());
        Assert.NotEqual("no_autenticado", problema.GetProperty("codigo").GetString());
    }

    private static Usuario UsuarioValido() => new()
    {
        Id = 2,
        Rol = "Usuario",
        Nombre = "Usuario de prueba",
        Email = "usuario@javito.local",
        Activo = true
    };

    private async Task<HttpResponseMessage> ConTokenAsync(string token, string ruta = "/api/categorias")
    {
        using var solicitud = new HttpRequestMessage(HttpMethod.Get, ruta);
        solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await _cliente.SendAsync(solicitud);
    }
}
