using KmOcr.Domain.Auth;

namespace KmOcr.Application.Contracts.Persistence;

/// <summary>
/// Provides persistence access to hashed refresh tokens.
/// </summary>
public interface IRefreshTokenRepository
{
    /// <summary>
    /// Adds a refresh token to the current unit of work.
    /// </summary>
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a refresh token by secure token hash.
    /// </summary>
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);
}
