using KmOcr.Domain.Common;

namespace KmOcr.Domain.WorkflowEngine;

/// <summary>
/// Directed connection between two workflow nodes.
/// </summary>
public sealed class WorkflowEdge : Entity
{
    /// <summary>
    /// Creates an empty edge for Entity Framework.
    /// </summary>
    private WorkflowEdge()
    {
        SourceNodeKey = string.Empty;
        TargetNodeKey = string.Empty;
    }

    /// <summary>
    /// Creates a workflow edge.
    /// </summary>
    internal WorkflowEdge(Guid workflowDefinitionId, string sourceNodeKey, string targetNodeKey, string? conditionExpression)
    {
        WorkflowDefinitionId = workflowDefinitionId;
        SourceNodeKey = RequireText(sourceNodeKey, nameof(sourceNodeKey));
        TargetNodeKey = RequireText(targetNodeKey, nameof(targetNodeKey));
        ConditionExpression = string.IsNullOrWhiteSpace(conditionExpression) ? null : conditionExpression.Trim();
    }

    /// <summary>
    /// Gets the owning workflow definition id.
    /// </summary>
    public Guid WorkflowDefinitionId { get; private set; }

    /// <summary>
    /// Gets the source node key.
    /// </summary>
    public string SourceNodeKey { get; private set; }

    /// <summary>
    /// Gets the target node key.
    /// </summary>
    public string TargetNodeKey { get; private set; }

    /// <summary>
    /// Gets the optional edge condition expression.
    /// </summary>
    public string? ConditionExpression { get; private set; }

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
