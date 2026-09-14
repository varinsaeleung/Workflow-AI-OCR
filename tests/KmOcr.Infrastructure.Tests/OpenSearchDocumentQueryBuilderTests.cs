using System.Text.Json;
using FluentAssertions;
using KmOcr.Application.Search;
using KmOcr.Infrastructure.Search;
using Xunit;

namespace KmOcr.Infrastructure.Tests;

/// <summary>
/// Verifies OpenSearch document query JSON generation.
/// </summary>
public sealed class OpenSearchDocumentQueryBuilderTests
{
    /// <summary>
    /// Ensures full-text, filters, and highlights are included in the OpenSearch request.
    /// </summary>
    [Fact]
    public void BuildSearchRequest_should_include_text_query_filters_and_highlight()
    {
        var query = new SearchDocumentsQuery(
            "invoice toner",
            "invoice",
            "OcrCompleted",
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            "vendor",
            "Konica Minolta",
            20,
            10);

        var json = OpenSearchDocumentQueryBuilder.BuildSearchRequest(query);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        root.GetProperty("from").GetInt32().Should().Be(10);
        root.GetProperty("size").GetInt32().Should().Be(20);
        root.GetProperty("query").GetProperty("bool").GetProperty("must")[0]
            .GetProperty("multi_match").GetProperty("query").GetString().Should().Be("invoice toner");
        root.GetProperty("query").GetProperty("bool").GetProperty("filter").GetArrayLength().Should().Be(4);
        root.GetProperty("highlight").GetProperty("fields").TryGetProperty("ocrText", out _).Should().BeTrue();
        root.GetProperty("highlight").GetProperty("fields").TryGetProperty("metadata.vendor", out _).Should().BeTrue();
    }

    /// <summary>
    /// Ensures unsafe metadata field names cannot be injected into OpenSearch field paths.
    /// </summary>
    [Fact]
    public void BuildSearchRequest_should_reject_unsafe_metadata_key()
    {
        var query = new SearchDocumentsQuery(
            "invoice",
            null,
            null,
            null,
            "vendor.keyword\":{}",
            "Konica Minolta",
            20,
            0);

        var act = () => OpenSearchDocumentQueryBuilder.BuildSearchRequest(query);

        act.Should().Throw<ArgumentException>()
            .WithMessage("MetadataKey may contain only letters, numbers, underscore, or hyphen, and must be 1 to 64 characters.*")
            .And.ParamName.Should().Be("metadataKey");
    }
}
