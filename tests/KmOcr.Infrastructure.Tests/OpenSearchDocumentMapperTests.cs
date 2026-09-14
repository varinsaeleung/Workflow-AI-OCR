using System.Text.Json;
using FluentAssertions;
using KmOcr.Application.Search;
using KmOcr.Infrastructure.Search;
using Xunit;

namespace KmOcr.Infrastructure.Tests;

/// <summary>
/// Verifies OpenSearch index document mapping.
/// </summary>
public sealed class OpenSearchDocumentMapperTests
{
    /// <summary>
    /// Ensures document, OCR, AI, and metadata fields are serialized for indexing.
    /// </summary>
    [Fact]
    public void ToIndexJson_should_include_document_ocr_ai_and_metadata_fields()
    {
        var document = new DocumentIndexDto(
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            "invoice.pdf",
            "invoice",
            "OcrCompleted",
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            "admin@km.local",
            new DateTimeOffset(2026, 9, 11, 8, 0, 0, TimeSpan.Zero),
            new Dictionary<string, string> { ["vendor"] = "Konica Minolta" },
            "Tax invoice total THB 1,200.00",
            0.95m,
            "invoice",
            "Invoice for service fee",
            false);

        var json = OpenSearchDocumentMapper.ToIndexJson(document);
        using var jsonDocument = JsonDocument.Parse(json);
        var root = jsonDocument.RootElement;

        root.GetProperty("id").GetString().Should().Be("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        root.GetProperty("fileName").GetString().Should().Be("invoice.pdf");
        root.GetProperty("ocrText").GetString().Should().Be("Tax invoice total THB 1,200.00");
        root.GetProperty("aiSummary").GetString().Should().Be("Invoice for service fee");
        root.GetProperty("metadata").GetProperty("vendor").GetString().Should().Be("Konica Minolta");
        root.GetProperty("isDeleted").GetBoolean().Should().BeFalse();
    }
}
