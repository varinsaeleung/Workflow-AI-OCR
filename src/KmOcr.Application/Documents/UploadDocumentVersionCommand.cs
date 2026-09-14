namespace KmOcr.Application.Documents;

/// <summary>
/// Command data required to upload a new document version.
/// </summary>
public sealed record UploadDocumentVersionCommand(Guid DocumentId, string FileName, string ContentType, Stream Content, string CreatedBy);
