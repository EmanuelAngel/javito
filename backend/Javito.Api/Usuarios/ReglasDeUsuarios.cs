using Javito.Api.Seguridad;

namespace Javito.Api.Usuarios;

/// <summary>
/// Las reglas de una cuenta que se crea o se edita desde la API. Viven acá y no en el controlador
/// porque el alta de administración (#85) y el registro público (#68) tienen que rechazar lo mismo
/// con el mismo texto: dos copias de estos límites se desincronizan en silencio y una deja entrar lo
/// que la otra prohíbe. Los largos son los de <c>database.sql</c> (<c>VARCHAR(100)</c> del nombre,
/// <c>VARCHAR(150)</c> del email).
/// </summary>
/// <remarks>
/// Lo que una DataAnnotation ya puede decir —obligatorio, largo, formato— vive en los DTO de entrada;
/// acá queda lo que ninguna anotación puede expresar: qué roles existen y cómo se compara un email.
/// </remarks>
public static class ReglasDeUsuarios
{
    public const int LongitudMinimaDelNombre = 1;

    public const int LongitudMaximaDelNombre = 100;

    public const int LongitudMaximaDelEmail = 150;

    public const int LongitudMinimaDeLaContrasena = 8;

    /// <summary>
    /// El tope de la contraseña son 72 caracteres, no los 100 de <c>VARCHAR(100)</c>: BCrypt hashea
    /// como máximo 72 <em>bytes</em> del password y trunca el resto en silencio, así que aceptar más
    /// sólo cambia lo que el usuario cree que escribió. Con 72 bytes compartidos por dos cuentas, las
    /// dos contraseñas se autentican entre sí. Ver <see cref="ContrasenaDeBcryptAttribute"/>, que
    /// además mira los bytes: un carácter acentuado o un emoji ocupa más de uno.
    /// </summary>
    public const int LongitudMaximaDeLaContrasena = 72;

    /// <summary>
    /// El tope real del algoritmo, en bytes de UTF-8, no en caracteres: un password de 40 caracteres
    /// con tildes ya pasa los 72 bytes y BCrypt lo truncaría. Es el mismo número del largo en
    /// caracteres porque con ASCII un byte es un carácter, y el caso multibyte lo cubre la anotación.
    /// </summary>
    public const int LongitudMaximaDeLaContrasenaEnBytes = LongitudMaximaDeLaContrasena;

    /// <summary>
    /// Los roles que acepta la aplicación, los mismos del <c>ENUM</c> de <c>usuario.rol</c>. La lista
    /// es la que decide: comparar contra la base dejaría pasar cualquier texto que la intercalación
    /// insensible a mayúsculas del <c>ENUM</c> resuelva sola, y un alta podría elegir un rol por
    /// accidente.
    /// </summary>
    public static readonly IReadOnlyList<string> RolesValidos = [Roles.Administrador, Roles.Usuario];

    public static bool EsRolValido(string? rol) =>
        rol is not null && RolesValidos.Contains(rol, StringComparer.Ordinal);

    /// <summary>
    /// Trim más minúsculas invariantes: el email se guarda y se busca siempre igual. La columna es
    /// <c>UNIQUE</c> e insensible a mayúsculas, así que sin normalizar el chequeo previo de duplicados
    /// y la fila guardada podrían no coincidir, y el 409 llegaría recién desde la base como un 500.
    /// </summary>
    public static string NormalizarEmail(string email) => email.Trim().ToLowerInvariant();
}
