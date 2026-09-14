namespace KmOcr.Application.Contracts.Storage;

/// <summary>
/// Abstraction for saving uploaded document binaries.
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Saves file content and returns storage object metadata.
    /// </summary>
    Task<StoredObject> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken);

    /// <summary>
    /// Opens a stored object for download.
    /// </summary>
    Task<StoredFile> OpenReadAsync(string storagePath, string fileName, string contentType, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a preview URL for a stored object.
    /// </summary>
    Task<string> GetPreviewUrlAsync(string storagePath, TimeSpan expiresIn, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a stored object when retention policy allows physical deletion.
    /// </summary>
    Task DeleteAsync(string storagePath, CancellationToken cancellationToken);
}

/// <summary>
/// Metadata returned after storing an object.
/// </summary>
public sealed record StoredObject(string BucketName, string StoragePath, long SizeBytes, string? ChecksumSha256);

/// <summary>
/// Downloadable stored file stream and metadata.
/// </summary>
public sealed record StoredFile(string FileName, string ContentType, Stream Content);
