# PostgreSQL DDL

The production-ready DDL is stored here:

```text
database/postgresql/001_enterprise_schema.sql
```

## Step 1: Extensions

The schema enables:

```sql
create extension if not exists pgcrypto;
create extension if not exists pg_trgm;
```

`pgcrypto` provides `gen_random_uuid()`. `pg_trgm` supports fast fuzzy text filtering on file names and metadata values.

## Step 2: Tables

The DDL creates enterprise tables for:

```text
User
Permission
Authentication
Document Management
Metadata
OCR
AI
Workflow
Audit
```

## Step 3: Indexes

The DDL includes B-tree, partial, and GIN indexes. Important examples:

```sql
create unique index if not exists ux_users_email_active
on users (lower(email))
where deleted_at is null;

create index if not exists ix_documents_search_vector
on documents using gin (search_vector);

create index if not exists ix_ocr_results_search_vector
on ocr_results using gin (search_vector);

create index if not exists ix_ai_extractions_entities_json
on ai_extractions using gin (extracted_entities_json);
```

Authentication-specific indexes:

```sql
create unique index if not exists ux_refresh_tokens_token_hash
on refresh_tokens (token_hash);

create index if not exists ix_refresh_tokens_user_active
on refresh_tokens (user_id, expires_at desc)
where revoked_at is null;
```

## Step 4: Constraints

The DDL includes primary keys, foreign keys, unique constraints, and check constraints.

`refresh_tokens` stores only opaque-token hashes, expiration, rotation, IP, and user-agent audit metadata. `users.password_hash` is nullable in the shared enterprise schema so external identity synchronization remains possible; local seeded accounts always have a PBKDF2 hash.

Examples:

```sql
constraint ck_ocr_results_confidence_score
check (confidence_score >= 0 and confidence_score <= 1)
```

```sql
constraint ck_documents_file_size_positive
check (file_size_bytes is null or file_size_bytes > 0)
```

```sql
constraint ck_workflow_tasks_status
check (status in ('Pending', 'Approved', 'Rejected', 'Skipped', 'Cancelled'))
```

## Step 5: Docker

The SQL file is mounted into PostgreSQL as an init script:

```text
/docker-entrypoint-initdb.d/001_enterprise_schema.sql
```

It runs automatically only when the PostgreSQL volume is created for the first time.
