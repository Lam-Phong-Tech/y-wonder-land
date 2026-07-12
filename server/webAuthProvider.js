// Web auth adapter for Game API.
// Production uses the web-owned auth endpoint and keeps GAME_API_SECRET server-side.
const crypto = require("crypto");
const jwt = require("jsonwebtoken");

const MODE = (process.env.WEB_AUTH_MODE || "mock").toLowerCase();
const LOGIN_URL = process.env.WEB_AUTH_LOGIN_URL || "https://api.ywonder.net/api/game/auth";
const VERIFY_URL = process.env.WEB_AUTH_VERIFY_URL || "";
const WEB_AUTH_SECRET = process.env.WEB_AUTH_SECRET || process.env.GAME_API_SECRET || "";
const WEB_AUTH_TIMEOUT_MS = Number(process.env.WEB_AUTH_TIMEOUT_MS || 8000);

function optionalBoolean(value) {
  if (value == null || value === "") return null;
  if (typeof value === "boolean") return value;
  if (typeof value === "number") return value !== 0;
  const normalized = String(value).trim().toLowerCase();
  if (["true", "1", "yes", "on"].includes(normalized)) return true;
  if (["false", "0", "no", "off"].includes(normalized)) return false;
  return null;
}

function webAccountAccessError(webUser) {
  if (!webUser) return "WEB_AUTH_RESPONSE_MISSING_USER_ID";
  if (webUser.locked === true) return "WEB_ACCOUNT_LOCKED";
  if (webUser.softDeleted === true) return "WEB_ACCOUNT_DELETED";
  if (webUser.active === false) return "WEB_ACCOUNT_INACTIVE";

  const status = String(webUser.status || "").trim().toLowerCase();
  if (["locked", "blocked", "suspended"].includes(status)) return "WEB_ACCOUNT_LOCKED";
  if (["deleted", "soft_deleted", "soft-deleted"].includes(status)) return "WEB_ACCOUNT_DELETED";
  if (["inactive", "disabled", "deactivated"].includes(status)) return "WEB_ACCOUNT_INACTIVE";
  return "";
}

function normalizeUpstreamError(status, payload) {
  const upstreamCode = String(payload && (payload.error || payload.code) || "").trim().toUpperCase();
  if (["ACCOUNT_LOCKED", "USER_LOCKED", "WEB_ACCOUNT_LOCKED"].includes(upstreamCode)) {
    return { status: 403, error: "WEB_ACCOUNT_LOCKED" };
  }
  if (["ACCOUNT_DELETED", "USER_DELETED", "SOFT_DELETED", "WEB_ACCOUNT_DELETED"].includes(upstreamCode)) {
    return { status: 403, error: "WEB_ACCOUNT_DELETED" };
  }
  if (["ACCOUNT_INACTIVE", "USER_INACTIVE", "WEB_ACCOUNT_INACTIVE"].includes(upstreamCode)) {
    return { status: 403, error: "WEB_ACCOUNT_INACTIVE" };
  }
  if (status === 400 || status === 401 || status === 404) {
    return { status: 401, error: "WEB_AUTH_INVALID_CREDENTIALS" };
  }
  if (status === 403) return { status: 403, error: "WEB_AUTH_FORBIDDEN" };
  if (status === 429) return { status: 429, error: "WEB_AUTH_RATE_LIMITED" };
  return { status: status === 504 ? 504 : 502, error: "WEB_AUTH_UNAVAILABLE" };
}

function makeMockWebUser(input) {
  const raw = (input.username || input.email || input.phone || input.token || "").trim();
  const safe = raw || "demo";
  return {
    id: `mock:${safe.toLowerCase()}`,
    username: input.username || safe,
    email: input.email || "",
    phone: input.phone || "",
    displayName: input.displayName || input.username || safe,
    authSource: "mock",
  };
}

function normalizeWebUser(payload) {
  const data = payload && (payload.user || payload.data || payload);
  const id = data && (data.web_user_id || data.user_id || data.userId || data.id || data.uuid || data.uid);
  if (!id) return null;
  return {
    id: String(id),
    username: data.username || data.uid || data.name || data.email || data.phone || String(id),
    email: data.email || "",
    phone: data.phone || "",
    refCode: data.refCode || data.ref_code || "",
    fullName: data.fullName || data.full_name || "",
    displayName: data.fullName || data.full_name || data.display_name || data.displayName || data.name || data.username || "Player",
    gameToken: data.gameToken || data.game_token || "",
    expiresIn: data.expiresIn || data.expires_in || 0,
    status: data.status || "",
    locked: optionalBoolean(data.locked ?? data.isLocked ?? data.is_locked),
    softDeleted: optionalBoolean(data.softDeleted ?? data.soft_deleted ?? data.isDeleted ?? data.is_deleted),
    active: optionalBoolean(data.active ?? data.isActive ?? data.is_active),
    authSource: "web",
    raw: data,
  };
}

function base64UrlToBuffer(value) {
  const normalized = String(value || "").replace(/-/g, "+").replace(/_/g, "/");
  const padded = normalized + "=".repeat((4 - (normalized.length % 4)) % 4);
  return Buffer.from(padded, "base64");
}

function decodeBase64UrlJson(value) {
  return JSON.parse(base64UrlToBuffer(value).toString("utf8"));
}

function verifyJwtGameToken(gameToken) {
  if (!WEB_AUTH_SECRET) {
    return { ok: false, error: "WEB_AUTH_SECRET_NOT_CONFIGURED" };
  }

  try {
    const payload = jwt.verify(gameToken, WEB_AUTH_SECRET, { algorithms: ["HS256"] });
    return { ok: true, payload };
  } catch (e) {
    return { ok: false, error: e.name === "TokenExpiredError" ? "WEB_GAME_TOKEN_EXPIRED" : "WEB_GAME_TOKEN_JWT_INVALID" };
  }
}

function verifyLegacyGameToken(gameToken) {
  if (!WEB_AUTH_SECRET) {
    return { ok: false, error: "WEB_AUTH_SECRET_NOT_CONFIGURED" };
  }

  const parts = String(gameToken || "").split(".");
  if (parts.length !== 2 || !parts[0] || !parts[1]) {
    return { ok: false, error: "WEB_GAME_TOKEN_INVALID_FORMAT" };
  }

  const expected = crypto.createHmac("sha256", WEB_AUTH_SECRET).update(parts[0]).digest();
  const actual = base64UrlToBuffer(parts[1]);
  if (expected.length !== actual.length || !crypto.timingSafeEqual(expected, actual)) {
    return { ok: false, error: "WEB_GAME_TOKEN_BAD_SIGNATURE" };
  }

  let payload;
  try {
    payload = decodeBase64UrlJson(parts[0]);
  } catch (e) {
    return { ok: false, error: "WEB_GAME_TOKEN_BAD_PAYLOAD" };
  }

  const nowSec = Math.floor(Date.now() / 1000);
  if (payload.exp && Number(payload.exp) < nowSec) {
    return { ok: false, error: "WEB_GAME_TOKEN_EXPIRED" };
  }

  return { ok: true, payload };
}

function verifyGameToken(gameToken) {
  const parts = String(gameToken || "").split(".");
  if (parts.length === 3) return verifyJwtGameToken(gameToken);
  if (parts.length === 2) return verifyLegacyGameToken(gameToken);
  return { ok: false, error: "WEB_GAME_TOKEN_INVALID_FORMAT" };
}

async function postJson(url, body, secret) {
  if (!url) {
    return { ok: false, status: 503, error: "WEB_AUTH_URL_NOT_CONFIGURED" };
  }

  if (!secret) {
    return { ok: false, status: 503, error: "WEB_AUTH_SECRET_NOT_CONFIGURED" };
  }

  if (typeof fetch !== "function") {
    return { ok: false, status: 500, error: "NODE_FETCH_NOT_AVAILABLE" };
  }

  const headers = { "Content-Type": "application/json" };
  headers.Authorization = `Bearer ${secret}`;

  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), Math.max(1000, WEB_AUTH_TIMEOUT_MS));
  let response;
  try {
    response = await fetch(url, {
      method: "POST",
      headers,
      body: JSON.stringify(body || {}),
      signal: controller.signal,
    });
  } catch (e) {
    return {
      ok: false,
      status: e.name === "AbortError" ? 504 : 502,
      error: e.name === "AbortError" ? "WEB_AUTH_TIMEOUT" : "WEB_AUTH_FETCH_FAILED",
    };
  } finally {
    clearTimeout(timeout);
  }

  let payload = null;
  try {
    payload = await response.json();
  } catch (e) {
    payload = null;
  }

  if (!response.ok) {
    const normalizedError = normalizeUpstreamError(response.status, payload);
    return {
      ok: false,
      status: normalizedError.status,
      error: normalizedError.error,
    };
  }

  const webUser = normalizeWebUser(payload);
  if (!webUser) {
    return { ok: false, status: 502, error: "WEB_AUTH_RESPONSE_MISSING_USER_ID", payload };
  }

  const accessError = webAccountAccessError(webUser);
  if (accessError) {
    return { ok: false, status: 403, error: accessError };
  }

  if (webUser.gameToken) {
    const tokenCheck = verifyGameToken(webUser.gameToken);
    if (!tokenCheck.ok) {
      return { ok: false, status: 502, error: tokenCheck.error, payload };
    }

    const tokenUid = tokenCheck.payload.sub || tokenCheck.payload.uid || tokenCheck.payload.userId || tokenCheck.payload.user_id;
    if (tokenUid && String(tokenUid) !== webUser.id && String(tokenUid) !== String(webUser.username)) {
      return { ok: false, status: 502, error: "WEB_GAME_TOKEN_UID_MISMATCH", payload };
    }
    webUser.tokenPayload = tokenCheck.payload;
  }

  return { ok: true, status: response.status, webUser, payload };
}

async function verifyLogin(input) {
  const body = input || {};

  if (MODE === "disabled") {
    return { ok: false, status: 503, error: "WEB_AUTH_DISABLED" };
  }

  if (MODE === "mock") {
    if (!body.username && !body.email && !body.phone && !body.token) {
      return { ok: false, status: 400, error: "MISSING_WEB_LOGIN_IDENTITY" };
    }
    if (!body.token && !body.password) {
      return { ok: false, status: 400, error: "MISSING_WEB_LOGIN_PASSWORD_OR_TOKEN" };
    }
    return { ok: true, status: 200, webUser: makeMockWebUser(body), mode: MODE };
  }

  if (MODE === "http") {
    return postJson(LOGIN_URL, body, WEB_AUTH_SECRET);
  }

  return { ok: false, status: 500, error: `UNKNOWN_WEB_AUTH_MODE:${MODE}` };
}

async function verifyToken(token) {
  if (!token) return { ok: false, status: 400, error: "MISSING_TOKEN" };

  if (MODE === "mock") {
    return { ok: true, status: 200, webUser: makeMockWebUser({ token, username: token }), mode: MODE };
  }

  if (MODE === "http") {
    if (VERIFY_URL) {
      return postJson(VERIFY_URL, { token }, WEB_AUTH_SECRET);
    }

    const tokenCheck = verifyGameToken(token);
    if (!tokenCheck.ok) {
      return { ok: false, status: 401, error: tokenCheck.error };
    }

    const payload = tokenCheck.payload;
    return {
      ok: true,
      status: 200,
      webUser: normalizeWebUser({
        userId: payload.sub || payload.uid || payload.userId || payload.user_id,
        username: payload.username || payload.uid || "",
        gameToken: token,
      }),
      payload,
    };
  }

  return { ok: false, status: 503, error: "WEB_TOKEN_VERIFY_UNAVAILABLE" };
}

function verifyTrustedIdentity(input) {
  const normalized = normalizeWebUser(input || {});
  const accessError = webAccountAccessError(normalized);
  if (accessError) {
    return { ok: false, status: accessError === "WEB_AUTH_RESPONSE_MISSING_USER_ID" ? 400 : 403, error: accessError };
  }

  return {
    ok: true,
    status: 200,
    webUser: {
      id: normalized.id,
      username: normalized.username,
      email: normalized.email,
      phone: normalized.phone,
      refCode: normalized.refCode,
      fullName: normalized.fullName,
      displayName: normalized.displayName,
      status: normalized.status,
      locked: normalized.locked,
      softDeleted: normalized.softDeleted,
      active: normalized.active,
      authSource: "web-browser",
    },
  };
}

module.exports = {
  mode: MODE,
  verifyLogin,
  verifyToken,
  verifyGameToken,
  verifyTrustedIdentity,
};
