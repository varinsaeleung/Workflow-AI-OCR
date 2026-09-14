using FluentAssertions;
using KmOcr.Application.Contracts.Search;
using KmOcr.Application.Search;
using Xunit;

namespace KmOcr.Application.Tests;

/// <summary>
/// Verifies enterprise search application use cases.
/// </summary>
public sealed class SearchModuleTests
{
    /// <summary>
    /// Ensures search criteria are delegated to the configured search index abstraction.
    /// </summary>
    [Fact]
    public async Task SearchAsync_should_delegate_query_to_search_index()
    {
        var index = new CapturingDocumentSearchIndex();
        var module = new SearchModule(index);
        var query = new SearchDocumentsQuery("invoice", "invoice", null, null, null, null);

        var response = await module.SearchAsync(query, CancellationToken.None);

        index.CapturedQuery.Should().Be(query);
        response.Total.Should().Be(1);
        response.Items.Single().Highlights["ocrText"].Single().Should().Be("Tax <mark>invoice</mark>");
    }

    /// <summary>
    /// Captures search calls for application module tests.
    /// </summary>
    private sealed class CapturingDocumentSearchIndex : IDocumentSearchIndex
    {
        /// <summary>
        /// Gets the query captured from the application module.
        /// </summary>
        public SearchDocumentsQuery? CapturedQuery { get; private set; }

        /// <summary>
        /// Marks the search index as ready for tests.
        /// </summary>
        public Task EnsureIndexAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Records indexed documents for tests.
        /// </summary>
        public Task IndexAsync(DocumentIndexDto document, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Records deleted documents for tests.
        /// </summary>
        public Task DeleteAsync(Guid documentId, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Captures the search query and returns a deterministic highlighted result.
        /// </summary>
        public Task<DocumentSearchResponseDto> SearchAsync(SearchDocumentsQuery query, CancellationToken cancellationToken)
        {
            CapturedQuery = query;
            return Task.FromResult(new DocumentSearchResponseDto(1, new[]
            {
                new DocumentSearchResultDto(
                    Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                    "invoice.pdf",
                    "invoice",
                    "OcrCompleted",
                    null,
                    DateTimeOffset.UtcNow,
                    new Dictionary<string, string>(),
                    "Tax invoice",
                    "invoice",
                    "Invoice summary",
                    new Dictionary<string, IReadOnlyList<string>>
                    {
                        ["ocrText"] = new[] { "Tax <mark>invoice</mark>" }
                    })
            }));
        }
    }
}
