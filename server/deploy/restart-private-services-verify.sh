#!/usr/bin/env bash
set -Eeuo pipefail

[[ ${EUID} -eq 0 ]] || {
  echo "This verification must run as root." >&2
  exit 77
}

for command_name in curl diff flock id pg_isready psql runuser sha256sum systemctl; do
  command -v "${command_name}" >/dev/null || {
    echo "Missing command: ${command_name}" >&2
    exit 69
  }
done

lock_file="/run/lock/ywonder-restart-verify.lock"
log_file="/var/lib/ywonder-game/restart-verify.log"
service_user="ywonder_game"
database_name="ywonder_game"
backend_service="ywonder-game-server.service"
backup_timer="ywonder-db-backup.timer"

install -d -o root -g "${service_user}" -m 0750 /var/lib/ywonder-game
exec 9>"${lock_file}"
flock -n 9 || {
  echo "Another Y Wonder restart verification is already running." >&2
  exit 75
}

before_file="$(mktemp /tmp/ywonder-p1-before.XXXXXX)"
after_file="$(mktemp /tmp/ywonder-p1-after.XXXXXX)"

finalize() {
  local exit_code=$?
  rm -f "${before_file}" "${after_file}"
  if id deploy >/dev/null 2>&1; then
    chown root:deploy "${log_file}" 2>/dev/null || true
  else
    chown root:"${service_user}" "${log_file}" 2>/dev/null || true
  fi
  chmod 0640 "${log_file}" 2>/dev/null || true
  exit "${exit_code}"
}
trap finalize EXIT

exec > >(tee "${log_file}") 2>&1

snapshot_p1_accounts() {
  runuser -u "${service_user}" -- \
    psql -X -v ON_ERROR_STOP=1 -At -F '|' -d "${database_name}" <<'SQL'
select
  lower(a.username),
  a.status,
  case when a.soft_deleted then 1 else 0 end,
  coalesce(p.level, 0),
  coalesce(p.exp, 0),
  coalesce(md5(p.profile_json::text), '-'),
  coalesce(e.pos, 0),
  coalesce(e.upos, 0),
  coalesce((select count(*) from player_inventory i where i.player_id = a.player_id), 0),
  coalesce((select sum(i.quantity) from player_inventory i where i.player_id = a.player_id), 0),
  md5(coalesce((
    select string_agg(
      i.item_id || ':' || i.quantity::text || ':' || i.slot_tab || ':' ||
      case when i.equipped then '1' else '0' end,
      ',' order by i.item_id
    )
    from player_inventory i
    where i.player_id = a.player_id
  ), '')),
  coalesce(md5(f.state_json::text), '-'),
  md5(coalesce((
    select string_agg(
      d.limit_key || ':' || d.period_key || ':' || d.used_count::text || ':' || d.max_count::text,
      ',' order by d.limit_key, d.period_key
    )
    from player_daily_limits d
    where d.player_id = a.player_id
  ), '')),
  coalesce((select count(*) from game_transactions t where t.player_id = a.player_id), 0)
from game_accounts a
left join player_profiles p on p.player_id = a.player_id
left join player_economy e on e.player_id = a.player_id
left join player_farm_state f on f.player_id = a.player_id
where lower(a.username) in ('p1a_h09433', 'p1b_h09433', 'p1race_h09433')
order by lower(a.username);
SQL
}

wait_for_postgres() {
  for _ in $(seq 1 60); do
    if runuser -u "${service_user}" -- \
      pg_isready -q -d "${database_name}"; then
      return 0
    fi
    sleep 1
  done
  return 1
}

wait_for_health() {
  local url="$1"
  for _ in $(seq 1 60); do
    if curl --fail --silent --show-error --max-time 2 "${url}" >/dev/null; then
      return 0
    fi
    sleep 1
  done
  return 1
}

echo "[restart-verify] started_at=$(date -u +%Y-%m-%dT%H:%M:%SZ)"
systemctl is-active --quiet postgresql.service
systemctl is-active --quiet "${backend_service}"
systemctl is-active --quiet caddy.service
systemctl is-active --quiet "${backup_timer}"

snapshot_p1_accounts >"${before_file}"
account_count="$(wc -l <"${before_file}")"
[[ "${account_count}" -eq 3 ]] || {
  echo "Expected 3 P1 accounts, found ${account_count}. Refusing to restart." >&2
  cat "${before_file}"
  exit 65
}

before_sha="$(sha256sum "${before_file}" | awk '{print $1}')"
echo "[restart-verify] before_sha256=${before_sha} accounts=${account_count}"
cat "${before_file}"

echo "[restart-verify] restarting PostgreSQL"
systemctl restart postgresql.service
wait_for_postgres || {
  echo "PostgreSQL did not become ready within 60 seconds." >&2
  exit 70
}

echo "[restart-verify] restarting backend"
systemctl restart "${backend_service}"
wait_for_health http://127.0.0.1:3000/health || {
  echo "Backend health did not recover within 60 seconds." >&2
  exit 70
}
wait_for_health http://127.0.0.1:8080/health || {
  echo "Caddy private health did not recover within 60 seconds." >&2
  exit 70
}

snapshot_p1_accounts >"${after_file}"
after_sha="$(sha256sum "${after_file}" | awk '{print $1}')"
echo "[restart-verify] after_sha256=${after_sha} accounts=$(wc -l <"${after_file}")"

if ! diff -u "${before_file}" "${after_file}"; then
  echo "P1 account data changed after service restart." >&2
  exit 74
fi

systemctl is-active --quiet postgresql.service
systemctl is-active --quiet "${backend_service}"
systemctl is-active --quiet caddy.service
systemctl is-active --quiet "${backup_timer}"
systemctl is-enabled --quiet postgresql.service
systemctl is-enabled --quiet "${backend_service}"
systemctl is-enabled --quiet caddy.service
systemctl is-enabled --quiet "${backup_timer}"

echo "[restart-verify] PASS: PostgreSQL/backend restarted and all P1 snapshots are unchanged."
echo "[restart-verify] completed_at=$(date -u +%Y-%m-%dT%H:%M:%SZ)"
