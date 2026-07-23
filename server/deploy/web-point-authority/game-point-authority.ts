import { createHmac, randomUUID } from "crypto";
import { db } from "@/lib/db";

const MICROS_SCALE = 1_000_000;

export type GamePointWalletView = {
  linked: boolean;
  available: boolean;
  point: number | null;
  playerId?: string;
  error?: string;
};

export type GamePointConversionAuthority = {
  mode: "game" | "legacy-web" | "blocked";
  linked: boolean;
  error?: string;
};

export type GamePointLinkedAccount = {
  userId: string;
  gamePlayerId: string;
};

export type GamePointReservationOperation = "reserve" | "capture" | "release";

export type GamePointReservationInput = {
  reservationId: string;
  webUserId: string;
  expectedPlayerId: string;
  pointAmount: number;
  purpose: string;
  source: string;
  occurredAt: string;
};

export type GamePointReservationResult = {
  duplicate: boolean;
  operation: GamePointReservationOperation;
  playerId: string;
  point: number;
  status: "RESERVED" | "CAPTURED" | "RELEASED";
};

export class GamePointCommandError extends Error {
  readonly retryable: boolean;
  readonly httpStatus: number | null;

  constructor(code: string, retryable: boolean, httpStatus: number | null = null) {
    super(code);
    this.name = "GamePointCommandError";
    this.retryable = retryable;
    this.httpStatus = httpStatus;
  }
}

function enabled(): boolean {
  return String(process.env.WEB_TOPUP_ENABLED || "").trim().toLowerCase() === "true";
}

function debitEnabled(): boolean {
  return String(process.env.WEB_POINT_WALLET_DEBIT_ENABLED || "")
    .trim()
    .toLowerCase() === "true";
}

function mode(): "canary" | "open" {
  return String(process.env.WEB_TOPUP_MODE || "canary").trim().toLowerCase() === "open"
    ? "open"
    : "canary";
}

function allowedWebUserIds(): Set<string> {
  return new Set(String(process.env.WEB_TOPUP_ALLOWED_WEB_USER_IDS || "")
    .split(",")
    .map((entry) => entry.trim())
    .filter(Boolean));
}

function transportAllows(userId: string): boolean {
  return enabled() && (mode() === "open" || allowedWebUserIds().has(userId));
}

export function gamePointDebitTransportAllows(userId: string): boolean {
  return debitEnabled() && transportAllows(userId);
}

export async function getGamePointLinkedAccount(
  userId: string
): Promise<GamePointLinkedAccount | null> {
  return db.gamePointLinkedAccount.findUnique({
    where: { userId },
    select: { userId: true, gamePlayerId: true },
  });
}

export async function isGamePointLinkedAccount(userId: string): Promise<boolean> {
  return Boolean(await getGamePointLinkedAccount(userId));
}

export async function resolveGamePointConversionAuthority(
  userId: string
): Promise<GamePointConversionAuthority> {
  const linked = await isGamePointLinkedAccount(userId);
  const transportAllowed = transportAllows(userId);
  if (linked && !transportAllowed) {
    return { mode: "blocked", linked: true, error: "GAME_POINT_SYNC_UNAVAILABLE" };
  }
  if (!linked && transportAllowed) {
    return { mode: "blocked", linked: false, error: "GAME_POINT_LINK_REQUIRED" };
  }
  return linked
    ? { mode: "game", linked: true }
    : { mode: "legacy-web", linked: false };
}

export function normalizeGamePointConversionRequestId(value: unknown): string {
  const requestId = String(value || "").trim().toLowerCase();
  if (!/^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/.test(requestId)) {
    throw new Error("INVALID_GAME_POINT_CONVERSION_REQUEST_ID");
  }
  return requestId;
}

export function gamePointConversionTransactionId(requestId: string): string {
  return `gpc_${normalizeGamePointConversionRequestId(requestId).replace(/-/g, "")}`;
}

export function normalizeUsdtMicros(value: number): { amount: number; micros: string } {
  if (!Number.isFinite(value) || value <= 0) throw new Error("INVALID_USDT_AMOUNT");
  const micros = Math.round(value * MICROS_SCALE);
  if (!Number.isSafeInteger(micros) || micros < 1
      || Math.abs((micros / MICROS_SCALE) - value) > 0.000000001) {
    throw new Error("INVALID_USDT_AMOUNT");
  }
  return { amount: micros / MICROS_SCALE, micros: String(micros) };
}

export function pointAmountTextToMicros(value: string): string {
  const match = /^(0|[1-9]\d*)\.(\d{6})$/.exec(String(value || ""));
  if (!match) throw new Error("INVALID_GAME_POINT_AMOUNT");
  const micros = Number(match[1]) * MICROS_SCALE + Number(match[2]);
  if (!Number.isSafeInteger(micros) || micros < 1) throw new Error("INVALID_GAME_POINT_AMOUNT");
  return String(micros);
}

function internalEndpoint(pathname: string): string {
  const configured = String(process.env.GAME_POINT_SYNC_URL || "").trim();
  if (!configured) throw new Error("GAME_POINT_SYNC_NOT_CONFIGURED");
  const url = new URL(configured);
  const host = url.hostname.toLowerCase();
  if (url.protocol !== "http:" || !["127.0.0.1", "::1", "localhost"].includes(host)) {
    throw new Error("GAME_POINT_SYNC_MUST_USE_LOOPBACK");
  }
  if (url.username || url.password || url.search || url.hash
      || url.pathname.replace(/\/+$/, "") !== "/internal/web/point-credit") {
    throw new Error("INVALID_GAME_POINT_SYNC_URL");
  }
  url.pathname = pathname;
  return url.toString();
}

function balanceEndpoint(): string {
  return internalEndpoint("/internal/web/point-balance");
}

function reservationEndpoint(operation: GamePointReservationOperation): string {
  return internalEndpoint(`/internal/web/point-${operation}`);
}

function canonicalBalance(timestamp: string, requestId: string, webUserId: string): string {
  return JSON.stringify(["ywonder-point-balance-v1", timestamp, requestId, webUserId]);
}

function canonicalReservation(
  timestamp: string,
  operation: GamePointReservationOperation,
  input: GamePointReservationInput
): string {
  return JSON.stringify([
    "ywonder-point-reservation-v1",
    timestamp,
    operation,
    input.reservationId,
    input.webUserId,
    input.expectedPlayerId,
    String(input.pointAmount),
    input.purpose,
    input.source,
    input.occurredAt,
  ]);
}

function safeError(error: unknown): string {
  const message = error instanceof Error ? error.message : String(error || "UNKNOWN_ERROR");
  return message.replace(/[\r\n\0]/g, " ").slice(0, 160);
}

function stableRemoteError(value: unknown): string {
  const code = String(value || "GAME_POINT_RESERVATION_REJECTED").trim();
  return /^[A-Z][A-Z0-9_]{2,95}$/.test(code)
    ? code
    : "GAME_POINT_RESERVATION_REJECTED";
}

export async function sendGamePointReservationCommand(
  operation: GamePointReservationOperation,
  input: GamePointReservationInput
): Promise<GamePointReservationResult> {
  if (!gamePointDebitTransportAllows(input.webUserId)) {
    throw new GamePointCommandError("GAME_POINT_DEBIT_UNAVAILABLE", true);
  }
  if (!Number.isSafeInteger(input.pointAmount) || input.pointAmount < 1) {
    throw new GamePointCommandError("POINT_DEBIT_REQUIRES_WHOLE_POINT", false);
  }
  const linked = await getGamePointLinkedAccount(input.webUserId);
  if (!linked || linked.gamePlayerId !== input.expectedPlayerId) {
    throw new GamePointCommandError("GAME_POINT_IDENTITY_MISMATCH", false);
  }

  const secret = String(process.env.WEB_TOPUP_SECRET || "");
  if (secret.length < 32) {
    throw new GamePointCommandError("WEB_TOPUP_SECRET_NOT_CONFIGURED", true);
  }
  const timestamp = String(Math.floor(Date.now() / 1000));
  const signature = createHmac("sha256", secret)
    .update(canonicalReservation(timestamp, operation, input), "utf8")
    .digest("hex");
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), 5000);
  let response: Response;
  try {
    response = await fetch(reservationEndpoint(operation), {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "X-YWonder-Timestamp": timestamp,
        "X-YWonder-Signature": signature,
      },
      body: JSON.stringify({
        reservation_id: input.reservationId,
        web_user_id: input.webUserId,
        expected_player_id: input.expectedPlayerId,
        point_amount: input.pointAmount,
        purpose: input.purpose,
        source: input.source,
        occurred_at: input.occurredAt,
      }),
      cache: "no-store",
      signal: controller.signal,
    });
  } catch (error) {
    const code = error instanceof Error && error.name === "AbortError"
      ? "GAME_POINT_RESERVATION_TIMEOUT"
      : "GAME_POINT_RESERVATION_TRANSPORT_ERROR";
    throw new GamePointCommandError(code, true);
  } finally {
    clearTimeout(timeout);
  }

  let body: any = null;
  try {
    body = await response.json();
  } catch {
    body = null;
  }
  if (!response.ok) {
    const code = stableRemoteError(body?.error || `GAME_POINT_RESERVATION_HTTP_${response.status}`);
    const retryable = response.status === 404 || response.status === 425
      || response.status === 429 || response.status >= 500
      || code === "WEB_TOPUP_CANARY_USER_NOT_ALLOWED";
    throw new GamePointCommandError(code, retryable, response.status);
  }

  const reservation = body?.reservation;
  const point = Number(body?.economy?.pos);
  const status = String(reservation?.status || "");
  const expectedStatus = operation === "capture"
    ? "CAPTURED"
    : operation === "release" ? "RELEASED" : null;
  if (body?.ok !== true || body?.operation !== operation
      || body?.player_id !== input.expectedPlayerId
      || !Number.isSafeInteger(point) || point < 0
      || reservation?.id !== input.reservationId
      || reservation?.playerId !== input.expectedPlayerId
      || reservation?.webUserId !== input.webUserId
      || reservation?.expectedPlayerId !== input.expectedPlayerId
      || Number(reservation?.pointAmount) !== input.pointAmount
      || reservation?.purpose !== input.purpose
      || reservation?.source !== input.source
      || reservation?.occurredAt !== input.occurredAt
      || !["RESERVED", "CAPTURED", "RELEASED"].includes(status)
      || (expectedStatus && status !== expectedStatus)) {
    throw new GamePointCommandError("INVALID_GAME_POINT_RESERVATION_RESPONSE", true, response.status);
  }

  return {
    duplicate: Boolean(body.duplicate),
    operation,
    playerId: body.player_id,
    point,
    status: status as GamePointReservationResult["status"],
  };
}

export async function getGamePointWalletView(userId: string): Promise<GamePointWalletView> {
  const linked = await getGamePointLinkedAccount(userId);
  if (!linked) return { linked: false, available: true, point: null };
  if (!transportAllows(userId)) {
    return { linked: true, available: false, point: null, error: "GAME_POINT_SYNC_UNAVAILABLE" };
  }

  try {
    const secret = String(process.env.WEB_TOPUP_SECRET || "");
    if (secret.length < 32) throw new Error("WEB_TOPUP_SECRET_NOT_CONFIGURED");
    const timestamp = String(Math.floor(Date.now() / 1000));
    const requestId = `balance_${randomUUID()}`;
    const signature = createHmac("sha256", secret)
      .update(canonicalBalance(timestamp, requestId, userId), "utf8")
      .digest("hex");
    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), 5000);
    let response: Response;
    try {
      response = await fetch(balanceEndpoint(), {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "X-YWonder-Timestamp": timestamp,
          "X-YWonder-Signature": signature,
        },
        body: JSON.stringify({ request_id: requestId, web_user_id: userId }),
        cache: "no-store",
        signal: controller.signal,
      });
    } finally {
      clearTimeout(timeout);
    }
    if (!response.ok) throw new Error(`GAME_POINT_BALANCE_HTTP_${response.status}`);
    const body = await response.json() as {
      ok?: boolean;
      web_user_id?: string;
      player_id?: string;
      point?: number;
    };
    if (body.ok !== true || body.web_user_id !== userId
        || !body.player_id || !Number.isSafeInteger(body.point) || Number(body.point) < 0) {
      throw new Error("INVALID_GAME_POINT_BALANCE_RESPONSE");
    }
    if (body.player_id !== linked.gamePlayerId) {
      throw new Error("GAME_POINT_IDENTITY_MISMATCH");
    }
    return {
      linked: true,
      available: true,
      point: Number(body.point),
      playerId: body.player_id,
    };
  } catch (error) {
    console.error(JSON.stringify({
      event: "game_point_balance_read_failed",
      error: safeError(error),
    }));
    return { linked: true, available: false, point: null, error: safeError(error) };
  }
}
