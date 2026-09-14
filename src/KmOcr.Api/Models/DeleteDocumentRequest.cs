namespace KmOcr.Api.Models;

/// <summary>
/// HTTP request body used to soft-delete a document.
/// </summary>
public sealed class DeleteDocumentRequest
{
    /// <summary>
    /// Gets or sets the actor deleting the document.
    /// </summary>
    public string Actor { get; set; } = string.Empty;
}
