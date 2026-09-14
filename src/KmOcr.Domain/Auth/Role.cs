using KmOcr.Domain.Common;

namespace KmOcr.Domain.Auth;

/// <summary>
/// Represents an RBAC role that groups one or more permission grants.
/// </summary>
public sealed class Role : Entity
{
    private readonly List<RolePermission> _permissions = [];

    /// <summary>
    /// Creates an empty role for Entity Framework.
    /// </summary>
    private Role()
    {
        Code = string.Empty;
        Name = string.Empty;
    }

    /// <summary>
    /// Creates a role with a normalized code.
    /// </summary>
    private Role(string code, string name, bool isSystemRole)
    {
        Code = NormalizeCode(code);
        Name = RequireText(name, nameof(name));
        IsSystemRole = isSystemRole;
        IsActive = true;
    }

    /// <summary>
    /// Gets the normalized role code.
    /// </summary>
    public string Code { get; private set; }

    /// <summary>
    /// Gets the role display name.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets whether the role is managed by the system.
    /// </summary>
    public bool IsSystemRole { get; private set; }

    /// <summary>
    /// Gets whether the role can currently grant access.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Gets permission grants assigned to the role.
    /// </summary>
    public IReadOnlyCollection<RolePermission> Permissions => _permissions.AsReadOnly();

    /// <summary>
    /// Creates a new active role.
    /// </summary>
    public static Role Create(string code, string name, bool isSystemRole)
    {
        return new Role(code, name, isSystemRole);
    }

    /// <summary>
    /// Grants a permission when it is not already granted.
    /// </summary>
    public void GrantPermission(Guid permissionId)
    {
        if (_permissions.Any(permission => permission.PermissionId == permissionId))
        {
            return;
        }

        _permissions.Add(RolePermission.Create(Id, permissionId));
        Touch();
    }

    /// <summary>
    /// Returns true when the role already grants the supplied permission.
    /// </summary>
    public bool HasPermission(Guid permissionId)
    {
        return _permissions.Any(permission => permission.PermissionId == permissionId);
    }

    /// <summary>
    /// Normalizes and validates a role code.
    /// </summary>
    private static string NormalizeCode(string value)
    {
        return RequireText(value, nameof(value)).ToLowerInvariant();
    }

    /// <summary>
    /// Validates required text and returns the trimmed value.
    /// </summary>
    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }
}
