-- Link selected web accounts to the game-owned Point ledger without changing
-- legacy web Point balances for any existing user.
create table "GamePointLinkedAccount" (
    "userId" text not null primary key,
    "gamePlayerId" text not null,
    "linkedBy" text not null,
    "note" text,
    "linkedAt" datetime not null default current_timestamp,
    constraint "GamePointLinkedAccount_userId_fkey"
        foreign key ("userId") references "User" ("id") on delete cascade on update cascade
);

create unique index "GamePointLinkedAccount_gamePlayerId_key"
    on "GamePointLinkedAccount"("gamePlayerId");

-- Immutable Admin-controlled rate versions. Settlement stores the selected
-- version and exact micro-rate; retries never read a newer rate.
create table "PointExchangeRateVersion" (
    "id" text not null primary key,
    "pair" text not null,
    "rateMicros" text not null,
    "isActive" integer not null default 1,
    "effectiveAt" datetime not null default current_timestamp,
    "createdBy" text not null,
    "sourceRateId" text,
    "createdAt" datetime not null default current_timestamp
);

create index "PointExchangeRateVersion_pair_effectiveAt_idx"
    on "PointExchangeRateVersion"("pair", "effectiveAt");
create unique index "PointExchangeRateVersion_one_active_pair"
    on "PointExchangeRateVersion"("pair") where "isActive" = 1;

insert into "PointExchangeRateVersion"
    ("id", "pair", "rateMicros", "isActive", "effectiveAt", "createdBy", "sourceRateId", "createdAt")
select
    'point_rate_legacy_' || "id",
    'USDT_POINT',
    printf('%.0f', round("rate" * 1000000)),
    1,
    "updatedAt",
    'migration',
    "id",
    current_timestamp
from "ExchangeRate"
where "fromCurrency" = 'USDT' and "toCurrency" = 'GXL' and "isActive" = 1
order by "updatedAt" desc
limit 1;

-- Exact conversion journal. Amounts are decimal micros encoded as text so the
-- bridge never relies on SQLite floating-point values for reconciliation.
create table "GamePointConversion" (
    "id" text not null primary key,
    "requestId" text not null,
    "sourceTransactionId" text not null,
    "outboxId" text not null,
    "userId" text not null,
    "usdtMicros" text not null,
    "pointMicros" text not null,
    "rateVersionId" text not null,
    "rateMicros" text not null,
    "roundingRemainder" text not null default '0',
    "status" text not null default 'PENDING',
    "lastError" text,
    "sentAt" datetime,
    "createdAt" datetime not null default current_timestamp,
    "updatedAt" datetime not null,
    constraint "GamePointConversion_userId_fkey"
        foreign key ("userId") references "User" ("id") on delete cascade on update cascade,
    constraint "GamePointConversion_rateVersionId_fkey"
        foreign key ("rateVersionId") references "PointExchangeRateVersion" ("id") on delete restrict on update cascade
);

create unique index "GamePointConversion_requestId_key"
    on "GamePointConversion"("requestId");
create unique index "GamePointConversion_sourceTransactionId_key"
    on "GamePointConversion"("sourceTransactionId");
create unique index "GamePointConversion_outboxId_key"
    on "GamePointConversion"("outboxId");
create index "GamePointConversion_userId_createdAt_idx"
    on "GamePointConversion"("userId", "createdAt");
create index "GamePointConversion_status_createdAt_idx"
    on "GamePointConversion"("status", "createdAt");

-- A user cannot reserve another conversion while an earlier one still needs
-- delivery or operator reconciliation.
create unique index "GamePointConversion_one_unresolved_per_user"
    on "GamePointConversion"("userId")
    where "status" not in ('SENT', 'REFUNDED');

-- Point debit is a durable saga journal. The game reservation is authoritative;
-- web settlement happens once before capture, while failed settlement releases
-- the same reservation. Unknown terminal conflicts remain blocked for review.
create table "GamePointDebit" (
    "id" text not null primary key,
    "requestId" text not null,
    "reservationId" text not null,
    "sourceTransactionId" text not null,
    "userId" text not null,
    "gamePlayerId" text not null,
    "targetCurrency" text not null,
    "pointAmount" integer not null check ("pointAmount" > 0),
    "pointMicros" text not null,
    "grossTargetMicros" text not null,
    "feeBps" integer not null check ("feeBps" >= 0 and "feeBps" < 10000),
    "feeMicros" text not null,
    "netTargetMicros" text not null,
    "rateVersionId" text not null,
    "rateMicros" text not null,
    "roundingRemainder" text not null default '0',
    "feeRoundingRemainder" text not null default '0',
    "requestFingerprint" text not null,
    "purpose" text not null,
    "source" text not null,
    "occurredAt" datetime not null,
    "status" text not null default 'RESERVE_PENDING'
        check ("status" in (
            'RESERVE_PENDING','RESERVED','CAPTURE_PENDING','CAPTURED',
            'RELEASE_PENDING','RELEASED','REJECTED','MANUAL_REVIEW'
        )),
    "attempts" integer not null default 0,
    "lastError" text,
    "nextAttemptAt" datetime not null default current_timestamp,
    "reservedAt" datetime,
    "settledAt" datetime,
    "capturedAt" datetime,
    "releasedAt" datetime,
    "createdAt" datetime not null default current_timestamp,
    "updatedAt" datetime not null,
    constraint "GamePointDebit_userId_fkey"
        foreign key ("userId") references "User" ("id") on delete cascade on update cascade,
    constraint "GamePointDebit_rateVersionId_fkey"
        foreign key ("rateVersionId") references "PointExchangeRateVersion" ("id") on delete restrict on update cascade
);

create unique index "GamePointDebit_requestId_key" on "GamePointDebit"("requestId");
create unique index "GamePointDebit_reservationId_key" on "GamePointDebit"("reservationId");
create unique index "GamePointDebit_sourceTransactionId_key"
    on "GamePointDebit"("sourceTransactionId");
create index "GamePointDebit_userId_createdAt_idx"
    on "GamePointDebit"("userId", "createdAt");
create index "GamePointDebit_status_nextAttemptAt_idx"
    on "GamePointDebit"("status", "nextAttemptAt");
create unique index "GamePointDebit_one_unresolved_per_user"
    on "GamePointDebit"("userId")
    where "status" not in ('CAPTURED', 'RELEASED', 'REJECTED');

-- A linked wallet may have only one unresolved conversion direction. The
-- application checks this first for stable errors; triggers close tab/race gaps.
create trigger "GamePointConversion_block_active_debit_insert"
before insert on "GamePointConversion"
when exists (
    select 1 from "GamePointDebit" debit
    where debit."userId" = new."userId"
      and debit."status" not in ('CAPTURED', 'RELEASED', 'REJECTED')
)
begin
    select raise(abort, 'GAME_POINT_WALLET_OPERATION_ALREADY_PENDING');
end;

create trigger "GamePointConversion_block_active_debit_update"
before update of "status" on "GamePointConversion"
when new."status" not in ('SENT', 'REFUNDED')
and exists (
    select 1 from "GamePointDebit" debit
    where debit."userId" = new."userId"
      and debit."status" not in ('CAPTURED', 'RELEASED', 'REJECTED')
)
begin
    select raise(abort, 'GAME_POINT_WALLET_OPERATION_ALREADY_PENDING');
end;

create trigger "GamePointDebit_block_active_conversion_insert"
before insert on "GamePointDebit"
when exists (
    select 1 from "GamePointConversion" conversion
    where conversion."userId" = new."userId"
      and conversion."status" not in ('SENT', 'REFUNDED')
)
begin
    select raise(abort, 'GAME_POINT_WALLET_OPERATION_ALREADY_PENDING');
end;

create trigger "GamePointDebit_block_active_conversion_update"
before update of "status" on "GamePointDebit"
when new."status" not in ('CAPTURED', 'RELEASED', 'REJECTED')
and exists (
    select 1 from "GamePointConversion" conversion
    where conversion."userId" = new."userId"
      and conversion."status" not in ('SENT', 'REFUNDED')
)
begin
    select raise(abort, 'GAME_POINT_WALLET_OPERATION_ALREADY_PENDING');
end;

create trigger "GamePointLinkedAccount_require_zero_wallet"
before insert on "GamePointLinkedAccount"
when not exists (
    select 1 from "Wallet"
    where "userId" = new."userId"
      and abs(coalesce("balanceGXL", 0)) <= 0.000000001
      and abs(coalesce("lockedGXL", 0)) <= 0.000000001
)
begin
    select raise(abort, 'GAME_POINT_LINK_REQUIRES_ZERO_LEGACY_BALANCE');
end;

create trigger "Wallet_freeze_linked_point_update"
before update of "balanceGXL", "lockedGXL" on "Wallet"
when exists (
    select 1 from "GamePointLinkedAccount" linked
    where linked."userId" = old."userId"
)
and (
    abs(coalesce(new."balanceGXL", 0) - coalesce(old."balanceGXL", 0)) > 0.000000001
    or abs(coalesce(new."lockedGXL", 0) - coalesce(old."lockedGXL", 0)) > 0.000000001
)
begin
    select raise(abort, 'GAME_POINT_LEDGER_IS_AUTHORITATIVE');
end;

create trigger "Wallet_require_zero_point_for_linked_insert"
before insert on "Wallet"
when exists (
    select 1 from "GamePointLinkedAccount" linked
    where linked."userId" = new."userId"
)
and (
    abs(coalesce(new."balanceGXL", 0)) > 0.000000001
    or abs(coalesce(new."lockedGXL", 0)) > 0.000000001
)
begin
    select raise(abort, 'GAME_POINT_LEDGER_IS_AUTHORITATIVE');
end;
