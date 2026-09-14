using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using KmOcr.Domain.Auth;
using KmOcr.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Xunit;

namespace KmOcr.Infrastructure.Tests;

/// <summary>
/// Verifies infrastructure security adapters.
/// </summary>
public sealed class AuthSecurityTests
{
    /// <summary>
    /// Ensures PBKDF2 hashing does not store plaintext and validates only the correct password.
    /// </summary>
    [Fact]
    public void Password_hasher_should_hash_and_verify_passwords()
    {
        var hasher = new Pbkdf2PasswordHasher();

        var hash = hasher.HashPassword("Pass@123");

        hash.Should().NotBe("Pass@123");
        hasher.VerifyPassword("Pass@123", hash).Should().BeTrue();
        hasher.VerifyPassword("wrong", hash).Should().BeFalse();
    }

    /// <summary>
    /// Ensures JWT creation includes subject, email, and permission claims.
    /// </summary>
    [Fact]
    public void Jwt_token_service_should_create_access_token_with_permission_claims()
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = "km-ocr",
            Audience = "km-ocr-web",
            SigningKey = "development-signing-key-for-unit-tests-64-bytes-long",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 7
        });
        var service = new JwtTokenService(options);
        var user = User.Create("admin@km.local", "System Admin", "hashed-password");

        var result = service.CreateAccessToken(user, ["documents.read"]);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);

        token.Claims.Should().Contain(claim => claim.Type == JwtRegisteredClaimNames.Sub && claim.Value == user.Id.ToString());
        token.Claims.Should().Contain(claim => claim.Type == JwtRegisteredClaimNames.Email && claim.Value == "admin@km.local");
        token.Claims.Should().Contain(claim => claim.Type == "permission" && claim.Value == "documents.read");
        result.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }
}
