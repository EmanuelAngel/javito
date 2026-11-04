using Javito.Api.Contratos;
using Javito.Api.Repositorios;
using Javito.Api.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Javito.Api.Controllers;

/// <summary>
/// Acceso: el login que emite el token y el "quién soy" que el SPA consulta al arrancar.
/// </summary>
[ApiController]
[Route("api/sesion")]
public class SesionController(IUsuarioRepositorio usuarios, ServicioDeTokens tokens) : ControllerBase
{
    /// <summary>
    /// Un solo detalle para email inexistente, contraseña incorrecta y cuenta desactivada: el
    /// cliente no puede deducir cuál de los tres falló.
    /// </summary>
    private const string DetalleDeCredencialesInvalidas = "El email o la contraseña no son correctos.";

    [HttpPost]
    [ProducesResponseType<SesionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> IniciarAsync(
        [FromBody] CredencialesDto credenciales,
        CancellationToken cancellationToken)
    {
        var usuario = await usuarios.BuscarPorEmailAsync(credenciales.Email, cancellationToken);

        // Cortocircuito: si no hay cuenta no se verifica la contraseña.
        if (usuario is null
            || !BCrypt.Net.BCrypt.Verify(credenciales.Password, usuario.PasswordHash)
            || !usuario.Activo)
        {
            return RespuestasDeError.De(new ErrorDeNegocio(
                "credenciales_invalidas",
                "No se pudo iniciar sesión",
                DetalleDeCredencialesInvalidas,
                StatusCodes.Status401Unauthorized));
        }

        return Ok(new SesionDto(tokens.Emitir(usuario), MapeoDeUsuarios.ADto(usuario)));
    }

    /// <summary>
    /// Devuelve la fila actual del usuario del token, no los claims congelados en él: si el perfil
    /// cambió el nombre, el SPA lo ve nuevo en la próxima recarga.
    /// </summary>
    /// <remarks>
    /// No se vuelve a mirar <c>activo</c>: la decisión S4 es que el token vale sus 8 horas y que la
    /// cuenta se valida sólo al entrar. Rechazarlo acá desloguearía al SPA mientras el mismo token
    /// seguiría sirviendo en el resto de la API.
    /// </remarks>
    [HttpGet("yo")]
    [Authorize]
    [ProducesResponseType<UsuarioDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> YoAsync(CancellationToken cancellationToken)
    {
        if (!ReclamosDeSesion.TryUsuarioId(User, out var id))
        {
            return RespuestasDeError.De(ErroresDeSesion.SesionIlegible());
        }

        var usuario = await usuarios.BuscarPorIdAsync(id, cancellationToken);

        return usuario is null
            ? RespuestasDeError.De(ErroresDeSesion.SesionIlegible())
            : Ok(MapeoDeUsuarios.ADto(usuario));
    }
}
