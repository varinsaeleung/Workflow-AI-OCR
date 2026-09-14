namespace KmOcr.Application.Documents;

/// <summary>
/// Command data required to move a document to a folder.
/// </summary>
public sealed record MoveDocumentCommand(Guid DocumentId, Guid? FolderId, string Actor);
