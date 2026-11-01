namespace Javito.Api.Seguridad;

/// <summary>
/// Roles del sistema, tal como viven en `usuario.rol` y viajan en el claim `rol`. Son constantes
/// porque <c>[Authorize(Roles = ...)]</c> exige un valor conocido al compilar: el mismo texto suelto
/// repetido en cada controlador se desincroniza en silencio.
/// </summary>
public static class Roles
{
    public const string Administrador = "Administrador";

    public const string Usuario = "Usuario";
}
