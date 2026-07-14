-- Keep exactly one active authenticated game session per player.

alter table game_players
    add column if not exists active_session_id text;

alter table game_players
    add column if not exists active_session_updated_at timestamptz;

create index if not exists idx_game_players_active_session_id
    on game_players(active_session_id)
    where active_session_id is not null;
