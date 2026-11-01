using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Javito.Api.Seguridad;

/// <summary>
/// Nombres de los claims del JWT. Son parte del contrato con el SPA, así que se escriben una sola
/// vez acá y no como literales sueltos.
/// </summary>
public static class ReclamosDeSesion
{
    public const string Rol = "rol";

    public const string Nombre = "nombre";

    public const string Email = "email";

    /// <summary>
    /// Id del usuario dueño de la sesión, leído del <c>sub</c> que escribe el login al firmar el
    /// token. Devuelve <c>false</c> cuando el claim falta o no es un id: el token entonces no sirve
    /// para identificar filas.
    /// </summary>
    public static bool TryUsuarioId(ClaimsPrincipal usuario, out int id) =>
        int.TryParse(usuario.FindFirstValue(JwtRegisteredClaimNames.Sub), out id);
}
