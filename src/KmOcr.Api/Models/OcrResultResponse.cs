namespace KmOcr.Api.Models;

/// <summary>
/// HTTP response body for OCR result details.
/// </summary>
public sealed record OcrResultResponse(Guid DocumentId, string? Text, decimal? Confidence, string Status);
