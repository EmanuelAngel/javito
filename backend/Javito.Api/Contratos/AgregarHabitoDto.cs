using System.ComponentModel.DataAnnotations;

namespace Javito.Api.Contratos;

/// <summary>
/// Qué hábito se cuelga de la rutina. Es nullable a propósito: con un <c>int</c> no nullable, un
/// cuerpo sin el campo se leería como 0 y respondería «inexistente» en vez de «falta el dato».
/// </summary>
public sealed record AgregarHabitoDto(
    [Required(ErrorMessage = "Indicá qué hábito querés agregar.")]
    int? HabitoId);
