namespace KmOcr.Application.Documents;

/// <summary>
/// Command data required to update document metadata.
/// </summary>
public sealed record UpdateMetadataCommand(
    Guid DocumentId,
    IReadOnlyDictionary<string, string> Metadata,
    string Actor);
