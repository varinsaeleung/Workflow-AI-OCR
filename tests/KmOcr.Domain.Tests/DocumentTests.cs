using FluentAssertions;
using KmOcr.Domain.Documents;
using KmOcr.Domain.Workflows;
using Xunit;

namespace KmOcr.Domain.Tests;

/// <summary>
/// Verifies document aggregate behavior without infrastructure dependencies.
/// </summary>
public sealed class DocumentTests
{
    /// <summary>
    /// Ensures a newly uploaded document captures its immutable upload details and starts in the uploaded state.
    /// </summary>
    [Fact]
    public void Create_should_capture_upload_details_and_initial_status()
    {
        var document = Document.Create(
            "invoice-001.pdf",
            "application/pdf",
            "local://documents/invoice-001.pdf",
            "invoice",
            "user-001");

        document.FileName.Should().Be("invoice-001.pdf");
        document.ContentType.Should().Be("application/pdf");
        document.StoragePath.Should().Be("local://documents/invoice-001.pdf");
        document.DocumentType.Should().Be("invoice");
        document.UploadedBy.Should().Be("user-001");
        document.Status.Should().Be(DocumentStatus.Uploaded);
    }

    /// <summary>
    /// Ensures OCR completion stores extracted text and moves the document into the OCR completed state.
    /// </summary>
    [Fact]
    public void CompleteOcr_should_store_text_confidence_and_status()
    {
        var document = Document.Create(
            "invoice-001.pdf",
            "application/pdf",
            "local://documents/invoice-001.pdf",
            "invoice",
            "user-001");

        document.QueueOcr();
        document.CompleteOcr("Invoice total 1200 THB", 0.98m, "tesseract");

        document.Status.Should().Be(DocumentStatus.OcrCompleted);
        document.OcrResult.Should().NotBeNull();
        document.OcrResult!.ExtractedText.Should().Be("Invoice total 1200 THB");
        document.OcrResult.ConfidenceScore.Should().Be(0.98m);
        document.OcrResult.Engine.Should().Be("tesseract");
    }

    /// <summary>
    /// Ensures metadata changes are idempotent and keep one value per key.
    /// </summary>
    [Fact]
    public void UpsertMetadata_should_replace_existing_key_value()
    {
        var document = Document.Create(
            "invoice-001.pdf",
            "application/pdf",
            "local://documents/invoice-001.pdf",
            "invoice",
            "user-001");

        document.UpsertMetadata("vendor", "KM Thailand");
        document.UpsertMetadata("vendor", "Konica Minolta Thailand");

        document.Metadata.Should().ContainSingle();
        document.Metadata.Single().Key.Should().Be("vendor");
        document.Metadata.Single().Value.Should().Be("Konica Minolta Thailand");
    }

    /// <summary>
    /// Ensures AI extraction output is stored on the document aggregate.
    /// </summary>
    [Fact]
    public void CompleteAiExtraction_should_store_classification_summary_and_entities()
    {
        var document = Document.Create(
            "invoice-001.pdf",
            "application/pdf",
            "local://documents/invoice-001.pdf",
            "invoice",
            "user-001");

        document.CompleteAiExtraction("invoice", "Supplier invoice", "{\"vendor\":\"KM\"}", 0.91m, "keyword-ai");

        document.AiExtraction.Should().NotBeNull();
        document.AiExtraction!.Classification.Should().Be("invoice");
        document.AiExtraction.Summary.Should().Be("Supplier invoice");
        document.AiExtraction.ExtractedEntitiesJson.Should().Be("{\"vendor\":\"KM\"}");
    }
}

/// <summary>
/// Verifies workflow aggregate behavior independently from persistence.
/// </summary>
public sealed class WorkflowTests
{
    /// <summary>
    /// Ensures approving a workflow task records the actor and terminal status.
    /// </summary>
    [Fact]
    public void ApproveTask_should_mark_task_as_approved()
    {
        var workflow = WorkflowInstance.Start(Guid.NewGuid(), "default-review", "user-001");

        workflow.ApproveTask(workflow.Tasks.Single().Id, "manager-001", "Looks correct");

        workflow.Tasks.Single().Status.Should().Be(WorkflowTaskStatus.Approved);
        workflow.Tasks.Single().CompletedBy.Should().Be("manager-001");
        workflow.Tasks.Single().Comment.Should().Be("Looks correct");
    }
}
