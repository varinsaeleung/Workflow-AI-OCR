# Admin Manual

## Step 1: Access Control

The platform uses JWT, refresh tokens, RBAC roles, and permission claims.

Core permissions:

- `documents.read`
- `documents.write`
- `documents.ai`
- `documents.delete`
- `workflow.read`
- `workflow.write`
- `workflow.approve`
- `dashboard.read`
- `admin.users`

## Step 2: Audit Log

Authentication, authorization denial, and sensitive operations should be reviewed through audit log records in PostgreSQL.

Recommended checks:

- Failed login attempts
- Token refresh activity
- Permission denied events
- Document delete events
- Workflow approval events

## Step 3: Storage Administration

MinIO stores binary document files. PostgreSQL stores document metadata and version references.

Operational checks:

```bash
docker compose logs minio
docker compose exec minio mc ls local/documents
```

## Step 4: OCR Administration

Worker containers consume RabbitMQ OCR jobs and run PaddleOCR.

Operational checks:

```bash
docker compose logs worker
docker compose logs rabbitmq
```

## Step 5: AI Administration

Ollama provides local AI classification, summary, and metadata extraction.

Change model in `.env`:

```text
OLLAMA_MODEL=llama3.1
```

Restart Ollama init and dependent services after changing model:

```bash
docker compose up -d ollama ollama-init api worker
```

## Step 6: Search Administration

OpenSearch stores the document search index. PostgreSQL remains the source of truth.

Re-index a document through Swagger:

```text
POST /api/v1/search/documents/{documentId}/index
```

## Step 7: Security Checklist

- Replace all `.env` secrets before production use.
- Keep database and object storage ports private.
- Put TLS termination in front of Web/API.
- Rotate JWT signing key by planned maintenance procedure.
- Review admin accounts monthly.
- Run backup before upgrade.

## Step 8: Monitoring Checklist

Check these daily:

- API container health
- Worker logs
- RabbitMQ queue depth
- PostgreSQL disk usage
- MinIO bucket usage
- OpenSearch heap pressure
- Failed OCR count
- Failed login and permission denied audit events
