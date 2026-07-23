"use client";

const KEY_PREFIX = "ywonder:point-debit-intent:v1:";

export type GamePointDebitIntent = {
  requestId: string;
  userId: string;
  pointAmount: number;
  targetCurrency: "USDT";
  createdAt: string;
};

function storageKey(userId: string): string {
  return `${KEY_PREFIX}${encodeURIComponent(String(userId || "").trim())}`;
}

function validRequestId(value: unknown): value is string {
  return /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i
    .test(String(value || ""));
}

function normalizePointAmount(value: number): number {
  const pointAmount = Number(value);
  if (!Number.isSafeInteger(pointAmount) || pointAmount < 1) {
    throw new Error("POINT_DEBIT_REQUIRES_WHOLE_POINT");
  }
  return pointAmount;
}

export function readGamePointDebitIntent(userId: string): GamePointDebitIntent | null {
  const normalizedUserId = String(userId || "").trim();
  if (typeof window === "undefined" || !normalizedUserId) return null;
  try {
    const raw = window.localStorage.getItem(storageKey(normalizedUserId));
    if (!raw) return null;
    const parsed = JSON.parse(raw) as Partial<GamePointDebitIntent>;
    const createdAt = String(parsed.createdAt || "");
    if (parsed.userId !== normalizedUserId || !validRequestId(parsed.requestId)
        || parsed.targetCurrency !== "USDT" || !Number.isFinite(Date.parse(createdAt))) {
      return null;
    }
    return {
      requestId: parsed.requestId,
      userId: normalizedUserId,
      pointAmount: normalizePointAmount(Number(parsed.pointAmount)),
      targetCurrency: "USDT",
      createdAt,
    };
  } catch {
    return null;
  }
}

export function getOrCreateGamePointDebitIntent(
  userId: string,
  pointAmount: number
): GamePointDebitIntent {
  const normalizedUserId = String(userId || "").trim();
  if (!normalizedUserId) throw new Error("GAME_POINT_DEBIT_USER_REQUIRED");
  const normalizedPointAmount = normalizePointAmount(pointAmount);
  const existing = readGamePointDebitIntent(normalizedUserId);
  if (existing) {
    if (existing.pointAmount !== normalizedPointAmount) {
      throw new Error("GAME_POINT_DEBIT_INTENT_AMOUNT_MISMATCH");
    }
    return existing;
  }
  if (typeof window === "undefined") throw new Error("GAME_POINT_DEBIT_BROWSER_REQUIRED");

  const intent: GamePointDebitIntent = {
    requestId: crypto.randomUUID(),
    userId: normalizedUserId,
    pointAmount: normalizedPointAmount,
    targetCurrency: "USDT",
    createdAt: new Date().toISOString(),
  };
  window.localStorage.setItem(storageKey(normalizedUserId), JSON.stringify(intent));
  return intent;
}

export function clearGamePointDebitIntent(userId: string, requestId: string): void {
  if (typeof window === "undefined") return;
  const existing = readGamePointDebitIntent(userId);
  if (existing?.requestId === requestId) {
    window.localStorage.removeItem(storageKey(userId));
  }
}
