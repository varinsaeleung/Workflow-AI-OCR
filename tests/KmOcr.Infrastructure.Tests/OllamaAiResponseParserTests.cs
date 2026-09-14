using FluentAssertions;
using KmOcr.Infrastructure.Ai;
using Xunit;

namespace KmOcr.Infrastructure.Tests;

/// <summary>
/// Verifies Ollama AI JSON response parsing behavior.
/// </summary>
public sealed class OllamaAiResponseParserTests
{
    /// <summary>
    /// Ensures a valid Ollama response is converted into an AI engine result.
    /// </summary>
    [Fact]
    public void Parse_should_convert_ollama_json_payload_to_ai_result()
    {
        var responseJson = """
        {
          "response": "{\"classification\":\"invoice\",\"summary\":\"Invoice for copier service\",\"metadata\":{\"amount\":\"THB 1,200.00\",\"vendor\":\"Konica Minolta\"},\"confidence\":0.91}"
        }
        """;

        var result = OllamaAiResponseParser.Parse(responseJson, "llama3.1");

        result.Classification.Should().Be("invoice");
        result.Summary.Should().Be("Invoice for copier service");
        result.Entities.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            ["amount"] = "THB 1,200.00",
            ["vendor"] = "Konica Minolta"
        });
        result.ConfidenceScore.Should().Be(0.91m);
        result.Engine.Should().Be("ollama:llama3.1");
    }

    /// <summary>
    /// Ensures malformed AI JSON is rejected instead of being silently stored.
    /// </summary>
    [Fact]
    public void Parse_should_reject_missing_classification()
    {
        var responseJson = """
        {
          "response": "{\"summary\":\"Missing classification\",\"metadata\":{},\"confidence\":0.4}"
        }
        """;

        var action = () => OllamaAiResponseParser.Parse(responseJson, "llama3.1");

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Ollama response did not include a classification.");
    }
}
