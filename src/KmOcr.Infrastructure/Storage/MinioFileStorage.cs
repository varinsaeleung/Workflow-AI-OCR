using System.Security.Cryptography;
using KmOcr.Application.Contracts.Storage;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace KmOcr.Infrastructure.Storage;

/// <summary>
/// Stores document binaries in MinIO object storage.
/// </summary>
public sealed class MinioFileStorage : IFileStorage
{
    private readonly IMinioClient _client;
    private readonly MinioStorageOptions _options;

    /// <summary>
    /// Creates MinIO storage with configured options.
    /// </summary>
    public MinioFileStorage(IOptions<MinioStorageOptions> options)
    {
        _options = options.Value;
        _client = new MinioClient()
            .WithEndpoint(_options.Endpoint)
            .WithCredentials(_options.AccessKey, _options.SecretKey)
            .WithSSL(_options.UseSsl)
            .Build();
    }

    /// <summary>
    /// Saves file content to MinIO and returns object metadata.
    /// </summary>
    public async Task<StoredObject> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken)
    {
        await EnsureBucketAsync(cancellationToken);
        var safeFileName = Path.GetFileName(fileName);
        var objectName = $"{DateTimeOffset.UtcNow:yyyy/MM/dd}/{Guid.NewGuid():N}-{safeFileName}";
        await using var buffered = new MemoryStream();
        await content.CopyToAsync(buffered, cancellationToken);
        buffered.Position = 0;
        var checksum = Convert.ToHexString(SHA256.HashData(buffered)).ToLowerInvariant();
        buffered.Position = 0;

        var putArgs = new PutObjectArgs()
            .WithBucket(_options.BucketName)
            .WithObject(objectName)
            .WithStreamData(buffered)
            .WithObjectSize(buffered.Length)
            .WithContentType(contentType);
        await _client.PutObjectAsync(putArgs, cancellationToken);
        return new StoredObject(_options.BucketName, BuildStoragePath(objectName), buffered.Length, checksum);
    }

    /// <summary>
    /// Opens a MinIO object for download.
    /// </summary>
    public async Task<StoredFile> OpenReadAsync(string storagePath, string fileName, string contentType, CancellationToken cancellationToken)
    {
        var objectName = ParseObjectName(storagePath);
        var memory = new MemoryStream();
        var getArgs = new GetObjectArgs()
            .WithBucket(_options.BucketName)
            .WithObject(objectName)
            .WithCallbackStream(stream => stream.CopyTo(memory));
        await _client.GetObjectAsync(getArgs, cancellationToken);
        memory.Position = 0;
        return new StoredFile(fileName, contentType, memory);
    }

    /// <summary>
    /// Creates a presigned URL for previewing a MinIO object.
    /// </summary>
    public Task<string> GetPreviewUrlAsync(string storagePath, TimeSpan expiresIn, CancellationToken cancellationToken)
    {
        var objectName = ParseObjectName(storagePath);
        var seconds = Math.Max(1, (int)expiresIn.TotalSeconds);
        var args = new PresignedGetObjectArgs()
            .WithBucket(_options.BucketName)
            .WithObject(objectName)
            .WithExpiry(seconds);
        return _client.PresignedGetObjectAsync(args);
    }

    /// <summary>
    /// Deletes a MinIO object when physical deletion is explicitly requested.
    /// </summary>
    public async Task DeleteAsync(string storagePath, CancellationToken cancellationToken)
    {
        var objectName = ParseObjectName(storagePath);
        var args = new RemoveObjectArgs()
            .WithBucket(_options.BucketName)
            .WithObject(objectName);
        await _client.RemoveObjectAsync(args, cancellationToken);
    }

    /// <summary>
    /// Ensures the configured bucket exists.
    /// </summary>
    private async Task EnsureBucketAsync(CancellationToken cancellationToken)
    {
        var existsArgs = new BucketExistsArgs().WithBucket(_options.BucketName);
        if (!await _client.BucketExistsAsync(existsArgs, cancellationToken))
        {
            var makeArgs = new MakeBucketArgs().WithBucket(_options.BucketName);
            await _client.MakeBucketAsync(makeArgs, cancellationToken);
        }
    }

    /// <summary>
    /// Builds a stable storage URI for a MinIO object.
    /// </summary>
    private string BuildStoragePath(string objectName)
    {
        return $"minio://{_options.BucketName}/{objectName}";
    }

    /// <summary>
    /// Extracts the object name from a MinIO storage URI.
    /// </summary>
    private string ParseObjectName(string storagePath)
    {
        var prefix = $"minio://{_options.BucketName}/";
        if (!storagePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Storage path does not belong to bucket '{_options.BucketName}'.");
        }

        return storagePath[prefix.Length..];
    }
}
