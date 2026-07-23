const fs = require("fs");
const path = require("path");

const scriptPath = path.join(__dirname, "deploy-web-point-canary-hardening.sh");
const source = fs.readFileSync(scriptPath, "utf8");
const activationSource = fs.readFileSync(path.join(__dirname, "activate-web-point-canary.sh"), "utf8");
const deactivationSource = fs.readFileSync(path.join(__dirname, "deactivate-web-point-canary.sh"), "utf8");

function assert(condition, message) {
  if (!condition) throw new Error(message);
}

function requireText(text, message) {
  assert(source.includes(text), message);
  return source.indexOf(text);
}

function requireBefore(first, second, message) {
  const firstIndex = requireText(first, `Missing safety marker: ${first}`);
  const secondIndex = requireText(second, `Missing safety marker: ${second}`);
  assert(firstIndex < secondIndex, message);
}

requireText("[[ $# -eq 4 ]]", "Deploy runner does not require the pinned input contract.");
requireText("[[ ${EUID} -eq 0 ]]", "Deploy runner does not require root.");
requireText("Overlay must contain exactly two files.", "Overlay file-count guard is missing.");
requireText("Overlay contains an unsafe path.", "Unsafe archive-path guard is missing.");
requireText("Overlay contains a link.", "Archive link guard is missing.");
requireText("flock -n 9", "Single-run deployment lock is missing.");
requireText("resource_gate", "Resource gate is missing.");
requireText("nice -n 10 ionice -c 2 -n 7", "Low-priority build guard is missing.");
requireText("timeout --signal=TERM --kill-after=30s", "Build timeout guard is missing.");

requireBefore(
  'env_value "${game_env}" WEB_TOPUP_ENABLED',
  "WEB_POINT_CANARY_HARDENING=already-applied",
  "Already-applied exit occurs before the dormant production gate."
);
requireBefore(
  "WEB_POINT_CANARY_VALIDATE_ONLY",
  'install -d -o root -g root -m 0700 "${backup_dir}"',
  "Validate-only mode can create a production backup."
);
requireBefore(
  'install -o root -g root -m 0600 "${web_root}/lib/game-point-sync.ts"',
  'log "switching web source and build with rollback armed"',
  "Live source is switched before its rollback copy is created."
);
requireBefore(
  'printf \'%s\\n\' "$(cat "${web_root}/.next/BUILD_ID")"',
  'log "switching web source and build with rollback armed"',
  "Current build identity is not backed up before the service switch."
);

requireText("restore_live", "Rollback function is missing.");
requireText("trap cleanup EXIT", "Rollback cleanup trap is missing.");
requireText('mv "${backup_dir}/next" "${web_root}/.next"', "Old web build restore is missing.");
requireText('sha256sum "${game_env}"', "Game environment integrity guard is missing.");
requireText('sha256sum "${web_root}/.env"', "Web environment integrity guard is missing.");
requireText("GAME_SERVICE_RESTARTED=no", "Final game-service no-restart claim is missing.");
requireText("REAL_PAYMENT_USED=no", "Final no-payment claim is missing.");
requireText("?mode=ro", "SQLite source is not opened read-only for the candidate build.");

assert(!source.includes('systemctl stop "${game_service}"'),
  "Web hardening runner can stop the game service.");
assert(!source.includes("prisma db execute"),
  "Web hardening runner can execute a live database migration.");
assert(!source.includes('DATABASE_URL="file:${live_db}"'),
  "Web hardening runner can point a build or Prisma command at the live SQLite database.");
assert(!source.includes("upsert_env"),
  "Web hardening runner can rewrite environment files.");

function requireToggleText(toggleSource, text, message) {
  assert(toggleSource.includes(text), message);
  return toggleSource.indexOf(text);
}

function requireToggleBefore(toggleSource, first, second, message) {
  const firstIndex = requireToggleText(toggleSource, first, `Missing toggle safety marker: ${first}`);
  const secondIndex = requireToggleText(toggleSource, second, `Missing toggle safety marker: ${second}`);
  assert(firstIndex < secondIndex, message);
}

for (const [label, toggleSource] of [
  ["activation", activationSource],
  ["deactivation", deactivationSource],
]) {
  requireToggleText(toggleSource, "[[ ${EUID} -eq 0 ]]", `${label} does not require root.`);
  requireToggleText(toggleSource, "/run/lock/ywonder-point-canary-config.lock", `${label} does not share the canary configuration lock.`);
  requireToggleText(toggleSource, "flock -n 9", `${label} does not enforce a single configuration writer.`);
  requireToggleText(toggleSource, "restore_backup", `${label} does not define rollback restoration.`);
  requireToggleText(toggleSource, "trap rollback EXIT", `${label} does not arm rollback.`);
  requireToggleText(toggleSource, "WEB_POINT_CANARY_VALIDATE_ONLY", `${label} does not support validate-only mode.`);
  requireToggleText(toggleSource, "https://api.ywonder.net/game-api/internal/web/point-credit", `${label} does not pin the public ingress guard.`);
  requireToggleText(toggleSource, 'install -d -o root -g root -m 0700 "${backup_dir}"', `${label} does not create a root-only backup.`);
  requireToggleBefore(
    toggleSource,
    "WEB_POINT_CANARY_VALIDATE_ONLY",
    'install -d -o root -g root -m 0700 "${backup_dir}"',
    `${label} validate-only mode can create a backup or mutate production.`
  );
  assert(!toggleSource.includes('systemctl stop "${web_service}"'), `${label} can stop the web service.`);
  assert(!toggleSource.includes('systemctl stop "${game_service}"'), `${label} can stop the game service.`);
  assert(!toggleSource.includes("WEB_TOPUP_MODE open"), `${label} can enable open mode.`);
  assert(!toggleSource.includes("rm -"), `${label} contains a destructive remove command.`);
}

requireToggleText(activationSource, 'upsert_env "${game_env}" WEB_TOPUP_ALLOW_REMOTE false',
  "Activation does not keep remote Point ingress disabled.");
requireToggleText(activationSource, 'upsert_env "${game_env}" CLIENT_ASSET_GRANTS_BLOCKED_WEB_USER_IDS "${web_user_id}"',
  "Activation does not scope the client grant block to the canary user.");
requireToggleText(deactivationSource, 'upsert_env "${web_env}" WEB_TOPUP_ENABLED false',
  "Deactivation does not disable the web producer.");
requireToggleText(deactivationSource, 'upsert_env "${game_env}" WEB_TOPUP_ENABLED false',
  "Deactivation does not disable the game ingress.");
requireToggleText(deactivationSource, 'upsert_env "${game_env}" WEB_TOPUP_ALLOWED_WEB_USER_IDS ""',
  "Deactivation does not clear the game canary allowlist.");
requireToggleText(deactivationSource, 'upsert_env "${game_env}" CLIENT_ASSET_GRANTS_BLOCKED_WEB_USER_IDS ""',
  "Deactivation does not clear the scoped client grant block.");
requireToggleText(deactivationSource, '[[ "${topup_http}" == "404" ]]',
  "Deactivation does not verify dormant loopback ingress.");

console.log("[web-point-canary-deploy] PASS: hardening deploy plus canary activation/deactivation locks, validate-only gates, backups and rollback are intact.");
