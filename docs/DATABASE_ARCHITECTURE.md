# PostgreSQL Database Architecture

## Step 1: Database Boundary

PostgreSQL is the system of record for users, permissions, documents, metadata, OCR status, AI extraction output, workflow state, and audit logs.

RabbitMQ remains the asynchronous transport for OCR work. PostgreSQL stores job state so the platform can recover, inspect failures, and report dashboard metrics.

## Step 2: Schema Modules

| Module | Tables |
|---|---|
| Identity and Authentication | `departments`, `users`, `refresh_tokens` |
| Permission | `roles`, `permissions`, `user_roles`, `role_permissions`, `document_access_grants` |
| Document Management | `document_types`, `documents`, `document_versions` |
| Metadata | `metadata_fields`, `document_metadata`, `document_metadata_values` |
| OCR | `ocr_jobs`, `ocr_results` |
| AI | `ai_models`, `ai_extractions`, `ai_extraction_entities` |
| Workflow | `workflow_templates`, `workflow_steps`, `workflow_instances`, `workflow_tasks`, `workflow_task_actions` |
| Audit | `audit_logs` |

## Step 3: Key Design Decisions

1. UUID primary keys use `gen_random_uuid()` from `pgcrypto`.
2. Soft-delete columns use `deleted_at` on master and long-lived business tables.
3. Active uniqueness uses partial unique indexes, such as `ux_users_email_active`.
4. OCR and document full-text search use generated `tsvector` columns with GIN indexes.
5. Metadata supports both starter key-value rows and enterprise typed metadata rows.
6. AI output is stored both as JSONB and normalized entity rows.
7. Workflow templates are versioned so running workflow instances remain stable.
8. Audit log stores both actor text and optional user FK to survive user deactivation.
9. Passwords are stored as PBKDF2 hashes; raw passwords and raw refresh tokens never enter PostgreSQL.
10. Refresh tokens are one-time rotated, uniquely hashed, and indexed with a partial active-token index.

## Step 4: Index Strategy

| Query Pattern | Index |
|---|---|
| Login or user lookup by email | `ux_users_email_active` |
| Refresh-token lookup and active-session listing | `ux_refresh_tokens_token_hash`, `ix_refresh_tokens_user_active` |
| RBAC permission checks | `user_roles`, `role_permissions` primary keys and reverse indexes |
| Document dashboard status counts | `ix_documents_status_created_at` |
| Recent uploads by user | `ix_documents_uploaded_by_user_created_at` |
| Document full-text search | `ix_documents_search_vector` |
| OCR text search | `ix_ocr_results_search_vector` |
| Metadata text filters | `ix_document_metadata_values_field_text` |
| Metadata numeric/date filters | `ix_document_metadata_values_field_number`, `ix_document_metadata_values_field_date` |
| Pending OCR jobs | `ix_ocr_jobs_ready` |
| Assigned workflow tasks | `ix_workflow_tasks_assignee_status` |
| SLA due tasks | `ix_workflow_tasks_pending_due_at` |
| Resource audit lookup | `ix_audit_logs_resource` |

## Step 5: Constraint Strategy

| Constraint Type | Examples |
|---|---|
| Identity | Primary keys on all entity tables |
| Referential integrity | Foreign keys between documents, workflow, OCR, AI, users, and permissions |
| Status safety | CHECK constraints for document, OCR job, workflow, and user status |
| Confidence safety | CHECK constraints requiring AI/OCR confidence between 0 and 1 |
| Data quality | NOT NULL and not-blank CHECK constraints on business identifiers |
| Metadata type safety | CHECK constraint allowing at most one typed value column per metadata row |
| Lifecycle safety | CHECK constraints for positive version, file size, retention, SLA, and attempt counts |

## Step 6: Clean Architecture Mapping

The DDL is the physical database contract. EF Core mappings in `ApplicationDbContext` map current aggregates to snake_case table and column names that align with the SQL schema.

The application still follows Clean Architecture:

```text
API -> Application Interfaces -> Domain
Infrastructure -> Application Interfaces
```

Database-specific choices stay in Infrastructure and SQL files. Domain and Application layers do not depend on PostgreSQL.
