using KmOcr.Application.Contracts.Storage;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace KmOcr.Infrastructure.Storage;

/// <summary>
/// Stores uploaded files on a mounted Linux-compatible filesystem path.
/// </summary>
public sealed class LocalFileStorage : IFileStorage
{
    private readonly LocalFileStorageOptions _options;

    /// <summary>
    /// Creates local storage with configured options.
    /// </summary>
    public LocalFileStorage(IOptions<LocalFileStorageOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// Saves file bytes to a date-partitioned path and returns that path.
    /// </summary>
    public async Task<StoredObject> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken)
    {
        var safeFileName = Path.GetFileName(fileName);
        var datePath = DateTimeOffset.UtcNow.ToString("yyyy/MM/dd");
        var directory = Path.Combine(_options.RootPath, datePath);
        Directory.CreateDirectory(directory);

        var targetPath = Path.Combine(directory, $"{Guid.NewGuid():N}-{safeFileName}");
        await using var output = File.Create(targetPath);
        await content.CopyToAsync(output, cancellationToken);
        var fileInfo = new FileInfo(targetPath);
        return new StoredObject("local", targetPath, fileInfo.Length, await ComputeChecksumAsync(targetPath, cancellationToken));
    }

    /// <summary>
    /// Opens a local file for download.
    /// </summary>
    public Task<StoredFile> OpenReadAsync(string storagePath, string fileName, string contentType, CancellationToken cancellationToken)
    {
        Stream stream = File.OpenRead(storagePath);
        return Task.FromResult(new StoredFile(fileName, contentType, stream));
    }

    /// <summary>
    /// Returns a local preview path for development usage.
    /// </summary>
    public Task<string> GetPreviewUrlAsync(string storagePath, TimeSpan expiresIn, CancellationToken cancellationToken)
    {
        return Task.FromResult(storagePath);
    }

    /// <summary>
    /// Deletes a local file when physical deletion is requested.
    /// </summary>
    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken)
    {
        if (File.Exists(storagePath))
        {
            File.Delete(storagePath);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Computes the SHA-256 checksum for a stored file.
    /// </summary>
    private static async Task<string> ComputeChecksumAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
