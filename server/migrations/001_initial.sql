-- Y WONDER GREEN FARM - initial PostgreSQL schema.

create table if not exists game_players (
    id text primary key,
    web_user_id text unique,
    username text not null,
    display_name text not null,
    auth_source text not null default 'web',
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table if not exists game_accounts (
    id text primary key,
    player_id text not null unique references game_players(id) on delete cascade,
    username text not null,
    email text not null default '',
    phone text not null default '',
    password_hash text not null,
    status text not null default 'active',
    soft_deleted boolean not null default false,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create unique index if not exists ux_game_accounts_username_ci
    on game_accounts (lower(username));
create unique index if not exists ux_game_accounts_email_ci
    on game_accounts (lower(email)) where email <> '';

create table if not exists player_profiles (
    player_id text primary key references game_players(id) on delete cascade,
    version integer not null default 1,
    name text not null default 'Player',
    gender text not null default 'male',
    avatar_id text not null default '',
    level integer not null default 1,
    exp double precision not null default 0,
    character_created boolean not null default false,
    tutorial_completed boolean not null default false,
    profile_json jsonb not null default '{}'::jsonb,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table if not exists player_economy (
    player_id text primary key references game_players(id) on delete cascade,
    version integer not null default 1,
    pos bigint not null default 5000 check (pos >= 0),
    upos bigint not null default 0 check (upos >= 0),
    updated_at timestamptz not null default now()
);

create table if not exists player_inventory_meta (
    player_id text primary key references game_players(id) on delete cascade,
    version integer not null default 1,
    max_slots integer not null default 50 check (max_slots >= 0),
    updated_at timestamptz not null default now()
);

create table if not exists player_inventory (
    player_id text not null references game_players(id) on delete cascade,
    item_id text not null,
    quantity integer not null default 0 check (quantity >= 0),
    slot_tab text not null default '',
    equipped boolean not null default false,
    durability integer null,
    updated_at timestamptz not null default now(),
    primary key (player_id, item_id)
);

create table if not exists player_farm_state (
    player_id text primary key references game_players(id) on delete cascade,
    version integer not null default 1,
    state_json jsonb not null default '{}'::jsonb,
    updated_at timestamptz not null default now()
);

create table if not exists player_daily_limits (
    player_id text not null references game_players(id) on delete cascade,
    limit_key text not null,
    period_key text not null,
    used_count integer not null default 0 check (used_count >= 0),
    max_count integer not null default 10 check (max_count > 0),
    version integer not null default 1,
    updated_at timestamptz not null default now(),
    primary key (player_id, limit_key, period_key)
);

create table if not exists game_transactions (
    id text primary key,
    player_id text not null references game_players(id) on delete cascade,
    type text not null,
    ref text not null default '',
    idempotency_key text null,
    request_signature text not null default '',
    delta_pos bigint not null default 0,
    delta_upos bigint not null default 0,
    item_id text null,
    quantity_delta integer null,
    details_json jsonb not null default '{}'::jsonb,
    result_json jsonb not null default '{}'::jsonb,
    created_at timestamptz not null default now()
);

create unique index if not exists ux_game_transactions_idempotency
    on game_transactions (idempotency_key)
    where idempotency_key is not null and idempotency_key <> '';
create index if not exists idx_game_players_web_user_id on game_players(web_user_id);
create index if not exists idx_player_inventory_player_id on player_inventory(player_id);
create index if not exists idx_player_daily_limits_player_id on player_daily_limits(player_id);
create index if not exists idx_game_transactions_player_id on game_transactions(player_id);
