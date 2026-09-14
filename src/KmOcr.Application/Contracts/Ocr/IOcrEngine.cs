namespace KmOcr.Application.Contracts.Ocr;

/// <summary>
/// Defines an infrastructure OCR engine adapter.
/// </summary>
public interface IOcrEngine
{
    /// <summary>
    /// Extracts text from a stored file path and returns structured OCR JSON data.
    /// </summary>
    Task<OcrEngineResult> ExtractAsync(string filePath, string contentType, CancellationToken cancellationToken);
}
