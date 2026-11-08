using System.ComponentModel.DataAnnotations;
using Javito.Api.Usuarios;

namespace Javito.Api.Contratos;

/// <summary>
/// Cuerpo del alta de una cuenta desde administración (HU-USU-02). Las anotaciones van sobre el
/// parámetro del record, sin el prefijo <c>[property: …]</c>: con el prefijo MVC las ignora y avisa al
/// validar. Los límites son los de <see cref="ReglasDeUsuarios"/>, que son los de database.sql salvo la
/// contraseña: BCrypt hashea 72 bytes y truncaría el resto en silencio.
/// </summary>
/// <remarks>
/// La contraseña llega en claro y no vuelve nunca: se hashea en el controlador y <c>password_hash</c>
/// no aparece en ningún DTO de salida (S14). El rol no lleva anotación de valores posibles porque una
/// anotación sólo sabe de largos: la lista la valida el controlador con <c>rol_invalido</c>.
/// </remarks>
public sealed record AltaDeUsuarioDto(
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    // `.*\S.*` exige al menos un caracter que no sea espacio: `[Required]` acepta `"   "`. El patrón
    // cubre la cadena entera porque `RegularExpressionAttribute` no busca "contiene".
    [RegularExpression(@".*\S.*", ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(
        ReglasDeUsuarios.LongitudMaximaDelNombre,
        ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    string Nombre,
    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El email no tiene un formato válido.")]
    [StringLength(
        ReglasDeUsuarios.LongitudMaximaDelEmail,
        ErrorMessage = "El email no puede superar los 150 caracteres.")]
    string Email,
    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(
        ReglasDeUsuarios.LongitudMaximaDeLaContrasena,
        MinimumLength = ReglasDeUsuarios.LongitudMinimaDeLaContrasena,
        ErrorMessage = "La contraseña debe tener entre 8 y 72 caracteres.")]
    // El tope en caracteres no alcanza: BCrypt hashea 72 bytes y los caracteres multibyte ocupan
    // más de uno, así que una contraseña de 40 tildes ya se truncaría en silencio.
    [ContrasenaDeBcrypt]
    string Password,
    [Required(ErrorMessage = "El rol es obligatorio.")]
    string Rol);
