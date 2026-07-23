create table if not exists legacy_upoint_balances (
    player_id text primary key references game_players(id) on delete cascade,
    upoint_balance bigint not null,
    archived_at timestamptz not null default now()
);

do $$
begin
    if exists (
        select 1
        from information_schema.columns
        where table_schema = current_schema()
          and table_name = 'player_economy'
          and column_name = 'upos'
    ) then
        execute $archive$
            insert into legacy_upoint_balances (player_id, upoint_balance, archived_at)
            select player_id, upos, now()
            from player_economy
            where upos <> 0
            on conflict (player_id) do update set
                upoint_balance = excluded.upoint_balance,
                archived_at = excluded.archived_at
        $archive$;
    end if;
end $$;

do $$
begin
    if exists (
        select 1
        from information_schema.columns
        where table_schema = current_schema()
          and table_name = 'game_transactions'
          and column_name = 'delta_upos'
    ) then
        execute $archive$
            update game_transactions
            set details_json = jsonb_set(
                coalesce(details_json, '{}'::jsonb),
                '{legacyDeltaUpos}',
                to_jsonb(delta_upos),
                true
            )
            where delta_upos <> 0
        $archive$;
    end if;
end $$;

-- Expand/contract rollout safety:
-- This release archives legacy values but deliberately keeps the old columns.
-- The running release before this migration still reads those columns because
-- deploy-private-release.sh migrates before switching the current symlink.
-- Drop them only in a later migration, after the Point-only release has been
-- deployed and verified as the rollback target.
