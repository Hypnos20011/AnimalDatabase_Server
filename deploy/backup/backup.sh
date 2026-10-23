#!/bin/bash
set -euo pipefail

BACKUP_DIR="${BACKUP_DIR:-/backups}"
BACKUP_TIME_UTC="${BACKUP_TIME_UTC:-01:30}"
BACKUP_KEEP_DAYS="${BACKUP_KEEP_DAYS:-14}"
DATABASE="${MARIADB_DATABASE:-animaldatabase}"
DB_HOST="${DB_HOST:-mariadb}"

run_backup() {
  local stamp file
  stamp="$(date -u +%Y-%m-%d_%H%M%S)"
  file="${BACKUP_DIR}/${DATABASE}-${stamp}.sql.gz"
  mkdir -p "$BACKUP_DIR"
  if ! mariadb-dump -h "$DB_HOST" -uroot \
    --single-transaction --routines --triggers --events --hex-blob \
    --databases "$DATABASE" | gzip -9 > "${file}.partial"; then
    rm -f "${file}.partial"
    return 1
  fi
  mv "${file}.partial" "$file"
  find "$BACKUP_DIR" -name "${DATABASE}-*.sql.gz" -type f -mtime +"$((BACKUP_KEEP_DAYS - 1))" -delete
  echo "$(date -u +%FT%TZ) backup written: $(basename "$file") ($(du -h "$file" | cut -f1))"
}

if [ "${1:-}" = "now" ]; then
  run_backup
  exit 0
fi

echo "Nightly backup of ${DATABASE} at ${BACKUP_TIME_UTC} UTC, keeping ${BACKUP_KEEP_DAYS} days"
while true; do
  now="$(date -u +%s)"
  next="$(date -u -d "today ${BACKUP_TIME_UTC}" +%s)"
  if [ "$next" -le "$now" ]; then
    next="$(date -u -d "tomorrow ${BACKUP_TIME_UTC}" +%s)"
  fi
  sleep "$((next - now))"
  run_backup || echo "$(date -u +%FT%TZ) backup failed" >&2
done
