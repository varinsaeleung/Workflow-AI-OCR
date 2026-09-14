using KmOcr.Domain.Auth;

namespace KmOcr.Application.Contracts.Persistence;

/// <summary>
/// Provides persistence access to users and their effective permissions.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Adds a user to the current unit of work.
    /// </summary>
    Task AddAsync(User user, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a user by normalized email address.
    /// </summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a user by identifier.
    /// </summary>
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Gets effective permission codes for a user.
    /// </summary>
    Task<IReadOnlyList<string>> GetPermissionCodesAsync(Guid userId, CancellationToken cancellationToken);
}
