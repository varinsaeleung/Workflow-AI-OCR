using FluentAssertions;
using KmOcr.Application.Contracts.Persistence;
using KmOcr.Application.Contracts.Search;
using KmOcr.Application.Search;
using KmOcr.Domain.Documents;
using Xunit;

namespace KmOcr.Application.Tests;

/// <summary>
/// Verifies document indexing use cases.
/// </summary>
public sealed class SearchIndexingModuleTests
{
    /// <summary>
    /// Ensures document, OCR, AI, and metadata values are sent to the search index.
    /// </summary>
    [Fact]
    public async Task IndexDocumentAsync_should_index_document_with_ocr_ai_and_metadata()
    {
        var document = Document.Create("invoice.pdf", "application/pdf", "minio://documents/invoice.pdf", "invoice", "admin@km.local");
        document.UpsertMetadata("vendor", "Konica Minolta");
        document.CompleteOcr("Tax invoice total THB 1,200.00", 0.94m, "paddleocr");
        document.CompleteAiExtraction("invoice", "Invoice summary", "{\"amount\":\"THB 1,200.00\"}", 0.88m, "ollama:llama3.1");
        var repository = new FakeDocumentRepository(document);
        var index = new CapturingSearchIndex();
        var module = new SearchIndexingModule(repository, index);

        await module.IndexDocumentAsync(document.Id, CancellationToken.None);

        index.IndexedDocument.Should().NotBeNull();
        index.IndexedDocument!.FileName.Should().Be("invoice.pdf");
        index.IndexedDocument.OcrText.Should().Be("Tax invoice total THB 1,200.00");
        index.IndexedDocument.AiSummary.Should().Be("Invoice summary");
        index.IndexedDocument.Metadata["vendor"].Should().Be("Konica Minolta");
    }

    /// <summary>
    /// Ensures deleted documents are removed from the search index.
    /// </summary>
    [Fact]
    public async Task IndexDocumentAsync_should_delete_index_entry_when_document_is_deleted()
    {
        var document = Document.Create("deleted.pdf", "application/pdf", "minio://documents/deleted.pdf", "invoice", "admin@km.local");
        document.SoftDelete("admin@km.local");
        var repository = new FakeDocumentRepository(document);
        var index = new CapturingSearchIndex();
        var module = new SearchIndexingModule(repository, index);

        await module.IndexDocumentAsync(document.Id, CancellationToken.None);

        index.DeletedDocumentId.Should().Be(document.Id);
        index.IndexedDocument.Should().BeNull();
    }

    /// <summary>
    /// Provides one document aggregate to indexing use case tests.
    /// </summary>
    private sealed class FakeDocumentRepository : IDocumentRepository
    {
        private readonly Document _document;

        /// <summary>
        /// Creates the repository with one stored document.
        /// </summary>
        public FakeDocumentRepository(Document document)
        {
            _document = document;
        }

        /// <summary>
        /// Accepts document add operations that are not used by these tests.
        /// </summary>
        public Task AddAsync(Document document, CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>
        /// Accepts folder add operations that are not used by these tests.
        /// </summary>
        public Task AddFolderAsync(DocumentFolder folder, CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>
        /// Returns the stored document when ids match.
        /// </summary>
        public Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(id == _document.Id ? _document : null);

        /// <summary>
        /// Returns no folder for indexing tests.
        /// </summary>
        public Task<DocumentFolder?> GetFolderByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<DocumentFolder?>(null);

        /// <summary>
        /// Returns no folders for indexing tests.
        /// </summary>
        public Task<IReadOnlyList<DocumentFolder>> ListFoldersAsync(Guid? parentFolderId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<DocumentFolder>>([]);

        /// <summary>
        /// Returns the stored document for repository search tests.
        /// </summary>
        public Task<IReadOnlyList<Document>> SearchAsync(string? query, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Document>>([_document]);

        /// <summary>
        /// Returns an empty status count for indexing tests.
        /// </summary>
        public Task<IReadOnlyDictionary<DocumentStatus, int>> CountByStatusAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyDictionary<DocumentStatus, int>>(new Dictionary<DocumentStatus, int>());
    }

    /// <summary>
    /// Captures index operations for indexing module tests.
    /// </summary>
    private sealed class CapturingSearchIndex : IDocumentSearchIndex
    {
        /// <summary>
        /// Gets the latest indexed document.
        /// </summary>
        public DocumentIndexDto? IndexedDocument { get; private set; }

        /// <summary>
        /// Gets the latest deleted document id.
        /// </summary>
        public Guid? DeletedDocumentId { get; private set; }

        /// <summary>
        /// Accepts index creation calls for tests.
        /// </summary>
        public Task EnsureIndexAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>
        /// Captures the indexed document.
        /// </summary>
        public Task IndexAsync(DocumentIndexDto document, CancellationToken cancellationToken)
        {
            IndexedDocument = document;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Captures the deleted document id.
        /// </summary>
        public Task DeleteAsync(Guid documentId, CancellationToken cancellationToken)
        {
            DeletedDocumentId = documentId;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Returns no search results because indexing tests do not search.
        /// </summary>
        public Task<DocumentSearchResponseDto> SearchAsync(SearchDocumentsQuery query, CancellationToken cancellationToken) => Task.FromResult(new DocumentSearchResponseDto(0, []));
    }
}
