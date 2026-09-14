namespace KmOcr.Application.Ocr;

/// <summary>
/// JSON-safe OCR result returned to API clients.
/// </summary>
public sealed record OcrResultDto(
    string Engine,
    string Language,
    string Text,
    decimal ConfidenceScore,
    IReadOnlyList<OcrPageDto> Pages);

/// <summary>
/// JSON-safe OCR page result returned to API clients.
/// </summary>
public sealed record OcrPageDto(
    int PageNumber,
    string Text,
    decimal ConfidenceScore,
    IReadOnlyList<OcrWordDto> Words);

/// <summary>
/// JSON-safe OCR word bounding box returned to API clients.
/// </summary>
public sealed record OcrWordDto(
    string Text,
    decimal X,
    decimal Y,
    decimal Width,
    decimal Height,
    decimal ConfidenceScore);
