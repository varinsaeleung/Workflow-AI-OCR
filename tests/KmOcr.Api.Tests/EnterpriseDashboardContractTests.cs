using FluentAssertions;
using KmOcr.Api.Controllers;
using KmOcr.Application.Dashboard;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace KmOcr.Api.Tests;

/// <summary>
/// Verifies enterprise dashboard API contract metadata.
/// </summary>
public sealed class EnterpriseDashboardContractTests
{
    /// <summary>
    /// Ensures the enterprise dashboard endpoint advertises its Swagger response type.
    /// </summary>
    [Fact]
    public void EnterpriseAsync_should_produce_enterprise_dashboard_response()
    {
        var attribute = typeof(DashboardController)
            .GetMethod(nameof(DashboardController.EnterpriseAsync))!
            .GetCustomAttributes(typeof(ProducesResponseTypeAttribute), false)
            .Cast<ProducesResponseTypeAttribute>()
            .Single(item => item.StatusCode == 200);

        attribute.Type.Should().Be(typeof(EnterpriseDashboardDto));
    }
}
