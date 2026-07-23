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

[[ -f "${archive}" ]] || { echo "Overlay archive not found." >&2; exit 66; }
[[ "${expected_sha}" =~ ^[0-9a-f]{64}$ ]] || { echo "Invalid archive hash." >&2; exit 65; }
[[ "${migration_id}" =~ ^[0-9]{14}_game_point_sync_outbox$ ]] || { echo "Invalid migration id." >&2; exit 65; }
[[ "$(sha256sum "${archive}" | awk '{print $1}')" == "${expected_sha}" ]] || {
  echo "Overlay archive checksum mismatch." >&2
  exit 65
}

overlay="$(mktemp -d /tmp/ywonder-web-point-overlay.XXXXXX)"
stage="$(mktemp -d /tmp/ywonder-web-point-stage.XXXXXX)"
cleanup() {
  rm -rf -- "${overlay}" "${stage}"
}
trap cleanup EXIT

tar -xzf "${archive}" -C "${overlay}"
for file in apply-web-point-sync-patch.js game-point-sync.ts cron-route.ts migration.sql; do
  [[ -f "${overlay}/${file}" ]] || { echo "Overlay missing ${file}." >&2; exit 65; }
done

declare -A expected_sources=(
  ["prisma/schema.prisma"]="e33cb40624d6d5d6d4ac9ddcc0a29ca0e8cffba23bad07bdc346e31290c25b81"
  ["lib/actions/convert.ts"]="0462df0862bf5c2b0c9bb2ce0e5f15d15fcddecf7ee831ddebd486e9ea1d4208"
  ["deploy/crontab.txt"]="f21d7c04d897e53062473aaffa3b4806415007e211eaf54a13f5cd3c3f4152ae"
)
for relative in "${!expected_sources[@]}"; do
  actual="$(sha256sum "${web_root}/${relative}" | awk '{print $1}')"
  [[ "${actual}" == "${expected_sources[${relative}]}" ]] || {
    echo "Live web source changed after audit: ${relative}. Refusing staged validation." >&2
    exit 73
  }
done

echo "[stage] copying source without live DB, environment, node_modules or build output"
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

echo "[stage] installing isolated dependencies and generating Prisma client"
cd "${stage}"
npm ci --ignore-scripts --no-audit --no-fund
npx prisma generate

database_url="$(cd "${web_root}" && /usr/local/bin/node -e 'process.loadEnvFile(".env"); process.stdout.write(process.env.DATABASE_URL || "")')"
[[ "${database_url}" == file:* ]] || {
  echo "Production web DATABASE_URL is not a SQLite file URL." >&2
  exit 65
}
db_ref="${database_url#file:}"
db_ref="${db_ref%%\?*}"
if [[ "${db_ref}" = /* ]]; then
  live_db="${db_ref}"
else
  live_db="${web_root}/prisma/${db_ref#./}"
fi
[[ -f "${live_db}" ]] || { echo "Production SQLite DB file was not found." >&2; exit 66; }

validation_db="${stage}/prisma/validation.db"
if command -v sqlite3 >/dev/null; then
  sqlite3 "${live_db}" ".timeout 10000" ".backup '${validation_db}'"
elif command -v python3 >/dev/null; then
  python3 -c 'import sqlite3,sys; source=sqlite3.connect(sys.argv[1]); target=sqlite3.connect(sys.argv[2]); source.backup(target); target.close(); source.close()' \
    "${live_db}" "${validation_db}"
elif [[ ! -f "${live_db}-wal" ]]; then
  cp --preserve=mode,timestamps "${live_db}" "${validation_db}"
else
  echo "sqlite3 is required because the production database has an active WAL." >&2
  exit 69
fi
echo "[stage] applying migration to a copied production SQLite database"
DATABASE_URL="file:${validation_db}" npx prisma db execute \
  --file "prisma/migrations/${migration_id}/migration.sql" \
  --schema prisma/schema.prisma

python3 -c '
import sqlite3,sys
db=sqlite3.connect(sys.argv[1])
columns={row[1] for row in db.execute("pragma table_info(\"GamePointSyncOutbox\")")}
indexes={row[1] for row in db.execute("pragma index_list(\"GamePointSyncOutbox\")")}
required_columns={"id","sourceTransactionId","userId","pointAmount","occurredAt","source","status","attempts","lastError","nextAttemptAt","sentAt","createdAt","updatedAt"}
required_indexes={"GamePointSyncOutbox_sourceTransactionId_key","GamePointSyncOutbox_status_nextAttemptAt_idx","GamePointSyncOutbox_userId_createdAt_idx"}
if columns != required_columns or not required_indexes.issubset(indexes):
    raise SystemExit("Copied DB migration verification failed")
db.close()
' "${validation_db}"

echo "[stage] building complete Next.js application"
DATABASE_URL="file:${validation_db}" npm run build

schema_count="$(grep -c '^model GamePointSyncOutbox' prisma/schema.prisma)"
hook_count="$(grep -c 'gamePointSyncOutbox.create' lib/actions/convert.ts)"
route_count="$(find app/api/cron/game-point-sync -name route.ts -type f | wc -l)"
[[ "${schema_count}" -eq 1 && "${hook_count}" -eq 1 && "${route_count}" -eq 1 ]] || {
  echo "Patched artifact verification failed." >&2
  exit 74
}

for relative in "${!expected_sources[@]}"; do
  actual="$(sha256sum "${web_root}/${relative}" | awk '{print $1}')"
  [[ "${actual}" == "${expected_sources[${relative}]}" ]] || {
    echo "Live source changed during staged validation: ${relative}." >&2
    exit 74
  }
done

echo "WEB_POINT_SYNC_STAGE_VALIDATION=success"
echo "LIVE_WEB_UNCHANGED=yes"
