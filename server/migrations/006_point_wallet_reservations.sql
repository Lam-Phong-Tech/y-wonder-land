create table if not exists point_wallet_reservations (
    id text primary key,
    player_id text not null references game_players(id) on delete cascade,
    web_user_id text not null,
    expected_player_id text not null,
    point_amount bigint not null check (point_amount > 0),
    purpose text not null,
    source text not null,
    occurred_at timestamptz not null,
    request_signature text not null,
    status text not null default 'RESERVED'
        check (status in ('RESERVED', 'CAPTURED', 'RELEASED')),
    captured_at timestamptz null,
    released_at timestamptz null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists idx_point_wallet_reservations_player
    on point_wallet_reservations (player_id, created_at);

create index if not exists idx_point_wallet_reservations_status
    on point_wallet_reservations (status, updated_at);
