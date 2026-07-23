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
