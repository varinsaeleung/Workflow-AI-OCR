namespace KmOcr.Application.Documents;

/// <summary>
/// Client-safe document representation returned by APIs.
/// </summary>
public sealed record DocumentDto(
    Guid Id,
    string FileName,
    string ContentType,
    string DocumentType,
    string Status,
    string UploadedBy,
    string StoragePath,
    Guid? FolderId,
    string? StorageBucket,
    long? FileSizeBytes,
    int CurrentVersionNumber,
    DateTimeOffset? DeletedAt,
    DateTimeOffset CreatedAt,
    IReadOnlyDictionary<string, string> Metadata,
    string? OcrText,
    decimal? OcrConfidence,
    string? AiClassification,
    string? AiSummary);
