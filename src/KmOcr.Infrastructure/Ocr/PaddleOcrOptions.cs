namespace KmOcr.Infrastructure.Ocr;

/// <summary>
/// Configuration for the PaddleOCR command adapter.
/// </summary>
public sealed class PaddleOcrOptions
{
    /// <summary>
    /// Gets or sets the Python executable used to run the PaddleOCR script.
    /// </summary>
    public string PythonExecutable { get; set; } = "python3";

    /// <summary>
    /// Gets or sets the PaddleOCR script path.
    /// </summary>
    public string ScriptPath { get; set; } = "Ocr/paddle_ocr_service.py";

    /// <summary>
    /// Gets or sets the OCR language list passed to PaddleOCR.
    /// </summary>
    public string Languages { get; set; } = "tha+eng";

    /// <summary>
    /// Gets or sets the external process timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 120;
}
