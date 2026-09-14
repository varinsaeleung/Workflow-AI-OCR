# Node Workflow Engine Phase 1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add node-based workflow definition, validation, and publish APIs as the backend foundation for a drag-and-drop workflow designer.

**Architecture:** Domain owns immutable workflow graph rules. Application owns create, update, validate, and publish use cases. Infrastructure persists definitions, nodes, edges, and versions in PostgreSQL. API exposes Swagger-documented endpoints secured with RBAC.

**Tech Stack:** ASP.NET Core, EF Core, PostgreSQL, React-ready JSON graph contracts, xUnit, FluentAssertions.

**Spec:** User-approved chat design on 2026-09-11.

## Global Constraints

- Use Clean Architecture, SOLID, Dependency Injection, Repository Pattern, and Unit of Work.
- Support Docker and Linux.
- Use PostgreSQL.
- Code must be production ready.
- No paid libraries.
- Every function must have comments.
- Every API must have Swagger metadata.
- Every module must have unit tests.

---

### Task 1: Domain Graph

**Files:**
- Create: `src/KmOcr.Domain/WorkflowEngine/WorkflowDefinition.cs`
- Create: `src/KmOcr.Domain/WorkflowEngine/WorkflowNode.cs`
- Create: `src/KmOcr.Domain/WorkflowEngine/WorkflowEdge.cs`
- Create: `src/KmOcr.Domain/WorkflowEngine/WorkflowDefinitionVersion.cs`
- Create: `src/KmOcr.Domain/WorkflowEngine/WorkflowNodeType.cs`
- Test: `tests/KmOcr.Domain.Tests/WorkflowEngineTests.cs`

**Steps:**
- Write failing tests for valid graph publish, missing start validation, unsupported edge validation, and immutable published version.
- Implement minimal graph entities and validation.
- Run domain tests.

### Task 2: Application Designer Module

**Files:**
- Create: `src/KmOcr.Application/WorkflowEngine/IWorkflowDesignerModule.cs`
- Create: `src/KmOcr.Application/WorkflowEngine/WorkflowDesignerModule.cs`
- Create commands and DTOs under `src/KmOcr.Application/WorkflowEngine/`
- Create: `src/KmOcr.Application/Contracts/Persistence/IWorkflowDefinitionRepository.cs`
- Test: `tests/KmOcr.Application.Tests/WorkflowDesignerModuleTests.cs`

**Steps:**
- Write failing tests for create, update graph, validate, and publish.
- Implement use cases through repository and unit of work.
- Register module in DI.
- Run application tests.

### Task 3: Infrastructure Persistence

**Files:**
- Create: `src/KmOcr.Infrastructure/Repositories/WorkflowDefinitionRepository.cs`
- Modify: `src/KmOcr.Infrastructure/Persistence/ApplicationDbContext.cs`
- Modify: `src/KmOcr.Infrastructure/InfrastructureServiceRegistration.cs`
- Modify: `database/postgresql/001_enterprise_schema.sql`
- Test: `tests/KmOcr.Infrastructure.Tests/DatabaseSchemaContractTests.cs`

**Steps:**
- Write failing schema mapping tests for workflow definitions, nodes, edges, and versions.
- Implement EF mappings, repository, and DDL.
- Run infrastructure tests.

### Task 4: API Contract

**Files:**
- Create: `src/KmOcr.Api/Controllers/WorkflowDefinitionsController.cs`
- Create request models under `src/KmOcr.Api/Models/`
- Modify API security and Swagger tests.

**Steps:**
- Write failing API metadata tests.
- Implement endpoints for create, get, update graph, validate, and publish.
- Run API tests.
