namespace KmOcr.Api.Models;

/// <summary>
/// HTTP request body used to assign a workflow task.
/// </summary>
public sealed class AssignWorkflowTaskRequest
{
    /// <summary>
    /// Gets or sets the next assignee user identifier.
    /// </summary>
    public string Assignee { get; set; } = string.Empty;
}
