# Document Storage MinIO Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build enterprise document storage features for upload, folders, metadata, versioning, download, preview, delete, and move using PostgreSQL and MinIO.

**Architecture:** Domain owns document, folder, and version behavior. Application orchestrates storage and persistence through repository, unit of work, and file storage interfaces. Infrastructure maps PostgreSQL with EF Core and implements MinIO object storage. API exposes Swagger-documented REST endpoints protected by RBAC.

**Tech Stack:** ASP.NET Core, EF Core, PostgreSQL, MinIO, Docker, xUnit, FluentAssertions.

**Spec:** User-approved chat design on 2026-09-11.

## Global Constraints

- Use Clean Architecture, SOLID, Dependency Injection, Repository Pattern, and Unit of Work.
- Support Docker and Linux.
- Use PostgreSQL for metadata and MinIO for binary objects.
- Code must be production ready.
- No paid libraries.
- Every function must have comments.
- Every API must have Swagger metadata.
- Every module must have unit tests.

---

### Task 1: Domain Model

**Files:**
- Create: `src/KmOcr.Domain/Documents/DocumentFolder.cs`
- Create: `src/KmOcr.Domain/Documents/DocumentVersion.cs`
- Modify: `src/KmOcr.Domain/Documents/Document.cs`
- Test: `tests/KmOcr.Domain.Tests/DocumentStorageTests.cs`

**Interfaces:**
- Produces document folder, version, move, soft delete, and version creation behavior.

- [ ] Write failing domain tests for folder creation, document move, version append, and soft delete.
- [ ] Implement domain entities and behavior.
- [ ] Run domain tests.

### Task 2: Application Storage Use Cases

**Files:**
- Modify: `src/KmOcr.Application/Contracts/Storage/IFileStorage.cs`
- Modify: `src/KmOcr.Application/Contracts/Persistence/IDocumentRepository.cs`
- Modify: `src/KmOcr.Application/Documents/DocumentModule.cs`
- Create command and DTO files under `src/KmOcr.Application/Documents/`.
- Test: `tests/KmOcr.Application.Tests/DocumentStorageModuleTests.cs`

**Interfaces:**
- Produces folder create/list, upload to folder, upload version, download, preview, move, and delete use cases.

- [ ] Write failing application tests with fake repository and fake storage.
- [ ] Implement contracts and use cases.
- [ ] Run application tests.

### Task 3: Infrastructure PostgreSQL and MinIO

**Files:**
- Create: `src/KmOcr.Infrastructure/Storage/MinioFileStorage.cs`
- Create: `src/KmOcr.Infrastructure/Storage/MinioStorageOptions.cs`
- Modify: `src/KmOcr.Infrastructure/Persistence/ApplicationDbContext.cs`
- Modify: `src/KmOcr.Infrastructure/Repositories/DocumentRepository.cs`
- Modify: `database/postgresql/001_enterprise_schema.sql`
- Test: `tests/KmOcr.Infrastructure.Tests/DatabaseSchemaContractTests.cs`

**Interfaces:**
- Produces EF mappings, schema constraints, indexes, and MinIO storage adapter registration.

- [ ] Write failing database contract tests for folders, versions, and MinIO storage columns.
- [ ] Implement EF mappings and DDL.
- [ ] Add MinIO package and storage adapter.
- [ ] Run infrastructure tests.

### Task 4: API and Docker

**Files:**
- Modify: `src/KmOcr.Api/Controllers/DocumentsController.cs`
- Create: `src/KmOcr.Api/Controllers/FoldersController.cs`
- Add API request models under `src/KmOcr.Api/Models/`.
- Modify: `docker-compose.yml`
- Modify: `.env.example`
- Test: `tests/KmOcr.Api.Tests/SecurityContractTests.cs`
- Test: `tests/KmOcr.Api.Tests/SwaggerContractTests.cs`

**Interfaces:**
- Produces Swagger-documented protected APIs and Docker Compose MinIO services.

- [ ] Write failing API metadata tests for folder and document storage endpoints.
- [ ] Implement controllers and models.
- [ ] Add MinIO and bucket initialization to Docker Compose.
- [ ] Run API tests and publish checks.
