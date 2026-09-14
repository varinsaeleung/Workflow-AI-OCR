using System.Net;
using System.Text;
using System.Text.Json;
using KmOcr.Application.Contracts.Search;
using KmOcr.Application.Search;
using Microsoft.Extensions.Options;

namespace KmOcr.Infrastructure.Search;

/// <summary>
/// OpenSearch HTTP adapter for document indexing and highlighted search.
/// </summary>
public sealed class OpenSearchDocumentIndex : IDocumentSearchIndex
{
    private readonly HttpClient _httpClient;
    private readonly OpenSearchOptions _options;

    /// <summary>
    /// Creates the OpenSearch adapter with HTTP and configuration dependencies.
    /// </summary>
    public OpenSearchDocumentIndex(HttpClient httpClient, IOptions<OpenSearchOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _httpClient.BaseAddress = new Uri(_options.Url);
    }

    /// <summary>
    /// Creates the index and mapping when OpenSearch does not already have it.
    /// </summary>
    public async Task EnsureIndexAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Head, $"/{_options.IndexName}");
        using var existsResponse = await _httpClient.SendAsync(request, cancellationToken);

        if (existsResponse.StatusCode == HttpStatusCode.OK)
        {
            return;
        }

        using var response = await _httpClient.PutAsync(
            $"/{_options.IndexName}",
            CreateJsonContent(OpenSearchIndexMapping.BuildMappingJson()),
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Indexes one document search read model by document id.
    /// </summary>
    public async Task IndexAsync(DocumentIndexDto document, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PutAsync(
            $"/{_options.IndexName}/_doc/{document.Id}",
            CreateJsonContent(OpenSearchDocumentMapper.ToIndexJson(document)),
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Deletes one document from the search index.
    /// </summary>
    public async Task DeleteAsync(Guid documentId, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.DeleteAsync($"/{_options.IndexName}/_doc/{documentId}", cancellationToken);

        if (response.StatusCode != HttpStatusCode.NotFound)
        {
            response.EnsureSuccessStatusCode();
        }
    }

    /// <summary>
    /// Searches documents with OpenSearch filters and highlight snippets.
    /// </summary>
    public async Task<DocumentSearchResponseDto> SearchAsync(SearchDocumentsQuery query, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsync(
            $"/{_options.IndexName}/_search",
            CreateJsonContent(OpenSearchDocumentQueryBuilder.BuildSearchRequest(query)),
            cancellationToken);
        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseSearchResponse(responseJson);
    }

    /// <summary>
    /// Creates JSON HTTP content for OpenSearch requests.
    /// </summary>
    private static StringContent CreateJsonContent(string json)
    {
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    /// <summary>
    /// Parses OpenSearch search response JSON into the application response contract.
    /// </summary>
    private static DocumentSearchResponseDto ParseSearchResponse(string responseJson)
    {
        using var document = JsonDocument.Parse(responseJson);
        var hitsRoot = document.RootElement.GetProperty("hits");
        var total = hitsRoot.GetProperty("total").GetProperty("value").GetInt64();
        var items = hitsRoot.GetProperty("hits")
            .EnumerateArray()
            .Select(ParseHit)
            .ToList();
        return new DocumentSearchResponseDto(total, items);
    }

    /// <summary>
    /// Parses one OpenSearch hit into a document search result DTO.
    /// </summary>
    private static DocumentSearchResultDto ParseHit(JsonElement hit)
    {
        var source = hit.GetProperty("_source");
        return new DocumentSearchResultDto(
            Guid.Parse(ReadString(source, "id")),
            ReadString(source, "fileName"),
            ReadString(source, "documentType"),
            ReadString(source, "status"),
            ReadNullableGuid(source, "folderId"),
            source.GetProperty("createdAt").GetDateTimeOffset(),
            ReadMetadata(source),
            ReadNullableString(source, "ocrText"),
            ReadNullableString(source, "aiClassification"),
            ReadNullableString(source, "aiSummary"),
            ReadHighlights(hit));
    }

    /// <summary>
    /// Reads a required string property from a JSON object.
    /// </summary>
    private static string ReadString(JsonElement source, string propertyName)
    {
        return source.GetProperty(propertyName).GetString() ?? string.Empty;
    }

    /// <summary>
    /// Reads an optional string property from a JSON object.
    /// </summary>
    private static string? ReadNullableString(JsonElement source, string propertyName)
    {
        return source.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    /// <summary>
    /// Reads an optional GUID property from a JSON object.
    /// </summary>
    private static Guid? ReadNullableGuid(JsonElement source, string propertyName)
    {
        var value = ReadNullableString(source, propertyName);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    /// <summary>
    /// Reads metadata key-value pairs from an OpenSearch source document.
    /// </summary>
    private static IReadOnlyDictionary<string, string> ReadMetadata(JsonElement source)
    {
        if (!source.TryGetProperty("metadata", out var metadata) || metadata.ValueKind != JsonValueKind.Object)
        {
            return new Dictionary<string, string>();
        }

        return metadata.EnumerateObject().ToDictionary(
            property => property.Name,
            property => property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() ?? string.Empty : property.Value.GetRawText());
    }

    /// <summary>
    /// Reads highlight snippets returned by OpenSearch.
    /// </summary>
    private static IReadOnlyDictionary<string, IReadOnlyList<string>> ReadHighlights(JsonElement hit)
    {
        if (!hit.TryGetProperty("highlight", out var highlight) || highlight.ValueKind != JsonValueKind.Object)
        {
            return new Dictionary<string, IReadOnlyList<string>>();
        }

        return highlight.EnumerateObject().ToDictionary(
            property => property.Name,
            property => (IReadOnlyList<string>)property.Value.EnumerateArray().Select(value => value.GetString() ?? string.Empty).ToList());
    }
}
