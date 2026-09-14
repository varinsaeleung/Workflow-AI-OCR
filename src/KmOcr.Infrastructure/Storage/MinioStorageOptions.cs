namespace KmOcr.Infrastructure.Storage;

/// <summary>
/// Configuration for MinIO object storage.
/// </summary>
public sealed class MinioStorageOptions
{
    /// <summary>
    /// Gets or sets the MinIO endpoint.
    /// </summary>
    public string Endpoint { get; set; } = "localhost:9000";

    /// <summary>
    /// Gets or sets the MinIO access key.
    /// </summary>
    public string AccessKey { get; set; } = "minioadmin";

    /// <summary>
    /// Gets or sets the MinIO secret key.
    /// </summary>
    public string SecretKey { get; set; } = "minioadmin";

    /// <summary>
    /// Gets or sets the bucket for document binaries.
    /// </summary>
    public string BucketName { get; set; } = "documents";

    /// <summary>
    /// Gets or sets whether HTTPS should be used.
    /// </summary>
    public bool UseSsl { get; set; }

    /// <summary>
    /// Gets or sets the default preview URL lifetime in minutes.
    /// </summary>
    public int PreviewUrlMinutes { get; set; } = 10;
}
