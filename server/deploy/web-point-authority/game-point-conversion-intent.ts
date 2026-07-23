"use client";

const KEY_PREFIX = "ywonder:point-conversion-intent:v1:";
const MICROS_SCALE = 1_000_000;

export type GamePointConversionIntent = {
  requestId: string;
  userId: string;
  usdtMicros: string;
  amount: number;
  createdAt: string;
};

function storageKey(userId: string): string {
  return `${KEY_PREFIX}${encodeURIComponent(String(userId || "").trim())}`;
}

function amountMicros(amount: number): string {
  const micros = Math.round(Number(amount) * MICROS_SCALE);
  if (!Number.isSafeInteger(micros) || micros < 1) {
    throw new Error("INVALID_USDT_AMOUNT");
  }
  return String(micros);
}

function validRequestId(value: unknown): value is string {
  return /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i
    .test(String(value || ""));
}

export function readGamePointConversionIntent(userId: string): GamePointConversionIntent | null {
  if (typeof window === "undefined" || !String(userId || "").trim()) return null;
  try {
    const raw = window.localStorage.getItem(storageKey(userId));
    if (!raw) return null;
    const parsed = JSON.parse(raw) as Partial<GamePointConversionIntent>;
    const micros = String(parsed.usdtMicros || "");
    const createdAt = String(parsed.createdAt || "");
    if (parsed.userId !== userId || !validRequestId(parsed.requestId)
        || !/^[1-9]\d*$/.test(micros) || !Number.isFinite(Date.parse(createdAt))) {
      return null;
    }
    const numericMicros = Number(micros);
    if (!Number.isSafeInteger(numericMicros) || numericMicros < 1) return null;
    return {
      requestId: parsed.requestId,
      userId,
      usdtMicros: micros,
      amount: numericMicros / MICROS_SCALE,
      createdAt,
    };
  } catch {
    return null;
  }
}

export function getOrCreateGamePointConversionIntent(
  userId: string,
  amount: number
): GamePointConversionIntent {
  const normalizedUserId = String(userId || "").trim();
  if (!normalizedUserId) throw new Error("GAME_POINT_CONVERSION_USER_REQUIRED");
  const requestedMicros = amountMicros(amount);
  const existing = readGamePointConversionIntent(normalizedUserId);
  if (existing) return existing;
  if (typeof window === "undefined") throw new Error("GAME_POINT_CONVERSION_BROWSER_REQUIRED");

  const intent: GamePointConversionIntent = {
    requestId: crypto.randomUUID(),
    userId: normalizedUserId,
    usdtMicros: requestedMicros,
    amount: Number(requestedMicros) / MICROS_SCALE,
    createdAt: new Date().toISOString(),
  };
  window.localStorage.setItem(storageKey(normalizedUserId), JSON.stringify(intent));
  return intent;
}

export function clearGamePointConversionIntent(userId: string, requestId: string): void {
  if (typeof window === "undefined") return;
  const existing = readGamePointConversionIntent(userId);
  if (existing?.requestId === requestId) {
    window.localStorage.removeItem(storageKey(userId));
  }
}
