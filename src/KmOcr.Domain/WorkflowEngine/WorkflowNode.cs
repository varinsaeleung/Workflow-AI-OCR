using KmOcr.Domain.Common;

namespace KmOcr.Domain.WorkflowEngine;

/// <summary>
/// Node in a workflow definition graph.
/// </summary>
public sealed class WorkflowNode : Entity
{
    /// <summary>
    /// Creates an empty node for Entity Framework.
    /// </summary>
    private WorkflowNode()
    {
        NodeKey = string.Empty;
        ConfigJson = "{}";
    }

    /// <summary>
    /// Creates a workflow node.
    /// </summary>
    internal WorkflowNode(Guid workflowDefinitionId, string nodeKey, WorkflowNodeType nodeType, decimal positionX, decimal positionY, string configJson)
    {
        WorkflowDefinitionId = workflowDefinitionId;
        NodeKey = RequireText(nodeKey, nameof(nodeKey));
        NodeType = nodeType;
        PositionX = positionX;
        PositionY = positionY;
        ConfigJson = string.IsNullOrWhiteSpace(configJson) ? "{}" : configJson.Trim();
    }

    /// <summary>
    /// Gets the owning workflow definition id.
    /// </summary>
    public Guid WorkflowDefinitionId { get; private set; }

    /// <summary>
    /// Gets the stable designer node key.
    /// </summary>
    public string NodeKey { get; private set; }

    /// <summary>
    /// Gets the node type.
    /// </summary>
    public WorkflowNodeType NodeType { get; private set; }

    /// <summary>
    /// Gets the horizontal designer position.
    /// </summary>
    public decimal PositionX { get; private set; }

    /// <summary>
    /// Gets the vertical designer position.
    /// </summary>
    public decimal PositionY { get; private set; }

    /// <summary>
    /// Gets node configuration JSON.
    /// </summary>
    public string ConfigJson { get; private set; }

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
