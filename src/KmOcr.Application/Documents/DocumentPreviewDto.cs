namespace KmOcr.Application.Documents;

/// <summary>
/// Preview result containing a time-limited access URL.
/// </summary>
public sealed record DocumentPreviewDto(Guid DocumentId, string Url, DateTimeOffset ExpiresAt);
