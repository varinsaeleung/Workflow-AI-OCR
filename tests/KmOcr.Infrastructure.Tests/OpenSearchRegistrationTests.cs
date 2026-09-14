using FluentAssertions;
using KmOcr.Application.Contracts.Search;
using KmOcr.Infrastructure;
using KmOcr.Infrastructure.Search;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KmOcr.Infrastructure.Tests;

/// <summary>
/// Verifies OpenSearch dependency injection registration.
/// </summary>
public sealed class OpenSearchRegistrationTests
{
    /// <summary>
    /// Ensures the document search index abstraction resolves to the OpenSearch adapter.
    /// </summary>
    [Fact]
    public void AddInfrastructure_should_register_opensearch_document_index()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = "Host=localhost;Database=km_ocr;Username=km_ocr;Password=km_ocr_password",
                ["OpenSearch:Url"] = "http://localhost:9200",
                ["OpenSearch:IndexName"] = "km-documents"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IDocumentSearchIndex>().Should().BeOfType<OpenSearchDocumentIndex>();
    }
}
