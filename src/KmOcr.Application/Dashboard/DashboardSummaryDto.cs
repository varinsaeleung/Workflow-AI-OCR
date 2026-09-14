namespace KmOcr.Application.Dashboard;

/// <summary>
/// Aggregated counts used by the dashboard.
/// </summary>
public sealed record DashboardSummaryDto(
    int TotalDocuments,
    int OcrQueued,
    int OcrCompleted,
    int InWorkflow,
    int Approved,
    int Rejected);
