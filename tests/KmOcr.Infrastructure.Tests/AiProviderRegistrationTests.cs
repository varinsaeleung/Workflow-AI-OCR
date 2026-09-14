using FluentAssertions;
using KmOcr.Application.Contracts.Ai;
using KmOcr.Infrastructure;
using KmOcr.Infrastructure.Ai;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KmOcr.Infrastructure.Tests;

/// <summary>
/// Verifies AI provider dependency injection registration behavior.
/// </summary>
public sealed class AiProviderRegistrationTests
{
    /// <summary>
    /// Ensures configuration can switch the AI engine implementation to Ollama.
    /// </summary>
    [Fact]
    public void AddInfrastructure_should_register_ollama_ai_engine_when_provider_is_ollama()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = "Host=localhost;Database=km_ocr;Username=km_ocr;Password=km_ocr_password",
                ["Ai:Provider"] = "Ollama",
                ["Ollama:BaseUrl"] = "http://localhost:11434",
                ["Ollama:Model"] = "qwen2.5",
                ["Ollama:TimeoutSeconds"] = "30"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IAiEngine>().Should().BeOfType<OllamaAiEngine>();
    }
}
