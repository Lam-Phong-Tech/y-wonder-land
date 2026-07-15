#!/usr/bin/env bash
set -Eeuo pipefail
umask 077

[[ $# -eq 4 ]] || {
  echo "Usage: $0 <game-archive.tar.gz> <game-sha256> <web-overlay.tar.gz> <overlay-sha256>" >&2
  exit 64
}
[[ ${EUID} -eq 0 ]] || { echo "Run as root." >&2; exit 77; }

game_archive="$1"
game_expected_sha="$2"
overlay_archive="$3"
overlay_expected_sha="$4"
live_web_root="/var/www/ywonder"
game_env="/etc/ywonder-game/game-server.env"
game_service="ywonder-game-server.service"
web_service="greenxland.service"
node_bin="/usr/local/bin/node"
min_memory_mb="${E2E_MIN_MEMORY_MB:-1536}"
min_disk_mb="${E2E_MIN_DISK_MB:-6144}"
max_load_per_cpu="${E2E_MAX_LOAD_PER_CPU:-1.50}"
run_id="$(date -u +%Y%m%d%H%M%S)_$(openssl rand -hex 4)"
temporary_database="yw_point_e2e_${run_id}"
run_root=""

log() {
  printf '[web-point-e2e-runner] %s\n' "$*"
}

fail() {
  echo "[web-point-e2e-runner] $*" >&2
  exit 1
}

safe_cleanup() {
  local exit_code=$?
  local database_marker marked_database
  trap - EXIT INT TERM

  terminate_e2e_process web || exit_code=74
  terminate_e2e_process game || exit_code=74

  database_marker="${run_root}/runtime/database.name"
  if [[ -n "${run_root}" && -f "${database_marker}" ]]; then
    marked_database="$(tr -d '[:space:]' <"${database_marker}")"
    if [[ "${marked_database}" == "${temporary_database}"
        && "${marked_database}" =~ ^yw_point_e2e_[a-z0-9_]{8,40}$ ]]; then
      if ! runuser -u postgres -- dropdb --if-exists --force "${marked_database}" >/dev/null 2>&1; then
        echo "TEMPORARY_DATABASE_CLEANUP_FAILED=${marked_database}" >&2
        exit_code=74
      fi
    else
      echo "Unsafe E2E database cleanup marker refused: ${database_marker}" >&2
      exit_code=74
    fi
  fi

  if [[ -n "${run_root}" && "${run_root}" == /tmp/ywonder-web-point-e2e.* ]]; then
    rm -rf -- "${run_root}"
  elif [[ -n "${run_root}" ]]; then
    echo "Unsafe E2E cleanup path refused: ${run_root}" >&2
    exit_code=74
  fi
  exit "${exit_code}"
}
trap safe_cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM

terminate_e2e_process() {
  local label="$1"
  local pid_file pid pgid
  [[ -n "${run_root}" ]] || return 0
  pid_file="${run_root}/runtime/${label}.pid"
  [[ -f "${pid_file}" ]] || return 0
  pid="$(tr -d '[:space:]' <"${pid_file}")"
  [[ "${pid}" =~ ^[1-9][0-9]*$ && -d "/proc/${pid}" ]] || return 0
  tr '\0' '\n' <"/proc/${pid}/environ" | grep -Fxq "E2E_RUN_ID=${run_id}" || {
    echo "Refusing to stop unmarked pid ${pid} from ${pid_file}." >&2
    return 1
  }
  pgid="$(ps -o pgid= -p "${pid}" | tr -d '[:space:]')"
  [[ "${pgid}" == "${pid}" ]] || {
    echo "Refusing to stop pid ${pid}: it is not its E2E process-group leader." >&2
    return 1
  }
  kill -TERM -- "-${pid}" 2>/dev/null || true
  for _ in 1 2 3 4 5; do
    [[ ! -d "/proc/${pid}" ]] && return 0
    sleep 1
  done
  kill -KILL -- "-${pid}" 2>/dev/null || true
}

for command_name in \
  awk chown chmod createdb curl date df dropdb env flock getent grep id install ionice \
  mkdir mktemp nice nproc npm openssl ps psql python3 rm runuser sha256sum sleep systemctl tar timeout tr; do
  command -v "${command_name}" >/dev/null || fail "Missing command: ${command_name}"
done
[[ -x "${node_bin}" ]] || fail "Node.js runtime not found at ${node_bin}."
[[ -f "${game_archive}" && -f "${overlay_archive}" ]] || fail "Input archive is missing."
[[ "${game_expected_sha}" =~ ^[0-9a-f]{64}$ ]] || fail "Invalid game archive checksum."
[[ "${overlay_expected_sha}" =~ ^[0-9a-f]{64}$ ]] || fail "Invalid overlay archive checksum."
[[ "$(sha256sum "${game_archive}" | awk '{print $1}')" == "${game_expected_sha}" ]] \
  || fail "Game archive checksum mismatch."
[[ "$(sha256sum "${overlay_archive}" | awk '{print $1}')" == "${overlay_expected_sha}" ]] \
  || fail "Web overlay archive checksum mismatch."
[[ "${run_id}" =~ ^[a-z0-9][a-z0-9_]{7,39}$ ]] || fail "Generated E2E run id is unsafe."
[[ "${temporary_database}" =~ ^yw_point_e2e_[a-z0-9_]{8,40}$ ]] \
  || fail "Generated E2E database name is unsafe."

[[ "${min_memory_mb}" =~ ^[0-9]+$ && "${min_memory_mb}" -ge 1024 ]] \
  || fail "E2E_MIN_MEMORY_MB must be at least 1024."
[[ "${min_disk_mb}" =~ ^[0-9]+$ && "${min_disk_mb}" -ge 4096 ]] \
  || fail "E2E_MIN_DISK_MB must be at least 4096."
[[ "${max_load_per_cpu}" =~ ^[0-9]+([.][0-9]+)?$ ]] \
  || fail "E2E_MAX_LOAD_PER_CPU must be numeric."
python3 - "${max_load_per_cpu}" <<'PY'
import sys
value = float(sys.argv[1])
if value <= 0 or value > 2:
    raise SystemExit("E2E_MAX_LOAD_PER_CPU must be greater than 0 and no more than 2")
PY

exec 9>/run/lock/ywonder-web-point-e2e.lock
flock -n 9 || fail "Another web Point E2E run is active."

run_root="$(mktemp -d "/tmp/ywonder-web-point-e2e.${run_id}.XXXXXX")"
chmod 0711 "${run_root}"
game_stage="${run_root}/game"
web_stage="${run_root}/web"
overlay_stage="${run_root}/overlay"
runtime_root="${run_root}/runtime"
mkdir -p "${game_stage}" "${web_stage}" "${overlay_stage}" "${runtime_root}"

validate_archive() {
  local archive="$1"
  local label="$2"
  local listing="${run_root}/${label}.list"
  local verbose_listing="${run_root}/${label}.verbose-list"
  tar -tzf "${archive}" >"${listing}"
  tar -tvzf "${archive}" >"${verbose_listing}"
  awk '
    /^\// { bad=1 }
    /(^|\/)\.\.($|\/)/ { bad=1 }
    END { exit bad ? 1 : 0 }
  ' "${listing}" || fail "${label} archive contains an unsafe path."
  awk 'substr($1,1,1) == "l" || substr($1,1,1) == "h" { bad=1 } END { exit bad ? 1 : 0 }' \
    "${verbose_listing}" || fail "${label} archive contains a link."
}

resource_gate() {
  local available_memory_mb available_disk_mb load_one cpu_count
  available_memory_mb="$(awk '/^MemAvailable:/ {print int($2 / 1024)}' /proc/meminfo)"
  available_disk_mb="$(df -Pm /tmp | awk 'NR == 2 {print $4}')"
  load_one="$(awk '{print $1}' /proc/loadavg)"
  cpu_count="$(nproc)"

  [[ "${available_memory_mb}" -ge "${min_memory_mb}" ]] \
    || fail "Only ${available_memory_mb} MB RAM is available; ${min_memory_mb} MB is required."
  [[ "${available_disk_mb}" -ge "${min_disk_mb}" ]] \
    || fail "Only ${available_disk_mb} MB disk is available in /tmp; ${min_disk_mb} MB is required."
  python3 - "${load_one}" "${cpu_count}" "${max_load_per_cpu}" <<'PY'
import sys
load, cpus, maximum = float(sys.argv[1]), int(sys.argv[2]), float(sys.argv[3])
if cpus < 1 or load / cpus > maximum:
    raise SystemExit(f"VPS load per CPU is {load / max(cpus, 1):.2f}; limit is {maximum:.2f}")
PY
  log "resource gate passed: memory=${available_memory_mb}MB disk=${available_disk_mb}MB load1=${load_one} cpus=${cpu_count}"
}

service_user() {
  local service="$1"
  local user
  user="$(systemctl show --property=User --value "${service}")"
  [[ "${user}" =~ ^[a-z_][a-z0-9_-]*[$]?$ ]] || fail "Unsafe service user for ${service}."
  printf '%s' "${user}"
}

user_home() {
  local user="$1"
  local home
  home="$(getent passwd "${user}" | awk -F: '{print $6}')"
  [[ "${home}" == /* ]] || fail "No safe home directory for ${user}."
  printf '%s' "${home}"
}

run_limited_as() {
  local user="$1"
  local duration="$2"
  shift 2
  local home cache
  home="$(user_home "${user}")"
  cache="${run_root}/npm-cache-${user}"
  mkdir -p "${cache}"
  chown "${user}:$(id -gn "${user}")" "${cache}"
  timeout --signal=TERM --kill-after=30s "${duration}" \
    runuser -u "${user}" -- \
    env HOME="${home}" npm_config_cache="${cache}" npm_config_jobs=2 \
    nice -n 10 ionice -c 2 -n 7 "$@"
}

run_limited_root() {
  local duration="$1"
  shift
  timeout --signal=TERM --kill-after=30s "${duration}" \
    nice -n 10 ionice -c 2 -n 7 "$@"
}

env_value() {
  local file="$1"
  local key="$2"
  awk -F= -v wanted="${key}" '$1 == wanted {value=substr($0,index($0,"=")+1)} END {gsub(/^[[:space:]]+|[[:space:]]+$/,"",value); gsub(/^\047|\047$/,"",value); gsub(/^\042|\042$/,"",value); print value}' "${file}"
}

validate_archive "${game_archive}" "game"
validate_archive "${overlay_archive}" "overlay"

if grep -Eiq '(^|/)(\.env([^/]*|$)|node_modules|\.git|data\.json|[^/]*\.log|[^/]*\.(pem|key|p12|pfx)|id_rsa)(/|$)' \
    "${run_root}/game.list"; then
  fail "Game archive contains environment, player data, logs, credentials, dependencies or Git data."
fi
overlay_entry_count="$(awk 'NF && $0 !~ /\/$/ {count++} END {print count+0}' "${run_root}/overlay.list")"
[[ "${overlay_entry_count}" -eq 2 ]] || fail "Web overlay archive must contain exactly two files."
grep -qx 'game-point-sync.ts' "${run_root}/overlay.list" || fail "Overlay is missing game-point-sync.ts."
grep -qx 'cron-route.ts' "${run_root}/overlay.list" || fail "Overlay is missing cron-route.ts."

[[ "$(systemctl is-active "${game_service}")" == "active" ]] || fail "Game service is not active."
[[ "$(systemctl is-active "${web_service}")" == "active" ]] || fail "Web service is not active."
[[ "$(env_value "${game_env}" WEB_TOPUP_ENABLED)" == "false" ]] \
  || fail "Production WEB_TOPUP_ENABLED is not false."
[[ "$(env_value "${game_env}" WEB_TOPUP_ALLOW_REMOTE)" == "false" ]] \
  || fail "Production WEB_TOPUP_ALLOW_REMOTE is not false."
[[ "$(curl -sS -o /dev/null -w '%{http_code}' http://127.0.0.1:3000/health)" == "200" ]] \
  || fail "Production game health check failed."
[[ "$(curl -sS -o /dev/null -w '%{http_code}' http://127.0.0.1:3033/api/health)" == "200" ]] \
  || fail "Production web health check failed."
[[ "$(curl -sS -o /dev/null -w '%{http_code}' -X POST -H 'Content-Type: application/json' -d '{}' \
  http://127.0.0.1:3000/internal/web/point-credit)" == "404" ]] \
  || fail "Production Point top-up endpoint is not dormant."

game_user="$(service_user "${game_service}")"
web_user="$(service_user "${web_service}")"
resource_gate

log "extracting hardened game candidate"
tar -xzf "${game_archive}" -C "${game_stage}"
tar -xzf "${overlay_archive}" -C "${overlay_stage}"
for required in \
  index.js package.json package-lock.json security.js webPointCredit.js \
  deploy/test-web-point-sync-e2e-isolated.js; do
  [[ -f "${game_stage}/${required}" ]] || fail "Game candidate is missing ${required}."
done
[[ -d "${game_stage}/migrations" ]] || fail "Game candidate is missing migrations."
chown -R "${game_user}:$(id -gn "${game_user}")" "${game_stage}"

log "installing and testing isolated game candidate at low priority"
(
  cd "${game_stage}"
  run_limited_as "${game_user}" 10m npm ci --ignore-scripts --no-audit --no-fund
  run_limited_as "${game_user}" 2m "${node_bin}" --check index.js
  run_limited_as "${game_user}" 2m "${node_bin}" --check deploy/test-web-point-sync-e2e-isolated.js
  run_limited_as "${game_user}" 4m npm run test:security
  run_limited_as "${game_user}" 4m npm run test:web-point-credit
)

[[ -f "${live_web_root}/package-lock.json" ]] || fail "Production web package lock is missing."
[[ -f "${live_web_root}/.env" ]] || fail "Production web environment is missing."
[[ "$(grep -c '^model GamePointSyncOutbox' "${live_web_root}/prisma/schema.prisma")" -eq 1 ]] \
  || fail "Production web source does not have exactly one Point outbox model."
[[ "$(grep -c 'gamePointSyncOutbox.create' "${live_web_root}/lib/actions/convert.ts")" -eq 1 ]] \
  || fail "Production web source does not have exactly one conversion outbox hook."

log "copying production web source into an isolated stage"
tar -C "${live_web_root}" \
  --exclude='./.env' \
  --exclude='./.next' \
  --exclude='./node_modules' \
  --exclude='*.db' \
  --exclude='*.db-*' \
  --exclude='*.sqlite' \
  --exclude='*.sqlite-*' \
  -cf "${run_root}/web-source.tar" .
tar -xf "${run_root}/web-source.tar" -C "${web_stage}"
install -m 0600 "${live_web_root}/.env" "${web_stage}/.env"
install -m 0644 "${overlay_stage}/game-point-sync.ts" "${web_stage}/lib/game-point-sync.ts"
install -d -m 0755 "${web_stage}/app/api/cron/game-point-sync"
install -m 0644 "${overlay_stage}/cron-route.ts" \
  "${web_stage}/app/api/cron/game-point-sync/route.ts"
grep -q 'WEB_TOPUP_ALLOWED_WEB_USER_IDS' "${web_stage}/lib/game-point-sync.ts" \
  || fail "Staged web dispatcher is missing canary allowlist support."
grep -q 'GAME_POINT_SYNC_CANARY_USER_NOT_ALLOWED' "${web_stage}/lib/game-point-sync.ts" \
  || fail "Staged web dispatcher is missing canary rejection behavior."
chown -R "${web_user}:$(id -gn "${web_user}")" "${web_stage}"

log "installing isolated web dependencies"
(
  cd "${web_stage}"
  run_limited_as "${web_user}" 15m npm ci --ignore-scripts --no-audit --no-fund
  run_limited_as "${web_user}" 5m ./node_modules/.bin/prisma generate
)

production_web_db="$(cd "${live_web_root}" && "${node_bin}" -e '
  const path = require("path");
  process.loadEnvFile(".env");
  const raw = String(process.env.DATABASE_URL || "");
  if (!raw.startsWith("file:")) process.exit(65);
  let ref = raw.slice(5).split("?", 1)[0];
  if (!path.isAbsolute(ref)) ref = path.resolve(process.cwd(), "prisma", ref.replace(/^\.\//, ""));
  process.stdout.write(ref);
')"
[[ -f "${production_web_db}" ]] || fail "Production web SQLite database was not found."
build_database="${web_stage}/prisma/e2e-build.db"
python3 - "${production_web_db}" "${build_database}" <<'PY'
import sqlite3, sys
source = sqlite3.connect("file:" + sys.argv[1] + "?mode=ro", uri=True, timeout=10)
target = sqlite3.connect(sys.argv[2])
source.backup(target, pages=256, sleep=0.01)
target.close()
source.close()
PY
chown "${web_user}:$(id -gn "${web_user}")" "${build_database}"

resource_gate
log "building isolated web candidate at low priority"
build_topup_secret="$(openssl rand -hex 48)"
build_cron_secret="$(openssl rand -hex 48)"
(
  cd "${web_stage}"
  run_limited_as "${web_user}" 20m \
    env DATABASE_URL="file:${build_database}" \
      WEB_TOPUP_SECRET="${build_topup_secret}" \
      WEB_TOPUP_MODE=canary \
      WEB_TOPUP_ALLOWED_WEB_USER_IDS=e2e-build-placeholder \
      CRON_SECRET="${build_cron_secret}" \
      GAME_POINT_SYNC_URL=http://127.0.0.1:9/internal/web/point-credit \
      NODE_OPTIONS=--max-old-space-size=1536 \
      npm run build
)
[[ -f "${web_stage}/.next/BUILD_ID" ]] || fail "Isolated web build did not produce BUILD_ID."
grep -R -q 'GAME_POINT_SYNC_CANARY_USER_NOT_ALLOWED' "${web_stage}/.next/server" \
  || fail "Built web artifact does not contain canary rejection logic."

resource_gate
log "running no-money production-artifact E2E"
run_limited_root 8m \
  env E2E_RUN_ID="${run_id}" \
    E2E_RUNTIME_ROOT="${runtime_root}" \
    GAME_SERVER_ROOT="${game_stage}" \
    E2E_WEB_ROOT="${web_stage}" \
    "${node_bin}" "${game_stage}/deploy/test-web-point-sync-e2e-isolated.js"

[[ "$(systemctl is-active "${game_service}")" == "active" ]] || fail "Game service changed state after E2E."
[[ "$(systemctl is-active "${web_service}")" == "active" ]] || fail "Web service changed state after E2E."
[[ "$(env_value "${game_env}" WEB_TOPUP_ENABLED)" == "false" ]] \
  || fail "Production WEB_TOPUP_ENABLED changed during E2E."
[[ "$(curl -sS -o /dev/null -w '%{http_code}' -X POST -H 'Content-Type: application/json' -d '{}' \
  http://127.0.0.1:3000/internal/web/point-credit)" == "404" ]] \
  || fail "Production Point top-up endpoint changed during E2E."

echo "WEB_POINT_SYNC_HARDENED_RUNNER=success"
echo "LIVE_WEB_BUILD_REPLACED=no"
echo "PRODUCTION_SERVICES_RESTARTED=no"
echo "REAL_PAYMENT_USED=no"
