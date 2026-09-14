using KmOcr.Domain.Common;

namespace KmOcr.Domain.Workflows;

/// <summary>
/// Represents one human approval task in a workflow instance.
/// </summary>
public sealed class WorkflowTask : Entity
{
    /// <summary>
    /// Creates an empty workflow task for Entity Framework.
    /// </summary>
    private WorkflowTask()
    {
        Name = string.Empty;
        AssignedTo = string.Empty;
    }

    /// <summary>
    /// Creates a pending workflow task.
    /// </summary>
    private WorkflowTask(Guid workflowInstanceId, string name, string assignedTo)
    {
        WorkflowInstanceId = workflowInstanceId;
        Name = RequireText(name, nameof(name));
        AssignedTo = RequireText(assignedTo, nameof(assignedTo));
        Status = WorkflowTaskStatus.Pending;
    }

    /// <summary>
    /// Gets the owning workflow instance identifier.
    /// </summary>
    public Guid WorkflowInstanceId { get; private set; }

    /// <summary>
    /// Gets the task display name.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the current assignee user identifier.
    /// </summary>
    public string AssignedTo { get; private set; }

    /// <summary>
    /// Gets the current task status.
    /// </summary>
    public WorkflowTaskStatus Status { get; private set; }

    /// <summary>
    /// Gets the user identifier that completed the task.
    /// </summary>
    public string? CompletedBy { get; private set; }

    /// <summary>
    /// Gets the optional completion comment.
    /// </summary>
    public string? Comment { get; private set; }

    /// <summary>
    /// Gets the timestamp when the task was completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>
    /// Creates a pending task assigned to a user.
    /// </summary>
    public static WorkflowTask Create(Guid workflowInstanceId, string name, string assignedTo)
    {
        return new WorkflowTask(workflowInstanceId, name, assignedTo);
    }

    /// <summary>
    /// Assigns the task to another user.
    /// </summary>
    public void AssignTo(string assignee)
    {
        AssignedTo = RequireText(assignee, nameof(assignee));
        Touch();
    }

    /// <summary>
    /// Approves the task and records completion details.
    /// </summary>
    public void Approve(string actor, string? comment)
    {
        Complete(WorkflowTaskStatus.Approved, actor, comment);
    }

    /// <summary>
    /// Rejects the task and records completion details.
    /// </summary>
    public void Reject(string actor, string? comment)
    {
        Complete(WorkflowTaskStatus.Rejected, actor, comment);
    }

    /// <summary>
    /// Completes the task with the requested terminal status.
    /// </summary>
    private void Complete(WorkflowTaskStatus status, string actor, string? comment)
    {
        if (Status != WorkflowTaskStatus.Pending)
        {
            throw new InvalidOperationException("Task is already completed.");
        }

        Status = status;
        CompletedBy = RequireText(actor, nameof(actor));
        Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        CompletedAt = DateTimeOffset.UtcNow;
        Touch();
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
