namespace Javito.Api.Modelos;

/// <summary>
/// Cuenta de acceso. Refleja la tabla 1:1 y no lleva DataAnnotations: las reglas de entrada viven
/// en los DTO de los endpoints, donde producen un 400 legible.
/// </summary>
/// <remarks>
/// <see cref="PasswordHash"/> es el hash BCrypt del seed (<c>$2b$11$</c>). El modelo nunca se
/// serializa: el controlador proyecta a <c>UsuarioDto</c> antes de responder.
/// </remarks>
public class Usuario
{
    public int Id { get; set; }

    public string Rol { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    public bool Activo { get; set; }
}
