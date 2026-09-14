namespace KmOcr.Application.Search;

/// <summary>
/// Search response containing total count and highlighted document hits.
/// </summary>
public sealed record DocumentSearchResponseDto(long Total, IReadOnlyList<DocumentSearchResultDto> Items);
