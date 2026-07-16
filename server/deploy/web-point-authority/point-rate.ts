import { db } from "@/lib/db";

export const POINT_RATE_PAIR = "USDT_POINT";
const MICROS_SCALE = BigInt(1_000_000);
const MAX_RATE_MICROS = BigInt("1000000000000000");
const MAX_POINT_MICROS = BigInt(1_000_000_000) * MICROS_SCALE;

function parsePositiveMicros(value: string, errorCode: string): bigint {
  const text = String(value || "").trim();
  if (!/^[1-9]\d*$/.test(text)) throw new Error(errorCode);
  const parsed = BigInt(text);
  if (parsed < BigInt(1)) throw new Error(errorCode);
  return parsed;
}

export function decimalRateToMicrosText(value: number | string): string {
  const numeric = Number(value);
  if (!Number.isFinite(numeric) || numeric <= 0 || numeric > 1_000_000_000) {
    throw new Error("INVALID_POINT_RATE");
  }
  const fixed = numeric.toFixed(6);
  if (Math.abs(Number(fixed) - numeric) > 0.000000001) {
    throw new Error("POINT_RATE_PRECISION_EXCEEDED");
  }
  const [whole, fraction = ""] = fixed.split(".");
  const micros = BigInt(whole) * MICROS_SCALE
    + BigInt(fraction.padEnd(6, "0").slice(0, 6));
  if (micros < BigInt(1) || micros > MAX_RATE_MICROS) throw new Error("INVALID_POINT_RATE");
  return micros.toString();
}

export function pointMicrosToAmountText(value: string): string {
  const micros = parsePositiveMicros(value, "INVALID_POINT_MICROS");
  const whole = micros / MICROS_SCALE;
  const fraction = String(micros % MICROS_SCALE).padStart(6, "0");
  return `${whole}.${fraction}`;
}

export function pointMicrosToNumber(value: string): number {
  const micros = parsePositiveMicros(value, "INVALID_POINT_MICROS");
  if (micros > BigInt(Number.MAX_SAFE_INTEGER)) throw new Error("POINT_AMOUNT_TOO_LARGE");
  return Number(micros) / Number(MICROS_SCALE);
}

export function quoteUsdtToPointMicros(usdtMicrosText: string, rateMicrosText: string) {
  const usdtMicros = parsePositiveMicros(usdtMicrosText, "INVALID_USDT_MICROS");
  const rateMicros = parsePositiveMicros(rateMicrosText, "INVALID_POINT_RATE");
  if (rateMicros > MAX_RATE_MICROS) throw new Error("INVALID_POINT_RATE");
  const raw = usdtMicros * rateMicros;
  const pointMicros = raw / MICROS_SCALE;
  const roundingRemainder = raw % MICROS_SCALE;
  if (pointMicros < BigInt(1) || pointMicros > MAX_POINT_MICROS) {
    throw new Error("POINT_AMOUNT_TOO_LARGE");
  }
  return {
    pointMicros: pointMicros.toString(),
    pointAmount: pointMicrosToAmountText(pointMicros.toString()),
    roundingRemainder: roundingRemainder.toString(),
  };
}

export async function getActiveUsdtPointRate(txdb: any = db) {
  const current = await txdb.pointExchangeRateVersion.findFirst({
    where: {
      pair: POINT_RATE_PAIR,
      isActive: true,
      effectiveAt: { lte: new Date() },
    },
    orderBy: [{ effectiveAt: "desc" }, { createdAt: "desc" }],
  });
  if (!current) throw new Error("ACTIVE_POINT_RATE_NOT_CONFIGURED");
  parsePositiveMicros(current.rateMicros, "INVALID_POINT_RATE");
  return current;
}

export async function replaceActiveUsdtPointRate(
  txdb: any,
  rate: number,
  createdBy: string,
  sourceRateId?: string
) {
  const rateMicros = decimalRateToMicrosText(rate);
  await txdb.pointExchangeRateVersion.updateMany({
    where: { pair: POINT_RATE_PAIR, isActive: true },
    data: { isActive: false },
  });
  return txdb.pointExchangeRateVersion.create({
    data: {
      pair: POINT_RATE_PAIR,
      rateMicros,
      isActive: true,
      effectiveAt: new Date(),
      createdBy,
      sourceRateId: sourceRateId || null,
    },
  });
}
