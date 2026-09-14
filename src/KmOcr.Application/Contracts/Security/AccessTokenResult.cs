namespace KmOcr.Application.Contracts.Security;

/// <summary>
/// Represents a signed access token and its expiry time.
/// </summary>
public sealed record AccessTokenResult(string Token, DateTimeOffset ExpiresAt);
