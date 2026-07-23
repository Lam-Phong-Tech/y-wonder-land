import { createHmac, randomUUID } from "crypto";
import { db } from "../lib/db";
import { dispatchGamePointSyncOutboxById } from "../lib/game-point-sync";
import { getActiveUsdtPointRate, quoteUsdtToPointMicros } from "../lib/point-rate";

const SECRET = String(process.env.WEB_TOPUP_SECRET || "");
const SOURCE = "ywonder-web-usdt-to-point";
let EXPECTED_POINT_AMOUNT = "";
let EXPECTED_POINT_NUMBER = 0;
let EXPECTED_POINT_MICROS = "";
let EXPECTED_RATE_VERSION_ID = "";
let EXPECTED_RATE_MICROS = "";
let EXPECTED_ROUNDING_REMAINDER = "";

type Scenario = "valid" | "invalid-200" | "identity-mismatch" | "commit-timeout" | "conflict" | "race";
type Seed = {
  userId: string;
  gamePlayerId: string;
  transactionId: string;
  outboxId: string;
  conversionId: string;
};

function assert(condition: unknown, message: string): asserts condition {
  if (!condition) throw new Error(message);
}

function canonical(timestamp: string, body: any): string {
  return JSON.stringify([
    "ywonder-point-credit-v2",
    timestamp,
    body.transaction_id,
    body.web_user_id,
    body.expected_player_id,
    body.point_amount,
    body.occurred_at,
    body.source,
    body.username,
    body.display_name,
  ]);
}

function responseBody(body: any, duplicate: boolean, point: number) {
  return {
    ok: true,
    duplicate,
    player_id: body.expected_player_id,
    economy: { pos: point },
    transaction: {
      ref: body.transaction_id,
      webUserId: body.web_user_id,
      expectedPlayerId: body.expected_player_id,
      pointAmount: body.point_amount,
      source: body.source,
    },
  };
}

async function seed(label: string): Promise<Seed> {
  const suffix = randomUUID().replace(/-/g, "");
  const userId = `authority_runtime_${label}_${suffix}`;
  const gamePlayerId = `player_${userId}`;
  const transactionId = `gpc_${randomUUID().replace(/-/g, "")}`;
  const outboxId = `authority_outbox_${suffix}`;
  const conversionId = `authority_conversion_${suffix}`;
  const requestId = randomUUID();
  const occurredAt = new Date();

  await db.$transaction(async (tx) => {
    await tx.user.create({
      data: {
        id: userId,
        email: `${userId}@example.test`,
        username: userId,
        fullName: `Authority Runtime ${label}`,
        refCode: `ART${suffix}`,
      },
    });
    await tx.wallet.create({
      data: { id: `authority_wallet_${suffix}`, userId, balanceUsdt: 10, balanceGXL: 0 },
    });
    await tx.gamePointLinkedAccount.create({
      data: { userId, gamePlayerId, linkedBy: "isolated-runtime-e2e" },
    });
    const debit = await tx.wallet.updateMany({
      where: { userId, balanceUsdt: { gte: 0.06 } },
      data: { balanceUsdt: { decrement: 0.06 } },
    });
    assert(debit.count === 1, "Synthetic conversion did not reserve USDT exactly once");
    await tx.transaction.create({
      data: {
        id: transactionId,
        userId,
        type: "SWAP",
        amount: EXPECTED_POINT_NUMBER,
        currency: "GXL",
        status: "PENDING",
        metadata: JSON.stringify({
          requestId,
          usdtMicros: "60000",
          pointMicros: EXPECTED_POINT_MICROS,
          rateVersionId: EXPECTED_RATE_VERSION_ID,
          rateMicros: EXPECTED_RATE_MICROS,
          roundingRemainder: EXPECTED_ROUNDING_REMAINDER,
          authority: "game",
        }),
      },
    });
    await tx.gamePointSyncOutbox.create({
      data: {
        id: outboxId,
        sourceTransactionId: transactionId,
        userId,
        pointAmount: EXPECTED_POINT_AMOUNT,
        occurredAt,
        source: SOURCE,
      },
    });
    await tx.gamePointConversion.create({
      data: {
        id: conversionId,
        requestId,
        sourceTransactionId: transactionId,
        outboxId,
        userId,
        usdtMicros: "60000",
        pointMicros: EXPECTED_POINT_MICROS,
        rateVersionId: EXPECTED_RATE_VERSION_ID,
        rateMicros: EXPECTED_RATE_MICROS,
        roundingRemainder: EXPECTED_ROUNDING_REMAINDER,
      },
    });
  });
  return { userId, gamePlayerId, transactionId, outboxId, conversionId };
}

async function state(seedData: Seed) {
  const [wallet, outbox, conversion, transaction] = await Promise.all([
    db.wallet.findUnique({ where: { userId: seedData.userId } }),
    db.gamePointSyncOutbox.findUnique({ where: { id: seedData.outboxId } }),
    db.gamePointConversion.findUnique({ where: { id: seedData.conversionId } }),
    db.transaction.findUnique({ where: { id: seedData.transactionId } }),
  ]);
  return { wallet, outbox, conversion, transaction };
}

async function resetForDuplicate(seedData: Seed) {
  await db.$transaction([
    db.gamePointSyncOutbox.update({
      where: { id: seedData.outboxId },
      data: { status: "RETRY", sentAt: null, nextAttemptAt: new Date(0) },
    }),
    db.gamePointConversion.update({
      where: { id: seedData.conversionId },
      data: { status: "RETRY", sentAt: null },
    }),
    db.transaction.update({ where: { id: seedData.transactionId }, data: { status: "PENDING" } }),
  ]);
}

async function cleanup(seeds: Seed[]) {
  const userIds = seeds.map((entry) => entry.userId);
  await db.gamePointConversion.deleteMany({ where: { userId: { in: userIds } } });
  await db.gamePointSyncOutbox.deleteMany({ where: { userId: { in: userIds } } });
  await db.transaction.deleteMany({ where: { userId: { in: userIds } } });
  await db.gamePointLinkedAccount.deleteMany({ where: { userId: { in: userIds } } });
  await db.wallet.deleteMany({ where: { userId: { in: userIds } } });
  await db.user.deleteMany({ where: { id: { in: userIds } } });
}

async function main() {
  assert(SECRET.length >= 32, "Runtime E2E secret is missing");
  const activeRate = await getActiveUsdtPointRate();
  const quote = quoteUsdtToPointMicros("60000", activeRate.rateMicros);
  EXPECTED_POINT_AMOUNT = quote.pointAmount;
  EXPECTED_POINT_MICROS = quote.pointMicros;
  EXPECTED_POINT_NUMBER = Number(quote.pointMicros) / 1_000_000;
  EXPECTED_RATE_VERSION_ID = activeRate.id;
  EXPECTED_RATE_MICROS = activeRate.rateMicros;
  EXPECTED_ROUNDING_REMAINDER = quote.roundingRemainder;
  assert(EXPECTED_POINT_NUMBER > 0 && EXPECTED_POINT_AMOUNT !== "1.000000",
    "Runtime E2E did not use the Admin-controlled Point rate");
  const seeds: Seed[] = [];
  let replacementRateId = "";
  const credits = new Map<string, { signature: string; point: number }>();
  let scenario: Scenario = "valid";
  let raceCalls = 0;
  let releaseRace: (() => void) | null = null;
  let raceBarrier = Promise.resolve();

  globalThis.fetch = (async (input: string | URL | Request, init?: RequestInit) => {
    const url = String(input);
    assert(url.endsWith("/internal/web/point-credit"), "Dispatcher called an unexpected endpoint");
    const body = JSON.parse(String(init?.body || "{}"));
    const headers = new Headers(init?.headers);
    const timestamp = String(headers.get("X-YWonder-Timestamp") || "");
    const expected = createHmac("sha256", SECRET).update(canonical(timestamp, body), "utf8").digest("hex");
    assert(headers.get("X-YWonder-Signature") === expected, "Dispatcher HMAC is invalid");
    assert(body.source === SOURCE && body.point_amount === EXPECTED_POINT_AMOUNT, "Dispatcher payload changed");
    assert(body.expected_player_id === `player_${body.web_user_id}`,
      "Dispatcher did not pin the expected game player");

    if (scenario === "conflict") {
      return new Response(JSON.stringify({ error: "IDEMPOTENCY_CONFLICT" }), {
        status: 409,
        headers: { "Content-Type": "application/json" },
      });
    }
    if (scenario === "invalid-200") {
      return new Response(JSON.stringify({ ok: true, duplicate: false }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    if (scenario === "identity-mismatch") {
      const mismatched = responseBody(body, false, 1);
      mismatched.player_id = `wrong_${body.expected_player_id}`;
      return new Response(JSON.stringify(mismatched), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }

    const requestSignature = JSON.stringify(body);
    const existing = credits.get(body.transaction_id);
    if (existing) assert(existing.signature === requestSignature, "Fake game saw an idempotency conflict");
    else credits.set(body.transaction_id, { signature: requestSignature, point: EXPECTED_POINT_NUMBER });
    const duplicate = Boolean(existing);

    if (scenario === "commit-timeout") throw new Error("ISOLATED_POST_COMMIT_RESPONSE_LOSS");
    if (scenario === "race") {
      const callNumber = ++raceCalls;
      if (callNumber === 2) releaseRace?.();
      await Promise.race([
        raceBarrier,
        new Promise((_, reject) => setTimeout(() => reject(new Error("ISOLATED_RACE_BARRIER_TIMEOUT")), 2000)),
      ]);
      if (callNumber === 2) {
        await new Promise((resolve) => setTimeout(resolve, 100));
        throw new Error("ISOLATED_CONCURRENT_RESPONSE_LOSS");
      }
      await new Promise((resolve) => setTimeout(resolve, 10));
    }
    return new Response(JSON.stringify(responseBody(body, duplicate, 1)), {
      status: 200,
      headers: { "Content-Type": "application/json" },
    });
  }) as typeof fetch;

  try {
    const normal = await seed("normal");
    seeds.push(normal);
    replacementRateId = `authority_runtime_rate_${randomUUID().replace(/-/g, "")}`;
    await db.$transaction([
      db.pointExchangeRateVersion.update({
        where: { id: EXPECTED_RATE_VERSION_ID },
        data: { isActive: false },
      }),
      db.pointExchangeRateVersion.create({
        data: {
          id: replacementRateId,
          pair: "USDT_POINT",
          rateMicros: "25000000",
          isActive: true,
          createdBy: "isolated-runtime-rate-change",
        },
      }),
    ]);
    const replacementQuote = quoteUsdtToPointMicros("60000", "25000000");
    assert(replacementQuote.pointAmount !== EXPECTED_POINT_AMOUNT,
      "Rate-change fixture did not produce a different quote");
    scenario = "valid";
    const first = await dispatchGamePointSyncOutboxById(normal.outboxId);
    assert(first.ok && !first.duplicate, "Normal dispatch did not succeed");
    let current = await state(normal);
    assert(current.outbox?.status === "SENT" && current.conversion?.status === "SENT"
      && current.transaction?.status === "SUCCESS", "Normal dispatch did not settle every journal");
    assert(current.wallet?.balanceUsdt === 9.94 && current.wallet.balanceGXL === 0,
      "Normal dispatch changed the wrong web balance");

    await resetForDuplicate(normal);
    const duplicate = await dispatchGamePointSyncOutboxById(normal.outboxId);
    assert(duplicate.ok && duplicate.duplicate, "Duplicate dispatch was not idempotent");
    current = await state(normal);
    assert(current.outbox?.status === "SENT" && current.conversion?.status === "SENT"
      && current.transaction?.status === "SUCCESS", "Duplicate dispatch did not restore settled journals");
    assert(credits.size === 1, "Duplicate dispatch created a second game credit");

    const invalid = await seed("invalid");
    seeds.push(invalid);
    scenario = "invalid-200";
    const invalidResult = await dispatchGamePointSyncOutboxById(invalid.outboxId);
    assert(!invalidResult.ok && invalidResult.error === "INVALID_GAME_POINT_SYNC_RESPONSE",
      "Invalid HTTP 200 response was accepted");
    current = await state(invalid);
    assert(current.outbox?.status === "RETRY" && current.conversion?.status === "RETRY"
      && current.transaction?.status === "PENDING", "Invalid response settled a conversion");
    scenario = "valid";
    const recoveredInvalid = await dispatchGamePointSyncOutboxById(invalid.outboxId);
    assert(recoveredInvalid.ok, "Invalid-response conversion did not recover");

    const identity = await seed("identity");
    seeds.push(identity);
    scenario = "identity-mismatch";
    const identityResult = await dispatchGamePointSyncOutboxById(identity.outboxId);
    assert(!identityResult.ok && identityResult.error === "INVALID_GAME_POINT_SYNC_RESPONSE",
      "A response for the wrong game player was accepted");
    current = await state(identity);
    assert(current.outbox?.status === "RETRY" && current.conversion?.status === "RETRY"
      && current.transaction?.status === "PENDING", "Identity mismatch settled a conversion");
    scenario = "valid";
    const recoveredIdentity = await dispatchGamePointSyncOutboxById(identity.outboxId);
    assert(recoveredIdentity.ok, "Identity-mismatch conversion did not recover");

    const timeout = await seed("timeout");
    seeds.push(timeout);
    scenario = "commit-timeout";
    const lost = await dispatchGamePointSyncOutboxById(timeout.outboxId);
    assert(!lost.ok && lost.error?.includes("ISOLATED_POST_COMMIT_RESPONSE_LOSS"),
      "Post-commit response loss was not retryable");
    current = await state(timeout);
    assert(current.outbox?.status === "RETRY" && current.conversion?.status === "RETRY"
      && current.transaction?.status === "PENDING", "Response loss corrupted pending journals");
    scenario = "valid";
    const recovered = await dispatchGamePointSyncOutboxById(timeout.outboxId);
    assert(recovered.ok && recovered.duplicate, "Post-commit response loss did not recover idempotently");
    assert(credits.get(timeout.transactionId)?.point === EXPECTED_POINT_NUMBER, "Post-commit retry credited twice");

    const conflict = await seed("conflict");
    seeds.push(conflict);
    scenario = "conflict";
    const rejected = await dispatchGamePointSyncOutboxById(conflict.outboxId);
    assert(!rejected.ok && rejected.permanent, "Game idempotency conflict was not quarantined");
    current = await state(conflict);
    assert(current.outbox?.status === "FAILED" && current.conversion?.status === "FAILED"
      && current.transaction?.status === "PENDING", "Permanent conflict was incorrectly settled");
    assert(current.wallet?.balanceUsdt === 9.94 && current.wallet.balanceGXL === 0,
      "Permanent conflict minted web Point or released reserved USDT");

    const race = await seed("race");
    seeds.push(race);
    scenario = "race";
    raceCalls = 0;
    raceBarrier = new Promise<void>((resolve) => { releaseRace = resolve; });
    const raced = await Promise.all([
      dispatchGamePointSyncOutboxById(race.outboxId),
      dispatchGamePointSyncOutboxById(race.outboxId),
    ]);
    assert(raced.some((result) => result.ok) && raced.some((result) => !result.ok),
      "Concurrent success/failure race was not exercised");
    current = await state(race);
    assert(current.outbox?.status === "SENT" && current.conversion?.status === "SENT"
      && current.transaction?.status === "SUCCESS", "Late failure regressed a successful settlement");
    assert(credits.get(race.transactionId)?.point === EXPECTED_POINT_NUMBER, "Concurrent dispatch credited twice");

    console.log("WEB_POINT_AUTHORITY_RUNTIME_E2E=pass");
  } finally {
    await cleanup(seeds);
    if (replacementRateId) {
      await db.pointExchangeRateVersion.delete({ where: { id: replacementRateId } }).catch(() => {});
      await db.pointExchangeRateVersion.update({
        where: { id: EXPECTED_RATE_VERSION_ID },
        data: { isActive: true },
      }).catch(() => {});
    }
    await db.$disconnect();
  }
}

main().catch((error) => {
  console.error(`WEB_POINT_AUTHORITY_RUNTIME_E2E=fail: ${String(error?.message || error)}`);
  process.exitCode = 1;
});
