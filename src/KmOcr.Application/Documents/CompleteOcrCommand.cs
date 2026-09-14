namespace KmOcr.Application.Documents;

/// <summary>
/// Command data required to persist an OCR result.
/// </summary>
public sealed record CompleteOcrCommand(Guid DocumentId, string ExtractedText, decimal ConfidenceScore, string Engine);
