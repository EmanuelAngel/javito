namespace Javito.Api.Contratos;

/// <summary>
/// El 401 de sesión, escrito una sola vez. Lo emiten cinco lugares —las cuatro acciones que necesitan
/// el id del token y el challenge de JwtBearer— y antes cada uno armaba su propio <see
/// cref="ErrorDeNegocio"/> con su propio texto: el mismo <c>codigo</c> se explicaba de cuatro formas.
/// </summary>
public static class ErroresDeSesion
{
    /// <summary>
    /// Falta el token, venció, o su <c>sub</c> no es un id. Para el cliente es un solo caso: la sesión
    /// no sirve para identificar filas y hay que volver a iniciarla.
    /// </summary>
    public static ErrorDeNegocio SesionIlegible() => new(
        "no_autenticado",
        "Sesión requerida",
        "La sesión no es válida o ya venció. Volvé a iniciar sesión.",
        StatusCodes.Status401Unauthorized);
}
