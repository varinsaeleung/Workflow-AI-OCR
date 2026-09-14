# Docker CI/CD Operations Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add production-oriented Docker hardening, CI/CD, deployment guide, user manual, admin manual, and PostgreSQL/MinIO backup-restore operations for KM AI Workflow OCR Platform.

**Architecture:** Keep application code in Clean Architecture layers and add operations assets around the existing ASP.NET Core, React, PostgreSQL, RabbitMQ, MinIO, Ollama, and OpenSearch services. Docker images run as non-root users where practical, CI validates backend/frontend quality gates, and operational scripts use container-native open-source CLIs.

**Tech Stack:** Docker Compose, GitHub Actions, ASP.NET Core, React, PostgreSQL `pg_dump`/`psql`, MinIO `mc`, Bash, Linux.

**Spec:** User request in current task: Docker, CI/CD, Deployment Guide, User Manual, Admin Manual, Backup, Restore.

## Global Constraints

- Use Clean Architecture.
- Use SOLID.
- Use Dependency Injection.
- Use Repository Pattern.
- Use Unit of Work.
- Support Docker.
- Support Linux.
- Use PostgreSQL.
- Code must be production ready.
- No paid libraries.
- Every function must have comments.
- Every API must have Swagger.
- Every module must have unit tests.
- Answer step by step.

---

### Task 1: Docker Production Hardening

**Files:**
- Modify: `src/KmOcr.Api/Dockerfile`
- Modify: `src/KmOcr.Worker/Dockerfile`
- Modify: `apps/web/Dockerfile`
- Create: `deploy/docker-compose.prod.yml`

**Interfaces:**
- Consumes: existing `docker-compose.yml`, existing service environment variable names.
- Produces: production override file invoked by `docker compose -f docker-compose.yml -f deploy/docker-compose.prod.yml up -d`.

- [ ] Add non-root runtime users to API, Worker, and Web images.
- [ ] Add production compose override with required secrets using `${NAME:?message}`.
- [ ] Run Docker Compose config validation when Docker is available.

### Task 2: CI/CD Pipeline

**Files:**
- Create: `.github/workflows/ci-cd.yml`

**Interfaces:**
- Consumes: solution `KM.AIWorkflowOCR.sln`, React project `apps/web/package-lock.json`, Dockerfiles.
- Produces: CI pipeline with test, publish, frontend build, and Docker build jobs.

- [ ] Add backend restore/build/test/publish.
- [ ] Add frontend `npm ci`, test, build.
- [ ] Add Docker image build validation for API, Worker, and Web.

### Task 3: Backup And Restore Automation

**Files:**
- Create: `scripts/backup.sh`
- Create: `scripts/restore.sh`

**Interfaces:**
- Consumes: running Docker Compose services named `postgres` and `minio`.
- Produces: timestamped backup folder with PostgreSQL dump, MinIO mirror, and restore script that imports both.

- [ ] Write Linux Bash backup script with commented functions.
- [ ] Write Linux Bash restore script with commented functions.
- [ ] Validate shell syntax with `bash -n` where Bash is available.

### Task 4: Operations Documentation

**Files:**
- Create: `docs/DEPLOYMENT_GUIDE.md`
- Create: `docs/USER_MANUAL.md`
- Create: `docs/ADMIN_MANUAL.md`
- Create: `docs/BACKUP_RESTORE.md`
- Modify: `README.md`

**Interfaces:**
- Consumes: current application URLs, API endpoints, existing README flow.
- Produces: step-by-step deployment, user, admin, backup, and restore guides.

- [ ] Document Docker deployment for development and production.
- [ ] Document user workflows for login, upload, folder, metadata, OCR, AI, workflow designer, search, dashboard.
- [ ] Document admin workflows for RBAC, audit, monitoring, secrets, storage, indexing.
- [ ] Link all guides from README.

### Task 5: Verification

**Files:**
- No source files created.

**Interfaces:**
- Consumes: test suites and build scripts.
- Produces: command evidence for completion.

- [ ] Run backend unit tests.
- [ ] Run frontend unit tests.
- [ ] Run frontend production build.
- [ ] Run shell syntax checks.
- [ ] Run whitespace check.
