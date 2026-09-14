using KmOcr.Domain.Common;

namespace KmOcr.Domain.Documents;

/// <summary>
/// Stores OCR output and confidence for a processed document.
/// </summary>
public sealed class OcrResult : Entity
{
    /// <summary>
    /// Creates an empty OCR result for Entity Framework.
    /// </summary>
    private OcrResult()
    {
        ExtractedText = string.Empty;
        Engine = string.Empty;
    }

    /// <summary>
    /// Creates a completed OCR result.
    /// </summary>
    private OcrResult(Guid documentId, string extractedText, decimal confidenceScore, string engine)
    {
        DocumentId = documentId;
        ExtractedText = RequireText(extractedText, nameof(extractedText));
        ConfidenceScore = confidenceScore;
        Engine = RequireText(engine, nameof(engine));
    }

    /// <summary>
    /// Gets the owning document identifier.
    /// </summary>
    public Guid DocumentId { get; private set; }

    /// <summary>
    /// Gets the raw text extracted by the OCR engine.
    /// </summary>
    public string ExtractedText { get; private set; }

    /// <summary>
    /// Gets the OCR engine confidence score between 0 and 1.
    /// </summary>
    public decimal ConfidenceScore { get; private set; }

    /// <summary>
    /// Gets the OCR engine name.
    /// </summary>
    public string Engine { get; private set; }

    /// <summary>
    /// Creates a validated OCR result.
    /// </summary>
    public static OcrResult Create(Guid documentId, string extractedText, decimal confidenceScore, string engine)
    {
        ValidateConfidence(confidenceScore);
        return new OcrResult(documentId, extractedText, confidenceScore, engine);
    }

    /// <summary>
    /// Replaces OCR output for a reprocessed document.
    /// </summary>
    public void Update(string extractedText, decimal confidenceScore, string engine)
    {
        ValidateConfidence(confidenceScore);
        ExtractedText = RequireText(extractedText, nameof(extractedText));
        ConfidenceScore = confidenceScore;
        Engine = RequireText(engine, nameof(engine));
        Touch();
    }

    /// <summary>
    /// Validates that OCR confidence is in the supported range.
    /// </summary>
    private static void ValidateConfidence(decimal confidenceScore)
    {
        if (confidenceScore is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(confidenceScore), "Confidence score must be between 0 and 1.");
        }
    }

    /// <summary>
    /// Validates required text and returns the trimmed value.
    /// </summary>
    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }
}
