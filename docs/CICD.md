# CI/CD Guide

## Step 1: Pipeline Trigger

The GitHub Actions workflow runs on:

- Push to `main`
- Push to `develop`
- Pull request to `main`
- Pull request to `develop`
- Manual `workflow_dispatch`

Workflow file:

```text
.github/workflows/ci-cd.yml
```

## Step 2: Backend Quality Gate

The backend job performs:

```bash
dotnet restore KM.AIWorkflowOCR.sln
dotnet build KM.AIWorkflowOCR.sln --configuration Release --no-restore
dotnet test KM.AIWorkflowOCR.sln --configuration Release --no-build
dotnet publish src/KmOcr.Api/KmOcr.Api.csproj --configuration Release
dotnet publish src/KmOcr.Worker/KmOcr.Worker.csproj --configuration Release
```

## Step 3: Frontend Quality Gate

The frontend job performs:

```bash
cd apps/web
npm ci
npm test -- --run --maxWorkers=1
npm run build
```

## Step 4: Docker Quality Gate

The Docker job builds:

```bash
docker build -f src/KmOcr.Api/Dockerfile -t km-ocr-api:ci .
docker build -f src/KmOcr.Worker/Dockerfile -t km-ocr-worker:ci .
docker build -f apps/web/Dockerfile -t km-ocr-web:ci .
docker compose -f docker-compose.yml config
```

## Step 5: Deployment Promotion

For production, promote only builds that pass all jobs. The recommended release flow is:

1. Merge pull request into `main`.
2. Tag the release.
3. Pull the release tag on the Linux server.
4. Run backup.
5. Start services with production compose override.

```bash
git fetch --tags
git checkout <release-tag>
bash scripts/backup.sh
docker compose -f docker-compose.yml -f deploy/docker-compose.prod.yml up --build -d
```

## Step 6: Rollback

If validation fails after deployment:

```bash
git checkout <previous-release-tag>
docker compose -f docker-compose.yml -f deploy/docker-compose.prod.yml up --build -d
```

Restore backup only when application rollback is not enough.
