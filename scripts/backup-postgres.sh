#!/usr/bin/env bash
# =============================================================================
# US-081 — PostgreSQL backup script
# =============================================================================
# Performs a `pg_dump` of the IMS database and uploads to an S3-compatible
# bucket. Designed to run inside a small alpine container (or host with
# postgresql-client + aws-cli installed) on a daily cron.
#
# Required environment variables:
#   POSTGRES_HOST       (default: postgres)
#   POSTGRES_PORT       (default: 5432)
#   POSTGRES_DB         (required)
#   POSTGRES_USER       (required)
#   POSTGRES_PASSWORD   (required, also accepted via PGPASSWORD)
#   S3_BUCKET           (required, e.g. ims-backups)
#   S3_PREFIX           (default: postgres)
#   S3_ENDPOINT_URL     (optional, e.g. https://s3.amazonaws.com or MinIO URL)
#   AWS_ACCESS_KEY_ID   (required for upload)
#   AWS_SECRET_ACCESS_KEY (required for upload)
#   AWS_REGION          (default: us-east-1)
#   RETENTION_DAILY     (default: 7)
#   RETENTION_WEEKLY    (default: 4)
#   RETENTION_MONTHLY   (default: 12)
#
# Exit codes:
#   0 — success
#   1 — missing required environment variable
#   2 — pg_dump failure
#   3 — upload failure
# =============================================================================
set -euo pipefail

# ─── Required env ─────────────────────────────────────────────────────────────
: "${POSTGRES_DB:?POSTGRES_DB is required}"
: "${POSTGRES_USER:?POSTGRES_USER is required}"
: "${POSTGRES_PASSWORD:?POSTGRES_PASSWORD is required}"
: "${S3_BUCKET:?S3_BUCKET is required}"
: "${AWS_ACCESS_KEY_ID:?AWS_ACCESS_KEY_ID is required}"
: "${AWS_SECRET_ACCESS_KEY:?AWS_SECRET_ACCESS_KEY is required}"

POSTGRES_HOST="${POSTGRES_HOST:-postgres}"
POSTGRES_PORT="${POSTGRES_PORT:-5432}"
S3_PREFIX="${S3_PREFIX:-postgres}"
AWS_REGION="${AWS_REGION:-us-east-1}"
RETENTION_DAILY="${RETENTION_DAILY:-7}"
RETENTION_WEEKLY="${RETENTION_WEEKLY:-4}"
RETENTION_MONTHLY="${RETENTION_MONTHLY:-12}"

export PGPASSWORD="${POSTGRES_PASSWORD}"
export AWS_ACCESS_KEY_ID AWS_SECRET_ACCESS_KEY AWS_REGION

# ─── Compose backup name ──────────────────────────────────────────────────────
NOW="$(date -u +%Y%m%dT%H%M%SZ)"
DAY_OF_WEEK="$(date -u +%u)"   # 1=Mon..7=Sun
DAY_OF_MONTH="$(date -u +%d)"

if [[ "${DAY_OF_MONTH}" == "01" ]]; then
  TIER="monthly"
elif [[ "${DAY_OF_WEEK}" == "7" ]]; then
  TIER="weekly"
else
  TIER="daily"
fi

BACKUP_NAME="ims-${POSTGRES_DB}-${TIER}-${NOW}.sql.gz"
BACKUP_PATH="/tmp/${BACKUP_NAME}"
S3_KEY="${S3_PREFIX}/${TIER}/${BACKUP_NAME}"

echo "[backup-postgres] Tier=${TIER}  Target=s3://${S3_BUCKET}/${S3_KEY}"

# ─── Dump ──────────────────────────────────────────────────────────────────────
echo "[backup-postgres] pg_dump starting..."
if ! pg_dump \
      --host="${POSTGRES_HOST}" \
      --port="${POSTGRES_PORT}" \
      --username="${POSTGRES_USER}" \
      --dbname="${POSTGRES_DB}" \
      --no-owner --no-privileges \
      --format=plain \
    | gzip --best > "${BACKUP_PATH}"; then
  echo "[backup-postgres] ✗ pg_dump failed" >&2
  exit 2
fi

BACKUP_SIZE="$(stat -c%s "${BACKUP_PATH}" 2>/dev/null || stat -f%z "${BACKUP_PATH}")"
echo "[backup-postgres] ✓ dump done (${BACKUP_SIZE} bytes)"

# ─── Upload ────────────────────────────────────────────────────────────────────
S3_ARGS=()
if [[ -n "${S3_ENDPOINT_URL:-}" ]]; then
  S3_ARGS+=(--endpoint-url "${S3_ENDPOINT_URL}")
fi

echo "[backup-postgres] uploading to S3..."
if ! aws "${S3_ARGS[@]}" s3 cp "${BACKUP_PATH}" "s3://${S3_BUCKET}/${S3_KEY}" \
      --metadata "tier=${TIER},source=ims-monolith,db=${POSTGRES_DB}"; then
  echo "[backup-postgres] ✗ upload failed" >&2
  exit 3
fi

rm -f "${BACKUP_PATH}"
echo "[backup-postgres] ✓ uploaded s3://${S3_BUCKET}/${S3_KEY}"

# ─── Retention ────────────────────────────────────────────────────────────────
prune_tier() {
  local tier="$1"
  local keep="$2"
  echo "[backup-postgres] pruning '${tier}' (keep ${keep} most recent)..."

  mapfile -t keys < <(aws "${S3_ARGS[@]}" s3api list-objects-v2 \
      --bucket "${S3_BUCKET}" \
      --prefix "${S3_PREFIX}/${tier}/" \
      --query 'reverse(sort_by(Contents,&LastModified))[].Key' \
      --output text 2>/dev/null | tr '\t' '\n' | sed '/^None$/d')

  local i=0
  for k in "${keys[@]}"; do
    i=$((i + 1))
    if (( i > keep )); then
      echo "[backup-postgres]   - delete s3://${S3_BUCKET}/${k}"
      aws "${S3_ARGS[@]}" s3 rm "s3://${S3_BUCKET}/${k}" >/dev/null
    fi
  done
}

prune_tier "daily"   "${RETENTION_DAILY}"
prune_tier "weekly"  "${RETENTION_WEEKLY}"
prune_tier "monthly" "${RETENTION_MONTHLY}"

echo "[backup-postgres] ✓ done"
