using KmOcr.Application.Dashboard;
using KmOcr.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KmOcr.Api.Controllers;

/// <summary>
/// Dashboard APIs for operational metrics.
/// </summary>
[ApiController]
[Route("api/v1/dashboard")]
[Produces("application/json")]
[Authorize(Policy = PermissionCodes.DashboardRead)]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardModule _dashboard;

    /// <summary>
    /// Creates the controller with the dashboard application module.
    /// </summary>
    public DashboardController(IDashboardModule dashboard)
    {
        _dashboard = dashboard;
    }

    /// <summary>
    /// Gets document processing summary counts.
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardSummaryDto>> SummaryAsync(CancellationToken cancellationToken)
    {
        var summary = await _dashboard.GetSummaryAsync(cancellationToken);
        return Ok(summary);
    }

    /// <summary>
    /// Gets enterprise dashboard metrics for OCR, workflow, users, errors, performance, and storage.
    /// </summary>
    [HttpGet("enterprise")]
    [ProducesResponseType(typeof(EnterpriseDashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<EnterpriseDashboardDto>> EnterpriseAsync(CancellationToken cancellationToken)
    {
        var dashboard = await _dashboard.GetEnterpriseAsync(cancellationToken);
        return Ok(dashboard);
    }
}
