namespace KmOcr.Application.Search;

/// <summary>
/// Application module exposing enterprise document search use cases.
/// </summary>
public interface ISearchModule
{
    /// <summary>
    /// Searches indexed document, OCR, AI, and metadata fields.
    /// </summary>
    Task<DocumentSearchResponseDto> SearchAsync(SearchDocumentsQuery query, CancellationToken cancellationToken);
}
