# API Design

Base URL:

```text
/api/v1
```

Swagger:

```text
/swagger
```

Protected endpoints require:

```http
Authorization: Bearer <access-token>
```

## Authentication and RBAC

| Method | Path | Description | Access |
|---|---|---|---|
| POST | `/auth/login` | Validate credentials and issue access/refresh tokens | Anonymous |
| POST | `/auth/refresh` | Rotate an active refresh token | Anonymous |
| POST | `/auth/logout` | Revoke the current refresh token | Authenticated |
| GET | `/auth/me` | Get the current user profile | Authenticated |
| GET | `/auth/permissions` | Get effective RBAC permissions | Authenticated |

Permission policies are enforced by JWT `permission` claims. Current permissions are `documents.read`, `documents.write`, `documents.ai`, `documents.delete`, `workflow.read`, `workflow.write`, `workflow.approve`, `dashboard.read`, and `admin.users`.

Authentication failures and permission-denied requests are written to `audit_logs` with actions such as `auth.login`, `auth.login_failed`, `auth.refresh`, `auth.logout`, and `auth.permission_denied`.

## Documents

| Method | Path | Description |
|---|---|---|
| POST | `/documents` | Upload document and queue OCR |
| GET | `/documents?q=` | List or search documents |
| GET | `/documents/{id}` | Get document details |
| POST | `/documents/{id}/versions` | Upload a new file version |
| GET | `/documents/{id}/download` | Download current version |
| GET | `/documents/{id}/versions/{versionNumber}/download` | Download a specific version |
| GET | `/documents/{id}/preview` | Get a time-limited MinIO preview URL |
| PUT | `/documents/{id}/metadata` | Update metadata |
| POST | `/documents/{id}/move` | Move document to a folder or root |
| DELETE | `/documents/{id}` | Soft-delete document |
| POST | `/documents/{id}/reprocess-ocr` | Queue OCR retry |
| GET | `/documents/{id}/ocr` | Get OCR result |
| POST | `/documents/{id}/ai-extraction` | Run AI extraction |
| GET | `/documents/{id}/ai-extraction` | Get AI extraction |

AI extraction uses the configured `IAiEngine` provider. The Docker profile defaults to Ollama at `http://ollama:11434`; change `OLLAMA_MODEL` to switch models, or set `AI_PROVIDER=Keyword` for deterministic local tests.

## Folders

| Method | Path | Description | Access |
|---|---|---|---|
| POST | `/folders` | Create root or child folder | `documents.write` |
| GET | `/folders?parentFolderId=` | List folders under a parent or root | `documents.read` |
| POST | `/folders/{id}/move` | Move folder under another folder or root | `documents.write` |

Documents are stored in MinIO and organized in PostgreSQL with `folder_id`, `current_version_number`, `document_versions`, and soft-delete fields. Deleting a document marks it archived in PostgreSQL and keeps MinIO objects for retention/audit.

## OCR

| Method | Path | Description | Access |
|---|---|---|---|
| POST | `/ocr/extract` | Extract Thai and English OCR JSON from PDF, JPG, or PNG | `documents.ai` |

Request:

```http
POST /api/v1/ocr/extract
Authorization: Bearer <access-token>
Content-Type: multipart/form-data
```

Form fields:

| Field | Type | Required | Description |
|---|---|---|---|
| `file` | file | Yes | PDF, JPG, or PNG file |

Response:

```json
{
  "engine": "paddleocr",
  "language": "tha+eng",
  "text": "สวัสดี Hello",
  "confidenceScore": 0.91,
  "pages": [
    {
      "pageNumber": 1,
      "text": "สวัสดี Hello",
      "confidenceScore": 0.91,
      "words": [
        {
          "text": "Hello",
          "x": 12,
          "y": 20,
          "width": 60,
          "height": 24,
          "confidenceScore": 0.93
        }
      ]
    }
  ]
}
```

## Search

| Method | Path | Description |
|---|---|---|
| GET | `/search?q=&documentType=&status=&folderId=&metadataKey=&metadataValue=&size=&from=` | Search indexed document, OCR, AI, and metadata fields with filters and highlights |
| POST | `/search/documents/{documentId}/index` | Rebuild one OpenSearch document entry from PostgreSQL source data |

Search responses include total hits, result items, and per-field highlight snippets:

```json
{
  "total": 1,
  "items": [
    {
      "documentId": "dddddddd-dddd-dddd-dddd-dddddddddddd",
      "fileName": "invoice.pdf",
      "documentType": "invoice",
      "status": "OcrCompleted",
      "folderId": null,
      "metadata": {
        "vendor": "Konica Minolta"
      },
      "ocrText": "Tax invoice",
      "aiClassification": "invoice",
      "aiSummary": "Invoice summary",
      "highlights": {
        "ocrText": [ "Tax <mark>invoice</mark>" ]
      }
    }
  ]
}
```

## Workflow

| Method | Path | Description |
|---|---|---|
| POST | `/workflows` | Start workflow |
| GET | `/workflows/{id}` | Get workflow |
| GET | `/workflows/tasks/my?assignee=` | Get assigned tasks |
| POST | `/workflows/{workflowId}/tasks/{taskId}/assign` | Assign task |
| POST | `/workflows/{workflowId}/tasks/{taskId}/approve` | Approve task |
| POST | `/workflows/{workflowId}/tasks/{taskId}/reject` | Reject task |

## Node Workflow Definitions

| Method | Path | Description | Access |
|---|---|---|---|
| POST | `/workflow-definitions` | Create a draft node workflow definition | `workflow.write` |
| GET | `/workflow-definitions` | List workflow definitions | `workflow.read` |
| GET | `/workflow-definitions/{id}` | Get workflow graph details | `workflow.read` |
| PUT | `/workflow-definitions/{id}/graph` | Replace nodes and edges from the drag-and-drop designer | `workflow.write` |
| POST | `/workflow-definitions/{id}/validate` | Validate graph rules before publish | `workflow.write` |
| POST | `/workflow-definitions/{id}/publish` | Publish immutable graph version | `workflow.write` |

Supported node types are `Start`, `Ocr`, `Ai`, `Email`, `Folder`, `Webhook`, `Condition`, `Loop`, `Approval`, and `End`.

Graph payload:

```json
{
  "nodes": [
    {
      "nodeKey": "start",
      "nodeType": "Start",
      "positionX": 40,
      "positionY": 120,
      "configJson": "{}"
    },
    {
      "nodeKey": "ocr",
      "nodeType": "Ocr",
      "positionX": 240,
      "positionY": 120,
      "configJson": "{\"language\":\"tha+eng\"}"
    }
  ],
  "edges": [
    {
      "sourceNodeKey": "start",
      "targetNodeKey": "ocr",
      "conditionExpression": null
    }
  ]
}
```

## Dashboard

| Method | Path | Description |
|---|---|---|
| GET | `/dashboard/summary` | Get processing summary |
| GET | `/dashboard/enterprise` | Get OCR, workflow, users, errors, performance, storage, and throughput metrics |

## Health

| Method | Path | Description |
|---|---|---|
| GET | `/health` | API liveness |
