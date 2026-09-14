namespace KmOcr.Worker;

/// <summary>
/// Represents OCR text and confidence returned by a worker processor.
/// </summary>
public sealed record OcrExtractionResult(string Text, decimal ConfidenceScore, string Engine);
