using FluentAssertions;
using KmOcr.Domain.Auth;
using KmOcr.Domain.Documents;
using KmOcr.Domain.Workflows;
using KmOcr.Infrastructure.Persistence;
using KmOcr.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KmOcr.Infrastructure.Tests;

/// <summary>
/// Verifies enterprise dashboard metric aggregation from persistence.
/// </summary>
public sealed class DashboardMetricsRepositoryTests
{
    /// <summary>
    /// Ensures dashboard metrics aggregate document, workflow, user, and storage records.
    /// </summary>
    [Fact]
    public async Task GetEnterpriseAsync_should_aggregate_enterprise_metrics()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ApplicationDbContext(options);
        var document = Document.Create(
            "invoice.pdf",
            "application/pdf",
            "minio://documents/invoice.pdf",
            "invoice",
            "admin@km.local",
            null,
            "documents",
            2048,
            null);
        document.CompleteOcr("Tax invoice", 0.90m, "paddleocr");
        context.Documents.Add(document);
        context.WorkflowTasks.Add(WorkflowTask.Create(Guid.NewGuid(), "Approval", "admin@km.local"));
        context.Users.Add(User.Create("active@km.local", "Active User", "hash"));
        var suspended = User.Create("locked@km.local", "Locked User", "hash");
        suspended.Suspend();
        context.Users.Add(suspended);
        context.Roles.Add(Role.Create("admin", "Administrator", true));
        await context.SaveChangesAsync(CancellationToken.None);
        var repository = new DashboardMetricsRepository(context);

        var dashboard = await repository.GetEnterpriseAsync(CancellationToken.None);

        dashboard.Ocr.Completed.Should().Be(1);
        dashboard.Workflow.PendingTasks.Should().Be(1);
        dashboard.Users.ActiveUsers.Should().Be(1);
        dashboard.Users.LockedUsers.Should().Be(1);
        dashboard.Users.RoleCount.Should().Be(1);
        dashboard.Storage.TotalBytes.Should().Be(2048);
        dashboard.Storage.VersionCount.Should().Be(1);
        dashboard.Performance.AverageOcrConfidence.Should().Be(0.90m);
    }
}
