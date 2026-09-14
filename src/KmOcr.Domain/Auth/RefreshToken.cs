using KmOcr.Domain.Common;

namespace KmOcr.Domain.Auth;

/// <summary>
/// Represents a hashed refresh token that can be revoked and rotated.
/// </summary>
public sealed class RefreshToken : Entity
{
    /// <summary>
    /// Creates an empty refresh token for Entity Framework.
    /// </summary>
    private RefreshToken()
    {
        TokenHash = string.Empty;
        CreatedByIp = string.Empty;
        UserAgent = string.Empty;
    }

    /// <summary>
    /// Creates a refresh token with hashed token material.
    /// </summary>
    private RefreshToken(Guid userId, string tokenHash, DateTimeOffset expiresAt, string createdByIp, string userAgent)
    {
        UserId = userId;
        TokenHash = RequireText(tokenHash, nameof(tokenHash));
        ExpiresAt = expiresAt;
        CreatedByIp = RequireText(createdByIp, nameof(createdByIp));
        UserAgent = RequireText(userAgent, nameof(userAgent));
    }

    /// <summary>
    /// Gets the user that owns the refresh token.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the secure hash of the refresh token value.
    /// </summary>
    public string TokenHash { get; private set; }

    /// <summary>
    /// Gets the UTC expiration timestamp.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>
    /// Gets the revocation timestamp when the token is no longer usable.
    /// </summary>
    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>
    /// Gets the hash of the replacement refresh token when the token was rotated.
    /// </summary>
    public string? ReplacedByTokenHash { get; private set; }

    /// <summary>
    /// Gets the IP address that created the token.
    /// </summary>
    public string CreatedByIp { get; private set; }

    /// <summary>
    /// Gets the IP address that revoked the token.
    /// </summary>
    public string? RevokedByIp { get; private set; }

    /// <summary>
    /// Gets the user agent that requested the token.
    /// </summary>
    public string UserAgent { get; private set; }

    /// <summary>
    /// Gets whether the token can still be used.
    /// </summary>
    public bool IsActive => RevokedAt is null && ExpiresAt > DateTimeOffset.UtcNow;

    /// <summary>
    /// Creates a new active refresh token record.
    /// </summary>
    public static RefreshToken Create(Guid userId, string tokenHash, DateTimeOffset expiresAt, string createdByIp, string userAgent)
    {
        return new RefreshToken(userId, tokenHash, expiresAt, createdByIp, userAgent);
    }

    /// <summary>
    /// Revokes the token and optionally records the replacement token hash.
    /// </summary>
    public void Revoke(string revokedByIp, string? replacedByTokenHash)
    {
        if (RevokedAt is not null)
        {
            return;
        }

        RevokedAt = DateTimeOffset.UtcNow;
        RevokedByIp = RequireText(revokedByIp, nameof(revokedByIp));
        ReplacedByTokenHash = string.IsNullOrWhiteSpace(replacedByTokenHash) ? null : replacedByTokenHash.Trim();
        Touch();
    }

    /// <summary>
    /// Validates required text and returns the trimmed value.
    /// </summary>
    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }
}
