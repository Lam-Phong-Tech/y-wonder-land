#!/usr/bin/env bash
set -Eeuo pipefail

usage() {
  echo "Usage: $0 <browser-callback-route.ts> <release-id>" >&2
  exit 64
}

[[ $# -eq 2 ]] || usage

route_file="$1"
release_id="$2"
web_root=/var/www/ywonder
service=greenxland.service
service_user=greenxland
service_group="$(id -gn "${service_user}")"
timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
stage="/var/tmp/ywonder-web-callback-${release_id}-${timestamp}"
backup="/var/backups/ywonder-web/browser-callback-${release_id}-${timestamp}"
route_rel='app/api/game/browser/callback/route.ts'
source_installed=0
completed=0

rollback() {
  local reason="${1:-unknown}"
  trap - ERR EXIT
  set +e
  echo "[rollback] reason=${reason}" >&2

  if [[ ${source_installed} -eq 1 && -f "${backup}/${route_rel}" ]]; then
    install -o "${service_user}" -g "${service_group}" -m 0644 \
      "${backup}/${route_rel}" "${web_root}/${route_rel}"
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

[[ -f "${route_file}" ]] || { echo "Callback route file missing." >&2; exit 66; }
[[ "${release_id}" =~ ^[A-Za-z0-9._-]{7,80}$ ]] || { echo "Invalid release id." >&2; exit 64; }
[[ -d "${web_root}" && -d "${web_root}/node_modules" && -d "${web_root}/.next" ]] || {
  echo "Web runtime is incomplete." >&2
  exit 66
}
[[ -f "${web_root}/${route_rel}" ]] || { echo "Active callback route is missing." >&2; exit 66; }
[[ "$(systemctl is-active "${service}" 2>/dev/null || true)" == "active" ]] || {
  echo "Web service is not active before deployment." >&2
  exit 69
}

echo "[stage] copying web source and installing callback ${release_id}"
install -d -o "${service_user}" -g "${service_group}" -m 0750 "${stage}"
rsync -a \
  --exclude='.next' \
  --exclude='node_modules' \
  --exclude='.git' \
  "${web_root}/" "${stage}/"
ln -s "${web_root}/node_modules" "${stage}/node_modules"
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

echo "[backup] preserving active callback and build"
install -d -o root -g root -m 0750 "${backup}/$(dirname "${route_rel}")"
cp -a -- "${web_root}/${route_rel}" "${backup}/${route_rel}"
sha256sum "${route_file}" >"${backup}/deployment-input.sha256"
systemctl cat "${service}" >"${backup}/greenxland.service.txt"

echo "[switch] replacing callback source and build"
source_installed=1
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
login_redirect="$(curl --silent --show-error --output /dev/null --write-out '%{redirect_url}' \
  --max-time 5 "http://127.0.0.1:3033/api/game/browser/callback?request=${probe_request}&intent=login")"
register_redirect="$(curl --silent --show-error --output /dev/null --write-out '%{redirect_url}' \
  --max-time 5 "http://127.0.0.1:3033/api/game/browser/callback?request=${probe_request}&intent=register")"
switch_headers="${stage}/switch-account.headers"
switch_redirect="$(curl --silent --show-error --dump-header "${switch_headers}" --output /dev/null --write-out '%{redirect_url}' \
  --max-time 5 "http://127.0.0.1:3033/api/game/browser/callback?request=${probe_request}&intent=login&account_action=switch")"

grep -Eiq '^cache-control:.*no-store' "${switch_headers}"
grep -Eiq '^set-cookie: (__Secure-)?authjs\.session-token=' "${switch_headers}"

/usr/local/bin/node -e '
  const [loginValue, registerValue, switchValue] = process.argv.slice(1);
  const login = new URL(loginValue);
  const register = new URL(registerValue);
  const switching = new URL(switchValue);
  if (login.origin !== "https://ywonder.net" || login.pathname !== "/vi/login") process.exit(2);
  if (register.origin !== "https://ywonder.net" || register.pathname !== "/vi/register") process.exit(3);
  if (switching.origin !== "https://ywonder.net" ||
      switching.pathname !== "/vi/login" ||
      switching.searchParams.get("locked") !== "1") process.exit(5);
  const loginCallback = new URL(login.searchParams.get("callbackUrl") || "https://invalid/");
  const callback = new URL(register.searchParams.get("callbackUrl") || "https://invalid/");
  const switchCallback = new URL(switching.searchParams.get("callbackUrl") || "https://invalid/");
  if (loginCallback.searchParams.get("account_confirmed") !== "1") process.exit(6);
  if (callback.origin !== "https://ywonder.net" ||
      callback.pathname !== "/api/game/browser/callback" ||
      callback.searchParams.get("intent") !== "register" ||
      callback.searchParams.get("registration_completed") !== "1" ||
      callback.searchParams.get("account_confirmed") !== "1") process.exit(4);
  if (switchCallback.origin !== "https://ywonder.net" ||
      switchCallback.pathname !== "/api/game/browser/callback" ||
      switchCallback.searchParams.get("account_confirmed") !== "1" ||
      switchCallback.searchParams.has("account_action")) process.exit(7);
' "${login_redirect}" "${register_redirect}" "${switch_redirect}"

completed=1
echo "WEB_BROWSER_CALLBACK_DEPLOY=success"
echo "WEB_BUILD_ID=$(cat "${web_root}/.next/BUILD_ID")"
echo "WEB_BACKUP=${backup}"
echo "WEB_SERVICE=$(systemctl is-active "${service}")"
echo "LOGIN_REDIRECT=pass"
echo "REGISTER_REDIRECT=pass"
echo "SWITCH_ACCOUNT_REDIRECT=pass"
