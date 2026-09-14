namespace KmOcr.Application.Workflows;

/// <summary>
/// Client-safe workflow task representation returned by APIs.
/// </summary>
public sealed record WorkflowTaskDto(
    Guid Id,
    Guid WorkflowId,
    string Name,
    string AssignedTo,
    string Status,
    string? CompletedBy,
    string? Comment,
    DateTimeOffset? CompletedAt);
