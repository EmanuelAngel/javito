using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Javito.Api.Modelos;
using Microsoft.IdentityModel.Tokens;

namespace Javito.Api.Seguridad;

/// <summary>
/// Emite el JWT de sesión. Es un helper, no una capa: no consulta nada, sólo firma lo que recibe.
/// </summary>
public sealed class ServicioDeTokens(OpcionesDeJwt opciones)
{
    public string Emitir(Usuario usuario)
    {
        var ahora = DateTimeOffset.UtcNow;

        // Los claims son la identidad que el servidor cree en cada request. El rol viaja con el
        // nombre que usa TokenValidationParameters.RoleClaimType, así [Authorize(Roles=...)] lo lee.
        Claim[] claims =
        [
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString(CultureInfo.InvariantCulture)),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ReclamosDeSesion.Rol, usuario.Rol),
            new(ReclamosDeSesion.Nombre, usuario.Nombre),
            new(ReclamosDeSesion.Email, usuario.Email)
        ];

        var token = new JwtSecurityToken(
            issuer: opciones.Emisor,
            audience: opciones.Audiencia,
            claims: claims,
            notBefore: ahora.UtcDateTime,
            expires: ahora.AddHours(opciones.HorasDeVigencia).UtcDateTime,
            signingCredentials: new SigningCredentials(opciones.ClaveDeFirma, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
