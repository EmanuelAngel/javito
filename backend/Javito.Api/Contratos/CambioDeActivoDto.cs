using System.ComponentModel.DataAnnotations;

namespace Javito.Api.Contratos;

/// <summary>
/// Estado al que se quiere llevar la categoría. Es nullable a propósito: con un <c>bool</c> no
/// nullable, un cuerpo vacío se leería como <c>false</c> y desactivaría justo lo que se quería
/// activar.
/// </summary>
public sealed record CambioDeActivoDto(
    [Required(ErrorMessage = "Indicá el estado: true para activar, false para desactivar.")]
    bool? Activo);
