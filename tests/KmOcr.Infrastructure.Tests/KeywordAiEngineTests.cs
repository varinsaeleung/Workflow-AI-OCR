using FluentAssertions;
using KmOcr.Infrastructure.Ai;
using Xunit;

namespace KmOcr.Infrastructure.Tests;

/// <summary>
/// Verifies the starter AI engine classification and extraction behavior.
/// </summary>
public sealed class KeywordAiEngineTests
{
    /// <summary>
    /// Ensures invoice text is classified and amount metadata is extracted.
    /// </summary>
    [Fact]
    public async Task ExtractAsync_should_classify_invoice_and_extract_amount()
    {
        var engine = new KeywordAiEngine();

        var result = await engine.ExtractAsync("Tax invoice total THB 1,200.00", CancellationToken.None);

        result.Classification.Should().Be("invoice");
        result.Entities.Should().ContainKey("amount");
        result.Engine.Should().Be("keyword-ai");
    }
}
