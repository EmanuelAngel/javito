using Javito.Api.Modelos;

namespace Javito.Api.Contratos;

/// <summary>Proyecta el modelo a lo que viaja por HTTP. El modelo nunca se serializa.</summary>
public static class MapeoDeUsuarios
{
    public static UsuarioDto ADto(Usuario usuario) =>
        new(usuario.Id, usuario.Nombre, usuario.Email, usuario.Rol);
}
