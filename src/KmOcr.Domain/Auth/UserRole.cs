namespace KmOcr.Domain.Auth;

/// <summary>
/// Links one user to one RBAC role.
/// </summary>
public sealed class UserRole
{
    /// <summary>
    /// Creates an empty role assignment for Entity Framework.
    /// </summary>
    private UserRole()
    {
    }

    /// <summary>
    /// Creates a role assignment.
    /// </summary>
    private UserRole(Guid userId, Guid roleId, Guid? assignedByUserId)
    {
        UserId = userId;
        RoleId = roleId;
        AssignedByUserId = assignedByUserId;
        AssignedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets the assigned user identifier.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the assigned role identifier.
    /// </summary>
    public Guid RoleId { get; private set; }

    /// <summary>
    /// Gets the administrator that assigned the role, when known.
    /// </summary>
    public Guid? AssignedByUserId { get; private set; }

    /// <summary>
    /// Gets the assignment timestamp.
    /// </summary>
    public DateTimeOffset AssignedAt { get; private set; }

    /// <summary>
    /// Creates a user-role link.
    /// </summary>
    public static UserRole Create(Guid userId, Guid roleId, Guid? assignedByUserId = null)
    {
        return new UserRole(userId, roleId, assignedByUserId);
    }
}
