namespace KmOcr.Application.Contracts.Ai;

/// <summary>
/// Result returned by an AI extraction engine.
/// </summary>
public sealed record AiEngineResult(
    string Classification,
    string Summary,
    IReadOnlyDictionary<string, string> Entities,
    decimal ConfidenceScore,
    string Engine);
