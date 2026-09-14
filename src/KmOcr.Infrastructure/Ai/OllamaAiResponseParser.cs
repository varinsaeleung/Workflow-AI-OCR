using System.Text.Json;
using KmOcr.Application.Contracts.Ai;

namespace KmOcr.Infrastructure.Ai;

/// <summary>
/// Parses JSON returned by Ollama into the application AI engine contract.
/// </summary>
public static class OllamaAiResponseParser
{
    /// <summary>
    /// Converts an Ollama generate response into a normalized AI engine result.
    /// </summary>
    public static AiEngineResult Parse(string ollamaResponseJson, string model)
    {
        using var outerDocument = JsonDocument.Parse(ollamaResponseJson);
        var responseText = outerDocument.RootElement.GetProperty("response").GetString();

        if (string.IsNullOrWhiteSpace(responseText))
        {
            throw new InvalidOperationException("Ollama response did not include generated content.");
        }

        using var innerDocument = JsonDocument.Parse(responseText);
        var root = innerDocument.RootElement;
        var classification = ReadRequiredString(root, "classification", "Ollama response did not include a classification.");
        var summary = ReadRequiredString(root, "summary", "Ollama response did not include a summary.");
        var metadata = ReadMetadata(root);
        var confidence = ReadConfidence(root);
        return new AiEngineResult(classification, summary, metadata, confidence, $"ollama:{model}");
    }

    /// <summary>
    /// Reads a required string property from the AI JSON payload.
    /// </summary>
    private static string ReadRequiredString(JsonElement root, string propertyName, string errorMessage)
    {
        if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException(errorMessage);
        }

        var value = property.GetString();

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(errorMessage);
        }

        return value;
    }

    /// <summary>
    /// Reads metadata key-value pairs from the AI JSON payload.
    /// </summary>
    private static IReadOnlyDictionary<string, string> ReadMetadata(JsonElement root)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!root.TryGetProperty("metadata", out var metadataElement) || metadataElement.ValueKind != JsonValueKind.Object)
        {
            return metadata;
        }

        foreach (var property in metadataElement.EnumerateObject())
        {
            metadata[property.Name] = property.Value.ValueKind == JsonValueKind.String
                ? property.Value.GetString() ?? string.Empty
                : property.Value.GetRawText();
        }

        return metadata;
    }

    /// <summary>
    /// Reads confidence from the AI JSON payload and falls back to a conservative value.
    /// </summary>
    private static decimal ReadConfidence(JsonElement root)
    {
        if (!root.TryGetProperty("confidence", out var confidenceElement) || confidenceElement.ValueKind != JsonValueKind.Number)
        {
            return 0.5m;
        }

        return confidenceElement.GetDecimal();
    }
}
