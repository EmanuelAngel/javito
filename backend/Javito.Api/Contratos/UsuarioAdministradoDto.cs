namespace Javito.Api.Contratos;

/// <summary>
/// Lo que la pantalla de administración de usuarios necesita de una cuenta: los datos que se leen y se
/// corrigen, con el estado a la vista. No reemplaza a <see cref="UsuarioDto"/>, que es el contrato de
/// la sesión (viaja dentro de <c>SesionDto</c> en el login y en <c>GET api/sesion/yo</c>): sumarle
/// <see cref="Activo"/> obligaría a tocar dos caminos que este módulo no tiene por qué mover.
/// <c>password_hash</c> no aparece acá ni en ninguna otra proyección: no sale del servidor.
/// </summary>
public sealed record UsuarioAdministradoDto(
    int Id,
    string Nombre,
    string Email,
    string Rol,
    bool Activo);
