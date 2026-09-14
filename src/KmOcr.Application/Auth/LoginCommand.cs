namespace KmOcr.Application.Auth;

/// <summary>
/// Login request passed from the API into the authentication use case.
/// </summary>
public sealed record LoginCommand(string Email, string Password, string IpAddress, string UserAgent);
