namespace KmOcr.Application.Auth;

/// <summary>
/// User identity returned to the authenticated client.
/// </summary>
public sealed record AuthenticatedUserDto(Guid Id, string Email, string DisplayName);
