namespace KmOcr.Application.Contracts.Ai;

/// <summary>
/// Abstraction for local AI classification and metadata extraction.
/// </summary>
public interface IAiEngine
{
    /// <summary>
    /// Extracts classification, summary, and entities from text.
    /// </summary>
    Task<AiEngineResult> ExtractAsync(string text, CancellationToken cancellationToken);
}
