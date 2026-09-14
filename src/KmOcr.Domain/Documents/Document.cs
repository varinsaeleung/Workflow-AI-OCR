using KmOcr.Domain.Common;

namespace KmOcr.Domain.Documents;

/// <summary>
/// Aggregate root for an uploaded enterprise document and its processing state.
/// </summary>
public sealed class Document : Entity
{
    private readonly List<DocumentMetadata> _metadata = [];
    private readonly List<DocumentVersion> _versions = [];

    /// <summary>
    /// Creates an empty document for Entity Framework.
    /// </summary>
    private Document()
    {
        FileName = string.Empty;
        ContentType = string.Empty;
        StoragePath = string.Empty;
        StorageBucket = string.Empty;
        DocumentType = string.Empty;
        UploadedBy = string.Empty;
    }

    /// <summary>
    /// Creates a document aggregate with validated upload information.
    /// </summary>
    private Document(string fileName, string contentType, string storagePath, string documentType, string uploadedBy)
    {
        FileName = RequireText(fileName, nameof(fileName));
        ContentType = RequireText(contentType, nameof(contentType));
        StoragePath = RequireText(storagePath, nameof(storagePath));
        DocumentType = RequireText(documentType, nameof(documentType));
        UploadedBy = RequireText(uploadedBy, nameof(uploadedBy));
        Status = DocumentStatus.Uploaded;
    }

    /// <summary>
    /// Creates a document aggregate with storage metadata and first version details.
    /// </summary>
    private Document(
        string fileName,
        string contentType,
        string storagePath,
        string documentType,
        string uploadedBy,
        Guid? folderId,
        string storageBucket,
        long fileSizeBytes,
        string? checksumSha256)
        : this(fileName, contentType, storagePath, documentType, uploadedBy)
    {
        FolderId = folderId;
        StorageBucket = RequireText(storageBucket, nameof(storageBucket));
        FileSizeBytes = fileSizeBytes > 0 ? fileSizeBytes : throw new ArgumentOutOfRangeException(nameof(fileSizeBytes), "File size must be positive.");
        ChecksumSha256 = string.IsNullOrWhiteSpace(checksumSha256) ? null : checksumSha256.Trim();
        CurrentVersionNumber = 1;
        _versions.Add(new DocumentVersion(Id, 1, FileName, ContentType, StoragePath, FileSizeBytes.Value, ChecksumSha256, UploadedBy));
    }

    /// <summary>
    /// Gets the original file name.
    /// </summary>
    public string FileName { get; private set; }

    /// <summary>
    /// Gets the MIME type supplied during upload.
    /// </summary>
    public string ContentType { get; private set; }

    /// <summary>
    /// Gets the storage path used by the file storage adapter.
    /// </summary>
    public string StoragePath { get; private set; }

    /// <summary>
    /// Gets the MinIO bucket used for the current version.
    /// </summary>
    public string? StorageBucket { get; private set; }

    /// <summary>
    /// Gets the folder containing the document.
    /// </summary>
    public Guid? FolderId { get; private set; }

    /// <summary>
    /// Gets the business document type such as invoice, contract, or receipt.
    /// </summary>
    public string DocumentType { get; private set; }

    /// <summary>
    /// Gets the user identifier that uploaded the document.
    /// </summary>
    public string UploadedBy { get; private set; }

    /// <summary>
    /// Gets the current file size in bytes.
    /// </summary>
    public long? FileSizeBytes { get; private set; }

    /// <summary>
    /// Gets the current SHA-256 checksum.
    /// </summary>
    public string? ChecksumSha256 { get; private set; }

    /// <summary>
    /// Gets the current active version number.
    /// </summary>
    public int CurrentVersionNumber { get; private set; } = 1;

    /// <summary>
    /// Gets the current document lifecycle state.
    /// </summary>
    public DocumentStatus Status { get; private set; }

    /// <summary>
    /// Gets the OCR result when processing has completed.
    /// </summary>
    public OcrResult? OcrResult { get; private set; }

    /// <summary>
    /// Gets the AI extraction result when AI processing has completed.
    /// </summary>
    public AiExtraction? AiExtraction { get; private set; }

    /// <summary>
    /// Gets the document metadata values.
    /// </summary>
    public IReadOnlyCollection<DocumentMetadata> Metadata => _metadata.AsReadOnly();

    /// <summary>
    /// Gets immutable binary versions for the document.
    /// </summary>
    public IReadOnlyCollection<DocumentVersion> Versions => _versions.AsReadOnly();

    /// <summary>
    /// Gets the timestamp when the document was soft-deleted.
    /// </summary>
    public DateTimeOffset? DeletedAt { get; private set; }

    /// <summary>
    /// Gets the user that soft-deleted the document.
    /// </summary>
    public string? DeletedBy { get; private set; }

    /// <summary>
    /// Creates a document aggregate for a new upload.
    /// </summary>
    public static Document Create(string fileName, string contentType, string storagePath, string documentType, string uploadedBy)
    {
        return new Document(fileName, contentType, storagePath, documentType, uploadedBy);
    }

    /// <summary>
    /// Creates a document aggregate for a new upload with version and storage metadata.
    /// </summary>
    public static Document Create(
        string fileName,
        string contentType,
        string storagePath,
        string documentType,
        string uploadedBy,
        Guid? folderId,
        string storageBucket,
        long fileSizeBytes,
        string? checksumSha256)
    {
        return new Document(fileName, contentType, storagePath, documentType, uploadedBy, folderId, storageBucket, fileSizeBytes, checksumSha256);
    }

    /// <summary>
    /// Adds a new immutable file version and makes it the current version.
    /// </summary>
    public void AddVersion(string fileName, string contentType, string storagePath, long fileSizeBytes, string? checksumSha256, string createdBy)
    {
        var nextVersion = CurrentVersionNumber + 1;
        FileName = RequireText(fileName, nameof(fileName));
        ContentType = RequireText(contentType, nameof(contentType));
        StoragePath = RequireText(storagePath, nameof(storagePath));
        FileSizeBytes = fileSizeBytes > 0 ? fileSizeBytes : throw new ArgumentOutOfRangeException(nameof(fileSizeBytes), "File size must be positive.");
        ChecksumSha256 = string.IsNullOrWhiteSpace(checksumSha256) ? null : checksumSha256.Trim();
        CurrentVersionNumber = nextVersion;
        _versions.Add(new DocumentVersion(Id, nextVersion, FileName, ContentType, StoragePath, FileSizeBytes.Value, ChecksumSha256, createdBy));
        Touch();
    }

    /// <summary>
    /// Moves the document to another folder.
    /// </summary>
    public void MoveToFolder(Guid? folderId)
    {
        FolderId = folderId;
        Touch();
    }

    /// <summary>
    /// Soft-deletes the document while keeping metadata and storage records for audit.
    /// </summary>
    public void SoftDelete(string deletedBy)
    {
        DeletedBy = RequireText(deletedBy, nameof(deletedBy));
        DeletedAt = DateTimeOffset.UtcNow;
        Status = DocumentStatus.Archived;
        Touch();
    }

    /// <summary>
    /// Moves the document to the OCR queued state.
    /// </summary>
    public void QueueOcr()
    {
        Status = DocumentStatus.OcrQueued;
        Touch();
    }

    /// <summary>
    /// Records a successful OCR run and moves the document to the OCR completed state.
    /// </summary>
    public void CompleteOcr(string extractedText, decimal confidenceScore, string engine)
    {
        if (OcrResult is null)
        {
            OcrResult = OcrResult.Create(Id, extractedText, confidenceScore, engine);
        }
        else
        {
            OcrResult.Update(extractedText, confidenceScore, engine);
        }

        Status = DocumentStatus.OcrCompleted;
        Touch();
    }

    /// <summary>
    /// Records AI extraction output for the document.
    /// </summary>
    public void CompleteAiExtraction(string classification, string summary, string extractedEntitiesJson, decimal confidenceScore, string engine)
    {
        if (AiExtraction is null)
        {
            AiExtraction = AiExtraction.Create(Id, classification, summary, extractedEntitiesJson, confidenceScore, engine);
        }
        else
        {
            AiExtraction.Update(classification, summary, extractedEntitiesJson, confidenceScore, engine);
        }

        Touch();
    }

    /// <summary>
    /// Records an OCR failure and keeps the document available for retry.
    /// </summary>
    public void FailOcr()
    {
        Status = DocumentStatus.OcrFailed;
        Touch();
    }

    /// <summary>
    /// Adds a metadata value or updates the existing value for the same key.
    /// </summary>
    public void UpsertMetadata(string key, string value)
    {
        var cleanKey = RequireText(key, nameof(key));
        var existing = _metadata.FirstOrDefault(metadata => metadata.Key.Equals(cleanKey, StringComparison.OrdinalIgnoreCase));

        if (existing is null)
        {
            _metadata.Add(new DocumentMetadata(Id, cleanKey, value));
        }
        else
        {
            existing.UpdateValue(value);
        }

        Touch();
    }

    /// <summary>
    /// Marks the document as being handled by a workflow.
    /// </summary>
    public void MoveToWorkflow()
    {
        Status = DocumentStatus.InWorkflow;
        Touch();
    }

    /// <summary>
    /// Marks the document as approved by the workflow.
    /// </summary>
    public void Approve()
    {
        Status = DocumentStatus.Approved;
        Touch();
    }

    /// <summary>
    /// Marks the document as rejected by the workflow.
    /// </summary>
    public void Reject()
    {
        Status = DocumentStatus.Rejected;
        Touch();
    }

    /// <summary>
    /// Validates required text and returns the trimmed value.
    /// </summary>
    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }
}
