#!/usr/bin/env bash
set -Eeuo pipefail
umask 077

[[ $# -eq 4 ]] || {
  echo "Usage: $0 <overlay.tar.gz> <sha256> <expected-live-sync-sha256> <expected-live-cron-sha256>" >&2
  exit 64
}
[[ ${EUID} -eq 0 ]] || { echo "Run as root." >&2; exit 77; }

archive="$1"
expected_archive_sha="$2"
expected_live_sync_sha="$3"
expected_live_cron_sha="$4"
web_root="/var/www/ywonder"
web_service="greenxland.service"
game_service="ywonder-game-server.service"
web_user="greenxland"
web_home=""
node_bin="/usr/local/bin/node"
npm_bin="/usr/local/bin/npm"
game_env="/etc/ywonder-game/game-server.env"
stamp="$(date -u +%Y%m%dT%H%M%SZ)"
backup_dir="/var/backups/ywonder-web/point-canary-hardening-${stamp}"
stage=""
overlay=""
candidate_next="${web_root}/.next.point-canary-${stamp}"
web_stopped=0
live_modified=0
next_moved=0
completed=0

log() {
  printf '[web-point-canary-deploy] %s\n' "$*"
}

fail() {
  echo "[web-point-canary-deploy] $*" >&2
  exit 1
}

env_value() {
  local file="$1" key="$2"
  awk -F= -v wanted="${key}" '$1 == wanted {value=substr($0,index($0,"=")+1)} END {gsub(/^[[:space:]]+|[[:space:]]+$/,"",value); gsub(/^\047|\047$/,"",value); gsub(/^\042|\042$/,"",value); print value}' "${file}"
}

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
    env HOME="${web_home}" npm_config_cache="${stage}/npm-cache" \
    nice -n 10 ionice -c 2 -n 7 "$@"
}

database_path() {
  cd "${web_root}"
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

restore_live() {
  local rollback_failed=0
  log "rolling back web Point canary source and build"
  systemctl stop "${web_service}" || rollback_failed=1
  if [[ -f "${backup_dir}/game-point-sync.ts" ]]; then
    install -o "${web_user}" -g "${web_user}" -m 0644 \
      "${backup_dir}/game-point-sync.ts" "${web_root}/lib/game-point-sync.ts" \
      || rollback_failed=1
  fi
  if [[ -f "${backup_dir}/cron-route.ts" ]]; then
    install -o "${web_user}" -g "${web_user}" -m 0644 \
      "${backup_dir}/cron-route.ts" "${web_root}/app/api/cron/game-point-sync/route.ts" \
      || rollback_failed=1
  fi
  if [[ ${next_moved} -eq 1 && -d "${backup_dir}/next" ]]; then
    rm -rf -- "${web_root}/.next"
    mv "${backup_dir}/next" "${web_root}/.next" || rollback_failed=1
    chown -R "${web_user}:${web_user}" "${web_root}/.next" || rollback_failed=1
  fi
  systemctl start "${web_service}" || rollback_failed=1
  if [[ ${rollback_failed} -eq 0 ]]; then
    echo "WEB_POINT_CANARY_ROLLBACK=complete" >&2
  else
    echo "WEB_POINT_CANARY_ROLLBACK=incomplete" >&2
  fi
}

cleanup() {
  local exit_code=$?
  trap - EXIT INT TERM
  if [[ ${completed} -eq 0 ]]; then
    if [[ ${live_modified} -eq 1 ]]; then
      restore_live
    elif [[ ${web_stopped} -eq 1 ]]; then
      systemctl start "${web_service}" || true
    fi
  fi
  if [[ -n "${overlay}" && "${overlay}" == /tmp/ywonder-web-point-canary-overlay.* ]]; then
    rm -rf -- "${overlay}"
  fi
  if [[ -n "${stage}" && "${stage}" == /tmp/ywonder-web-point-canary-stage.* ]]; then
    rm -rf -- "${stage}"
  fi
  if [[ "${candidate_next}" == "${web_root}"/.next.point-canary-* ]]; then
    rm -rf -- "${candidate_next}"
  fi
  exit "${exit_code}"
}
trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM

for command_name in \
  awk cat chown chmod cp curl date df env flock getent grep install ionice mkdir mktemp mv nice \
  nproc openssl python3 rm runuser seq sha256sum sleep systemctl tar timeout; do
  command -v "${command_name}" >/dev/null || fail "Missing command: ${command_name}"
done
[[ -x "${node_bin}" && -x "${npm_bin}" ]] || fail "Node.js/npm runtime is missing."
web_home="$(getent passwd "${web_user}" | awk -F: '{print $6}')"
[[ "${web_home}" == /* ]] || fail "Web service user has no safe home directory."
[[ -f "${archive}" ]] || fail "Overlay archive is missing."
[[ "${expected_archive_sha}" =~ ^[0-9a-f]{64}$ ]] || fail "Invalid overlay checksum."
[[ "${expected_live_sync_sha}" =~ ^[0-9a-f]{64}$ ]] || fail "Invalid expected sync checksum."
[[ "${expected_live_cron_sha}" =~ ^[0-9a-f]{64}$ ]] || fail "Invalid expected cron checksum."
[[ "$(sha256sum "${archive}" | awk '{print $1}')" == "${expected_archive_sha}" ]] \
  || fail "Overlay checksum mismatch."

exec 9>/run/lock/ywonder-web-point-canary-hardening.lock
flock -n 9 || fail "Another web Point hardening deploy is active."

overlay="$(mktemp -d /tmp/ywonder-web-point-canary-overlay.XXXXXX)"
stage="$(mktemp -d /tmp/ywonder-web-point-canary-stage.XXXXXX)"
chmod 0711 "${stage}"

tar -tzf "${archive}" >"${stage}/overlay.list"
tar -tvzf "${archive}" >"${stage}/overlay.verbose-list"
[[ "$(awk 'NF && $0 !~ /\/$/ {count++} END {print count+0}' "${stage}/overlay.list")" -eq 2 ]] \
  || fail "Overlay must contain exactly two files."
grep -qx 'game-point-sync.ts' "${stage}/overlay.list" || fail "Overlay is missing game-point-sync.ts."
grep -qx 'cron-route.ts' "${stage}/overlay.list" || fail "Overlay is missing cron-route.ts."
awk '/^\// || /(^|\/)\.\.($|\/)/ {bad=1} END {exit bad ? 1 : 0}' "${stage}/overlay.list" \
  || fail "Overlay contains an unsafe path."
awk 'substr($1,1,1) == "l" || substr($1,1,1) == "h" {bad=1} END {exit bad ? 1 : 0}' \
  "${stage}/overlay.verbose-list" || fail "Overlay contains a link."
tar -xzf "${archive}" -C "${overlay}"

new_sync_sha="$(sha256sum "${overlay}/game-point-sync.ts" | awk '{print $1}')"
new_cron_sha="$(sha256sum "${overlay}/cron-route.ts" | awk '{print $1}')"
[[ "$(systemctl is-active "${game_service}")" == active ]] || fail "Game service is not active."
[[ "$(systemctl is-active "${web_service}")" == active ]] || fail "Web service is not active."
[[ "$(env_value "${game_env}" WEB_TOPUP_ENABLED)" == false ]] || fail "Game top-up is not dormant."
[[ "$(env_value "${game_env}" WEB_TOPUP_ALLOW_REMOTE)" == false ]] || fail "Remote game top-up is not disabled."
[[ "$(curl -sS -o /dev/null -w '%{http_code}' http://127.0.0.1:3000/health)" == 200 ]] \
  || fail "Game health check failed."
[[ "$(curl -sS -o /dev/null -w '%{http_code}' http://127.0.0.1:3033/api/health)" == 200 ]] \
  || fail "Web health check failed."
[[ "$(curl -sS -o /dev/null -w '%{http_code}' -X POST -H 'Content-Type: application/json' -d '{}' \
  http://127.0.0.1:3000/internal/web/point-credit)" == 404 ]] || fail "Game top-up endpoint is not dormant."

if [[ "$(sha256sum "${web_root}/lib/game-point-sync.ts" | awk '{print $1}')" == "${new_sync_sha}" \
    && "$(sha256sum "${web_root}/app/api/cron/game-point-sync/route.ts" | awk '{print $1}')" == "${new_cron_sha}" ]] \
    && grep -R -q 'GAME_POINT_SYNC_CANARY_USER_NOT_ALLOWED' "${web_root}/.next/server"; then
  echo "WEB_POINT_CANARY_HARDENING=already-applied"
  completed=1
  exit 0
fi

live_sync_sha="$(sha256sum "${web_root}/lib/game-point-sync.ts" | awk '{print $1}')"
live_cron_sha="$(sha256sum "${web_root}/app/api/cron/game-point-sync/route.ts" | awk '{print $1}')"
[[ "${live_sync_sha}" == "${expected_live_sync_sha}" ]] || fail "Live sync source changed after audit."
[[ "${live_cron_sha}" == "${expected_live_cron_sha}" ]] || fail "Live cron source changed after audit."
game_pid_before="$(systemctl show --property=MainPID --value "${game_service}")"
game_env_sha_before="$(sha256sum "${game_env}" | awk '{print $1}')"
web_env_sha_before="$(sha256sum "${web_root}/.env" | awk '{print $1}')"

resource_gate
log "copying and building an isolated web candidate"
tar -C "${web_root}" \
  --exclude='./.env' \
  --exclude='./.next' \
  --exclude='./node_modules' \
  --exclude='*.db' \
  --exclude='*.db-*' \
  --exclude='*.sqlite' \
  --exclude='*.sqlite-*' \
  -cf "${stage}/source.tar" .
tar -xf "${stage}/source.tar" -C "${stage}"
rm -f "${stage}/source.tar"
install -m 0600 "${web_root}/.env" "${stage}/.env"
install -m 0644 "${overlay}/game-point-sync.ts" "${stage}/lib/game-point-sync.ts"
install -m 0644 "${overlay}/cron-route.ts" "${stage}/app/api/cron/game-point-sync/route.ts"
grep -q 'WEB_TOPUP_ALLOWED_WEB_USER_IDS' "${stage}/lib/game-point-sync.ts" \
  || fail "Candidate is missing the canary allowlist."
grep -q 'GAME_POINT_SYNC_CANARY_USER_NOT_ALLOWED' "${stage}/lib/game-point-sync.ts" \
  || fail "Candidate is missing canary rejection behavior."
mkdir -p "${stage}/npm-cache"
chown -R "${web_user}:${web_user}" "${stage}"

(
  cd "${stage}"
  run_as_web 15m "${npm_bin}" ci --ignore-scripts --no-audit --no-fund
  run_as_web 5m ./node_modules/.bin/prisma generate
)

live_db="$(database_path)"
[[ -f "${live_db}" ]] || fail "Production web SQLite database was not found."
build_db="${stage}/prisma/point-canary-build.db"
python3 - "${live_db}" "${build_db}" <<'PY'
import sqlite3, sys
source = sqlite3.connect("file:" + sys.argv[1] + "?mode=ro", uri=True, timeout=10)
target = sqlite3.connect(sys.argv[2])
source.backup(target, pages=256, sleep=0.01)
target.close()
source.close()
PY
chown "${web_user}:${web_user}" "${build_db}"
build_topup_secret="$(openssl rand -hex 48)"
build_cron_secret="$(openssl rand -hex 48)"
(
  cd "${stage}"
  run_as_web 20m env \
    DATABASE_URL="file:${build_db}" \
    WEB_TOPUP_SECRET="${build_topup_secret}" \
    WEB_TOPUP_MODE=canary \
    WEB_TOPUP_ALLOWED_WEB_USER_IDS=point-canary-build-placeholder \
    CRON_SECRET="${build_cron_secret}" \
    GAME_POINT_SYNC_URL=http://127.0.0.1:9/internal/web/point-credit \
    NODE_OPTIONS=--max-old-space-size=1536 \
    "${npm_bin}" run build
)
unset build_topup_secret build_cron_secret
[[ -f "${stage}/.next/BUILD_ID" ]] || fail "Candidate build did not produce BUILD_ID."
grep -R -q 'GAME_POINT_SYNC_CANARY_USER_NOT_ALLOWED' "${stage}/.next/server" \
  || fail "Built candidate does not contain canary rejection behavior."

resource_gate
[[ "$(sha256sum "${web_root}/lib/game-point-sync.ts" | awk '{print $1}')" == "${expected_live_sync_sha}" ]] \
  || fail "Live sync source changed during candidate build."
[[ "$(sha256sum "${web_root}/app/api/cron/game-point-sync/route.ts" | awk '{print $1}')" == "${expected_live_cron_sha}" ]] \
  || fail "Live cron source changed during candidate build."
[[ "$(sha256sum "${game_env}" | awk '{print $1}')" == "${game_env_sha_before}" ]] \
  || fail "Game environment changed during candidate build."
[[ "$(sha256sum "${web_root}/.env" | awk '{print $1}')" == "${web_env_sha_before}" ]] \
  || fail "Web environment changed during candidate build."

if [[ "${WEB_POINT_CANARY_VALIDATE_ONLY:-false}" == true ]]; then
  completed=1
  echo "WEB_POINT_CANARY_VALIDATION=success"
  echo "LIVE_WEB_CHANGED=no"
  echo "REAL_PAYMENT_USED=no"
  exit 0
fi

install -d -o root -g root -m 0700 "${backup_dir}"
install -o root -g root -m 0600 "${web_root}/lib/game-point-sync.ts" "${backup_dir}/game-point-sync.ts"
install -o root -g root -m 0600 "${web_root}/app/api/cron/game-point-sync/route.ts" "${backup_dir}/cron-route.ts"
install -o root -g root -m 0600 "${web_root}/.env" "${backup_dir}/web.env"
printf '%s\n' "$(cat "${web_root}/.next/BUILD_ID")" >"${backup_dir}/previous-build-id.txt"
[[ ! -e "${candidate_next}" ]] || fail "Candidate build path already exists."
cp -a "${stage}/.next" "${candidate_next}"
chown -R "${web_user}:${web_user}" "${candidate_next}"

log "switching web source and build with rollback armed"
if ! systemctl stop "${web_service}"; then
  systemctl start "${web_service}" || true
  fail "Web service could not be stopped cleanly."
fi
web_stopped=1
mv "${web_root}/.next" "${backup_dir}/next"
next_moved=1
live_modified=1
install -o "${web_user}" -g "${web_user}" -m 0644 \
  "${overlay}/game-point-sync.ts" "${web_root}/lib/game-point-sync.ts"
install -o "${web_user}" -g "${web_user}" -m 0644 \
  "${overlay}/cron-route.ts" "${web_root}/app/api/cron/game-point-sync/route.ts"
mv "${candidate_next}" "${web_root}/.next"
chown -R "${web_user}:${web_user}" "${web_root}/.next"
systemctl start "${web_service}"
web_stopped=0

for _ in $(seq 1 45); do
  if curl --fail --silent --max-time 2 http://127.0.0.1:3033/api/health >/dev/null; then
    break
  fi
  sleep 1
done
[[ "$(systemctl is-active "${web_service}")" == active ]] || fail "Web service did not become active."
[[ "$(curl -sS -o /dev/null -w '%{http_code}' http://127.0.0.1:3033/api/health)" == 200 ]] \
  || fail "Web health check failed after switch."
[[ "$(systemctl show --property=MainPID --value "${game_service}")" == "${game_pid_before}" ]] \
  || fail "Game service restarted during web deploy."
[[ "$(sha256sum "${game_env}" | awk '{print $1}')" == "${game_env_sha_before}" ]] \
  || fail "Game environment changed during web deploy."
[[ "$(sha256sum "${web_root}/.env" | awk '{print $1}')" == "${web_env_sha_before}" ]] \
  || fail "Web environment changed during web deploy."
[[ "$(sha256sum "${web_root}/lib/game-point-sync.ts" | awk '{print $1}')" == "${new_sync_sha}" ]] \
  || fail "Live sync source hash is incorrect."
[[ "$(sha256sum "${web_root}/app/api/cron/game-point-sync/route.ts" | awk '{print $1}')" == "${new_cron_sha}" ]] \
  || fail "Live cron source hash is incorrect."
grep -R -q 'GAME_POINT_SYNC_CANARY_USER_NOT_ALLOWED' "${web_root}/.next/server" \
  || fail "Live build is missing canary rejection behavior."
[[ "$(curl -sS -o /dev/null -w '%{http_code}' -X POST http://127.0.0.1:3033/api/cron/game-point-sync)" == 401 ]] \
  || fail "Unauthenticated Point cron was accepted."
[[ "$(curl -sS -o /dev/null -w '%{http_code}' -X POST -H 'Content-Type: application/json' -d '{}' \
  http://127.0.0.1:3000/internal/web/point-credit)" == 404 ]] || fail "Game top-up endpoint is no longer dormant."

completed=1
echo "WEB_POINT_CANARY_HARDENING=success"
echo "WEB_HEALTH=200"
echo "GAME_TOPUP_ENDPOINT=404"
echo "GAME_SERVICE_RESTARTED=no"
echo "WEB_ENV_CHANGED=no"
echo "REAL_PAYMENT_USED=no"
echo "BACKUP_DIR=${backup_dir}"
echo "WEB_BUILD_ID=$(cat "${web_root}/.next/BUILD_ID")"
