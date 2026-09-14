using KmOcr.Application.Common;
using KmOcr.Application.Contracts.Ocr;

namespace KmOcr.Application.Ocr;

/// <summary>
/// Implements OCR use cases and keeps file validation outside infrastructure.
/// </summary>
public sealed class OcrModule : IOcrModule
{
    private static readonly HashSet<string> SupportedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/jpg",
        "image/png"
    };

    private readonly IOcrEngine _ocrEngine;

    /// <summary>
    /// Creates the OCR module with the selected OCR engine.
    /// </summary>
    public OcrModule(IOcrEngine ocrEngine)
    {
        _ocrEngine = ocrEngine;
    }

    /// <summary>
    /// Validates the upload, writes it to a temporary file, and extracts OCR JSON.
    /// </summary>
    public async Task<OcrResultDto> ExtractAsync(OcrExtractCommand command, CancellationToken cancellationToken)
    {
        Validate(command);
        var extension = Path.GetExtension(command.FileName);
        var temporaryPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{extension}");

        try
        {
            await using (var fileStream = File.Create(temporaryPath))
            {
                await command.Content.CopyToAsync(fileStream, cancellationToken);
            }

            var result = await _ocrEngine.ExtractAsync(temporaryPath, command.ContentType, cancellationToken);
            return Map(result);
        }
        finally
        {
            DeleteTemporaryFile(temporaryPath);
        }
    }

    /// <summary>
    /// Validates OCR upload input before writing any temporary file.
    /// </summary>
    private static void Validate(OcrExtractCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.FileName))
        {
            throw new ValidationException("File name is required.");
        }

        if (!SupportedContentTypes.Contains(command.ContentType))
        {
            throw new ValidationException($"Unsupported OCR content type: {command.ContentType}.");
        }

        if (command.Content.CanSeek && command.Content.Length == 0)
        {
            throw new ValidationException("File content is required.");
        }
    }

    /// <summary>
    /// Deletes the temporary OCR input file when it exists.
    /// </summary>
    private static void DeleteTemporaryFile(string temporaryPath)
    {
        if (File.Exists(temporaryPath))
        {
            File.Delete(temporaryPath);
        }
    }

    /// <summary>
    /// Maps engine output into the public OCR DTO.
    /// </summary>
    private static OcrResultDto Map(OcrEngineResult result)
    {
        return new OcrResultDto(
            result.Engine,
            result.Language,
            result.Text,
            result.ConfidenceScore,
            result.Pages.Select(page => new OcrPageDto(
                page.PageNumber,
                page.Text,
                page.ConfidenceScore,
                page.Words.Select(word => new OcrWordDto(
                    word.Text,
                    word.X,
                    word.Y,
                    word.Width,
                    word.Height,
                    word.ConfidenceScore)).ToList())).ToList());
    }
}
