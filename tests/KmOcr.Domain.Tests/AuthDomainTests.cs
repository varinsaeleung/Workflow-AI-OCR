using FluentAssertions;
using KmOcr.Domain.Auth;
using Xunit;

namespace KmOcr.Domain.Tests;

/// <summary>
/// Verifies identity and permission domain behavior.
/// </summary>
public sealed class AuthDomainTests
{
    /// <summary>
    /// Ensures a new user has normalized identity values and is enabled for login.
    /// </summary>
    [Fact]
    public void User_create_should_normalize_email_and_start_active()
    {
        var user = User.Create("  ADMIN@KM.LOCAL  ", "  System Admin  ", "hashed-password");

        user.Email.Should().Be("admin@km.local");
        user.DisplayName.Should().Be("System Admin");
        user.PasswordHash.Should().Be("hashed-password");
        user.Status.Should().Be(UserStatus.Active);
    }

    /// <summary>
    /// Ensures duplicate role assignment is ignored by the aggregate.
    /// </summary>
    [Fact]
    public void User_assign_role_should_keep_one_role_per_role_id()
    {
        var user = User.Create("admin@km.local", "System Admin", "hashed-password");
        var roleId = Guid.NewGuid();

        user.AssignRole(roleId);
        user.AssignRole(roleId);

        user.Roles.Should().ContainSingle(role => role.RoleId == roleId);
    }

    /// <summary>
    /// Ensures duplicate permission grants are ignored by the role aggregate.
    /// </summary>
    [Fact]
    public void Role_grant_permission_should_keep_one_permission_per_permission_id()
    {
        var role = Role.Create("admin", "Administrator", isSystemRole: true);
        var permissionId = Guid.NewGuid();

        role.GrantPermission(permissionId);
        role.GrantPermission(permissionId);

        role.Permissions.Should().ContainSingle(permission => permission.PermissionId == permissionId);
    }

    /// <summary>
    /// Ensures refresh tokens expose active state and revocation metadata.
    /// </summary>
    [Fact]
    public void Refresh_token_revoke_should_mark_token_inactive()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hashed-token", DateTimeOffset.UtcNow.AddDays(7), "127.0.0.1", "unit-test");

        token.IsActive.Should().BeTrue();
        token.Revoke("127.0.0.1", "replacement-hash");

        token.IsActive.Should().BeFalse();
        token.RevokedAt.Should().NotBeNull();
        token.RevokedByIp.Should().Be("127.0.0.1");
        token.ReplacedByTokenHash.Should().Be("replacement-hash");
    }
}
