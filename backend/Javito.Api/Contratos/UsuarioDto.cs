namespace Javito.Api.Contratos;

/// <summary>
/// Lo que la API expone de una cuenta. El hash de la contraseña nunca sale del servidor.
/// <see cref="AvatarUrl"/> es una ruta relativa al almacén de archivos (<c>/uploads/avatares/…</c>)
/// o <c>null</c> si la cuenta todavía no subió una: el SPA decide qué mostrar en ese caso.
/// </summary>
public sealed record UsuarioDto(int Id, string Nombre, string Email, string Rol, string? AvatarUrl);
