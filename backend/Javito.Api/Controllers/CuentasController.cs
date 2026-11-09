using Javito.Api.Contratos;
using Javito.Api.Modelos;
using Javito.Api.Repositorios;
using Javito.Api.Seguridad;
using Javito.Api.Usuarios;
using Microsoft.AspNetCore.Mvc;

namespace Javito.Api.Controllers;

/// <summary>
/// ACC-01 (HU-ACC-01): el registro público. Es anónimo a propósito; el alta con rol elegido sigue
/// siendo <c>POST /api/usuarios</c>, sólo de administración.
/// </summary>
[ApiController]
[Route("api/cuentas")]
public sealed class CuentasController(IUsuarioRepositorio usuarios, ServicioDeTokens tokens) : ControllerBase
{
    /// <summary>
    /// Crea una cuenta <c>Usuario</c> activa y devuelve la sesión ya iniciada, igual que el login.
    /// </summary>
    /// <remarks>
    /// El email repetido se detecta antes del INSERT; si dos registros del mismo email se cruzan, el
    /// <c>UNIQUE</c> responde con un 500 antes que dejar entrar un duplicado.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType<SesionDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegistrarAsync(
        [FromBody] RegistroDeCuentaDto entrada,
        CancellationToken cancellationToken)
    {
        var email = ReglasDeUsuarios.NormalizarEmail(entrada.Email);

        if (await usuarios.BuscarPorEmailAsync(email, cancellationToken) is not null)
        {
            return RespuestasDeError.De(new ErrorDeNegocio(
                "email_repetido",
                "Email repetido",
                $"Ya existe una cuenta con el email «{email}».",
                StatusCodes.Status409Conflict));
        }

        var usuario = new Usuario
        {
            Rol = Roles.Usuario,
            Nombre = entrada.Nombre.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(entrada.Password),
            AvatarUrl = null,
            Activo = true
        };

        usuario.Id = await usuarios.CrearAsync(usuario, cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            new SesionDto(tokens.Emitir(usuario), MapeoDeUsuarios.ADto(usuario)));
    }
}
