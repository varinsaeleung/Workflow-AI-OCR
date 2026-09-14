using KmOcr.Application.Contracts.Ai;
using KmOcr.Application.Contracts.Messaging;
using KmOcr.Application.Contracts.Ocr;
using KmOcr.Application.Contracts.Persistence;
using KmOcr.Application.Contracts.Security;
using KmOcr.Application.Contracts.Search;
using KmOcr.Application.Contracts.Storage;
using KmOcr.Infrastructure.Ai;
using KmOcr.Infrastructure.Messaging;
using KmOcr.Infrastructure.Ocr;
using KmOcr.Infrastructure.Persistence;
using KmOcr.Infrastructure.Repositories;
using KmOcr.Infrastructure.Security;
using KmOcr.Infrastructure.Search;
using KmOcr.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KmOcr.Infrastructure;

/// <summary>
/// Registers infrastructure adapters with the dependency injection container.
/// </summary>
public static class InfrastructureServiceRegistration
{
    /// <summary>
    /// Adds PostgreSQL, repositories, storage, and messaging adapters.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required.");

        services.Configure<LocalFileStorageOptions>(configuration.GetSection("Storage"));
        services.Configure<MinioStorageOptions>(configuration.GetSection("Minio"));
        services.Configure<RabbitMqOptions>(configuration.GetSection("RabbitMq"));
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.Configure<PaddleOcrOptions>(configuration.GetSection("PaddleOcr"));
        services.Configure<OllamaAiOptions>(configuration.GetSection("Ollama"));
        services.Configure<OpenSearchOptions>(configuration.GetSection("OpenSearch"));
        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IWorkflowRepository, WorkflowRepository>();
        services.AddScoped<IWorkflowDefinitionRepository, WorkflowDefinitionRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IDashboardMetricsRepository, DashboardMetricsRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddHttpClient<OllamaAiEngine>();
        services.AddHttpClient<OpenSearchDocumentIndex>();
        services.AddScoped<KeywordAiEngine>();
        services.AddScoped<IAiEngine>(provider => CreateAiEngine(provider, configuration));
        services.AddScoped<IDocumentSearchIndex>(provider => provider.GetRequiredService<OpenSearchDocumentIndex>());
        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddSingleton<IProcessRunner, ProcessRunner>();
        services.AddScoped<IOcrEngine, PaddleOcrEngine>();
        services.AddScoped<IFileStorage, MinioFileStorage>();
        services.AddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();
        return services;
    }

    /// <summary>
    /// Creates the configured AI engine provider from application configuration.
    /// </summary>
    private static IAiEngine CreateAiEngine(IServiceProvider provider, IConfiguration configuration)
    {
        var providerName = configuration["Ai:Provider"] ?? "Keyword";

        return providerName.Equals("Ollama", StringComparison.OrdinalIgnoreCase)
            ? provider.GetRequiredService<OllamaAiEngine>()
            : provider.GetRequiredService<KeywordAiEngine>();
    }
}
