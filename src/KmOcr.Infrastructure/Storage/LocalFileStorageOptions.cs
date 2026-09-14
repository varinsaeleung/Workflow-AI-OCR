namespace KmOcr.Infrastructure.Storage;

/// <summary>
/// Options for filesystem-backed document storage.
/// </summary>
public sealed class LocalFileStorageOptions
{
    /// <summary>
    /// Gets or sets the root directory where uploaded files are stored.
    /// </summary>
    public string RootPath { get; set; } = "uploads";
}
