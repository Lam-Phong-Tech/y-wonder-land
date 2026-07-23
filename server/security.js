const crypto = require("crypto");

const DEVELOPMENT_JWT_SECRET = "ywonderland_dev_secret_change_me";

function envBoolean(name, fallback = false, env = process.env) {
  const raw = env[name];
  if (raw == null || raw === "") return fallback;
  return String(raw).trim().toLowerCase() === "true";
}

function envInteger(name, fallback, options = {}, env = process.env) {
  const min = options.min == null ? 1 : options.min;
  const max = options.max == null ? Number.MAX_SAFE_INTEGER : options.max;
  const parsed = Number(env[name]);
  if (!Number.isInteger(parsed)) return fallback;
  return Math.min(max, Math.max(min, parsed));
}

function envList(name, env = process.env) {
  return [...new Set(String(env[name] || "")
    .split(",")
    .map((entry) => entry.trim())
    .filter(Boolean))];
}

function resolveAuthTransitionMode(webAuthMode, env = process.env) {
  const configured = String(env.AUTH_TRANSITION_MODE || "").trim().toLowerCase();
  if (configured) return configured;
  return webAuthMode === "http" ? "web-primary" : "local-primary";
}

function normalizeIp(value) {
  const ip = String(value || "unknown").trim();
  return ip.startsWith("::ffff:") ? ip.slice(7) : ip;
}

function getRequestIp(req) {
  return normalizeIp(req.ip || (req.socket && req.socket.remoteAddress));
}

function safeRequestId(value) {
  const candidate = String(value || "").trim();
  return /^[A-Za-z0-9._-]{1,64}$/.test(candidate) ? candidate : "";
}

function stableHash(value) {
  return crypto.createHash("sha256").update(String(value || "")).digest("hex");
}

function authIdentityKey(req) {
  const body = req.body || {};
  const identity = body.username || body.email || body.phone || body.refCode || body.ref_code || "";
  if (identity) return `identity:${stableHash(String(identity).trim().toLowerCase())}`;
  if (body.token) return `token:${stableHash(body.token)}`;
  return `ip:${stableHash(getRequestIp(req))}`;
}

function buildSecurityConfig(env = process.env) {
  const production = String(env.NODE_ENV || "").toLowerCase() === "production";
  const configuredOrigins = String(env.CORS_ALLOWED_ORIGINS || "")
    .split(",")
    .map((origin) => origin.trim())
    .filter(Boolean);

  return {
    production,
    trustProxy: env.TRUST_PROXY || (production ? "loopback" : ""),
    jsonBodyLimit: env.JSON_BODY_LIMIT || "512kb",
    corsAllowedOrigins: configuredOrigins.length > 0
      ? configuredOrigins
      : production ? [] : ["*"],
    accessLogEnabled: envBoolean("HTTP_ACCESS_LOG", production, env),
    bcryptRounds: envInteger("BCRYPT_ROUNDS", production ? 12 : 10, { min: 8, max: 14 }, env),
    localRegistrationEnabled: envBoolean("LOCAL_REGISTRATION_ENABLED", true, env),
    authIpWindowMs: envInteger("AUTH_IP_RATE_LIMIT_WINDOW_MS", 10 * 60 * 1000, { min: 1000 }, env),
    authIpMax: envInteger("AUTH_IP_RATE_LIMIT_MAX", 120, { min: 1 }, env),
    authIdentityWindowMs: envInteger("AUTH_IDENTITY_RATE_LIMIT_WINDOW_MS", 15 * 60 * 1000, { min: 1000 }, env),
    authIdentityMax: envInteger("AUTH_IDENTITY_RATE_LIMIT_MAX", 15, { min: 1 }, env),
    registerWindowMs: envInteger("AUTH_REGISTER_RATE_LIMIT_WINDOW_MS", 60 * 60 * 1000, { min: 1000 }, env),
    registerMax: envInteger("AUTH_REGISTER_RATE_LIMIT_MAX", 30, { min: 1 }, env),
    browserAuthEnabled: envBoolean("BROWSER_AUTH_ENABLED", false, env),
    browserAuthTtlMs: envInteger("BROWSER_AUTH_TTL_MS", 10 * 60 * 1000, { min: 60_000, max: 15 * 60 * 1000 }, env),
    browserAuthPollIntervalMs: envInteger("BROWSER_AUTH_POLL_INTERVAL_MS", 1000, { min: 500, max: 5000 }, env),
    browserAuthStartWindowMs: envInteger("BROWSER_AUTH_START_RATE_LIMIT_WINDOW_MS", 10 * 60 * 1000, { min: 1000 }, env),
    browserAuthStartMax: envInteger("BROWSER_AUTH_START_RATE_LIMIT_MAX", 30, { min: 1 }, env),
    browserAuthExchangeWindowMs: envInteger("BROWSER_AUTH_EXCHANGE_RATE_LIMIT_WINDOW_MS", 10 * 60 * 1000, { min: 1000 }, env),
    browserAuthExchangeMax: envInteger("BROWSER_AUTH_EXCHANGE_RATE_LIMIT_MAX", 1500, { min: 1 }, env),
    webTopupEnabled: envBoolean("WEB_TOPUP_ENABLED", false, env),
    webTopupAllowRemote: envBoolean("WEB_TOPUP_ALLOW_REMOTE", false, env),
    webTopupClockSkewSec: envInteger("WEB_TOPUP_CLOCK_SKEW_SEC", 300, { min: 30, max: 900 }, env),
    webTopupMaxPoints: envInteger("WEB_TOPUP_MAX_POINTS", 1_000_000_000, { min: 1, max: Number.MAX_SAFE_INTEGER }, env),
    clientAssetGrantsEnabled: envBoolean("CLIENT_ASSET_GRANTS_ENABLED", true, env),
    clientAssetGrantsBlockedWebUserIds: new Set(envList("CLIENT_ASSET_GRANTS_BLOCKED_WEB_USER_IDS", env)),
    rateLimitEnabled: envBoolean("RATE_LIMIT_ENABLED", true, env),
    requestTimeoutMs: envInteger("HTTP_REQUEST_TIMEOUT_MS", 30_000, { min: 1000 }, env),
    headersTimeoutMs: envInteger("HTTP_HEADERS_TIMEOUT_MS", 15_000, { min: 1000 }, env),
    keepAliveTimeoutMs: envInteger("HTTP_KEEP_ALIVE_TIMEOUT_MS", 5_000, { min: 1000 }, env),
  };
}

function validateProductionConfig(env = process.env) {
  if (String(env.NODE_ENV || "").toLowerCase() !== "production") return;

  const errors = [];
  const jwtSecret = String(env.JWT_SECRET || "");
  const host = String(env.HOST || "");
  const storeMode = String(env.STORE_MODE || "").toLowerCase();
  const webAuthMode = String(env.WEB_AUTH_MODE || "").toLowerCase();
  const authTransitionMode = resolveAuthTransitionMode(webAuthMode, env);
  const localRegistrationEnabled = envBoolean("LOCAL_REGISTRATION_ENABLED", true, env);

  if (jwtSecret.length < 32 || jwtSecret === DEVELOPMENT_JWT_SECRET) {
    errors.push("JWT_SECRET must be a unique secret with at least 32 characters");
  }
  if (host !== "127.0.0.1" && host !== "::1" && host !== "localhost") {
    errors.push("HOST must bind to loopback behind the reverse proxy");
  }
  if (storeMode !== "postgres") {
    errors.push("STORE_MODE must be postgres");
  }
  if (webAuthMode !== "disabled" && webAuthMode !== "http") {
    errors.push("WEB_AUTH_MODE must be disabled or http");
  }
  if (!["local-primary", "parallel", "web-primary"].includes(authTransitionMode)) {
    errors.push("AUTH_TRANSITION_MODE must be local-primary, parallel, or web-primary");
  }
  if (webAuthMode === "disabled" && authTransitionMode !== "local-primary") {
    errors.push("WEB_AUTH_MODE=disabled requires AUTH_TRANSITION_MODE=local-primary");
  }
  if (webAuthMode === "http") {
    const loginUrl = String(env.WEB_AUTH_LOGIN_URL || "");
    const authSecret = String(env.WEB_AUTH_SECRET || env.GAME_API_SECRET || "");
    let parsedLoginUrl = null;
    try {
      parsedLoginUrl = new URL(loginUrl);
    } catch (error) {
      parsedLoginUrl = null;
    }
    if (!parsedLoginUrl || parsedLoginUrl.protocol !== "https:") {
      errors.push("WEB_AUTH_LOGIN_URL must be a valid HTTPS URL");
    }
    if (authSecret.length < 16) {
      errors.push("WEB_AUTH_SECRET or GAME_API_SECRET must contain at least 16 characters");
    }
    if (authTransitionMode === "local-primary") {
      errors.push("WEB_AUTH_MODE=http requires AUTH_TRANSITION_MODE=parallel or web-primary");
    }
    if (authTransitionMode === "parallel" && !localRegistrationEnabled) {
      errors.push("AUTH_TRANSITION_MODE=parallel requires LOCAL_REGISTRATION_ENABLED=true");
    }
    if (authTransitionMode === "web-primary" && localRegistrationEnabled) {
      errors.push("AUTH_TRANSITION_MODE=web-primary requires LOCAL_REGISTRATION_ENABLED=false");
    }
  }
  if (envBoolean("BROWSER_AUTH_ENABLED", false, env)) {
    const loginUrl = String(env.BROWSER_AUTH_LOGIN_URL || "");
    const callbackUrl = String(env.BROWSER_AUTH_CALLBACK_URL || "");
    const authSecret = String(env.WEB_AUTH_SECRET || env.GAME_API_SECRET || "");
    let parsedLoginUrl = null;
    let parsedCallbackUrl = null;
    try { parsedLoginUrl = new URL(loginUrl); } catch (error) { parsedLoginUrl = null; }
    try { parsedCallbackUrl = new URL(callbackUrl); } catch (error) { parsedCallbackUrl = null; }

    if (webAuthMode !== "http") {
      errors.push("BROWSER_AUTH_ENABLED=true requires WEB_AUTH_MODE=http");
    }
    if (!parsedLoginUrl || parsedLoginUrl.protocol !== "https:") {
      errors.push("BROWSER_AUTH_LOGIN_URL must be a valid HTTPS URL");
    }
    if (!parsedCallbackUrl || parsedCallbackUrl.protocol !== "https:") {
      errors.push("BROWSER_AUTH_CALLBACK_URL must be a valid HTTPS URL");
    }
    if (parsedLoginUrl && parsedCallbackUrl && parsedLoginUrl.origin !== parsedCallbackUrl.origin) {
      errors.push("BROWSER_AUTH_LOGIN_URL and BROWSER_AUTH_CALLBACK_URL must use the same origin");
    }
    if (parsedCallbackUrl && parsedCallbackUrl.pathname !== "/api/game/browser/callback") {
      errors.push("BROWSER_AUTH_CALLBACK_URL must use /api/game/browser/callback");
    }
    if (authSecret.length < 16) {
      errors.push("Browser auth approval requires WEB_AUTH_SECRET or GAME_API_SECRET");
    }
  }
  if (envBoolean("WEB_TOPUP_ENABLED", false, env)) {
    const topupSecret = String(env.WEB_TOPUP_SECRET || "");
    const topupMode = String(env.WEB_TOPUP_MODE || "canary").trim().toLowerCase();
    const allowedWebUserIds = envList("WEB_TOPUP_ALLOWED_WEB_USER_IDS", env);
    const blockedGrantWebUserIds = envList("CLIENT_ASSET_GRANTS_BLOCKED_WEB_USER_IDS", env);
    const clientAssetGrantsEnabled = envBoolean("CLIENT_ASSET_GRANTS_ENABLED", true, env);
    if (topupSecret.length < 32) {
      errors.push("WEB_TOPUP_SECRET must contain at least 32 characters when WEB_TOPUP_ENABLED=true");
    }
    if (!["canary", "open"].includes(topupMode)) {
      errors.push("WEB_TOPUP_MODE must be canary or open when WEB_TOPUP_ENABLED=true");
    } else if (topupMode === "canary" && allowedWebUserIds.length === 0) {
      errors.push("WEB_TOPUP_ALLOWED_WEB_USER_IDS must contain at least one web user ID in canary mode");
    } else if (topupMode === "canary" && allowedWebUserIds.length > 25) {
      errors.push("WEB_TOPUP_ALLOWED_WEB_USER_IDS cannot contain more than 25 IDs in canary mode");
    } else if (topupMode === "open" && allowedWebUserIds.length > 0) {
      errors.push("WEB_TOPUP_ALLOWED_WEB_USER_IDS must be empty in open mode");
    }
    if (topupMode === "canary" && clientAssetGrantsEnabled) {
      const blockedSet = new Set(blockedGrantWebUserIds);
      const exactCanaryGrantBlock = blockedGrantWebUserIds.length === allowedWebUserIds.length
        && allowedWebUserIds.every((webUserId) => blockedSet.has(webUserId));
      if (!exactCanaryGrantBlock) {
        errors.push("CLIENT_ASSET_GRANTS_BLOCKED_WEB_USER_IDS must exactly match WEB_TOPUP_ALLOWED_WEB_USER_IDS when canary top-up keeps client grants enabled");
      }
    }
    if (topupMode === "open") {
      if (clientAssetGrantsEnabled) {
        errors.push("CLIENT_ASSET_GRANTS_ENABLED must be false when WEB_TOPUP_MODE=open");
      }
      if (blockedGrantWebUserIds.length > 0) {
        errors.push("CLIENT_ASSET_GRANTS_BLOCKED_WEB_USER_IDS must be empty when WEB_TOPUP_MODE=open");
      }
    }
  }
  if (String(env.ADMIN_DASHBOARD_ENABLED || "").toLowerCase() !== "false") {
    errors.push("ADMIN_DASHBOARD_ENABLED must be false");
  }
  if (String(env.DEMO_ACCOUNTS_ENABLED || "").toLowerCase() !== "false") {
    errors.push("DEMO_ACCOUNTS_ENABLED must be false");
  }

  if (errors.length > 0) {
    throw new Error(`Unsafe production configuration: ${errors.join("; ")}`);
  }
}

function createRequestSecurityMiddleware(options = {}) {
  const accessLogEnabled = Boolean(options.accessLogEnabled);

  return (req, res, next) => {
    const startedAt = process.hrtime.bigint();
    const requestId = safeRequestId(req.headers["x-request-id"]) || crypto.randomUUID();
    req.requestId = requestId;

    res.setHeader("X-Request-ID", requestId);
    res.setHeader("X-Content-Type-Options", "nosniff");
    res.setHeader("X-Frame-Options", "DENY");
    res.setHeader("Referrer-Policy", "no-referrer");
    res.setHeader("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
    res.setHeader("Cache-Control", "no-store");

    if (accessLogEnabled) {
      res.once("finish", () => {
        if (req.path === "/health" && res.statusCode < 400) return;
        const durationMs = Number(process.hrtime.bigint() - startedAt) / 1e6;
        console.log(JSON.stringify({
          event: "http_request",
          requestId,
          method: req.method,
          path: req.path,
          status: res.statusCode,
          durationMs: Number(durationMs.toFixed(1)),
          ip: getRequestIp(req),
        }));
      });
    }

    next();
  };
}

function createCorsOptions(allowedOrigins) {
  const origins = new Set(allowedOrigins || []);
  const allowAll = origins.has("*");

  return {
    origin(origin, callback) {
      if (!origin || allowAll || origins.has(origin)) return callback(null, true);
      return callback(null, false);
    },
    methods: ["GET", "POST", "PUT", "OPTIONS"],
    allowedHeaders: ["Authorization", "Content-Type", "X-Request-ID"],
    exposedHeaders: ["X-Request-ID", "RateLimit-Limit", "RateLimit-Remaining", "RateLimit-Reset", "Retry-After"],
    credentials: false,
    maxAge: 600,
  };
}

function createRateLimiter(options) {
  const name = options.name;
  const windowMs = options.windowMs;
  const max = options.max;
  const enabled = options.enabled !== false;
  const keyGenerator = options.keyGenerator || ((req) => `ip:${stableHash(getRequestIp(req))}`);
  const maxBuckets = options.maxBuckets || 10_000;
  const buckets = new Map();
  let requestCount = 0;

  function cleanExpired(now) {
    for (const [key, bucket] of buckets.entries()) {
      if (bucket.resetAt <= now) buckets.delete(key);
    }
  }

  return (req, res, next) => {
    if (!enabled) return next();

    const now = Date.now();
    requestCount += 1;
    if (requestCount % 100 === 0) cleanExpired(now);

    const key = String(keyGenerator(req) || `ip:${stableHash(getRequestIp(req))}`).slice(0, 256);
    let bucket = buckets.get(key);
    if (!bucket || bucket.resetAt <= now) {
      if (buckets.size >= maxBuckets) {
        cleanExpired(now);
        if (buckets.size >= maxBuckets) {
          const oldestKey = buckets.keys().next().value;
          if (oldestKey) buckets.delete(oldestKey);
        }
      }
      bucket = { count: 0, resetAt: now + windowMs };
      buckets.set(key, bucket);
    }

    const retryAfterSec = Math.max(1, Math.ceil((bucket.resetAt - now) / 1000));
    res.setHeader("RateLimit-Limit", String(max));
    res.setHeader("RateLimit-Remaining", String(Math.max(0, max - bucket.count - 1)));
    res.setHeader("RateLimit-Reset", String(retryAfterSec));

    if (bucket.count >= max) {
      res.setHeader("RateLimit-Remaining", "0");
      res.setHeader("Retry-After", String(retryAfterSec));
      console.warn(JSON.stringify({
        event: "rate_limited",
        limiter: name,
        requestId: req.requestId || "",
        path: req.path,
        ip: getRequestIp(req),
        keyHash: stableHash(key).slice(0, 12),
        retryAfterSec,
      }));
      return res.status(429).json({ error: "RATE_LIMITED", retryAfterSec });
    }

    bucket.count += 1;
    return next();
  };
}

function validateRegistrationBody(body) {
  const username = String((body && body.username) || "").trim();
  const password = String((body && body.password) || "");
  const email = String((body && body.email) || "").trim().toLowerCase();
  const phone = String((body && body.phone) || "").trim();

  if (!username || !password) return { ok: false, error: "MISSING_REQUIRED_FIELDS" };
  if (!/^[A-Za-z0-9_]{9,20}$/.test(username)) return { ok: false, error: "INVALID_USERNAME" };
  if (password.length < 9 || password.length > 20 ||
      !/[A-Z]/.test(password) || !/[a-z]/.test(password) || !/[0-9]/.test(password) ||
      !/[^A-Za-z0-9]/.test(password)) {
    return { ok: false, error: "WEAK_PASSWORD" };
  }
  if (email && (email.length > 254 || !/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(email))) {
    return { ok: false, error: "INVALID_EMAIL" };
  }
  if (phone && (phone.length > 32 || !/^[0-9+().\-\s]{6,32}$/.test(phone))) {
    return { ok: false, error: "INVALID_PHONE" };
  }

  return { ok: true, username, password, email, phone };
}

function validateLoginBody(body) {
  const username = String((body && body.username) || "").trim();
  const password = String((body && body.password) || "");
  if (!username || !password) return { ok: false, error: "MISSING_CREDENTIALS" };
  if (username.length > 254 || password.length > 128) return { ok: false, error: "INVALID_CREDENTIALS_FORMAT" };
  return { ok: true, username, password };
}

module.exports = {
  DEVELOPMENT_JWT_SECRET,
  authIdentityKey,
  buildSecurityConfig,
  createCorsOptions,
  createRateLimiter,
  createRequestSecurityMiddleware,
  getRequestIp,
  validateLoginBody,
  validateProductionConfig,
  validateRegistrationBody,
};
