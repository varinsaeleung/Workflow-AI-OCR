namespace KmOcr.Domain.Auth;

/// <summary>
/// Defines whether a user can authenticate and access the platform.
/// </summary>
public enum UserStatus
{
    /// <summary>
    /// Indicates that the user can authenticate and use assigned permissions.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Indicates that the user is temporarily blocked from access.
    /// </summary>
    Suspended = 2,

    /// <summary>
    /// Indicates that the user is disabled and should not authenticate.
    /// </summary>
    Deactivated = 3
}
