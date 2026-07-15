import { createHmac } from "crypto";
import { db } from "@/lib/db";

const POINT_MICROS_SCALE = 1_000_000;
const SOURCE = "ywonder-web-usdt-to-point";

function canaryWebUserIds(): string[] {
  if (String(process.env.WEB_TOPUP_MODE || "canary").trim().toLowerCase() === "open") return [];
  return Array.from(new Set(String(process.env.WEB_TOPUP_ALLOWED_WEB_USER_IDS || "")
    .split(",")
    .map((entry) => entry.trim())
    .filter(Boolean)));
}

type DispatchResult = {
  ok: boolean;
  duplicate?: boolean;
  permanent?: boolean;
  error?: string;
};

export function pointToGameAmountText(point: number): string {
  const micros = Math.round(point * POINT_MICROS_SCALE);
  if (!Number.isSafeInteger(micros) || micros < 1) {
    throw new Error("INVALID_GAME_POINT_AMOUNT");
  }
  const whole = Math.floor(micros / POINT_MICROS_SCALE);
  const fraction = String(micros % POINT_MICROS_SCALE).padStart(6, "0");
  return `${whole}.${fraction}`;
}

function gamePointSyncEndpoint(): string {
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
  return url.toString();
}

function canonicalPayload(timestamp: string, input: {
  transactionId: string;
  webUserId: string;
  pointAmount: string;
  occurredAt: string;
  source: string;
  username: string;
  displayName: string;
}): string {
  return JSON.stringify([
    "ywonder-point-credit-v1",
    timestamp,
    input.transactionId,
    input.webUserId,
    input.pointAmount,
    input.occurredAt,
    input.source,
    input.username,
    input.displayName,
  ]);
}

function safeError(error: unknown): string {
  const message = error instanceof Error ? error.message : String(error || "UNKNOWN_ERROR");
  return message.replace(/[\r\n\0]/g, " ").slice(0, 240);
}

function retryAt(attempts: number): Date {
  const seconds = Math.min(3600, Math.max(15, 15 * (2 ** Math.min(attempts, 8))));
  return new Date(Date.now() + seconds * 1000);
}

async function markFailure(id: string, attempts: number, error: string, permanent: boolean) {
  await db.gamePointSyncOutbox.update({
    where: { id },
    data: {
      status: permanent ? "FAILED" : "RETRY",
      attempts,
      lastError: safeError(error),
      nextAttemptAt: permanent ? new Date("9999-12-31T23:59:59.000Z") : retryAt(attempts),
    },
  });
}

export async function dispatchGamePointSyncOutboxById(id: string): Promise<DispatchResult> {
  const record = await db.gamePointSyncOutbox.findUnique({
    where: { id },
    include: {
      user: { select: { id: true, email: true, username: true, fullName: true } },
    },
  });
  if (!record) return { ok: false, permanent: true, error: "OUTBOX_NOT_FOUND" };
  if (record.status === "SENT") return { ok: true, duplicate: true };
  if (record.status === "FAILED") return { ok: false, permanent: true, error: record.lastError || "FAILED" };
  const canaryIds = canaryWebUserIds();
  if (String(process.env.WEB_TOPUP_MODE || "canary").trim().toLowerCase() !== "open"
      && !canaryIds.includes(record.user.id)) {
    return { ok: false, permanent: false, error: "GAME_POINT_SYNC_CANARY_USER_NOT_ALLOWED" };
  }

  const attempts = record.attempts + 1;
  try {
    const secret = String(process.env.WEB_TOPUP_SECRET || "");
    if (secret.length < 32) throw new Error("WEB_TOPUP_SECRET_NOT_CONFIGURED");

    const timestamp = String(Math.floor(Date.now() / 1000));
    const input = {
      transactionId: record.sourceTransactionId,
      webUserId: record.user.id,
      pointAmount: record.pointAmount,
      occurredAt: record.occurredAt.toISOString(),
      source: record.source,
      username: record.user.username || record.user.email || "",
      displayName: record.user.fullName || record.user.username || "",
    };
    const signature = createHmac("sha256", secret)
      .update(canonicalPayload(timestamp, input), "utf8")
      .digest("hex");

    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), 8000);
    let response: Response;
    try {
      response = await fetch(gamePointSyncEndpoint(), {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "X-YWonder-Timestamp": timestamp,
          "X-YWonder-Signature": signature,
        },
        body: JSON.stringify({
          transaction_id: input.transactionId,
          web_user_id: input.webUserId,
          point_amount: input.pointAmount,
          occurred_at: input.occurredAt,
          source: input.source,
          username: input.username,
          display_name: input.displayName,
        }),
        cache: "no-store",
        signal: controller.signal,
      });
    } finally {
      clearTimeout(timeout);
    }

    if (response.ok) {
      const body = await response.json().catch(() => ({})) as { duplicate?: boolean };
      await db.gamePointSyncOutbox.update({
        where: { id: record.id },
        data: {
          status: "SENT",
          attempts,
          lastError: null,
          sentAt: new Date(),
          nextAttemptAt: new Date(),
        },
      });
      return { ok: true, duplicate: Boolean(body.duplicate) };
    }

    // A 404 is expected during a safe rollout while the game endpoint is still
    // disabled. Keep the durable row retryable so deployment order cannot lose
    // a successful web conversion.
    const retryable = response.status === 404
      || response.status === 408
      || response.status === 425
      || response.status === 429
      || response.status >= 500;
    const permanent = response.status >= 400 && response.status < 500 && !retryable;
    const error = `GAME_POINT_SYNC_HTTP_${response.status}`;
    await markFailure(record.id, attempts, error, permanent);
    return { ok: false, permanent, error };
  } catch (error) {
    const message = safeError(error);
    await markFailure(record.id, attempts, message, false);
    return { ok: false, permanent: false, error: message };
  }
}

export async function retryPendingGamePointSync(limit = 25) {
  const canaryIds = canaryWebUserIds();
  const openMode = String(process.env.WEB_TOPUP_MODE || "canary").trim().toLowerCase() === "open";
  if (!openMode && canaryIds.length === 0) {
    return { processed: 0, sent: 0, retry: 0, failed: 0 };
  }
  const records = await db.gamePointSyncOutbox.findMany({
    where: {
      status: { in: ["PENDING", "RETRY"] },
      nextAttemptAt: { lte: new Date() },
      ...(openMode ? {} : { userId: { in: canaryIds } }),
    },
    orderBy: { createdAt: "asc" },
    take: Math.min(100, Math.max(1, Math.trunc(limit))),
    select: { id: true },
  });

  let sent = 0;
  let retry = 0;
  let failed = 0;
  for (const record of records) {
    const result = await dispatchGamePointSyncOutboxById(record.id);
    if (result.ok) sent += 1;
    else if (result.permanent) failed += 1;
    else retry += 1;
  }
  return { processed: records.length, sent, retry, failed };
}

export const GAME_POINT_SYNC_SOURCE = SOURCE;
