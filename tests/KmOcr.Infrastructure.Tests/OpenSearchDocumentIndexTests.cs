using System.Net;
using System.Text.Json;
using FluentAssertions;
using KmOcr.Application.Search;
using KmOcr.Infrastructure.Search;
using Microsoft.Extensions.Options;
using Xunit;

namespace KmOcr.Infrastructure.Tests;

/// <summary>
/// Verifies OpenSearch document index HTTP adapter behavior.
/// </summary>
public sealed class OpenSearchDocumentIndexTests
{
    /// <summary>
    /// Ensures searches are posted to the configured index and highlights are parsed.
    /// </summary>
    [Fact]
    public async Task SearchAsync_should_return_total_hits_and_highlights()
    {
        var handler = new CapturingOpenSearchHandler("""
        {
          "hits": {
            "total": { "value": 1 },
            "hits": [
              {
                "_source": {
                  "id": "dddddddd-dddd-dddd-dddd-dddddddddddd",
                  "fileName": "invoice.pdf",
                  "documentType": "invoice",
                  "status": "OcrCompleted",
                  "folderId": null,
                  "createdAt": "2026-09-11T08:00:00+00:00",
                  "metadata": { "vendor": "Konica Minolta" },
                  "ocrText": "Tax invoice",
                  "aiClassification": "invoice",
                  "aiSummary": "Invoice summary"
                },
                "highlight": {
                  "ocrText": [ "Tax <mark>invoice</mark>" ]
                }
              }
            ]
          }
        }
        """);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:9200") };
        var options = Options.Create(new OpenSearchOptions
        {
            Url = "http://localhost:9200",
            IndexName = "km-documents"
        });
        var index = new OpenSearchDocumentIndex(httpClient, options);

        var result = await index.SearchAsync(new SearchDocumentsQuery("invoice", null, null, null, null, null), CancellationToken.None);

        handler.RequestUri.Should().Be(new Uri("http://localhost:9200/km-documents/_search"));
        handler.RequestPayload.GetProperty("highlight").GetProperty("fields").TryGetProperty("ocrText", out _).Should().BeTrue();
        result.Total.Should().Be(1);
        result.Items.Single().Highlights["ocrText"].Single().Should().Be("Tax <mark>invoice</mark>");
    }

    /// <summary>
    /// Captures OpenSearch requests and returns a deterministic JSON response.
    /// </summary>
    private sealed class CapturingOpenSearchHandler : HttpMessageHandler
    {
        private readonly string _responseJson;

        /// <summary>
        /// Creates a handler with the response returned to the index adapter.
        /// </summary>
        public CapturingOpenSearchHandler(string responseJson)
        {
            _responseJson = responseJson;
        }

        /// <summary>
        /// Gets the requested OpenSearch URI.
        /// </summary>
        public Uri? RequestUri { get; private set; }

        /// <summary>
        /// Gets the posted OpenSearch request JSON.
        /// </summary>
        public JsonElement RequestPayload { get; private set; }

        /// <summary>
        /// Handles and records the outgoing request.
        /// </summary>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            var requestJson = await request.Content!.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(requestJson);
            RequestPayload = document.RootElement.Clone();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseJson)
            };
        }
    }
}
