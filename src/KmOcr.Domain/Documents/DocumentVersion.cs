using KmOcr.Domain.Common;

namespace KmOcr.Domain.Documents;

/// <summary>
/// Immutable binary version for a document.
/// </summary>
public sealed class DocumentVersion : Entity
{
    /// <summary>
    /// Creates an empty version for Entity Framework.
    /// </summary>
    private DocumentVersion()
    {
        FileName = string.Empty;
        ContentType = string.Empty;
        StoragePath = string.Empty;
        CreatedBy = string.Empty;
    }

    /// <summary>
    /// Creates an immutable document version.
    /// </summary>
    internal DocumentVersion(Guid documentId, int versionNumber, string fileName, string contentType, string storagePath, long fileSizeBytes, string? checksumSha256, string createdBy)
    {
        DocumentId = documentId;
        VersionNumber = versionNumber > 0 ? versionNumber : throw new ArgumentOutOfRangeException(nameof(versionNumber), "Version number must be positive.");
        FileName = RequireText(fileName, nameof(fileName));
        ContentType = RequireText(contentType, nameof(contentType));
        StoragePath = RequireText(storagePath, nameof(storagePath));
        FileSizeBytes = fileSizeBytes > 0 ? fileSizeBytes : throw new ArgumentOutOfRangeException(nameof(fileSizeBytes), "File size must be positive.");
        ChecksumSha256 = string.IsNullOrWhiteSpace(checksumSha256) ? null : checksumSha256.Trim();
        CreatedBy = RequireText(createdBy, nameof(createdBy));
    }

    /// <summary>
    /// Gets the owning document id.
    /// </summary>
    public Guid DocumentId { get; private set; }

    /// <summary>
    /// Gets the version number within the document.
    /// </summary>
    public int VersionNumber { get; private set; }

    /// <summary>
    /// Gets the file name for this version.
    /// </summary>
    public string FileName { get; private set; }

    /// <summary>
    /// Gets the content type for this version.
    /// </summary>
    public string ContentType { get; private set; }

    /// <summary>
    /// Gets the MinIO object path for this version.
    /// </summary>
    public string StoragePath { get; private set; }

    /// <summary>
    /// Gets the file size in bytes.
    /// </summary>
    public long FileSizeBytes { get; private set; }

    /// <summary>
    /// Gets the optional SHA-256 checksum.
    /// </summary>
    public string? ChecksumSha256 { get; private set; }

    /// <summary>
    /// Gets the user that created this version.
    /// </summary>
    public string CreatedBy { get; private set; }

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
