#!/usr/bin/env bash
set -Eeuo pipefail

usage() {
  echo "Usage: $0 <load-test-js> <suffix>" >&2
  exit 64
}

[[ $# -eq 2 ]] || usage
[[ ${EUID} -eq 0 ]] || { echo "This script must run as root." >&2; exit 77; }

load_test_js="$1"
suffix="$2"
service_user="ywonder_game"
database_name="ywonder_game"
service_name="ywonder-game-server.service"
backup_service="ywonder-db-backup.service"
current_release="/opt/ywonder-game/current"
node_bin="/usr/local/bin/node"
base_url="http://127.0.0.1:8080"
account_prefix="Load${suffix}"

[[ -f "${load_test_js}" ]] || { echo "Load test file not found: ${load_test_js}" >&2; exit 66; }
[[ "${suffix}" =~ ^[A-Za-z0-9]{3,14}$ ]] || { echo "Suffix must contain 3-14 letters or digits." >&2; exit 65; }
[[ -x "${node_bin}" ]] || { echo "Node binary not found: ${node_bin}" >&2; exit 69; }
[[ -d "${current_release}/node_modules" ]] || { echo "Current release dependencies are missing." >&2; exit 69; }
cd /tmp

for command_name in curl find free journalctl pg_isready psql runuser sha256sum systemctl; do
  command -v "${command_name}" >/dev/null || { echo "Missing command: ${command_name}" >&2; exit 69; }
done

cleaned=0
cleanup_accounts() {
  if [[ ${cleaned} -eq 1 ]]; then
    return
  fi
  echo "[private-load] Cleaning accounts with prefix ${account_prefix}"
  runuser -u "${service_user}" -- \
    psql --no-psqlrc --set=ON_ERROR_STOP=1 --quiet \
      --dbname="${database_name}" --set="account_prefix=${account_prefix}" <<'SQL'
delete from game_players
where username like :'account_prefix' || '%';
SQL
  cleaned=1
}
trap cleanup_accounts EXIT

echo "[private-load] Creating pre-cutover PostgreSQL backup"
backup_started_epoch="$(date +%s)"
systemctl start "${backup_service}"
backup_result="$(systemctl show "${backup_service}" --property=Result --value)"
[[ "${backup_result}" == "success" ]] || {
  echo "Backup service result is ${backup_result:-unknown}." >&2
  exit 70
}

backup_roots=()
for candidate in /var/backups/ywonder-game /var/lib/ywonder-game/backups /opt/ywonder-game/backups; do
  [[ -d "${candidate}" ]] && backup_roots+=("${candidate}")
done
[[ ${#backup_roots[@]} -gt 0 ]] || { echo "No known backup directory exists." >&2; exit 66; }

latest_backup="$({
  find "${backup_roots[@]}" -type f ! -name '*.sha256' \
    -printf '%T@ %p\n'
} | sort -nr | head -n 1 | cut -d' ' -f2-)"
[[ -n "${latest_backup}" && -f "${latest_backup}" ]] || { echo "No backup artifact found." >&2; exit 66; }
backup_mtime="$(stat -c %Y "${latest_backup}")"
[[ "${backup_mtime}" -ge "${backup_started_epoch}" ]] || {
  echo "Newest backup was not created by this run: ${latest_backup}" >&2
  exit 70
}
backup_sha="$(sha256sum "${latest_backup}" | awk '{print $1}')"
backup_size="$(stat -c %s "${latest_backup}")"
echo "[private-load] Backup: ${latest_backup}"
echo "[private-load] Backup bytes: ${backup_size}"
echo "[private-load] Backup SHA-256: ${backup_sha}"

systemctl is-active --quiet postgresql
systemctl is-active --quiet "${service_name}"
systemctl is-active --quiet caddy
pg_isready --quiet
curl --fail --silent --show-error --max-time 5 "${base_url}/health" >/dev/null

echo "[private-load] VPS resources before load"
free -h
uptime
load_started_at="$(date --iso-8601=seconds)"

runuser -u "${service_user}" --preserve-environment -- \
  env USER="${service_user}" LOGNAME="${service_user}" HOME="/opt/ywonder-game" \
    NODE_PATH="${current_release}/node_modules" \
    PHASE1_LOAD_BASE_URL="${base_url}" \
    PHASE1_LOAD_CLIENTS="20" \
    PHASE1_LOAD_BATCH_SIZE="4" \
    PHASE1_LOAD_HOLD_MS="3000" \
    PHASE1_LOAD_SUFFIX="${suffix}" \
    "${node_bin}" "${load_test_js}"

systemctl is-active --quiet postgresql
systemctl is-active --quiet "${service_name}"
systemctl is-active --quiet caddy
curl --fail --silent --show-error --max-time 5 "${base_url}/health" >/dev/null

if journalctl -k --since "${load_started_at}" --no-pager \
    | grep -Eiq 'out of memory|oom-kill|killed process'; then
  echo "Kernel reported an OOM event during the load test." >&2
  exit 70
fi

cleanup_accounts
remaining_accounts="$(runuser -u "${service_user}" -- \
  psql --no-psqlrc --tuples-only --no-align --set=ON_ERROR_STOP=1 \
    --dbname="${database_name}" \
    --command="select count(*) from game_players where username like '${account_prefix}%';")"
[[ "${remaining_accounts}" == "0" ]] || {
  echo "Load-test account cleanup left ${remaining_accounts} rows." >&2
  exit 70
}

echo "[private-load] VPS resources after load"
free -h
uptime
echo "[private-load] PASS: backup created, 20-client private load passed, services healthy, no OOM, test accounts removed."
