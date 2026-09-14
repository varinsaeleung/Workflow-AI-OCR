using KmOcr.Domain.Auth;

namespace KmOcr.Application.Contracts.Security;

/// <summary>
/// Issues access tokens and secure refresh token material.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Creates a signed access token for the supplied user and permissions.
    /// </summary>
    AccessTokenResult CreateAccessToken(User user, IReadOnlyCollection<string> permissions);

    /// <summary>
    /// Creates an opaque refresh token value for the browser to store.
    /// </summary>
    string CreateRefreshToken();

    /// <summary>
    /// Hashes an opaque refresh token before lookup or storage.
    /// </summary>
    string HashRefreshToken(string refreshToken);

    /// <summary>
    /// Gets the refresh token expiration timestamp using configured policy.
    /// </summary>
    DateTimeOffset GetRefreshTokenExpiry();
}
