namespace KmOcr.Application.Workflows;

/// <summary>
/// Command data required to approve or reject a workflow task.
/// </summary>
public sealed record CompleteWorkflowTaskCommand(Guid WorkflowId, Guid TaskId, string Actor, string? Comment);
