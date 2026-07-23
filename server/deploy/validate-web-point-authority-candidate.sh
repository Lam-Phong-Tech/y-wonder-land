#!/usr/bin/env bash
set -Eeuo pipefail
umask 077

[[ $# -eq 3 ]] || {
  echo "Usage: $0 <overlay.tar.gz> <sha256> <migration-id>" >&2
  exit 64
}
[[ ${EUID} -eq 0 ]] || { echo "Run as root." >&2; exit 77; }

archive="$1"
expected_archive_sha="$2"
migration_id="$3"
web_service="greenxland.service"
game_service="ywonder-game-server.service"
canonical_web_root="/var/www/ywonder"
web_env="${canonical_web_root}/.env"
node_bin="/usr/local/bin/node"
npm_bin="/usr/local/bin/npm"
run_root=""
live_web_root=""
web_user=""
web_home=""
stage=""
overlay_root=""

log() {
  printf '[web-point-authority-validate] %s\n' "$*"
}

fail() {
  echo "[web-point-authority-validate] $*" >&2
  exit 1
}

cleanup() {
  local exit_code=$?
  trap - EXIT INT TERM
  if [[ -n "${run_root}" && "${run_root}" == /tmp/ywonder-web-point-authority-validate.* ]]; then
    rm -rf -- "${run_root}"
  elif [[ -n "${run_root}" ]]; then
    echo "Unsafe validation cleanup path refused: ${run_root}" >&2
    exit_code=74
  fi
  exit "${exit_code}"
}
trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM

for command_name in \
  awk cat chown chmod cp curl df env flock getent grep id install ionice mkdir mktemp nice \
  nproc openssl python3 readlink rm runuser sed sha256sum sleep stat systemctl tar timeout tr; do
  command -v "${command_name}" >/dev/null || fail "Missing command: ${command_name}"
done
[[ -x "${node_bin}" && -x "${npm_bin}" ]] || fail "Node.js/npm runtime is missing."
[[ -f "${archive}" ]] || fail "Overlay archive is missing."
[[ "${expected_archive_sha}" =~ ^[0-9a-f]{64}$ ]] || fail "Invalid overlay checksum."
[[ "${migration_id}" =~ ^[0-9]{14}_game_point_authority$ ]] || fail "Invalid migration id."
[[ "$(sha256sum "${archive}" | awk '{print $1}')" == "${expected_archive_sha}" ]] \
  || fail "Overlay checksum mismatch."

exec 9>/run/lock/ywonder-web-point-authority-validate.lock
flock -n 9 || fail "Another Point authority validation is active."

run_root="$(mktemp -d /tmp/ywonder-web-point-authority-validate.XXXXXX)"
chmod 0711 "${run_root}"
stage="${run_root}/web"
overlay_root="${run_root}/overlay"
mkdir -p "${stage}" "${overlay_root}"

tar -tzf "${archive}" | sed 's#^\./##' >"${run_root}/overlay.list"
tar -tvzf "${archive}" >"${run_root}/overlay.verbose-list"
[[ "$(awk 'NF && $0 !~ /\/$/ {count++} END {print count+0}' "${run_root}/overlay.list")" -eq 17 ]] \
  || fail "Overlay must contain exactly seventeen files."
for required in \
  apply-web-point-authority-patch.js convert-point-to-usdt-action.tsfrag \
  convert-usdt-to-point-action.tsfrag cron-route.ts \
  game-balance-route.ts game-credit-route.ts game-point-authority.ts \
  game-point-conversion-intent.ts game-point-debit-intent.ts game-point-debit.ts \
  game-point-sync.ts point-rate.ts \
  migration.sql notification-poll-route.ts web-point-authority-db-e2e.js \
  web-point-authority-runtime-e2e.ts web-point-debit-runtime-e2e.ts; do
  grep -qx "${required}" "${run_root}/overlay.list" || fail "Overlay is missing ${required}."
done
awk '/^\// || /(^|\/)\.\.($|\/)/ {bad=1} END {exit bad ? 1 : 0}' "${run_root}/overlay.list" \
  || fail "Overlay contains an unsafe path."
awk 'substr($1,1,1) == "l" || substr($1,1,1) == "h" {bad=1} END {exit bad ? 1 : 0}' \
  "${run_root}/overlay.verbose-list" || fail "Overlay contains a link."
tar -xzf "${archive}" -C "${overlay_root}"

resource_gate() {
  local memory_mb disk_mb load_one cpus
  memory_mb="$(awk '/^MemAvailable:/ {print int($2 / 1024)}' /proc/meminfo)"
  disk_mb="$(df -Pm /tmp | awk 'NR == 2 {print $4}')"
  load_one="$(awk '{print $1}' /proc/loadavg)"
  cpus="$(nproc)"
  [[ "${memory_mb}" -ge 1536 ]] || fail "Only ${memory_mb} MB RAM is available."
  [[ "${disk_mb}" -ge 6144 ]] || fail "Only ${disk_mb} MB disk is available in /tmp."
  python3 - "${load_one}" "${cpus}" <<'PY'
import sys
load, cpus = float(sys.argv[1]), int(sys.argv[2])
if cpus < 1 or load / cpus > 1.5:
    raise SystemExit(f"Load per CPU is {load / max(cpus, 1):.2f}; limit is 1.50")
PY
  log "resource gate passed: memory=${memory_mb}MB disk=${disk_mb}MB load1=${load_one} cpus=${cpus}"
}

run_as_web() {
  local duration="$1"
  shift
  timeout --signal=TERM --kill-after=30s "${duration}" \
    runuser -u "${web_user}" -- \
    env HOME="${web_home}" npm_config_cache="${run_root}/npm-cache" npm_config_jobs=2 \
    nice -n 10 ionice -c 2 -n 7 "$@"
}

database_path() {
  cd "${live_web_root}"
  "${node_bin}" -e '
    const path = require("path");
    process.loadEnvFile(".env");
    const raw = String(process.env.DATABASE_URL || "");
    if (!raw.startsWith("file:")) process.exit(65);
    let ref = raw.slice(5).split("?", 1)[0];
    if (!path.isAbsolute(ref)) ref = path.resolve(process.cwd(), "prisma", ref.replace(/^\.\//, ""));
    process.stdout.write(ref);
  '
}

schema_fingerprint() {
  python3 - "$1" <<'PY'
import hashlib, sqlite3, sys
db = sqlite3.connect("file:" + sys.argv[1] + "?mode=ro", uri=True, timeout=10)
rows = db.execute("select type,name,tbl_name,coalesce(sql,'') from sqlite_master order by type,name").fetchall()
payload = "\n".join("\t".join(map(str, row)) for row in rows).encode()
print(hashlib.sha256(payload).hexdigest())
db.close()
PY
}

[[ "$(systemctl is-active "${web_service}")" == active ]] || fail "Web service is not active."
[[ "$(systemctl is-active "${game_service}")" == active ]] || fail "Game service is not active."
live_web_root="$(systemctl show --property=WorkingDirectory --value "${web_service}")"
[[ "${live_web_root}" =~ ^/var/www/(ywonder|ywonder-releases/[A-Za-z0-9._-]+)$ ]] \
  || fail "Active web root is outside the approved paths."
live_web_root="$(readlink -f "${live_web_root}")"
[[ -d "${live_web_root}" ]] || fail "Active web root is missing."
[[ -e "${live_web_root}/.env" && "$(readlink -f "${live_web_root}/.env")" == "$(readlink -f "${web_env}")" ]] \
  || fail "Active release does not use the canonical environment."
web_user="$(systemctl show --property=User --value "${web_service}")"
[[ "${web_user}" =~ ^[a-z_][a-z0-9_-]*[$]?$ ]] || fail "Unsafe web service user."
web_home="$(getent passwd "${web_user}" | awk -F: '{print $6}')"
[[ "${web_home}" == /* ]] || fail "Web service user has no safe home."

live_db="$(database_path)"
[[ -f "${live_db}" ]] || fail "Production web SQLite database is missing."
baseline_web_pid="$(systemctl show --property=MainPID --value "${web_service}")"
baseline_game_pid="$(systemctl show --property=MainPID --value "${game_service}")"
baseline_web_env_sha="$(sha256sum "${web_env}" | awk '{print $1}')"
baseline_build_id="$(cat "${live_web_root}/.next/BUILD_ID")"
baseline_schema_sha="$(schema_fingerprint "${live_db}")"
baseline_source_sha="$(tar -C "${live_web_root}" -cf - \
  prisma/schema.prisma lib/actions/convert.ts lib/actions/admin.ts lib/queries.ts lib/game-point-sync.ts \
  app/api/cron/game-point-sync/route.ts components/PointConvertActions.tsx \
  components/Wallet1Section.tsx 'app/[locale]/(app)/wallet/page.tsx' \
  app/api/game/balance/route.ts app/api/game/credit/route.ts \
  app/api/notifications/poll/route.ts | sha256sum | awk '{print $1}')"
[[ "$(curl -sS -o /dev/null -w '%{http_code}' http://127.0.0.1:3033/api/health)" == 200 ]] \
  || fail "Web health check failed."
[[ "$(curl -sS -o /dev/null -w '%{http_code}' http://127.0.0.1:3000/health)" == 200 ]] \
  || fail "Game health check failed."

resource_gate
log "copying active source and SQLite into an isolated stage"
tar -C "${live_web_root}" \
  --exclude='./.env' --exclude='./.next' --exclude='./node_modules' \
  --exclude='*.db' --exclude='*.db-*' --exclude='*.sqlite' --exclude='*.sqlite-*' \
  -cf "${run_root}/source.tar" .
tar -xf "${run_root}/source.tar" -C "${stage}"
rm -f -- "${run_root}/source.tar"
install -m 0600 "${web_env}" "${stage}/.env"

stage_db="${stage}/prisma/authority-validation.db"
python3 - "${live_db}" "${stage_db}" <<'PY'
import sqlite3, sys
source = sqlite3.connect("file:" + sys.argv[1] + "?mode=ro", uri=True, timeout=10)
target = sqlite3.connect(sys.argv[2])
source.backup(target, pages=256, sleep=0.01)
target.close()
source.close()
PY

"${node_bin}" "${overlay_root}/apply-web-point-authority-patch.js" "${stage}" "${migration_id}"
install -m 0600 "${overlay_root}/web-point-authority-db-e2e.js" \
  "${stage}/deploy/web-point-authority-db-e2e.js"
install -m 0600 "${overlay_root}/web-point-authority-runtime-e2e.ts" \
  "${stage}/deploy/web-point-authority-runtime-e2e.ts"
install -m 0600 "${overlay_root}/web-point-debit-runtime-e2e.ts" \
  "${stage}/deploy/web-point-debit-runtime-e2e.ts"
mkdir -p "${run_root}/npm-cache"
chown -R "${web_user}:$(id -gn "${web_user}")" "${stage}" "${run_root}/npm-cache"

log "installing dependencies and validating Prisma in the isolated stage"
(
  cd "${stage}"
  run_as_web 15m "${npm_bin}" ci --ignore-scripts --no-audit --no-fund
  run_as_web 5m ./node_modules/.bin/prisma generate
  run_as_web 2m ./node_modules/.bin/prisma validate
  run_as_web 2m env DATABASE_URL="file:${stage_db}" \
    ./node_modules/.bin/prisma db execute \
      --file "prisma/migrations/${migration_id}/migration.sql" \
      --schema prisma/schema.prisma
)

python3 - "${stage_db}" <<'PY'
import sqlite3, sys
db = sqlite3.connect("file:" + sys.argv[1] + "?mode=ro", uri=True)
tables = {row[0] for row in db.execute("select name from sqlite_master where type='table'")}
triggers = {row[0] for row in db.execute("select name from sqlite_master where type='trigger'")}
indexes = {row[0] for row in db.execute("select name from sqlite_master where type='index'")}
required_tables = {"GamePointLinkedAccount", "GamePointConversion", "GamePointDebit", "PointExchangeRateVersion"}
required_triggers = {
    "GamePointLinkedAccount_require_zero_wallet",
    "Wallet_freeze_linked_point_update",
    "Wallet_require_zero_point_for_linked_insert",
    "GamePointConversion_block_active_debit_insert",
    "GamePointConversion_block_active_debit_update",
    "GamePointDebit_block_active_conversion_insert",
    "GamePointDebit_block_active_conversion_update",
}
required_indexes = {
    "GamePointLinkedAccount_gamePlayerId_key",
    "GamePointConversion_requestId_key",
    "GamePointConversion_one_unresolved_per_user",
    "GamePointDebit_requestId_key",
    "GamePointDebit_reservationId_key",
    "GamePointDebit_one_unresolved_per_user",
    "PointExchangeRateVersion_one_active_pair",
}
if not required_tables.issubset(tables) or not required_triggers.issubset(triggers) or not required_indexes.issubset(indexes):
    raise SystemExit("Point authority migration verification failed")
rate = db.execute('select "rateMicros" from "PointExchangeRateVersion" where "pair"=? and "isActive"=1', ("USDT_POINT",)).fetchone()
if not rate or not str(rate[0]).isdigit() or int(rate[0]) <= 0:
    raise SystemExit("Point authority migration did not seed an active exact rate")
db.close()
PY

log "running isolated Point authority database E2E"
(
  cd "${stage}"
  run_as_web 3m env DATABASE_URL="file:${stage_db}" \
    "${node_bin}" deploy/web-point-authority-db-e2e.js
)

resource_gate
log "building the isolated Next.js candidate"
build_topup_secret="$(openssl rand -hex 48)"
build_cron_secret="$(openssl rand -hex 48)"
(
  cd "${stage}"
  run_as_web 20m env \
    DATABASE_URL="file:${stage_db}" \
    WEB_TOPUP_ENABLED=true \
    WEB_TOPUP_MODE=canary \
    WEB_TOPUP_ALLOWED_WEB_USER_IDS=authority-build-placeholder \
    WEB_TOPUP_SECRET="${build_topup_secret}" \
    WEB_POINT_WALLET_DEBIT_ENABLED=true \
    WEB_POINT_DEBIT_FEE_BPS=1000 \
    WEB_POINT_DEBIT_MAX_POINTS=1000000 \
    CRON_SECRET="${build_cron_secret}" \
    GAME_POINT_SYNC_URL=http://127.0.0.1:9/internal/web/point-credit \
    NODE_OPTIONS=--max-old-space-size=1536 \
    "${npm_bin}" run build
)
unset build_topup_secret build_cron_secret
[[ -f "${stage}/.next/BUILD_ID" ]] || fail "Candidate build did not produce BUILD_ID."
grep -R -q 'ywonder-game-point-debit-v1' "${stage}/.next/server" \
  || fail "Built candidate is missing the Point-to-USDT debit journal."
grep -R -q 'ywonder-point-reservation-v1' "${stage}/.next/server" \
  || fail "Built candidate is missing signed Point reservation commands."
grep -R -q 'GAME_POINT_LEDGER_IS_AUTHORITATIVE' "${stage}/.next/server" \
  || fail "Built candidate is missing the legacy credit guard."
grep -R -q 'ywonder-point-balance-v1' "${stage}/.next/server" \
  || fail "Built candidate is missing the signed balance reader."
grep -R -q 'ywonder:point-conversion-intent:v1:' "${stage}/.next" \
  || fail "Built candidate is missing the durable browser conversion intent."
grep -R -q 'ywonder:point-debit-intent:v1:' "${stage}/.next" \
  || fail "Built candidate is missing the durable browser debit intent."
grep -R -q 'ACTIVE_POINT_RATE_NOT_CONFIGURED' "${stage}/.next/server" \
  || fail "Built candidate is missing the Admin-controlled Point rate gate."

log "running isolated Point authority runtime fault E2E"
runtime_topup_secret="$(openssl rand -hex 48)"
(
  cd "${stage}"
  run_as_web 5m env \
    DATABASE_URL="file:${stage_db}" \
    WEB_TOPUP_ENABLED=true \
    WEB_TOPUP_MODE=open \
    WEB_TOPUP_SECRET="${runtime_topup_secret}" \
    WEB_POINT_WALLET_DEBIT_ENABLED=true \
    WEB_POINT_DEBIT_FEE_BPS=1000 \
    WEB_POINT_DEBIT_MAX_POINTS=1000000 \
    GAME_POINT_SYNC_URL=http://127.0.0.1:9/internal/web/point-credit \
    ./node_modules/.bin/tsx deploy/web-point-authority-runtime-e2e.ts
)

log "running isolated Point debit saga fault E2E"
(
  cd "${stage}"
  run_as_web 5m env \
    DATABASE_URL="file:${stage_db}" \
    WEB_TOPUP_ENABLED=true \
    WEB_TOPUP_MODE=open \
    WEB_TOPUP_SECRET="${runtime_topup_secret}" \
    WEB_POINT_WALLET_DEBIT_ENABLED=true \
    WEB_POINT_DEBIT_FEE_BPS=1000 \
    WEB_POINT_DEBIT_MAX_POINTS=1000000 \
    GAME_POINT_SYNC_URL=http://127.0.0.1:9/internal/web/point-credit \
    ./node_modules/.bin/tsx deploy/web-point-debit-runtime-e2e.ts
)
unset runtime_topup_secret

[[ "$(systemctl show --property=MainPID --value "${web_service}")" == "${baseline_web_pid}" ]] \
  || fail "Production web service restarted during validation."
[[ "$(systemctl show --property=MainPID --value "${game_service}")" == "${baseline_game_pid}" ]] \
  || fail "Production game service restarted during validation."
[[ "$(readlink -f "$(systemctl show --property=WorkingDirectory --value "${web_service}")")" == "${live_web_root}" ]] \
  || fail "Production web root changed during validation."
[[ "$(sha256sum "${web_env}" | awk '{print $1}')" == "${baseline_web_env_sha}" ]] \
  || fail "Production web environment changed during validation."
[[ "$(cat "${live_web_root}/.next/BUILD_ID")" == "${baseline_build_id}" ]] \
  || fail "Production web build changed during validation."
[[ "$(schema_fingerprint "${live_db}")" == "${baseline_schema_sha}" ]] \
  || fail "Production SQLite schema changed during validation."
current_source_sha="$(tar -C "${live_web_root}" -cf - \
  prisma/schema.prisma lib/actions/convert.ts lib/actions/admin.ts lib/queries.ts lib/game-point-sync.ts \
  app/api/cron/game-point-sync/route.ts components/PointConvertActions.tsx \
  components/Wallet1Section.tsx 'app/[locale]/(app)/wallet/page.tsx' \
  app/api/game/balance/route.ts app/api/game/credit/route.ts \
  app/api/notifications/poll/route.ts | sha256sum | awk '{print $1}')"
[[ "${current_source_sha}" == "${baseline_source_sha}" ]] \
  || fail "Production web source changed during validation."
[[ "$(curl -sS -o /dev/null -w '%{http_code}' http://127.0.0.1:3033/api/health)" == 200 ]] \
  || fail "Production web health changed after validation."
[[ "$(curl -sS -o /dev/null -w '%{http_code}' http://127.0.0.1:3000/health)" == 200 ]] \
  || fail "Production game health changed after validation."

echo "WEB_POINT_AUTHORITY_VALIDATION=success"
echo "CANDIDATE_BUILD_ID=$(cat "${stage}/.next/BUILD_ID")"
echo "LIVE_WEB_CHANGED=no"
echo "PRODUCTION_DATABASE_MUTATED=no"
echo "PRODUCTION_SERVICES_RESTARTED=no"
echo "REAL_PAYMENT_USED=no"
