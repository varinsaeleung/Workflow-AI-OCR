# KM AI Workflow OCR Platform Installation

## Step 1: Requirements

- Linux server or workstation
- Docker Engine
- Docker Compose plugin
- Git
- At least 16 GB RAM for PostgreSQL, RabbitMQ, MinIO, OpenSearch, Ollama, API, Worker, and Web containers

## Step 2: Configure

Copy `.env.example` to `.env`.

```bash
cp .env.example .env
```

Replace every password and secret before shared or production use:

```text
POSTGRES_PASSWORD
RABBITMQ_DEFAULT_PASS
MINIO_ROOT_PASSWORD
JWT_SIGNING_KEY
SEED_ADMIN_PASSWORD
OPENSEARCH_INITIAL_ADMIN_PASSWORD
WEB_PUBLIC_ORIGIN
```

## Step 3: Run Development

```bash
docker compose up --build -d
```

Open:

```text
Web: http://localhost:3000
Swagger: http://localhost:8080/swagger
```

## Step 4: Run Production Style

```bash
docker compose -f docker-compose.yml -f deploy/docker-compose.prod.yml up --build -d
```

## Step 5: Login

Use the administrator values configured in `.env`.

```text
Email: SEED_ADMIN_EMAIL
Password: SEED_ADMIN_PASSWORD
```

## Step 6: Backup

```bash
bash scripts/backup.sh
```

## Step 7: Restore

```bash
bash scripts/restore.sh backups/<timestamp>
```

## Step 8: Documentation

- `README.md`
- `docs/DEPLOYMENT_GUIDE.md`
- `docs/CICD.md`
- `docs/USER_MANUAL.md`
- `docs/ADMIN_MANUAL.md`
- `docs/BACKUP_RESTORE.md`
