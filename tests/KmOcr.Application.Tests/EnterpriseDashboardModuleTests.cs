using FluentAssertions;
using KmOcr.Application.Contracts.Persistence;
using KmOcr.Application.Dashboard;
using KmOcr.Domain.Documents;
using Xunit;

namespace KmOcr.Application.Tests;

/// <summary>
/// Verifies enterprise dashboard application use cases.
/// </summary>
public sealed class EnterpriseDashboardModuleTests
{
    /// <summary>
    /// Ensures the dashboard module returns enterprise OCR, workflow, user, error, performance, and storage metrics.
    /// </summary>
    [Fact]
    public async Task GetEnterpriseAsync_should_return_enterprise_dashboard_metrics()
    {
        var repository = new FakeDashboardMetricsRepository();
        var module = new DashboardModule(new EmptyDocumentRepository(), repository);

        var dashboard = await module.GetEnterpriseAsync(CancellationToken.None);

        dashboard.Ocr.Completed.Should().Be(8);
        dashboard.Workflow.PendingTasks.Should().Be(3);
        dashboard.Users.ActiveUsers.Should().Be(12);
        dashboard.Errors.TotalErrors.Should().Be(2);
        dashboard.Performance.AverageOcrConfidence.Should().Be(0.91m);
        dashboard.Storage.TotalBytes.Should().Be(2048);
        dashboard.Throughput.Should().ContainSingle(point => point.Label == "2026-09-11" && point.Value == 5);
    }

    /// <summary>
    /// Returns deterministic enterprise dashboard metrics.
    /// </summary>
    private sealed class FakeDashboardMetricsRepository : IDashboardMetricsRepository
    {
        /// <summary>
        /// Returns a complete dashboard fixture for application tests.
        /// </summary>
        public Task<EnterpriseDashboardDto> GetEnterpriseAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new EnterpriseDashboardDto(
                new OcrDashboardDto(10, 1, 8, 1, 80),
                new WorkflowDashboardDto(4, 7, 2, 3),
                new UserDashboardDto(12, 1, 4),
                new ErrorDashboardDto(2, 1, 1),
                new PerformanceDashboardDto(0.91m, 14, 280),
                new StorageDashboardDto(20, 2048, 25),
                new[] { new DashboardChartPointDto("2026-09-11", 5) }));
        }
    }

    /// <summary>
    /// Provides legacy document counts required by the existing summary method.
    /// </summary>
    private sealed class EmptyDocumentRepository : IDocumentRepository
    {
        /// <summary>
        /// Accepts add operations that are not used by dashboard tests.
        /// </summary>
        public Task AddAsync(Document document, CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>
        /// Accepts folder add operations that are not used by dashboard tests.
        /// </summary>
        public Task AddFolderAsync(DocumentFolder folder, CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>
        /// Returns no document for dashboard tests.
        /// </summary>
        public Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<Document?>(null);

        /// <summary>
        /// Returns no folder for dashboard tests.
        /// </summary>
        public Task<DocumentFolder?> GetFolderByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<DocumentFolder?>(null);

        /// <summary>
        /// Returns no folders for dashboard tests.
        /// </summary>
        public Task<IReadOnlyList<DocumentFolder>> ListFoldersAsync(Guid? parentFolderId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<DocumentFolder>>([]);

        /// <summary>
        /// Returns no documents for dashboard tests.
        /// </summary>
        public Task<IReadOnlyList<Document>> SearchAsync(string? query, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Document>>([]);

        /// <summary>
        /// Returns no status counts for dashboard tests.
        /// </summary>
        public Task<IReadOnlyDictionary<DocumentStatus, int>> CountByStatusAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyDictionary<DocumentStatus, int>>(new Dictionary<DocumentStatus, int>());
    }
}
