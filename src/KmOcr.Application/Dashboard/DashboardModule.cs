using KmOcr.Application.Contracts.Persistence;
using KmOcr.Domain.Documents;

namespace KmOcr.Application.Dashboard;

/// <summary>
/// Implements dashboard read use cases.
/// </summary>
public sealed class DashboardModule : IDashboardModule
{
    private readonly IDocumentRepository _documents;
    private readonly IDashboardMetricsRepository _dashboardMetrics;

    /// <summary>
    /// Creates the dashboard module with document read access.
    /// </summary>
    public DashboardModule(IDocumentRepository documents, IDashboardMetricsRepository dashboardMetrics)
    {
        _documents = documents;
        _dashboardMetrics = dashboardMetrics;
    }

    /// <summary>
    /// Returns processing counts grouped by status.
    /// </summary>
    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var counts = await _documents.CountByStatusAsync(cancellationToken);
        var total = counts.Values.Sum();

        return new DashboardSummaryDto(
            total,
            GetCount(counts, DocumentStatus.OcrQueued),
            GetCount(counts, DocumentStatus.OcrCompleted),
            GetCount(counts, DocumentStatus.InWorkflow),
            GetCount(counts, DocumentStatus.Approved),
            GetCount(counts, DocumentStatus.Rejected));
    }

    /// <summary>
    /// Returns enterprise dashboard metrics for charts and operational monitoring.
    /// </summary>
    public Task<EnterpriseDashboardDto> GetEnterpriseAsync(CancellationToken cancellationToken)
    {
        return _dashboardMetrics.GetEnterpriseAsync(cancellationToken);
    }

    /// <summary>
    /// Reads one status count from the grouped count dictionary.
    /// </summary>
    private static int GetCount(IReadOnlyDictionary<DocumentStatus, int> counts, DocumentStatus status)
    {
        return counts.TryGetValue(status, out var count) ? count : 0;
    }
}
