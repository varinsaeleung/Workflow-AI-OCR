using KmOcr.Application.Common;
using KmOcr.Application.Contracts.Messaging;
using KmOcr.Application.Contracts.Persistence;
using KmOcr.Application.Contracts.Storage;
using KmOcr.Domain.Documents;
using Microsoft.Extensions.Logging;

namespace KmOcr.Application.Documents;

/// <summary>
/// Implements document use cases without depending on infrastructure frameworks.
/// </summary>
public sealed class DocumentModule : IDocumentModule
{
    private readonly IDocumentRepository _documents;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorage _fileStorage;
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<DocumentModule> _logger;

    /// <summary>
    /// Creates the document module with its required abstractions.
    /// </summary>
    public DocumentModule(
        IDocumentRepository documents,
        IUnitOfWork unitOfWork,
        IFileStorage fileStorage,
        IMessagePublisher publisher,
        ILogger<DocumentModule> logger)
    {
        _documents = documents;
        _unitOfWork = unitOfWork;
        _fileStorage = fileStorage;
        _publisher = publisher;
        _logger = logger;
    }

    /// <summary>
    /// Uploads a document, stores metadata, commits it, and publishes an OCR job.
    /// </summary>
    public async Task<DocumentDto> UploadAsync(UploadDocumentCommand command, CancellationToken cancellationToken)
    {
        ValidateUpload(command);

        var storedObject = await _fileStorage.SaveAsync(command.Content, command.FileName, command.ContentType, cancellationToken);
        var document = Document.Create(
            command.FileName,
            command.ContentType,
            storedObject.StoragePath,
            command.DocumentType,
            command.UploadedBy,
            command.FolderId,
            storedObject.BucketName,
            storedObject.SizeBytes,
            storedObject.ChecksumSha256);

        foreach (var metadata in command.Metadata)
        {
            document.UpsertMetadata(metadata.Key, metadata.Value);
        }

        document.QueueOcr();
        await _documents.AddAsync(document, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _publisher.PublishOcrJobAsync(new OcrJobMessage(document.Id, document.StoragePath, document.ContentType), cancellationToken);
        _logger.LogInformation("Document {DocumentId} uploaded and queued for OCR.", document.Id);

        return Map(document);
    }

    /// <summary>
    /// Uploads a new binary version and makes it current.
    /// </summary>
    public async Task<DocumentDto> UploadVersionAsync(UploadDocumentVersionCommand command, CancellationToken cancellationToken)
    {
        ValidateFile(command.FileName, command.ContentType, command.Content);
        if (string.IsNullOrWhiteSpace(command.CreatedBy))
        {
            throw new ValidationException("Version creator is required.");
        }

        var document = await LoadDocumentAsync(command.DocumentId, cancellationToken);
        var storedObject = await _fileStorage.SaveAsync(command.Content, command.FileName, command.ContentType, cancellationToken);
        document.AddVersion(
            command.FileName,
            command.ContentType,
            storedObject.StoragePath,
            storedObject.SizeBytes,
            storedObject.ChecksumSha256,
            command.CreatedBy);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Document {DocumentId} version {VersionNumber} uploaded.", document.Id, document.CurrentVersionNumber);
        return Map(document);
    }

    /// <summary>
    /// Loads a document or throws when the id does not exist.
    /// </summary>
    public async Task<DocumentDto> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var document = await LoadDocumentAsync(id, cancellationToken);
        return Map(document);
    }

    /// <summary>
    /// Searches documents and maps domain aggregates to client-safe DTOs.
    /// </summary>
    public async Task<IReadOnlyList<DocumentDto>> SearchAsync(string? query, CancellationToken cancellationToken)
    {
        var documents = await _documents.SearchAsync(query, cancellationToken);
        return documents.Select(Map).ToList();
    }

    /// <summary>
    /// Opens the current or requested document version for download.
    /// </summary>
    public async Task<DownloadDocumentDto> DownloadAsync(DownloadDocumentCommand command, CancellationToken cancellationToken)
    {
        var document = await LoadDocumentAsync(command.DocumentId, cancellationToken);
        var version = SelectVersion(document, command.VersionNumber);
        var file = await _fileStorage.OpenReadAsync(version.StoragePath, version.FileName, version.ContentType, cancellationToken);
        return new DownloadDocumentDto(file.FileName, file.ContentType, file.Content);
    }

    /// <summary>
    /// Creates a time-limited preview URL for the current document version.
    /// </summary>
    public async Task<DocumentPreviewDto> PreviewAsync(Guid id, CancellationToken cancellationToken)
    {
        var document = await LoadDocumentAsync(id, cancellationToken);
        var expiresIn = TimeSpan.FromMinutes(10);
        var url = await _fileStorage.GetPreviewUrlAsync(document.StoragePath, expiresIn, cancellationToken);
        return new DocumentPreviewDto(document.Id, url, DateTimeOffset.UtcNow.Add(expiresIn));
    }

    /// <summary>
    /// Moves a document to the selected folder and commits the change.
    /// </summary>
    public async Task<DocumentDto> MoveAsync(MoveDocumentCommand command, CancellationToken cancellationToken)
    {
        var document = await LoadDocumentAsync(command.DocumentId, cancellationToken);
        document.MoveToFolder(command.FolderId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Document {DocumentId} moved by {Actor}.", document.Id, command.Actor);
        return Map(document);
    }

    /// <summary>
    /// Soft-deletes a document and keeps stored objects for retention.
    /// </summary>
    public async Task DeleteAsync(DeleteDocumentCommand command, CancellationToken cancellationToken)
    {
        var document = await LoadDocumentAsync(command.DocumentId, cancellationToken);
        document.SoftDelete(command.Actor);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Document {DocumentId} soft-deleted by {Actor}.", document.Id, command.Actor);
    }

    /// <summary>
    /// Creates a folder under the requested parent.
    /// </summary>
    public async Task<DocumentFolderDto> CreateFolderAsync(CreateFolderCommand command, CancellationToken cancellationToken)
    {
        var parent = command.ParentFolderId.HasValue
            ? await LoadFolderAsync(command.ParentFolderId.Value, cancellationToken)
            : null;
        var folder = parent is null
            ? DocumentFolder.CreateRoot(command.Name, command.CreatedBy)
            : DocumentFolder.CreateChild(command.Name, parent.Id, parent.Path, command.CreatedBy);
        await _documents.AddFolderAsync(folder, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapFolder(folder);
    }

    /// <summary>
    /// Lists folders under the requested parent.
    /// </summary>
    public async Task<IReadOnlyList<DocumentFolderDto>> ListFoldersAsync(Guid? parentFolderId, CancellationToken cancellationToken)
    {
        var folders = await _documents.ListFoldersAsync(parentFolderId, cancellationToken);
        return folders.Select(MapFolder).ToList();
    }

    /// <summary>
    /// Moves a folder under another folder.
    /// </summary>
    public async Task<DocumentFolderDto> MoveFolderAsync(MoveFolderCommand command, CancellationToken cancellationToken)
    {
        var folder = await LoadFolderAsync(command.FolderId, cancellationToken);
        var parent = command.ParentFolderId.HasValue
            ? await LoadFolderAsync(command.ParentFolderId.Value, cancellationToken)
            : null;
        folder.MoveTo(parent?.Id, parent?.Path ?? "/");
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Folder {FolderId} moved by {Actor}.", folder.Id, command.Actor);
        return MapFolder(folder);
    }

    /// <summary>
    /// Updates document metadata and commits the changes.
    /// </summary>
    public async Task<DocumentDto> UpdateMetadataAsync(UpdateMetadataCommand command, CancellationToken cancellationToken)
    {
        if (command.Metadata.Count == 0)
        {
            throw new ValidationException("At least one metadata value is required.");
        }

        var document = await LoadDocumentAsync(command.DocumentId, cancellationToken);

        foreach (var metadata in command.Metadata)
        {
            document.UpsertMetadata(metadata.Key, metadata.Value);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Document {DocumentId} metadata updated by {Actor}.", document.Id, command.Actor);
        return Map(document);
    }

    /// <summary>
    /// Queues a document for another OCR run and publishes a broker message.
    /// </summary>
    public async Task<DocumentDto> QueueOcrAsync(Guid id, CancellationToken cancellationToken)
    {
        var document = await LoadDocumentAsync(id, cancellationToken);
        document.QueueOcr();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _publisher.PublishOcrJobAsync(new OcrJobMessage(document.Id, document.StoragePath, document.ContentType), cancellationToken);
        _logger.LogInformation("Document {DocumentId} requeued for OCR.", document.Id);
        return Map(document);
    }

    /// <summary>
    /// Persists OCR output and commits the document state transition.
    /// </summary>
    public async Task<DocumentDto> CompleteOcrAsync(CompleteOcrCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.ExtractedText))
        {
            throw new ValidationException("OCR text is required.");
        }

        var document = await LoadDocumentAsync(command.DocumentId, cancellationToken);
        document.CompleteOcr(command.ExtractedText, command.ConfidenceScore, command.Engine);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Document {DocumentId} OCR completed with confidence {ConfidenceScore}.", document.Id, command.ConfidenceScore);
        return Map(document);
    }

    /// <summary>
    /// Loads a document aggregate and converts missing data into an application exception.
    /// </summary>
    private async Task<Document> LoadDocumentAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _documents.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Document '{id}' was not found.");
    }

    /// <summary>
    /// Loads a folder aggregate and converts missing data into an application exception.
    /// </summary>
    private async Task<DocumentFolder> LoadFolderAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _documents.GetFolderByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Folder '{id}' was not found.");
    }

    /// <summary>
    /// Validates upload input before any side effects happen.
    /// </summary>
    private static void ValidateUpload(UploadDocumentCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.FileName))
        {
            throw new ValidationException("File name is required.");
        }

        if (string.IsNullOrWhiteSpace(command.ContentType))
        {
            throw new ValidationException("Content type is required.");
        }

        if (string.IsNullOrWhiteSpace(command.UploadedBy))
        {
            throw new ValidationException("Uploader is required.");
        }

        if (string.IsNullOrWhiteSpace(command.DocumentType))
        {
            throw new ValidationException("Document type is required.");
        }

        ValidateFile(command.FileName, command.ContentType, command.Content);
    }

    /// <summary>
    /// Validates file input before storage writes happen.
    /// </summary>
    private static void ValidateFile(string fileName, string contentType, Stream content)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ValidationException("File name is required.");
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ValidationException("Content type is required.");
        }

        if (content.CanSeek && content.Length == 0)
        {
            throw new ValidationException("File content is required.");
        }
    }

    /// <summary>
    /// Selects the current version or an explicit version from a document.
    /// </summary>
    private static DocumentVersion SelectVersion(Document document, int? versionNumber)
    {
        var selectedVersionNumber = versionNumber ?? document.CurrentVersionNumber;
        return document.Versions.SingleOrDefault(version => version.VersionNumber == selectedVersionNumber)
            ?? throw new NotFoundException($"Document version '{selectedVersionNumber}' was not found.");
    }

    /// <summary>
    /// Maps a domain aggregate to a document DTO.
    /// </summary>
    private static DocumentDto Map(Document document)
    {
        var metadata = document.Metadata.ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase);
        return new DocumentDto(
            document.Id,
            document.FileName,
            document.ContentType,
            document.DocumentType,
            document.Status.ToString(),
            document.UploadedBy,
            document.StoragePath,
            document.FolderId,
            document.StorageBucket,
            document.FileSizeBytes,
            document.CurrentVersionNumber,
            document.DeletedAt,
            document.CreatedAt,
            metadata,
            document.OcrResult?.ExtractedText,
            document.OcrResult?.ConfidenceScore,
            document.AiExtraction?.Classification,
            document.AiExtraction?.Summary);
    }

    /// <summary>
    /// Maps a folder aggregate to a DTO.
    /// </summary>
    private static DocumentFolderDto MapFolder(DocumentFolder folder)
    {
        return new DocumentFolderDto(folder.Id, folder.ParentFolderId, folder.Name, folder.Path, folder.CreatedBy, folder.CreatedAt);
    }
}
