using KmOcr.Domain.Common;

namespace KmOcr.Domain.WorkflowEngine;

/// <summary>
/// Immutable published workflow graph snapshot.
/// </summary>
public sealed class WorkflowDefinitionVersion : Entity
{
    /// <summary>
    /// Creates an empty version for Entity Framework.
    /// </summary>
    private WorkflowDefinitionVersion()
    {
        GraphJson = "{}";
        PublishedBy = string.Empty;
    }

    /// <summary>
    /// Creates an immutable published workflow definition version.
    /// </summary>
    internal WorkflowDefinitionVersion(Guid workflowDefinitionId, int versionNumber, string graphJson, string publishedBy)
    {
        WorkflowDefinitionId = workflowDefinitionId;
        VersionNumber = versionNumber > 0 ? versionNumber : throw new ArgumentOutOfRangeException(nameof(versionNumber), "Version number must be positive.");
        GraphJson = string.IsNullOrWhiteSpace(graphJson) ? "{}" : graphJson;
        PublishedBy = string.IsNullOrWhiteSpace(publishedBy) ? throw new ArgumentException("Value is required.", nameof(publishedBy)) : publishedBy.Trim();
    }

    /// <summary>
    /// Gets the workflow definition id.
    /// </summary>
    public Guid WorkflowDefinitionId { get; private set; }

    /// <summary>
    /// Gets the immutable published version number.
    /// </summary>
    public int VersionNumber { get; private set; }

    /// <summary>
    /// Gets the published graph JSON.
    /// </summary>
    public string GraphJson { get; private set; }

    /// <summary>
    /// Gets the user that published the version.
    /// </summary>
    public string PublishedBy { get; private set; }
}
