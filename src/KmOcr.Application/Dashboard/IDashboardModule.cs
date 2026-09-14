namespace KmOcr.Application.Dashboard;

/// <summary>
/// Application module that exposes dashboard use cases.
/// </summary>
public interface IDashboardModule
{
    /// <summary>
    /// Returns a document processing summary for dashboard cards.
    /// </summary>
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns enterprise dashboard metrics for charts and operations monitoring.
    /// </summary>
    Task<EnterpriseDashboardDto> GetEnterpriseAsync(CancellationToken cancellationToken);
}
