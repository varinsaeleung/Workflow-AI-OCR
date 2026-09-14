using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using KmOcr.Application.Contracts.Security;
using KmOcr.Domain.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace KmOcr.Infrastructure.Security;

/// <summary>
/// Issues signed JWT access tokens and opaque refresh tokens.
/// </summary>
public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;

    /// <summary>
    /// Creates the token service with validated JWT options.
    /// </summary>
    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
        ValidateOptions(_options);
    }

    /// <summary>
    /// Creates a signed access token for the supplied user and permissions.
    /// </summary>
    public AccessTokenResult CreateAccessToken(User user, IReadOnlyCollection<string> permissions)
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddMinutes(_options.AccessTokenMinutes);
        var credentials = new SigningCredentials(CreateSecurityKey(_options.SigningKey), SecurityAlgorithms.HmacSha256);
        var claims = CreateClaims(user, permissions);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new AccessTokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    /// <summary>
    /// Creates an opaque refresh token value for the browser to store.
    /// </summary>
    public string CreateRefreshToken()
    {
        return Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));
    }

    /// <summary>
    /// Hashes an opaque refresh token before lookup or storage.
    /// </summary>
    public string HashRefreshToken(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new ArgumentException("Refresh token is required.", nameof(refreshToken));
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Base64UrlEncoder.Encode(hash);
    }

    /// <summary>
    /// Gets the refresh token expiration timestamp using configured policy.
    /// </summary>
    public DateTimeOffset GetRefreshTokenExpiry()
    {
        return DateTimeOffset.UtcNow.AddDays(_options.RefreshTokenDays);
    }

    /// <summary>
    /// Creates claims used by authorization policies and account endpoints.
    /// </summary>
    private static List<Claim> CreateClaims(User user, IReadOnlyCollection<string> permissions)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.DisplayName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(permissions.Distinct(StringComparer.OrdinalIgnoreCase).Select(permission => new Claim("permission", permission)));
        return claims;
    }

    /// <summary>
    /// Creates the symmetric signing key from configuration.
    /// </summary>
    private static SymmetricSecurityKey CreateSecurityKey(string signingKey)
    {
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
    }

    /// <summary>
    /// Validates JWT options at startup and token service creation.
    /// </summary>
    private static void ValidateOptions(JwtOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            throw new InvalidOperationException("Jwt:Issuer is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            throw new InvalidOperationException("Jwt:Audience is required.");
        }

        if (Encoding.UTF8.GetByteCount(options.SigningKey) < 32)
        {
            throw new InvalidOperationException("Jwt:SigningKey must be at least 32 bytes.");
        }

        if (options.AccessTokenMinutes <= 0)
        {
            throw new InvalidOperationException("Jwt:AccessTokenMinutes must be greater than zero.");
        }

        if (options.RefreshTokenDays <= 0)
        {
            throw new InvalidOperationException("Jwt:RefreshTokenDays must be greater than zero.");
        }
    }
}
