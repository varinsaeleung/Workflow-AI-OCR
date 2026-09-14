namespace KmOcr.Domain.Workflows;

/// <summary>
/// Defines the lifecycle of a workflow task.
/// </summary>
public enum WorkflowTaskStatus
{
    /// <summary>
    /// The task is waiting for action.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// The task has been approved.
    /// </summary>
    Approved = 1,

    /// <summary>
    /// The task has been rejected.
    /// </summary>
    Rejected = 2
}
