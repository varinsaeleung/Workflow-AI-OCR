using KmOcr.Application.Common;
using KmOcr.Application.Contracts.Persistence;
using KmOcr.Application.Contracts.Security;
using KmOcr.Domain.Audit;
using KmOcr.Domain.Auth;
using Microsoft.Extensions.Logging;

namespace KmOcr.Application.Auth;

/// <summary>
/// Coordinates login, refresh token rotation, logout, RBAC lookup, and audit events.
/// </summary>
public sealed class AuthModule : IAuthModule
{
    private const string UserResource = "user";
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IAuditLogRepository _auditLogs;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuthModule> _logger;

    /// <summary>
    /// Creates the authentication module with persistence, security, audit, and logging dependencies.
    /// </summary>
    public AuthModule(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IAuditLogRepository auditLogs,
        IUnitOfWork unitOfWork,
        ILogger<AuthModule> logger)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _auditLogs = auditLogs;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates credentials and issues access and refresh tokens.
    /// </summary>
    public async Task<AuthResponseDto> LoginAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(command.Email);
        var user = await _users.GetByEmailAsync(email, cancellationToken);

        if (user is null || !_passwordHasher.VerifyPassword(command.Password, user.PasswordHash))
        {
            await AuditAsync(email, "auth.login_failed", "auth", email, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogWarning("Failed login attempt for {Email} from {IpAddress}.", email, command.IpAddress);
            throw new ValidationException("Invalid email or password.");
        }

        if (user.Status != UserStatus.Active)
        {
            await AuditAsync(user.Email, "auth.login_blocked", UserResource, user.Id.ToString(), cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new ValidationException("User account is not active.");
        }

        user.RecordLogin();
        var response = await IssueTokensAsync(user, command.IpAddress, command.UserAgent, cancellationToken);
        await AuditAsync(user.Email, "auth.login", UserResource, user.Id.ToString(), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return response;
    }

    /// <summary>
    /// Exchanges a valid refresh token for a new token pair.
    /// </summary>
    public async Task<AuthResponseDto> RefreshAsync(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var oldTokenHash = _tokenService.HashRefreshToken(command.RefreshToken);
        var existingToken = await _refreshTokens.GetByTokenHashAsync(oldTokenHash, cancellationToken);

        if (existingToken is null || !existingToken.IsActive)
        {
            await AuditAsync("anonymous", "auth.refresh_failed", "refresh_token", oldTokenHash, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new ValidationException("Refresh token is invalid or expired.");
        }

        var user = await _users.GetByIdAsync(existingToken.UserId, cancellationToken)
            ?? throw new NotFoundException($"User {existingToken.UserId} was not found.");

        if (user.Status != UserStatus.Active)
        {
            throw new ValidationException("User account is not active.");
        }

        var response = await IssueTokensAsync(user, command.IpAddress, command.UserAgent, cancellationToken);
        existingToken.Revoke(command.IpAddress, _tokenService.HashRefreshToken(response.RefreshToken));
        await AuditAsync(user.Email, "auth.refresh", UserResource, user.Id.ToString(), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return response;
    }

    /// <summary>
    /// Revokes the supplied refresh token for logout.
    /// </summary>
    public async Task LogoutAsync(LogoutCommand command, CancellationToken cancellationToken)
    {
        var tokenHash = _tokenService.HashRefreshToken(command.RefreshToken);
        var token = await _refreshTokens.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (token is not null && token.IsActive)
        {
            token.Revoke(command.IpAddress, null);
        }

        await AuditAsync(command.Actor, "auth.logout", "refresh_token", tokenHash, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Loads the authenticated user profile.
    /// </summary>
    public async Task<AuthenticatedUserDto> GetMeAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException($"User {userId} was not found.");
        return MapUser(user);
    }

    /// <summary>
    /// Loads effective permission codes for the authenticated user.
    /// </summary>
    public Task<IReadOnlyList<string>> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        return _users.GetPermissionCodesAsync(userId, cancellationToken);
    }

    /// <summary>
    /// Creates access and refresh tokens and persists the refresh token hash.
    /// </summary>
    private async Task<AuthResponseDto> IssueTokensAsync(User user, string ipAddress, string userAgent, CancellationToken cancellationToken)
    {
        var permissions = await _users.GetPermissionCodesAsync(user.Id, cancellationToken);
        var accessToken = _tokenService.CreateAccessToken(user, permissions);
        var refreshToken = _tokenService.CreateRefreshToken();
        var refreshTokenHash = _tokenService.HashRefreshToken(refreshToken);
        var refreshRecord = RefreshToken.Create(
            user.Id,
            refreshTokenHash,
            _tokenService.GetRefreshTokenExpiry(),
            NormalizeNetworkText(ipAddress),
            NormalizeNetworkText(userAgent));

        await _refreshTokens.AddAsync(refreshRecord, cancellationToken);
        return new AuthResponseDto(accessToken.Token, refreshToken, accessToken.ExpiresAt, MapUser(user), permissions);
    }

    /// <summary>
    /// Adds an audit log entry for an authentication event.
    /// </summary>
    private Task AuditAsync(string actor, string action, string resourceType, string resourceId, CancellationToken cancellationToken)
    {
        var auditLog = AuditLog.Create(NormalizeNetworkText(actor), action, resourceType, resourceId);
        return _auditLogs.AddAsync(auditLog, cancellationToken);
    }

    /// <summary>
    /// Maps the domain user to the public account DTO.
    /// </summary>
    private static AuthenticatedUserDto MapUser(User user)
    {
        return new AuthenticatedUserDto(user.Id, user.Email, user.DisplayName);
    }

    /// <summary>
    /// Normalizes email input for lookup.
    /// </summary>
    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ValidationException("Email is required.");
        }

        return email.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Replaces missing request metadata with a safe audit value.
    /// </summary>
    private static string NormalizeNetworkText(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim();
    }
}
