-- Y WONDER GREEN FARM - PostgreSQL schema snapshot.
-- Versioned migrations live in server/migrations and are the deployment source.

create table if not exists schema_migrations (
    version text primary key,
    applied_at timestamptz not null default now()
);

create table if not exists game_players (
    id text primary key,
    web_user_id text unique,
    username text not null,
    display_name text not null,
    auth_source text not null default 'web',
    active_session_id text,
    active_session_updated_at timestamptz,
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
    web_point_micros_remainder bigint not null default 0
        check (web_point_micros_remainder >= 0 and web_point_micros_remainder < 1000000),
    updated_at timestamptz not null default now()
);

-- Audit-only archive populated by migration 004 when upgrading legacy UPoint data.
-- It is never read as an active wallet and is not converted to Point automatically.
create table if not exists legacy_upoint_balances (
    player_id text primary key references game_players(id) on delete cascade,
    upoint_balance bigint not null,
    archived_at timestamptz not null default now()
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
create index if not exists idx_game_players_active_session_id
    on game_players(active_session_id)
    where active_session_id is not null;
create index if not exists idx_player_inventory_player_id on player_inventory(player_id);
create index if not exists idx_player_daily_limits_player_id on player_daily_limits(player_id);
create index if not exists idx_game_transactions_player_id on game_transactions(player_id);

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

-- Dormant source ledger. Existing balances are not backfilled automatically;
-- an approved reconciliation migration must create their opening lots later.
create table if not exists point_source_lots (
    id text primary key,
    player_id text not null references game_players(id) on delete cascade,
    source_event_id text not null,
    source_event_index integer not null default 0 check (source_event_index >= 0),
    origin_type text not null
        check (origin_type in ('USDT', 'YWH', 'GAMEPLAY', 'ADMIN', 'LEGACY', 'UNATTRIBUTED')),
    acquisition_type text not null
        check (acquisition_type in ('ORIGIN', 'TRANSFER', 'MIGRATION')),
    commission_asset text null
        check (commission_asset is null or commission_asset in ('USDT', 'POINT')),
    point_amount_micros bigint not null check (point_amount_micros > 0),
    remaining_point_micros bigint not null
        check (remaining_point_micros >= 0 and remaining_point_micros <= point_amount_micros),
    source_rate_pair text null,
    source_rate_version_id text null,
    point_micros_per_source_unit bigint null
        check (point_micros_per_source_unit is null or point_micros_per_source_unit > 0),
    commission_rate_version_id text null,
    point_micros_per_usdt bigint null
        check (point_micros_per_usdt is null or point_micros_per_usdt > 0),
    parent_lot_id text null,
    root_lot_id text not null,
    occurred_at timestamptz not null,
    request_signature text not null,
    metadata_json jsonb not null default '{}'::jsonb,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    constraint ux_point_source_lots_event
        unique (player_id, source_event_id, source_event_index),
    constraint ck_point_source_lots_lineage check (
        (acquisition_type = 'TRANSFER' and parent_lot_id is not null)
        or (acquisition_type in ('ORIGIN', 'MIGRATION') and parent_lot_id is null and root_lot_id = id)
    ),
    constraint ck_point_source_lots_origin_policy check (
        (
            origin_type in ('USDT', 'YWH', 'ADMIN', 'LEGACY')
            and commission_asset is not null
            and commission_asset = 'USDT'
            and commission_rate_version_id is not null
            and commission_rate_version_id <> ''
            and point_micros_per_usdt is not null
            and (
                (
                    origin_type = 'USDT'
                    and source_rate_pair is not null
                    and source_rate_pair = 'USDT_POINT'
                    and source_rate_version_id is not null
                    and source_rate_version_id <> ''
                    and point_micros_per_source_unit is not null
                )
                or (
                    origin_type = 'YWH'
                    and source_rate_pair is not null
                    and source_rate_pair = 'YWH_POINT'
                    and source_rate_version_id is not null
                    and source_rate_version_id <> ''
                    and point_micros_per_source_unit is not null
                )
                or (
                    origin_type in ('ADMIN', 'LEGACY')
                    and source_rate_pair is null
                    and source_rate_version_id is null
                    and point_micros_per_source_unit is null
                )
            )
        )
        or (
            origin_type = 'GAMEPLAY'
            and commission_asset is not null
            and commission_asset = 'POINT'
            and source_rate_pair is null
            and source_rate_version_id is null
            and point_micros_per_source_unit is null
            and commission_rate_version_id is null
            and point_micros_per_usdt is null
        )
        or (
            origin_type = 'UNATTRIBUTED'
            and commission_asset is null
            and source_rate_pair is null
            and source_rate_version_id is null
            and point_micros_per_source_unit is null
            and commission_rate_version_id is null
            and point_micros_per_usdt is null
        )
    ),
    constraint ck_point_source_lots_initial_remaining check (
        acquisition_type = 'MIGRATION' or remaining_point_micros = point_amount_micros
    )
);

create index if not exists idx_point_source_lots_player_fifo
    on point_source_lots (player_id, occurred_at, created_at, id)
    where remaining_point_micros > 0;
create index if not exists idx_point_source_lots_root
    on point_source_lots (root_lot_id, created_at);
create index if not exists idx_point_source_lots_parent
    on point_source_lots (parent_lot_id)
    where parent_lot_id is not null;

create table if not exists browser_auth_requests (
    request_id_hash char(64) primary key,
    pkce_challenge varchar(128) not null,
    intent varchar(16) not null default 'login'
        check (intent in ('login', 'register')),
    status varchar(16) not null default 'pending'
        check (status in ('pending', 'approved', 'consumed', 'expired')),
    web_user_id text null,
    web_user_json jsonb not null default '{}'::jsonb,
    expires_at timestamptz not null,
    approved_at timestamptz null,
    consumed_at timestamptz null,
    created_at timestamptz not null default now()
);

create index if not exists idx_browser_auth_requests_expires_at
    on browser_auth_requests(expires_at);
create index if not exists idx_browser_auth_requests_status
    on browser_auth_requests(status);
