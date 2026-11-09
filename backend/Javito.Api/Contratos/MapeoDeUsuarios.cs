using Javito.Api.Modelos;

namespace Javito.Api.Contratos;

/// <summary>Proyecta el modelo a lo que viaja por HTTP. El modelo nunca se serializa.</summary>
public static class MapeoDeUsuarios
{
    public static UsuarioDto ADto(Usuario usuario) =>
        new(usuario.Id, usuario.Nombre, usuario.Email, usuario.Rol, usuario.AvatarUrl);

    /// <summary>
    /// La proyección de la pantalla de administración: agrega el estado y no lleva el avatar, que ahí
    /// no se muestra. El hash no se proyecta en ninguna de las dos.
    /// </summary>
    public static UsuarioAdministradoDto AAdministrado(Usuario usuario) =>
        new(usuario.Id, usuario.Nombre, usuario.Email, usuario.Rol, usuario.Activo);
}
