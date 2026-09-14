namespace KmOcr.Api.Models;

/// <summary>
/// HTTP request body used to create a node workflow definition.
/// </summary>
public sealed class CreateWorkflowDefinitionRequest
{
    /// <summary>
    /// Gets or sets the unique workflow code.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the workflow display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user creating the workflow definition.
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
}
