namespace KmOcr.Application.Documents;

/// <summary>
/// Application module that exposes document use cases.
/// </summary>
public interface IDocumentModule
{
    /// <summary>
    /// Uploads a document, persists metadata, and queues OCR processing.
    /// </summary>
    Task<DocumentDto> UploadAsync(UploadDocumentCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Uploads a new binary version for an existing document.
    /// </summary>
    Task<DocumentDto> UploadVersionAsync(UploadDocumentVersionCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Loads one document by identifier.
    /// </summary>
    Task<DocumentDto> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Searches documents by free text.
    /// </summary>
    Task<IReadOnlyList<DocumentDto>> SearchAsync(string? query, CancellationToken cancellationToken);

    /// <summary>
    /// Downloads the current or selected document version.
    /// </summary>
    Task<DownloadDocumentDto> DownloadAsync(DownloadDocumentCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a time-limited preview URL for the current document version.
    /// </summary>
    Task<DocumentPreviewDto> PreviewAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Moves a document to another folder.
    /// </summary>
    Task<DocumentDto> MoveAsync(MoveDocumentCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Soft-deletes a document.
    /// </summary>
    Task DeleteAsync(DeleteDocumentCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a folder.
    /// </summary>
    Task<DocumentFolderDto> CreateFolderAsync(CreateFolderCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Lists folders under a parent folder.
    /// </summary>
    Task<IReadOnlyList<DocumentFolderDto>> ListFoldersAsync(Guid? parentFolderId, CancellationToken cancellationToken);

    /// <summary>
    /// Moves a folder under another parent folder.
    /// </summary>
    Task<DocumentFolderDto> MoveFolderAsync(MoveFolderCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Updates metadata for a document.
    /// </summary>
    Task<DocumentDto> UpdateMetadataAsync(UpdateMetadataCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Queues a document for OCR reprocessing.
    /// </summary>
    Task<DocumentDto> QueueOcrAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Completes OCR processing for a document.
    /// </summary>
    Task<DocumentDto> CompleteOcrAsync(CompleteOcrCommand command, CancellationToken cancellationToken);
}
