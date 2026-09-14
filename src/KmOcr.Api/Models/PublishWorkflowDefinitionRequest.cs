namespace KmOcr.Api.Models;

/// <summary>
/// HTTP request body used to publish a workflow definition.
/// </summary>
public sealed class PublishWorkflowDefinitionRequest
{
    /// <summary>
    /// Gets or sets the user publishing the workflow definition.
    /// </summary>
    public string PublishedBy { get; set; } = string.Empty;
}
