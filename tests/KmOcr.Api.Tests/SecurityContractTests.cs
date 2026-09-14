using System.Reflection;
using FluentAssertions;
using KmOcr.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace KmOcr.Api.Tests;

/// <summary>
/// Verifies HTTP security metadata for enterprise APIs.
/// </summary>
public sealed class SecurityContractTests
{
    /// <summary>
    /// Ensures business API actions require permission policies.
    /// </summary>
    [Fact]
    public void Business_api_actions_should_require_permission_policies()
    {
        var controllerTypes = new[]
        {
            typeof(DocumentsController),
            typeof(FoldersController),
            typeof(OcrController),
            typeof(SearchController),
            typeof(WorkflowController),
            typeof(WorkflowDefinitionsController),
            typeof(DashboardController)
        };

        var actionsWithoutPolicies = controllerTypes
            .SelectMany(GetHttpActions)
            .Where(method => !HasPermissionPolicy(method))
            .Select(method => $"{method.DeclaringType!.Name}.{method.Name}")
            .ToList();

        actionsWithoutPolicies.Should().BeEmpty();
    }

    /// <summary>
    /// Ensures credential endpoints are anonymous while account endpoints require JWT authentication.
    /// </summary>
    [Fact]
    public void Auth_controller_should_expose_expected_authentication_metadata()
    {
        typeof(AuthController).GetMethod(nameof(AuthController.LoginAsync))!
            .GetCustomAttributes<AllowAnonymousAttribute>()
            .Should()
            .NotBeEmpty();
        typeof(AuthController).GetMethod(nameof(AuthController.RefreshAsync))!
            .GetCustomAttributes<AllowAnonymousAttribute>()
            .Should()
            .NotBeEmpty();
        typeof(AuthController).GetMethod(nameof(AuthController.MeAsync))!
            .GetCustomAttributes<AuthorizeAttribute>()
            .Should()
            .NotBeEmpty();
        typeof(AuthController).GetMethod(nameof(AuthController.LogoutAsync))!
            .GetCustomAttributes<AuthorizeAttribute>()
            .Should()
            .NotBeEmpty();
    }

    /// <summary>
    /// Gets public HTTP action methods from one controller.
    /// </summary>
    private static IEnumerable<MethodInfo> GetHttpActions(Type controllerType)
    {
        return controllerType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => method.GetCustomAttributes<HttpMethodAttribute>().Any());
    }

    /// <summary>
    /// Returns true when an action or its controller has an authorization policy.
    /// </summary>
    private static bool HasPermissionPolicy(MethodInfo method)
    {
        var actionPolicies = method.GetCustomAttributes<AuthorizeAttribute>();
        var controllerPolicies = method.DeclaringType!.GetCustomAttributes<AuthorizeAttribute>();
        return actionPolicies.Concat(controllerPolicies).Any(attribute => !string.IsNullOrWhiteSpace(attribute.Policy));
    }
}
