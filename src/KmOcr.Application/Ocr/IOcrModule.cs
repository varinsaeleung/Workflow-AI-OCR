namespace KmOcr.Application.Ocr;

/// <summary>
/// Defines OCR use cases exposed to API and background jobs.
/// </summary>
public interface IOcrModule
{
    /// <summary>
    /// Extracts OCR JSON from an uploaded file.
    /// </summary>
    Task<OcrResultDto> ExtractAsync(OcrExtractCommand command, CancellationToken cancellationToken);
}
