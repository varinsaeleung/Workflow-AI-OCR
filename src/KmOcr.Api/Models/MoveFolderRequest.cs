namespace KmOcr.Api.Models;

/// <summary>
/// HTTP request body used to move a folder.
/// </summary>
public sealed class MoveFolderRequest
{
    /// <summary>
    /// Gets or sets the target parent folder id; null moves the folder to root.
    /// </summary>
    public Guid? ParentFolderId { get; set; }

    /// <summary>
    /// Gets or sets the actor moving the folder.
    /// </summary>
    public string Actor { get; set; } = string.Empty;
}
