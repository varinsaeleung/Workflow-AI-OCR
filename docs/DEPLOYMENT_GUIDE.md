# Deployment Guide

## Step 1: Prepare Linux Server

Install Docker Engine, Docker Compose plugin, Git, and at least 16 GB RAM for OCR, OpenSearch, and Ollama on one host.

```bash
sudo apt-get update
sudo apt-get install -y ca-certificates curl git
```

Follow the official Docker Engine installation guide for your Linux distribution, then verify:

```bash
docker --version
docker compose version
```

## Step 2: Configure Environment

Copy the sample environment file and replace every secret.

```bash
cp .env.example .env
```

Minimum production values:

```text
POSTGRES_PASSWORD=<strong-password>
RABBITMQ_DEFAULT_PASS=<strong-password>
MINIO_ROOT_PASSWORD=<strong-password>
JWT_SIGNING_KEY=<random-secret-at-least-32-bytes>
SEED_ADMIN_PASSWORD=<strong-temporary-password>
WEB_PUBLIC_ORIGIN=https://your-domain.example
OPENSEARCH_INITIAL_ADMIN_PASSWORD=<strong-password>
```

## Step 3: Build And Start

Development:

```bash
docker compose up --build -d
```

Production override:

```bash
docker compose -f docker-compose.yml -f deploy/docker-compose.prod.yml up --build -d
```

## Step 4: Verify Services

Check running containers:

```bash
docker compose ps
```

Open the application:

```text
http://<server-ip>:3000
```

Open Swagger:

```text
http://<server-ip>:8080/swagger
```

## Step 5: Initial Admin Login

Sign in with `SEED_ADMIN_EMAIL` and `SEED_ADMIN_PASSWORD`, then change the administrator password process according to your organization policy before allowing users onto the system.

## Step 6: Operational Ports

Expose only required public ports through firewall or reverse proxy:

```text
3000 Web UI
8080 API and Swagger
```

Keep PostgreSQL, RabbitMQ, MinIO, Ollama, and OpenSearch internal unless operations explicitly need temporary access.

## Step 7: Upgrade

Pull the latest source and rebuild:

```bash
git pull
docker compose -f docker-compose.yml -f deploy/docker-compose.prod.yml up --build -d
```

Run backup before every upgrade.

## Step 8: Rollback

Checkout the previous release tag or commit, then restart:

```bash
git checkout <previous-release>
docker compose -f docker-compose.yml -f deploy/docker-compose.prod.yml up --build -d
```

Restore data only when schema or data migration created an unrecoverable issue.
