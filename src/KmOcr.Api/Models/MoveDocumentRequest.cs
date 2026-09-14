namespace KmOcr.Api.Models;

/// <summary>
/// HTTP request body used to move a document to a folder.
/// </summary>
public sealed class MoveDocumentRequest
{
    /// <summary>
    /// Gets or sets the target folder id; null moves the document to root.
    /// </summary>
    public Guid? FolderId { get; set; }

    /// <summary>
    /// Gets or sets the actor moving the document.
    /// </summary>
    public string Actor { get; set; } = string.Empty;
}
