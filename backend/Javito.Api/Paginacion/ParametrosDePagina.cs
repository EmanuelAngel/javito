using System.ComponentModel.DataAnnotations;

namespace Javito.Api.Paginacion;

/// <summary>
/// Lo que pide cualquier catálogo paginado del servidor, atado desde el query string. Vive en
/// <c>Paginacion/</c> justamente porque es articulo-agnóstico: los catálogos de hábitos y de usuarios
/// lo reusan tal cual, y por eso acá no hay nada del dominio.
/// </summary>
/// <remarks>
/// Las anotaciones hacen el trabajo: con <c>[ApiController]</c> un valor fuera de rango se convierte
/// en el 400 de validación con el campo nombrado en <c>errors</c>, sin que ningún controlador escriba
/// ese error a mano.
/// </remarks>
public sealed class ParametrosDePagina
{
    /// <summary>Seis por página: la cantidad que el usuario eligió para el catálogo de artículos.</summary>
    public const int TamanoDePaginaPorDefecto = 6;

    /// <summary>
    /// Tope del tamaño de página. No es un límite de la base: es el techo de lo que una pantalla de
    /// catálogo puede mostrar de una vez sin bajar el servidor.
    /// </summary>
    public const int TamanoDePaginaMaximo = 50;

    /// <summary>La primera página es la 1, no la 0: es el número que se ve en la pantalla.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "La página tiene que ser 1 o más.")]
    public int Pagina { get; set; } = 1;

    [Range(
        1,
        TamanoDePaginaMaximo,
        ErrorMessage = "El tamaño de página tiene que estar entre 1 y 50.")]
    public int TamanioDePagina { get; set; } = TamanoDePaginaPorDefecto;

    /// <summary>
    /// Cuántas filas se saltean. Es <c>long</c> a propósito: con un <c>int</c>, una página enorme y
    /// multiplicada por el tamaño desborda y el <c>OFFSET</c> viajaría negativo.
    /// </summary>
    public long Desplazamiento => ((long)Pagina - 1) * TamanioDePagina;
}
