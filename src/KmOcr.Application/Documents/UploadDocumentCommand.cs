namespace KmOcr.Application.Documents;

/// <summary>
/// Command data required to upload and queue a document.
/// </summary>
public sealed record UploadDocumentCommand(
    string FileName,
    string ContentType,
    Stream Content,
    string UploadedBy,
    string DocumentType,
    IReadOnlyDictionary<string, string> Metadata,
    Guid? FolderId = null);
