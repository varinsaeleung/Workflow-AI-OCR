# Paddle OCR Service Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a production-ready OCR service that uses PaddleOCR for PDF, JPG, and PNG files with Thai and English recognition, returning JSON through API and worker flows.

**Architecture:** Application owns OCR validation and DTO mapping through `IOcrModule`. Infrastructure owns PaddleOCR process execution and JSON parsing through `IOcrEngine`. API exposes multipart upload extraction behind RBAC, and Worker reuses the same engine through its existing queue consumer.

**Tech Stack:** ASP.NET Core, PostgreSQL, RabbitMQ, Docker, Python, PaddleOCR, xUnit, FluentAssertions.

**Spec:** User-approved chat design on 2026-09-11.

## Global Constraints

- Use Clean Architecture.
- Use SOLID, Dependency Injection, Repository Pattern, and Unit of Work where persistence applies.
- Support Docker and Linux.
- Use PostgreSQL.
- Code must be production ready.
- No paid libraries.
- Every function must have comments.
- Every API must have Swagger metadata.
- Every module must have unit tests.

---

### Task 1: Application OCR Contract

**Files:**
- Create: `src/KmOcr.Application/Contracts/Ocr/IOcrEngine.cs`
- Create: `src/KmOcr.Application/Contracts/Ocr/OcrEngineResult.cs`
- Create: `src/KmOcr.Application/Ocr/IOcrModule.cs`
- Create: `src/KmOcr.Application/Ocr/OcrModule.cs`
- Test: `tests/KmOcr.Application.Tests/OcrModuleTests.cs`

**Interfaces:**
- Produces: `IOcrModule.ExtractAsync(OcrExtractCommand, CancellationToken)` returns `OcrResultDto`.
- Consumes: `IOcrEngine.ExtractAsync(string, string, CancellationToken)`.

- [ ] Write failing tests for supported content types, JSON shape, empty stream validation, and unsupported content type validation.
- [ ] Run application tests and verify they fail because OCR types do not exist.
- [ ] Implement OCR contracts, DTOs, command, and module.
- [ ] Register `IOcrModule` in Application DI.
- [ ] Run application tests and verify pass.

### Task 2: PaddleOCR Infrastructure Adapter

**Files:**
- Create: `src/KmOcr.Infrastructure/Ocr/PaddleOcrOptions.cs`
- Create: `src/KmOcr.Infrastructure/Ocr/PaddleOcrEngine.cs`
- Create: `src/KmOcr.Infrastructure/Ocr/PaddleOcrJsonParser.cs`
- Create: `src/KmOcr.Infrastructure/Ocr/ProcessRunner.cs`
- Create: `src/KmOcr.Infrastructure/Ocr/paddle_ocr_service.py`
- Modify: `src/KmOcr.Infrastructure/InfrastructureServiceRegistration.cs`
- Test: `tests/KmOcr.Infrastructure.Tests/PaddleOcrJsonParserTests.cs`

**Interfaces:**
- Consumes: `IOcrEngine`.
- Produces: PaddleOCR JSON result with pages, words, confidence, language, and engine.

- [ ] Write failing parser tests using a hand-authored PaddleOCR JSON fixture.
- [ ] Run infrastructure tests and verify they fail because parser does not exist.
- [ ] Implement options, parser, process runner, and engine.
- [ ] Register `IOcrEngine` and options in DI.
- [ ] Run infrastructure tests and verify pass.

### Task 3: API OCR Endpoint

**Files:**
- Create: `src/KmOcr.Api/Controllers/OcrController.cs`
- Modify: `tests/KmOcr.Api.Tests/SecurityContractTests.cs`
- Modify: `tests/KmOcr.Api.Tests/SwaggerContractTests.cs`

**Interfaces:**
- Consumes: `IOcrModule.ExtractAsync`.
- Produces: `POST /api/v1/ocr/extract` with JSON response.

- [ ] Write failing API metadata tests that include `OcrController`.
- [ ] Run API tests and verify they fail because controller does not exist.
- [ ] Implement controller with Swagger response metadata, multipart upload, and `documents.ai` permission policy.
- [ ] Run API tests and verify pass.

### Task 4: Worker and Docker Integration

**Files:**
- Modify: `src/KmOcr.Worker/CliOcrProcessor.cs`
- Modify: `src/KmOcr.Worker/Program.cs`
- Modify: `src/KmOcr.Worker/Dockerfile`
- Modify: `src/KmOcr.Api/Dockerfile`
- Modify: `docker-compose.yml`
- Modify: `src/KmOcr.Api/appsettings.json`
- Modify: `src/KmOcr.Worker/appsettings.json`
- Modify: `.env.example`

**Interfaces:**
- Consumes: `IOcrEngine`.
- Produces: Worker OCR completion through PaddleOCR engine.

- [ ] Update worker processor to use `IOcrEngine`.
- [ ] Install Python, PaddleOCR, PaddlePaddle, Poppler, and OpenCV runtime dependencies in API and worker containers.
- [ ] Add `PaddleOcr` configuration for executable path, script path, timeout, and languages.
- [ ] Run .NET tests, React tests, and release publish.

### Task 5: Documentation

**Files:**
- Modify: `docs/API_DESIGN.md`
- Modify: `docs/ARCHITECTURE.md`
- Modify: `README.md`

**Interfaces:**
- Documents how to call OCR API and run Docker stack.

- [ ] Add OCR endpoint JSON example.
- [ ] Add PaddleOCR runtime notes.
- [ ] Add run instructions and limitations.
