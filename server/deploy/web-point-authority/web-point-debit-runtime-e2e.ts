import { createHmac, randomUUID } from "crypto";
import { db } from "../lib/db";
import {
  convertLinkedGamePointToUsdt,
  getGamePointDebitUiConfig,
} from "../lib/game-point-debit";
import { quotePointToUsdtMicros } from "../lib/point-rate";

const SECRET = String(process.env.WEB_TOPUP_SECRET || "");
const POINT_AMOUNT = 100;
const INITIAL_GAME_POINT = 1000;
const INITIAL_WEB_USDT = 10;

type Scenario =
  | "valid"
  | "reserve-response-loss"
  | "capture-response-loss"
  | "settlement-failure"
  | "identity-mismatch"
  | "conflict";

type Seed = {
  userId: string;
  gamePlayerId: string;
  requestId: string;
};

type FakeReservation = {
  id: string;
  playerId: string;
  webUserId: string;
  expectedPlayerId: string;
  pointAmount: number;
  purpose: string;
  source: string;
  occurredAt: string;
  status: "RESERVED" | "CAPTURED" | "RELEASED";
  signature: string;
};

function assert(condition: unknown, message: string): asserts condition {
  if (!condition) throw new Error(message);
}

function closeEnough(actual: number, expected: number): boolean {
  return Math.abs(actual - expected) < 0.000000001;
}

function canonical(timestamp: string, operation: string, body: any): string {
  return JSON.stringify([
    "ywonder-point-reservation-v1",
    timestamp,
    operation,
    body.reservation_id,
    body.web_user_id,
    body.expected_player_id,
    String(body.point_amount),
    body.purpose,
    body.source,
    body.occurred_at,
  ]);
}

function reservationSignature(body: any): string {
  return JSON.stringify([
    body.reservation_id,
    body.web_user_id,
    body.expected_player_id,
    body.point_amount,
    body.purpose,
    body.source,
    body.occurred_at,
  ]);
}

function json(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "Content-Type": "application/json" },
  });
}

async function seed(label: string): Promise<Seed> {
  const suffix = randomUUID().replace(/-/g, "");
  const userId = `debit_runtime_${label}_${suffix}`;
  const gamePlayerId = `player_${userId}`;
  await db.user.create({
    data: {
      id: userId,
      email: `${userId}@example.test`,
      username: userId,
      fullName: `Debit Runtime ${label}`,
      refCode: `DR${suffix}`,
    },
  });
  await db.wallet.create({
    data: {
      id: `debit_wallet_${suffix}`,
      userId,
      balanceUsdt: INITIAL_WEB_USDT,
      balanceGXL: 0,
    },
  });
  await db.gamePointLinkedAccount.create({
    data: { userId, gamePlayerId, linkedBy: "isolated-debit-runtime-e2e" },
  });
  return { userId, gamePlayerId, requestId: randomUUID() };
}

async function state(seedData: Seed) {
  const debit = await db.gamePointDebit.findUnique({
    where: { requestId: seedData.requestId },
  });
  const [wallet, transactionCount, transaction] = await Promise.all([
    db.wallet.findUnique({ where: { userId: seedData.userId } }),
    db.transaction.count({ where: { userId: seedData.userId } }),
    debit
      ? db.transaction.findUnique({ where: { id: debit.sourceTransactionId } })
      : Promise.resolve(null),
  ]);
  return { debit, wallet, transaction, transactionCount };
}

async function makeDue(requestId: string) {
  await db.gamePointDebit.update({
    where: { requestId },
    data: { nextAttemptAt: new Date(0) },
  });
}

async function main() {
  assert(SECRET.length >= 32, "Debit runtime E2E secret is missing");
  assert(process.env.WEB_POINT_WALLET_DEBIT_ENABLED === "true",
    "Debit runtime E2E feature flag is disabled");
  const activeRate = await db.pointExchangeRateVersion.findFirst({
    where: { pair: "USDT_POINT", isActive: true },
    orderBy: [{ effectiveAt: "desc" }, { createdAt: "desc" }],
  });
  assert(activeRate, "Debit runtime E2E has no active Point rate");
  const feeBps = Number(process.env.WEB_POINT_DEBIT_FEE_BPS);
  const pointMicros = String(POINT_AMOUNT * 1_000_000);
  const originalQuote = quotePointToUsdtMicros(pointMicros, activeRate.rateMicros, feeBps);
  const originalNetUsdt = Number(originalQuote.netUsdtMicros) / 1_000_000;

  const seeds: Seed[] = [];
  const gameBalances = new Map<string, number>();
  const scenarios = new Map<string, Scenario>();
  const reservations = new Map<string, FakeReservation>();
  const oneShot = new Set<string>();
  const originalFetch = globalThis.fetch;
  let replacementRateId = "";

  globalThis.fetch = (async (input: string | URL | Request, init?: RequestInit) => {
    const url = new URL(String(input));
    assert(url.hostname === "127.0.0.1" || url.hostname === "localhost",
      "Debit dispatcher escaped loopback");
    const match = /\/internal\/web\/point-(reserve|capture|release)$/.exec(url.pathname);
    assert(match, "Debit dispatcher called an unexpected endpoint");
    const operation = match[1] as "reserve" | "capture" | "release";
    const body = JSON.parse(String(init?.body || "{}"));
    const headers = new Headers(init?.headers);
    const timestamp = String(headers.get("X-YWonder-Timestamp") || "");
    const expectedHmac = createHmac("sha256", SECRET)
      .update(canonical(timestamp, operation, body), "utf8")
      .digest("hex");
    assert(headers.get("X-YWonder-Signature") === expectedHmac,
      "Debit dispatcher HMAC is invalid");
    assert(body.point_amount === POINT_AMOUNT && body.purpose === "point_to_usdt"
      && body.source === "ywonder-web", "Debit dispatcher payload changed");

    const scenario = scenarios.get(body.web_user_id) || "valid";
    if (scenario === "conflict") return json({ error: "IDEMPOTENCY_CONFLICT" }, 409);
    const expectedPlayerId = `player_${body.web_user_id}`;
    assert(body.expected_player_id === expectedPlayerId,
      "Debit dispatcher did not pin the expected game player");
    const signature = reservationSignature(body);
    let reservation = reservations.get(body.reservation_id);
    if (reservation && reservation.signature !== signature) {
      return json({ error: "IDEMPOTENCY_CONFLICT" }, 409);
    }

    let duplicate = false;
    if (operation === "reserve") {
      if (reservation) {
        duplicate = true;
      } else {
        const balance = gameBalances.get(body.web_user_id);
        assert(Number.isSafeInteger(balance), "Fake game balance is missing");
        if (balance! < body.point_amount) return json({ error: "INSUFFICIENT_BALANCE" }, 409);
        gameBalances.set(body.web_user_id, balance! - body.point_amount);
        reservation = {
          id: body.reservation_id,
          playerId: expectedPlayerId,
          webUserId: body.web_user_id,
          expectedPlayerId,
          pointAmount: body.point_amount,
          purpose: body.purpose,
          source: body.source,
          occurredAt: body.occurred_at,
          status: "RESERVED",
          signature,
        };
        reservations.set(reservation.id, reservation);
      }
    } else {
      if (!reservation) return json({ error: "POINT_RESERVATION_NOT_FOUND" }, 404);
      const target = operation === "capture" ? "CAPTURED" : "RELEASED";
      if (reservation.status === target) {
        duplicate = true;
      } else if (reservation.status !== "RESERVED") {
        return json({ error: "POINT_RESERVATION_STATE_CONFLICT" }, 409);
      } else if (operation === "capture") {
        reservation.status = "CAPTURED";
      } else {
        reservation.status = "RELEASED";
        gameBalances.set(
          body.web_user_id,
          Number(gameBalances.get(body.web_user_id)) + body.point_amount
        );
      }
    }

    const shotKey = `${scenario}:${operation}:${body.reservation_id}`;
    if (scenario === "settlement-failure" && operation === "reserve" && !oneShot.has(shotKey)) {
      oneShot.add(shotKey);
      await db.wallet.delete({ where: { userId: body.web_user_id } });
    }
    if (scenario === "reserve-response-loss" && operation === "reserve" && !oneShot.has(shotKey)) {
      oneShot.add(shotKey);
      throw new Error("ISOLATED_DEBIT_RESERVE_RESPONSE_LOSS");
    }
    if (scenario === "capture-response-loss" && operation === "capture" && !oneShot.has(shotKey)) {
      oneShot.add(shotKey);
      throw new Error("ISOLATED_DEBIT_CAPTURE_RESPONSE_LOSS");
    }

    const response = {
      ok: true,
      duplicate,
      operation,
      player_id: expectedPlayerId,
      economy: { pos: gameBalances.get(body.web_user_id) },
      reservation: {
        id: reservation!.id,
        playerId: reservation!.playerId,
        webUserId: reservation!.webUserId,
        expectedPlayerId: reservation!.expectedPlayerId,
        pointAmount: reservation!.pointAmount,
        purpose: reservation!.purpose,
        source: reservation!.source,
        occurredAt: reservation!.occurredAt,
        status: reservation!.status,
      },
      transaction: { ref: reservation!.id },
    };
    if (scenario === "identity-mismatch" && operation === "reserve" && !oneShot.has(shotKey)) {
      oneShot.add(shotKey);
      response.player_id = `wrong_${expectedPlayerId}`;
    }
    return json(response);
  }) as typeof fetch;

  try {
    const normal = await seed("normal");
    seeds.push(normal);
    gameBalances.set(normal.userId, INITIAL_GAME_POINT);
    scenarios.set(normal.userId, "valid");
    const first = await convertLinkedGamePointToUsdt({
      userId: normal.userId,
      pointAmount: POINT_AMOUNT,
      requestId: normal.requestId,
    });
    assert(first.ok && first.terminal && first.debitStatus === "CAPTURED",
      "Normal Point debit did not capture");
    let current = await state(normal);
    assert(current.debit?.status === "CAPTURED" && current.transaction?.status === "SUCCESS",
      "Normal Point debit did not settle both journals");
    assert(current.transactionCount === 1 && current.wallet
      && closeEnough(current.wallet.balanceUsdt, INITIAL_WEB_USDT + originalNetUsdt),
    "Normal Point debit did not credit web USDT exactly once");
    assert(gameBalances.get(normal.userId) === INITIAL_GAME_POINT - POINT_AMOUNT,
      "Normal Point debit changed the wrong game balance");
    assert(getGamePointDebitUiConfig(normal.userId).supportsYwh === false,
      "YWH debit adapter was enabled without an approved contract");

    const replay = await convertLinkedGamePointToUsdt({
      userId: normal.userId,
      pointAmount: POINT_AMOUNT,
      requestId: normal.requestId,
    });
    assert(replay.ok && replay.duplicate, "Same debit request did not replay idempotently");
    current = await state(normal);
    assert(current.transactionCount === 1 && current.wallet
      && closeEnough(current.wallet.balanceUsdt, INITIAL_WEB_USDT + originalNetUsdt),
    "Debit replay credited web USDT twice");
    const conflictingReplay = await convertLinkedGamePointToUsdt({
      userId: normal.userId,
      pointAmount: POINT_AMOUNT + 1,
      requestId: normal.requestId,
    });
    assert(!conflictingReplay.ok && conflictingReplay.error === "IDEMPOTENCY_CONFLICT",
      "Same debit request ID accepted a different amount");

    const concurrent = await seed("concurrent");
    seeds.push(concurrent);
    gameBalances.set(concurrent.userId, INITIAL_GAME_POINT);
    scenarios.set(concurrent.userId, "valid");
    const concurrentResults = await Promise.all([
      convertLinkedGamePointToUsdt({
        userId: concurrent.userId,
        pointAmount: POINT_AMOUNT,
        requestId: concurrent.requestId,
      }),
      convertLinkedGamePointToUsdt({
        userId: concurrent.userId,
        pointAmount: POINT_AMOUNT,
        requestId: concurrent.requestId,
      }),
    ]);
    assert(concurrentResults.some((result) => result.ok),
      "Concurrent debit calls produced no recoverable success");
    current = await state(concurrent);
    assert(current.debit?.status === "CAPTURED" && current.transaction?.status === "SUCCESS"
      && current.transactionCount === 1 && current.wallet
      && closeEnough(current.wallet.balanceUsdt, INITIAL_WEB_USDT + originalNetUsdt),
    "Concurrent debit calls did not settle exactly once");
    assert(gameBalances.get(concurrent.userId) === INITIAL_GAME_POINT - POINT_AMOUNT,
      "Concurrent debit calls reserved game Point twice");

    const reserveLoss = await seed("reserve_loss");
    seeds.push(reserveLoss);
    gameBalances.set(reserveLoss.userId, INITIAL_GAME_POINT);
    scenarios.set(reserveLoss.userId, "reserve-response-loss");
    const lostReserve = await convertLinkedGamePointToUsdt({
      userId: reserveLoss.userId,
      pointAmount: POINT_AMOUNT,
      requestId: reserveLoss.requestId,
    });
    assert(lostReserve.pending && lostReserve.debitStatus === "RESERVE_PENDING",
      "Reserve response loss did not remain pending");
    current = await state(reserveLoss);
    assert(current.transactionCount === 0 && current.wallet?.balanceUsdt === INITIAL_WEB_USDT,
      "Reserve response loss settled web USDT before confirmation");
    assert(gameBalances.get(reserveLoss.userId) === INITIAL_GAME_POINT - POINT_AMOUNT,
      "Reserve response loss fixture did not commit game reservation");
    const secondRequest = await convertLinkedGamePointToUsdt({
      userId: reserveLoss.userId,
      pointAmount: POINT_AMOUNT,
      requestId: randomUUID(),
    });
    assert(!secondRequest.ok && secondRequest.terminal
      && secondRequest.error === "GAME_POINT_DEBIT_ALREADY_PENDING",
      "A second unresolved debit was accepted for one account");

    replacementRateId = `debit_runtime_rate_${randomUUID().replace(/-/g, "")}`;
    const replacementRateMicros = activeRate.rateMicros === "25000000" ? "20000000" : "25000000";
    const replacementQuote = quotePointToUsdtMicros(pointMicros, replacementRateMicros, feeBps);
    assert(replacementQuote.netUsdtMicros !== originalQuote.netUsdtMicros,
      "Rate-change fixture did not produce a different debit quote");
    await db.$transaction(async (tx) => {
      await tx.pointExchangeRateVersion.update({
        where: { id: activeRate.id },
        data: { isActive: false },
      });
      await tx.pointExchangeRateVersion.create({
        data: {
          id: replacementRateId,
          pair: "USDT_POINT",
          rateMicros: replacementRateMicros,
          isActive: true,
          createdBy: "isolated-debit-runtime-rate-change",
        },
      });
    });
    scenarios.set(reserveLoss.userId, "valid");
    await makeDue(reserveLoss.requestId);
    const recoveredReserve = await convertLinkedGamePointToUsdt({
      userId: reserveLoss.userId,
      pointAmount: POINT_AMOUNT,
      requestId: reserveLoss.requestId,
    });
    assert(recoveredReserve.ok && recoveredReserve.debitStatus === "CAPTURED",
      "Reserve response loss did not recover idempotently");
    current = await state(reserveLoss);
    assert(current.debit?.rateVersionId === activeRate.id
      && current.debit.rateMicros === activeRate.rateMicros,
    "Debit retry changed its pinned Admin rate");
    assert(current.wallet && closeEnough(
      current.wallet.balanceUsdt,
      INITIAL_WEB_USDT + originalNetUsdt
    ), "Debit retry settled with the replacement rate");
    await db.$transaction(async (tx) => {
      await tx.pointExchangeRateVersion.delete({ where: { id: replacementRateId } });
      await tx.pointExchangeRateVersion.update({
        where: { id: activeRate.id },
        data: { isActive: true },
      });
    });
    replacementRateId = "";

    const captureLoss = await seed("capture_loss");
    seeds.push(captureLoss);
    gameBalances.set(captureLoss.userId, INITIAL_GAME_POINT);
    scenarios.set(captureLoss.userId, "capture-response-loss");
    const lostCapture = await convertLinkedGamePointToUsdt({
      userId: captureLoss.userId,
      pointAmount: POINT_AMOUNT,
      requestId: captureLoss.requestId,
    });
    assert(lostCapture.pending && lostCapture.debitStatus === "CAPTURE_PENDING",
      "Capture response loss did not remain pending");
    current = await state(captureLoss);
    assert(current.transaction?.status === "PENDING" && current.transactionCount === 1
      && current.wallet?.balanceUsdt === INITIAL_WEB_USDT,
    "Capture response loss exposed pending USDT as spendable");
    scenarios.set(captureLoss.userId, "valid");
    await makeDue(captureLoss.requestId);
    const recoveredCapture = await convertLinkedGamePointToUsdt({
      userId: captureLoss.userId,
      pointAmount: POINT_AMOUNT,
      requestId: captureLoss.requestId,
    });
    assert(recoveredCapture.ok && recoveredCapture.debitStatus === "CAPTURED",
      "Capture response loss did not recover idempotently");
    current = await state(captureLoss);
    assert(current.transaction?.status === "SUCCESS" && current.transactionCount === 1
      && current.wallet && closeEnough(
        current.wallet.balanceUsdt,
        INITIAL_WEB_USDT + originalNetUsdt
      ), "Capture retry credited web USDT twice");

    const tampered = await seed("tampered_journal");
    seeds.push(tampered);
    gameBalances.set(tampered.userId, INITIAL_GAME_POINT);
    scenarios.set(tampered.userId, "capture-response-loss");
    const tamperedPending = await convertLinkedGamePointToUsdt({
      userId: tampered.userId,
      pointAmount: POINT_AMOUNT,
      requestId: tampered.requestId,
    });
    assert(tamperedPending.pending && tamperedPending.debitStatus === "CAPTURE_PENDING",
      "Tampered-journal fixture did not stop after remote capture");
    current = await state(tampered);
    assert(current.transaction, "Tampered-journal fixture has no settlement transaction");
    await db.transaction.update({
      where: { id: current.transaction.id },
      data: { metadata: "{}" },
    });
    scenarios.set(tampered.userId, "valid");
    await makeDue(tampered.requestId);
    const quarantinedJournal = await convertLinkedGamePointToUsdt({
      userId: tampered.userId,
      pointAmount: POINT_AMOUNT,
      requestId: tampered.requestId,
    });
    assert(!quarantinedJournal.ok && quarantinedJournal.pending
      && quarantinedJournal.debitStatus === "MANUAL_REVIEW",
    "Tampered settlement journal was marked captured");
    current = await state(tampered);
    assert(current.transactionCount === 1 && current.transaction?.status === "PENDING"
      && current.wallet?.balanceUsdt === INITIAL_WEB_USDT,
    "Tampered settlement journal exposed pending USDT as spendable");

    const settlementFailure = await seed("settlement_failure");
    seeds.push(settlementFailure);
    gameBalances.set(settlementFailure.userId, INITIAL_GAME_POINT);
    scenarios.set(settlementFailure.userId, "settlement-failure");
    const released = await convertLinkedGamePointToUsdt({
      userId: settlementFailure.userId,
      pointAmount: POINT_AMOUNT,
      requestId: settlementFailure.requestId,
    });
    assert(!released.ok && released.terminal && released.debitStatus === "RELEASED",
      "Failed web settlement did not release the game reservation");
    current = await state(settlementFailure);
    assert(current.transactionCount === 0 && !current.wallet,
      "Failed web settlement left a credited transaction");
    assert(gameBalances.get(settlementFailure.userId) === INITIAL_GAME_POINT,
      "Failed web settlement did not restore game Point");

    const identity = await seed("identity");
    seeds.push(identity);
    gameBalances.set(identity.userId, INITIAL_GAME_POINT);
    scenarios.set(identity.userId, "identity-mismatch");
    const mismatched = await convertLinkedGamePointToUsdt({
      userId: identity.userId,
      pointAmount: POINT_AMOUNT,
      requestId: identity.requestId,
    });
    assert(mismatched.pending && mismatched.debitStatus === "RESERVE_PENDING",
      "Wrong-player response was not quarantined for retry");
    current = await state(identity);
    assert(current.transactionCount === 0 && current.wallet?.balanceUsdt === INITIAL_WEB_USDT,
      "Wrong-player response settled web USDT");
    scenarios.set(identity.userId, "valid");
    await makeDue(identity.requestId);
    const recoveredIdentity = await convertLinkedGamePointToUsdt({
      userId: identity.userId,
      pointAmount: POINT_AMOUNT,
      requestId: identity.requestId,
    });
    assert(recoveredIdentity.ok && recoveredIdentity.debitStatus === "CAPTURED",
      "Wrong-player response did not recover with the same reservation ID");

    const conflict = await seed("conflict");
    seeds.push(conflict);
    gameBalances.set(conflict.userId, INITIAL_GAME_POINT);
    scenarios.set(conflict.userId, "conflict");
    const rejected = await convertLinkedGamePointToUsdt({
      userId: conflict.userId,
      pointAmount: POINT_AMOUNT,
      requestId: conflict.requestId,
    });
    assert(!rejected.ok && rejected.pending && rejected.debitStatus === "MANUAL_REVIEW",
      "Remote idempotency conflict was not quarantined");
    current = await state(conflict);
    assert(current.transactionCount === 0 && current.wallet?.balanceUsdt === INITIAL_WEB_USDT,
      "Remote idempotency conflict changed web USDT");
    assert(gameBalances.get(conflict.userId) === INITIAL_GAME_POINT,
      "Remote idempotency conflict changed game Point");

    console.log("WEB_POINT_DEBIT_RUNTIME_E2E=pass");
  } finally {
    globalThis.fetch = originalFetch;
    const userIds = seeds.map((entry) => entry.userId);
    await db.gamePointDebit.deleteMany({ where: { userId: { in: userIds } } });
    await db.transaction.deleteMany({ where: { userId: { in: userIds } } });
    await db.gamePointLinkedAccount.deleteMany({ where: { userId: { in: userIds } } });
    await db.wallet.deleteMany({ where: { userId: { in: userIds } } });
    await db.user.deleteMany({ where: { id: { in: userIds } } });
    if (replacementRateId) {
      await db.pointExchangeRateVersion.delete({ where: { id: replacementRateId } }).catch(() => {});
      await db.pointExchangeRateVersion.update({
        where: { id: activeRate.id },
        data: { isActive: true },
      }).catch(() => {});
    }
    await db.$disconnect();
  }
}

main().catch((error) => {
  console.error(`WEB_POINT_DEBIT_RUNTIME_E2E=fail: ${String(error?.message || error)}`);
  process.exitCode = 1;
});
