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

ensureDemoAccounts(store, bcrypt);

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

function verifyTokenPayload(token) {
  return canonicalizeDemoAuthPayload(jwt.verify(token, JWT_SECRET), store);
}

// Middleware: xác thực Bearer token -> gắn req.userId
function auth(req, res, next) {
  const header = req.headers["authorization"] || "";
  const token = header.startsWith("Bearer ") ? header.slice(7) : null;
  if (!token) return res.status(401).json({ error: "Thiếu token" });
  try {
    const payload = verifyTokenPayload(token);
    req.userId = payload.uid;
    req.auth = payload;
    next();
  } catch (e) {
    return res.status(401).json({ error: "Token không hợp lệ" });
  }
}

// ── Health check ──
api.get("/", (req, res) => res.json({ ok: true, service: "ywonderland-stub" }));
api.get("/health", (req, res) =>
  res.json({ ok: true, service: "ywonderland-stub", checkedAt: nowISO() })
);

// ── Auth ──
api.post("/auth/register", (req, res) => {
  const username = String((req.body && req.body.username) || "").trim();
  const password = String((req.body && req.body.password) || "");
  const email = String((req.body && req.body.email) || "").trim().toLowerCase();
  const phone = String((req.body && req.body.phone) || "").trim();
  if (!username || !password)
    return res.status(400).json({ error: "Thiếu username/password" });
  if (store.findUserByName(username))
    return res.status(409).json({ error: "USERNAME_EXISTS" });
  if (email && store.findUserByEmail(email))
    return res.status(409).json({ error: "EMAIL_EXISTS" });

  const id = "u_" + Date.now() + "_" + Math.floor(Math.random() * 1e6);
  store.createUser({
    id,
    username,
    email,
    phone,
    password_hash: bcrypt.hashSync(password, 8),
    created_at: nowISO(),
    updated_at: nowISO(),
  });

  // Tạo profile mặc định gắn tên đăng nhập
  const profile = makeDefaultProfile();
  profile.name = username;
  store.setProfile(id, profile);

  console.log(`[auth] Đăng ký mới: ${username} (${id})`);
  res.json({ token: signToken(id, { username, email, authSource: "local" }), userId: id, playerId: id, username, email });
});

api.post("/auth/login", (req, res) => {
  const username = String((req.body && req.body.username) || "").trim();
  const password = String((req.body && req.body.password) || "");
  if (!username || !password)
    return res.status(400).json({ error: "Thiếu username/password" });

  const user = store.findUserByName(username);
  if (!user)
    return res.status(404).json({ error: "USER_NOT_FOUND" });
  const passwordOk =
    bcrypt.compareSync(password, user.password_hash) ||
    isAllowedDemoPassword(user.username, password);
  if (!passwordOk)
    return res.status(401).json({ error: "Sai username hoặc mật khẩu" });

  console.log(`[auth] Đăng nhập: ${username} (${user.id})`);
  res.json({
    token: signToken(user.id, { username: user.username, email: user.email || "", authSource: "local" }),
    userId: user.id,
    playerId: user.id,
    username: user.username,
    email: user.email || "",
  });
});

api.post("/auth/web-login", async (req, res) => {
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

  const player = store.getOrCreatePlayerForWebUser(authResult.webUser);
  let profile = store.getProfile(player.id);
  if (!profile) {
    profile = makeDefaultProfileForName(player.displayName);
    store.setProfile(player.id, profile);
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
});

// ── Player profile ──
api.get("/player/profile", auth, (req, res) => {
  let profile = store.getProfile(req.userId);
  if (!profile) {
    profile = makeDefaultProfile();
    store.setProfile(req.userId, profile);
  }
  res.json({ player_profile: profile });
});

api.put("/player/profile", auth, (req, res) => {
  const incoming = (req.body && req.body.player_profile) || null;
  if (!incoming)
    return res.status(400).json({ error: "Thiếu player_profile trong body" });

  const current = store.getProfile(req.userId) || makeDefaultProfile();
  const merged = { ...current, ...incoming, updatedAt: nowISO() };
  store.setProfile(req.userId, merged);

  console.log(
    `[profile] Cập nhật ${req.userId}: characterCreated=${merged.characterCreated}, tutorialCompleted=${merged.tutorialCompleted}`
  );
  res.json({ ok: true, updatedAt: merged.updatedAt });
});

api.get("/player/bootstrap", auth, (req, res) => {
  let profile = store.getProfile(req.userId);
  if (!profile) {
    profile = makeDefaultProfile();
    store.setProfile(req.userId, profile);
  }

  res.json({
    player_profile: profile,
    economy: store.getEconomy(req.userId),
    inventory: store.getInventory(req.userId),
    farm_state: store.getFarmState(req.userId),
    daily_limits: store.getDailyLimits(req.userId),
  });
});

api.get("/player/economy", auth, (req, res) => {
  res.json({ economy: store.getEconomy(req.userId) });
});

api.put("/player/economy", auth, (req, res) => {
  const incoming = req.body && req.body.economy;
  if (!incoming) return res.status(400).json({ error: "Missing economy" });
  res.json({ ok: true, economy: store.setEconomy(req.userId, incoming) });
});

api.post("/player/economy/apply", auth, (req, res) => {
  const body = req.body || {};
  const result = store.applyEconomyDelta(
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
});

api.get("/player/inventory", auth, (req, res) => {
  res.json({ inventory: store.getInventory(req.userId) });
});

api.put("/player/inventory", auth, (req, res) => {
  const incoming = req.body && req.body.inventory;
  if (!incoming) return res.status(400).json({ error: "Missing inventory" });
  res.json({ ok: true, inventory: store.setInventory(req.userId, incoming) });
});

api.post("/player/inventory/adjust", auth, (req, res) => {
  const body = req.body || {};
  const itemId = body.item_id || body.itemId;
  const quantityDelta = body.quantity_delta || body.quantityDelta;
  if (!itemId || !Number.isFinite(Number(quantityDelta))) {
    return res.status(400).json({ error: "Missing item_id or quantity_delta" });
  }

  const result = store.adjustInventoryItem(req.userId, itemId, Number(quantityDelta), {
    type: body.type || "inventory_adjust",
    ref: body.ref || itemId,
    idempotencyKey: body.idempotency_key || body.idempotencyKey || "",
  });

  if (!result.ok) return res.status(409).json({ error: result.error, inventory: result.inventory });
  res.json(result);
});

api.post("/player/shop/transaction", auth, (req, res) => {
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

  const result = store.transactShop(req.userId, offer, quantity, { idempotencyKey });
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
});

api.get("/player/daily-limits", auth, (req, res) => {
  res.json({ daily_limits: store.getDailyLimits(req.userId) });
});

api.post("/player/daily-limits/consume", auth, (req, res) => {
  const body = req.body || {};
  const limitKey = body.limit_key || body.limitKey;
  const amount = body.amount == null ? 1 : body.amount;
  if (!limitKey) {
    return res.status(400).json({ error: "Missing limit_key" });
  }

  const result = store.consumeDailyLimit(req.userId, limitKey, amount, {
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
});

api.get("/player/farm-state", auth, (req, res) => {
  res.json({ farm_state: store.getFarmState(req.userId) });
});

api.put("/player/farm-state", auth, (req, res) => {
  const incoming = req.body && req.body.farm_state;
  if (!incoming) return res.status(400).json({ error: "Missing farm_state" });
  res.json({ ok: true, farm_state: store.setFarmState(req.userId, incoming) });
});

app.use("/game-api", api);
app.use("/", api);

const server = http.createServer(app);
attachRealtimeServer(server, { verifyToken: verifyTokenPayload, store });

server.listen(PORT, () => {
  console.log(`[ywonderland-stub] listening on :${PORT}`);
});
