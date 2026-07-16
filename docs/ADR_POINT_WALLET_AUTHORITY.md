# ADR: One Authoritative Point Wallet

Date: 2026-07-16
Status: Candidate accepted for isolated implementation; not approved for production rollout

## Context

The customer confirmed that Point on the web and Point in the game are the same
currency and must display one balance. The current production implementation does
not satisfy that rule:

- The web stores spendable Point in SQLite `Wallet.balanceGXL`.
- The game stores spendable Point in PostgreSQL `player_economy.pos`.
- `USDT -> Point` currently increments the web balance and independently queues a
  positive credit to the game.
- Game purchases debit only the game balance, so the web balance does not follow.
- The Admin rate table currently contains an active `USDT -> GXL` rate of `26.5`,
  while conversion code still uses the fixed constant `1 Point = 0.06 USDT`.

Read-only production inventory on 2026-07-16 found:

- 159 web wallets; 10 have non-zero Point, totalling `3422.666667` Point.
- 31 game economies; 6 are mapped to web identities.
- All 6 mapped balances differ between web and game.
- Of the mapped accounts, one has 12 web Point from legacy commission; one has
  three settled one-Point web outbox credits that also exist in the game ledger.
- Nine non-mapped web wallets hold `3410.666667` legacy Point in total.

Therefore, copying or adding one database balance into the other would either
duplicate value or destroy legitimate history.

## Decision

`player_economy.pos` in the game PostgreSQL database is the sole spendable Point
balance for every linked web/game account.

Reasons:

1. Game purchases must debit Point and grant inventory/farm assets atomically.
2. PostgreSQL already provides row locks, idempotent game transactions and the
   authoritative inventory/farm state needed for that transaction boundary.
3. Making SQLite authoritative would require every game purchase to perform a
   distributed reserve/capture protocol before it could grant an asset.
4. A third wallet service would be a valid long-term extraction, but migrating both
   current systems to it adds risk without improving the MVP transaction boundary.

For a linked account, web `balanceGXL` is frozen at zero and is not spendable. The
web obtains Point from a signed loopback-only game balance endpoint. The web UI and
game HUD are two views of the same PostgreSQL balance.

Unlinked accounts remain on the legacy web ledger until they pass an explicit
reconciliation and linking process. There is no automatic balance addition.

## Required Invariants

1. Exactly one mutable Point balance exists for a linked identity.
2. Every wallet command has an immutable transaction ID and request fingerprint.
3. A retry with the same ID and same payload returns the original result.
4. The same ID with a different payload is rejected as an idempotency conflict.
5. Unity never sends an arbitrary positive Point delta and never holds a wallet
   shared secret.
6. Web-to-game calls are HMAC signed, timestamp bounded, loopback-only and pinned to
   the already linked `webUserId -> playerId` pair.
7. No conversion recomputes a rate after commit. Each transaction stores an exact,
   immutable rate-version snapshot.
8. Point arithmetic uses integer micros and `BigInt`/database integers. New wallet
   code must not use floating-point arithmetic for value settlement.
9. A game purchase and its Point debit remain one PostgreSQL transaction.
10. A successful game purchase emits one durable consumption event keyed by that
    purchase transaction. Future YWH commission uses the same key and reversal key.

## Internal Wallet Contract

All endpoints remain private and unavailable when the wallet feature is dormant.

| Operation | Purpose | Atomic effect in game PostgreSQL |
|---|---|---|
| `balance` | Web displays the shared Point balance | Read linked player and economy |
| `credit` | Settled USDT/YWH conversion creates Point | Add Point/remainder and ledger row |
| `reserve` | Start Point -> USDT/YWH or another web-side spend | Subtract available Point and create reservation |
| `capture` | Web-side value was committed | Mark reservation captured; no second debit |
| `release` | Web-side operation failed/cancelled | Restore Point once and mark reservation released |
| `transfer` | Optional Point P2P transfer | Debit sender and credit receiver in one DB transaction |

`reserve/capture/release` is a state machine, not three unrelated deltas:

```text
NONE -> RESERVED -> CAPTURED
                 -> RELEASED
```

`CAPTURED` and `RELEASED` are terminal. Repeating the matching terminal command is
idempotent. Crossing terminal states is a conflict.

## Dynamic Rate Contract

Admin changes create a new immutable rate version; they do not update the rate used
by an existing transaction. A version contains:

- pair (`USDT_POINT`, `YWH_POINT`);
- integer numerator/denominator or decimal micros text;
- effective timestamp and enabled state;
- Admin actor and audit before/after;
- rounding rule and optional fee rule.

The conversion journal stores `rateVersionId`, source micros, destination micros,
fee micros and rounding remainder. A retry loads this journal before reading the
current rate.

The current `ExchangeRate` row can seed the first version, but the fixed constants
in `lib/tokens.ts` cannot remain settlement authority.

## Legacy Reconciliation

Before linking an existing account, generate a per-account report from both ledgers
and classify every value as one of:

- already represented in game;
- legacy web-only value approved for one-time migration;
- game seed/demo/QA value that is not customer money;
- unsupported legacy reward that must be converted to the correct asset;
- disputed, requiring manual approval.

The migration command records one immutable migration transaction and then freezes
the web Point columns. It never performs `gamePoint += webBalance` without an
approved reconciliation record.

## Business Gates

The customer answers are sufficient to build the shared-wallet foundation, but the
following operations stay feature-gated until their conflicting rules are resolved:

- `YWH -> Point`: allowed in one answer but also listed as not changing game Point.
- Point P2P transfer: a real transfer must change the shared sender/receiver balance.
- Legacy web quest, commission, investment, gift and staking actions: they currently
  mutate `balanceGXL`, while the customer said they must not change game Point.
- Point withdrawal fee/min/max/approval and YWH commission rates/levels/reversals.

For linked users these legacy Point mutation paths must fail closed, not silently
write a second balance.

## Rollout Gates

1. Isolated schema and unit tests.
2. PostgreSQL transaction tests for credit/reserve/capture/release and races.
3. Web build against a copied SQLite database and temporary game schema.
4. No-money end-to-end tests including timeout after commit, duplicate delivery,
   rate change during retry and process restart.
5. Per-account migration dry-run with zero unexplained differences.
6. Dedicated QA identity acceptance on EXE and APK.
7. Separate approval for a minimal real-money canary.

Until all gates pass, public wallet callbacks remain `404`, real-money conversion is
disabled, and no production balance is migrated.
