#!/usr/bin/env bash
set -Eeuo pipefail

requested_target="${1:-/etc/nginx/sites-enabled/ywonder.net.conf}"
backup_dir="/var/backups/ywonder-game/nginx"
status_file="/var/lib/ywonder-game/nginx-public-route-status"
marker_begin="# BEGIN YWONDER GAME BACKEND"
marker_end="# END YWONDER GAME BACKEND"
game_health_url="https://api.ywonder.net/game-api/health"
old_health_url="https://api.ywonder.net/health"
old_auth_url="https://api.ywonder.net/api/game/auth"

[[ ${EUID} -eq 0 ]] || { echo "This script must run as root." >&2; exit 77; }
[[ -f "${requested_target}" ]] || { echo "Nginx site not found: ${requested_target}" >&2; exit 66; }

for command_name in cmp curl grep install nginx python3 readlink sha256sum stat systemctl; do
  command -v "${command_name}" >/dev/null || { echo "Missing command: ${command_name}" >&2; exit 69; }
done

target="$(readlink -f "${requested_target}")"
case "${target}" in
  /etc/nginx/*) ;;
  *) echo "Refusing to edit path outside /etc/nginx: ${target}" >&2; exit 65 ;;
esac

curl --fail --silent --show-error --max-time 5 \
  http://127.0.0.1:3000/health | grep -q '"mode":"postgres"'

original_owner="$(stat -c %U "${target}")"
original_group="$(stat -c %G "${target}")"
original_mode="$(stat -c %a "${target}")"
before_sha="$(sha256sum "${target}" | awk '{print $1}')"
timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
backup_path="${backup_dir}/$(basename "${target}").${timestamp}.bak"
temp_file="$(mktemp /tmp/ywonder-nginx.XXXXXX)"
changed=0
completed=0

rollback() {
  local exit_code=$?
  if [[ ${completed} -eq 0 && ${changed} -eq 1 ]]; then
    echo "Public Nginx route failed; restoring ${backup_path}." >&2
    install -o "${original_owner}" -g "${original_group}" -m "${original_mode}" \
      "${backup_path}" "${target}"
    nginx -t
    systemctl reload nginx
  fi
  rm -f "${temp_file}"
  exit "${exit_code}"
}
trap rollback EXIT

install -d -o root -g root -m 0700 "${backup_dir}"
install -o root -g root -m 0600 "${target}" "${backup_path}"

python3 - "${target}" "${temp_file}" "${marker_begin}" "${marker_end}" <<'PY'
import pathlib
import sys

source_path = pathlib.Path(sys.argv[1])
output_path = pathlib.Path(sys.argv[2])
marker_begin = sys.argv[3]
marker_end = sys.argv[4]
text = source_path.read_text(encoding="utf-8")

block = """    # BEGIN YWONDER GAME BACKEND
    location = /game-api/realtime {
        access_log off;
        proxy_pass http://127.0.0.1:3000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_read_timeout 3600s;
        proxy_send_timeout 3600s;
        proxy_buffering off;
    }

    location ^~ /game-api/ {
        proxy_pass http://127.0.0.1:3000;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_read_timeout 60s;
    }
    # END YWONDER GAME BACKEND
"""

has_begin = marker_begin in text
has_end = marker_end in text
if has_begin or has_end:
    if has_begin and has_end and block.rstrip() in text:
        output_path.write_text(text, encoding="utf-8")
        print("already_configured")
        raise SystemExit(0)
    raise SystemExit("Existing YWONDER marker is incomplete or differs from the expected block.")

lines = text.splitlines(keepends=True)
blocks = []
index = 0
while index < len(lines):
    stripped = lines[index].split("#", 1)[0].strip()
    if stripped != "server {":
        index += 1
        continue
    start = index
    depth = 0
    while index < len(lines):
        code = lines[index].split("#", 1)[0]
        depth += code.count("{") - code.count("}")
        if depth == 0:
            break
        index += 1
    if depth != 0:
        raise SystemExit("Could not parse Nginx server block braces.")
    end = index
    block_text = "".join(lines[start : end + 1])
    if (
        "server_name api.ywonder.net;" in block_text
        and "listen 443 ssl" in block_text
        and "location /api/game/" in block_text
        and "proxy_pass http://127.0.0.1:3033;" in block_text
        and "proxy_pass http://127.0.0.1:3036;" in block_text
    ):
        blocks.append((start, end))
    index += 1

if len(blocks) != 1:
    raise SystemExit(f"Expected one HTTPS api.ywonder.net block, found {len(blocks)}.")

start, end = blocks[0]
insert_at = None
for line_index in range(start, end + 1):
    if lines[line_index].strip() == "server_name api.ywonder.net;":
        insert_at = line_index + 1
        break
if insert_at is None:
    raise SystemExit("Could not find api.ywonder.net server_name insertion point.")

lines.insert(insert_at, block + "\n")
output_path.write_text("".join(lines), encoding="utf-8")
print("configured")
PY

if cmp -s "${target}" "${temp_file}"; then
  echo "Expected /game-api Nginx block is already present."
else
  install -o "${original_owner}" -g "${original_group}" -m "${original_mode}" \
    "${temp_file}" "${target}"
  changed=1
fi

nginx -t
systemctl reload nginx

for _ in $(seq 1 20); do
  if curl --fail --silent --show-error --max-time 5 "${game_health_url}" \
      | grep -q '"mode":"postgres"'; then
    break
  fi
  sleep 1
done
curl --fail --silent --show-error --max-time 5 "${game_health_url}" \
  | grep -q '"mode":"postgres"'
curl --fail --silent --show-error --max-time 5 "${old_health_url}" \
  | grep -q '"service":"ywonderland-main-game-api"'

old_auth_status="$(curl --silent --show-error --max-time 8 --output /dev/null \
  --write-out '%{http_code}' --request POST --header 'Content-Type: application/json' \
  --data '{"username":"NginxRouteProbe","password":"ProbeOnly@123"}' \
  "${old_auth_url}")"
case "${old_auth_status}" in
  400|401|403) ;;
  *) echo "Old web auth route returned unexpected status ${old_auth_status}." >&2; exit 70 ;;
esac

after_sha="$(sha256sum "${target}" | awk '{print $1}')"
install -d -o root -g ywonder_game -m 0750 /var/lib/ywonder-game
cat >"${status_file}" <<EOF
status=passed
completed_at=$(date -u +%Y-%m-%dT%H:%M:%SZ)
target=${target}
backup=${backup_path}
before_sha256=${before_sha}
after_sha256=${after_sha}
game_health=passed
old_health=passed
old_auth_status=${old_auth_status}
EOF
chown root:ywonder_game "${status_file}"
chmod 0640 "${status_file}"

completed=1
echo "Public /game-api Nginx route configured successfully."
echo "Backup: ${backup_path}"
echo "Before SHA-256: ${before_sha}"
echo "After SHA-256: ${after_sha}"
