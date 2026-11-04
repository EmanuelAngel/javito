using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Javito.Api.Tests.Infraestructura;
using Microsoft.IdentityModel.Tokens;

namespace Javito.Api.Tests;

/// <summary>
/// S3 del issue #53: el 401 de <c>no_autenticado</c> se escribe en un solo lugar, así que los cinco
/// caminos que lo emiten devuelven el mismo cuerpo. Antes el detalle dependía de quién atendiera la
/// request —cuatro textos para el mismo <c>codigo</c>— y el SPA no podía explicar el mismo estado de
/// una sola forma. El <c>codigo</c> es el contrato; el texto también tiene que ser uno.
/// </summary>
public class ErroresDeSesionEndpointTests(FabricaDeApi fabrica) : IClassFixture<FabricaDeApi>
{
    private const string EmisorReal = "javito";

    private const string AudienciaReal = "javito-web";

    private readonly HttpClient _cliente = fabrica.CreateClient();

    /// <summary>
    /// Las cuatro acciones que necesitan el id del token, más el challenge de JwtBearer, que corre
    /// fuera de MVC. Es la lista completa de emisores de <c>no_autenticado</c> en la API.
    /// </summary>
    [Fact]
    public async Task El_401_de_no_autenticado_tiene_un_solo_texto_en_los_cinco_caminos()
    {
        var token = TokenConSubIlegible();

        var respuestas = new (string Camino, HttpResponseMessage Respuesta)[]
        {
            ("dias", await EnviarAsync(HttpMethod.Get, "/api/dias/2024-01-01", token)),
            ("habitos", await EnviarAsync(HttpMethod.Get, "/api/habitos", token)),
            ("sesion", await EnviarAsync(HttpMethod.Get, "/api/sesion/yo", token)),
            ("perfil", await EnviarAsync(HttpMethod.Post, "/api/perfil/avatar", token)),
            ("challenge", await EnviarAsync(HttpMethod.Get, "/api/habitos", token: null))
        };

        var cuerpos = new List<(string Camino, JsonElement Problema)>();

        foreach (var (camino, respuesta) in respuestas)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
            Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);

            var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("no_autenticado", problema.GetProperty("codigo").GetString());
            Assert.Equal(401, problema.GetProperty("status").GetInt32());

            cuerpos.Add((camino, problema));
        }

        var (_, primero) = cuerpos[0];
        var titulo = primero.GetProperty("title").GetString();
        var detalle = primero.GetProperty("detail").GetString();

        Assert.False(string.IsNullOrWhiteSpace(detalle));

        foreach (var (camino, problema) in cuerpos)
        {
            Assert.True(
                titulo == problema.GetProperty("title").GetString(),
                $"El 401 de «{camino}» tiene otro título que el de «{cuerpos[0].Camino}».");
            Assert.True(
                detalle == problema.GetProperty("detail").GetString(),
                $"El 401 de «{camino}» tiene otro detalle que el de «{cuerpos[0].Camino}».");
        }
    }

    /// <summary>
    /// Firma un token con la clave, el emisor y la audiencia reales: pasa el challenge y llega a la
    /// acción. Lo único roto es el <c>sub</c>, que no es un id. Es el caso que antes cada controlador
    /// resolvía a mano, parseando el claim por su cuenta.
    /// </summary>
    private static string TokenConSubIlegible()
    {
        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(FabricaDeApi.ClaveDeJwtDePruebas)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: EmisorReal,
            audience: AudienciaReal,
            claims: [new Claim(JwtRegisteredClaimNames.Sub, "no-es-un-id")],
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credenciales);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<HttpResponseMessage> EnviarAsync(HttpMethod metodo, string ruta, string? token)
    {
        using var solicitud = new HttpRequestMessage(metodo, ruta);

        if (token is not null)
        {
            solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await _cliente.SendAsync(solicitud);
    }
}
