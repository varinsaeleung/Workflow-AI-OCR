using KmOcr.Application.Contracts.Persistence;
using KmOcr.Domain.Auth;
using KmOcr.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KmOcr.Infrastructure.Repositories;

/// <summary>
/// Entity Framework repository for refresh token persistence.
/// </summary>
public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Creates the repository with the application database context.
    /// </summary>
    public RefreshTokenRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Adds a refresh token to the current unit of work.
    /// </summary>
    public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        await _context.RefreshTokens.AddAsync(refreshToken, cancellationToken);
    }

    /// <summary>
    /// Gets a refresh token by secure token hash.
    /// </summary>
    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        return _context.RefreshTokens.SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
    }
}
