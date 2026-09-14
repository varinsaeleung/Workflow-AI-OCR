using KmOcr.Domain.Common;

namespace KmOcr.Domain.Documents;

/// <summary>
/// Folder aggregate used to organize enterprise documents.
/// </summary>
public sealed class DocumentFolder : Entity
{
    /// <summary>
    /// Creates an empty folder for Entity Framework.
    /// </summary>
    private DocumentFolder()
    {
        Name = string.Empty;
        Path = string.Empty;
        CreatedBy = string.Empty;
    }

    /// <summary>
    /// Creates a folder with normalized parent and path values.
    /// </summary>
    private DocumentFolder(string name, Guid? parentFolderId, string parentPath, string createdBy)
    {
        Name = RequireText(name, nameof(name));
        ParentFolderId = parentFolderId;
        Path = BuildPath(parentPath, Name);
        CreatedBy = RequireText(createdBy, nameof(createdBy));
    }

    /// <summary>
    /// Gets the optional parent folder id.
    /// </summary>
    public Guid? ParentFolderId { get; private set; }

    /// <summary>
    /// Gets the folder display name.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the materialized folder path.
    /// </summary>
    public string Path { get; private set; }

    /// <summary>
    /// Gets the user that created the folder.
    /// </summary>
    public string CreatedBy { get; private set; }

    /// <summary>
    /// Gets the timestamp when the folder was soft-deleted.
    /// </summary>
    public DateTimeOffset? DeletedAt { get; private set; }

    /// <summary>
    /// Creates a root folder.
    /// </summary>
    public static DocumentFolder CreateRoot(string name, string createdBy)
    {
        return new DocumentFolder(name, null, "/", createdBy);
    }

    /// <summary>
    /// Creates a child folder under a parent path.
    /// </summary>
    public static DocumentFolder CreateChild(string name, Guid parentFolderId, string parentPath, string createdBy)
    {
        return new DocumentFolder(name, parentFolderId, parentPath, createdBy);
    }

    /// <summary>
    /// Moves the folder under another parent path.
    /// </summary>
    public void MoveTo(Guid? parentFolderId, string parentPath)
    {
        ParentFolderId = parentFolderId;
        Path = BuildPath(parentPath, Name);
        Touch();
    }

    /// <summary>
    /// Renames the folder and rebuilds its path under the same parent path.
    /// </summary>
    public void Rename(string name, string parentPath)
    {
        Name = RequireText(name, nameof(name));
        Path = BuildPath(parentPath, Name);
        Touch();
    }

    /// <summary>
    /// Soft-deletes the folder.
    /// </summary>
    public void SoftDelete()
    {
        DeletedAt = DateTimeOffset.UtcNow;
        Touch();
    }

    /// <summary>
    /// Builds a normalized materialized path.
    /// </summary>
    private static string BuildPath(string parentPath, string name)
    {
        var cleanParent = string.IsNullOrWhiteSpace(parentPath) ? "/" : parentPath.Trim().TrimEnd('/');
        return cleanParent == "/" ? $"/{name}" : $"{cleanParent}/{name}";
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
