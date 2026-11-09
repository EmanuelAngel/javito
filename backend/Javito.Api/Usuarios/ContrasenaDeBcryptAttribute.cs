using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Javito.Api.Usuarios;

/// <summary>
/// Rechaza una contraseña que BCrypt no puede aprovechar entera: hashea como máximo 72 <em>bytes</em>
/// de UTF-8 y trunca el resto sin avisar. Un <c>StringLength</c> no alcanza porque cuenta caracteres
/// y no bytes: <c>"é"</c> ayuda a explicar el caso —40 de esos son 40 caracteres y 80 bytes—, así que
/// dos cuentas cuyos primeros 72 bytes coinciden se autentican entre sí.
/// </summary>
/// <remarks>
/// El esquema de hash no se puede cambiar para arreglar esto: el login verifica los hashes
/// <c>$2b$11$</c> del seed con <c>BCrypt.Verify</c>, y cualquier pre-hash dejaría afuera a las cuentas
/// que ya existen. Lo que sí se puede hacer es rechazar lo que el algoritmo no va a leer.
/// Como el resto de las reglas de la cuenta, vive acá y no en el DTO para que el registro público
/// (#68) rechace lo mismo con el mismo texto.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class ContrasenaDeBcryptAttribute : ValidationAttribute
{
    public ContrasenaDeBcryptAttribute()
        : base(
            $"La contraseña no puede superar los {ReglasDeUsuarios.LongitudMaximaDeLaContrasenaEnBytes} " +
            "bytes: las tildes, las eñes y los emojis ocupan más de un byte cada uno.")
    {
    }

    /// <summary>
    /// Devuelve el <see cref="ValidationResult"/> con el nombre del miembro validado para que
    /// <c>[ApiController]</c> lo publique en <c>errors.Password</c>: sin el nombre, el 400 sale con el
    /// error en la clave general y el formulario no sabe qué campo marcar.
    /// </summary>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        // `null` o vacío no son asunto de esta anotación: lo dice `[Required]`.
        if (value is not string contrasena ||
            Encoding.UTF8.GetByteCount(contrasena) <= ReglasDeUsuarios.LongitudMaximaDeLaContrasenaEnBytes)
        {
            return ValidationResult.Success;
        }

        var miembro = validationContext.MemberName;

        return new ValidationResult(
            FormatErrorMessage(validationContext.DisplayName),
            string.IsNullOrEmpty(miembro) ? [] : [miembro]);
    }
}
