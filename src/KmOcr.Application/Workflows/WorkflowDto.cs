namespace KmOcr.Application.Workflows;

/// <summary>
/// Client-safe workflow representation returned by APIs.
/// </summary>
public sealed record WorkflowDto(
    Guid Id,
    Guid DocumentId,
    string TemplateKey,
    string StartedBy,
    DateTimeOffset CreatedAt,
    IReadOnlyList<WorkflowTaskDto> Tasks);
