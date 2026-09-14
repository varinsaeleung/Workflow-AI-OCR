using KmOcr.Application.Common;
using KmOcr.Application.Contracts.Persistence;
using KmOcr.Application.Contracts.Search;
using KmOcr.Domain.Documents;

namespace KmOcr.Application.Search;

/// <summary>
/// Implements document indexing use cases from PostgreSQL aggregates to search read models.
/// </summary>
public sealed class SearchIndexingModule : ISearchIndexingModule
{
    private readonly IDocumentRepository _documents;
    private readonly IDocumentSearchIndex _searchIndex;

    /// <summary>
    /// Creates the indexing module with repository and search index dependencies.
    /// </summary>
    public SearchIndexingModule(IDocumentRepository documents, IDocumentSearchIndex searchIndex)
    {
        _documents = documents;
        _searchIndex = searchIndex;
    }

    /// <summary>
    /// Indexes the current document state or removes deleted documents from the index.
    /// </summary>
    public async Task IndexDocumentAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var document = await _documents.GetByIdAsync(documentId, cancellationToken)
            ?? throw new NotFoundException($"Document '{documentId}' was not found.");

        if (document.DeletedAt.HasValue)
        {
            await _searchIndex.DeleteAsync(document.Id, cancellationToken);
            return;
        }

        await _searchIndex.EnsureIndexAsync(cancellationToken);
        await _searchIndex.IndexAsync(Map(document), cancellationToken);
    }

    /// <summary>
    /// Maps a document aggregate into a search index DTO.
    /// </summary>
    private static DocumentIndexDto Map(Document document)
    {
        var metadata = document.Metadata.ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase);
        return new DocumentIndexDto(
            document.Id,
            document.FileName,
            document.DocumentType,
            document.Status.ToString(),
            document.FolderId,
            document.UploadedBy,
            document.CreatedAt,
            metadata,
            document.OcrResult?.ExtractedText,
            document.OcrResult?.ConfidenceScore,
            document.AiExtraction?.Classification,
            document.AiExtraction?.Summary,
            document.DeletedAt.HasValue);
    }
}
