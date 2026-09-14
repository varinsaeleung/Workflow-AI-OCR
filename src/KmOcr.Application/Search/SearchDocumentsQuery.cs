namespace KmOcr.Application.Search;

/// <summary>
/// Query criteria for document search across document fields, OCR text, and metadata.
/// </summary>
public sealed record SearchDocumentsQuery(
    string? Query,
    string? DocumentType,
    string? Status,
    Guid? FolderId,
    string? MetadataKey,
    string? MetadataValue,
    int Size = 20,
    int From = 0);
