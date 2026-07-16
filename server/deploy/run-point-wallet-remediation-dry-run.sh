#!/usr/bin/env bash
set -Eeuo pipefail
umask 077

[[ $# -eq 5 ]] || {
  echo "Usage: $0 <web-sqlite-database> <approved-decisions.json> <approved-sha256> <plan.json> <plan.md>" >&2
  exit 64
}

script_root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
web_database="$1"
approved_path="$2"
approved_sha256="${3,,}"
json_output_path="$4"
markdown_output_path="$5"
run_root=""
report_key="${POINT_MIGRATION_REPORT_KEY:-}"
game_export_user="${POINT_MIGRATION_GAME_EXPORT_USER:-}"

fail() {
  echo "[point-wallet-remediation-dry-run] $*" >&2
  exit 1
}

cleanup() {
  local exit_code=$?
  trap - EXIT INT TERM
  if [[ -n "${run_root}" && "${run_root}" == /tmp/ywonder-point-remediation-dry-run.* ]]; then
    rm -rf -- "${run_root}"
  elif [[ -n "${run_root}" ]]; then
    echo "Unsafe remediation cleanup path refused: ${run_root}" >&2
    exit_code=74
  fi
  exit "${exit_code}"
}
trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM

for command_name in awk chmod cmp date dirname env id install mktemp node python3 rm sha256sum; do
  command -v "${command_name}" >/dev/null || fail "Missing command: ${command_name}"
done
[[ -f "${web_database}" ]] || fail "Web SQLite database is missing."
[[ -f "${approved_path}" ]] || fail "Approved decision artifact is missing."
[[ "${approved_sha256}" =~ ^[a-f0-9]{64}$ ]] || fail "Approved SHA-256 has an invalid format."
[[ "$(sha256sum "${approved_path}" | awk '{print $1}')" == "${approved_sha256}" ]] \
  || fail "Approved decision artifact checksum mismatch."
[[ -n "${DATABASE_URL:-}" ]] || fail "DATABASE_URL is required for the game PostgreSQL read."
[[ ${#report_key} -ge 32 ]] || fail "POINT_MIGRATION_REPORT_KEY must contain at least 32 characters."
[[ ! -e "${json_output_path}" && ! -L "${json_output_path}" ]] \
  || fail "Refusing to overwrite the JSON plan."
[[ ! -e "${markdown_output_path}" && ! -L "${markdown_output_path}" ]] \
  || fail "Refusing to overwrite the Markdown plan."
[[ "${json_output_path}" != "${markdown_output_path}" ]] || fail "Plan outputs must be distinct."

if [[ -n "${game_export_user}" ]]; then
  command -v runuser >/dev/null || fail "Missing command: runuser"
  [[ "${game_export_user}" =~ ^[a-z_][a-z0-9_-]*$ ]] \
    || fail "POINT_MIGRATION_GAME_EXPORT_USER has an invalid format."
  id "${game_export_user}" >/dev/null 2>&1 \
    || fail "POINT_MIGRATION_GAME_EXPORT_USER does not exist."
fi

run_root="$(mktemp -d /tmp/ywonder-point-remediation-dry-run.XXXXXX)"
chmod 0700 "${run_root}"
web_snapshot_first="${run_root}/web.first.raw.json"
game_snapshot_first="${run_root}/game.first.raw.json"
web_snapshot_second="${run_root}/web.second.raw.json"
game_snapshot_second="${run_root}/game.second.raw.json"
json_plan="${run_root}/plan.json"
markdown_plan="${run_root}/plan.md"

export_game_snapshot() {
  if [[ -n "${game_export_user}" ]]; then
    runuser -u "${game_export_user}" --preserve-environment -- \
      env -u POINT_MIGRATION_REPORT_KEY \
      -u PGPASSWORD \
      USER="${game_export_user}" \
      LOGNAME="${game_export_user}" \
      PGUSER="${game_export_user}" \
      node "${script_root}/exportGamePointMigrationSnapshot.js"
  else
    node "${script_root}/exportGamePointMigrationSnapshot.js"
  fi
}

export POINT_MIGRATION_RAW_EXPORT_ACK=I_UNDERSTAND_THIS_OUTPUT_CONTAINS_RAW_WALLET_IDENTITIES
python3 "${script_root}/exportWebPointMigrationSnapshot.py" \
  --database "${web_database}" >"${web_snapshot_first}"
export_game_snapshot >"${game_snapshot_first}"
python3 "${script_root}/exportWebPointMigrationSnapshot.py" \
  --database "${web_database}" >"${web_snapshot_second}"
export_game_snapshot >"${game_snapshot_second}"
unset POINT_MIGRATION_RAW_EXPORT_ACK

cmp -s "${web_snapshot_first}" "${web_snapshot_second}" \
  || fail "Web Point ledger changed during the cross-database capture."
cmp -s "${game_snapshot_first}" "${game_snapshot_second}" \
  || fail "Game Point ledger changed during the cross-database capture."

web_sha256="$(sha256sum "${web_snapshot_second}" | awk '{print $1}')"
game_sha256="$(sha256sum "${game_snapshot_second}" | awk '{print $1}')"
generated_at="$(date -u +%Y-%m-%dT%H:%M:%S.000Z)"
common_args=(
  --approved "${approved_path}"
  --approved-sha256 "${approved_sha256}"
  --web-snapshot "${web_snapshot_second}"
  --web-sha256 "${web_sha256}"
  --game-snapshot "${game_snapshot_second}"
  --game-sha256 "${game_sha256}"
  --generated-at "${generated_at}"
)
node "${script_root}/pointWalletMigrationRemediationPlan.js" \
  "${common_args[@]}" --format json --output "${json_plan}"
node "${script_root}/pointWalletMigrationRemediationPlan.js" \
  "${common_args[@]}" --format markdown --output "${markdown_plan}"

install -m 0600 "${json_plan}" "${json_output_path}"
install -m 0600 "${markdown_plan}" "${markdown_output_path}"
echo "POINT_WALLET_REMEDIATION_DRY_RUN=success"
echo "DATABASE_MUTATIONS_PERFORMED=no"
echo "EXECUTION_STATEMENTS_GENERATED=0"
echo "RAW_SNAPSHOTS_RETAINED=no"
echo "JSON_PLAN_PATH=${json_output_path}"
echo "MARKDOWN_PLAN_PATH=${markdown_output_path}"
