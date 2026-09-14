using FluentAssertions;
using KmOcr.Domain.Documents;
using KmOcr.Infrastructure.Persistence;
using KmOcr.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KmOcr.Infrastructure.Tests;

/// <summary>
/// Verifies Entity Framework repository behavior with an in-memory provider.
/// </summary>
public sealed class DocumentRepositoryTests
{
    /// <summary>
    /// Ensures repository search returns persisted documents by file name.
    /// </summary>
    [Fact]
    public async Task SearchAsync_should_return_documents_matching_file_name()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ApplicationDbContext(options);
        var repository = new DocumentRepository(context);
        var document = Document.Create(
            "invoice-001.pdf",
            "application/pdf",
            "local://documents/invoice-001.pdf",
            "invoice",
            "user-001");
        await repository.AddAsync(document, CancellationToken.None);
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await repository.SearchAsync("invoice", CancellationToken.None);

        result.Should().ContainSingle(item => item.Id == document.Id);
    }
}
