namespace Javito.Api.Seguridad;

/// <summary>
/// Nombres de los claims del JWT. Son parte del contrato con el SPA, así que se escriben una sola
/// vez acá y no como literales sueltos.
/// </summary>
public static class ReclamosDeSesion
{
    public const string Rol = "rol";

    public const string Nombre = "nombre";

    public const string Email = "email";
}
