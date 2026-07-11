#!/usr/bin/env bash
set -Eeuo pipefail

usage() {
  echo "Usage: $0 <archive.tar.gz> <release-id> <sha256>" >&2
  exit 64
}

[[ $# -eq 3 ]] || usage
[[ ${EUID} -eq 0 ]] || { echo "This script must run as root." >&2; exit 77; }

archive="$1"
release_id="$2"
expected_sha="$3"

[[ "${release_id}" =~ ^[0-9a-f]{7,40}$ ]] || { echo "Invalid release id." >&2; exit 65; }
[[ "${expected_sha}" =~ ^[0-9a-f]{64}$ ]] || { echo "Invalid SHA-256." >&2; exit 65; }
[[ -f "${archive}" ]] || { echo "Archive not found: ${archive}" >&2; exit 66; }

base_dir="/opt/ywonder-game"
releases_dir="${base_dir}/releases"
release_dir="${releases_dir}/${release_id}"
current_link="${base_dir}/current"
env_file="/etc/ywonder-game/game-server.env"
unit_file="/etc/systemd/system/ywonder-game-server.service"
service_name="ywonder-game-server.service"
service_user="ywonder_game"
service_group="ywonder_game"
node_bin="/usr/local/bin/node"
npm_bin="/usr/local/bin/npm"

for command_name in sha256sum tar runuser systemctl systemd-analyze curl; do
  command -v "${command_name}" >/dev/null || { echo "Missing command: ${command_name}" >&2; exit 69; }
done
[[ -x "${node_bin}" ]] || { echo "Node binary is missing: ${node_bin}" >&2; exit 69; }
[[ -x "${npm_bin}" ]] || { echo "npm binary is missing: ${npm_bin}" >&2; exit 69; }
[[ -f "${env_file}" ]] || { echo "Environment file is missing: ${env_file}" >&2; exit 66; }
[[ -L "${current_link}" || ! -e "${current_link}" ]] || {
  echo "Current release path must be a symlink: ${current_link}" >&2
  exit 73
}
[[ ! -e "${release_dir}" ]] || { echo "Release already exists: ${release_dir}" >&2; exit 73; }

actual_sha="$(sha256sum "${archive}" | awk '{print $1}')"
[[ "${actual_sha}" == "${expected_sha}" ]] || {
  echo "Archive checksum mismatch." >&2
  exit 65
}

previous_release=""
if [[ -L "${current_link}" ]]; then
  previous_release="$(readlink -f "${current_link}")"
fi

unit_backup="$(mktemp /tmp/ywonder-game-unit.XXXXXX)"
if [[ -f "${unit_file}" ]]; then
  cp --preserve=mode,ownership,timestamps "${unit_file}" "${unit_backup}"
else
  : >"${unit_backup}"
fi

switched=0
completed=0

rollback() {
  local exit_code=$?
  if [[ ${completed} -eq 0 && ${switched} -eq 1 ]]; then
    echo "Deployment failed; restoring previous release." >&2
    if [[ -n "${previous_release}" && -d "${previous_release}" ]]; then
      ln -sfn "${previous_release}" "${current_link}"
    fi
    if [[ -s "${unit_backup}" ]]; then
      install -o root -g root -m 0644 "${unit_backup}" "${unit_file}"
    fi
    systemctl daemon-reload || true
    systemctl restart "${service_name}" || true
  fi
  rm -f "${unit_backup}"
  exit "${exit_code}"
}
trap rollback EXIT

install -d -o root -g "${service_group}" -m 0750 "${releases_dir}"
install -d -o root -g "${service_group}" -m 0750 "${release_dir}"
tar -xzf "${archive}" -C "${release_dir}"

[[ -f "${release_dir}/package.json" ]] || { echo "package.json missing from release." >&2; exit 65; }
[[ -f "${release_dir}/package-lock.json" ]] || { echo "package-lock.json missing from release." >&2; exit 65; }
[[ -f "${release_dir}/security.js" ]] || { echo "security.js missing from release." >&2; exit 65; }

cd "${release_dir}"
"${npm_bin}" ci --omit=dev --ignore-scripts --no-audit --no-fund

chown -R root:"${service_group}" "${release_dir}"
find "${release_dir}" -type d -exec chmod 0750 {} +
find "${release_dir}" -type f -exec chmod 0640 {} +

set -a
# shellcheck disable=SC1090
source "${env_file}"
set +a

runuser -u "${service_user}" --preserve-environment -- \
  env USER="${service_user}" LOGNAME="${service_user}" HOME="${base_dir}" PGUSER="${service_user}" \
  "${node_bin}" -e "require('./security').validateProductionConfig();"
runuser -u "${service_user}" --preserve-environment -- \
  env USER="${service_user}" LOGNAME="${service_user}" HOME="${base_dir}" PGUSER="${service_user}" \
  "${node_bin}" scripts/migratePostgres.js

install -o root -g root -m 0644 \
  "${release_dir}/deploy/ywonder-game-server.service.example" "${unit_file}"
systemd-analyze verify "${unit_file}"
systemctl daemon-reload

ln -sfn "${release_dir}" "${current_link}"
switched=1
systemctl restart "${service_name}"

node_health=0
for _ in $(seq 1 30); do
  if curl --fail --silent --show-error --max-time 2 \
      http://127.0.0.1:3000/health >/dev/null; then
    node_health=1
    break
  fi
  sleep 1
done
[[ ${node_health} -eq 1 ]] || { echo "Node health check failed." >&2; exit 70; }

curl --fail --silent --show-error --max-time 5 \
  http://127.0.0.1:8080/health >/dev/null

install -d -o root -g "${service_group}" -m 0750 /var/lib/ywonder-game
cat >/var/lib/ywonder-game/deployment-status <<EOF
status=passed
release=${release_id}
sha256=${expected_sha}
completed_at=$(date -u +%Y-%m-%dT%H:%M:%SZ)
node_health=passed
caddy_health=passed
EOF
chown root:"${service_group}" /var/lib/ywonder-game/deployment-status
chmod 0640 /var/lib/ywonder-game/deployment-status

completed=1
echo "Private release ${release_id} deployed successfully."
