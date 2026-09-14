namespace KmOcr.Application.Ai;

/// <summary>
/// Client-safe AI extraction representation returned by APIs.
/// </summary>
public sealed record AiExtractionDto(
    Guid DocumentId,
    string? Classification,
    string? Summary,
    IReadOnlyDictionary<string, string> Entities,
    decimal? ConfidenceScore,
    string? Engine);
