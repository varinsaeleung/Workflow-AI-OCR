using KmOcr.Application.Contracts.Search;

namespace KmOcr.Application.Search;

/// <summary>
/// Implements document search use cases through a search index abstraction.
/// </summary>
public sealed class SearchModule : ISearchModule
{
    private readonly IDocumentSearchIndex _searchIndex;

    /// <summary>
    /// Creates the search module with its search index dependency.
    /// </summary>
    public SearchModule(IDocumentSearchIndex searchIndex)
    {
        _searchIndex = searchIndex;
    }

    /// <summary>
    /// Searches indexed document, OCR, AI, and metadata fields.
    /// </summary>
    public Task<DocumentSearchResponseDto> SearchAsync(SearchDocumentsQuery query, CancellationToken cancellationToken)
    {
        return _searchIndex.SearchAsync(query, cancellationToken);
    }
}
