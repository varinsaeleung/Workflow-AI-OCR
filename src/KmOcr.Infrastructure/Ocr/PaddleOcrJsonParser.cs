using System.Text.Json;
using KmOcr.Application.Contracts.Ocr;

namespace KmOcr.Infrastructure.Ocr;

/// <summary>
/// Parses JSON emitted by the PaddleOCR Python adapter.
/// </summary>
public static class PaddleOcrJsonParser
{
    /// <summary>
    /// Converts PaddleOCR adapter JSON into the application OCR result contract.
    /// </summary>
    public static OcrEngineResult Parse(string json)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<PaddleOcrPayload>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new InvalidOperationException("PaddleOCR JSON payload is empty.");

            var pages = payload.Pages
                .Select(page => new OcrPageResult(
                    page.PageNumber,
                    page.Text,
                    page.ConfidenceScore,
                    page.Words.Select(word => new OcrWordResult(
                        word.Text,
                        word.X,
                        word.Y,
                        word.Width,
                        word.Height,
                        word.ConfidenceScore)).ToList()))
                .ToList();

            var text = string.Join('\n', pages.Select(page => page.Text).Where(value => !string.IsNullOrWhiteSpace(value)));
            var confidence = pages.Count == 0 ? 0m : Math.Round(pages.Average(page => page.ConfidenceScore), 4);
            return new OcrEngineResult(payload.Engine, payload.Language, text, confidence, pages);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException($"PaddleOCR JSON could not be parsed: {exception.Message}", exception);
        }
    }

    private sealed record PaddleOcrPayload(string Engine, string Language, IReadOnlyList<PaddleOcrPage> Pages);

    private sealed record PaddleOcrPage(int PageNumber, string Text, decimal ConfidenceScore, IReadOnlyList<PaddleOcrWord> Words);

    private sealed record PaddleOcrWord(string Text, decimal X, decimal Y, decimal Width, decimal Height, decimal ConfidenceScore);
}
