namespace KmOcr.Application.Documents;

/// <summary>
/// Client-safe folder representation returned by APIs.
/// </summary>
public sealed record DocumentFolderDto(Guid Id, Guid? ParentFolderId, string Name, string Path, string CreatedBy, DateTimeOffset CreatedAt);
