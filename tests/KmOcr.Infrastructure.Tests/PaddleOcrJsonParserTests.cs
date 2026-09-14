using FluentAssertions;
using KmOcr.Infrastructure.Ocr;
using Xunit;

namespace KmOcr.Infrastructure.Tests;

/// <summary>
/// Tests PaddleOCR JSON parsing without launching the external OCR process.
/// </summary>
public sealed class PaddleOcrJsonParserTests
{
    /// <summary>
    /// Verifies that PaddleOCR JSON pages and words are mapped to the application OCR result.
    /// </summary>
    [Fact]
    public void Parse_should_map_pages_words_text_and_confidence()
    {
        const string json = """
        {
          "engine": "paddleocr",
          "language": "tha+eng",
          "pages": [
            {
              "pageNumber": 1,
              "text": "สวัสดี Hello",
              "confidenceScore": 0.92,
              "words": [
                { "text": "สวัสดี", "x": 10, "y": 15, "width": 80, "height": 22, "confidenceScore": 0.91 },
                { "text": "Hello", "x": 95, "y": 15, "width": 60, "height": 22, "confidenceScore": 0.93 }
              ]
            },
            {
              "pageNumber": 2,
              "text": "Workflow",
              "confidenceScore": 0.86,
              "words": [
                { "text": "Workflow", "x": 20, "y": 35, "width": 110, "height": 25, "confidenceScore": 0.86 }
              ]
            }
          ]
        }
        """;

        var result = PaddleOcrJsonParser.Parse(json);

        result.Engine.Should().Be("paddleocr");
        result.Language.Should().Be("tha+eng");
        result.Text.Should().Be("สวัสดี Hello\nWorkflow");
        result.ConfidenceScore.Should().Be(0.89m);
        result.Pages.Should().HaveCount(2);
        result.Pages[0].Words.Should().HaveCount(2);
        result.Pages[0].Words[0].Text.Should().Be("สวัสดี");
    }

    /// <summary>
    /// Verifies that malformed PaddleOCR JSON is rejected with a useful exception.
    /// </summary>
    [Fact]
    public void Parse_should_reject_malformed_json()
    {
        var act = () => PaddleOcrJsonParser.Parse("{ malformed");

        act.Should().Throw<InvalidOperationException>().WithMessage("*PaddleOCR JSON*");
    }
}
