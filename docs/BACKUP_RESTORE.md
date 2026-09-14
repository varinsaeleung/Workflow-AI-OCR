# Backup And Restore

## Backup Scope

The backup scripts protect:

- PostgreSQL database records
- MinIO document objects

RabbitMQ queues, OpenSearch indexes, and Ollama model cache are not treated as the source of truth. OpenSearch indexes can be rebuilt from PostgreSQL and documents.

## Step 1: Start Services

Backup requires PostgreSQL and MinIO containers to be running.

```bash
docker compose ps
```

## Step 2: Run Backup

```bash
bash scripts/backup.sh
```

The script creates:

```text
backups/<timestamp>/postgres.dump
backups/<timestamp>/minio/
backups/<timestamp>/manifest.txt
```

Set a custom location:

```bash
BACKUP_ROOT=/mnt/km-ocr-backups bash scripts/backup.sh
```

## Step 3: Verify Backup

```bash
ls -lah backups/<timestamp>
cat backups/<timestamp>/manifest.txt
```

Store backup copies outside the application server.

## Step 4: Restore

Stop API and Worker to prevent writes during restore:

```bash
docker compose stop api worker
```

Run restore:

```bash
bash scripts/restore.sh backups/<timestamp>
```

Start services:

```bash
docker compose up -d api worker web
```

## Step 5: Post-Restore Checks

1. Sign in to the web application.
2. Open dashboard.
3. Search for a known document.
4. Preview and download a known document.
5. Check API and Worker logs.

## Recommended Schedule

- Daily backup for production.
- Backup before every upgrade.
- Keep at least 7 daily backups and 4 weekly backups.
- Test restore monthly on a non-production server.
