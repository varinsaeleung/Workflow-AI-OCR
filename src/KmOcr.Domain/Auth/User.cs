using KmOcr.Domain.Common;

namespace KmOcr.Domain.Auth;

/// <summary>
/// Represents an application user that can authenticate and receive RBAC roles.
/// </summary>
public sealed class User : Entity
{
    private readonly List<UserRole> _roles = [];

    /// <summary>
    /// Creates an empty user for Entity Framework.
    /// </summary>
    private User()
    {
        Email = string.Empty;
        DisplayName = string.Empty;
        PasswordHash = string.Empty;
    }

    /// <summary>
    /// Creates a user with normalized identity values.
    /// </summary>
    private User(string email, string displayName, string passwordHash)
    {
        Email = NormalizeEmail(email);
        DisplayName = RequireText(displayName, nameof(displayName));
        PasswordHash = RequireText(passwordHash, nameof(passwordHash));
        Status = UserStatus.Active;
    }

    /// <summary>
    /// Gets the normalized email address used for login.
    /// </summary>
    public string Email { get; private set; }

    /// <summary>
    /// Gets the display name shown in the user interface.
    /// </summary>
    public string DisplayName { get; private set; }

    /// <summary>
    /// Gets the password hash produced by the configured password hasher.
    /// </summary>
    public string PasswordHash { get; private set; }

    /// <summary>
    /// Gets the current account status.
    /// </summary>
    public UserStatus Status { get; private set; }

    /// <summary>
    /// Gets the latest successful login timestamp in UTC.
    /// </summary>
    public DateTimeOffset? LastLoginAt { get; private set; }

    /// <summary>
    /// Gets assigned role links for RBAC.
    /// </summary>
    public IReadOnlyCollection<UserRole> Roles => _roles.AsReadOnly();

    /// <summary>
    /// Creates a new active user.
    /// </summary>
    public static User Create(string email, string displayName, string passwordHash)
    {
        return new User(email, displayName, passwordHash);
    }

    /// <summary>
    /// Assigns a role to the user when it is not already assigned.
    /// </summary>
    public void AssignRole(Guid roleId, Guid? assignedByUserId = null)
    {
        if (_roles.Any(role => role.RoleId == roleId))
        {
            return;
        }

        _roles.Add(UserRole.Create(Id, roleId, assignedByUserId));
        Touch();
    }

    /// <summary>
    /// Returns true when the user already has the supplied role.
    /// </summary>
    public bool HasRole(Guid roleId)
    {
        return _roles.Any(role => role.RoleId == roleId);
    }

    /// <summary>
    /// Records a successful login timestamp.
    /// </summary>
    public void RecordLogin()
    {
        LastLoginAt = DateTimeOffset.UtcNow;
        Touch();
    }

    /// <summary>
    /// Blocks the user from future authentication attempts.
    /// </summary>
    public void Suspend()
    {
        Status = UserStatus.Suspended;
        Touch();
    }

    /// <summary>
    /// Restores user access after suspension or deactivation.
    /// </summary>
    public void Activate()
    {
        Status = UserStatus.Active;
        Touch();
    }

    /// <summary>
    /// Normalizes and validates an email address.
    /// </summary>
    private static string NormalizeEmail(string value)
    {
        var email = RequireText(value, nameof(value)).ToLowerInvariant();

        if (!email.Contains('@', StringComparison.Ordinal))
        {
            throw new ArgumentException("Email must contain @.", nameof(value));
        }

        return email;
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
