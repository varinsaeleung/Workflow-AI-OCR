using KmOcr.Application.Search;

namespace KmOcr.Application.Contracts.Search;

/// <summary>
/// Abstraction for indexing and searching documents outside the relational source of truth.
/// </summary>
public interface IDocumentSearchIndex
{
    /// <summary>
    /// Creates the document index and mapping when it does not already exist.
    /// </summary>
    Task EnsureIndexAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Indexes or updates one document search document.
    /// </summary>
    Task IndexAsync(DocumentIndexDto document, CancellationToken cancellationToken);

    /// <summary>
    /// Removes one document from the search index.
    /// </summary>
    Task DeleteAsync(Guid documentId, CancellationToken cancellationToken);

    /// <summary>
    /// Searches indexed documents with text, filters, and highlights.
    /// </summary>
    Task<DocumentSearchResponseDto> SearchAsync(SearchDocumentsQuery query, CancellationToken cancellationToken);
}
