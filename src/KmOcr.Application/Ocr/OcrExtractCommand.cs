namespace KmOcr.Application.Ocr;

/// <summary>
/// Carries an uploaded file into the OCR use case.
/// </summary>
public sealed record OcrExtractCommand(string FileName, string ContentType, Stream Content);
