#!/usr/bin/env bash
set -euo pipefail

BACKUP_ROOT="${BACKUP_ROOT:-./backups}"
TIMESTAMP="$(date -u +%Y%m%dT%H%M%SZ)"
BACKUP_DIR="${BACKUP_ROOT}/${TIMESTAMP}"
POSTGRES_DUMP="${BACKUP_DIR}/postgres.dump"
MINIO_DIR="${BACKUP_DIR}/minio"
MANIFEST_FILE="${BACKUP_DIR}/manifest.txt"

# Prints one timestamped log line for operators.
log() {
  printf '[%s] %s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)" "$*"
}

# Reads environment variables from a .env file when it exists.
load_environment() {
  if [ -f ".env" ]; then
    set -a
    # shellcheck disable=SC1091
    . ".env"
    set +a
  fi
}

# Creates the folder layout used by this backup run.
prepare_backup_directory() {
  mkdir -p "${MINIO_DIR}"
}

# Dumps PostgreSQL in custom format so restore can replace existing objects safely.
backup_postgres() {
  local database="${POSTGRES_DB:-km_ocr}"
  local user="${POSTGRES_USER:-km_ocr}"

  log "Backing up PostgreSQL database ${database}"
  docker compose exec -T postgres pg_dump --format=custom --clean --if-exists --username="${user}" "${database}" > "${POSTGRES_DUMP}"
}

# Mirrors the configured MinIO bucket into the backup directory on the host.
backup_minio() {
  export MINIO_ROOT_USER="${MINIO_ROOT_USER:-km_ocr}"
  export MINIO_ROOT_PASSWORD="${MINIO_ROOT_PASSWORD:-km_ocr_password}"
  export MINIO_BUCKET="${MINIO_BUCKET:-documents}"

  log "Backing up MinIO bucket ${MINIO_BUCKET}"
  docker compose run --rm --no-deps --entrypoint /bin/sh -e MINIO_ROOT_USER -e MINIO_ROOT_PASSWORD -e MINIO_BUCKET -v "$(pwd)/${MINIO_DIR}:/backup" minio-init -c '
    mc alias set local http://minio:9000 "$MINIO_ROOT_USER" "$MINIO_ROOT_PASSWORD" >/dev/null;
    mc mirror --overwrite "local/$MINIO_BUCKET" /backup;
  '
}

# Writes a small manifest so operators can identify what the backup contains.
write_manifest() {
  {
    printf 'created_at_utc=%s\n' "${TIMESTAMP}"
    printf 'postgres_dump=%s\n' "postgres.dump"
    printf 'minio_directory=%s\n' "minio"
    printf 'postgres_db=%s\n' "${POSTGRES_DB:-km_ocr}"
    printf 'minio_bucket=%s\n' "${MINIO_BUCKET:-documents}"
  } > "${MANIFEST_FILE}"
}

# Runs every backup step in order.
main() {
  load_environment
  prepare_backup_directory
  backup_postgres
  backup_minio
  write_manifest
  log "Backup complete: ${BACKUP_DIR}"
}

main "$@"
