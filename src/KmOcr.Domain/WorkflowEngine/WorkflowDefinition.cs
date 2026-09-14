using System.Text.Json;
using KmOcr.Domain.Common;

namespace KmOcr.Domain.WorkflowEngine;

/// <summary>
/// Aggregate root for a node-based workflow definition.
/// </summary>
public sealed class WorkflowDefinition : Entity
{
    private readonly List<WorkflowNode> _nodes = [];
    private readonly List<WorkflowEdge> _edges = [];
    private readonly List<WorkflowDefinitionVersion> _versions = [];

    /// <summary>
    /// Creates an empty definition for Entity Framework.
    /// </summary>
    private WorkflowDefinition()
    {
        Code = string.Empty;
        Name = string.Empty;
        CreatedBy = string.Empty;
    }

    /// <summary>
    /// Creates a workflow definition.
    /// </summary>
    private WorkflowDefinition(string code, string name, string createdBy)
    {
        Code = RequireText(code, nameof(code));
        Name = RequireText(name, nameof(name));
        CreatedBy = RequireText(createdBy, nameof(createdBy));
    }

    /// <summary>
    /// Gets the unique workflow definition code.
    /// </summary>
    public string Code { get; private set; }

    /// <summary>
    /// Gets the workflow definition name.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the user that created the definition.
    /// </summary>
    public string CreatedBy { get; private set; }

    /// <summary>
    /// Gets whether the definition has a published version.
    /// </summary>
    public bool IsPublished { get; private set; }

    /// <summary>
    /// Gets the latest published version number.
    /// </summary>
    public int PublishedVersionNumber { get; private set; }

    /// <summary>
    /// Gets active designer nodes.
    /// </summary>
    public IReadOnlyCollection<WorkflowNode> Nodes => _nodes.AsReadOnly();

    /// <summary>
    /// Gets active designer edges.
    /// </summary>
    public IReadOnlyCollection<WorkflowEdge> Edges => _edges.AsReadOnly();

    /// <summary>
    /// Gets immutable published versions.
    /// </summary>
    public IReadOnlyCollection<WorkflowDefinitionVersion> Versions => _versions.AsReadOnly();

    /// <summary>
    /// Creates a workflow definition aggregate.
    /// </summary>
    public static WorkflowDefinition Create(string code, string name, string createdBy)
    {
        return new WorkflowDefinition(code, name, createdBy);
    }

    /// <summary>
    /// Adds a node to the workflow graph.
    /// </summary>
    public WorkflowNode AddNode(string nodeKey, WorkflowNodeType nodeType, decimal positionX, decimal positionY, string configJson)
    {
        if (_nodes.Any(node => node.NodeKey.Equals(nodeKey, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Node '{nodeKey}' already exists.");
        }

        var node = new WorkflowNode(Id, nodeKey, nodeType, positionX, positionY, configJson);
        _nodes.Add(node);
        Touch();
        return node;
    }

    /// <summary>
    /// Adds an edge to the workflow graph.
    /// </summary>
    public WorkflowEdge AddEdge(string sourceNodeKey, string targetNodeKey, string? conditionExpression)
    {
        var edge = new WorkflowEdge(Id, sourceNodeKey, targetNodeKey, conditionExpression);
        _edges.Add(edge);
        Touch();
        return edge;
    }

    /// <summary>
    /// Replaces the editable graph with a new set of nodes and edges.
    /// </summary>
    public void ReplaceGraph(IEnumerable<WorkflowNodeDraft> nodes, IEnumerable<WorkflowEdgeDraft> edges)
    {
        _nodes.Clear();
        _edges.Clear();

        foreach (var node in nodes)
        {
            AddNode(node.NodeKey, node.NodeType, node.PositionX, node.PositionY, node.ConfigJson);
        }

        foreach (var edge in edges)
        {
            AddEdge(edge.SourceNodeKey, edge.TargetNodeKey, edge.ConditionExpression);
        }

        Touch();
    }

    /// <summary>
    /// Validates graph rules required before publish or execution.
    /// </summary>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (_nodes.Count(node => node.NodeType == WorkflowNodeType.Start) != 1)
        {
            errors.Add("Workflow graph must contain exactly one start node.");
        }

        if (_nodes.Count(node => node.NodeType == WorkflowNodeType.End) == 0)
        {
            errors.Add("Workflow graph must contain at least one end node.");
        }

        var nodeKeys = _nodes.Select(node => node.NodeKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var edge in _edges)
        {
            if (!nodeKeys.Contains(edge.SourceNodeKey) || !nodeKeys.Contains(edge.TargetNodeKey))
            {
                errors.Add($"Edge '{edge.SourceNodeKey}->{edge.TargetNodeKey}' references a missing node.");
            }
        }

        foreach (var group in _edges.GroupBy(edge => edge.SourceNodeKey, StringComparer.OrdinalIgnoreCase))
        {
            var source = _nodes.SingleOrDefault(node => node.NodeKey.Equals(group.Key, StringComparison.OrdinalIgnoreCase));
            if (source is not null && source.NodeType != WorkflowNodeType.Condition && source.NodeType != WorkflowNodeType.Loop && group.Count() > 1)
            {
                errors.Add($"Only Condition or Loop nodes can have multiple outgoing edges. Node '{source.NodeKey}' is {source.NodeType}.");
            }
        }

        return errors;
    }

    /// <summary>
    /// Publishes an immutable graph version after validation passes.
    /// </summary>
    public WorkflowDefinitionVersion Publish(string publishedBy)
    {
        var errors = Validate();
        if (errors.Count > 0)
        {
            throw new InvalidOperationException(string.Join(" ", errors));
        }

        var versionNumber = PublishedVersionNumber + 1;
        var version = new WorkflowDefinitionVersion(Id, versionNumber, BuildGraphJson(), publishedBy);
        _versions.Add(version);
        IsPublished = true;
        PublishedVersionNumber = versionNumber;
        Touch();
        return version;
    }

    /// <summary>
    /// Builds the JSON graph snapshot used by published versions.
    /// </summary>
    private string BuildGraphJson()
    {
        var payload = new
        {
            nodes = _nodes.Select(node => new
            {
                node.NodeKey,
                nodeType = node.NodeType.ToString(),
                node.PositionX,
                node.PositionY,
                node.ConfigJson
            }),
            edges = _edges.Select(edge => new
            {
                edge.SourceNodeKey,
                edge.TargetNodeKey,
                edge.ConditionExpression
            })
        };
        return JsonSerializer.Serialize(payload);
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

/// <summary>
/// Draft node data used to replace a workflow graph.
/// </summary>
public sealed record WorkflowNodeDraft(string NodeKey, WorkflowNodeType NodeType, decimal PositionX, decimal PositionY, string ConfigJson);

/// <summary>
/// Draft edge data used to replace a workflow graph.
/// </summary>
public sealed record WorkflowEdgeDraft(string SourceNodeKey, string TargetNodeKey, string? ConditionExpression);
