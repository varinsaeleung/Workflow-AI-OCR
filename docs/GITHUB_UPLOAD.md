# GitHub Upload Guide

## Step 1: Verify Ignored Files

Do not upload secrets or generated output.

Ignored by `.gitignore`:

- `.env`
- `.env.*`
- `.dotnet-sdk/`
- `node_modules/`
- `bin/`
- `obj/`
- `dist/`
- `coverage/`
- `TestResults/`
- `artifacts/`

## Step 2: Initialize Repository

If the repository has no commits yet:

```bash
git add .
git commit -m "feat: initial km ai workflow ocr platform"
```

## Step 3: Create GitHub Repository

Create an empty GitHub repository without README, license, or gitignore because this project already includes those files.

## Step 4: Add Remote

```bash
git remote add origin https://github.com/<org-or-user>/<repo-name>.git
```

If a remote already exists:

```bash
git remote set-url origin https://github.com/<org-or-user>/<repo-name>.git
```

## Step 5: Push

```bash
git branch -M main
git push -u origin main
```

## Step 6: Enable CI/CD

GitHub Actions will run automatically from:

```text
.github/workflows/ci-cd.yml
```

## Step 7: Protect Production Secrets

Keep production values out of Git. Store them in:

- Server-side `.env`
- GitHub Environments
- GitHub Actions secrets
- Organization-approved secret manager
