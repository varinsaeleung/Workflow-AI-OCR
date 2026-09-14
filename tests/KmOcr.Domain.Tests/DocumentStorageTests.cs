using FluentAssertions;
using KmOcr.Domain.Documents;
using Xunit;

namespace KmOcr.Domain.Tests;

/// <summary>
/// Verifies enterprise document storage behavior in the domain layer.
/// </summary>
public sealed class DocumentStorageTests
{
    /// <summary>
    /// Ensures folders normalize their names and compute a stable path.
    /// </summary>
    [Fact]
    public void DocumentFolder_CreateRoot_should_normalize_name_and_path()
    {
        var folder = DocumentFolder.CreateRoot(" Invoices ", "admin@km.local");

        folder.Name.Should().Be("Invoices");
        folder.Path.Should().Be("/Invoices");
        folder.CreatedBy.Should().Be("admin@km.local");
    }

    /// <summary>
    /// Ensures folder moves update the parent id and materialized path.
    /// </summary>
    [Fact]
    public void DocumentFolder_MoveTo_should_update_parent_and_path()
    {
        var source = DocumentFolder.CreateRoot("Invoices", "admin@km.local");
        var target = DocumentFolder.CreateRoot("Archive", "admin@km.local");

        source.MoveTo(target.Id, target.Path);

        source.ParentFolderId.Should().Be(target.Id);
        source.Path.Should().Be("/Archive/Invoices");
    }

    /// <summary>
    /// Ensures a new document records folder placement and creates version one.
    /// </summary>
    [Fact]
    public void Document_Create_should_record_folder_and_first_version()
    {
        var folderId = Guid.NewGuid();
        var document = Document.Create(
            "invoice.pdf",
            "application/pdf",
            "minio://documents/2026/invoice.pdf",
            "invoice",
            "admin@km.local",
            folderId,
            "documents",
            100,
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

        document.FolderId.Should().Be(folderId);
        document.StorageBucket.Should().Be("documents");
        document.FileSizeBytes.Should().Be(100);
        document.CurrentVersionNumber.Should().Be(1);
        document.Versions.Should().ContainSingle(version => version.VersionNumber == 1);
    }

    /// <summary>
    /// Ensures uploading a new binary version advances the current version.
    /// </summary>
    [Fact]
    public void AddVersion_should_increment_current_version()
    {
        var document = CreateDocument();

        document.AddVersion(
            "invoice-v2.pdf",
            "application/pdf",
            "minio://documents/2026/invoice-v2.pdf",
            120,
            "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
            "admin@km.local");

        document.CurrentVersionNumber.Should().Be(2);
        document.StoragePath.Should().Be("minio://documents/2026/invoice-v2.pdf");
        document.Versions.Should().Contain(version => version.VersionNumber == 2 && version.FileSizeBytes == 120);
    }

    /// <summary>
    /// Ensures documents can be moved between folders without changing versions.
    /// </summary>
    [Fact]
    public void MoveToFolder_should_update_folder_only()
    {
        var document = CreateDocument();
        var targetFolderId = Guid.NewGuid();

        document.MoveToFolder(targetFolderId);

        document.FolderId.Should().Be(targetFolderId);
        document.CurrentVersionNumber.Should().Be(1);
    }

    /// <summary>
    /// Ensures delete is represented as a soft delete for audit-friendly recovery.
    /// </summary>
    [Fact]
    public void SoftDelete_should_mark_document_deleted_and_archived()
    {
        var document = CreateDocument();

        document.SoftDelete("admin@km.local");

        document.DeletedAt.Should().NotBeNull();
        document.DeletedBy.Should().Be("admin@km.local");
        document.Status.Should().Be(DocumentStatus.Archived);
    }

    /// <summary>
    /// Creates a valid document for storage behavior tests.
    /// </summary>
    private static Document CreateDocument()
    {
        return Document.Create(
            "invoice.pdf",
            "application/pdf",
            "minio://documents/2026/invoice.pdf",
            "invoice",
            "admin@km.local",
            Guid.NewGuid(),
            "documents",
            100,
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
    }
}
