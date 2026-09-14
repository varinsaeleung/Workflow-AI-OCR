namespace KmOcr.Domain.Auth;

/// <summary>
/// Links one RBAC role to one permission.
/// </summary>
public sealed class RolePermission
{
    /// <summary>
    /// Creates an empty permission grant for Entity Framework.
    /// </summary>
    private RolePermission()
    {
    }

    /// <summary>
    /// Creates a permission grant.
    /// </summary>
    private RolePermission(Guid roleId, Guid permissionId)
    {
        RoleId = roleId;
        PermissionId = permissionId;
        GrantedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets the role identifier.
    /// </summary>
    public Guid RoleId { get; private set; }

    /// <summary>
    /// Gets the permission identifier.
    /// </summary>
    public Guid PermissionId { get; private set; }

    /// <summary>
    /// Gets the grant timestamp.
    /// </summary>
    public DateTimeOffset GrantedAt { get; private set; }

    /// <summary>
    /// Creates a role-permission link.
    /// </summary>
    public static RolePermission Create(Guid roleId, Guid permissionId)
    {
        return new RolePermission(roleId, permissionId);
    }
}
