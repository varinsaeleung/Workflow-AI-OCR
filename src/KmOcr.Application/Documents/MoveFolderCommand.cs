namespace KmOcr.Application.Documents;

/// <summary>
/// Command data required to move a folder.
/// </summary>
public sealed record MoveFolderCommand(Guid FolderId, Guid? ParentFolderId, string Actor);
