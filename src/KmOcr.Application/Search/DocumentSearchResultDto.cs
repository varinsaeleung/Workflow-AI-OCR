namespace KmOcr.Application.Search;

/// <summary>
/// One document search hit returned to API clients.
/// </summary>
public sealed record DocumentSearchResultDto(
    Guid DocumentId,
    string FileName,
    string DocumentType,
    string Status,
    Guid? FolderId,
    DateTimeOffset CreatedAt,
    IReadOnlyDictionary<string, string> Metadata,
    string? OcrText,
    string? AiClassification,
    string? AiSummary,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Highlights);
