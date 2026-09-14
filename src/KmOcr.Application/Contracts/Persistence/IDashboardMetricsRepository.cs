using KmOcr.Application.Dashboard;

namespace KmOcr.Application.Contracts.Persistence;

/// <summary>
/// Repository contract for enterprise dashboard read-model metrics.
/// </summary>
public interface IDashboardMetricsRepository
{
    /// <summary>
    /// Loads enterprise dashboard metrics from the persistence layer.
    /// </summary>
    Task<EnterpriseDashboardDto> GetEnterpriseAsync(CancellationToken cancellationToken);
}
