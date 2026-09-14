using System.Net;
using System.Text.Json;
using FluentAssertions;
using KmOcr.Infrastructure.Ai;
using Microsoft.Extensions.Options;
using Xunit;

namespace KmOcr.Infrastructure.Tests;

/// <summary>
/// Verifies Ollama AI engine HTTP integration behavior.
/// </summary>
public sealed class OllamaAiEngineTests
{
    /// <summary>
    /// Ensures the configured model is sent to Ollama and the generated JSON is parsed.
    /// </summary>
    [Fact]
    public async Task ExtractAsync_should_send_configured_model_to_ollama()
    {
        var handler = new CapturingHttpMessageHandler("""
        {
          "response": "{\"classification\":\"contract\",\"summary\":\"Service contract summary\",\"metadata\":{\"party\":\"KM\"},\"confidence\":0.82}"
        }
        """);
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:11434")
        };
        var options = Options.Create(new OllamaAiOptions
        {
            BaseUrl = "http://localhost:11434",
            Model = "qwen2.5",
            TimeoutSeconds = 30
        });
        var engine = new OllamaAiEngine(httpClient, options);

        var result = await engine.ExtractAsync("Agreement between KM and customer.", CancellationToken.None);

        handler.RequestUri.Should().Be(new Uri("http://localhost:11434/api/generate"));
        handler.RequestPayload.GetProperty("model").GetString().Should().Be("qwen2.5");
        handler.RequestPayload.GetProperty("stream").GetBoolean().Should().BeFalse();
        result.Classification.Should().Be("contract");
        result.Summary.Should().Be("Service contract summary");
        result.Entities.Should().ContainKey("party").WhoseValue.Should().Be("KM");
        result.Engine.Should().Be("ollama:qwen2.5");
    }

    /// <summary>
    /// Captures an outgoing Ollama HTTP request and returns a deterministic response.
    /// </summary>
    private sealed class CapturingHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _responseJson;

        /// <summary>
        /// Creates the handler with a fixed Ollama response body.
        /// </summary>
        public CapturingHttpMessageHandler(string responseJson)
        {
            _responseJson = responseJson;
        }

        /// <summary>
        /// Gets the URI requested by the AI engine.
        /// </summary>
        public Uri? RequestUri { get; private set; }

        /// <summary>
        /// Gets the JSON payload posted by the AI engine.
        /// </summary>
        public JsonElement RequestPayload { get; private set; }

        /// <summary>
        /// Handles the outgoing request and records it for assertions.
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
