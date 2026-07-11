// REST and realtime game backend for Y WONDER GREEN FARM.
// Production runs behind a loopback reverse proxy with PostgreSQL storage.
const express = require("express");
const http = require("http");
const crypto = require("crypto");
const cors = require("cors");
const bcrypt = require("bcryptjs");
const jwt = require("jsonwebtoken");
const store = require("./store");
const webAuth = require("./webAuthProvider");
const { attachRealtimeServer } = require("./realtimeServer");
const { createAdminDashboardRouter } = require("./adminDashboard");
const { resolveShopOffer } = require("./shopCatalog");
const {
  ensureDemoAccounts,
  isAllowedDemoPassword,
  canonicalizeDemoAuthPayload,
} = require("./demoAccounts");
const {
  DEVELOPMENT_JWT_SECRET,
  authIdentityKey,
  buildSecurityConfig,
  createCorsOptions,
  createRateLimiter,
  createRequestSecurityMiddleware,
  validateLoginBody,
  validateProductionConfig,
  validateRegistrationBody,
} = require("./security");

const PORT = process.env.PORT || 3000;
const HOST = process.env.HOST || "0.0.0.0";
const JWT_SECRET = process.env.JWT_SECRET || DEVELOPMENT_JWT_SECRET;
const TOKEN_TTL = process.env.JWT_TOKEN_TTL || "30d";
const securityConfig = buildSecurityConfig();

validateProductionConfig();

const app = express();
app.disable("x-powered-by");
if (securityConfig.trustProxy) app.set("trust proxy", securityConfig.trustProxy);
app.use(createRequestSecurityMiddleware({ accessLogEnabled: securityConfig.accessLogEnabled }));
app.use(cors(createCorsOptions(securityConfig.corsAllowedOrigins)));
app.use(express.json({ limit: securityConfig.jsonBodyLimit, strict: true }));

function asyncRoute(handler) {
  return (req, res, next) => Promise.resolve(handler(req, res, next)).catch(next);
}

if (process.env.ADMIN_DASHBOARD_ENABLED !== "false") {
  app.use("/admin", createAdminDashboardRouter());
}

// Mount the same routes for local dev and the public Unity baseUrl.
const api = express.Router();

const authIpLimiter = createRateLimiter({
  name: "auth_ip",
  windowMs: securityConfig.authIpWindowMs,
  max: securityConfig.authIpMax,
  enabled: securityConfig.rateLimitEnabled,
});
const authIdentityLimiter = createRateLimiter({
  name: "auth_identity",
  windowMs: securityConfig.authIdentityWindowMs,
  max: securityConfig.authIdentityMax,
  enabled: securityConfig.rateLimitEnabled,
  keyGenerator: authIdentityKey,
});
const registerLimiter = createRateLimiter({
  name: "auth_register",
  windowMs: securityConfig.registerWindowMs,
  max: securityConfig.registerMax,
  enabled: securityConfig.rateLimitEnabled,
});

api.use("/auth", authIpLimiter);

// ── Helpers ──
function nowISO() {
  return new Date().toISOString();
}

function makeDefaultProfile() {
  return {
    version: 1,
    name: "Player",
    gender: "male",
    avatarId: "",
    level: 1,
    exp: 0.0,
    characterCreated: false,
    tutorialCompleted: false,
    createdAt: nowISO(),
    updatedAt: nowISO(),
  };
}

function makeDefaultProfileForName(name) {
  const profile = makeDefaultProfile();
  if (name) profile.name = name;
  return profile;
}

function signToken(userId, extra = {}) {
  return jwt.sign({ uid: userId, ...extra }, JWT_SECRET, { algorithm: "HS256", expiresIn: TOKEN_TTL });
}

async function verifyTokenPayload(token) {
  return canonicalizeDemoAuthPayload(jwt.verify(token, JWT_SECRET, { algorithms: ["HS256"] }), store);
}

// Middleware: xác thực Bearer token -> gắn req.userId
const auth = asyncRoute(async (req, res, next) => {
  const header = req.headers["authorization"] || "";
  const token = header.startsWith("Bearer ") ? header.slice(7) : null;
  if (!token) return res.status(401).json({ error: "Thiếu token" });
  try {
    const payload = await verifyTokenPayload(token);
    req.userId = payload.uid;
    req.auth = payload;
    next();
  } catch (e) {
    return res.status(401).json({ error: "Token không hợp lệ" });
  }
});

// ── Health check ──
api.get("/", (req, res) => res.json({ ok: true, service: "ywonderland-stub" }));
api.get("/health", asyncRoute(async (req, res) => {
  const storage = await store.healthCheck();
  res.json({ ok: true, service: "ywonderland-stub", storage, checkedAt: nowISO() });
}));

// ── Auth ──
api.post("/auth/register", registerLimiter, asyncRoute(async (req, res) => {
  const validated = validateRegistrationBody(req.body);
  if (!validated.ok) return res.status(400).json({ error: validated.error });
  const { username, password, email, phone } = validated;

  if (await store.findUserByName(username))
    return res.status(409).json({ error: "USERNAME_EXISTS" });
  if (email && await store.findUserByEmail(email))
    return res.status(409).json({ error: "EMAIL_EXISTS" });

  const id = `u_${crypto.randomUUID()}`;
  try {
    await store.createUser({
      id,
      username,
      email,
      phone,
      password_hash: await bcrypt.hash(password, securityConfig.bcryptRounds),
      created_at: nowISO(),
      updated_at: nowISO(),
    });
  } catch (error) {
    if (error && error.code === "23505") {
      const field = String(error.constraint || "").includes("email") ? "EMAIL_EXISTS" : "USERNAME_EXISTS";
      return res.status(409).json({ error: field });
    }
    throw error;
  }

  // Tạo profile mặc định gắn tên đăng nhập
  const profile = makeDefaultProfile();
  profile.name = username;
  await store.setProfile(id, profile);

  console.log(JSON.stringify({ event: "auth_register_succeeded", requestId: req.requestId, userId: id }));
  res.json({ token: signToken(id, { username, email, authSource: "local" }), userId: id, playerId: id, username, email });
}));

api.post("/auth/login", authIdentityLimiter, asyncRoute(async (req, res) => {
  const validated = validateLoginBody(req.body);
  if (!validated.ok) return res.status(400).json({ error: validated.error });
  const { username, password } = validated;

  const user = await store.findUserByName(username);
  if (!user)
    return res.status(404).json({ error: "USER_NOT_FOUND" });
  let passwordOk = false;
  try {
    passwordOk = Boolean(user.password_hash) && await bcrypt.compare(password, user.password_hash);
  } catch (error) {
    console.warn(JSON.stringify({
      event: "auth_password_hash_invalid",
      requestId: req.requestId,
      userId: user.id,
    }));
  }
  passwordOk = passwordOk ||
    (!securityConfig.production && process.env.DEMO_ACCOUNTS_ENABLED !== "false" &&
      isAllowedDemoPassword(user.username, password));
  if (!passwordOk)
    return res.status(401).json({ error: "Sai username hoặc mật khẩu" });

  console.log(JSON.stringify({ event: "auth_login_succeeded", requestId: req.requestId, userId: user.id }));
  const playerId = user.player_id || user.id;
  res.json({
    token: signToken(playerId, { username: user.username, email: user.email || "", authSource: "local" }),
    userId: user.id,
    playerId,
    username: user.username,
    email: user.email || "",
  });
}));

api.post("/auth/web-login", authIdentityLimiter, asyncRoute(async (req, res) => {
  let authResult;
  try {
    authResult = await webAuth.verifyLogin(req.body || {});
  } catch (e) {
    console.warn("[auth] Web login adapter exception:", e.message);
    return res.status(502).json({ error: "WEB_AUTH_ADAPTER_EXCEPTION" });
  }

  if (!authResult.ok) {
    return res.status(authResult.status || 401).json({ error: authResult.error || "WEB_AUTH_FAILED" });
  }

  const demoCandidate = await canonicalizeDemoAuthPayload({
    username: authResult.webUser.username || authResult.webUser.displayName || "",
  }, store);
  const player = demoCandidate.authSource === "local-demo" && demoCandidate.uid
    ? {
      id: demoCandidate.uid,
      webUserId: authResult.webUser.id,
      authSource: "local-demo",
      username: demoCandidate.username,
      displayName: demoCandidate.displayName || demoCandidate.username,
    }
    : await store.getOrCreatePlayerForWebUser(authResult.webUser);
  let profile = await store.getProfile(player.id);
  if (!profile) {
    profile = makeDefaultProfileForName(player.displayName);
    await store.setProfile(player.id, profile);
  }

  const token = signToken(player.id, {
    webUserId: player.webUserId,
    authSource: player.authSource,
    username: player.username,
    displayName: player.displayName || profile.name,
  });

  console.log(JSON.stringify({
    event: "web_auth_login_succeeded",
    requestId: req.requestId,
    webUserId: player.webUserId,
    playerId: player.id,
  }));
  res.json({
    token,
    userId: player.id,
    playerId: player.id,
    webUserId: player.webUserId,
    authSource: player.authSource,
    player_profile: profile,
  });
}));

// ── Player profile ──
api.get("/player/profile", auth, asyncRoute(async (req, res) => {
  let profile = await store.getProfile(req.userId);
  if (!profile) {
    profile = makeDefaultProfile();
    await store.setProfile(req.userId, profile);
  }
  res.json({ player_profile: profile });
}));

api.put("/player/profile", auth, asyncRoute(async (req, res) => {
  const incoming = (req.body && req.body.player_profile) || null;
  if (!incoming)
    return res.status(400).json({ error: "Thiếu player_profile trong body" });

  const current = await store.getProfile(req.userId) || makeDefaultProfile();
  const merged = { ...current, ...incoming, updatedAt: nowISO() };
  await store.setProfile(req.userId, merged);

  console.log(
    `[profile] Cập nhật ${req.userId}: characterCreated=${merged.characterCreated}, tutorialCompleted=${merged.tutorialCompleted}`
  );
  res.json({ ok: true, updatedAt: merged.updatedAt });
}));

api.get("/player/bootstrap", auth, asyncRoute(async (req, res) => {
  let profile = await store.getProfile(req.userId);
  if (!profile) {
    profile = makeDefaultProfile();
    await store.setProfile(req.userId, profile);
  }

  res.json({
    player_profile: profile,
    economy: await store.getEconomy(req.userId),
    inventory: await store.getInventory(req.userId),
    farm_state: await store.getFarmState(req.userId),
    daily_limits: await store.getDailyLimits(req.userId),
  });
}));

api.get("/player/economy", auth, asyncRoute(async (req, res) => {
  res.json({ economy: await store.getEconomy(req.userId) });
}));

api.put("/player/economy", auth, asyncRoute(async (req, res) => {
  const incoming = req.body && req.body.economy;
  if (!incoming) return res.status(400).json({ error: "Missing economy" });
  res.json({ ok: true, economy: await store.setEconomy(req.userId, incoming) });
}));

api.post("/player/economy/apply", auth, asyncRoute(async (req, res) => {
  const body = req.body || {};
  const result = await store.applyEconomyDelta(
    req.userId,
    { pos: body.delta_pos || body.deltaPos || 0, upos: body.delta_upos || body.deltaUpos || 0 },
    {
      type: body.type || "adjust",
      ref: body.ref || "",
      idempotencyKey: body.idempotency_key || body.idempotencyKey || "",
    }
  );

  if (!result.ok) return res.status(409).json({ error: result.error, economy: result.economy });
  res.json(result);
}));

api.get("/player/inventory", auth, asyncRoute(async (req, res) => {
  res.json({ inventory: await store.getInventory(req.userId) });
}));

api.put("/player/inventory", auth, asyncRoute(async (req, res) => {
  const incoming = req.body && req.body.inventory;
  if (!incoming) return res.status(400).json({ error: "Missing inventory" });
  res.json({ ok: true, inventory: await store.setInventory(req.userId, incoming) });
}));

api.post("/player/inventory/adjust", auth, asyncRoute(async (req, res) => {
  const body = req.body || {};
  const itemId = body.item_id || body.itemId;
  const quantityDelta = body.quantity_delta || body.quantityDelta;
  if (!itemId || !Number.isFinite(Number(quantityDelta))) {
    return res.status(400).json({ error: "Missing item_id or quantity_delta" });
  }

  const result = await store.adjustInventoryItem(req.userId, itemId, Number(quantityDelta), {
    type: body.type || "inventory_adjust",
    ref: body.ref || itemId,
    idempotencyKey: body.idempotency_key || body.idempotencyKey || "",
  });

  if (!result.ok) return res.status(409).json({ error: result.error, inventory: result.inventory });
  res.json(result);
}));

api.post("/player/shop/transaction", auth, asyncRoute(async (req, res) => {
  const body = req.body || {};
  const shopId = body.shop_id || body.shopId;
  const itemId = body.item_id || body.itemId;
  const mode = body.mode;
  const quantity = Number(body.quantity);
  const idempotencyKey = String(body.idempotency_key || body.idempotencyKey || "").trim();

  if (!Number.isInteger(quantity) || quantity < 1 || quantity > 999) {
    return res.status(400).json({ error: "INVALID_QUANTITY" });
  }
  if (!idempotencyKey) {
    return res.status(400).json({ error: "MISSING_IDEMPOTENCY_KEY" });
  }

  const offer = resolveShopOffer(shopId, mode, itemId);
  if (!offer.ok) {
    const status = ["SHOP_NOT_FOUND", "ITEM_NOT_FOUND"].includes(offer.error) ? 404
      : offer.error === "SHOP_ITEM_NOT_ALLOWED" ? 403
        : 400;
    return res.status(status).json({ error: offer.error });
  }

  const result = await store.transactShop(req.userId, offer, quantity, { idempotencyKey });
  if (!result.ok) {
    const status = ["INSUFFICIENT_BALANCE", "INSUFFICIENT_ITEM", "IDEMPOTENCY_CONFLICT"].includes(result.error)
      ? 409
      : 400;
    return res.status(status).json({
      error: result.error,
      economy: result.economy,
      inventory: result.inventory,
    });
  }

  res.json(result);
}));

api.get("/player/daily-limits", auth, asyncRoute(async (req, res) => {
  res.json({ daily_limits: await store.getDailyLimits(req.userId) });
}));

api.post("/player/daily-limits/consume", auth, asyncRoute(async (req, res) => {
  const body = req.body || {};
  const limitKey = body.limit_key || body.limitKey;
  const amount = body.amount == null ? 1 : body.amount;
  if (!limitKey) {
    return res.status(400).json({ error: "Missing limit_key" });
  }

  const result = await store.consumeDailyLimit(req.userId, limitKey, amount, {
    maxCount: body.max_count || body.maxCount || 10,
    periodKey: body.period_key || body.periodKey || "",
    type: body.type || `${limitKey}_daily_limit`,
    ref: body.ref || limitKey,
    idempotencyKey: body.idempotency_key || body.idempotencyKey || "",
  });

  if (!result.ok) {
    return res.status(409).json({
      error: result.error,
      daily_limits: result.daily_limits,
      limit: result.limit,
    });
  }

  res.json(result);
}));

api.get("/player/farm-state", auth, asyncRoute(async (req, res) => {
  res.json({ farm_state: await store.getFarmState(req.userId) });
}));

api.put("/player/farm-state", auth, asyncRoute(async (req, res) => {
  const incoming = req.body && req.body.farm_state;
  if (!incoming) return res.status(400).json({ error: "Missing farm_state" });
  res.json({ ok: true, farm_state: await store.setFarmState(req.userId, incoming) });
}));

app.use("/game-api", api);
app.use("/", api);

app.use((error, req, res, next) => {
  if (res.headersSent) return next(error);

  if (error && error.type === "entity.too.large") {
    return res.status(413).json({ error: "PAYLOAD_TOO_LARGE", requestId: req.requestId });
  }
  if (error instanceof SyntaxError && error.status === 400 && Object.prototype.hasOwnProperty.call(error, "body")) {
    return res.status(400).json({ error: "INVALID_JSON", requestId: req.requestId });
  }

  console.error(JSON.stringify({
    event: "api_error",
    requestId: req.requestId || "",
    method: req.method,
    path: req.path,
    errorCode: error && (error.code || error.name) || "UNKNOWN_ERROR",
  }));
  return res.status(500).json({ error: "INTERNAL_SERVER_ERROR", requestId: req.requestId });
});

const server = http.createServer(app);
server.requestTimeout = securityConfig.requestTimeoutMs;
server.headersTimeout = securityConfig.headersTimeoutMs;
server.keepAliveTimeout = securityConfig.keepAliveTimeoutMs;
server.maxHeadersCount = 100;

const realtime = attachRealtimeServer(server, { verifyToken: verifyTokenPayload, store });
let shuttingDown = false;

async function shutdown(signal) {
  if (shuttingDown) return;
  shuttingDown = true;
  console.log(JSON.stringify({ event: "shutdown_started", signal }));

  const forceExit = setTimeout(() => {
    console.error(JSON.stringify({ event: "shutdown_forced", signal }));
    process.exit(1);
  }, 15_000);
  forceExit.unref();

  realtime.close(1012, "Server restarting");
  server.close(async (serverError) => {
    try {
      await store.close();
      clearTimeout(forceExit);
      if (serverError) throw serverError;
      console.log(JSON.stringify({ event: "shutdown_completed", signal }));
      process.exit(0);
    } catch (error) {
      console.error(JSON.stringify({
        event: "shutdown_failed",
        signal,
        errorCode: error && (error.code || error.name) || "UNKNOWN_ERROR",
      }));
      process.exit(1);
    }
  });
}

process.once("SIGTERM", () => shutdown("SIGTERM"));
process.once("SIGINT", () => shutdown("SIGINT"));

async function startServer() {
  await ensureDemoAccounts(store, bcrypt);
  await new Promise((resolve, reject) => {
    server.once("error", reject);
    server.listen(PORT, HOST, () => {
      server.off("error", reject);
      console.log(`[ywonderland-stub] listening on ${HOST}:${PORT} store=${store.mode} security=enabled`);
      resolve();
    });
  });
}

startServer().catch(async (error) => {
  console.error("[startup] Failed to start backend:", error);
  try {
    await store.close();
  } catch (closeError) {
    console.error("[startup] Failed to close store:", closeError.message);
  }
  process.exitCode = 1;
});
