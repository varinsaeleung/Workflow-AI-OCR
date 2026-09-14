namespace KmOcr.Api.Models;

/// <summary>
/// Login request body.
/// </summary>
public sealed record LoginRequest(string Email, string Password);
