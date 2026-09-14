using System.Text.RegularExpressions;
using KmOcr.Application.Contracts.Ai;

namespace KmOcr.Infrastructure.Ai;

/// <summary>
/// Open-source starter AI engine that classifies and extracts metadata with deterministic rules.
/// </summary>
public sealed partial class KeywordAiEngine : IAiEngine
{
    /// <summary>
    /// Extracts classification, summary, and basic entities from OCR text.
    /// </summary>
    public Task<AiEngineResult> ExtractAsync(string text, CancellationToken cancellationToken)
    {
        var classification = Classify(text);
        var entities = ExtractEntities(text);
        var summary = Summarize(text, classification);
        var confidence = classification == "unknown" ? 0.35m : 0.78m;
        return Task.FromResult(new AiEngineResult(classification, summary, entities, confidence, "keyword-ai"));
    }

    /// <summary>
    /// Classifies the document from common enterprise keywords.
    /// </summary>
    private static string Classify(string text)
    {
        var normalized = text.ToLowerInvariant();

        if (normalized.Contains("invoice") || normalized.Contains("tax invoice"))
        {
            return "invoice";
        }

        if (normalized.Contains("agreement") || normalized.Contains("contract"))
        {
            return "contract";
        }

        if (normalized.Contains("receipt"))
        {
            return "receipt";
        }

        return "unknown";
    }

    /// <summary>
    /// Extracts simple enterprise entities from OCR text.
    /// </summary>
    private static IReadOnlyDictionary<string, string> ExtractEntities(string text)
    {
        var entities = new Dictionary<string, string>();
        var amountMatch = AmountRegex().Match(text);

        if (amountMatch.Success)
        {
            entities["amount"] = amountMatch.Value;
        }

        var emailMatch = EmailRegex().Match(text);

        if (emailMatch.Success)
        {
            entities["email"] = emailMatch.Value;
        }

        return entities;
    }

    /// <summary>
    /// Creates a compact summary from the first useful OCR text segment.
    /// </summary>
    private static string Summarize(string text, string classification)
    {
        var cleanText = text.ReplaceLineEndings(" ").Trim();

        if (cleanText.Length > 180)
        {
            cleanText = cleanText[..180];
        }

        return $"{classification}: {cleanText}";
    }

    /// <summary>
    /// Provides a compiled regex for common currency amounts.
    /// </summary>
    [GeneratedRegex(@"(?:THB|USD|EUR|\$)?\s?\d{1,3}(?:,\d{3})*(?:\.\d{2})?", RegexOptions.IgnoreCase)]
    private static partial Regex AmountRegex();

    /// <summary>
    /// Provides a compiled regex for email extraction.
    /// </summary>
    [GeneratedRegex(@"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();
}
