// index.js — Server stub REST cho YWONDERLAND (Đợt 1: Profile + Tutorial).
// CHỈ dùng cho dev/test ở local. KHÔNG bảo mật cho production (JWT secret cứng, không rate-limit).
const express = require("express");
const http = require("http");
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

const PORT = process.env.PORT || 3000;
const JWT_SECRET = process.env.JWT_SECRET || "ywonderland_dev_secret_change_me";
const TOKEN_TTL = "30d";

const app = express();
app.use(cors());
app.use(express.json());

function asyncRoute(handler) {
  return (req, res, next) => Promise.resolve(handler(req, res, next)).catch(next);
}

if (process.env.ADMIN_DASHBOARD_ENABLED !== "false") {
  app.use("/admin", createAdminDashboardRouter());
}

// Mount the same routes for local dev and the public Unity baseUrl.
const api = express.Router();

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
  return jwt.sign({ uid: userId, ...extra }, JWT_SECRET, { expiresIn: TOKEN_TTL });
}

async function verifyTokenPayload(token) {
  return canonicalizeDemoAuthPayload(jwt.verify(token, JWT_SECRET), store);
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
api.post("/auth/register", asyncRoute(async (req, res) => {
  const username = String((req.body && req.body.username) || "").trim();
  const password = String((req.body && req.body.password) || "");
  const email = String((req.body && req.body.email) || "").trim().toLowerCase();
  const phone = String((req.body && req.body.phone) || "").trim();
  if (!username || !password)
    return res.status(400).json({ error: "Thiếu username/password" });
  if (await store.findUserByName(username))
    return res.status(409).json({ error: "USERNAME_EXISTS" });
  if (email && await store.findUserByEmail(email))
    return res.status(409).json({ error: "EMAIL_EXISTS" });

  const id = "u_" + Date.now() + "_" + Math.floor(Math.random() * 1e6);
  try {
    await store.createUser({
      id,
      username,
      email,
      phone,
      password_hash: bcrypt.hashSync(password, 8),
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

  console.log(`[auth] Đăng ký mới: ${username} (${id})`);
  res.json({ token: signToken(id, { username, email, authSource: "local" }), userId: id, playerId: id, username, email });
}));

api.post("/auth/login", asyncRoute(async (req, res) => {
  const username = String((req.body && req.body.username) || "").trim();
  const password = String((req.body && req.body.password) || "");
  if (!username || !password)
    return res.status(400).json({ error: "Thiếu username/password" });

  const user = await store.findUserByName(username);
  if (!user)
    return res.status(404).json({ error: "USER_NOT_FOUND" });
  const passwordOk =
    bcrypt.compareSync(password, user.password_hash) ||
    isAllowedDemoPassword(user.username, password);
  if (!passwordOk)
    return res.status(401).json({ error: "Sai username hoặc mật khẩu" });

  console.log(`[auth] Đăng nhập: ${username} (${user.id})`);
  const playerId = user.player_id || user.id;
  res.json({
    token: signToken(playerId, { username: user.username, email: user.email || "", authSource: "local" }),
    userId: user.id,
    playerId,
    username: user.username,
    email: user.email || "",
  });
}));

api.post("/auth/web-login", asyncRoute(async (req, res) => {
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

  console.log(`[auth] Web login mapped ${player.webUserId} -> ${player.id}`);
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
  console.error(`[api] ${req.method} ${req.originalUrl} failed:`, error);
  if (res.headersSent) return next(error);
  return res.status(500).json({ error: "INTERNAL_SERVER_ERROR" });
});

const server = http.createServer(app);
attachRealtimeServer(server, { verifyToken: verifyTokenPayload, store });

async function startServer() {
  await ensureDemoAccounts(store, bcrypt);
  await new Promise((resolve, reject) => {
    server.once("error", reject);
    server.listen(PORT, () => {
      server.off("error", reject);
      console.log(`[ywonderland-stub] listening on :${PORT} store=${store.mode}`);
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
