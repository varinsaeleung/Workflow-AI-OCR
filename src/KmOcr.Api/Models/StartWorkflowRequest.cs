namespace KmOcr.Api.Models;

/// <summary>
/// HTTP request body used to start a document workflow.
/// </summary>
public sealed class StartWorkflowRequest
{
    /// <summary>
    /// Gets or sets the document identifier.
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// Gets or sets the workflow template key.
    /// </summary>
    public string TemplateKey { get; set; } = "default-review";

    /// <summary>
    /// Gets or sets the user starting the workflow.
    /// </summary>
    public string StartedBy { get; set; } = string.Empty;
}
