using Microsoft.AspNetCore.Mvc;

namespace KmOcr.Api.Controllers;

/// <summary>
/// Health APIs used by Docker and load balancers.
/// </summary>
[ApiController]
[Route("api/v1/health")]
[Produces("application/json")]
public sealed class HealthController : ControllerBase
{
    /// <summary>
    /// Returns a lightweight liveness response.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public ActionResult<object> Get()
    {
        return Ok(new { status = "ok", service = "km-ocr-api" });
    }
}
