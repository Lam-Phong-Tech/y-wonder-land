# Web Wallet -> Game Point: No-Money Canary

Date: 2026-07-16

> **HOLD - không chạy playbook này ở production.** Sau khi tài liệu được viết, BA/khách
> xác nhận Point web và game là một ví, `USDT -> Point`, `YWH <-> Point`,
> `Point -> USDT` và tỷ giá do Admin thay đổi. Giả định cố định `0.06 USDT -> 1 Point`
> bên dưới không còn là contract chính thức. Identity canary hiện tại cũng được xác
> nhận là tài khoản thật `Nhien345`, không phải QA. Chỉ mở lại bài test sau khi có
> tài khoản QA riêng, rate version được chụp/audit và thiết kế ví chung tại
> `docs/POINT_WALLET_BUSINESS_RULES.md` đã được chuyển thành API contract nhất quán.

## Purpose

Prove the complete software path without paying a bank, blockchain network, or payment provider:

```text
audited synthetic USDT funding
  -> real web conversion action
  -> web Transaction + GamePointConversion + outbox
  -> authenticated cron
  -> loopback HMAC callback v2
  -> PostgreSQL game ledger
  -> realtime/relogin EXE and APK Point HUD
```

This test may be classified as **technical no-money canary complete** when every gate below passes. It must not be classified as a real-money canary because provider collection, settlement, callback authenticity, chargeback, and refund behavior are outside this test.

## Preconditions

- Use one dedicated QA web account only. Do not use a customer account.
- Pin exactly one `User.id -> gamePlayerId` row. Both IDs must be unique.
- The signed game balance response must return that pinned `gamePlayerId` before linking.
- Legacy web `balanceGXL` and `lockedGXL` must both be zero before linking.
- There must be no unresolved `GamePointConversion` for the QA user.
- Keep `WEB_TOPUP_MODE=canary`, one identical allowlisted web user at both ends, loopback-only callback, public callback `404`, and the scoped positive-grant block on the same user.
- Take root-only backups of the active web release, SQLite database, service configuration, environment checksums, and game release before any production mutation.
- Record baseline web USDT, web legacy Point, game Point, micro remainder, conversion/outbox counts, and game ledger count.

## Test Transaction

Use a new immutable synthetic funding ID and one browser conversion request ID:

- Synthetic funding: exactly `+0.06 USDT` to the QA web wallet.
- Funding journal: `currency=USDT`, `status=SUCCESS`, metadata includes `synthetic=true`, operator, reason, and test run ID.
- Block withdrawal for the QA identity during the test.
- Conversion: use the real logged-in web wallet UI to convert exactly `0.06 USDT` to `1 Point`.
- Expected client change for the current QA baseline: `5003 -> 5004`.

Do not insert an outbox row directly. Direct insertion bypasses the web conversion action and cannot prove that web wallet debit, durable browser request ID, journal creation, and dispatcher are connected correctly.

## Gate Loop

For every failed gate: preserve the same conversion request ID and source transaction ID, disable dispatch if needed, collect evidence, fix in an isolated candidate, rerun automated tests, then retry the same immutable transaction. Never create a new ID to hide an unresolved transaction.

| Gate | Action | Expected evidence |
|---|---|---|
| 1. Baseline | Read both databases and service state | One web user, one pinned player, no unresolved conversion, Point `5003`, web USDT baseline recorded |
| 2. Synthetic funding | Atomically journal and add `0.06 USDT` | One synthetic USDT transaction; no Point/outbox/game-ledger change |
| 3. Real web action | User converts `0.06 USDT` from the wallet UI | Web USDT returns to baseline; web GXL stays `0`; one pending SWAP, conversion, and outbox share one source ID |
| 4. Delivery | Run the authenticated cron | Outbox and conversion become `SENT`; web SWAP becomes `SUCCESS`; exactly one game ledger row uses the same source ID |
| 5. Online client | Keep EXE or APK online during delivery | HUD receives absolute Point `5004` without relogin |
| 6. Persistence | Logout/login, full app restart, EXE -> APK -> EXE | Every client restores `5004` from PostgreSQL |
| 7. Idempotency | Retry the same browser request and source transaction | No second USDT debit, outbox, game ledger, or Point credit |
| 8. Response loss | Drop the success response after game commit, then retry | Web recovers `RETRY -> SENT`; game remains one ledger and Point `5004` |
| 9. Identity rejection | Send a correctly signed v2 payload with another `expected_player_id` in isolation | `409 GAME_POINT_IDENTITY_MISMATCH`; no balance or ledger change |
| 10. Final reconciliation | Compare all records by source transaction ID | Web funding, SWAP, conversion, outbox, game ledger, game balance, and HUD all agree |

## Abort And Recovery

- On any identity, amount, or journal mismatch, stop the canary producer/cron and keep the callback non-public.
- Do not refund or credit manually until the exact game source transaction ID has been queried.
- If the game ledger exists, settle/retry the web journals with the same ID; do not issue another credit.
- If the game ledger is proven absent and dispatch is quarantined, restore the synthetic USDT in one audited `REFUNDED` transaction and mark the conversion `REFUNDED` atomically.
- A timeout is not proof that the game did not commit.

## Current Candidate Evidence

- Overlay SHA-256: `8f326cb79e0c8123712aec90217602f2428612cfa6e54d30c42aad3e804cf9fb`.
- Isolated candidate build: `m31Ry3w4SeOT1N3oxcdJw`, Next.js `15.5.20`.
- Backend integration passes v1 compatibility, v2 identity pinning, wrong-player rejection, balance read, decimal remainder, realtime, retry, and restart persistence.
- The production-artifact harness also runs the PostgreSQL store smoke in a dedicated temporary database; pinned-player credit/rejection, cleanup, duplicate delivery, isolated web restart, and post-commit recovery pass without changing production.
- Isolated web validation passes migration, database E2E, build, duplicate delivery, malformed success, wrong-player response, permanent `409`, post-commit response loss, and concurrent success/failure race.
- Validator reports `LIVE_WEB_CHANGED=no`, `PRODUCTION_DATABASE_MUTATED=no`, `PRODUCTION_SERVICES_RESTARTED=no`, and `REAL_PAYMENT_USED=no`.
- The candidate has not been deployed. Production remains at the prior exact-one canary state until separately approved.

## Classification

After all ten gates pass:

- Allowed label: `technical no-money canary complete`.
- Not allowed label: `real-money canary complete`.
- Keep rollout at one QA identity while real payment approval is pending.
- Do not use `WEB_TOPUP_MODE=open` until positive gameplay grants are globally server-authoritative, reconciliation/refund tooling is operational, legacy web Point is migrated intentionally, and the web USDT Float ledger risk is removed or formally accepted.
