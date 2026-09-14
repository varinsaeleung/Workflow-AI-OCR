namespace KmOcr.Application.Auth;

/// <summary>
/// Refresh token exchange request passed into the authentication use case.
/// </summary>
public sealed record RefreshTokenCommand(string RefreshToken, string IpAddress, string UserAgent);
