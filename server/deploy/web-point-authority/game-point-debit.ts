import { createHash } from "crypto";
import { db } from "@/lib/db";
import {
  GamePointCommandError,
  gamePointDebitTransportAllows,
  getGamePointLinkedAccount,
  normalizeGamePointConversionRequestId,
  resolveGamePointConversionAuthority,
  sendGamePointReservationCommand,
  type GamePointReservationInput,
} from "@/lib/game-point-authority";
import {
  getActiveUsdtPointRate,
  microsTextToNumber,
  quotePointToUsdtMicros,
} from "@/lib/point-rate";

const MICROS_SCALE = 1_000_000;
const DEBIT_SOURCE = "ywonder-web";
const RETRYABLE_STATUSES = [
  "RESERVE_PENDING",
  "RESERVED",
  "CAPTURE_PENDING",
  "RELEASE_PENDING",
] as const;
const TERMINAL_STATUSES = ["CAPTURED", "RELEASED", "REJECTED"] as const;
const PARKED_UNTIL = "9999-12-31T23:59:59.000Z";

export type GamePointDebitResult = {
  ok: boolean;
  error?: string;
  usdt?: number;
  pending?: boolean;
  duplicate?: boolean;
  terminal?: boolean;
  debitStatus?: string;
};

type DebitConfig = {
  enabled: boolean;
  feeBps: number | null;
  maxPoints: number;
};

function envEnabled(name: string): boolean {
  return String(process.env[name] || "").trim().toLowerCase() === "true";
}

function readDebitConfig(): DebitConfig {
  const rawFeeBps = String(process.env.WEB_POINT_DEBIT_FEE_BPS || "").trim();
  const feeBps = /^\d{1,4}$/.test(rawFeeBps) ? Number(rawFeeBps) : null;
  const rawMax = String(process.env.WEB_POINT_DEBIT_MAX_POINTS || "1000000").trim();
  const maxPoints = /^\d+$/.test(rawMax) ? Number(rawMax) : 0;
  const validFee = feeBps !== null && Number.isSafeInteger(feeBps)
    && feeBps >= 0 && feeBps < 10_000;
  const validMax = Number.isSafeInteger(maxPoints) && maxPoints > 0 && maxPoints <= 1_000_000_000;
  return {
    enabled: envEnabled("WEB_POINT_WALLET_DEBIT_ENABLED") && validFee && validMax,
    feeBps: validFee ? feeBps : null,
    maxPoints: validMax ? maxPoints : 0,
  };
}

export function getGamePointDebitUiConfig(userId: string) {
  const config = readDebitConfig();
  const enabled = config.enabled && gamePointDebitTransportAllows(userId);
  return {
    enabled,
    feePct: config.feeBps == null ? null : config.feeBps / 100,
    supportsUsdt: enabled,
    supportsYwh: false,
  };
}

function safeError(error: unknown): string {
  const message = error instanceof Error ? error.message : String(error || "UNKNOWN_ERROR");
  return message.replace(/[\r\n\0]/g, " ").slice(0, 160);
}

function errorIncludes(error: unknown, code: string): boolean {
  return (error instanceof Error ? error.message : String(error || "")).includes(code);
}

function normalizeWholePoint(value: number, maxPoints: number): number {
  const pointAmount = Number(value);
  if (!Number.isSafeInteger(pointAmount) || pointAmount < 1) {
    throw new Error("POINT_DEBIT_REQUIRES_WHOLE_POINT");
  }
  if (pointAmount > maxPoints) throw new Error("POINT_DEBIT_AMOUNT_TOO_LARGE");
  return pointAmount;
}

function fingerprint(userId: string, requestId: string, pointAmount: number): string {
  return createHash("sha256")
    .update(JSON.stringify([
      "ywonder-game-point-debit-v1",
      userId,
      requestId,
      pointAmount,
      "USDT",
    ]), "utf8")
    .digest("hex");
}

function deterministicIds(requestId: string) {
  const suffix = requestId.replace(/-/g, "");
  return {
    id: `gpd_${suffix}`,
    reservationId: `gpdr_${suffix}`,
    sourceTransactionId: `gpdt_${suffix}`,
  };
}

function commandInput(row: any): GamePointReservationInput {
  return {
    reservationId: row.reservationId,
    webUserId: row.userId,
    expectedPlayerId: row.gamePlayerId,
    pointAmount: row.pointAmount,
    purpose: row.purpose,
    source: row.source,
    occurredAt: new Date(row.occurredAt).toISOString(),
  };
}

function retryAt(attempts: number): Date {
  const exponent = Math.min(Math.max(Number(attempts) || 0, 0), 7);
  return new Date(Date.now() + Math.min(300_000, 1000 * (2 ** exponent)));
}

function parkedAt(): Date {
  return new Date(PARKED_UNTIL);
}

async function markCommandFailure(row: any, error: unknown): Promise<any> {
  const code = safeError(error);
  const retryable = error instanceof GamePointCommandError && error.retryable;
  let status = row.status;
  if (!retryable) {
    status = row.status === "RESERVE_PENDING" && code === "INSUFFICIENT_BALANCE"
      ? "REJECTED"
      : "MANUAL_REVIEW";
  }
  await db.gamePointDebit.updateMany({
    where: { id: row.id, status: row.status },
    data: {
      status,
      attempts: { increment: 1 },
      lastError: code,
      nextAttemptAt: retryable ? retryAt(row.attempts) : parkedAt(),
    },
  });
  return db.gamePointDebit.findUnique({ where: { id: row.id } });
}

async function markLocalPersistenceFailure(
  row: any,
  phase: "RESERVE" | "CAPTURE" | "RELEASE",
  error: unknown
): Promise<any> {
  console.error(JSON.stringify({
    event: "game_point_debit_local_state_write_failed",
    phase,
    debitId: row.id,
    error: safeError(error),
  }));
  return markCommandFailure(
    row,
    new GamePointCommandError(`POINT_DEBIT_${phase}_STATE_WRITE_FAILED`, true)
  );
}

function validateSettlementTransaction(row: any, transaction: any): boolean {
  if (!transaction || transaction.id !== row.sourceTransactionId
      || transaction.userId !== row.userId || transaction.type !== "SWAP"
      || transaction.currency !== row.targetCurrency
      || transaction.status !== "PENDING") {
    return false;
  }
  try {
    const expectedAmount = microsTextToNumber(row.netTargetMicros, "INVALID_USDT_MICROS");
    if (!Number.isFinite(transaction.amount)
        || Math.abs(Number(transaction.amount) - expectedAmount) > 0.000000001) {
      return false;
    }
    const metadata = JSON.parse(String(transaction.metadata || "{}"));
    return metadata.debitId === row.id
      && metadata.requestFingerprint === row.requestFingerprint
      && metadata.pointMicros === row.pointMicros
      && metadata.netTargetMicros === row.netTargetMicros
      && metadata.rateVersionId === row.rateVersionId;
  } catch {
    return false;
  }
}

async function reconcileSettlement(row: any, settlementError?: unknown): Promise<any> {
  const current = await db.gamePointDebit.findUnique({ where: { id: row.id } });
  if (!current || current.status !== "RESERVED") return current;
  const transaction = await db.transaction.findUnique({
    where: { id: current.sourceTransactionId },
  });
  if (transaction) {
    const valid = validateSettlementTransaction(current, transaction);
    await db.gamePointDebit.updateMany({
      where: { id: current.id, status: "RESERVED" },
      data: valid
        ? { status: "CAPTURE_PENDING", settledAt: current.settledAt || new Date(), lastError: null }
        : {
            status: "MANUAL_REVIEW",
            lastError: "POINT_DEBIT_SETTLEMENT_IDEMPOTENCY_CONFLICT",
            nextAttemptAt: parkedAt(),
          },
    });
  } else {
    await db.gamePointDebit.updateMany({
      where: { id: current.id, status: "RESERVED" },
      data: {
        status: "RELEASE_PENDING",
        lastError: safeError(settlementError || "POINT_DEBIT_SETTLEMENT_FAILED"),
        nextAttemptAt: new Date(),
      },
    });
  }
  return db.gamePointDebit.findUnique({ where: { id: current.id } });
}

async function settleReserved(row: any): Promise<any> {
  try {
    await db.$transaction(async (txdb: any) => {
      const current = await txdb.gamePointDebit.findUnique({ where: { id: row.id } });
      if (!current || current.status !== "RESERVED") return;
      const existing = await txdb.transaction.findUnique({
        where: { id: current.sourceTransactionId },
      });
      if (existing) {
        if (!validateSettlementTransaction(current, existing)) {
          throw new Error("POINT_DEBIT_SETTLEMENT_IDEMPOTENCY_CONFLICT");
        }
      } else {
        if (current.targetCurrency !== "USDT") {
          throw new Error("POINT_DEBIT_TARGET_NOT_CONFIGURED");
        }
        const netAmount = microsTextToNumber(current.netTargetMicros, "INVALID_USDT_MICROS");
        const wallet = await txdb.wallet.findUnique({
          where: { userId: current.userId },
          select: { id: true },
        });
        if (!wallet) throw new Error("POINT_DEBIT_WALLET_MISSING");
        await txdb.transaction.create({
          data: {
            id: current.sourceTransactionId,
            userId: current.userId,
            type: "SWAP",
            amount: netAmount,
            currency: "USDT",
            status: "PENDING",
            metadata: JSON.stringify({
              from: "POINT",
              to: "USDT",
              authority: "game",
              debitId: current.id,
              reservationId: current.reservationId,
              requestId: current.requestId,
              requestFingerprint: current.requestFingerprint,
              pointMicros: current.pointMicros,
              grossTargetMicros: current.grossTargetMicros,
              feeBps: current.feeBps,
              feeMicros: current.feeMicros,
              netTargetMicros: current.netTargetMicros,
              rateVersionId: current.rateVersionId,
              rateMicros: current.rateMicros,
              roundingRemainder: current.roundingRemainder,
              feeRoundingRemainder: current.feeRoundingRemainder,
            }),
          },
        });
      }
      const transitioned = await txdb.gamePointDebit.updateMany({
        where: { id: current.id, status: "RESERVED" },
        data: {
          status: "CAPTURE_PENDING",
          settledAt: current.settledAt || new Date(),
          lastError: null,
          nextAttemptAt: new Date(),
        },
      });
      if (transitioned.count !== 1) throw new Error("POINT_DEBIT_SETTLEMENT_TRANSITION_RACE");
    });
  } catch (error) {
    return reconcileSettlement(row, error);
  }
  return db.gamePointDebit.findUnique({ where: { id: row.id } });
}

async function sendReserve(row: any): Promise<any> {
  let result: Awaited<ReturnType<typeof sendGamePointReservationCommand>>;
  try {
    result = await sendGamePointReservationCommand("reserve", commandInput(row));
  } catch (error) {
    return markCommandFailure(row, error);
  }
  try {
    if (result.status === "CAPTURED") {
      const transaction = await db.transaction.findUnique({
        where: { id: row.sourceTransactionId },
      });
      await db.gamePointDebit.updateMany({
        where: { id: row.id, status: "RESERVE_PENDING" },
        data: transaction && validateSettlementTransaction(row, transaction)
          ? { status: "CAPTURE_PENDING", reservedAt: new Date(), lastError: null }
          : {
              status: "MANUAL_REVIEW",
              lastError: "POINT_DEBIT_CAPTURE_WITHOUT_SETTLEMENT",
              nextAttemptAt: parkedAt(),
            },
      });
    } else {
      await db.gamePointDebit.updateMany({
        where: { id: row.id, status: "RESERVE_PENDING" },
        data: {
          status: result.status === "RELEASED" ? "RELEASED" : "RESERVED",
          attempts: { increment: 1 },
          reservedAt: result.status === "RESERVED" ? new Date() : row.reservedAt,
          releasedAt: result.status === "RELEASED" ? new Date() : row.releasedAt,
          lastError: null,
          nextAttemptAt: new Date(),
        },
      });
    }
  } catch (error) {
    return markLocalPersistenceFailure(row, "RESERVE", error);
  }
  return db.gamePointDebit.findUnique({ where: { id: row.id } });
}

async function sendCapture(row: any): Promise<any> {
  try {
    await sendGamePointReservationCommand("capture", commandInput(row));
  } catch (error) {
    return markCommandFailure(row, error);
  }
  try {
    await db.$transaction(async (txdb: any) => {
      const current = await txdb.gamePointDebit.findUnique({ where: { id: row.id } });
      if (!current || current.status !== "CAPTURE_PENDING") return;
      const settlement = await txdb.transaction.findUnique({
        where: { id: current.sourceTransactionId },
      });
      if (!validateSettlementTransaction(current, settlement)) {
        throw new Error("POINT_DEBIT_CAPTURE_SETTLEMENT_JOURNAL_INVALID");
      }
      const netAmount = microsTextToNumber(current.netTargetMicros, "INVALID_USDT_MICROS");
      const creditedWallet = await txdb.wallet.updateMany({
        where: { userId: current.userId },
        data: { balanceUsdt: { increment: netAmount } },
      });
      if (creditedWallet.count !== 1) {
        throw new Error("POINT_DEBIT_CAPTURE_WALLET_MISSING");
      }
      const transitioned = await txdb.gamePointDebit.updateMany({
        where: { id: current.id, status: "CAPTURE_PENDING" },
        data: {
          status: "CAPTURED",
          attempts: { increment: 1 },
          capturedAt: new Date(),
          lastError: null,
          nextAttemptAt: parkedAt(),
        },
      });
      if (transitioned.count !== 1) {
        throw new Error("POINT_DEBIT_CAPTURE_TRANSITION_RACE");
      }
      const settled = await txdb.transaction.updateMany({
        where: { id: current.sourceTransactionId, status: "PENDING" },
        data: { status: "SUCCESS" },
      });
      if (settled.count !== 1) {
        throw new Error("POINT_DEBIT_CAPTURE_SETTLEMENT_TRANSITION_RACE");
      }
    });
  } catch (error) {
    const code = safeError(error);
    if ([
      "POINT_DEBIT_CAPTURE_SETTLEMENT_JOURNAL_INVALID",
      "POINT_DEBIT_CAPTURE_WALLET_MISSING",
    ].includes(code)) {
      return markCommandFailure(row, new GamePointCommandError(code, false));
    }
    return markLocalPersistenceFailure(row, "CAPTURE", error);
  }
  return db.gamePointDebit.findUnique({ where: { id: row.id } });
}

async function sendRelease(row: any): Promise<any> {
  let transaction: any;
  try {
    transaction = await db.transaction.findUnique({ where: { id: row.sourceTransactionId } });
  } catch (error) {
    return markLocalPersistenceFailure(row, "RELEASE", error);
  }
  if (transaction) {
    try {
      await db.gamePointDebit.updateMany({
        where: { id: row.id, status: "RELEASE_PENDING" },
        data: validateSettlementTransaction(row, transaction)
          ? { status: "CAPTURE_PENDING", lastError: null, nextAttemptAt: new Date() }
          : {
              status: "MANUAL_REVIEW",
              lastError: "POINT_DEBIT_SETTLEMENT_IDEMPOTENCY_CONFLICT",
              nextAttemptAt: parkedAt(),
            },
      });
    } catch (error) {
      return markLocalPersistenceFailure(row, "RELEASE", error);
    }
    return db.gamePointDebit.findUnique({ where: { id: row.id } });
  }
  try {
    await sendGamePointReservationCommand("release", commandInput(row));
  } catch (error) {
    return markCommandFailure(row, error);
  }
  try {
    await db.gamePointDebit.updateMany({
      where: { id: row.id, status: "RELEASE_PENDING" },
      data: {
        status: "RELEASED",
        attempts: { increment: 1 },
        releasedAt: new Date(),
        lastError: null,
        nextAttemptAt: parkedAt(),
      },
    });
  } catch (error) {
    return markLocalPersistenceFailure(row, "RELEASE", error);
  }
  return db.gamePointDebit.findUnique({ where: { id: row.id } });
}

export async function progressGamePointDebitById(id: string): Promise<any> {
  let row = await db.gamePointDebit.findUnique({ where: { id } });
  if (row && !gamePointDebitTransportAllows(row.userId)) return row;
  if (row && RETRYABLE_STATUSES.includes(row.status as typeof RETRYABLE_STATUSES[number])
      && new Date(row.nextAttemptAt).getTime() > Date.now()) {
    return row;
  }
  for (let step = 0; row && step < 6; step += 1) {
    const previousStatus = row.status;
    if (row.status === "RESERVE_PENDING") row = await sendReserve(row);
    else if (row.status === "RESERVED") row = await settleReserved(row);
    else if (row.status === "CAPTURE_PENDING") row = await sendCapture(row);
    else if (row.status === "RELEASE_PENDING") row = await sendRelease(row);
    else break;
    if (row?.status === previousStatus
        && new Date(row.nextAttemptAt).getTime() > Date.now()) {
      break;
    }
  }
  return row;
}

function resultFromRow(row: any, duplicate: boolean): GamePointDebitResult {
  if (!row) return { ok: false, error: "POINT_DEBIT_JOURNAL_MISSING" };
  if (row.status === "CAPTURED") {
    return {
      ok: true,
      usdt: microsTextToNumber(row.netTargetMicros, "INVALID_USDT_MICROS"),
      duplicate,
      terminal: true,
      debitStatus: row.status,
    };
  }
  if (row.status === "REJECTED" || row.status === "RELEASED") {
    return {
      ok: false,
      error: row.lastError || (row.status === "REJECTED" ? "POINT_DEBIT_REJECTED" : "POINT_DEBIT_RELEASED"),
      duplicate,
      terminal: true,
      debitStatus: row.status,
    };
  }
  if (row.status === "MANUAL_REVIEW") {
    return {
      ok: false,
      error: row.lastError || "POINT_DEBIT_MANUAL_REVIEW",
      pending: true,
      duplicate,
      debitStatus: row.status,
    };
  }
  return {
    ok: true,
    pending: true,
    duplicate,
    debitStatus: row.status,
  };
}

export async function convertLinkedGamePointToUsdt(input: {
  userId: string;
  pointAmount: number;
  requestId: unknown;
}): Promise<GamePointDebitResult> {
  const config = readDebitConfig();
  if (!config.enabled || config.feeBps == null) {
    return { ok: false, error: "GAME_POINT_DEBIT_NOT_CONFIGURED", terminal: true };
  }
  let requestId: string;
  let pointAmount: number;
  try {
    requestId = normalizeGamePointConversionRequestId(input.requestId);
    pointAmount = normalizeWholePoint(input.pointAmount, config.maxPoints);
  } catch (error) {
    return { ok: false, error: safeError(error), terminal: true };
  }
  const requestFingerprint = fingerprint(input.userId, requestId, pointAmount);
  let row = await db.gamePointDebit.findUnique({ where: { requestId } });
  let duplicate = Boolean(row);
  if (row) {
    if (row.userId !== input.userId || row.requestFingerprint !== requestFingerprint) {
      return { ok: false, error: "IDEMPOTENCY_CONFLICT", terminal: true };
    }
  } else {
    const authority = await resolveGamePointConversionAuthority(input.userId);
    if (authority.mode !== "game") {
      return {
        ok: false,
        error: authority.error || "GAME_POINT_LINK_REQUIRED",
        terminal: true,
      };
    }
    const linked = await getGamePointLinkedAccount(input.userId);
    if (!linked) return { ok: false, error: "GAME_POINT_LINK_REQUIRED", terminal: true };
    const activeConversion = await db.gamePointConversion.findFirst({
      where: {
        userId: input.userId,
        status: { notIn: ["SENT", "REFUNDED"] },
      },
      select: { id: true },
    });
    if (activeConversion) {
      return {
        ok: false,
        error: "GAME_POINT_WALLET_OPERATION_ALREADY_PENDING",
        terminal: true,
      };
    }
    try {
      const activeRate = await getActiveUsdtPointRate();
      const pointMicros = String(pointAmount * MICROS_SCALE);
      const quote = quotePointToUsdtMicros(pointMicros, activeRate.rateMicros, config.feeBps);
      const ids = deterministicIds(requestId);
      const occurredAt = new Date();
      row = await db.gamePointDebit.create({
        data: {
          ...ids,
          requestId,
          userId: input.userId,
          gamePlayerId: linked.gamePlayerId,
          targetCurrency: "USDT",
          pointAmount,
          pointMicros,
          grossTargetMicros: quote.grossUsdtMicros,
          feeBps: config.feeBps,
          feeMicros: quote.feeMicros,
          netTargetMicros: quote.netUsdtMicros,
          rateVersionId: activeRate.id,
          rateMicros: activeRate.rateMicros,
          roundingRemainder: quote.roundingRemainder,
          feeRoundingRemainder: quote.feeRoundingRemainder,
          requestFingerprint,
          purpose: "point_to_usdt",
          source: DEBIT_SOURCE,
          occurredAt,
        },
      });
    } catch (error) {
      row = await db.gamePointDebit.findUnique({ where: { requestId } });
      if (!row) {
        if (errorIncludes(error, "GAME_POINT_WALLET_OPERATION_ALREADY_PENDING")) {
          return {
            ok: false,
            error: "GAME_POINT_WALLET_OPERATION_ALREADY_PENDING",
            terminal: true,
          };
        }
        const unresolved = await db.gamePointDebit.findFirst({
          where: { userId: input.userId, status: { notIn: [...TERMINAL_STATUSES] } },
        });
        return {
          ok: false,
          error: unresolved ? "GAME_POINT_DEBIT_ALREADY_PENDING" : safeError(error),
          terminal: true,
        };
      }
      duplicate = true;
      if (row.userId !== input.userId || row.requestFingerprint !== requestFingerprint) {
        return { ok: false, error: "IDEMPOTENCY_CONFLICT", terminal: true };
      }
    }
  }
  row = await progressGamePointDebitById(row.id);
  return resultFromRow(row, duplicate);
}

export async function retryPendingGamePointDebits(limit = 25) {
  const safeLimit = Math.max(1, Math.min(Number(limit) || 25, 100));
  const rows = await db.gamePointDebit.findMany({
    where: {
      status: { in: [...RETRYABLE_STATUSES] },
      nextAttemptAt: { lte: new Date() },
    },
    orderBy: [{ nextAttemptAt: "asc" }, { createdAt: "asc" }],
    take: safeLimit,
  });
  let completed = 0;
  let pending = 0;
  let paused = 0;
  for (const row of rows) {
    if (!gamePointDebitTransportAllows(row.userId)) {
      paused += 1;
      continue;
    }
    const current = await progressGamePointDebitById(row.id);
    if (current?.status === "CAPTURED" || current?.status === "RELEASED"
        || current?.status === "REJECTED") completed += 1;
    else pending += 1;
  }
  return { processed: rows.length - paused, completed, pending, paused };
}

export async function getGamePointDebitHealth() {
  const [retryable, manualReview, captured, released] = await Promise.all([
    db.gamePointDebit.count({ where: { status: { in: [...RETRYABLE_STATUSES] } } }),
    db.gamePointDebit.count({ where: { status: "MANUAL_REVIEW" } }),
    db.gamePointDebit.count({ where: { status: "CAPTURED" } }),
    db.gamePointDebit.count({ where: { status: "RELEASED" } }),
  ]);
  return { retryable, manualReview, captured, released };
}
