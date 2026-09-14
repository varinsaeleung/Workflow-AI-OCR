using FluentAssertions;
using KmOcr.Domain.Documents;
using KmOcr.Domain.WorkflowEngine;
using KmOcr.Domain.Workflows;
using KmOcr.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace KmOcr.Infrastructure.Tests;

/// <summary>
/// Verifies that EF Core mappings and SQL DDL follow the enterprise PostgreSQL schema contract.
/// </summary>
public sealed class DatabaseSchemaContractTests
{
    /// <summary>
    /// Ensures document columns use PostgreSQL-friendly snake_case names.
    /// </summary>
    [Fact]
    public void Entity_mappings_should_use_snake_case_columns_for_documents()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(Document));
        var table = StoreObjectIdentifier.Table("documents", null);

        entityType.Should().NotBeNull();
        entityType!.FindProperty(nameof(Document.FileName))!.GetColumnName(table).Should().Be("file_name");
        entityType.FindProperty(nameof(Document.ContentType))!.GetColumnName(table).Should().Be("content_type");
        entityType.FindProperty(nameof(Document.StoragePath))!.GetColumnName(table).Should().Be("storage_path");
        entityType.FindProperty(nameof(Document.DocumentType))!.GetColumnName(table).Should().Be("document_type");
        entityType.FindProperty(nameof(Document.UploadedBy))!.GetColumnName(table).Should().Be("uploaded_by");
        entityType.FindProperty(nameof(Document.FolderId))!.GetColumnName(table).Should().Be("folder_id");
        entityType.FindProperty(nameof(Document.StorageBucket))!.GetColumnName(table).Should().Be("storage_bucket");
        entityType.FindProperty(nameof(Document.FileSizeBytes))!.GetColumnName(table).Should().Be("file_size_bytes");
        entityType.FindProperty(nameof(Document.CurrentVersionNumber))!.GetColumnName(table).Should().Be("current_version_number");
        entityType.FindProperty(nameof(Document.DeletedAt))!.GetColumnName(table).Should().Be("deleted_at");
        entityType.FindProperty(nameof(Document.CreatedAt))!.GetColumnName(table).Should().Be("created_at");
        entityType.FindProperty(nameof(Document.UpdatedAt))!.GetColumnName(table).Should().Be("updated_at");
    }

    /// <summary>
    /// Ensures folder and version entities map to enterprise PostgreSQL tables.
    /// </summary>
    [Fact]
    public void Entity_mappings_should_include_document_folders_and_versions()
    {
        using var context = CreateContext();
        var folderType = context.Model.FindEntityType(typeof(DocumentFolder));
        var versionType = context.Model.FindEntityType(typeof(DocumentVersion));

        folderType.Should().NotBeNull();
        folderType!.GetTableName().Should().Be("document_folders");
        versionType.Should().NotBeNull();
        versionType!.GetTableName().Should().Be("document_versions");
    }

    /// <summary>
    /// Ensures node workflow entities map to enterprise PostgreSQL tables.
    /// </summary>
    [Fact]
    public void Entity_mappings_should_include_node_workflow_tables()
    {
        using var context = CreateContext();

        context.Model.FindEntityType(typeof(WorkflowDefinition))!.GetTableName().Should().Be("workflow_definitions");
        context.Model.FindEntityType(typeof(WorkflowNode))!.GetTableName().Should().Be("workflow_nodes");
        context.Model.FindEntityType(typeof(WorkflowEdge))!.GetTableName().Should().Be("workflow_edges");
        context.Model.FindEntityType(typeof(WorkflowDefinitionVersion))!.GetTableName().Should().Be("workflow_definition_versions");
    }

    /// <summary>
    /// Ensures workflow task columns use PostgreSQL-friendly snake_case names.
    /// </summary>
    [Fact]
    public void Entity_mappings_should_use_snake_case_columns_for_workflow_tasks()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(WorkflowTask));
        var table = StoreObjectIdentifier.Table("workflow_tasks", null);

        entityType.Should().NotBeNull();
        entityType!.FindProperty(nameof(WorkflowTask.WorkflowInstanceId))!.GetColumnName(table).Should().Be("workflow_instance_id");
        entityType.FindProperty(nameof(WorkflowTask.AssignedTo))!.GetColumnName(table).Should().Be("assigned_to");
        entityType.FindProperty(nameof(WorkflowTask.CompletedBy))!.GetColumnName(table).Should().Be("completed_by");
        entityType.FindProperty(nameof(WorkflowTask.CompletedAt))!.GetColumnName(table).Should().Be("completed_at");
    }

    /// <summary>
    /// Ensures the hand-written PostgreSQL DDL contains the enterprise tables, indexes, and constraints.
    /// </summary>
    [Fact]
    public void Enterprise_schema_sql_should_define_required_tables_indexes_and_constraints()
    {
        var sql = File.ReadAllText(FindRepositoryFile("database/postgresql/001_enterprise_schema.sql"));

        sql.Should().Contain("create extension if not exists pgcrypto");
        sql.Should().Contain("create table if not exists users");
        sql.Should().Contain("password_hash");
        sql.Should().Contain("create table if not exists refresh_tokens");
        sql.Should().Contain("create table if not exists role_permissions");
        sql.Should().Contain("create table if not exists document_folders");
        sql.Should().Contain("create table if not exists document_versions");
        sql.Should().Contain("folder_id uuid null references document_folders(id)");
        sql.Should().Contain("deleted_by varchar(128) null");
        sql.Should().Contain("create table if not exists metadata_fields");
        sql.Should().Contain("create table if not exists ocr_jobs");
        sql.Should().Contain("create table if not exists ai_extraction_entities");
        sql.Should().Contain("create table if not exists workflow_task_actions");
        sql.Should().Contain("create table if not exists workflow_definitions");
        sql.Should().Contain("create table if not exists workflow_nodes");
        sql.Should().Contain("create table if not exists workflow_edges");
        sql.Should().Contain("create table if not exists workflow_definition_versions");
        sql.Should().Contain("node_type varchar(32) not null");
        sql.Should().Contain("graph_json jsonb not null");
        sql.Should().Contain("check (confidence_score >= 0 and confidence_score <= 1)");
        sql.Should().Contain("create unique index if not exists ux_users_email_active");
        sql.Should().Contain("create unique index if not exists ux_refresh_tokens_token_hash");
        sql.Should().Contain("where revoked_at is null");
        sql.Should().Contain("create index if not exists ix_documents_search_vector");
        sql.Should().Contain("create index if not exists ix_documents_folder_created_at");
        sql.Should().Contain("create index if not exists ix_document_folders_parent_path");
        sql.Should().Contain("create unique index if not exists ux_workflow_definitions_code_active");
        sql.Should().Contain("create index if not exists ix_workflow_nodes_definition_id");
        sql.Should().Contain("using gin");
    }

    /// <summary>
    /// Creates an in-memory DbContext for metadata inspection.
    /// </summary>
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    /// <summary>
    /// Finds a repository file from the test output directory by walking up to the solution root.
    /// </summary>
    private static string FindRepositoryFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "KM.AIWorkflowOCR.sln")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new DirectoryNotFoundException("Repository root was not found.");
        }

        return Path.Combine(directory.FullName, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }
}
