namespace KmOcr.Infrastructure.Security;

/// <summary>
/// Configuration options for JWT access tokens and refresh token lifetime.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>
    /// Gets or sets the trusted JWT issuer.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the trusted JWT audience.
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the symmetric signing key.
    /// </summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the access token lifetime in minutes.
    /// </summary>
    public int AccessTokenMinutes { get; set; } = 15;

    /// <summary>
    /// Gets or sets the refresh token lifetime in days.
    /// </summary>
    public int RefreshTokenDays { get; set; } = 7;
}
