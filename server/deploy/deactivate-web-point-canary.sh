#!/usr/bin/env bash
set -Eeuo pipefail

usage() {
  echo "Usage: $0 <web-env-file> <game-env-file> <expected-web-user-id>" >&2
  exit 64
}

[[ $# -eq 3 ]] || usage
[[ ${EUID} -eq 0 ]] || { echo "Run as root." >&2; exit 77; }

web_env="$1"
game_env="$2"
expected_web_user_id="$3"
web_service="greenxland.service"
game_service="ywonder-game-server.service"
web_health_url="http://127.0.0.1:3033/api/health"
game_health_url="http://127.0.0.1:3000/health"
loopback_topup_url="http://127.0.0.1:3000/internal/web/point-credit"
public_topup_url="https://api.ywonder.net/game-api/internal/web/point-credit"
expected_sync_url="${loopback_topup_url}"
stamp="$(date -u +%Y%m%dT%H%M%SZ)"
backup_dir="/var/backups/ywonder-point-link/deactivate-${stamp}"
lock_file="/run/lock/ywonder-point-canary-config.lock"
configured=0
completed=0

for command_name in awk cp curl dirname flock install mktemp mv sha256sum stat systemctl; do
  command -v "${command_name}" >/dev/null || {
    echo "Missing command: ${command_name}" >&2
    exit 69
  }
done

[[ -f "${web_env}" ]] || { echo "Web environment file is missing." >&2; exit 66; }
[[ -f "${game_env}" ]] || { echo "Game environment file is missing." >&2; exit 66; }
[[ "${expected_web_user_id}" =~ ^[A-Za-z0-9._:-]{8,128}$ ]] || {
  echo "Expected canary web user ID has an invalid format." >&2
  exit 65
}

exec 9>"${lock_file}"
flock -n 9 || {
  echo "Another Point canary configuration change is active." >&2
  exit 75
}

env_value() {
  local file="$1"
  local key="$2"
  awk -v prefix="${key}=" 'index($0, prefix) == 1 { print substr($0, length(prefix) + 1); exit }' "${file}"
}

upsert_env() {
  local file="$1"
  local key="$2"
  local value="$3"
  local dir temp uid gid mode
  dir="$(dirname "${file}")"
  temp="$(mktemp "${dir}/.ywonder-env.XXXXXX")"
  uid="$(stat -c '%u' "${file}")"
  gid="$(stat -c '%g' "${file}")"
  mode="$(stat -c '%a' "${file}")"

  awk -v prefix="${key}=" 'index($0, prefix) != 1 { print }' "${file}" > "${temp}"
  printf '%s=%s\n' "${key}" "${value}" >> "${temp}"
  chown "${uid}:${gid}" "${temp}"
  chmod "${mode}" "${temp}"
  mv -f -- "${temp}" "${file}"
}

wait_for_health() {
  local url="$1"
  local attempt
  for attempt in $(seq 1 45); do
    if curl --fail --silent --max-time 2 "${url}" >/dev/null; then
      return 0
    fi
    sleep 1
  done
  return 1
}

http_status() {
  local url="$1"
  curl --silent --show-error --max-time 8 --output /dev/null \
    --write-out '%{http_code}' --request POST \
    --header 'Content-Type: application/json' --data '{}' "${url}"
}

restore_backup() {
  cp --preserve=mode,ownership,timestamps "${backup_dir}/web.env" "${web_env}"
  cp --preserve=mode,ownership,timestamps "${backup_dir}/game.env" "${game_env}"
  systemctl restart "${game_service}"
  wait_for_health "${game_health_url}"
  systemctl restart "${web_service}"
  wait_for_health "${web_health_url}"
}

rollback() {
  local exit_code=$?
  trap - EXIT
  if [[ ${completed} -eq 0 && ${configured} -eq 1 ]]; then
    echo "[rollback] restoring active canary environment files" >&2
    restore_backup || true
    echo "POINT_CANARY_DEACTIVATE_ROLLBACK=${backup_dir}" >&2
  fi
  exit "${exit_code}"
}
trap rollback EXIT

for service in "${web_service}" "${game_service}"; do
  [[ "$(systemctl is-active "${service}")" == "active" ]] || {
    echo "Service is not active: ${service}" >&2
    exit 70
  }
done
wait_for_health "${game_health_url}" || { echo "Game preflight health failed." >&2; exit 70; }
wait_for_health "${web_health_url}" || { echo "Web preflight health failed." >&2; exit 70; }

[[ "$(env_value "${web_env}" WEB_TOPUP_ENABLED)" == "true" ]] || exit 73
[[ "$(env_value "${web_env}" WEB_TOPUP_MODE)" == "canary" ]] || exit 73
[[ "$(env_value "${web_env}" WEB_TOPUP_ALLOWED_WEB_USER_IDS)" == "${expected_web_user_id}" ]] || exit 73
[[ "$(env_value "${web_env}" GAME_POINT_SYNC_URL)" == "${expected_sync_url}" ]] || exit 73
[[ "$(env_value "${game_env}" WEB_TOPUP_ENABLED)" == "true" ]] || exit 73
[[ "$(env_value "${game_env}" WEB_TOPUP_MODE)" == "canary" ]] || exit 73
[[ "$(env_value "${game_env}" WEB_TOPUP_ALLOW_REMOTE)" == "false" ]] || exit 73
[[ "$(env_value "${game_env}" WEB_TOPUP_ALLOWED_WEB_USER_IDS)" == "${expected_web_user_id}" ]] || exit 73
[[ "$(env_value "${game_env}" CLIENT_ASSET_GRANTS_ENABLED)" == "true" ]] || exit 73
[[ "$(env_value "${game_env}" CLIENT_ASSET_GRANTS_BLOCKED_WEB_USER_IDS)" == "${expected_web_user_id}" ]] || exit 73

web_secret_hash="$(env_value "${web_env}" WEB_TOPUP_SECRET | sha256sum | awk '{print $1}')"
game_secret_hash="$(env_value "${game_env}" WEB_TOPUP_SECRET | sha256sum | awk '{print $1}')"
[[ "${web_secret_hash}" == "${game_secret_hash}" ]] || {
  echo "Web and game Point secrets do not match." >&2
  exit 65
}

preflight_topup_http="$(http_status "${loopback_topup_url}")"
preflight_public_http="$(http_status "${public_topup_url}")"
preflight_cron_http="$(http_status http://127.0.0.1:3033/api/cron/game-point-sync)"
[[ "${preflight_topup_http}" == "401" ]] || exit 73
[[ "${preflight_public_http}" == "404" ]] || exit 73
[[ "${preflight_cron_http}" == "401" ]] || exit 73

case "${WEB_POINT_CANARY_VALIDATE_ONLY:-0}" in
  0) ;;
  1)
    echo "POINT_CANARY_DEACTIVATE_VALIDATE=pass"
    echo "CONFIG_CHANGE=no"
    echo "SERVICE_RESTARTED=no"
    exit 0
    ;;
  *)
    echo "WEB_POINT_CANARY_VALIDATE_ONLY must be 0 or 1." >&2
    exit 64
    ;;
esac

install -d -o root -g root -m 0700 "${backup_dir}"
cp --preserve=mode,ownership,timestamps "${web_env}" "${backup_dir}/web.env"
cp --preserve=mode,ownership,timestamps "${game_env}" "${backup_dir}/game.env"

configured=1
upsert_env "${web_env}" WEB_TOPUP_ENABLED false
upsert_env "${web_env}" WEB_TOPUP_MODE canary
upsert_env "${web_env}" WEB_TOPUP_ALLOWED_WEB_USER_IDS ""

upsert_env "${game_env}" WEB_TOPUP_ENABLED false
upsert_env "${game_env}" WEB_TOPUP_MODE canary
upsert_env "${game_env}" WEB_TOPUP_ALLOW_REMOTE false
upsert_env "${game_env}" WEB_TOPUP_ALLOWED_WEB_USER_IDS ""
upsert_env "${game_env}" CLIENT_ASSET_GRANTS_ENABLED true
upsert_env "${game_env}" CLIENT_ASSET_GRANTS_BLOCKED_WEB_USER_IDS ""

systemctl restart "${game_service}"
wait_for_health "${game_health_url}" || { echo "Game health failed after canary deactivation." >&2; exit 70; }
systemctl restart "${web_service}"
wait_for_health "${web_health_url}" || { echo "Web health failed after canary deactivation." >&2; exit 70; }

[[ "$(systemctl is-active "${game_service}")" == "active" ]] || exit 70
[[ "$(systemctl is-active "${web_service}")" == "active" ]] || exit 70
[[ "$(env_value "${web_env}" WEB_TOPUP_ENABLED)" == "false" ]] || exit 73
[[ "$(env_value "${web_env}" WEB_TOPUP_MODE)" == "canary" ]] || exit 73
[[ -z "$(env_value "${web_env}" WEB_TOPUP_ALLOWED_WEB_USER_IDS)" ]] || exit 73
[[ "$(env_value "${web_env}" GAME_POINT_SYNC_URL)" == "${expected_sync_url}" ]] || exit 73
[[ "$(env_value "${game_env}" WEB_TOPUP_ENABLED)" == "false" ]] || exit 73
[[ "$(env_value "${game_env}" WEB_TOPUP_MODE)" == "canary" ]] || exit 73
[[ "$(env_value "${game_env}" WEB_TOPUP_ALLOW_REMOTE)" == "false" ]] || exit 73
[[ -z "$(env_value "${game_env}" WEB_TOPUP_ALLOWED_WEB_USER_IDS)" ]] || exit 73
[[ "$(env_value "${game_env}" CLIENT_ASSET_GRANTS_ENABLED)" == "true" ]] || exit 73
[[ -z "$(env_value "${game_env}" CLIENT_ASSET_GRANTS_BLOCKED_WEB_USER_IDS)" ]] || exit 73
[[ "$(env_value "${web_env}" WEB_TOPUP_SECRET | sha256sum | awk '{print $1}')" == "${web_secret_hash}" ]] || exit 65
[[ "$(env_value "${game_env}" WEB_TOPUP_SECRET | sha256sum | awk '{print $1}')" == "${game_secret_hash}" ]] || exit 65

topup_http="$(http_status "${loopback_topup_url}")"
public_topup_http="$(http_status "${public_topup_url}")"
cron_http="$(http_status http://127.0.0.1:3033/api/cron/game-point-sync)"
[[ "${topup_http}" == "404" ]] || { echo "Dormant Point ingress is not hidden." >&2; exit 73; }
[[ "${public_topup_http}" == "404" ]] || { echo "Public Point ingress is unexpectedly reachable." >&2; exit 73; }
[[ "${cron_http}" == "401" ]] || { echo "Unauthenticated outbox cron was accepted." >&2; exit 73; }

completed=1
echo "POINT_CANARY=inactive"
echo "CANARY_USER_COUNT=0"
echo "CLIENT_GRANT_BLOCK_COUNT=0"
echo "WEB_SERVICE=active"
echo "GAME_SERVICE=active"
echo "WEB_HEALTH_HTTP=200"
echo "GAME_HEALTH_HTTP=200"
echo "LOOPBACK_UNSIGNED_TOPUP_HTTP=${topup_http}"
echo "PUBLIC_TOPUP_HTTP=${public_topup_http}"
echo "OUTBOX_CRON_UNAUTH_HTTP=${cron_http}"
echo "BACKUP_DIR=${backup_dir}"
