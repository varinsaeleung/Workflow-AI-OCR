using KmOcr.Domain.Documents;

namespace KmOcr.Application.Contracts.Persistence;

/// <summary>
/// Repository contract for document aggregate persistence.
/// </summary>
public interface IDocumentRepository
{
    /// <summary>
    /// Adds a document aggregate to the current unit of work.
    /// </summary>
    Task AddAsync(Document document, CancellationToken cancellationToken);

    /// <summary>
    /// Adds a document folder to the current unit of work.
    /// </summary>
    Task AddFolderAsync(DocumentFolder folder, CancellationToken cancellationToken);

    /// <summary>
    /// Loads a document aggregate by identifier.
    /// </summary>
    Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Loads a document folder by identifier.
    /// </summary>
    Task<DocumentFolder?> GetFolderByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Lists document folders under the provided parent folder id.
    /// </summary>
    Task<IReadOnlyList<DocumentFolder>> ListFoldersAsync(Guid? parentFolderId, CancellationToken cancellationToken);

    /// <summary>
    /// Searches documents by free text across filename, document type, OCR text, and metadata.
    /// </summary>
    Task<IReadOnlyList<Document>> SearchAsync(string? query, CancellationToken cancellationToken);

    /// <summary>
    /// Counts documents grouped by processing status.
    /// </summary>
    Task<IReadOnlyDictionary<DocumentStatus, int>> CountByStatusAsync(CancellationToken cancellationToken);
}
