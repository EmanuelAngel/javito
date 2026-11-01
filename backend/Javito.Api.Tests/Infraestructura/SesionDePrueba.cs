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

    /// <summary>Cuenta del seed con rol Administrador: la única que escribe en el catálogo.</summary>
    public const string EmailAdministrador = "admin@javito.local";

    public static Task<string> IniciarAsync(HttpClient cliente) => IniciarAsync(cliente, Email);

    public static async Task<string> IniciarAsync(HttpClient cliente, string email)
    {
        var respuesta = await cliente.PostAsJsonAsync(
            "/api/sesion",
            new { email, password = Password });
        respuesta.EnsureSuccessStatusCode();

        var sesion = await respuesta.Content.ReadFromJsonAsync<SesionDto>();
        return sesion?.Token ?? throw new InvalidOperationException("El login no devolvió token.");
    }

    public static Task<HttpClient> ClienteAutenticadoAsync(HttpClient cliente) =>
        ClienteConAsync(cliente, Email);

    public static Task<HttpClient> ClienteAdministradorAsync(HttpClient cliente) =>
        ClienteConAsync(cliente, EmailAdministrador);

    private static async Task<HttpClient> ClienteConAsync(HttpClient cliente, string email)
    {
        cliente.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await IniciarAsync(cliente, email));

        return cliente;
    }
}
