namespace KmOcr.Application.Search;

/// <summary>
/// Application module that keeps document search indexes synchronized with PostgreSQL state.
/// </summary>
public interface ISearchIndexingModule
{
    /// <summary>
    /// Indexes the latest document aggregate state by document id.
    /// </summary>
    Task IndexDocumentAsync(Guid documentId, CancellationToken cancellationToken);
}
