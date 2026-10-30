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
}
