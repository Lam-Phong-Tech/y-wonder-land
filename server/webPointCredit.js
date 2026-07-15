const crypto = require("crypto");

const POINT_MICROS_SCALE = 1_000_000;
const MAX_SAFE_POINT_AMOUNT = Math.floor(Number.MAX_SAFE_INTEGER / POINT_MICROS_SCALE);

function envBoolean(name, fallback, env) {
  const raw = env[name];
  if (raw == null || raw === "") return fallback;
  return String(raw).trim().toLowerCase() === "true";
}

function envInteger(name, fallback, min, max, env) {
  const parsed = Number(env[name]);
  if (!Number.isSafeInteger(parsed)) return fallback;
  return Math.min(max, Math.max(min, parsed));
}

function parseAllowedWebUserIds(value) {
  return new Set(String(value || "")
    .split(",")
    .map((entry) => entry.trim())
    .filter(Boolean));
}

function buildWebPointCreditConfig(env = process.env) {
  const configuredMode = String(env.WEB_TOPUP_MODE || "canary").trim().toLowerCase();
  return {
    enabled: envBoolean("WEB_TOPUP_ENABLED", false, env),
    allowRemote: envBoolean("WEB_TOPUP_ALLOW_REMOTE", false, env),
    clockSkewSec: envInteger("WEB_TOPUP_CLOCK_SKEW_SEC", 300, 30, 900, env),
    maxPoints: envInteger("WEB_TOPUP_MAX_POINTS", 1_000_000_000, 1, MAX_SAFE_POINT_AMOUNT, env),
    secret: String(env.WEB_TOPUP_SECRET || ""),
    mode: configuredMode === "open" ? "open" : "canary",
    allowedWebUserIds: parseAllowedWebUserIds(env.WEB_TOPUP_ALLOWED_WEB_USER_IDS),
  };
}

function normalizePointAmount(value, maxPoints) {
  const raw = String(value ?? "").trim();
  const match = /^(0|[1-9]\d*)(?:\.(\d{1,6}))?$/.exec(raw);
  if (!match) throw new Error("INVALID_POINT_AMOUNT");

  const whole = Number(match[1]);
  const fractionText = String(match[2] || "").padEnd(6, "0");
  const fraction = Number(fractionText);
  if (!Number.isSafeInteger(whole) || whole > maxPoints) {
    throw new Error("INVALID_POINT_AMOUNT");
  }

  const pointAmountMicros = whole * POINT_MICROS_SCALE + fraction;
  const maxMicros = maxPoints * POINT_MICROS_SCALE;
  if (!Number.isSafeInteger(pointAmountMicros) || pointAmountMicros < 1 || pointAmountMicros > maxMicros) {
    throw new Error("INVALID_POINT_AMOUNT");
  }

  return {
    pointAmount: `${match[1]}.${fractionText}`,
    pointAmountMicros,
  };
}

function normalizeText(value, field, maxLength, required = false) {
  const text = String(value || "").trim();
  if (required && !text) throw new Error(`MISSING_${field}`);
  if (text.length > maxLength || /[\r\n\0]/.test(text)) throw new Error(`INVALID_${field}`);
  return text;
}

function normalizeWebPointCreditBody(body, maxPoints) {
  const input = body && typeof body === "object" ? body : {};
  const transactionId = normalizeText(
    input.transaction_id ?? input.transactionId,
    "TRANSACTION_ID",
    128,
    true
  );
  if (!/^[A-Za-z0-9._:-]+$/.test(transactionId)) throw new Error("INVALID_TRANSACTION_ID");

  const webUserId = normalizeText(
    input.web_user_id ?? input.webUserId ?? input.uid,
    "WEB_USER_ID",
    128,
    true
  );
  const normalizedAmount = normalizePointAmount(
    input.point_amount ?? input.pointAmount ?? input.amount,
    maxPoints
  );

  const rawOccurredAt = normalizeText(
    input.occurred_at ?? input.occurredAt,
    "OCCURRED_AT",
    64,
    true
  );
  const occurredAtMs = Date.parse(rawOccurredAt);
  if (!Number.isFinite(occurredAtMs)) throw new Error("INVALID_OCCURRED_AT");

  const source = normalizeText(input.source || "ywonder-web", "SOURCE", 64, true);
  if (!/^[A-Za-z0-9._:-]+$/.test(source)) throw new Error("INVALID_SOURCE");

  return {
    transactionId,
    webUserId,
    ...normalizedAmount,
    occurredAt: new Date(occurredAtMs).toISOString(),
    source,
    username: normalizeText(input.username, "USERNAME", 254),
    displayName: normalizeText(input.display_name ?? input.displayName, "DISPLAY_NAME", 160),
  };
}

function canonicalWebPointCredit(timestamp, credit) {
  return JSON.stringify([
    "ywonder-point-credit-v1",
    String(timestamp),
    credit.transactionId,
    credit.webUserId,
    credit.pointAmount,
    credit.occurredAt,
    credit.source,
    credit.username,
    credit.displayName,
  ]);
}

function signWebPointCredit(secret, timestamp, credit) {
  return crypto
    .createHmac("sha256", secret)
    .update(canonicalWebPointCredit(timestamp, credit), "utf8")
    .digest("hex");
}

function signaturesMatch(expected, supplied) {
  const candidate = String(supplied || "").trim().replace(/^sha256=/i, "");
  if (!/^[a-f0-9]{64}$/i.test(candidate)) return false;
  return crypto.timingSafeEqual(Buffer.from(expected, "hex"), Buffer.from(candidate, "hex"));
}

function isLoopbackAddress(value) {
  const address = String(value || "").trim().toLowerCase();
  return address === "127.0.0.1" || address === "::1" || address === "::ffff:127.0.0.1";
}

function createWebPointCreditHandler(options) {
  const store = options.store;
  const config = options.config;
  const onCredit = options.onCredit;
  if (!store || typeof store.creditWebTopup !== "function") {
    throw new Error("Web Point credit requires a storage adapter with creditWebTopup().");
  }

  return async (req, res, next) => {
    try {
      if (!config.enabled) return res.status(404).json({ error: "NOT_FOUND" });
      if (!config.allowRemote && !isLoopbackAddress(req.socket && req.socket.remoteAddress)) {
        return res.status(403).json({ error: "WEB_TOPUP_LOOPBACK_ONLY" });
      }
      if (config.secret.length < 32) {
        return res.status(503).json({ error: "WEB_TOPUP_NOT_CONFIGURED" });
      }

      const timestamp = String(req.headers["x-ywonder-timestamp"] || "").trim();
      // Contract uses Unix seconds. Reject millisecond timestamps instead of
      // silently treating them as seconds and reporting a misleading expiry.
      if (!/^\d{10,11}$/.test(timestamp)) {
        return res.status(401).json({ error: "INVALID_WEB_TOPUP_SIGNATURE" });
      }
      const timestampSec = Number(timestamp);
      if (!Number.isInteger(timestampSec)) {
        return res.status(401).json({ error: "INVALID_WEB_TOPUP_SIGNATURE" });
      }
      if (Math.abs(Math.floor(Date.now() / 1000) - timestampSec) > config.clockSkewSec) {
        return res.status(401).json({ error: "WEB_TOPUP_REQUEST_EXPIRED" });
      }

      let credit;
      try {
        credit = normalizeWebPointCreditBody(req.body, config.maxPoints);
      } catch (error) {
        return res.status(400).json({ error: error.message || "INVALID_WEB_TOPUP_REQUEST" });
      }

      const expectedSignature = signWebPointCredit(config.secret, timestamp, credit);
      if (!signaturesMatch(expectedSignature, req.headers["x-ywonder-signature"])) {
        return res.status(401).json({ error: "INVALID_WEB_TOPUP_SIGNATURE" });
      }
      if (config.mode !== "open"
          && (!config.allowedWebUserIds || !config.allowedWebUserIds.has(credit.webUserId))) {
        // 425 is deliberately retryable by the web outbox. Non-canary rows stay
        // pending until the allowlist is expanded or the rollout enters open mode.
        return res.status(425).json({ error: "WEB_TOPUP_CANARY_USER_NOT_ALLOWED" });
      }

      const result = await store.creditWebTopup({
        id: credit.webUserId,
        username: credit.username,
        displayName: credit.displayName,
        authSource: "web",
      }, credit.pointAmountMicros, {
        pointAmount: credit.pointAmount,
        transactionId: credit.transactionId,
        occurredAt: credit.occurredAt,
        source: credit.source,
      });

      if (!result.ok && result.error === "IDEMPOTENCY_CONFLICT") {
        return res.status(409).json({ error: result.error });
      }
      if (!result.ok) return res.status(409).json({ error: result.error || "WEB_TOPUP_REJECTED" });

      if (typeof onCredit === "function") {
        try {
          await onCredit(result);
        } catch (error) {
          console.warn(JSON.stringify({
            event: "web_topup_realtime_notify_failed",
            errorCode: error && (error.code || error.name) || "UNKNOWN_ERROR",
          }));
        }
      }

      return res.json({
        ok: true,
        duplicate: Boolean(result.duplicate),
        player_id: result.player && result.player.id,
        economy: result.economy,
        transaction: result.transaction,
      });
    } catch (error) {
      return next(error);
    }
  };
}

module.exports = {
  POINT_MICROS_SCALE,
  buildWebPointCreditConfig,
  canonicalWebPointCredit,
  createWebPointCreditHandler,
  normalizePointAmount,
  normalizeWebPointCreditBody,
  signWebPointCredit,
};
