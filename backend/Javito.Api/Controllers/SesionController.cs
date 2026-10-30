using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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

    private const string DetalleDeSesionIlegible = "El token no identifica una cuenta. Volvé a iniciar sesión.";

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
            return Unauthorized(MapeoDeErrores.ProblemDetails(new ErrorDeNegocio(
                "credenciales_invalidas",
                "No se pudo iniciar sesión",
                DetalleDeCredencialesInvalidas,
                StatusCodes.Status401Unauthorized)));
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
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!int.TryParse(sub, out var id))
        {
            return Unauthorized(MapeoDeErrores.ProblemDetails(SesionIlegible()));
        }

        var usuario = await usuarios.BuscarPorIdAsync(id, cancellationToken);

        return usuario is null
            ? Unauthorized(MapeoDeErrores.ProblemDetails(SesionIlegible()))
            : Ok(MapeoDeUsuarios.ADto(usuario));
    }

    private static ErrorDeNegocio SesionIlegible() => new(
        "no_autenticado",
        "Sesión requerida",
        DetalleDeSesionIlegible,
        StatusCodes.Status401Unauthorized);
}
