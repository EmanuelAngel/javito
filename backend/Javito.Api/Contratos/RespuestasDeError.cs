using Microsoft.AspNetCore.Mvc;

namespace Javito.Api.Contratos;

/// <summary>
/// Escribe un <see cref="ErrorDeNegocio"/> como ProblemDetails directamente sobre la respuesta.
/// Lo usan los eventos de JwtBearer, que corren fuera de MVC y no pasan por el manejador de
/// excepciones: sin esto el 401 y el 403 saldrían con el cuerpo vacío.
/// </summary>
public static class RespuestasDeError
{
    public static Task EscribirAsync(HttpContext contexto, ErrorDeNegocio error) =>
        Results.Problem(MapeoDeErrores.ProblemDetails(error)).ExecuteAsync(contexto);

    /// <summary>
    /// El mismo ProblemDetails, pero devuelto desde una acción de MVC. Es el camino de los errores
    /// previstos: se devuelven, no se lanzan.
    /// </summary>
    public static ObjectResult De(ErrorDeNegocio error) => new(MapeoDeErrores.ProblemDetails(error))
    {
        StatusCode = error.Estado,
        ContentTypes = { "application/problem+json" }
    };
}
