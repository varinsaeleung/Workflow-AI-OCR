namespace KmOcr.Api.Models;

/// <summary>
/// Logout request body.
/// </summary>
public sealed record LogoutRequest(string RefreshToken);
