using System.Security.Claims;
using KmOcr.Application.Contracts.Persistence;
using KmOcr.Domain.Audit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace KmOcr.Api.Security;

/// <summary>
/// Writes audit entries when authenticated users are forbidden by permission policies.
/// </summary>
public sealed class AuditAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuditAuthorizationMiddlewareResultHandler> _logger;

    /// <summary>
    /// Creates the middleware result handler with scoped persistence access.
    /// </summary>
    public AuditAuthorizationMiddlewareResultHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<AuditAuthorizationMiddlewareResultHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Audits forbidden authorization results and then delegates to the framework handler.
    /// </summary>
    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden)
        {
            await AuditForbiddenAsync(context);
        }

        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }

    /// <summary>
    /// Writes a permission-denied audit entry without blocking the authorization response.
    /// </summary>
    private async Task AuditForbiddenAsync(HttpContext context)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var auditLogs = scope.ServiceProvider.GetRequiredService<IAuditLogRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var actor = context.User.FindFirstValue(ClaimTypes.Email)
                ?? context.User.Identity?.Name
                ?? "anonymous";
            var resourceId = context.Request.Path.Value ?? "unknown";
            await auditLogs.AddAsync(AuditLog.Create(actor, "auth.permission_denied", "route", resourceId), context.RequestAborted);
            await unitOfWork.SaveChangesAsync(context.RequestAborted);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Unable to write permission denied audit log.");
        }
    }
}
