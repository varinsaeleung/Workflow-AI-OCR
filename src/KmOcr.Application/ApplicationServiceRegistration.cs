using KmOcr.Application.Ai;
using KmOcr.Application.Auth;
using KmOcr.Application.Dashboard;
using KmOcr.Application.Documents;
using KmOcr.Application.Ocr;
using KmOcr.Application.Search;
using KmOcr.Application.WorkflowEngine;
using KmOcr.Application.Workflows;
using Microsoft.Extensions.DependencyInjection;

namespace KmOcr.Application;

/// <summary>
/// Registers application-layer modules with the dependency injection container.
/// </summary>
public static class ApplicationServiceRegistration
{
    /// <summary>
    /// Adds all application services to the service collection.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IDocumentModule, DocumentModule>();
        services.AddScoped<IAiModule, AiModule>();
        services.AddScoped<IAuthModule, AuthModule>();
        services.AddScoped<IOcrModule, OcrModule>();
        services.AddScoped<ISearchModule, SearchModule>();
        services.AddScoped<ISearchIndexingModule, SearchIndexingModule>();
        services.AddScoped<IWorkflowDesignerModule, WorkflowDesignerModule>();
        services.AddScoped<IWorkflowModule, WorkflowModule>();
        services.AddScoped<IDashboardModule, DashboardModule>();
        return services;
    }
}
