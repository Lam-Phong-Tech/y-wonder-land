#!/usr/bin/env bash
set -Eeuo pipefail

usage() {
  echo "Usage: $0 <web-env-file> <game-env-file> <loopback-sync-url>" >&2
  exit 64
}

[[ $# -eq 3 ]] || usage
[[ ${EUID} -eq 0 ]] || { echo "Run as root." >&2; exit 77; }

web_env="$1"
game_env="$2"
sync_url="$3"
web_service="greenxland.service"
game_service="ywonder-game-server.service"
stamp="$(date -u +%Y%m%dT%H%M%SZ)"
backup_dir="/var/backups/ywonder-point-link/dormant-${stamp}"
configured=0
completed=0

for command_name in awk cp curl mktemp openssl sha256sum stat systemctl; do
  command -v "${command_name}" >/dev/null || {
    echo "Missing command: ${command_name}" >&2
    exit 69
  }
done

[[ -f "${web_env}" ]] || { echo "Web environment file is missing." >&2; exit 66; }
[[ -f "${game_env}" ]] || { echo "Game environment file is missing." >&2; exit 66; }
[[ "${sync_url}" =~ ^http://127\.0\.0\.1:[0-9]+/internal/web/point-credit$ ]] || {
  echo "Point sync URL must use the expected loopback endpoint." >&2
  exit 65
}
[[ "$(systemctl is-active "${web_service}")" == "active" ]] || {
  echo "Web service is not active." >&2
  exit 70
}
[[ "$(systemctl is-active "${game_service}")" == "active" ]] || {
  echo "Game service is not active." >&2
  exit 70
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

rollback() {
  local exit_code=$?
  trap - EXIT
  if [[ ${completed} -eq 0 && ${configured} -eq 1 ]]; then
    echo "[rollback] restoring both environment files" >&2
    cp --preserve=mode,ownership,timestamps "${backup_dir}/web.env" "${web_env}" || true
    cp --preserve=mode,ownership,timestamps "${backup_dir}/game.env" "${game_env}" || true
    systemctl restart "${web_service}" || true
    systemctl restart "${game_service}" || true
    echo "DORMANT_POINT_LINK_ROLLBACK=${backup_dir}" >&2
  fi
  exit "${exit_code}"
}
trap rollback EXIT

wait_for_health http://127.0.0.1:3000/health || {
  echo "Game health check failed before dormant configuration." >&2
  exit 70
}
wait_for_health http://127.0.0.1:3033/api/health || {
  echo "Web health check failed before dormant configuration." >&2
  exit 70
}

preflight_topup_http="$(curl --silent --show-error --max-time 5 --output /dev/null \
  --write-out '%{http_code}' --request POST \
  --header 'Content-Type: application/json' --data '{}' \
  http://127.0.0.1:3000/internal/web/point-credit)"
[[ "${preflight_topup_http}" == "404" ]] || {
  echo "Game Point ingress is not dormant before configuration." >&2
  exit 73
}

install -d -o root -g root -m 0700 "${backup_dir}"
cp --preserve=mode,ownership,timestamps "${web_env}" "${backup_dir}/web.env"
cp --preserve=mode,ownership,timestamps "${game_env}" "${backup_dir}/game.env"

web_secret="$(env_value "${web_env}" WEB_TOPUP_SECRET)"
game_secret="$(env_value "${game_env}" WEB_TOPUP_SECRET)"
if [[ ${#web_secret} -ge 32 && "${web_secret}" == "${game_secret}" ]]; then
  shared_secret="${web_secret}"
else
  shared_secret="$(openssl rand -hex 48)"
fi

configured=1
upsert_env "${web_env}" GAME_POINT_SYNC_URL "${sync_url}"
upsert_env "${web_env}" WEB_TOPUP_SECRET "${shared_secret}"
upsert_env "${game_env}" WEB_TOPUP_SECRET "${shared_secret}"
upsert_env "${web_env}" WEB_TOPUP_MODE canary
upsert_env "${game_env}" WEB_TOPUP_MODE canary
upsert_env "${game_env}" WEB_TOPUP_ENABLED false
upsert_env "${game_env}" WEB_TOPUP_ALLOW_REMOTE false
unset web_secret game_secret shared_secret

systemctl restart "${game_service}"
systemctl restart "${web_service}"
wait_for_health http://127.0.0.1:3000/health || {
  echo "Game health check failed after dormant configuration." >&2
  exit 70
}
wait_for_health http://127.0.0.1:3033/api/health || {
  echo "Web health check failed after dormant configuration." >&2
  exit 70
}

web_secret_hash="$(env_value "${web_env}" WEB_TOPUP_SECRET | sha256sum | awk '{print $1}')"
game_secret_hash="$(env_value "${game_env}" WEB_TOPUP_SECRET | sha256sum | awk '{print $1}')"
[[ "${web_secret_hash}" == "${game_secret_hash}" ]] || {
  echo "Web and game Point secrets do not match." >&2
  exit 65
}
[[ "$(env_value "${web_env}" GAME_POINT_SYNC_URL)" == "${sync_url}" ]] || {
  echo "Web Point sync URL was not persisted." >&2
  exit 65
}
[[ "$(env_value "${web_env}" WEB_TOPUP_MODE)" == "canary" ]] || {
  echo "Web Point sync mode is not safely dormant in canary mode." >&2
  exit 73
}
[[ "$(env_value "${game_env}" WEB_TOPUP_MODE)" == "canary" ]] || {
  echo "Game Point sync mode is not safely dormant in canary mode." >&2
  exit 73
}
[[ "$(env_value "${game_env}" WEB_TOPUP_ENABLED)" == "false" ]] || {
  echo "Game Point ingress was unexpectedly enabled." >&2
  exit 73
}
[[ "$(env_value "${game_env}" WEB_TOPUP_ALLOW_REMOTE)" == "false" ]] || {
  echo "Remote Point ingress was unexpectedly enabled." >&2
  exit 73
}

topup_http="$(curl --silent --show-error --max-time 5 --output /dev/null \
  --write-out '%{http_code}' --request POST \
  --header 'Content-Type: application/json' --data '{}' \
  http://127.0.0.1:3000/internal/web/point-credit)"
cron_http="$(curl --silent --show-error --max-time 5 --output /dev/null \
  --write-out '%{http_code}' --request POST \
  http://127.0.0.1:3033/api/cron/game-point-sync)"
[[ "${topup_http}" == "404" ]] || {
  echo "Game Point ingress is not dormant after configuration." >&2
  exit 73
}
[[ "${cron_http}" == "401" ]] || {
  echo "Web Point cron accepted an unauthenticated request." >&2
  exit 73
}

completed=1
echo "DORMANT_POINT_LINK=success"
echo "SHARED_SECRET_MATCH=yes"
echo "WEB_SERVICE=active"
echo "GAME_SERVICE=active"
echo "WEB_HEALTH_HTTP=200"
echo "GAME_HEALTH_HTTP=200"
echo "OUTBOX_CRON_UNAUTH_HTTP=${cron_http}"
echo "GAME_TOPUP_ENABLED=false"
echo "POINT_TOPUP_MODE=canary"
echo "GAME_TOPUP_ENDPOINT_HTTP=${topup_http}"
echo "BACKUP_DIR=${backup_dir}"
