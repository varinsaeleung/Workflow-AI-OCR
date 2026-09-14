namespace KmOcr.Application.Ai;

/// <summary>
/// Application module that exposes AI extraction use cases.
/// </summary>
public interface IAiModule
{
    /// <summary>
    /// Runs AI extraction for a document using OCR text.
    /// </summary>
    Task<AiExtractionDto> RunExtractionAsync(Guid documentId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the current AI extraction for a document.
    /// </summary>
    Task<AiExtractionDto> GetExtractionAsync(Guid documentId, CancellationToken cancellationToken);
}
