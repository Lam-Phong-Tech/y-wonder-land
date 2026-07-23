# ADR: One Authoritative Point Wallet

Date: 2026-07-16
Status: Authority foundation deployed dormant; debit disabled and account linking/migration not approved

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
8. Rate, quote, fee, rounding and reconciliation arithmetic use integer micros and
   `BigInt`/database integers. The final compatibility write to the legacy web
   `Float` USDT column is derived once from pinned micros inside one SQLite
   transaction; retries never recompute the amount from floating-point values.
9. A game purchase, its Point debit and the exact allocation of that debit by Point
   source remain one PostgreSQL transaction.
10. Only a committed game purchase emits one durable consumption event keyed by the
    purchase transaction. Commission payout and any later reversal use that same key.
11. Commission currency is derived from the consumed Point source: USDT-funded Point
    produces USDT commission; Point earned in the game produces Point commission.
12. Point balance, debit, payout and reversal preserve fractional value with integer
    fixed-point arithmetic. The current whole-Point `pos` contract is transitional.
13. Source lots are consumed FIFO. USDT commission uses the immutable conversion-rate
    snapshot of each consumed lot, never the current Admin rate at spend or retry time.
14. Commission starts `PENDING` with a versioned hold policy of at least about ten
    minutes (`600` seconds candidate default), and pays only while the source purchase
    remains successful. A later refund still requires an idempotent reversal.
15. A Point transfer preserves and moves the sender's source lots and rate snapshots;
    it never reclassifies game-earned Point into a USDT-funded source.
16. The current official rate version is `26.5 Point/USDT` and `1.59 Point/YWH`.
    Transactions pin that version; a later Admin change never rewrites history.
17. Eligible consumption creates at most six commission shares: level 1 gets `8%`;
    levels 2 through 6 get `1%` each, for a maximum aggregate of `13%`.
18. VIP progress is lifetime-cumulative consumption of `2650 Point` from USDT-origin
    source lots. It does not require one purchase and must be derived idempotently from
    committed source-lot allocations, never from the current wallet balance. A transfer
    preserves origin, so transferred USDT-origin Point qualifies its receiver when spent.
19. A commission share records both its upstream recipient A and originating consumer
    B. It remains in an auditable `LOCKED_VIP` pool while either party is not VIP and
    becomes spendable/withdrawable only after both are VIP. Qualification releases all
    historical locked shares for that eligible A-B relationship.
20. A refund reverses its qualifying VIP progress idempotently. If the remaining progress
    falls below `2650 Point`, achieved VIP is revoked and eligibility is recalculated.

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

## Web Point-to-USDT Debit Saga Candidate

The local web overlay now implements the USDT target adapter as a durable saga. It
does not implement YWH conversion or an external USDT withdrawal/payment adapter.

```text
RESERVE_PENDING -> RESERVED -> CAPTURE_PENDING -> CAPTURED
                              -> RELEASE_PENDING -> RELEASED
                 any non-recoverable conflict -> MANUAL_REVIEW/REJECTED
```

- One browser intent creates one UUID and keeps it in local storage until a
  terminal response. The web journal derives deterministic reservation and
  transaction IDs from that UUID.
- `GamePointDebit` pins the linked player, whole Point amount, Admin rate version,
  gross/fee/net micros, both rounding remainders and request fingerprint.
- The web commits a `PENDING` USDT transaction journal before asking the game to
  capture. That pending value is not added to spendable `balanceUsdt` yet.
- After the game returns `CAPTURED`, one SQLite transaction credits spendable USDT,
  marks the web transaction `SUCCESS` and marks the debit `CAPTURED`. A lost
  response retries the same reservation and cannot credit twice.
- If the web cannot create its pending settlement, it releases the same game
  reservation. If any settlement journal already exists, release is forbidden and
  the saga continues toward capture or operator review.
- SQLite triggers and action checks allow only one unresolved wallet operation per
  user across both `USDT -> Point` and `Point -> USDT` directions.
- New debits require an explicit `WEB_POINT_DEBIT_FEE_BPS`; there is no inherited
  10% default. The adapter is further gated by `WEB_POINT_WALLET_DEBIT_ENABLED`,
  top-up mode and the canary allowlist.

Static safety, migration SQL, reservation/credit/security and Phase 1 regression
tests pass locally. The checksum-pinned overlay also passed full Prisma migration,
DB E2E, Next.js build and debit fault E2E against copied production source/SQLite.
The additive PostgreSQL/SQLite foundation is deployed in production with
`WEB_POINT_WALLET_DEBIT_ENABLED=false`; all new link/conversion/debit/reservation
tables remain empty. Account linking, legacy balance migration, no-money canary and
any debit/open activation remain separately approved gates.

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

### Read-only dry-run implementation

The candidate dry-run exports only the minimum Point fields from SQLite opened in
`mode=ro/query_only` and PostgreSQL under `REPEATABLE READ READ ONLY`. It captures
both ledgers twice and aborts if either relevant snapshot changes during the
cross-database window. Raw identities live only in a mode-`0700` temporary
directory; the retained report contains HMAC references, aggregate evidence and
classification reasons but no raw user/player/transaction IDs.

The report deliberately sets `automaticMigrationAllowed=false`, emits no SQL and
never suggests a migration amount. `READY_TO_LINK` only means that the captured
data has no unexplained difference; identity approval, rollback and a separate
write release are still required. This implementation has passed local isolated
tests but has not yet been run against production.

## Local Source-Lot Candidate

Migration candidate `007_point_source_ledger` and the JSON/PostgreSQL adapters add
an additive, dormant source-lot model. Each lot stores exact micro-Point amount,
source event/index, origin classification, remaining amount and immutable rate
snapshots. `USDT` and `YWH` lots pin both their conversion rate (`USDT_POINT` or
`YWH_POINT`) and the `USDT_POINT` rate used for USDT commission valuation;
Admin/legacy lots pin the commission valuation only, while gameplay lots carry
Point commission and no currency rate.

The local FIFO planner is fail-closed: it does not skip an older `UNATTRIBUTED`
lot to consume a newer classified lot. Transfer lineage preserves parent/root and
all source/rate fields, but the standalone persistence API rejects transfer children;
sender consumption and recipient creation must be implemented in one transaction.
The candidate does not backfill existing balances, change `player_economy.pos`, or
attach to current credit/shop/reservation routes. Migration `007` is not deployed.

## Business Gates

The customer answers are sufficient to keep building the shared-wallet foundation.
`YWH -> Point` updates the shared balance, mixed-source spending is FIFO, USDT
commission uses each lot's original rate, and P2P transfer preserves source/rate.
The current official rates are `26.5 Point/USDT` and `1.59 Point/YWH`; Admin/legacy
commission valuation also uses `26.5`. Commission pays `8%` at level 1 and `1%` at
levels 2-6. The customer's latest correction confirms that VIP progress is lifetime-
cumulative consumption of `2650 Point` from USDT-origin lots. Transferred USDT-origin
Point qualifies its receiver when spent; refund reverses progress and revokes VIP when
the remaining total falls below the threshold. A share is created for A even if A or the
originating consumer B is not VIP, remains locked until both are VIP, and then all
historical locked shares for that eligible relationship are released. Existing source/rate
lineage is sufficient for this VIP source rule; no original-funding-player field is needed.
The following operations remain feature-gated:
- Consequences of VIP revocation for other commission shares that were previously
  unlocked or paid, beyond reversing the refunded purchase's own commission.
- Commission finality after the minimum hold window and recovery when a paid
  commission is spent before a later refund. The hold reduces risk but cannot replace
  reversal unless refunds are forbidden after finalization.
- General fractional Point spending. Current top-up credit carries micros, but shop
  and reservation contracts still spend whole `pos` only.
- Point withdrawal fee/min/max/approval and external settlement/reconciliation.
- Legacy web quest, commission, investment, gift and staking actions until each is
  mapped to a confirmed asset and source classification.

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
