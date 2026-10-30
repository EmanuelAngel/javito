using System.Net.Http.Headers;
using System.Net.Http.Json;
using Javito.Api.Contratos;

namespace Javito.Api.Tests.Infraestructura;

/// <summary>
/// Inicia sesión contra la API alojada con las credenciales del seed. Las pruebas que necesitan un
/// token lo piden acá en lugar de firmar uno a mano: así también se ejercita el login.
/// </summary>
internal static class SesionDePrueba
{
    public const string Email = "usuario@javito.local";

    public const string Password = "Javito2026";

    public static async Task<string> IniciarAsync(HttpClient cliente)
    {
        var respuesta = await cliente.PostAsJsonAsync("/api/sesion", new { email = Email, password = Password });
        respuesta.EnsureSuccessStatusCode();

        var sesion = await respuesta.Content.ReadFromJsonAsync<SesionDto>();
        return sesion?.Token ?? throw new InvalidOperationException("El login no devolvió token.");
    }

    public static async Task<HttpClient> ClienteAutenticadoAsync(HttpClient cliente)
    {
        cliente.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await IniciarAsync(cliente));
        return cliente;
    }
}
