-- Y WONDER GREEN FARM - Game backend MVP schema (PostgreSQL target)
-- This is the production direction. The current dev stub still uses data.json.

create table if not exists game_players (
    id text primary key,
    web_user_id text not null unique,
    username text not null,
    display_name text not null,
    auth_source text not null default 'web',
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table if not exists player_profiles (
    player_id text primary key references game_players(id) on delete cascade,
    version integer not null default 1,
    name text not null default 'Player',
    gender text not null default 'male',
    avatar_id text not null default '',
    level integer not null default 1,
    exp real not null default 0,
    character_created boolean not null default false,
    tutorial_completed boolean not null default false,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table if not exists player_economy (
    player_id text primary key references game_players(id) on delete cascade,
    version integer not null default 1,
    pos bigint not null default 5000,
    upos bigint not null default 0,
    updated_at timestamptz not null default now()
);

create table if not exists player_inventory (
    player_id text not null references game_players(id) on delete cascade,
    item_id text not null,
    quantity integer not null default 0,
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

create table if not exists game_transactions (
    id text primary key,
    player_id text not null references game_players(id) on delete cascade,
    type text not null,
    ref text not null default '',
    idempotency_key text null unique,
    delta_pos bigint not null default 0,
    delta_upos bigint not null default 0,
    item_id text null,
    quantity_delta integer null,
    created_at timestamptz not null default now()
);

create index if not exists idx_game_players_web_user_id on game_players(web_user_id);
create index if not exists idx_player_inventory_player_id on player_inventory(player_id);
create index if not exists idx_game_transactions_player_id on game_transactions(player_id);
