namespace KmOcr.Application.Workflows;

/// <summary>
/// Command data required to assign a workflow task to a user.
/// </summary>
public sealed record AssignWorkflowTaskCommand(Guid WorkflowId, Guid TaskId, string Assignee);
