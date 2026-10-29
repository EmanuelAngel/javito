using Microsoft.AspNetCore.Diagnostics;

namespace Javito.Api.Contratos;

/// <summary>Convierte un ErrorDeNegocio en ProblemDetails; cualquier otra excepción sigue su curso.</summary>
public sealed class ManejadorDeErroresDeNegocio : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext contexto,
        Exception excepcion,
        CancellationToken cancellationToken)
    {
        if (excepcion is not ErrorDeNegocio error)
        {
            return false;
        }

        var resultado = Results.Problem(MapeoDeErrores.ProblemDetails(error));
        await resultado.ExecuteAsync(contexto);
        return true;
    }
}
