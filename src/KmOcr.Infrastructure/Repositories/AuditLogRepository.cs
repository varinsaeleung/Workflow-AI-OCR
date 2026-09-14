using KmOcr.Application.Contracts.Persistence;
using KmOcr.Domain.Audit;
using KmOcr.Infrastructure.Persistence;

namespace KmOcr.Infrastructure.Repositories;

/// <summary>
/// Entity Framework implementation of audit log repository operations.
/// </summary>
public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Creates the repository with an EF Core context.
    /// </summary>
    public AuditLogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Adds an audit log entry to the DbContext change tracker.
    /// </summary>
    public async Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken)
    {
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken);
    }
}
