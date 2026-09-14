using FluentAssertions;
using KmOcr.Application.Common;
using KmOcr.Application.Contracts.Messaging;
using KmOcr.Application.Contracts.Persistence;
using KmOcr.Application.Contracts.Storage;
using KmOcr.Application.Documents;
using KmOcr.Domain.Documents;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace KmOcr.Application.Tests;

/// <summary>
/// Tests enterprise document storage use cases.
/// </summary>
public sealed class DocumentStorageModuleTests
{
    /// <summary>
    /// Ensures upload stores binary content, creates metadata, and records folder placement.
    /// </summary>
    [Fact]
    public async Task UploadAsync_should_store_document_in_folder_with_version_one()
    {
        var repository = new InMemoryDocumentRepository();
        var storage = new CapturingFileStorage();
        var module = CreateModule(repository, storage);
        await using var content = new MemoryStream([1, 2, 3]);
        var folderId = Guid.NewGuid();

        var document = await module.UploadAsync(new UploadDocumentCommand(
            "invoice.pdf",
            "application/pdf",
            content,
            "admin@km.local",
            "invoice",
            new Dictionary<string, string> { ["vendor"] = "KM" },
            folderId),
            CancellationToken.None);

        document.FolderId.Should().Be(folderId);
        document.CurrentVersionNumber.Should().Be(1);
        document.Metadata.Should().ContainKey("vendor");
        storage.SavedFiles.Should().ContainSingle(file => file.FileName == "invoice.pdf");
    }

    /// <summary>
    /// Ensures uploading a new version advances the current version and storage object.
    /// </summary>
    [Fact]
    public async Task UploadVersionAsync_should_advance_current_version()
    {
        var repository = new InMemoryDocumentRepository();
        var storage = new CapturingFileStorage();
        var module = CreateModule(repository, storage);
        var document = Document.Create("invoice.pdf", "application/pdf", "minio://documents/one.pdf", "invoice", "admin@km.local", Guid.NewGuid(), "documents", 3, null);
        await repository.AddAsync(document, CancellationToken.None);
        await using var content = new MemoryStream([4, 5, 6, 7]);

        var updated = await module.UploadVersionAsync(new UploadDocumentVersionCommand(
            document.Id,
            "invoice-v2.pdf",
            "application/pdf",
            content,
            "admin@km.local"),
            CancellationToken.None);

        updated.CurrentVersionNumber.Should().Be(2);
        updated.StoragePath.Should().Contain("invoice-v2.pdf");
    }

    /// <summary>
    /// Ensures download opens the current stored object and returns file metadata.
    /// </summary>
    [Fact]
    public async Task DownloadAsync_should_return_current_file_stream()
    {
        var repository = new InMemoryDocumentRepository();
        var storage = new CapturingFileStorage();
        var module = CreateModule(repository, storage);
        var document = Document.Create("invoice.pdf", "application/pdf", "minio://documents/one.pdf", "invoice", "admin@km.local", Guid.NewGuid(), "documents", 3, null);
        await repository.AddAsync(document, CancellationToken.None);

        var download = await module.DownloadAsync(new DownloadDocumentCommand(document.Id, null), CancellationToken.None);

        download.FileName.Should().Be("invoice.pdf");
        download.ContentType.Should().Be("application/pdf");
        download.Content.ReadByte().Should().Be(9);
    }

    /// <summary>
    /// Ensures preview requests are delegated to storage and keep the document id.
    /// </summary>
    [Fact]
    public async Task PreviewAsync_should_return_preview_url()
    {
        var repository = new InMemoryDocumentRepository();
        var storage = new CapturingFileStorage();
        var module = CreateModule(repository, storage);
        var document = Document.Create("invoice.pdf", "application/pdf", "minio://documents/one.pdf", "invoice", "admin@km.local", Guid.NewGuid(), "documents", 3, null);
        await repository.AddAsync(document, CancellationToken.None);

        var preview = await module.PreviewAsync(document.Id, CancellationToken.None);

        preview.DocumentId.Should().Be(document.Id);
        preview.Url.Should().Be("https://minio.local/preview");
    }

    /// <summary>
    /// Ensures move changes the document folder and persists through unit of work.
    /// </summary>
    [Fact]
    public async Task MoveAsync_should_update_folder()
    {
        var repository = new InMemoryDocumentRepository();
        var module = CreateModule(repository, new CapturingFileStorage());
        var document = Document.Create("invoice.pdf", "application/pdf", "minio://documents/one.pdf", "invoice", "admin@km.local", Guid.NewGuid(), "documents", 3, null);
        await repository.AddAsync(document, CancellationToken.None);
        var targetFolderId = Guid.NewGuid();

        var moved = await module.MoveAsync(new MoveDocumentCommand(document.Id, targetFolderId, "admin@km.local"), CancellationToken.None);

        moved.FolderId.Should().Be(targetFolderId);
    }

    /// <summary>
    /// Ensures delete is a soft delete and does not remove the MinIO object.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_should_soft_delete_document()
    {
        var repository = new InMemoryDocumentRepository();
        var storage = new CapturingFileStorage();
        var module = CreateModule(repository, storage);
        var document = Document.Create("invoice.pdf", "application/pdf", "minio://documents/one.pdf", "invoice", "admin@km.local", Guid.NewGuid(), "documents", 3, null);
        await repository.AddAsync(document, CancellationToken.None);

        await module.DeleteAsync(new DeleteDocumentCommand(document.Id, "admin@km.local"), CancellationToken.None);

        document.DeletedAt.Should().NotBeNull();
        storage.DeletedObjects.Should().BeEmpty();
    }

    /// <summary>
    /// Creates the document module with fake dependencies.
    /// </summary>
    private static DocumentModule CreateModule(InMemoryDocumentRepository repository, CapturingFileStorage storage)
    {
        return new DocumentModule(repository, new CapturingUnitOfWork(), storage, new CapturingPublisher(), NullLogger<DocumentModule>.Instance);
    }

    private sealed class InMemoryDocumentRepository : IDocumentRepository
    {
        private readonly List<Document> _documents = [];

        /// <summary>
        /// Adds a document to the in-memory list.
        /// </summary>
        public Task AddAsync(Document document, CancellationToken cancellationToken)
        {
            _documents.Add(document);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Adds a folder to the in-memory list.
        /// </summary>
        public Task AddFolderAsync(DocumentFolder folder, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Counts documents by status.
        /// </summary>
        public Task<IReadOnlyDictionary<DocumentStatus, int>> CountByStatusAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyDictionary<DocumentStatus, int>>(_documents.GroupBy(document => document.Status).ToDictionary(group => group.Key, group => group.Count()));
        }

        /// <summary>
        /// Loads a document by id.
        /// </summary>
        public Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(_documents.SingleOrDefault(document => document.Id == id));
        }

        /// <summary>
        /// Loads a folder by id.
        /// </summary>
        public Task<DocumentFolder?> GetFolderByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult<DocumentFolder?>(null);
        }

        /// <summary>
        /// Lists folders under a parent id.
        /// </summary>
        public Task<IReadOnlyList<DocumentFolder>> ListFoldersAsync(Guid? parentFolderId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<DocumentFolder>>([]);
        }

        /// <summary>
        /// Searches documents by query.
        /// </summary>
        public Task<IReadOnlyList<Document>> SearchAsync(string? query, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<Document>>(_documents);
        }
    }

    private sealed class CapturingFileStorage : IFileStorage
    {
        /// <summary>
        /// Gets saved file requests.
        /// </summary>
        public List<SaveFileRequest> SavedFiles { get; } = [];

        /// <summary>
        /// Gets deleted object keys.
        /// </summary>
        public List<string> DeletedObjects { get; } = [];

        /// <summary>
        /// Deletes an object key.
        /// </summary>
        public Task DeleteAsync(string storagePath, CancellationToken cancellationToken)
        {
            DeletedObjects.Add(storagePath);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Opens a stored object for reading.
        /// </summary>
        public Task<StoredFile> OpenReadAsync(string storagePath, string fileName, string contentType, CancellationToken cancellationToken)
        {
            return Task.FromResult(new StoredFile(fileName, contentType, new MemoryStream([9, 8, 7])));
        }

        /// <summary>
        /// Returns a preview URL for a stored object.
        /// </summary>
        public Task<string> GetPreviewUrlAsync(string storagePath, TimeSpan expiresIn, CancellationToken cancellationToken)
        {
            return Task.FromResult("https://minio.local/preview");
        }

        /// <summary>
        /// Saves a file and returns object metadata.
        /// </summary>
        public Task<StoredObject> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken)
        {
            var request = new SaveFileRequest(fileName, contentType);
            SavedFiles.Add(request);
            return Task.FromResult(new StoredObject("documents", $"minio://documents/{fileName}", content.Length, null));
        }

        /// <summary>
        /// Captures a storage save request.
        /// </summary>
        public sealed record SaveFileRequest(string FileName, string ContentType);
    }

    private sealed class CapturingUnitOfWork : IUnitOfWork
    {
        /// <summary>
        /// Captures a successful unit-of-work save.
        /// </summary>
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(1);
        }
    }

    private sealed class CapturingPublisher : IMessagePublisher
    {
        /// <summary>
        /// Captures an OCR job publish request.
        /// </summary>
        public Task PublishOcrJobAsync(Application.Contracts.Messaging.OcrJobMessage message, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
