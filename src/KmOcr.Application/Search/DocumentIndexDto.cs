namespace KmOcr.Application.Search;

/// <summary>
/// Search index representation of one document aggregate.
/// </summary>
public sealed record DocumentIndexDto(
    Guid Id,
    string FileName,
    string DocumentType,
    string Status,
    Guid? FolderId,
    string UploadedBy,
    DateTimeOffset CreatedAt,
    IReadOnlyDictionary<string, string> Metadata,
    string? OcrText,
    decimal? OcrConfidence,
    string? AiClassification,
    string? AiSummary,
    bool IsDeleted);
