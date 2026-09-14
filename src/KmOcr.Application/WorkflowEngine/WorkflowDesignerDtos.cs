namespace KmOcr.Application.WorkflowEngine;

/// <summary>
/// Client-safe workflow definition DTO.
/// </summary>
public sealed record WorkflowDefinitionDto(
    Guid Id,
    string Code,
    string Name,
    string CreatedBy,
    bool IsPublished,
    int PublishedVersionNumber,
    WorkflowGraphDto Graph);

/// <summary>
/// Client-safe workflow graph DTO for designer nodes and edges.
/// </summary>
public sealed record WorkflowGraphDto(IReadOnlyList<WorkflowNodeDto> Nodes, IReadOnlyList<WorkflowEdgeDto> Edges);

/// <summary>
/// Client-safe workflow node DTO.
/// </summary>
public sealed record WorkflowNodeDto(string NodeKey, string NodeType, decimal PositionX, decimal PositionY, string ConfigJson);

/// <summary>
/// Client-safe workflow edge DTO.
/// </summary>
public sealed record WorkflowEdgeDto(string SourceNodeKey, string TargetNodeKey, string? ConditionExpression);

/// <summary>
/// Client-safe validation result DTO.
/// </summary>
public sealed record WorkflowValidationDto(bool IsValid, IReadOnlyList<string> Errors);
