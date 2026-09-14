namespace KmOcr.Application.Auth;

/// <summary>
/// Logout request passed into the authentication use case.
/// </summary>
public sealed record LogoutCommand(string RefreshToken, string Actor, string IpAddress);
