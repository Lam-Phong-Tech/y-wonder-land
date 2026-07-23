#!/usr/bin/env bash
set -Eeuo pipefail

[[ $# -eq 3 ]] || {
  echo "Usage: $0 <overlay-archive.tar.gz> <archive-sha256> <migration-id>" >&2
  exit 64
}
[[ ${EUID} -eq 0 ]] || { echo "Run as root." >&2; exit 77; }

archive="$1"
expected_sha="$2"
migration_id="$3"
web_root="/var/www/ywonder"
web_service="greenxland.service"
web_user="greenxland"
backup_root="/var/backups/ywonder-web"
stamp="$(date -u +%Y%m%dT%H%M%SZ)"
backup_dir="${backup_root}/point-sync-${stamp}"
candidate_next="${web_root}/.next.point-sync-${stamp}"
overlay="$(mktemp -d /tmp/ywonder-web-point-overlay.XXXXXX)"
stage="$(mktemp -d /tmp/ywonder-web-point-release.XXXXXX)"
live_db=""
db_uid=""
db_gid=""
db_mode=""
live_modified=0
next_moved=0
crontab_was_present=0

log() {
  printf '[web-point-deploy] %s\n' "$*"
}

database_path() {
  local database_url db_ref
  database_url="$(cd "${web_root}" && /usr/local/bin/node -e 'process.loadEnvFile(".env"); process.stdout.write(process.env.DATABASE_URL || "")')"
  [[ "${database_url}" == file:* ]] || {
    echo "Production web DATABASE_URL is not a SQLite file URL." >&2
    return 65
  }
  db_ref="${database_url#file:}"
  db_ref="${db_ref%%\?*}"
  if [[ "${db_ref}" = /* ]]; then
    printf '%s' "${db_ref}"
  else
    printf '%s' "${web_root}/prisma/${db_ref#./}"
  fi
}

backup_sqlite() {
  local source="$1" target="$2"
  if command -v sqlite3 >/dev/null; then
    sqlite3 "${source}" ".timeout 10000" ".backup '${target}'"
  elif command -v python3 >/dev/null; then
    python3 -c 'import sqlite3,sys; source=sqlite3.connect(sys.argv[1]); target=sqlite3.connect(sys.argv[2]); source.backup(target); target.close(); source.close()' \
      "${source}" "${target}"
  else
    echo "sqlite3 or python3 is required for a consistent SQLite backup." >&2
    return 69
  fi
}

verify_outbox_schema() {
  python3 -c '
import sqlite3,sys
db=sqlite3.connect(sys.argv[1])
columns={row[1] for row in db.execute("pragma table_info(\"GamePointSyncOutbox\")")}
indexes={row[1] for row in db.execute("pragma index_list(\"GamePointSyncOutbox\")")}
required_columns={"id","sourceTransactionId","userId","pointAmount","occurredAt","source","status","attempts","lastError","nextAttemptAt","sentAt","createdAt","updatedAt"}
required_indexes={"GamePointSyncOutbox_sourceTransactionId_key","GamePointSyncOutbox_status_nextAttemptAt_idx","GamePointSyncOutbox_userId_createdAt_idx"}
if columns != required_columns or not required_indexes.issubset(indexes):
    raise SystemExit("Web Point outbox schema verification failed")
db.close()
' "$1"
}

restore_live() {
  local rollback_failed=0
  log "rolling back web source, SQLite, build and crontab"
  systemctl stop "${web_service}" || rollback_failed=1

  rm -f -- \
    "${web_root}/lib/game-point-sync.ts" \
    "${web_root}/app/api/cron/game-point-sync/route.ts"
  rm -rf -- \
    "${web_root}/app/api/cron/game-point-sync" \
    "${web_root}/prisma/migrations/${migration_id}"
  tar -xzf "${backup_dir}/source.tar.gz" -C "${web_root}" || rollback_failed=1

  rm -f -- "${live_db}-wal" "${live_db}-shm"
  cp --preserve=mode,ownership,timestamps "${backup_dir}/web.db" "${live_db}" || rollback_failed=1
  chown "${db_uid}:${db_gid}" "${live_db}" || rollback_failed=1
  chmod "${db_mode}" "${live_db}" || rollback_failed=1

  rm -rf -- "${web_root}/.next"
  if [[ ${next_moved} -eq 1 && -d "${backup_dir}/next" ]]; then
    mv "${backup_dir}/next" "${web_root}/.next" || rollback_failed=1
  fi

  if [[ ${crontab_was_present} -eq 1 ]]; then
    crontab -u "${web_user}" "${backup_dir}/crontab.txt" || rollback_failed=1
  else
    crontab -r -u "${web_user}" 2>/dev/null || true
  fi

  if [[ -x "${web_root}/node_modules/.bin/prisma" ]]; then
    (cd "${web_root}" && ./node_modules/.bin/prisma generate >/dev/null) || rollback_failed=1
  fi
  chown -R "${web_user}:${web_user}" \
    "${web_root}/prisma/schema.prisma" \
    "${web_root}/lib/actions/convert.ts" \
    "${web_root}/deploy/crontab.txt" \
    "${web_root}/.next" 2>/dev/null || true
  systemctl start "${web_service}" || rollback_failed=1
  if [[ ${rollback_failed} -ne 0 ]]; then
    echo "ROLLBACK_INCOMPLETE=${backup_dir}" >&2
  else
    echo "ROLLBACK_COMPLETE=${backup_dir}" >&2
  fi
}

cleanup() {
  local exit_code=$?
  trap - EXIT
  if [[ ${exit_code} -ne 0 && ${live_modified} -eq 1 ]]; then
    restore_live
  fi
  rm -rf -- "${overlay}" "${stage}" "${candidate_next}"
  exit "${exit_code}"
}
trap cleanup EXIT

[[ -f "${archive}" ]] || { echo "Overlay archive not found." >&2; exit 66; }
[[ "${expected_sha}" =~ ^[0-9a-f]{64}$ ]] || { echo "Invalid archive hash." >&2; exit 65; }
[[ "${migration_id}" =~ ^[0-9]{14}_game_point_sync_outbox$ ]] || { echo "Invalid migration id." >&2; exit 65; }
[[ "$(sha256sum "${archive}" | awk '{print $1}')" == "${expected_sha}" ]] || {
  echo "Overlay archive checksum mismatch." >&2
  exit 65
}

tar -xzf "${archive}" -C "${overlay}"
for file in apply-web-point-sync-patch.js game-point-sync.ts cron-route.ts migration.sql; do
  [[ -f "${overlay}/${file}" ]] || { echo "Overlay missing ${file}." >&2; exit 65; }
done

# SSH can disconnect after the remote deployment completed but before the
# client received the final line. A retry must verify that exact completed
# state instead of attempting to patch production a second time.
existing_paths=0
for existing_path in \
  "${web_root}/lib/game-point-sync.ts" \
  "${web_root}/app/api/cron/game-point-sync/route.ts" \
  "${web_root}/prisma/migrations/${migration_id}/migration.sql"; do
  [[ -e "${existing_path}" ]] && existing_paths=$((existing_paths + 1))
done
if [[ ${existing_paths} -gt 0 ]]; then
  existing_db="$(database_path)"
  if [[ ${existing_paths} -eq 3 \
      && -f "${existing_db}" \
      && "$(grep -c '^model GamePointSyncOutbox' "${web_root}/prisma/schema.prisma")" -eq 1 \
      && "$(grep -c 'gamePointSyncOutbox.create' "${web_root}/lib/actions/convert.ts")" -eq 1 \
      && "$(crontab -u "${web_user}" -l 2>/dev/null | grep -c '/api/cron/game-point-sync')" -eq 1 ]] \
      && verify_outbox_schema "${existing_db}" 2>/dev/null \
      && [[ "$(systemctl is-active "${web_service}")" == "active" ]] \
      && [[ "$(curl -sS -o /dev/null -w '%{http_code}' http://127.0.0.1:3033/api/health)" == "200" ]] \
      && [[ "$(curl -sS -o /dev/null -w '%{http_code}' -X POST \
          -H 'Content-Type: application/json' -d '{}' \
          http://127.0.0.1:3000/internal/web/point-credit)" == "404" ]]; then
    echo "WEB_POINT_SYNC_DEPLOY=already-applied"
    echo "WEB_SERVICE=active"
    echo "OUTBOX_SCHEMA=pass"
    echo "OUTBOX_CRON=pass"
    echo "GAME_TOPUP_ENDPOINT=disabled"
    exit 0
  fi
  echo "Partial or inconsistent web Point sync deployment detected. Refusing automatic changes." >&2
  exit 73
fi

declare -A expected_sources=(
  ["prisma/schema.prisma"]="e33cb40624d6d5d6d4ac9ddcc0a29ca0e8cffba23bad07bdc346e31290c25b81"
  ["lib/actions/convert.ts"]="0462df0862bf5c2b0c9bb2ce0e5f15d15fcddecf7ee831ddebd486e9ea1d4208"
  ["deploy/crontab.txt"]="f21d7c04d897e53062473aaffa3b4806415007e211eaf54a13f5cd3c3f4152ae"
)
for relative in "${!expected_sources[@]}"; do
  actual="$(sha256sum "${web_root}/${relative}" | awk '{print $1}')"
  [[ "${actual}" == "${expected_sources[${relative}]}" ]] || {
    echo "Live web source changed after staging: ${relative}. Refusing deployment." >&2
    exit 73
  }
done

live_db="$(database_path)"
[[ -f "${live_db}" ]] || { echo "Production SQLite DB file was not found." >&2; exit 66; }
[[ -d "${web_root}/.next" ]] || { echo "Production Next.js build was not found." >&2; exit 66; }
db_uid="$(stat -c '%u' "${live_db}")"
db_gid="$(stat -c '%g' "${live_db}")"
db_mode="$(stat -c '%a' "${live_db}")"
for new_path in \
  "${web_root}/lib/game-point-sync.ts" \
  "${web_root}/app/api/cron/game-point-sync" \
  "${web_root}/prisma/migrations/${migration_id}"; do
  [[ ! -e "${new_path}" ]] || {
    echo "Point sync path already exists: ${new_path}. Refusing a non-idempotent redeploy." >&2
    exit 73
  }
done

log "building an isolated candidate before touching production"
tar -C "${web_root}" \
  --exclude='./.env' \
  --exclude='./.next' \
  --exclude='./node_modules' \
  --exclude='*.db' \
  --exclude='*.db-*' \
  --exclude='*.sqlite' \
  --exclude='*.sqlite-*' \
  -cf - . | tar -C "${stage}" -xf -
cp --preserve=mode,ownership,timestamps "${web_root}/.env" "${stage}/.env"
node "${overlay}/apply-web-point-sync-patch.js" "${stage}" "${migration_id}"

cd "${stage}"
npm ci --ignore-scripts --no-audit --no-fund
npx prisma generate
backup_sqlite "${live_db}" "${stage}/prisma/validation.db"
DATABASE_URL="file:${stage}/prisma/validation.db" npx prisma db execute \
  --file "prisma/migrations/${migration_id}/migration.sql" \
  --schema prisma/schema.prisma
verify_outbox_schema "${stage}/prisma/validation.db"
DATABASE_URL="file:${stage}/prisma/validation.db" npm run build

for relative in "${!expected_sources[@]}"; do
  actual="$(sha256sum "${web_root}/${relative}" | awk '{print $1}')"
  [[ "${actual}" == "${expected_sources[${relative}]}" ]] || {
    echo "Live source changed during candidate build: ${relative}." >&2
    exit 74
  }
done

rm -rf -- "${candidate_next}"
cp -a "${stage}/.next" "${candidate_next}"
chown -R "${web_user}:${web_user}" "${candidate_next}"

install -d -m 0700 "${backup_dir}"
if crontab -u "${web_user}" -l > "${backup_dir}/crontab.txt" 2>/dev/null; then
  crontab_was_present=1
else
  : > "${backup_dir}/crontab.txt"
fi

log "stopping web briefly for consistent backup and atomic build switch"
systemctl stop "${web_service}"
tar -czf "${backup_dir}/source.tar.gz" -C "${web_root}" \
  prisma/schema.prisma lib/actions/convert.ts deploy/crontab.txt
backup_sqlite "${live_db}" "${backup_dir}/web.db"
live_modified=1

DATABASE_URL="file:${live_db}" "${stage}/node_modules/.bin/prisma" db execute \
  --file "${stage}/prisma/migrations/${migration_id}/migration.sql" \
  --schema "${stage}/prisma/schema.prisma"
verify_outbox_schema "${live_db}"

install -m 0644 "${stage}/prisma/schema.prisma" "${web_root}/prisma/schema.prisma"
install -m 0644 "${stage}/lib/actions/convert.ts" "${web_root}/lib/actions/convert.ts"
install -m 0644 "${stage}/lib/game-point-sync.ts" "${web_root}/lib/game-point-sync.ts"
install -d -m 0755 "${web_root}/app/api/cron/game-point-sync"
install -m 0644 "${stage}/app/api/cron/game-point-sync/route.ts" \
  "${web_root}/app/api/cron/game-point-sync/route.ts"
install -d -m 0755 "${web_root}/prisma/migrations/${migration_id}"
install -m 0644 "${stage}/prisma/migrations/${migration_id}/migration.sql" \
  "${web_root}/prisma/migrations/${migration_id}/migration.sql"
install -m 0644 "${stage}/deploy/crontab.txt" "${web_root}/deploy/crontab.txt"

mv "${web_root}/.next" "${backup_dir}/next"
next_moved=1
mv "${candidate_next}" "${web_root}/.next"

if ! grep -q '/api/cron/game-point-sync' "${backup_dir}/crontab.txt"; then
  cp "${backup_dir}/crontab.txt" "${backup_dir}/crontab.new"
  printf '%s\n' \
    '' \
    '# Retry durable web -> game Point outbox every minute' \
    '* * * * * curl -sS -X POST -H "$HEADER" http://127.0.0.1:3033/api/cron/game-point-sync >> /var/log/greenxland/cron-game-point-sync.log 2>&1' \
    >> "${backup_dir}/crontab.new"
  crontab -u "${web_user}" "${backup_dir}/crontab.new"
fi

chown -R "${web_user}:${web_user}" \
  "${web_root}/prisma/schema.prisma" \
  "${web_root}/prisma/migrations/${migration_id}" \
  "${web_root}/lib/actions/convert.ts" \
  "${web_root}/lib/game-point-sync.ts" \
  "${web_root}/app/api/cron/game-point-sync" \
  "${web_root}/deploy/crontab.txt" \
  "${web_root}/.next"

(cd "${web_root}" && ./node_modules/.bin/prisma generate >/dev/null)
chown -R "${web_user}:${web_user}" \
  "${web_root}/node_modules/.prisma" \
  "${web_root}/node_modules/@prisma/client" 2>/dev/null || true

install -d -o "${web_user}" -g "${web_user}" -m 0755 /var/log/greenxland
systemctl start "${web_service}"

for _ in $(seq 1 30); do
  if [[ "$(curl -sS -o /tmp/ywonder-web-health.out -w '%{http_code}' http://127.0.0.1:3033/api/health || true)" == "200" ]]; then
    break
  fi
  sleep 1
done
[[ "$(systemctl is-active "${web_service}")" == "active" ]] || {
  echo "Web service did not become active." >&2
  exit 75
}
[[ "$(curl -sS -o /dev/null -w '%{http_code}' http://127.0.0.1:3033/api/health)" == "200" ]] || {
  echo "Web health check failed." >&2
  exit 75
}

cron_secret="$(cd "${web_root}" && /usr/local/bin/node -e 'process.loadEnvFile(".env"); process.stdout.write(process.env.CRON_SECRET || "")')"
[[ ${#cron_secret} -ge 32 ]] || { echo "CRON_SECRET is not configured." >&2; exit 75; }
cron_http="$(curl -sS -o "${backup_dir}/cron-response.json" -w '%{http_code}' \
  -X POST -H "Authorization: Bearer ${cron_secret}" \
  http://127.0.0.1:3033/api/cron/game-point-sync)"
unset cron_secret
[[ "${cron_http}" == "200" ]] || { echo "Authenticated outbox cron check failed." >&2; exit 75; }

[[ "$(curl -sS -o /dev/null -w '%{http_code}' -X POST \
  -H 'Content-Type: application/json' -d '{}' \
  http://127.0.0.1:3000/internal/web/point-credit)" == "404" ]] || {
  echo "Game Point ingress was unexpectedly enabled." >&2
  exit 75
}

verify_outbox_schema "${live_db}"
[[ "$(grep -c '^model GamePointSyncOutbox' "${web_root}/prisma/schema.prisma")" -eq 1 ]]
[[ "$(grep -c 'gamePointSyncOutbox.create' "${web_root}/lib/actions/convert.ts")" -eq 1 ]]
[[ "$(find "${web_root}/app/api/cron/game-point-sync" -name route.ts -type f | wc -l)" -eq 1 ]]
[[ "$(crontab -u "${web_user}" -l | grep -c '/api/cron/game-point-sync')" -eq 1 ]]

live_modified=0
echo "WEB_POINT_SYNC_DEPLOY=success"
echo "WEB_SERVICE=active"
echo "OUTBOX_SCHEMA=pass"
echo "OUTBOX_CRON=pass"
echo "GAME_TOPUP_ENDPOINT=disabled"
echo "BACKUP_DIR=${backup_dir}"
