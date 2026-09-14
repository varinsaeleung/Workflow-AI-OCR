# Release Notes

## KM AI Workflow OCR Platform

This package contains a complete enterprise starter platform for document management, OCR, AI extraction, workflow, search, dashboard, Docker deployment, CI/CD, and backup/restore.

## Included

- ASP.NET Core backend using Clean Architecture
- React frontend
- PostgreSQL schema and repositories
- RabbitMQ OCR queue
- PaddleOCR integration for PDF, JPG, PNG, Thai, and English
- MinIO document storage
- Ollama AI service for classification, summary, and metadata extraction
- OpenSearch indexing and search API
- Workflow engine and React node designer
- Dashboard with charts
- JWT, refresh token, RBAC, permission, and audit log
- Docker Compose and production override
- GitHub Actions CI/CD workflow
- Backup and restore scripts
- User, admin, deployment, CI/CD, and backup manuals

## Install

Read `INSTALL.md` first, then run:

```bash
cp .env.example .env
docker compose up --build -d
```

## Production

Replace all secrets in `.env`, then run:

```bash
docker compose -f docker-compose.yml -f deploy/docker-compose.prod.yml up --build -d
```
