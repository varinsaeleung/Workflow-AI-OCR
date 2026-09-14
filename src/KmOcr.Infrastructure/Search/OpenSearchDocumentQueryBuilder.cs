using System.Text.Json;
using System.Text.RegularExpressions;
using KmOcr.Application.Search;

namespace KmOcr.Infrastructure.Search;

/// <summary>
/// Builds OpenSearch JSON requests for document search.
/// </summary>
public static class OpenSearchDocumentQueryBuilder
{
    private static readonly Regex MetadataKeyPattern = new("^[A-Za-z0-9_-]{1,64}$", RegexOptions.Compiled);

    /// <summary>
    /// Builds a search request with full-text fields, exact filters, paging, and highlights.
    /// </summary>
    public static string BuildSearchRequest(SearchDocumentsQuery query)
    {
        ValidateMetadataKey(query.MetadataKey);

        var filters = BuildFilters(query);
        var request = new Dictionary<string, object?>
        {
            ["from"] = Math.Max(query.From, 0),
            ["size"] = Math.Clamp(query.Size, 1, 100),
            ["query"] = new Dictionary<string, object?>
            {
                ["bool"] = new Dictionary<string, object?>
                {
                    ["must"] = BuildMustClauses(query),
                    ["filter"] = filters
                }
            },
            ["highlight"] = BuildHighlight(query)
        };

        return JsonSerializer.Serialize(request);
    }

    /// <summary>
    /// Validates metadata field names before they are interpolated into OpenSearch field paths.
    /// </summary>
    private static void ValidateMetadataKey(string? metadataKey)
    {
        if (string.IsNullOrWhiteSpace(metadataKey))
        {
            return;
        }

        if (!MetadataKeyPattern.IsMatch(metadataKey))
        {
            throw new ArgumentException("MetadataKey may contain only letters, numbers, underscore, or hyphen, and must be 1 to 64 characters.", nameof(metadataKey));
        }
    }

    /// <summary>
    /// Builds full-text query clauses for document, OCR, AI, and metadata fields.
    /// </summary>
    private static IReadOnlyList<object> BuildMustClauses(SearchDocumentsQuery query)
    {
        if (string.IsNullOrWhiteSpace(query.Query))
        {
            return
            [
                new Dictionary<string, object?>
                {
                    ["match_all"] = new Dictionary<string, object?>()
                }
            ];
        }

        return
        [
            new Dictionary<string, object?>
            {
                ["multi_match"] = new Dictionary<string, object?>
                {
                    ["query"] = query.Query,
                    ["fields"] = new[] { "fileName^3", "documentType^2", "ocrText", "aiSummary", "aiClassification", "metadata.*" }
                }
            }
        ];
    }

    /// <summary>
    /// Builds exact-match filter clauses from structured query criteria.
    /// </summary>
    private static IReadOnlyList<object> BuildFilters(SearchDocumentsQuery query)
    {
        var filters = new List<object>();

        AddTermFilter(filters, "documentType.keyword", query.DocumentType);
        AddTermFilter(filters, "status.keyword", query.Status);
        AddTermFilter(filters, "folderId.keyword", query.FolderId?.ToString());

        if (!string.IsNullOrWhiteSpace(query.MetadataKey) && !string.IsNullOrWhiteSpace(query.MetadataValue))
        {
            AddTermFilter(filters, $"metadata.{query.MetadataKey}.keyword", query.MetadataValue);
        }

        return filters;
    }

    /// <summary>
    /// Adds one exact term filter when a value is present.
    /// </summary>
    private static void AddTermFilter(List<object> filters, string fieldName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        filters.Add(new Dictionary<string, object?>
        {
            ["term"] = new Dictionary<string, object?>
            {
                [fieldName] = value
            }
        });
    }

    /// <summary>
    /// Builds highlight fields for user-visible text matches.
    /// </summary>
    private static object BuildHighlight(SearchDocumentsQuery query)
    {
        var fields = new Dictionary<string, object?>
        {
            ["fileName"] = new Dictionary<string, object?>(),
            ["ocrText"] = new Dictionary<string, object?>(),
            ["aiSummary"] = new Dictionary<string, object?>()
        };

        if (!string.IsNullOrWhiteSpace(query.MetadataKey))
        {
            fields[$"metadata.{query.MetadataKey}"] = new Dictionary<string, object?>();
        }

        return new Dictionary<string, object?>
        {
            ["pre_tags"] = new[] { "<mark>" },
            ["post_tags"] = new[] { "</mark>" },
            ["fields"] = fields
        };
    }
}
