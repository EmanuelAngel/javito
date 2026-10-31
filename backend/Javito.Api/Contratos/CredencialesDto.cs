using System.ComponentModel.DataAnnotations;

namespace Javito.Api.Contratos;

/// <summary>
/// Cuerpo del login. Es un DTO de <b>entrada</b>, así que acá sí van las DataAnnotations:
/// <c>[ApiController]</c> las convierte en un 400 con <c>errors</c> por campo. Los límites de
/// longitud coinciden con los de <c>usuario</c> en <c>database.sql</c>.
/// </summary>
/// <remarks>
/// En un record posicional las anotaciones van <b>sobre el parámetro</b>, sin el prefijo
/// <c>[property: …]</c>. Con el prefijo, MVC las ignora y lanza al validar: "Record type has
/// validation metadata defined on property 'Password' that will be ignored".
/// </remarks>
public sealed record CredencialesDto(
    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El email no tiene un formato válido.")]
    [StringLength(150, ErrorMessage = "El email no puede superar los 150 caracteres.")]
    string Email,
    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "La contraseña no puede superar los 100 caracteres.")]
    string Password);
