namespace KmOcr.Application.Documents;

/// <summary>
/// Command data required to create a document folder.
/// </summary>
public sealed record CreateFolderCommand(string Name, Guid? ParentFolderId, string CreatedBy);
