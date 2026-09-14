namespace KmOcr.Infrastructure.Search;

/// <summary>
/// Configuration values for the OpenSearch document index.
/// </summary>
public sealed class OpenSearchOptions
{
    /// <summary>
    /// Gets or sets the OpenSearch base URL.
    /// </summary>
    public string Url { get; set; } = "http://localhost:9200";

    /// <summary>
    /// Gets or sets the document index name.
    /// </summary>
    public string IndexName { get; set; } = "km-documents";
}
