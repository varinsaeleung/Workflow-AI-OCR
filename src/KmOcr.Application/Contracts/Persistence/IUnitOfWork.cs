namespace KmOcr.Application.Contracts.Persistence;

/// <summary>
/// Coordinates transactional persistence across repositories.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Commits pending repository changes.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
