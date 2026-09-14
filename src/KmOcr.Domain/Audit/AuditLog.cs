using KmOcr.Domain.Common;

namespace KmOcr.Domain.Audit;

/// <summary>
/// Captures an immutable user or system action for enterprise auditability.
/// </summary>
public sealed class AuditLog : Entity
{
    /// <summary>
    /// Creates an empty audit log for Entity Framework.
    /// </summary>
    private AuditLog()
    {
        Actor = string.Empty;
        Action = string.Empty;
        ResourceType = string.Empty;
        ResourceId = string.Empty;
    }

    /// <summary>
    /// Creates an audit log entry.
    /// </summary>
    private AuditLog(string actor, string action, string resourceType, string resourceId)
    {
        Actor = RequireText(actor, nameof(actor));
        Action = RequireText(action, nameof(action));
        ResourceType = RequireText(resourceType, nameof(resourceType));
        ResourceId = RequireText(resourceId, nameof(resourceId));
    }

    /// <summary>
    /// Gets the user or system actor that performed the action.
    /// </summary>
    public string Actor { get; private set; }

    /// <summary>
    /// Gets the action name.
    /// </summary>
    public string Action { get; private set; }

    /// <summary>
    /// Gets the affected resource type.
    /// </summary>
    public string ResourceType { get; private set; }

    /// <summary>
    /// Gets the affected resource identifier.
    /// </summary>
    public string ResourceId { get; private set; }

    /// <summary>
    /// Creates a new audit log entry.
    /// </summary>
    public static AuditLog Create(string actor, string action, string resourceType, string resourceId)
    {
        return new AuditLog(actor, action, resourceType, resourceId);
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
