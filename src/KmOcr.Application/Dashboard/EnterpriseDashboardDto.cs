namespace KmOcr.Application.Dashboard;

/// <summary>
/// Enterprise dashboard read model for OCR, workflow, user, error, performance, and storage charts.
/// </summary>
public sealed record EnterpriseDashboardDto(
    OcrDashboardDto Ocr,
    WorkflowDashboardDto Workflow,
    UserDashboardDto Users,
    ErrorDashboardDto Errors,
    PerformanceDashboardDto Performance,
    StorageDashboardDto Storage,
    IReadOnlyList<DashboardChartPointDto> Throughput);

/// <summary>
/// OCR processing metrics shown on the enterprise dashboard.
/// </summary>
public sealed record OcrDashboardDto(int Total, int Queued, int Completed, int Failed, decimal CompletionRate);

/// <summary>
/// Workflow processing metrics shown on the enterprise dashboard.
/// </summary>
public sealed record WorkflowDashboardDto(int InProgress, int Approved, int Rejected, int PendingTasks);

/// <summary>
/// User and role metrics shown on the enterprise dashboard.
/// </summary>
public sealed record UserDashboardDto(int ActiveUsers, int LockedUsers, int RoleCount);

/// <summary>
/// Error metrics shown on the enterprise dashboard.
/// </summary>
public sealed record ErrorDashboardDto(int TotalErrors, int OcrErrors, int AiErrors);

/// <summary>
/// Performance metrics shown on the enterprise dashboard.
/// </summary>
public sealed record PerformanceDashboardDto(decimal AverageOcrConfidence, int DocumentsPerDay, int AverageProcessingSeconds);

/// <summary>
/// Storage metrics shown on the enterprise dashboard.
/// </summary>
public sealed record StorageDashboardDto(int DocumentCount, long TotalBytes, int VersionCount);

/// <summary>
/// Generic dashboard chart point used by frontend chart components.
/// </summary>
public sealed record DashboardChartPointDto(string Label, int Value);
