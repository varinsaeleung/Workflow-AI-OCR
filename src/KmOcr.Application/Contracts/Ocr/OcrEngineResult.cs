namespace KmOcr.Application.Contracts.Ocr;

/// <summary>
/// Represents OCR output returned by an external engine.
/// </summary>
public sealed record OcrEngineResult(
    string Engine,
    string Language,
    string Text,
    decimal ConfidenceScore,
    IReadOnlyList<OcrPageResult> Pages);

/// <summary>
/// Represents OCR output for one page.
/// </summary>
public sealed record OcrPageResult(
    int PageNumber,
    string Text,
    decimal ConfidenceScore,
    IReadOnlyList<OcrWordResult> Words);

/// <summary>
/// Represents OCR output for one detected word or line region.
/// </summary>
public sealed record OcrWordResult(
    string Text,
    decimal X,
    decimal Y,
    decimal Width,
    decimal Height,
    decimal ConfidenceScore);
