using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Javito.Api.Seguridad;

/// <summary>
/// Configuración del JWT de sesión. La clave de firma no vive en el repositorio: llega por
/// <c>dotnet user-secrets</c> o por la variable <c>Jwt__Clave</c>, igual que la cadena de conexión.
/// </summary>
/// <remarks>
/// Falla al arrancar si falta o es demasiado corta, con un mensaje que dice cómo cargarla. Un JWT
/// firmado con HMAC-SHA256 necesita al menos 32 bytes de clave: con menos, el algoritmo es
/// matemáticamente más débil que el que declara.
/// </remarks>
public sealed record OpcionesDeJwt(string Clave, string Emisor, string Audiencia, int HorasDeVigencia)
{
    public const string Seccion = "Jwt";

    public const int HorasPorDefecto = 8;

    public const int BytesMinimosDeClave = 32;

    public static OpcionesDeJwt Desde(IConfiguration configuracion)
    {
        var clave = configuracion[$"{Seccion}:Clave"];

        if (string.IsNullOrWhiteSpace(clave))
        {
            throw new InvalidOperationException(
                "Falta Jwt:Clave. Configurala con: dotnet user-secrets set \"Jwt:Clave\" \"<clave de 32 o " +
                "más caracteres>\" --project backend/Javito.Api");
        }

        if (Encoding.UTF8.GetByteCount(clave) < BytesMinimosDeClave)
        {
            throw new InvalidOperationException(
                $"Jwt:Clave es demasiado corta: HMAC-SHA256 exige al menos {BytesMinimosDeClave} bytes.");
        }

        return new OpcionesDeJwt(
            clave,
            configuracion[$"{Seccion}:Emisor"] ?? "javito",
            configuracion[$"{Seccion}:Audiencia"] ?? "javito-web",
            int.TryParse(configuracion[$"{Seccion}:HorasDeVigencia"], out var horas) ? horas : HorasPorDefecto);
    }

    public SymmetricSecurityKey ClaveDeFirma => new(Encoding.UTF8.GetBytes(Clave));
}
