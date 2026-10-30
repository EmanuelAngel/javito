namespace Javito.Api.Contratos;

/// <summary>Respuesta del login: el token firmado y quién lo recibió (S7 de la fundación).</summary>
public sealed record SesionDto(string Token, UsuarioDto Usuario);
