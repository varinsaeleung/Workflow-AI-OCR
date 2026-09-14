using KmOcr.Domain.Common;

namespace KmOcr.Domain.Auth;

/// <summary>
/// Represents one atomic permission checked by API authorization policies.
/// </summary>
public sealed class Permission : Entity
{
    /// <summary>
    /// Creates an empty permission for Entity Framework.
    /// </summary>
    private Permission()
    {
        Code = string.Empty;
        Name = string.Empty;
        Module = string.Empty;
    }

    /// <summary>
    /// Creates a permission definition.
    /// </summary>
    private Permission(string code, string name, string module)
    {
        Code = NormalizeCode(code);
        Name = RequireText(name, nameof(name));
        Module = RequireText(module, nameof(module));
    }

    /// <summary>
    /// Gets the normalized permission code.
    /// </summary>
    public string Code { get; private set; }

    /// <summary>
    /// Gets the permission display name.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the platform module that owns this permission.
    /// </summary>
    public string Module { get; private set; }

    /// <summary>
    /// Creates a new permission definition.
    /// </summary>
    public static Permission Create(string code, string name, string module)
    {
        return new Permission(code, name, module);
    }

    /// <summary>
    /// Normalizes and validates a permission code.
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
