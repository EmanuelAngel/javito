using Javito.Api.Contratos;
using Javito.Api.Modelos;
using Javito.Api.Paginacion;
using Javito.Api.Repositorios;
using Javito.Api.Seguridad;
using Javito.Api.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Javito.Api.Controllers;

/// <summary>
/// USU-01 (HU-USU-01), USU-02 (HU-USU-02) y USU-03 (HU-USU-03): el listado de cuentas, el alta desde
/// administración y la edición de los datos de una cuenta.<br/>
/// Todo el controlador es de administración, así que cada acción lleva el rol: la sesión sola no
/// alcanza. Un Usuario autenticado recibe 403 y ninguna fila se toca.
/// </summary>
[ApiController]
[Authorize]
[Route("api/usuarios")]
public sealed class UsuariosController(IUsuarioRepositorio usuarios) : ControllerBase
{
    /// <summary>
    /// Una página de cuentas, con el total para el paginador. Sin <paramref name="q"/> ni
    /// <paramref name="activo"/> trae todo: activos y desactivados, que es lo que la pantalla separa
    /// con el filtro de estado.
    /// </summary>
    /// <param name="q">
    /// Pedazo del nombre o del email. Los comodines de LIKE se buscan literales: «%» no trae todo.
    /// </param>
    [HttpGet]
    [Authorize(Roles = Roles.Administrador)]
    [ProducesResponseType<PaginaDto<UsuarioAdministradoDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListarAsync(
        [FromQuery] ParametrosDePagina parametros,
        [FromQuery] bool? activo,
        [FromQuery] string? q,
        CancellationToken cancellationToken)
    {
        var (elementos, total) = await usuarios.ListarAsync(
            activo,
            q,
            parametros.TamanioDePagina,
            parametros.Desplazamiento,
            cancellationToken);

        var dtos = elementos.Select(MapeoDeUsuarios.AAdministrado).ToList();

        return Ok(PaginaDto<UsuarioAdministradoDto>.De(dtos, parametros, total));
    }

    /// <summary>
    /// USU-03 (HU-USU-03): el detalle de una cuenta. Existe porque la pantalla de edición tiene que
    /// precargar el nombre y el email actuales y no había otra fuente honesta: el listado devuelve
    /// páginas y obligaría a buscar por texto la fila que ya se sabe cuál es. Devuelve exactamente lo
    /// mismo que una fila del listado, con el hash afuera (S14).
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = Roles.Administrador)]
    [ProducesResponseType<UsuarioAdministradoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerAsync(int id, CancellationToken cancellationToken)
    {
        var usuario = await usuarios.BuscarPorIdAsync(id, cancellationToken);

        return usuario is null
            ? NoEncontrado(id)
            : Ok(MapeoDeUsuarios.AAdministrado(usuario));
    }

    /// <summary>
    /// USU-02 (HU-USU-02): el administrador crea la cuenta y le pone la contraseña inicial, que le
    /// comunica él mismo: el sistema no manda emails (S6). El alta es la excepción; el camino normal
    /// sigue siendo el registro público (S5).
    /// </summary>
    /// <remarks>
    /// El orden importa: primero se normaliza el email, después se valida el rol y recién entonces se
    /// consulta el duplicado. Un email con otra caja es el mismo email para la base, así que el 409 se
    /// detecta antes de intentar el INSERT y el <c>UNIQUE</c> queda como última defensa. Si dos altas
    /// del mismo email se cruzan, la excepción de MySQL no se atrapa: sale un 500, igual que en
    /// categorías, antes que dejar entrar un duplicado.
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = Roles.Administrador)]
    [ProducesResponseType<UsuarioAdministradoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CrearAsync(
        [FromBody] AltaDeUsuarioDto entrada,
        CancellationToken cancellationToken)
    {
        var email = ReglasDeUsuarios.NormalizarEmail(entrada.Email);

        if (!ReglasDeUsuarios.EsRolValido(entrada.Rol))
        {
            return RolInvalido(entrada.Rol);
        }

        if (await usuarios.BuscarPorEmailAsync(email, cancellationToken) is not null)
        {
            return EmailRepetido(email);
        }

        var usuario = new Usuario
        {
            Rol = entrada.Rol,
            Nombre = entrada.Nombre.Trim(),
            Email = email,
            // El mismo algoritmo que verifica el login: la contraseña en claro no se guarda ni se
            // registra en ningún lado, y el DTO de salida no tiene campo donde devolverla.
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(entrada.Password),
            AvatarUrl = null,
            Activo = true
        };

        usuario.Id = await usuarios.CrearAsync(usuario, cancellationToken);

        // 201 con el cuerpo y sin Location, como quedó en L13: la Location del alta viaja con la
        // parte 2, junto con la decisión de si el alta pasa a usar CreatedAtAction.
        return StatusCode(StatusCodes.Status201Created, MapeoDeUsuarios.AAdministrado(usuario));
    }

    /// <summary>
    /// USU-03 (HU-USU-03): el administrador corrige el nombre y el email de una cuenta. Son los datos
    /// de la identidad de la fila; el rol, el estado y la contraseña no se editan acá (S11 y S15).
    /// </summary>
    /// <remarks>
    /// El orden importa y es el que sostiene la regla de que un rechazo no cambia la fila: primero se
    /// normaliza el email, después se confirma que la cuenta existe, recién entonces se busca el
    /// duplicado y sólo al final se escribe. Así el 404 no depende de que el UPDATE toque una fila, el
    /// 409 se detecta antes de intentar el UPDATE y el <c>UNIQUE</c> queda como última defensa: si dos
    /// ediciones del mismo email se cruzan, sale un 500 antes que un duplicado.
    /// </remarks>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Administrador)]
    [ProducesResponseType<UsuarioAdministradoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> EditarAsync(
        int id,
        [FromBody] EdicionDeUsuarioDto entrada,
        CancellationToken cancellationToken)
    {
        var email = ReglasDeUsuarios.NormalizarEmail(entrada.Email);
        var nombre = entrada.Nombre.Trim();

        if (await usuarios.BuscarPorIdAsync(id, cancellationToken) is null)
        {
            return NoEncontrado(id);
        }

        var conEseEmail = await usuarios.BuscarPorEmailAsync(email, cancellationToken);

        // El choque es con otra fila, no con la propia: guardar la cuenta con el email que ya tiene es
        // válido. Sin la comparación de id, cualquier edición del nombre con el mismo email daría 409.
        if (conEseEmail is not null && conEseEmail.Id != id)
        {
            return EmailRepetido(email);
        }

        if (!await usuarios.ActualizarAsync(id, nombre, email, cancellationToken))
        {
            // La fila existía al empezar y ya no está: se responde lo mismo que si nunca hubiera
            // estado, en vez de un 200 sobre una fila que no se escribió.
            return NoEncontrado(id);
        }

        // Se relee la fila y se devuelve ésa: el rol, el estado y el avatar no se inventan acá.
        var actualizada = await usuarios.BuscarPorIdAsync(id, cancellationToken);

        return actualizada is null
            ? NoEncontrado(id)
            : Ok(MapeoDeUsuarios.AAdministrado(actualizada));
    }

    /// <summary>
    /// Los errores previstos se devuelven, no se lanzan: el `codigo` estable es lo que lee el SPA.
    /// </summary>
    private static ObjectResult EmailRepetido(string email) => RespuestasDeError.De(new ErrorDeNegocio(
        "email_repetido",
        "Email repetido",
        $"Ya existe una cuenta con el email «{email}».",
        StatusCodes.Status409Conflict));

    /// <summary>
    /// El rol no es un dato libre: fuera de <c>Administrador</c> y <c>Usuario</c> el alta se rechaza
    /// antes de tocar la base, en vez de dejar que el <c>ENUM</c> decida por su intercalación.
    /// </summary>
    private static ObjectResult RolInvalido(string rol) => RespuestasDeError.De(new ErrorDeNegocio(
        "rol_invalido",
        "Rol inválido",
        $"«{rol}» no es un rol válido. Elegí {Roles.Administrador} o {Roles.Usuario}.",
        StatusCodes.Status400BadRequest));

    /// <summary>
    /// Mismo <c>no_encontrado</c> que el resto del repo —el único código de 404—, en un solo lugar del
    /// controlador: el detalle y la edición tienen que explicarlo igual.
    /// </summary>
    private static ObjectResult NoEncontrado(int id) => RespuestasDeError.De(new ErrorDeNegocio(
        "no_encontrado",
        "Usuario inexistente",
        $"No hay ninguna cuenta con id {id}.",
        StatusCodes.Status404NotFound));
}
