using KmOcr.Application.Contracts.Ocr;
using KmOcr.Application.Contracts.Storage;

namespace KmOcr.Worker;

/// <summary>
/// OCR processor that delegates document extraction to the configured enterprise OCR engine.
/// </summary>
public sealed class CliOcrProcessor : IOcrProcessor
{
    private readonly IOcrEngine _ocrEngine;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<CliOcrProcessor> _logger;

    /// <summary>
    /// Creates the processor with the OCR engine and logging.
    /// </summary>
    public CliOcrProcessor(IOcrEngine ocrEngine, IFileStorage fileStorage, ILogger<CliOcrProcessor> logger)
    {
        _ocrEngine = ocrEngine;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    /// <summary>
    /// Extracts text from supported document content types through PaddleOCR.
    /// </summary>
    public async Task<OcrExtractionResult> ExtractAsync(string storagePath, string contentType, CancellationToken cancellationToken)
    {
        if (IsPaddleSupported(contentType))
        {
            return await ExtractPaddleAsync(storagePath, contentType, cancellationToken);
        }

        _logger.LogWarning("Content type {ContentType} is not OCR-supported by PaddleOCR worker.", contentType);
        return new OcrExtractionResult($"Unsupported OCR content type: {contentType}", 0.1m, "unsupported");
    }

    /// <summary>
    /// Returns true when the content type is supported by the PaddleOCR adapter.
    /// </summary>
    private static bool IsPaddleSupported(string contentType)
    {
        return contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("image/jpg", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("image/png", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Downloads storage content to a temporary file when needed and runs PaddleOCR.
    /// </summary>
    private async Task<OcrExtractionResult> ExtractPaddleAsync(string storagePath, string contentType, CancellationToken cancellationToken)
    {
        var localPath = File.Exists(storagePath)
            ? storagePath
            : await DownloadToTemporaryFileAsync(storagePath, contentType, cancellationToken);

        try
        {
            var result = await _ocrEngine.ExtractAsync(localPath, contentType, cancellationToken);
            return new OcrExtractionResult(result.Text, result.ConfidenceScore, result.Engine);
        }
        finally
        {
            DeleteTemporaryFile(storagePath, localPath);
        }
    }

    /// <summary>
    /// Opens an object from storage and writes it to a local temporary path for OCR engines.
    /// </summary>
    private async Task<string> DownloadToTemporaryFileAsync(string storagePath, string contentType, CancellationToken cancellationToken)
    {
        var fileName = Path.GetFileName(storagePath);
        var extension = Path.GetExtension(fileName);
        var temporaryPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{extension}");
        var storedFile = await _fileStorage.OpenReadAsync(storagePath, fileName, contentType, cancellationToken);
        await using var input = storedFile.Content;
        await using var output = File.Create(temporaryPath);
        await input.CopyToAsync(output, cancellationToken);
        return temporaryPath;
    }

    /// <summary>
    /// Deletes temporary files created for remote storage objects.
    /// </summary>
    private static void DeleteTemporaryFile(string originalPath, string localPath)
    {
        if (!originalPath.Equals(localPath, StringComparison.OrdinalIgnoreCase) && File.Exists(localPath))
        {
            File.Delete(localPath);
        }
    }
}
