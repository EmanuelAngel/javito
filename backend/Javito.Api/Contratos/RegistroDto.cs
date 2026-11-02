using System.ComponentModel.DataAnnotations;

namespace Javito.Api.Contratos;

/// <summary>
/// El registro tal como lo devuelve la API. El cliente decide con `completado`, que deriva el
/// servidor: no se manda desde el navegador.
/// </summary>
public sealed record RegistroDto(decimal Valor, decimal MetaObjetivo, bool Completado);

/// <summary>
/// El toque: sólo el valor alcanzado. `decimal?` con [Required] para que un cuerpo sin `valor` sea un
/// 400 y no un cero silencioso, que desmarcaría lo que se quería marcar.
/// </summary>
public sealed record RegistroEntradaDto(
    [Required(ErrorMessage = "El valor alcanzado es obligatorio.")] decimal? Valor);
