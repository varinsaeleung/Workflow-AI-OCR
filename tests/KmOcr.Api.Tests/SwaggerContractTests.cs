using System.Reflection;
using FluentAssertions;
using KmOcr.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace KmOcr.Api.Tests;

/// <summary>
/// Verifies that public API actions expose Swagger response metadata.
/// </summary>
public sealed class SwaggerContractTests
{
    /// <summary>
    /// Ensures every public controller action declares at least one response type for Swagger.
    /// </summary>
    [Fact]
    public void Controller_actions_should_define_swagger_response_metadata()
    {
        var controllerTypes = new[]
        {
            typeof(AuthController),
            typeof(DocumentsController),
            typeof(FoldersController),
            typeof(OcrController),
            typeof(SearchController),
            typeof(WorkflowController),
            typeof(WorkflowDefinitionsController),
            typeof(DashboardController),
            typeof(HealthController)
        };

        var actionsWithoutResponses = controllerTypes
            .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            .Where(method => method.GetCustomAttributes<HttpMethodAttribute>().Any())
            .Where(method => !method.GetCustomAttributes<ProducesResponseTypeAttribute>().Any())
            .Select(method => $"{method.DeclaringType!.Name}.{method.Name}")
            .ToList();

        actionsWithoutResponses.Should().BeEmpty();
    }
}
