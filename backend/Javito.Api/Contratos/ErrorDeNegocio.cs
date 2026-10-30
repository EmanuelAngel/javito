namespace Javito.Api.Contratos;

/// <summary>
/// Regla de negocio incumplida. El código es parte del contrato: el SPA decide con él y nunca
/// comparando textos. El manejador de errores lo traduce a ProblemDetails.
/// </summary>
public sealed class ErrorDeNegocio(
    string codigo,
    string titulo,
    string detalle,
    int estado = StatusCodes.Status409Conflict) : Exception(detalle)
{
    public string Codigo { get; } = codigo;

    public string Titulo { get; } = titulo;

    public int Estado { get; } = estado;
}
