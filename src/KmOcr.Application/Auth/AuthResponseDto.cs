namespace KmOcr.Application.Auth;

/// <summary>
/// Token response returned after login or refresh.
/// </summary>
public sealed record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    AuthenticatedUserDto User,
    IReadOnlyList<string> Permissions);
