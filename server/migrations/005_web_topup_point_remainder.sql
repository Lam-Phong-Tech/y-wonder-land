alter table player_economy
    add column if not exists web_point_micros_remainder bigint not null default 0;

do $$
begin
    if not exists (
        select 1
        from pg_constraint
        where connamespace = current_schema()::regnamespace
          and conname = 'ck_player_economy_web_point_micros_remainder'
    ) then
        alter table player_economy
            add constraint ck_player_economy_web_point_micros_remainder
            check (
                web_point_micros_remainder >= 0
                and web_point_micros_remainder < 1000000
            );
    end if;
end $$;
