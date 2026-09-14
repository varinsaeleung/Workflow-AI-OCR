using KmOcr.Domain.Audit;
using KmOcr.Domain.Auth;
using KmOcr.Domain.Documents;
using KmOcr.Domain.WorkflowEngine;
using KmOcr.Domain.Workflows;
using Microsoft.EntityFrameworkCore;

namespace KmOcr.Infrastructure.Persistence;

/// <summary>
/// Entity Framework database context for PostgreSQL persistence.
/// </summary>
public sealed class ApplicationDbContext : DbContext
{
    /// <summary>
    /// Creates the database context with externally supplied options.
    /// </summary>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets the documents table.
    /// </summary>
    public DbSet<Document> Documents => Set<Document>();

    /// <summary>
    /// Gets the document folders table.
    /// </summary>
    public DbSet<DocumentFolder> DocumentFolders => Set<DocumentFolder>();

    /// <summary>
    /// Gets the document versions table.
    /// </summary>
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();

    /// <summary>
    /// Gets the document metadata table.
    /// </summary>
    public DbSet<DocumentMetadata> DocumentMetadata => Set<DocumentMetadata>();

    /// <summary>
    /// Gets the OCR result table.
    /// </summary>
    public DbSet<OcrResult> OcrResults => Set<OcrResult>();

    /// <summary>
    /// Gets the AI extraction table.
    /// </summary>
    public DbSet<AiExtraction> AiExtractions => Set<AiExtraction>();

    /// <summary>
    /// Gets the workflow instances table.
    /// </summary>
    public DbSet<WorkflowInstance> WorkflowInstances => Set<WorkflowInstance>();

    /// <summary>
    /// Gets the node workflow definitions table.
    /// </summary>
    public DbSet<WorkflowDefinition> WorkflowDefinitions => Set<WorkflowDefinition>();

    /// <summary>
    /// Gets the node workflow nodes table.
    /// </summary>
    public DbSet<WorkflowNode> WorkflowNodes => Set<WorkflowNode>();

    /// <summary>
    /// Gets the node workflow edges table.
    /// </summary>
    public DbSet<WorkflowEdge> WorkflowEdges => Set<WorkflowEdge>();

    /// <summary>
    /// Gets the node workflow definition versions table.
    /// </summary>
    public DbSet<WorkflowDefinitionVersion> WorkflowDefinitionVersions => Set<WorkflowDefinitionVersion>();

    /// <summary>
    /// Gets the workflow tasks table.
    /// </summary>
    public DbSet<WorkflowTask> WorkflowTasks => Set<WorkflowTask>();

    /// <summary>
    /// Gets the audit log table.
    /// </summary>
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    /// <summary>
    /// Gets the users table.
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// Gets the roles table.
    /// </summary>
    public DbSet<Role> Roles => Set<Role>();

    /// <summary>
    /// Gets the permissions table.
    /// </summary>
    public DbSet<Permission> Permissions => Set<Permission>();

    /// <summary>
    /// Gets the user-role join table.
    /// </summary>
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    /// <summary>
    /// Gets the role-permission join table.
    /// </summary>
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    /// <summary>
    /// Gets the refresh tokens table.
    /// </summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <summary>
    /// Configures tables, keys, indexes, relationships, and value conversions.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureAuth(modelBuilder);
        ConfigureDocuments(modelBuilder);
        ConfigureWorkflowEngine(modelBuilder);
        ConfigureWorkflows(modelBuilder);
        ConfigureAuditLogs(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Configures node-based workflow designer persistence.
    /// </summary>
    private static void ConfigureWorkflowEngine(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkflowDefinition>(entity =>
        {
            entity.ToTable("workflow_definitions");
            entity.HasKey(definition => definition.Id);
            entity.Property(definition => definition.Id).HasColumnName("id");
            entity.Property(definition => definition.Code).HasColumnName("code").HasMaxLength(128).IsRequired();
            entity.Property(definition => definition.Name).HasColumnName("name").HasMaxLength(256).IsRequired();
            entity.Property(definition => definition.CreatedBy).HasColumnName("created_by").HasMaxLength(128).IsRequired();
            entity.Property(definition => definition.IsPublished).HasColumnName("is_published");
            entity.Property(definition => definition.PublishedVersionNumber).HasColumnName("published_version_number");
            entity.Property(definition => definition.CreatedAt).HasColumnName("created_at");
            entity.Property(definition => definition.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(definition => definition.Code).IsUnique();
            entity.HasMany(definition => definition.Nodes).WithOne().HasForeignKey(node => node.WorkflowDefinitionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(definition => definition.Edges).WithOne().HasForeignKey(edge => edge.WorkflowDefinitionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(definition => definition.Versions).WithOne().HasForeignKey(version => version.WorkflowDefinitionId).OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(definition => definition.Nodes).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.Navigation(definition => definition.Edges).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.Navigation(definition => definition.Versions).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<WorkflowNode>(entity =>
        {
            entity.ToTable("workflow_nodes");
            entity.HasKey(node => node.Id);
            entity.Property(node => node.Id).HasColumnName("id");
            entity.Property(node => node.WorkflowDefinitionId).HasColumnName("workflow_definition_id");
            entity.Property(node => node.NodeKey).HasColumnName("node_key").HasMaxLength(128).IsRequired();
            entity.Property(node => node.NodeType).HasColumnName("node_type").HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(node => node.PositionX).HasColumnName("position_x").HasPrecision(12, 2);
            entity.Property(node => node.PositionY).HasColumnName("position_y").HasPrecision(12, 2);
            entity.Property(node => node.ConfigJson).HasColumnName("config_json").HasColumnType("jsonb").IsRequired();
            entity.Property(node => node.CreatedAt).HasColumnName("created_at");
            entity.Property(node => node.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(node => node.WorkflowDefinitionId);
            entity.HasIndex(node => new { node.WorkflowDefinitionId, node.NodeKey }).IsUnique();
        });

        modelBuilder.Entity<WorkflowEdge>(entity =>
        {
            entity.ToTable("workflow_edges");
            entity.HasKey(edge => edge.Id);
            entity.Property(edge => edge.Id).HasColumnName("id");
            entity.Property(edge => edge.WorkflowDefinitionId).HasColumnName("workflow_definition_id");
            entity.Property(edge => edge.SourceNodeKey).HasColumnName("source_node_key").HasMaxLength(128).IsRequired();
            entity.Property(edge => edge.TargetNodeKey).HasColumnName("target_node_key").HasMaxLength(128).IsRequired();
            entity.Property(edge => edge.ConditionExpression).HasColumnName("condition_expression").HasMaxLength(1024);
            entity.Property(edge => edge.CreatedAt).HasColumnName("created_at");
            entity.Property(edge => edge.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(edge => edge.WorkflowDefinitionId);
        });

        modelBuilder.Entity<WorkflowDefinitionVersion>(entity =>
        {
            entity.ToTable("workflow_definition_versions");
            entity.HasKey(version => version.Id);
            entity.Property(version => version.Id).HasColumnName("id");
            entity.Property(version => version.WorkflowDefinitionId).HasColumnName("workflow_definition_id");
            entity.Property(version => version.VersionNumber).HasColumnName("version_number");
            entity.Property(version => version.GraphJson).HasColumnName("graph_json").HasColumnType("jsonb").IsRequired();
            entity.Property(version => version.PublishedBy).HasColumnName("published_by").HasMaxLength(128).IsRequired();
            entity.Property(version => version.CreatedAt).HasColumnName("created_at");
            entity.Property(version => version.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(version => new { version.WorkflowDefinitionId, version.VersionNumber }).IsUnique();
        });
    }

    /// <summary>
    /// Configures users, roles, permissions, and refresh tokens.
    /// </summary>
    private static void ConfigureAuth(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Id).HasColumnName("id");
            entity.Property(user => user.Email).HasColumnName("email").HasMaxLength(320).IsRequired();
            entity.Property(user => user.DisplayName).HasColumnName("display_name").HasMaxLength(256).IsRequired();
            entity.Property(user => user.PasswordHash).HasColumnName("password_hash").HasMaxLength(512).IsRequired();
            entity.Property(user => user.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(user => user.LastLoginAt).HasColumnName("last_login_at");
            entity.Property(user => user.CreatedAt).HasColumnName("created_at");
            entity.Property(user => user.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(user => user.Email).IsUnique();
            entity.Navigation(user => user.Roles).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(role => role.Id);
            entity.Property(role => role.Id).HasColumnName("id");
            entity.Property(role => role.Code).HasColumnName("code").HasMaxLength(64).IsRequired();
            entity.Property(role => role.Name).HasColumnName("name").HasMaxLength(128).IsRequired();
            entity.Property(role => role.IsSystemRole).HasColumnName("is_system_role");
            entity.Property(role => role.IsActive).HasColumnName("is_active");
            entity.Property(role => role.CreatedAt).HasColumnName("created_at");
            entity.Property(role => role.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(role => role.Code).IsUnique();
            entity.Navigation(role => role.Permissions).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("permissions");
            entity.HasKey(permission => permission.Id);
            entity.Property(permission => permission.Id).HasColumnName("id");
            entity.Property(permission => permission.Code).HasColumnName("code").HasMaxLength(128).IsRequired();
            entity.Property(permission => permission.Name).HasColumnName("name").HasMaxLength(128).IsRequired();
            entity.Property(permission => permission.Module).HasColumnName("module").HasMaxLength(64).IsRequired();
            entity.Property(permission => permission.CreatedAt).HasColumnName("created_at");
            entity.Property(permission => permission.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(permission => permission.Code).IsUnique();
            entity.HasIndex(permission => permission.Module);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("user_roles");
            entity.HasKey(userRole => new { userRole.UserId, userRole.RoleId });
            entity.Property(userRole => userRole.UserId).HasColumnName("user_id");
            entity.Property(userRole => userRole.RoleId).HasColumnName("role_id");
            entity.Property(userRole => userRole.AssignedByUserId).HasColumnName("assigned_by_user_id");
            entity.Property(userRole => userRole.AssignedAt).HasColumnName("assigned_at");
            entity.HasIndex(userRole => userRole.RoleId);
            entity.HasOne<User>()
                .WithMany(user => user.Roles)
                .HasForeignKey(userRole => userRole.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Role>()
                .WithMany()
                .HasForeignKey(userRole => userRole.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("role_permissions");
            entity.HasKey(rolePermission => new { rolePermission.RoleId, rolePermission.PermissionId });
            entity.Property(rolePermission => rolePermission.RoleId).HasColumnName("role_id");
            entity.Property(rolePermission => rolePermission.PermissionId).HasColumnName("permission_id");
            entity.Property(rolePermission => rolePermission.GrantedAt).HasColumnName("granted_at");
            entity.HasIndex(rolePermission => rolePermission.PermissionId);
            entity.HasOne<Role>()
                .WithMany(role => role.Permissions)
                .HasForeignKey(rolePermission => rolePermission.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Permission>()
                .WithMany()
                .HasForeignKey(rolePermission => rolePermission.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(token => token.Id);
            entity.Property(token => token.Id).HasColumnName("id");
            entity.Property(token => token.UserId).HasColumnName("user_id");
            entity.Property(token => token.TokenHash).HasColumnName("token_hash").HasMaxLength(256).IsRequired();
            entity.Property(token => token.ExpiresAt).HasColumnName("expires_at");
            entity.Property(token => token.RevokedAt).HasColumnName("revoked_at");
            entity.Property(token => token.ReplacedByTokenHash).HasColumnName("replaced_by_token_hash").HasMaxLength(256);
            entity.Property(token => token.CreatedByIp).HasColumnName("created_by_ip").HasMaxLength(64).IsRequired();
            entity.Property(token => token.RevokedByIp).HasColumnName("revoked_by_ip").HasMaxLength(64);
            entity.Property(token => token.UserAgent).HasColumnName("user_agent").HasMaxLength(512).IsRequired();
            entity.Property(token => token.CreatedAt).HasColumnName("created_at");
            entity.Property(token => token.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(token => token.TokenHash).IsUnique();
            entity.HasIndex(token => new { token.UserId, token.ExpiresAt });
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// Configures document aggregate persistence.
    /// </summary>
    private static void ConfigureDocuments(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Document>(entity =>
        {
            entity.ToTable("documents");
            entity.HasKey(document => document.Id);
            entity.Property(document => document.Id).HasColumnName("id");
            entity.Property(document => document.FileName).HasColumnName("file_name").HasMaxLength(260).IsRequired();
            entity.Property(document => document.ContentType).HasColumnName("content_type").HasMaxLength(128).IsRequired();
            entity.Property(document => document.StoragePath).HasColumnName("storage_path").HasMaxLength(1024).IsRequired();
            entity.Property(document => document.StorageBucket).HasColumnName("storage_bucket").HasMaxLength(128);
            entity.Property(document => document.FolderId).HasColumnName("folder_id");
            entity.Property(document => document.DocumentType).HasColumnName("document_type").HasMaxLength(128).IsRequired();
            entity.Property(document => document.UploadedBy).HasColumnName("uploaded_by").HasMaxLength(128).IsRequired();
            entity.Property(document => document.FileSizeBytes).HasColumnName("file_size_bytes");
            entity.Property(document => document.ChecksumSha256).HasColumnName("checksum_sha256").HasMaxLength(64);
            entity.Property(document => document.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(64).IsRequired();
            entity.Property(document => document.CurrentVersionNumber).HasColumnName("current_version_number");
            entity.Property(document => document.DeletedAt).HasColumnName("deleted_at");
            entity.Property(document => document.DeletedBy).HasColumnName("deleted_by").HasMaxLength(128);
            entity.Property(document => document.CreatedAt).HasColumnName("created_at");
            entity.Property(document => document.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(document => document.FileName);
            entity.HasIndex(document => new { document.FolderId, document.CreatedAt });
            entity.HasIndex(document => document.DocumentType);
            entity.HasIndex(document => document.Status);
            entity.HasOne<DocumentFolder>()
                .WithMany()
                .HasForeignKey(document => document.FolderId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(document => document.Metadata)
                .WithOne()
                .HasForeignKey(metadata => metadata.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(document => document.Metadata).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.HasMany(document => document.Versions)
                .WithOne()
                .HasForeignKey(version => version.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(document => document.Versions).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.HasOne(document => document.OcrResult)
                .WithOne()
                .HasForeignKey<OcrResult>(result => result.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(document => document.AiExtraction)
                .WithOne()
                .HasForeignKey<AiExtraction>(extraction => extraction.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentFolder>(entity =>
        {
            entity.ToTable("document_folders");
            entity.HasKey(folder => folder.Id);
            entity.Property(folder => folder.Id).HasColumnName("id");
            entity.Property(folder => folder.ParentFolderId).HasColumnName("parent_folder_id");
            entity.Property(folder => folder.Name).HasColumnName("name").HasMaxLength(256).IsRequired();
            entity.Property(folder => folder.Path).HasColumnName("path").HasMaxLength(2048).IsRequired();
            entity.Property(folder => folder.CreatedBy).HasColumnName("created_by").HasMaxLength(128).IsRequired();
            entity.Property(folder => folder.DeletedAt).HasColumnName("deleted_at");
            entity.Property(folder => folder.CreatedAt).HasColumnName("created_at");
            entity.Property(folder => folder.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(folder => new { folder.ParentFolderId, folder.Path });
            entity.HasOne<DocumentFolder>()
                .WithMany()
                .HasForeignKey(folder => folder.ParentFolderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DocumentVersion>(entity =>
        {
            entity.ToTable("document_versions");
            entity.HasKey(version => version.Id);
            entity.Property(version => version.Id).HasColumnName("id");
            entity.Property(version => version.DocumentId).HasColumnName("document_id");
            entity.Property(version => version.VersionNumber).HasColumnName("version_number");
            entity.Property(version => version.FileName).HasColumnName("file_name").HasMaxLength(260).IsRequired();
            entity.Property(version => version.ContentType).HasColumnName("content_type").HasMaxLength(128).IsRequired();
            entity.Property(version => version.StoragePath).HasColumnName("storage_path").HasMaxLength(1024).IsRequired();
            entity.Property(version => version.FileSizeBytes).HasColumnName("file_size_bytes");
            entity.Property(version => version.ChecksumSha256).HasColumnName("checksum_sha256").HasMaxLength(64);
            entity.Property(version => version.CreatedBy).HasColumnName("created_by").HasMaxLength(128).IsRequired();
            entity.Property(version => version.CreatedAt).HasColumnName("created_at");
            entity.Property(version => version.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(version => new { version.DocumentId, version.VersionNumber }).IsUnique();
        });

        modelBuilder.Entity<DocumentMetadata>(entity =>
        {
            entity.ToTable("document_metadata");
            entity.HasKey(metadata => metadata.Id);
            entity.Property(metadata => metadata.Id).HasColumnName("id");
            entity.Property(metadata => metadata.DocumentId).HasColumnName("document_id");
            entity.Property(metadata => metadata.Key).HasColumnName("key").HasMaxLength(128).IsRequired();
            entity.Property(metadata => metadata.Value).HasColumnName("value").HasMaxLength(2048).IsRequired();
            entity.Property(metadata => metadata.CreatedAt).HasColumnName("created_at");
            entity.Property(metadata => metadata.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(metadata => new { metadata.DocumentId, metadata.Key }).IsUnique();
        });

        modelBuilder.Entity<OcrResult>(entity =>
        {
            entity.ToTable("ocr_results");
            entity.HasKey(result => result.Id);
            entity.Property(result => result.Id).HasColumnName("id");
            entity.Property(result => result.DocumentId).HasColumnName("document_id");
            entity.Property(result => result.ExtractedText).HasColumnName("extracted_text").IsRequired();
            entity.Property(result => result.ConfidenceScore).HasColumnName("confidence_score").HasPrecision(5, 4).IsRequired();
            entity.Property(result => result.Engine).HasColumnName("engine").HasMaxLength(128).IsRequired();
            entity.Property(result => result.CreatedAt).HasColumnName("created_at");
            entity.Property(result => result.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(result => result.DocumentId).IsUnique();
        });

        modelBuilder.Entity<AiExtraction>(entity =>
        {
            entity.ToTable("ai_extractions");
            entity.HasKey(extraction => extraction.Id);
            entity.Property(extraction => extraction.Id).HasColumnName("id");
            entity.Property(extraction => extraction.DocumentId).HasColumnName("document_id");
            entity.Property(extraction => extraction.Classification).HasColumnName("classification").HasMaxLength(128).IsRequired();
            entity.Property(extraction => extraction.Summary).HasColumnName("summary").HasMaxLength(2048).IsRequired();
            entity.Property(extraction => extraction.ExtractedEntitiesJson).HasColumnName("extracted_entities_json").HasColumnType("jsonb").IsRequired();
            entity.Property(extraction => extraction.ConfidenceScore).HasColumnName("confidence_score").HasPrecision(5, 4).IsRequired();
            entity.Property(extraction => extraction.Engine).HasColumnName("engine").HasMaxLength(128).IsRequired();
            entity.Property(extraction => extraction.CreatedAt).HasColumnName("created_at");
            entity.Property(extraction => extraction.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(extraction => extraction.DocumentId).IsUnique();
            entity.HasIndex(extraction => extraction.Classification);
        });
    }

    /// <summary>
    /// Configures workflow aggregate persistence.
    /// </summary>
    private static void ConfigureWorkflows(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkflowInstance>(entity =>
        {
            entity.ToTable("workflow_instances");
            entity.HasKey(workflow => workflow.Id);
            entity.Property(workflow => workflow.Id).HasColumnName("id");
            entity.Property(workflow => workflow.DocumentId).HasColumnName("document_id");
            entity.Property(workflow => workflow.TemplateKey).HasColumnName("template_key").HasMaxLength(128).IsRequired();
            entity.Property(workflow => workflow.StartedBy).HasColumnName("started_by").HasMaxLength(128).IsRequired();
            entity.Property(workflow => workflow.CreatedAt).HasColumnName("created_at");
            entity.Property(workflow => workflow.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(workflow => workflow.DocumentId);
            entity.HasMany(workflow => workflow.Tasks)
                .WithOne()
                .HasForeignKey(task => task.WorkflowInstanceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(workflow => workflow.Tasks).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<WorkflowTask>(entity =>
        {
            entity.ToTable("workflow_tasks");
            entity.HasKey(task => task.Id);
            entity.Property(task => task.Id).HasColumnName("id");
            entity.Property(task => task.WorkflowInstanceId).HasColumnName("workflow_instance_id");
            entity.Property(task => task.Name).HasColumnName("name").HasMaxLength(128).IsRequired();
            entity.Property(task => task.AssignedTo).HasColumnName("assigned_to").HasMaxLength(128).IsRequired();
            entity.Property(task => task.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(64).IsRequired();
            entity.Property(task => task.CompletedBy).HasColumnName("completed_by").HasMaxLength(128);
            entity.Property(task => task.Comment).HasColumnName("comment").HasMaxLength(2048);
            entity.Property(task => task.CompletedAt).HasColumnName("completed_at");
            entity.Property(task => task.CreatedAt).HasColumnName("created_at");
            entity.Property(task => task.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(task => task.AssignedTo);
            entity.HasIndex(task => task.Status);
        });
    }

    /// <summary>
    /// Configures audit log persistence.
    /// </summary>
    private static void ConfigureAuditLogs(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.HasKey(auditLog => auditLog.Id);
            entity.Property(auditLog => auditLog.Id).HasColumnName("id");
            entity.Property(auditLog => auditLog.Actor).HasColumnName("actor").HasMaxLength(128).IsRequired();
            entity.Property(auditLog => auditLog.Action).HasColumnName("action").HasMaxLength(128).IsRequired();
            entity.Property(auditLog => auditLog.ResourceType).HasColumnName("resource_type").HasMaxLength(128).IsRequired();
            entity.Property(auditLog => auditLog.ResourceId).HasColumnName("resource_id").HasMaxLength(256).IsRequired();
            entity.Property(auditLog => auditLog.CreatedAt).HasColumnName("created_at");
            entity.Property(auditLog => auditLog.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(auditLog => auditLog.Actor);
            entity.HasIndex(auditLog => new { auditLog.ResourceType, auditLog.ResourceId });
        });
    }
}
