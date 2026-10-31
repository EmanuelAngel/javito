namespace Javito.Api.Contratos;

/// <summary>Lo que la API expone de una cuenta. El hash de la contraseña nunca sale del servidor.</summary>
public sealed record UsuarioDto(int Id, string Nombre, string Email, string Rol);
