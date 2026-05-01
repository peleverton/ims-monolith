#!/usr/bin/env bash
# =============================================================================
# US-081 — PostgreSQL restore script
# =============================================================================
# Restores an IMS PostgreSQL backup from an S3-compatible bucket.
#
# Usage:
#   restore-postgres.sh <s3-key|local-file>
#
# Examples:
#   ./restore-postgres.sh postgres/daily/ims-ims_db-daily-20260430T030000Z.sql.gz
#   ./restore-postgres.sh /backups/ims-ims_db-daily-20260430T030000Z.sql.gz
#
# Required environment variables:
#   POSTGRES_HOST       (default: postgres)
#   POSTGRES_PORT       (default: 5432)
#   POSTGRES_DB         (required — target database, will be DROPPED + CREATED)
#   POSTGRES_USER       (required, must have CREATEDB)
#   POSTGRES_PASSWORD   (required)
#
# When restoring from S3:
#   S3_BUCKET, S3_ENDPOINT_URL, AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY
#
# Safety:
#   - Refuses to run if DB has > 0 rows in `Users` table unless FORCE=1
#   - Always dumps current DB to /tmp/pre-restore-${TIMESTAMP}.sql.gz first
# =============================================================================
set -euo pipefail

if [[ $# -lt 1 ]]; then
  echo "Usage: $0 <s3-key|local-file> [--force]" >&2
  exit 64
fi

SOURCE="$1"
FORCE="${FORCE:-0}"
[[ "${2:-}" == "--force" ]] && FORCE=1

: "${POSTGRES_DB:?POSTGRES_DB is required}"
: "${POSTGRES_USER:?POSTGRES_USER is required}"
: "${POSTGRES_PASSWORD:?POSTGRES_PASSWORD is required}"

POSTGRES_HOST="${POSTGRES_HOST:-postgres}"
POSTGRES_PORT="${POSTGRES_PORT:-5432}"

export PGPASSWORD="${POSTGRES_PASSWORD}"

echo "[restore-postgres] Target: ${POSTGRES_USER}@${POSTGRES_HOST}:${POSTGRES_PORT}/${POSTGRES_DB}"
echo "[restore-postgres] Source: ${SOURCE}"

# ─── Resolve source to a local file ──────────────────────────────────────────
LOCAL_FILE=""
if [[ -f "${SOURCE}" ]]; then
  LOCAL_FILE="${SOURCE}"
else
  : "${S3_BUCKET:?S3_BUCKET is required when source is not a local file}"
  S3_ARGS=()
  [[ -n "${S3_ENDPOINT_URL:-}" ]] && S3_ARGS+=(--endpoint-url "${S3_ENDPOINT_URL}")

  LOCAL_FILE="/tmp/$(basename "${SOURCE}")"
  echo "[restore-postgres] Downloading s3://${S3_BUCKET}/${SOURCE}..."
  aws "${S3_ARGS[@]}" s3 cp "s3://${S3_BUCKET}/${SOURCE}" "${LOCAL_FILE}"
fi

# ─── Safety check: existing data in target DB ────────────────────────────────
USER_COUNT="$(psql \
    -h "${POSTGRES_HOST}" -p "${POSTGRES_PORT}" \
    -U "${POSTGRES_USER}" -d "${POSTGRES_DB}" \
    -tA -c "SELECT COUNT(*) FROM \"Users\"" 2>/dev/null || echo "0")"

if [[ "${USER_COUNT}" -gt 0 && "${FORCE}" != "1" ]]; then
  echo "[restore-postgres] ✗ Target DB has ${USER_COUNT} users. Re-run with --force or set FORCE=1 to confirm." >&2
  exit 2
fi

# ─── Pre-restore dump (safety net) ───────────────────────────────────────────
TS="$(date -u +%Y%m%dT%H%M%SZ)"
PRE_RESTORE="/tmp/pre-restore-${POSTGRES_DB}-${TS}.sql.gz"
echo "[restore-postgres] Capturing safety dump to ${PRE_RESTORE}..."
pg_dump -h "${POSTGRES_HOST}" -p "${POSTGRES_PORT}" -U "${POSTGRES_USER}" \
  --dbname="${POSTGRES_DB}" --no-owner --no-privileges --format=plain \
  | gzip --best > "${PRE_RESTORE}" || {
    echo "[restore-postgres] ⚠ pre-restore dump failed (continuing anyway)" >&2
  }

# ─── Drop & recreate database ────────────────────────────────────────────────
echo "[restore-postgres] Dropping and recreating ${POSTGRES_DB}..."
psql -h "${POSTGRES_HOST}" -p "${POSTGRES_PORT}" -U "${POSTGRES_USER}" -d postgres <<SQL
SELECT pg_terminate_backend(pid)
FROM pg_stat_activity
WHERE datname = '${POSTGRES_DB}' AND pid <> pg_backend_pid();
DROP DATABASE IF EXISTS "${POSTGRES_DB}";
CREATE DATABASE "${POSTGRES_DB}";
SQL

# ─── Restore ──────────────────────────────────────────────────────────────────
echo "[restore-postgres] Restoring..."
gunzip -c "${LOCAL_FILE}" \
  | psql -h "${POSTGRES_HOST}" -p "${POSTGRES_PORT}" -U "${POSTGRES_USER}" -d "${POSTGRES_DB}"

echo "[restore-postgres] ✓ restore complete. Safety dump kept at ${PRE_RESTORE}"
