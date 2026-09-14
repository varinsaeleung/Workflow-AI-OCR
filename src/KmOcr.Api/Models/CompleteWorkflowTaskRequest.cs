namespace KmOcr.Api.Models;

/// <summary>
/// HTTP request body used to approve or reject a workflow task.
/// </summary>
public sealed class CompleteWorkflowTaskRequest
{
    /// <summary>
    /// Gets or sets the actor completing the workflow task.
    /// </summary>
    public string Actor { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional completion comment.
    /// </summary>
    public string? Comment { get; set; }
}
