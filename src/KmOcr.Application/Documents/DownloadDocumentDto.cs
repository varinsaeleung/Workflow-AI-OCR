namespace KmOcr.Application.Documents;

/// <summary>
/// Download result containing file metadata and content stream.
/// </summary>
public sealed record DownloadDocumentDto(string FileName, string ContentType, Stream Content);
