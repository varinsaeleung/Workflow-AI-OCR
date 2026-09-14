using FluentAssertions;
using KmOcr.Application.Ai;
using KmOcr.Application.Contracts.Ai;
using KmOcr.Application.Contracts.Messaging;
using KmOcr.Application.Contracts.Persistence;
using KmOcr.Application.Contracts.Storage;
using KmOcr.Application.Documents;
using KmOcr.Application.Workflows;
using KmOcr.Domain.Audit;
using KmOcr.Domain.Documents;
using KmOcr.Domain.Workflows;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace KmOcr.Application.Tests;

/// <summary>
/// Verifies document use cases using in-memory fakes instead of infrastructure mocks.
/// </summary>
public sealed class DocumentModuleTests
{
    /// <summary>
    /// Ensures uploading a document stores the binary, persists the aggregate, saves metadata, and publishes an OCR job.
    /// </summary>
    [Fact]
    public async Task UploadAsync_should_store_document_and_publish_ocr_job()
    {
        var repository = new FakeDocumentRepository();
        var unitOfWork = new FakeUnitOfWork();
        var storage = new FakeFileStorage();
        var publisher = new FakeMessagePublisher();
        var module = new DocumentModule(repository, unitOfWork, storage, publisher, NullLogger<DocumentModule>.Instance);
        await using var content = new MemoryStream([1, 2, 3]);
        var command = new UploadDocumentCommand(
            "invoice-001.pdf",
            "application/pdf",
            content,
            "user-001",
            "invoice",
            new Dictionary<string, string> { ["vendor"] = "KM Thailand" });

        var result = await module.UploadAsync(command, CancellationToken.None);

        result.FileName.Should().Be("invoice-001.pdf");
        result.Status.Should().Be(DocumentStatus.OcrQueued.ToString());
        repository.AddedDocument.Should().NotBeNull();
        repository.AddedDocument!.Metadata.Should().ContainSingle(metadata => metadata.Key == "vendor");
        storage.SavedFileName.Should().Be("invoice-001.pdf");
        publisher.LastOcrJob.Should().NotBeNull();
        publisher.LastOcrJob!.DocumentId.Should().Be(result.Id);
        unitOfWork.SaveCount.Should().Be(1);
    }

    private sealed class FakeDocumentRepository : IDocumentRepository
    {
        public Document? AddedDocument { get; private set; }

        /// <summary>
        /// Stores the aggregate in memory for assertions.
        /// </summary>
        public Task AddAsync(Document document, CancellationToken cancellationToken)
        {
            AddedDocument = document;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Accepts a folder without affecting document tests.
        /// </summary>
        public Task AddFolderAsync(DocumentFolder folder, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Returns the added aggregate when the requested id matches.
        /// </summary>
        public Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(AddedDocument?.Id == id ? AddedDocument : null);
        }

        /// <summary>
        /// Returns no folder for legacy document tests.
        /// </summary>
        public Task<DocumentFolder?> GetFolderByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult<DocumentFolder?>(null);
        }

        /// <summary>
        /// Returns an empty folder list for legacy document tests.
        /// </summary>
        public Task<IReadOnlyList<DocumentFolder>> ListFoldersAsync(Guid? parentFolderId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<DocumentFolder>>([]);
        }

        /// <summary>
        /// Returns all stored documents that contain the search text.
        /// </summary>
        public Task<IReadOnlyList<Document>> SearchAsync(string? query, CancellationToken cancellationToken)
        {
            var documents = AddedDocument is null ? [] : new[] { AddedDocument };
            return Task.FromResult<IReadOnlyList<Document>>(documents);
        }

        /// <summary>
        /// Counts stored documents by status for dashboard tests.
        /// </summary>
        public Task<IReadOnlyDictionary<DocumentStatus, int>> CountByStatusAsync(CancellationToken cancellationToken)
        {
            var counts = AddedDocument is null
                ? new Dictionary<DocumentStatus, int>()
                : new Dictionary<DocumentStatus, int> { [AddedDocument.Status] = 1 };

            return Task.FromResult<IReadOnlyDictionary<DocumentStatus, int>>(counts);
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        /// <summary>
        /// Counts calls instead of writing to a database.
        /// </summary>
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class FakeFileStorage : IFileStorage
    {
        public string? SavedFileName { get; private set; }

        /// <summary>
        /// Records the file name and returns a deterministic storage path.
        /// </summary>
        public Task<StoredObject> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken)
        {
            SavedFileName = fileName;
            return Task.FromResult(new StoredObject("local", $"local://documents/{fileName}", content.Length, null));
        }

        /// <summary>
        /// Opens a deterministic stream for legacy document tests.
        /// </summary>
        public Task<StoredFile> OpenReadAsync(string storagePath, string fileName, string contentType, CancellationToken cancellationToken)
        {
            return Task.FromResult(new StoredFile(fileName, contentType, new MemoryStream([1])));
        }

        /// <summary>
        /// Returns a deterministic preview URL for legacy document tests.
        /// </summary>
        public Task<string> GetPreviewUrlAsync(string storagePath, TimeSpan expiresIn, CancellationToken cancellationToken)
        {
            return Task.FromResult(storagePath);
        }

        /// <summary>
        /// Accepts physical delete requests for legacy document tests.
        /// </summary>
        public Task DeleteAsync(string storagePath, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeMessagePublisher : IMessagePublisher
    {
        public OcrJobMessage? LastOcrJob { get; private set; }

        /// <summary>
        /// Captures the OCR job message for assertions.
        /// </summary>
        public Task PublishOcrJobAsync(OcrJobMessage message, CancellationToken cancellationToken)
        {
            LastOcrJob = message;
            return Task.CompletedTask;
        }
    }
}

/// <summary>
/// Verifies workflow use cases through in-memory fakes.
/// </summary>
public sealed class WorkflowModuleTests
{
    /// <summary>
    /// Ensures starting a workflow creates a task and persists it once.
    /// </summary>
    [Fact]
    public async Task StartAsync_should_create_workflow_with_initial_review_task()
    {
        var documents = new FakeWorkflowDocumentRepository();
        var workflows = new FakeWorkflowRepository();
        var unitOfWork = new FakeUnitOfWork();
        var module = new WorkflowModule(documents, workflows, unitOfWork, NullLogger<WorkflowModule>.Instance);
        var document = Document.Create("invoice.txt", "text/plain", "local://documents/invoice.txt", "invoice", "user-001");
        documents.Document = document;

        var result = await module.StartAsync(new StartWorkflowCommand(document.Id, "default-review", "user-001"), CancellationToken.None);

        result.DocumentId.Should().Be(document.Id);
        result.TemplateKey.Should().Be("default-review");
        document.Status.Should().Be(DocumentStatus.InWorkflow);
        workflows.AddedWorkflow.Should().NotBeNull();
        workflows.AddedWorkflow!.Tasks.Should().ContainSingle();
        unitOfWork.SaveCount.Should().Be(1);
    }

    private sealed class FakeWorkflowDocumentRepository : IDocumentRepository
    {
        public Document? Document { get; set; }

        /// <summary>
        /// Stores the aggregate in memory.
        /// </summary>
        public Task AddAsync(Document document, CancellationToken cancellationToken)
        {
            Document = document;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Accepts a folder without affecting workflow tests.
        /// </summary>
        public Task AddFolderAsync(DocumentFolder folder, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Returns the configured document when the requested id matches.
        /// </summary>
        public Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(Document?.Id == id ? Document : null);
        }

        /// <summary>
        /// Returns no folder for workflow tests.
        /// </summary>
        public Task<DocumentFolder?> GetFolderByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult<DocumentFolder?>(null);
        }

        /// <summary>
        /// Returns an empty folder list for workflow tests.
        /// </summary>
        public Task<IReadOnlyList<DocumentFolder>> ListFoldersAsync(Guid? parentFolderId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<DocumentFolder>>([]);
        }

        /// <summary>
        /// Returns the configured document for search scenarios.
        /// </summary>
        public Task<IReadOnlyList<Document>> SearchAsync(string? query, CancellationToken cancellationToken)
        {
            var documents = Document is null ? [] : new[] { Document };
            return Task.FromResult<IReadOnlyList<Document>>(documents);
        }

        /// <summary>
        /// Counts the configured document by status.
        /// </summary>
        public Task<IReadOnlyDictionary<DocumentStatus, int>> CountByStatusAsync(CancellationToken cancellationToken)
        {
            var counts = Document is null
                ? new Dictionary<DocumentStatus, int>()
                : new Dictionary<DocumentStatus, int> { [Document.Status] = 1 };

            return Task.FromResult<IReadOnlyDictionary<DocumentStatus, int>>(counts);
        }
    }

    private sealed class FakeWorkflowRepository : IWorkflowRepository
    {
        public WorkflowInstance? AddedWorkflow { get; private set; }

        /// <summary>
        /// Stores the workflow in memory for assertions.
        /// </summary>
        public Task AddAsync(WorkflowInstance workflow, CancellationToken cancellationToken)
        {
            AddedWorkflow = workflow;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Returns the stored workflow when the requested id matches.
        /// </summary>
        public Task<WorkflowInstance?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(AddedWorkflow?.Id == id ? AddedWorkflow : null);
        }

        /// <summary>
        /// Returns tasks assigned to the requested user.
        /// </summary>
        public Task<IReadOnlyList<WorkflowTask>> GetTasksForUserAsync(string assignee, CancellationToken cancellationToken)
        {
            var tasks = AddedWorkflow?.Tasks.Where(task => task.AssignedTo == assignee).ToList() ?? [];
            return Task.FromResult<IReadOnlyList<WorkflowTask>>(tasks);
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        /// <summary>
        /// Counts persistence attempts.
        /// </summary>
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }
}

/// <summary>
/// Minimal fake audit repository kept available for future application service tests.
/// </summary>
internal sealed class FakeAuditLogRepository : IAuditLogRepository
{
    /// <summary>
    /// Accepts an audit entry without external dependencies.
    /// </summary>
    public Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// Verifies AI extraction use cases with in-memory fakes.
/// </summary>
public sealed class AiModuleTests
{
    /// <summary>
    /// Ensures AI extraction uses OCR text, stores the result, and commits once.
    /// </summary>
    [Fact]
    public async Task RunExtractionAsync_should_store_ai_result_from_ocr_text()
    {
        var document = Document.Create("invoice.txt", "text/plain", "local://documents/invoice.txt", "invoice", "user-001");
        document.QueueOcr();
        document.CompleteOcr("Invoice total THB 1,200 contact ap@example.com", 0.99m, "plain-text");
        var documents = new FakeAiDocumentRepository { Document = document };
        var unitOfWork = new FakeAiUnitOfWork();
        var aiEngine = new FakeAiEngine();
        var module = new AiModule(documents, unitOfWork, aiEngine, NullLogger<AiModule>.Instance);

        var result = await module.RunExtractionAsync(document.Id, CancellationToken.None);

        result.Classification.Should().Be("invoice");
        result.Entities.Should().ContainKey("amount");
        document.AiExtraction.Should().NotBeNull();
        unitOfWork.SaveCount.Should().Be(1);
    }

    private sealed class FakeAiDocumentRepository : IDocumentRepository
    {
        public Document? Document { get; set; }

        /// <summary>
        /// Stores the document in memory.
        /// </summary>
        public Task AddAsync(Document document, CancellationToken cancellationToken)
        {
            Document = document;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Accepts a folder without affecting AI tests.
        /// </summary>
        public Task AddFolderAsync(DocumentFolder folder, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Returns the configured document when the identifier matches.
        /// </summary>
        public Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(Document?.Id == id ? Document : null);
        }

        /// <summary>
        /// Returns no folder for AI tests.
        /// </summary>
        public Task<DocumentFolder?> GetFolderByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult<DocumentFolder?>(null);
        }

        /// <summary>
        /// Returns an empty folder list for AI tests.
        /// </summary>
        public Task<IReadOnlyList<DocumentFolder>> ListFoldersAsync(Guid? parentFolderId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<DocumentFolder>>([]);
        }

        /// <summary>
        /// Returns the configured document for search scenarios.
        /// </summary>
        public Task<IReadOnlyList<Document>> SearchAsync(string? query, CancellationToken cancellationToken)
        {
            var documents = Document is null ? [] : new[] { Document };
            return Task.FromResult<IReadOnlyList<Document>>(documents);
        }

        /// <summary>
        /// Counts the configured document by status.
        /// </summary>
        public Task<IReadOnlyDictionary<DocumentStatus, int>> CountByStatusAsync(CancellationToken cancellationToken)
        {
            var counts = Document is null
                ? new Dictionary<DocumentStatus, int>()
                : new Dictionary<DocumentStatus, int> { [Document.Status] = 1 };

            return Task.FromResult<IReadOnlyDictionary<DocumentStatus, int>>(counts);
        }
    }

    private sealed class FakeAiUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        /// <summary>
        /// Counts commits.
        /// </summary>
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class FakeAiEngine : IAiEngine
    {
        /// <summary>
        /// Returns deterministic AI extraction data.
        /// </summary>
        public Task<AiEngineResult> ExtractAsync(string text, CancellationToken cancellationToken)
        {
            var result = new AiEngineResult(
                "invoice",
                "invoice: Invoice total THB 1,200",
                new Dictionary<string, string> { ["amount"] = "THB 1,200" },
                0.80m,
                "fake-ai");
            return Task.FromResult(result);
        }
    }
}
