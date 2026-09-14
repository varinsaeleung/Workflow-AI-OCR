using KmOcr.Application.Contracts.Persistence;
using KmOcr.Application.Dashboard;
using KmOcr.Domain.Auth;
using KmOcr.Domain.Documents;
using KmOcr.Domain.Workflows;
using KmOcr.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KmOcr.Infrastructure.Repositories;

/// <summary>
/// Entity Framework read repository for enterprise dashboard metrics.
/// </summary>
public sealed class DashboardMetricsRepository : IDashboardMetricsRepository
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Creates the repository with the application database context.
    /// </summary>
    public DashboardMetricsRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Aggregates OCR, workflow, user, error, performance, and storage metrics.
    /// </summary>
    public async Task<EnterpriseDashboardDto> GetEnterpriseAsync(CancellationToken cancellationToken)
    {
        var documentMetrics = await _context.Documents
            .GroupBy(document => document.Status)
            .Select(group => new
            {
                Status = group.Key,
                Count = group.Count(),
                TotalBytes = group.Sum(document => document.FileSizeBytes ?? 0)
            })
            .ToListAsync(cancellationToken);
        var statusCounts = documentMetrics.ToDictionary(item => item.Status, item => item.Count);
        var totalDocuments = statusCounts.Values.Sum();
        var totalBytes = documentMetrics.Sum(item => item.TotalBytes);
        var ocrCompleted = GetDocumentStatusCount(statusCounts, DocumentStatus.OcrCompleted);
        var ocrFailed = GetDocumentStatusCount(statusCounts, DocumentStatus.OcrFailed);
        var ocrQueued = GetDocumentStatusCount(statusCounts, DocumentStatus.OcrQueued);
        var workflowStatusCounts = await _context.WorkflowTasks
            .GroupBy(task => task.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Status, item => item.Count, cancellationToken);
        var userStatusCounts = await _context.Users
            .GroupBy(user => user.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Status, item => item.Count, cancellationToken);
        var activeUsers = GetUserStatusCount(userStatusCounts, UserStatus.Active);
        var lockedUsers = userStatusCounts.Where(item => item.Key != UserStatus.Active).Sum(item => item.Value);
        var roleCount = await _context.Roles.CountAsync(role => role.IsActive, cancellationToken);
        var versionCount = await _context.DocumentVersions.CountAsync(cancellationToken);
        var averageConfidence = await ReadAverageOcrConfidenceAsync(cancellationToken);
        var throughput = await ReadThroughputAsync(cancellationToken);

        return new EnterpriseDashboardDto(
            new OcrDashboardDto(totalDocuments, ocrQueued, ocrCompleted, ocrFailed, CalculateRate(ocrCompleted, totalDocuments)),
            new WorkflowDashboardDto(GetDocumentStatusCount(statusCounts, DocumentStatus.InWorkflow), GetWorkflowStatusCount(workflowStatusCounts, WorkflowTaskStatus.Approved), GetWorkflowStatusCount(workflowStatusCounts, WorkflowTaskStatus.Rejected), GetWorkflowStatusCount(workflowStatusCounts, WorkflowTaskStatus.Pending)),
            new UserDashboardDto(activeUsers, lockedUsers, roleCount),
            new ErrorDashboardDto(ocrFailed, ocrFailed, 0),
            new PerformanceDashboardDto(averageConfidence, throughput.Sum(point => point.Value), 0),
            new StorageDashboardDto(totalDocuments, totalBytes, versionCount),
            throughput);
    }

    /// <summary>
    /// Reads one document status count from an aggregate dictionary.
    /// </summary>
    private static int GetDocumentStatusCount(IReadOnlyDictionary<DocumentStatus, int> counts, DocumentStatus status)
    {
        return counts.TryGetValue(status, out var count) ? count : 0;
    }

    /// <summary>
    /// Reads one workflow task status count from an aggregate dictionary.
    /// </summary>
    private static int GetWorkflowStatusCount(IReadOnlyDictionary<WorkflowTaskStatus, int> counts, WorkflowTaskStatus status)
    {
        return counts.TryGetValue(status, out var count) ? count : 0;
    }

    /// <summary>
    /// Reads one user status count from an aggregate dictionary.
    /// </summary>
    private static int GetUserStatusCount(IReadOnlyDictionary<UserStatus, int> counts, UserStatus status)
    {
        return counts.TryGetValue(status, out var count) ? count : 0;
    }

    /// <summary>
    /// Calculates a percentage rate from a value and total.
    /// </summary>
    private static decimal CalculateRate(int value, int total)
    {
        return total == 0 ? 0 : Math.Round((decimal)value / total * 100, 2);
    }

    /// <summary>
    /// Reads average OCR confidence while avoiding null aggregate results.
    /// </summary>
    private async Task<decimal> ReadAverageOcrConfidenceAsync(CancellationToken cancellationToken)
    {
        var average = await _context.OcrResults
            .Select(result => (decimal?)result.ConfidenceScore)
            .AverageAsync(cancellationToken);

        return average.HasValue ? Math.Round(average.Value, 2) : 0;
    }

    /// <summary>
    /// Reads document upload throughput by UTC date.
    /// </summary>
    private async Task<IReadOnlyList<DashboardChartPointDto>> ReadThroughputAsync(CancellationToken cancellationToken)
    {
        var documents = await _context.Documents
            .Select(document => document.CreatedAt)
            .ToListAsync(cancellationToken);

        return documents
            .GroupBy(createdAt => createdAt.UtcDateTime.Date)
            .OrderBy(group => group.Key)
            .Select(group => new DashboardChartPointDto(group.Key.ToString("yyyy-MM-dd"), group.Count()))
            .ToList();
    }
}
