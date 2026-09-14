using System.Net.Http.Json;
using KmOcr.Application.Contracts.Ai;
using Microsoft.Extensions.Options;

namespace KmOcr.Infrastructure.Ai;

/// <summary>
/// AI engine adapter that performs classification, summary, and metadata extraction through Ollama.
/// </summary>
public sealed class OllamaAiEngine : IAiEngine
{
    private readonly HttpClient _httpClient;
    private readonly OllamaAiOptions _options;

    /// <summary>
    /// Creates the Ollama AI engine with HTTP and configuration dependencies.
    /// </summary>
    public OllamaAiEngine(HttpClient httpClient, IOptions<OllamaAiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
    }

    /// <summary>
    /// Sends OCR text to Ollama and returns normalized AI extraction results.
    /// </summary>
    public async Task<AiEngineResult> ExtractAsync(string text, CancellationToken cancellationToken)
    {
        var request = new OllamaGenerateRequest(
            _options.Model,
            BuildPrompt(text),
            false,
            "json");
        using var response = await _httpClient.PostAsJsonAsync("/api/generate", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        return OllamaAiResponseParser.Parse(responseJson, _options.Model);
    }

    /// <summary>
    /// Builds an instruction prompt that asks the local model to return only machine-readable JSON.
    /// </summary>
    private static string BuildPrompt(string text)
    {
        return """
        You are an enterprise document AI service.
        Analyze the OCR text and return only valid JSON with this schema:
        {
          "classification": "invoice | contract | receipt | report | letter | unknown",
          "summary": "short business summary",
          "metadata": {
            "documentDate": "",
            "amount": "",
            "currency": "",
            "vendor": "",
            "customer": "",
            "email": "",
            "referenceNumber": ""
          },
          "confidence": 0.0
        }

        OCR text:
        """ + text;
    }

    /// <summary>
    /// Request payload accepted by Ollama generate API.
    /// </summary>
    private sealed record OllamaGenerateRequest(string Model, string Prompt, bool Stream, string Format);
}
