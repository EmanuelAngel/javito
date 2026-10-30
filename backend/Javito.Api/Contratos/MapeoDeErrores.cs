using Microsoft.AspNetCore.Mvc;

namespace Javito.Api.Contratos;

/// <summary>Traduce un ErrorDeNegocio al ProblemDetails que consume el SPA.</summary>
public static class MapeoDeErrores
{
    public static ProblemDetails ProblemDetails(ErrorDeNegocio error) => new()
    {
        Status = error.Estado,
        Title = error.Titulo,
        Detail = error.Message,
        Extensions = { ["codigo"] = error.Codigo }
    };
}
