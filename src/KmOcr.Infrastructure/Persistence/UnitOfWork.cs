using KmOcr.Application.Contracts.Persistence;

namespace KmOcr.Infrastructure.Persistence;

/// <summary>
/// Entity Framework unit of work implementation.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Creates the unit of work with one scoped database context.
    /// </summary>
    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Commits pending changes in a single database transaction boundary.
    /// </summary>
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
