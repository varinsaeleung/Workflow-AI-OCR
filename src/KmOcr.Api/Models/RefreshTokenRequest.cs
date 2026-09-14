namespace KmOcr.Api.Models;

/// <summary>
/// Refresh token exchange request body.
/// </summary>
public sealed record RefreshTokenRequest(string RefreshToken);
