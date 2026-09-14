using System.Text.Json;
using KmOcr.Application.Search;

namespace KmOcr.Infrastructure.Search;

/// <summary>
/// Maps application document index DTOs into OpenSearch JSON documents.
/// </summary>
public static class OpenSearchDocumentMapper
{
    /// <summary>
    /// Serializes one document into the JSON shape stored in OpenSearch.
    /// </summary>
    public static string ToIndexJson(DocumentIndexDto document)
    {
        var payload = new Dictionary<string, object?>
        {
            ["id"] = document.Id.ToString(),
            ["fileName"] = document.FileName,
            ["documentType"] = document.DocumentType,
            ["status"] = document.Status,
            ["folderId"] = document.FolderId?.ToString(),
            ["uploadedBy"] = document.UploadedBy,
            ["createdAt"] = document.CreatedAt,
            ["metadata"] = document.Metadata,
            ["ocrText"] = document.OcrText,
            ["ocrConfidence"] = document.OcrConfidence,
            ["aiClassification"] = document.AiClassification,
            ["aiSummary"] = document.AiSummary,
            ["isDeleted"] = document.IsDeleted
        };

        return JsonSerializer.Serialize(payload);
    }
}
