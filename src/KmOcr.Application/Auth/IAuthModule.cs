namespace KmOcr.Application.Auth;

/// <summary>
/// Defines authentication and account read use cases.
/// </summary>
public interface IAuthModule
{
    /// <summary>
    /// Authenticates credentials and issues access and refresh tokens.
    /// </summary>
    Task<AuthResponseDto> LoginAsync(LoginCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Exchanges a valid refresh token for a new token pair.
    /// </summary>
    Task<AuthResponseDto> RefreshAsync(RefreshTokenCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Revokes the supplied refresh token for logout.
    /// </summary>
    Task LogoutAsync(LogoutCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Loads the authenticated user profile.
    /// </summary>
    Task<AuthenticatedUserDto> GetMeAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Loads effective permission codes for the authenticated user.
    /// </summary>
    Task<IReadOnlyList<string>> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken);
}
