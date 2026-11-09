using System.ComponentModel.DataAnnotations;
using Javito.Api.Usuarios;

namespace Javito.Api.Contratos;

/// <summary>
/// Cuerpo de la edición de una cuenta desde administración (HU-USU-03). Son <b>dos</b> campos a
/// propósito: la edición corrige el nombre y el email, y nada más.
/// </summary>
/// <remarks>
/// Sin <c>Rol</c> ni <c>Activo</c> (S15): el rol y el estado se cambian en la parte 2. Sin
/// <c>Password</c> (S11): la contraseña de otra cuenta no se muestra ni se puede escribir desde acá, y
/// por eso el contrato no tiene dónde recibirla — lo que llegue con ese nombre se descarta en el
/// binding y jamás toca la columna.
/// <para>
/// Las anotaciones van sobre el parámetro del record, sin el prefijo <c>[property: …]</c>: con el
/// prefijo MVC las ignora y avisa al validar. Los largos son los de <see cref="ReglasDeUsuarios"/>, y
/// los mensajes son los mismos del alta para que el formulario, que es el mismo, muestre lo mismo.
/// </para>
/// </remarks>
public sealed record EdicionDeUsuarioDto(
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
    string Email);
