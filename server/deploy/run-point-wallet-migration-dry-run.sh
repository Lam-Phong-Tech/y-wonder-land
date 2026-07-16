#!/usr/bin/env bash
set -Eeuo pipefail
umask 077

[[ $# -eq 2 ]] || {
  echo "Usage: $0 <web-sqlite-database> <anonymized-report.json>" >&2
  exit 64
}

script_root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
web_database="$1"
report_path="$2"
run_root=""
report_key="${POINT_MIGRATION_REPORT_KEY:-}"

fail() {
  echo "[point-wallet-migration-dry-run] $*" >&2
  exit 1
}

cleanup() {
  local exit_code=$?
  trap - EXIT INT TERM
  if [[ -n "${run_root}" && "${run_root}" == /tmp/ywonder-point-migration-dry-run.* ]]; then
    rm -rf -- "${run_root}"
  elif [[ -n "${run_root}" ]]; then
    echo "Unsafe migration dry-run cleanup path refused: ${run_root}" >&2
    exit_code=74
  fi
  exit "${exit_code}"
}
trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM

for command_name in chmod cmp dirname install mktemp node python3 rm; do
  command -v "${command_name}" >/dev/null || fail "Missing command: ${command_name}"
done
[[ -f "${web_database}" ]] || fail "Web SQLite database is missing."
[[ -n "${DATABASE_URL:-}" ]] || fail "DATABASE_URL is required for the game PostgreSQL read."
[[ ${#report_key} -ge 32 ]] \
  || fail "POINT_MIGRATION_REPORT_KEY must contain at least 32 characters."
[[ ! -e "${report_path}" ]] || fail "Refusing to overwrite an existing report."
[[ ! -L "${report_path}" ]] || fail "Refusing to write the report through a symbolic link."

run_root="$(mktemp -d /tmp/ywonder-point-migration-dry-run.XXXXXX)"
chmod 0700 "${run_root}"
web_snapshot_first="${run_root}/web.first.raw.json"
game_snapshot_first="${run_root}/game.first.raw.json"
web_snapshot_second="${run_root}/web.second.raw.json"
game_snapshot_second="${run_root}/game.second.raw.json"
anonymized_report="${run_root}/report.json"

export POINT_MIGRATION_RAW_EXPORT_ACK=I_UNDERSTAND_THIS_OUTPUT_CONTAINS_RAW_WALLET_IDENTITIES
python3 "${script_root}/exportWebPointMigrationSnapshot.py" \
  --database "${web_database}" >"${web_snapshot_first}"
node "${script_root}/exportGamePointMigrationSnapshot.js" >"${game_snapshot_first}"
python3 "${script_root}/exportWebPointMigrationSnapshot.py" \
  --database "${web_database}" >"${web_snapshot_second}"
node "${script_root}/exportGamePointMigrationSnapshot.js" >"${game_snapshot_second}"
unset POINT_MIGRATION_RAW_EXPORT_ACK

cmp -s "${web_snapshot_first}" "${web_snapshot_second}" \
  || fail "Web Point ledger changed during the cross-database capture."
cmp -s "${game_snapshot_first}" "${game_snapshot_second}" \
  || fail "Game Point ledger changed during the cross-database capture."

node "${script_root}/pointWalletMigrationReport.js" \
  --web-snapshot "${web_snapshot_second}" \
  --game-snapshot "${game_snapshot_second}" >"${anonymized_report}"

install -m 0600 "${anonymized_report}" "${report_path}"
echo "POINT_WALLET_MIGRATION_DRY_RUN=success"
echo "DATABASE_MUTATIONS_PERFORMED=no"
echo "RAW_SNAPSHOTS_RETAINED=no"
echo "REPORT_PATH=${report_path}"
