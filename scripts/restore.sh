#!/usr/bin/env bash
set -euo pipefail

BACKUP_DIR="${1:-}"
POSTGRES_DUMP="${BACKUP_DIR}/postgres.dump"
MINIO_DIR="${BACKUP_DIR}/minio"

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

# Validates that the requested backup folder contains the required artifacts.
validate_backup_directory() {
  if [ -z "${BACKUP_DIR}" ]; then
    printf 'Usage: %s <backup-directory>\n' "$0" >&2
    exit 2
  fi

  if [ ! -f "${POSTGRES_DUMP}" ]; then
    printf 'PostgreSQL dump not found: %s\n' "${POSTGRES_DUMP}" >&2
    exit 2
  fi

  if [ ! -d "${MINIO_DIR}" ]; then
    printf 'MinIO backup directory not found: %s\n' "${MINIO_DIR}" >&2
    exit 2
  fi
}

# Restores PostgreSQL from the custom-format dump created by backup.sh.
restore_postgres() {
  local database="${POSTGRES_DB:-km_ocr}"
  local user="${POSTGRES_USER:-km_ocr}"

  log "Restoring PostgreSQL database ${database}"
  docker compose exec -T postgres pg_restore --clean --if-exists --no-owner --username="${user}" --dbname="${database}" < "${POSTGRES_DUMP}"
}

# Mirrors backup files back into the configured MinIO bucket.
restore_minio() {
  export MINIO_ROOT_USER="${MINIO_ROOT_USER:-km_ocr}"
  export MINIO_ROOT_PASSWORD="${MINIO_ROOT_PASSWORD:-km_ocr_password}"
  export MINIO_BUCKET="${MINIO_BUCKET:-documents}"

  log "Restoring MinIO bucket ${MINIO_BUCKET}"
  docker compose run --rm --no-deps --entrypoint /bin/sh -e MINIO_ROOT_USER -e MINIO_ROOT_PASSWORD -e MINIO_BUCKET -v "$(pwd)/${MINIO_DIR}:/backup:ro" minio-init -c '
    mc alias set local http://minio:9000 "$MINIO_ROOT_USER" "$MINIO_ROOT_PASSWORD" >/dev/null;
    mc mb --ignore-existing "local/$MINIO_BUCKET";
    mc mirror --overwrite /backup "local/$MINIO_BUCKET";
  '
}

# Runs every restore step in order.
main() {
  load_environment
  validate_backup_directory
  restore_postgres
  restore_minio
  log "Restore complete from: ${BACKUP_DIR}"
}

main "$@"
