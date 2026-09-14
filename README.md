# KM AI Workflow OCR Platform

Enterprise OCR, Ollama AI workflow, OpenSearch, metadata, and dashboard starter built with ASP.NET Core, React, PostgreSQL, RabbitMQ, Docker, Clean Architecture, SOLID, Dependency Injection, Repository Pattern, and Unit of Work.

## Step by Step Run

1. Install Docker Desktop or Docker Engine with Docker Compose.
2. For a shared or production-like environment, copy `.env.example` to `.env` and replace the JWT signing key and administrator password.
3. From the repository root, run:

   ```bash
   docker compose up --build
   ```

4. Open the web application:

   ```text
   http://localhost:3000
   ```

5. Open Swagger:

   ```text
   http://localhost:8080/swagger
   ```

6. Open RabbitMQ Management:

   ```text
   http://localhost:15672
   Username: km_ocr
   Password: km_ocr_password
   ```

7. Open MinIO Console:

   ```text
   http://localhost:9001
   Username: km_ocr
   Password: km_ocr_password
   ```

8. Sign in to the web application with the development administrator:

   ```text
   Email: admin@km.local
   Password: ChangeMe!2026
   ```

   Change `Jwt__SigningKey` and `SeedAdmin__Password` before any shared or production deployment. The development seeder runs only in the Development environment.

## Step by Step Test

1. Install .NET 10 SDK.
2. Run backend unit tests:

   ```bash
   dotnet test KM.AIWorkflowOCR.sln
   ```

3. Run frontend unit tests:

   ```bash
   cd apps/web
   npm install
   npm test
   ```

## Step by Step Operations

1. Deployment guide: `docs/DEPLOYMENT_GUIDE.md`
2. CI/CD guide: `docs/CICD.md`
3. User manual: `docs/USER_MANUAL.md`
4. Admin manual: `docs/ADMIN_MANUAL.md`
5. Backup and restore: `docs/BACKUP_RESTORE.md`
6. GitHub upload guide: `docs/GITHUB_UPLOAD.md`

Production Docker Compose:

```bash
docker compose -f docker-compose.yml -f deploy/docker-compose.prod.yml up --build -d
```

Backup:

```bash
bash scripts/backup.sh
```

Restore:

```bash
bash scripts/restore.sh backups/<timestamp>
```

## Folder Structure

```text
src/
  KmOcr.Domain/          Enterprise rules and aggregates
  KmOcr.Application/     Use cases, DTOs, interfaces, DI registration
  KmOcr.Infrastructure/  PostgreSQL, repositories, Unit of Work, MinIO storage, RabbitMQ, PaddleOCR adapter, Ollama AI adapter, OpenSearch adapter
  KmOcr.Api/             ASP.NET Core controllers, Swagger, middleware
  KmOcr.Worker/          RabbitMQ OCR worker using the PaddleOCR adapter
apps/
  web/                   React dashboard, upload, search UI
tests/
  KmOcr.Domain.Tests/
  KmOcr.Application.Tests/
  KmOcr.Infrastructure.Tests/
  KmOcr.Api.Tests/
  infrastructure/
    nginx/                 React reverse proxy config
  database/
    postgresql/            Enterprise PostgreSQL DDL
  docs/
    ARCHITECTURE.md
    API_DESIGN.md
    DATABASE_ARCHITECTURE.md
    ER_DIAGRAM.md
    POSTGRESQL_DDL.md
```

## Code Explanation

- `Domain` contains entities such as `Document`, `OcrResult`, and `WorkflowInstance`. These classes hold business behavior and do not depend on ASP.NET Core, EF Core, RabbitMQ, or React.
- `Application` contains use case modules such as `DocumentModule`, `WorkflowModule`, and `DashboardModule`. It depends only on contracts, so it follows Dependency Inversion.
- `Infrastructure` implements repository interfaces with EF Core, stores files in MinIO, publishes OCR jobs to RabbitMQ, runs PaddleOCR through a Python adapter, calls Ollama through an AI adapter, and indexes search read models in OpenSearch.
- `database/postgresql/001_enterprise_schema.sql` contains the enterprise PostgreSQL DDL with tables, indexes, constraints, and triggers.
- `Api` exposes REST endpoints with Swagger metadata and sends all business actions through Application modules.
- `Auth` provides JWT access tokens, hashed and rotated refresh tokens, RBAC permission claims, logout revocation, and audit events for authentication and denied access.
- `Worker` consumes RabbitMQ OCR messages, extracts text with PaddleOCR, then updates documents through `IDocumentModule`.
- `apps/web` is a React operational dashboard for upload, search, and status monitoring.

Every C# function and TypeScript function in the scaffold includes a summary comment or JSDoc comment.

## Open Source Stack

- ASP.NET Core / .NET 10 LTS
- React, TypeScript, Vite, Recharts
- PostgreSQL
- MinIO
- RabbitMQ
- EF Core with Npgsql
- PaddleOCR and PaddlePaddle
- Poppler for PDF page rendering
- Ollama local AI for classification, summary, and metadata extraction
- OpenSearch and OpenSearch Dashboards
- Deterministic keyword AI fallback for tests and offline development
- xUnit, Vitest
- NGINX

No paid library is required.

## Authentication Flow

1. `POST /api/v1/auth/login` verifies the PBKDF2 password hash, issues a short-lived JWT, stores only a SHA-256 refresh-token hash, and writes an audit event.
2. React sends the JWT as `Authorization: Bearer <token>` to protected APIs.
3. On a 401 caused by access-token expiry, React calls `POST /api/v1/auth/refresh`. The API revokes the old refresh token and returns a new pair.
4. `POST /api/v1/auth/logout` revokes the current refresh token and writes an audit event.
5. RBAC policies require permission claims such as `documents.read`, `documents.write`, `workflow.approve`, or `admin.users`.

## OCR Flow

1. `POST /api/v1/ocr/extract` accepts `PDF`, `JPG`, or `PNG` multipart uploads and requires the `documents.ai` permission.
2. `OcrModule` validates the content type and temporary file handling inside the Application layer.
3. `PaddleOcrEngine` invokes `python3 Ocr/paddle_ocr_service.py` and parses structured JSON.
4. The JSON response includes `engine`, `language`, full `text`, average `confidenceScore`, `pages`, and detected word bounding boxes.
5. Background OCR jobs use the same PaddleOCR engine through the worker so queued documents and direct API extraction share one OCR contract.

## AI Flow

1. `POST /api/v1/documents/{id}/ai-extraction` requires `documents.ai` and runs classification, summary, and metadata extraction from stored OCR text.
2. `AiModule` depends on `IAiEngine`, so the Application layer does not depend on Ollama directly.
3. `OllamaAiEngine` calls Ollama `/api/generate` with `format: json` and parses `classification`, `summary`, `metadata`, and `confidence`.
4. Change `OLLAMA_MODEL` in `.env` to switch models, for example `llama3.1`, `qwen2.5`, or any model already supported by your Ollama runtime.
5. Set `AI_PROVIDER=Keyword` when you want deterministic local extraction without running Ollama.

## OpenSearch Flow

1. PostgreSQL remains the source of truth for documents, OCR, AI, metadata, folders, and permissions.
2. `POST /api/v1/search/documents/{documentId}/index` rebuilds one OpenSearch read model from PostgreSQL.
3. `GET /api/v1/search` searches `fileName`, `documentType`, `ocrText`, `aiClassification`, `aiSummary`, and `metadata.*`.
4. Filters include `documentType`, `status`, `folderId`, `metadataKey`, and `metadataValue`.
5. Search results include `highlights` with `<mark>` snippets for matched fields.
6. OpenSearch Dashboards runs at `http://localhost:5601` in Docker Compose for index inspection.

## Dashboard Flow

1. `GET /api/v1/dashboard/enterprise` requires `dashboard.read`.
2. `DashboardModule` exposes the use case while depending only on repository abstractions.
3. `DashboardMetricsRepository` aggregates PostgreSQL records for OCR, workflow, users, errors, performance, storage, and throughput.
4. React renders metric tiles plus Recharts bar and line charts for operations monitoring.

## Document Storage Flow

1. `POST /api/v1/folders` creates root or child folders stored in PostgreSQL.
2. `POST /api/v1/documents` uploads a file to MinIO, creates document metadata, creates version 1, and queues OCR.
3. `POST /api/v1/documents/{id}/versions` uploads a new MinIO object and advances `current_version_number`.
4. `GET /api/v1/documents/{id}/download` streams the current MinIO object back to the client.
5. `GET /api/v1/documents/{id}/preview` returns a time-limited MinIO preview URL.
6. `POST /api/v1/documents/{id}/move` updates `folder_id` without touching object storage.
7. `DELETE /api/v1/documents/{id}` soft-deletes the document in PostgreSQL and keeps MinIO objects for retention.
