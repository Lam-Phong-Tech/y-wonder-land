# Web Wallet -> Game Point: No-Money Canary

Date: 2026-07-17

Status: **TECHNICAL NO-MONEY CANARY COMPLETE**

This run proves the deployed software path with one dedicated QA identity. It did not use a bank, blockchain network, payment provider, or real-money settlement, so it must not be described as a real-money canary.

## Scope

```text
audited synthetic USDT funding
  -> real web wallet conversion action
  -> web Transaction + GamePointConversion + outbox
  -> authenticated cron
  -> loopback HMAC callback v2
  -> PostgreSQL game ledger
  -> realtime/relogin EXE and APK Point HUD
```

- QA profile: `WalletQA2026`. Raw web/game identifiers are intentionally omitted from repository documents.
- Game release: `a22312df3aee5701a31aa502d2fea3728546b2b1`.
- Web release: `/var/www/ywonder-releases/point-v3-a22312df`.
- Next.js build: `2rdR_xG8o4G1uonGEYEg0`.
- Rollout remains exact-one QA canary, loopback-only. Public callback returns `404`.
- `WEB_POINT_WALLET_DEBIT_ENABLED=false` remains set on game and web.

## Baseline And Quote

- Web USDT: `0`.
- Signed authoritative game Point: `5000`.
- Point rounding remainder: `0`.
- Active Admin rate: `26.5 Point / USDT` (`26500000` Point micros per USDT).
- Planned synthetic funding: exactly `2 USDT`.
- Exact quote: `2 x 26.5 = 53 Point`, with zero fractional remainder.

The funding executor rejects a stale or non-integral quote, an identity mismatch, a non-zero legacy conflict, an unresolved conversion, or any change to real deposit principal. It records `QA_SYNTHETIC_USDT_FUNDING`, never `USDT_DEPOSIT`.

## Executed Chain

1. Validation-only run passed without mutation.
2. The synthetic funding operation added exactly `2 USDT` to the dedicated QA web wallet and wrote one auditable funding journal.
3. Replaying the same funding operation produced no second credit.
4. The project owner used the real authenticated web wallet UI to convert the full `2 USDT`.
5. Web USDT returned to `0`; authoritative Point changed exactly once from `5000` to `5053`.
6. The web UI and the online EXE both displayed `5053` immediately.

Audited operation and evidence:

- Funding operation: `point-qa-funding:141ec249b9bacb93e129abe437d6c674`.
- Masked conversion chain reference: `59ea80c1e5e586fb428bb25e`.
- Funding validation report: `/root/ywonder-point-reports/point-qa-funding-validate-141ec249.json`.
- Root-only backup: `/root/ywonder-point-backups/qa-funding-20260717T090332Z-141ec249`.
- Chain reconciliation: `/root/ywonder-point-reports/point-qa-conversion-chain-20260717T091536Z.txt`.
- Duplicate replay: `/root/ywonder-point-reports/point-qa-duplicate-replay-20260717T091734Z.txt`.
- Non-disruptive fault matrix: `/root/ywonder-point-reports/point-qa-nondisruptive-faults-20260717T092438Z.txt`.

## Reconciliation Result

The final source transaction resolves to exactly:

| Layer | Result |
|---|---|
| Synthetic funding journal | `1`, `SUCCESS`, `+2 USDT` |
| Web SWAP transaction | `1`, `SUCCESS`, `-2 USDT / +53 Point` |
| `GamePointConversion` | `1`, `SENT` |
| Web outbox | `1`, `SENT`, attempts `1` |
| PostgreSQL game ledger | `1`, `web_topup_credit`, `+53 Point` |
| Final web USDT | `0` |
| Final authoritative Point | `5053` |
| Final rounding remainder | `0` |

Real `USDT_DEPOSIT` count and real deposited/withdrawn totals remained `0`.

## Idempotency And Fault Evidence

- Replaying the exact signed callback returned HTTP `200` with `duplicate=true`; web journals, game ledger and Point stayed unchanged.
- Same transaction ID with a different amount returned `409 IDEMPOTENCY_CONFLICT`.
- Wrong expected player returned `409 GAME_POINT_IDENTITY_MISMATCH`.
- Bad HMAC returned `401 INVALID_WEB_TOPUP_SIGNATURE`.
- Expired HMAC returned `401 WEB_TOPUP_REQUEST_EXPIRED`.
- An identity outside the allowlist returned `425 WEB_TOPUP_CANARY_USER_NOT_ALLOWED` and was not created in the game database.
- Zero Point returned `400 INVALID_POINT_AMOUNT`.
- Public callback remained `404`.
- All rejected cases left web balances, Point, conversion/outbox counts and game ledger unchanged.

Backend-down and post-commit response-loss scenarios were not injected into the live production services. They passed against the production artifact in the isolated temporary-database harness, including retry with the original immutable source transaction ID.

## Client Acceptance

The project owner completed the following matrix and observed `5053 Point` in every step:

1. EXE logout/login.
2. EXE -> APK: EXE was displaced, APK showed `5053`, and the replaced-session toast appeared.
3. APK logout, full app close, reopen and login.
4. APK -> EXE: APK was displaced, EXE showed `5053`, and the replaced-session toast appeared.

This confirms realtime delivery, PostgreSQL persistence, cross-device consistency and single-session replacement for the dedicated QA identity.

## Classification And Remaining Gates

- Allowed label: `technical no-money canary complete`.
- Not allowed label: `real-money canary complete`.
- The synthetic `53 Point` remains in the dedicated QA wallet with its complete audit chain. Do not manually reset it; any reversal requires a separately approved, idempotent audited operation.
- Keep rollout limited to the exact QA identity while real payment approval is pending.
- Keep debit disabled and public internal routes closed.
- Before `WEB_TOPUP_MODE=open`, complete the real-provider tiny canary, legacy account migration decisions, global server-authoritative gameplay grant migration, operational reconciliation/refund alerts, and final YWH/referral commission contracts.
