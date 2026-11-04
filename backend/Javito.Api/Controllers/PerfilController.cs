using Javito.Api.Archivos;
using Javito.Api.Contratos;
using Javito.Api.Repositorios;
using Javito.Api.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Javito.Api.Controllers;

/// <summary>
/// Perfil de la cuenta propia. Hoy sólo el avatar (<c>PER-02</c>); los datos personales llegan con
/// <c>PER-01</c> sobre esta misma ruta.
/// </summary>
[ApiController]
[Authorize]
[Route("api/perfil")]
public class PerfilController(IUsuarioRepositorio usuarios, IAlmacenDeArchivos almacen) : ControllerBase
{
    /// <summary>Subcarpeta del almacén. La elige el servidor, nunca el cliente.</summary>
    private const string SubcarpetaDeAvatares = "avatares";

    /// <summary>
    /// Guarda la imagen y deja la cuenta apuntando a ella. El avatar anterior se borra **después** de
    /// que la fila quedó actualizada: si la escritura falla, se borra la imagen nueva y la cuenta sigue
    /// mostrando la de antes (HU-PER-02: se reemplaza, no se acumula).
    /// </summary>
    [HttpPost("avatar")]
    [ProducesResponseType<UsuarioDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    public async Task<IActionResult> SubirAvatarAsync(
        [FromForm] IFormFile? archivo,
        CancellationToken cancellationToken)
    {
        if (!ReclamosDeSesion.TryUsuarioId(User, out var id))
        {
            return RespuestasDeError.De(ErroresDeSesion.SesionIlegible());
        }

        var usuario = await usuarios.BuscarPorIdAsync(id, cancellationToken);

        if (usuario is null)
        {
            return RespuestasDeError.De(ErroresDeSesion.SesionIlegible());
        }

        if (archivo is null)
        {
            return RespuestasDeError.De(ReglasDeArchivos.Faltante());
        }

        var guardado = await almacen.GuardarImagenAsync(archivo, SubcarpetaDeAvatares, cancellationToken);

        if (guardado.Error is not null)
        {
            return RespuestasDeError.De(guardado.Error);
        }

        var anterior = usuario.AvatarUrl;

        if (!await usuarios.ActualizarAvatarAsync(id, guardado.Ruta, cancellationToken))
        {
            almacen.Borrar(guardado.Ruta);

            // La cuenta existía dos líneas más arriba: que la fila no se actualice es un fallo no previsto.
            throw new InvalidOperationException($"No se pudo guardar el avatar del usuario {id}.");
        }

        almacen.Borrar(anterior);

        usuario.AvatarUrl = guardado.Ruta;

        return Ok(MapeoDeUsuarios.ADto(usuario));
    }
}
