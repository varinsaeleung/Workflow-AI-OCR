using KmOcr.Domain.Audit;

namespace KmOcr.Application.Contracts.Persistence;

/// <summary>
/// Repository contract for audit log persistence.
/// </summary>
public interface IAuditLogRepository
{
    /// <summary>
    /// Adds an audit log entry to the current unit of work.
    /// </summary>
    Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken);
}
