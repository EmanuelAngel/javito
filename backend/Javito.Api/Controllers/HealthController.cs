using Javito.Api.Contratos;
using Javito.Api.Datos;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace Javito.Api.Controllers;

/// <summary>
/// Disponibilidad del servicio y de su base de datos. Es el contrato mínimo que el SPA consume
/// para comprobar el vínculo entre el frontend desacoplado y la WebAPI.
/// </summary>
[ApiController]
[Route("api/health")]
public class HealthController(IDbConnectionFactory fabrica) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<RespuestaDeSalud>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken)
    {
        var baseDeDatos = await EstadoDeBaseDeDatosAsync(cancellationToken);

        return Ok(new RespuestaDeSalud(
            baseDeDatos == "ok" ? "ok" : "degradado",
            "Javito.Api",
            baseDeDatos));
    }

    /// <summary>
    /// Un chequeo de salud no falla por la dependencia: informa. Cualquier excepción al abrir la
    /// conexión se traduce a "error" en el cuerpo, con 200.
    /// </summary>
    private async Task<string> EstadoDeBaseDeDatosAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var conexion = await fabrica.AbrirAsync(cancellationToken);
            await using var comando = new MySqlCommand("SELECT 1", conexion);
            await comando.ExecuteScalarAsync(cancellationToken);
            return "ok";
        }
        catch (Exception)
        {
            return "error";
        }
    }
}
