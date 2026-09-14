namespace KmOcr.Application.Documents;

/// <summary>
/// Command data required to download a document version.
/// </summary>
public sealed record DownloadDocumentCommand(Guid DocumentId, int? VersionNumber);
