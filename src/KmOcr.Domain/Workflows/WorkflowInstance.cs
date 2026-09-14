using KmOcr.Domain.Common;

namespace KmOcr.Domain.Workflows;

/// <summary>
/// Aggregate root for a workflow started against a document.
/// </summary>
public sealed class WorkflowInstance : Entity
{
    private readonly List<WorkflowTask> _tasks = [];

    /// <summary>
    /// Creates an empty workflow instance for Entity Framework.
    /// </summary>
    private WorkflowInstance()
    {
        TemplateKey = string.Empty;
        StartedBy = string.Empty;
    }

    /// <summary>
    /// Creates a workflow instance and its first review task.
    /// </summary>
    private WorkflowInstance(Guid documentId, string templateKey, string startedBy)
    {
        DocumentId = documentId;
        TemplateKey = RequireText(templateKey, nameof(templateKey));
        StartedBy = RequireText(startedBy, nameof(startedBy));
        _tasks.Add(WorkflowTask.Create(Id, "Document Review", StartedBy));
    }

    /// <summary>
    /// Gets the related document identifier.
    /// </summary>
    public Guid DocumentId { get; private set; }

    /// <summary>
    /// Gets the workflow template key.
    /// </summary>
    public string TemplateKey { get; private set; }

    /// <summary>
    /// Gets the user identifier that started the workflow.
    /// </summary>
    public string StartedBy { get; private set; }

    /// <summary>
    /// Gets the tasks in this workflow.
    /// </summary>
    public IReadOnlyCollection<WorkflowTask> Tasks => _tasks.AsReadOnly();

    /// <summary>
    /// Starts a workflow for a document.
    /// </summary>
    public static WorkflowInstance Start(Guid documentId, string templateKey, string startedBy)
    {
        return new WorkflowInstance(documentId, templateKey, startedBy);
    }

    /// <summary>
    /// Approves a pending task in the workflow.
    /// </summary>
    public void ApproveTask(Guid taskId, string actor, string? comment)
    {
        FindTask(taskId).Approve(actor, comment);
        Touch();
    }

    /// <summary>
    /// Rejects a pending task in the workflow.
    /// </summary>
    public void RejectTask(Guid taskId, string actor, string? comment)
    {
        FindTask(taskId).Reject(actor, comment);
        Touch();
    }

    /// <summary>
    /// Assigns a pending task to another user.
    /// </summary>
    public void AssignTask(Guid taskId, string assignee)
    {
        FindTask(taskId).AssignTo(assignee);
        Touch();
    }

    /// <summary>
    /// Finds a task by id or throws when it does not exist.
    /// </summary>
    private WorkflowTask FindTask(Guid taskId)
    {
        return _tasks.SingleOrDefault(task => task.Id == taskId)
            ?? throw new InvalidOperationException("Workflow task was not found.");
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
