using System.Security.Claims;
using KmOcr.Api.Models;
using KmOcr.Application.Auth;
using KmOcr.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KmOcr.Api.Controllers;

/// <summary>
/// Authentication APIs for login, refresh token rotation, logout, and current-user profile.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthModule _auth;

    /// <summary>
    /// Creates the controller with the authentication application module.
    /// </summary>
    public AuthController(IAuthModule auth)
    {
        _auth = auth;
    }

    /// <summary>
    /// Authenticates a user and returns JWT access and refresh tokens.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponseDto>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _auth.LoginAsync(
            new LoginCommand(request.Email, request.Password, GetIpAddress(), GetUserAgent()),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Rotates a refresh token and returns a new JWT access token pair.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponseDto>> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await _auth.RefreshAsync(
            new RefreshTokenCommand(request.RefreshToken, GetIpAddress(), GetUserAgent()),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Revokes the current refresh token.
    /// </summary>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LogoutAsync(LogoutRequest request, CancellationToken cancellationToken)
    {
        await _auth.LogoutAsync(new LogoutCommand(request.RefreshToken, GetActor(), GetIpAddress()), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Gets the authenticated user profile.
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(AuthenticatedUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuthenticatedUserDto>> MeAsync(CancellationToken cancellationToken)
    {
        var user = await _auth.GetMeAsync(GetUserId(), cancellationToken);
        return Ok(user);
    }

    /// <summary>
    /// Gets effective permissions for the authenticated user.
    /// </summary>
    [Authorize]
    [HttpGet("permissions")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<string>>> PermissionsAsync(CancellationToken cancellationToken)
    {
        var permissions = await _auth.GetPermissionsAsync(GetUserId(), cancellationToken);
        return Ok(permissions);
    }

    /// <summary>
    /// Reads the authenticated user identifier from JWT claims.
    /// </summary>
    private Guid GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(value, out var userId))
        {
            throw new ValidationException("Authenticated user id is invalid.");
        }

        return userId;
    }

    /// <summary>
    /// Reads the current actor name from JWT claims.
    /// </summary>
    private string GetActor()
    {
        return User.FindFirstValue(ClaimTypes.Email)
            ?? User.FindFirstValue(ClaimTypes.Name)
            ?? "unknown";
    }

    /// <summary>
    /// Gets the caller IP address for token audit metadata.
    /// </summary>
    private string GetIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    /// <summary>
    /// Gets the caller user-agent for refresh token audit metadata.
    /// </summary>
    private string GetUserAgent()
    {
        return Request.Headers.UserAgent.ToString();
    }
}
