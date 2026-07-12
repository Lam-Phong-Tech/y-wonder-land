const crypto = require("crypto");

const BASE64URL_RE = /^[A-Za-z0-9_-]+$/;
const REQUEST_ID_BYTES = 32;

function base64Url(buffer) {
  return Buffer.from(buffer).toString("base64")
    .replace(/\+/g, "-")
    .replace(/\//g, "_")
    .replace(/=+$/g, "");
}

function createRequestId() {
  return base64Url(crypto.randomBytes(REQUEST_ID_BYTES));
}

function hashRequestId(requestId) {
  return crypto.createHash("sha256").update(String(requestId || ""), "utf8").digest("hex");
}

function createPkceChallenge(verifier) {
  return base64Url(crypto.createHash("sha256").update(String(verifier || ""), "utf8").digest());
}

function isValidRequestId(value) {
  const text = String(value || "");
  return text.length === 43 && BASE64URL_RE.test(text);
}

function isValidPkceChallenge(value) {
  const text = String(value || "");
  return text.length === 43 && BASE64URL_RE.test(text);
}

function isValidPkceVerifier(value) {
  const text = String(value || "");
  return text.length >= 43 && text.length <= 128 && BASE64URL_RE.test(text);
}

function normalizeIntent(value) {
  return String(value || "").trim().toLowerCase() === "register" ? "register" : "login";
}

function buildBrowserLoginUrl(loginUrl, callbackUrl, requestId, intent) {
  const login = new URL(loginUrl);
  const callback = new URL(callbackUrl);
  callback.searchParams.set("request", requestId);
  callback.searchParams.set("intent", normalizeIntent(intent));
  login.searchParams.set("callbackUrl", callback.toString());
  return login.toString();
}

function safeSecretEqual(actual, expected) {
  const left = Buffer.from(String(actual || ""), "utf8");
  const right = Buffer.from(String(expected || ""), "utf8");
  if (left.length === 0 || left.length !== right.length) return false;
  return crypto.timingSafeEqual(left, right);
}

function bearerToken(header) {
  const value = String(header || "");
  return value.startsWith("Bearer ") ? value.slice(7) : "";
}

module.exports = {
  bearerToken,
  buildBrowserLoginUrl,
  createPkceChallenge,
  createRequestId,
  hashRequestId,
  isValidPkceChallenge,
  isValidPkceVerifier,
  isValidRequestId,
  normalizeIntent,
  safeSecretEqual,
};
