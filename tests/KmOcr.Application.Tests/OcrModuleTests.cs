using FluentAssertions;
using KmOcr.Application.Common;
using KmOcr.Application.Contracts.Ocr;
using KmOcr.Application.Ocr;
using Xunit;

namespace KmOcr.Application.Tests;

/// <summary>
/// Tests OCR application use cases.
/// </summary>
public sealed class OcrModuleTests
{
    /// <summary>
    /// Verifies that a supported image file is copied to the OCR engine and mapped to JSON output.
    /// </summary>
    [Fact]
    public async Task ExtractAsync_should_return_json_result_for_supported_image()
    {
        var engine = new CapturingOcrEngine(new OcrEngineResult(
            "paddleocr",
            "tha+eng",
            "สวัสดี Hello",
            0.91m,
            [
                new OcrPageResult(1, "สวัสดี Hello", 0.91m, [new OcrWordResult("Hello", 12, 20, 60, 24, 0.93m)])
            ]));
        var module = new OcrModule(engine);
        await using var content = new MemoryStream([1, 2, 3]);

        var result = await module.ExtractAsync(new OcrExtractCommand("scan.png", "image/png", content), CancellationToken.None);

        result.Engine.Should().Be("paddleocr");
        result.Language.Should().Be("tha+eng");
        result.Text.Should().Be("สวัสดี Hello");
        result.ConfidenceScore.Should().Be(0.91m);
        result.Pages.Should().ContainSingle().Which.Words.Should().ContainSingle(word => word.Text == "Hello");
        engine.CapturedContentType.Should().Be("image/png");
        File.Exists(engine.CapturedPath).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that PDF files are accepted by the OCR use case.
    /// </summary>
    [Fact]
    public async Task ExtractAsync_should_accept_pdf_files()
    {
        var engine = new CapturingOcrEngine(new OcrEngineResult("paddleocr", "tha+eng", "Invoice", 0.88m, []));
        var module = new OcrModule(engine);
        await using var content = new MemoryStream([37, 80, 68, 70]);

        var result = await module.ExtractAsync(new OcrExtractCommand("invoice.pdf", "application/pdf", content), CancellationToken.None);

        result.Text.Should().Be("Invoice");
        engine.CapturedContentType.Should().Be("application/pdf");
    }

    /// <summary>
    /// Verifies that unsupported files are rejected before external OCR execution.
    /// </summary>
    [Fact]
    public async Task ExtractAsync_should_reject_unsupported_content_types()
    {
        var engine = new CapturingOcrEngine(new OcrEngineResult("paddleocr", "tha+eng", string.Empty, 0m, []));
        var module = new OcrModule(engine);
        await using var content = new MemoryStream([1]);

        var act = () => module.ExtractAsync(new OcrExtractCommand("notes.txt", "text/plain", content), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>().WithMessage("*Unsupported OCR content type*");
        engine.CapturedPath.Should().BeNull();
    }

    /// <summary>
    /// Verifies that empty files are rejected.
    /// </summary>
    [Fact]
    public async Task ExtractAsync_should_reject_empty_files()
    {
        var engine = new CapturingOcrEngine(new OcrEngineResult("paddleocr", "tha+eng", string.Empty, 0m, []));
        var module = new OcrModule(engine);
        await using var content = new MemoryStream();

        var act = () => module.ExtractAsync(new OcrExtractCommand("empty.jpg", "image/jpeg", content), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>().WithMessage("*File content is required*");
    }

    private sealed class CapturingOcrEngine : IOcrEngine
    {
        private readonly OcrEngineResult _result;

        /// <summary>
        /// Creates the fake engine with a predetermined OCR result.
        /// </summary>
        public CapturingOcrEngine(OcrEngineResult result)
        {
            _result = result;
        }

        /// <summary>
        /// Gets the temporary path passed to the OCR engine.
        /// </summary>
        public string? CapturedPath { get; private set; }

        /// <summary>
        /// Gets the content type passed to the OCR engine.
        /// </summary>
        public string? CapturedContentType { get; private set; }

        /// <summary>
        /// Captures the request and returns the configured result.
        /// </summary>
        public Task<OcrEngineResult> ExtractAsync(string filePath, string contentType, CancellationToken cancellationToken)
        {
            CapturedPath = filePath;
            CapturedContentType = contentType;
            return Task.FromResult(_result);
        }
    }
}
