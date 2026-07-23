-- Durable outbox for web USDT -> Point conversions sent to the game backend.
create table "GamePointSyncOutbox" (
    "id" text not null primary key,
    "sourceTransactionId" text not null,
    "userId" text not null,
    "pointAmount" text not null,
    "occurredAt" datetime not null,
    "source" text not null default 'ywonder-web-usdt-to-point',
    "status" text not null default 'PENDING',
    "attempts" integer not null default 0,
    "lastError" text,
    "nextAttemptAt" datetime not null default current_timestamp,
    "sentAt" datetime,
    "createdAt" datetime not null default current_timestamp,
    "updatedAt" datetime not null,
    constraint "GamePointSyncOutbox_userId_fkey"
        foreign key ("userId") references "User" ("id") on delete cascade on update cascade
);

create unique index "GamePointSyncOutbox_sourceTransactionId_key"
    on "GamePointSyncOutbox"("sourceTransactionId");
create index "GamePointSyncOutbox_status_nextAttemptAt_idx"
    on "GamePointSyncOutbox"("status", "nextAttemptAt");
create index "GamePointSyncOutbox_userId_createdAt_idx"
    on "GamePointSyncOutbox"("userId", "createdAt");
