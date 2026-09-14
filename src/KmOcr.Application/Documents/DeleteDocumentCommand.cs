namespace KmOcr.Application.Documents;

/// <summary>
/// Command data required to soft-delete a document.
/// </summary>
public sealed record DeleteDocumentCommand(Guid DocumentId, string Actor);
