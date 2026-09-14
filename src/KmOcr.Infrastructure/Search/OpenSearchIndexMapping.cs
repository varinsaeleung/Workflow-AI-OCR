using System.Text.Json;

namespace KmOcr.Infrastructure.Search;

/// <summary>
/// Builds OpenSearch index settings and field mappings for enterprise document search.
/// </summary>
public static class OpenSearchIndexMapping
{
    /// <summary>
    /// Builds the index creation JSON with searchable text fields and keyword filter fields.
    /// </summary>
    public static string BuildMappingJson()
    {
        var mapping = new Dictionary<string, object?>
        {
            ["settings"] = new Dictionary<string, object?>
            {
                ["index"] = new Dictionary<string, object?>
                {
                    ["number_of_shards"] = 1,
                    ["number_of_replicas"] = 0
                }
            },
            ["mappings"] = new Dictionary<string, object?>
            {
                ["dynamic_templates"] = new object[]
                {
                    new Dictionary<string, object?>
                    {
                        ["metadata_keywords"] = new Dictionary<string, object?>
                        {
                            ["path_match"] = "metadata.*",
                            ["mapping"] = new Dictionary<string, object?>
                            {
                                ["type"] = "text",
                                ["fields"] = new Dictionary<string, object?>
                                {
                                    ["keyword"] = new Dictionary<string, object?> { ["type"] = "keyword" }
                                }
                            }
                        }
                    }
                },
                ["properties"] = BuildProperties()
            }
        };

        return JsonSerializer.Serialize(mapping);
    }

    /// <summary>
    /// Builds strongly typed field definitions used by filters, sorting, and highlights.
    /// </summary>
    private static object BuildProperties()
    {
        return new Dictionary<string, object?>
        {
            ["id"] = Keyword(),
            ["fileName"] = TextWithKeyword(),
            ["documentType"] = TextWithKeyword(),
            ["status"] = TextWithKeyword(),
            ["folderId"] = Keyword(),
            ["uploadedBy"] = Keyword(),
            ["createdAt"] = new Dictionary<string, object?> { ["type"] = "date" },
            ["metadata"] = new Dictionary<string, object?> { ["type"] = "object", ["dynamic"] = true },
            ["ocrText"] = new Dictionary<string, object?> { ["type"] = "text" },
            ["ocrConfidence"] = new Dictionary<string, object?> { ["type"] = "double" },
            ["aiClassification"] = TextWithKeyword(),
            ["aiSummary"] = new Dictionary<string, object?> { ["type"] = "text" },
            ["isDeleted"] = new Dictionary<string, object?> { ["type"] = "boolean" }
        };
    }

    /// <summary>
    /// Builds a keyword field mapping for exact filters.
    /// </summary>
    private static object Keyword()
    {
        return new Dictionary<string, object?> { ["type"] = "keyword" };
    }

    /// <summary>
    /// Builds a text mapping that also supports exact keyword filters.
    /// </summary>
    private static object TextWithKeyword()
    {
        return new Dictionary<string, object?>
        {
            ["type"] = "text",
            ["fields"] = new Dictionary<string, object?>
            {
                ["keyword"] = Keyword()
            }
        };
    }
}
