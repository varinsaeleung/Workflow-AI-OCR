namespace KmOcr.Api.Models;

/// <summary>
/// HTTP request body used to create a document folder.
/// </summary>
public sealed class CreateFolderRequest
{
    /// <summary>
    /// Gets or sets the folder name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parent folder id; null creates a root folder.
    /// </summary>
    public Guid? ParentFolderId { get; set; }

    /// <summary>
    /// Gets or sets the actor creating the folder.
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
}
