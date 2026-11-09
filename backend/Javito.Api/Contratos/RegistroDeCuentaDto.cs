using System.ComponentModel.DataAnnotations;
using Javito.Api.Usuarios;

namespace Javito.Api.Contratos;

/// <summary>
/// Cuerpo del registro público (HU-ACC-01). Mismas reglas y mismos textos que el alta de
/// administración, pero sin <c>rol</c> ni avatar: la cuenta siempre nace <c>Usuario</c> y activa, y un
/// campo <c>rol</c> en el JSON se ignora.
/// </summary>
public sealed record RegistroDeCuentaDto(
    [Required(ErrorMessage = "El nombre es obligatorio.")]
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
    [ContrasenaDeBcrypt]
    string Password);
