#!/usr/bin/env bash
set -Eeuo pipefail

usage() {
  echo "Usage: $0 <login-register-transform.mjs> <browser-callback-route.ts>" >&2
  exit 64
}

[[ $# -eq 2 ]] || usage

transform_file="$1"
route_file="$2"
web_root=/var/www/ywonder
service=greenxland.service
service_user=greenxland
service_group="$(id -gn "${service_user}")"
timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
stage="/var/tmp/ywonder-web-browser-sso-${timestamp}"
backup="/var/backups/ywonder-web/browser-sso-${timestamp}"
login_rel='app/[locale]/login/page.tsx'
register_rel='app/[locale]/register/page.tsx'
route_rel='app/api/game/browser/callback/route.ts'
source_installed=0
route_existed=0
completed=0

rollback() {
  local reason="${1:-unknown}"
  trap - ERR EXIT
  set +e
  echo "[rollback] reason=${reason}" >&2

  if [[ ${source_installed} -eq 1 ]]; then
    install -o "${service_user}" -g "${service_group}" -m 0644 \
      "${backup}/${login_rel}" "${web_root}/${login_rel}"
    install -o "${service_user}" -g "${service_group}" -m 0644 \
      "${backup}/${register_rel}" "${web_root}/${register_rel}"
    if [[ ${route_existed} -eq 1 ]]; then
      install -d -o "${service_user}" -g "${service_group}" -m 0755 \
        "$(dirname "${web_root}/${route_rel}")"
      install -o "${service_user}" -g "${service_group}" -m 0644 \
        "${backup}/${route_rel}" "${web_root}/${route_rel}"
    else
      rm -f -- "${web_root}/${route_rel}"
    fi
  fi

  if [[ -d "${backup}/.next" ]]; then
    if [[ -d "${web_root}/.next" ]]; then
      mv "${web_root}/.next" "${backup}/.next.failed-${timestamp}"
    fi
    mv "${backup}/.next" "${web_root}/.next"
    chown -R "${service_user}:${service_group}" "${web_root}/.next"
  fi

  systemctl restart "${service}" || true
  rm -rf -- "${stage}"
}

on_exit() {
  local code=$?
  if [[ ${completed} -ne 1 ]]; then
    rollback "exit-${code}"
  else
    rm -rf -- "${stage}"
  fi
  exit "${code}"
}
trap on_exit EXIT

[[ -f "${transform_file}" ]] || { echo "Transform file missing." >&2; exit 66; }
[[ -f "${route_file}" ]] || { echo "Callback route file missing." >&2; exit 66; }
[[ -d "${web_root}" && -d "${web_root}/node_modules" && -d "${web_root}/.next" ]] || {
  echo "Web runtime is incomplete." >&2
  exit 66
}
[[ -f "${web_root}/${login_rel}" && -f "${web_root}/${register_rel}" ]] || {
  echo "Login/register source is missing." >&2
  exit 66
}
command -v rsync >/dev/null
command -v curl >/dev/null
[[ "$(systemctl is-active "${service}" 2>/dev/null || true)" == "active" ]] || {
  echo "Web service is not active before deployment." >&2
  exit 69
}

echo "[stage] copying web source without active build or dependencies"
install -d -o "${service_user}" -g "${service_group}" -m 0750 "${stage}"
rsync -a \
  --exclude='.next' \
  --exclude='node_modules' \
  --exclude='.git' \
  "${web_root}/" "${stage}/"
ln -s "${web_root}/node_modules" "${stage}/node_modules"

/usr/local/bin/node "${transform_file}" "${stage}"
install -d -o "${service_user}" -g "${service_group}" -m 0755 \
  "$(dirname "${stage}/${route_rel}")"
install -o "${service_user}" -g "${service_group}" -m 0644 \
  "${route_file}" "${stage}/${route_rel}"
chown -R "${service_user}:${service_group}" "${stage}"

echo "[build] compiling isolated Next.js build"
runuser -u "${service_user}" -- \
  env HOME="${web_root}" USER="${service_user}" LOGNAME="${service_user}" \
  bash -c "cd '${stage}' && /usr/local/bin/node node_modules/next/dist/bin/next build"
[[ -f "${stage}/.next/BUILD_ID" ]] || { echo "Staged Next.js build has no BUILD_ID." >&2; exit 70; }

echo "[backup] preserving exact source, active build and unit metadata"
install -d -o root -g root -m 0750 \
  "${backup}/$(dirname "${login_rel}")" \
  "${backup}/$(dirname "${register_rel}")" \
  "${backup}/$(dirname "${route_rel}")"
cp -a -- "${web_root}/${login_rel}" "${backup}/${login_rel}"
cp -a -- "${web_root}/${register_rel}" "${backup}/${register_rel}"
if [[ -f "${web_root}/${route_rel}" ]]; then
  route_existed=1
  cp -a -- "${web_root}/${route_rel}" "${backup}/${route_rel}"
fi
systemctl cat "${service}" >"${backup}/greenxland.service.txt"
sha256sum "${transform_file}" "${route_file}" >"${backup}/deployment-inputs.sha256"

echo "[switch] installing source and atomically replacing .next"
source_installed=1
install -o "${service_user}" -g "${service_group}" -m 0644 \
  "${stage}/${login_rel}" "${web_root}/${login_rel}"
install -o "${service_user}" -g "${service_group}" -m 0644 \
  "${stage}/${register_rel}" "${web_root}/${register_rel}"
install -d -o "${service_user}" -g "${service_group}" -m 0755 \
  "$(dirname "${web_root}/${route_rel}")"
install -o "${service_user}" -g "${service_group}" -m 0644 \
  "${stage}/${route_rel}" "${web_root}/${route_rel}"

mv "${web_root}/.next" "${backup}/.next"
mv "${stage}/.next" "${web_root}/.next"
chown -R "${service_user}:${service_group}" "${web_root}/.next"

systemctl restart "${service}"

echo "[verify] waiting for web service"
health_ok=0
for _ in $(seq 1 45); do
  if curl --fail --silent --show-error --max-time 3 \
      http://127.0.0.1:3033/vi/login >/dev/null; then
    health_ok=1
    break
  fi
  sleep 1
done
[[ ${health_ok} -eq 1 ]] || { echo "Web login health failed." >&2; exit 70; }

probe_request='AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA'
callback_code="$(curl --silent --show-error --output /dev/null --write-out '%{http_code}' \
  --max-time 5 "http://127.0.0.1:3033/api/game/browser/callback?request=${probe_request}&intent=login")"
[[ "${callback_code}" == "302" || "${callback_code}" == "307" ]] || {
  echo "Unauthenticated callback returned ${callback_code}, expected login redirect." >&2
  exit 70
}

completed=1
echo "WEB_BROWSER_SSO_DEPLOY=success"
echo "WEB_BUILD_ID=$(cat "${web_root}/.next/BUILD_ID")"
echo "WEB_BACKUP=${backup}"
echo "WEB_SERVICE=$(systemctl is-active "${service}")"
echo "LOGIN_HTTP=200"
echo "CALLBACK_UNAUTH_HTTP=${callback_code}"
