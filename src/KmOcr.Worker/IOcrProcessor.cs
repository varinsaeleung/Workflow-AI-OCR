namespace KmOcr.Worker;

/// <summary>
/// Abstraction for extracting text from a stored document.
/// </summary>
public interface IOcrProcessor
{
    /// <summary>
    /// Extracts text from a stored document path.
    /// </summary>
    Task<OcrExtractionResult> ExtractAsync(string storagePath, string contentType, CancellationToken cancellationToken);
}
