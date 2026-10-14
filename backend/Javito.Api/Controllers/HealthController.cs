using Microsoft.AspNetCore.Mvc;

namespace Javito.Api.Controllers;

/// <summary>
/// Disponibilidad del servicio. Es el contrato mínimo que el SPA consume para
/// comprobar el vínculo entre el frontend desacoplado y la WebAPI.
/// </summary>
[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<RespuestaDeSalud>(StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return Ok(new RespuestaDeSalud("ok", "Javito.Api"));
    }
}

public sealed record RespuestaDeSalud(string Estado, string Servicio);
