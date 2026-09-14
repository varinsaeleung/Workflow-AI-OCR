namespace KmOcr.Application.Workflows;

/// <summary>
/// Application module that exposes workflow use cases.
/// </summary>
public interface IWorkflowModule
{
    /// <summary>
    /// Starts a workflow for a document.
    /// </summary>
    Task<WorkflowDto> StartAsync(StartWorkflowCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Loads one workflow by identifier.
    /// </summary>
    Task<WorkflowDto> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Lists workflow tasks assigned to a user.
    /// </summary>
    Task<IReadOnlyList<WorkflowTaskDto>> GetMyTasksAsync(string assignee, CancellationToken cancellationToken);

    /// <summary>
    /// Assigns a workflow task to another user.
    /// </summary>
    Task<WorkflowDto> AssignAsync(AssignWorkflowTaskCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Approves a workflow task.
    /// </summary>
    Task<WorkflowDto> ApproveAsync(CompleteWorkflowTaskCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Rejects a workflow task.
    /// </summary>
    Task<WorkflowDto> RejectAsync(CompleteWorkflowTaskCommand command, CancellationToken cancellationToken);
}
