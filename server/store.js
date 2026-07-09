// Storage facade for the game backend.
// Default mode is a JSON-file store for local/dev. STORE_MODE=postgres selects
// the DB adapter scaffold in postgresStore.js.
const fs = require("fs");
const path = require("path");
const { createPostgresStore } = require("./postgresStore");

const STORE_MODE = (process.env.STORE_MODE || "json").toLowerCase();
const DB_PATH = process.env.YW_DATA_PATH || path.join(__dirname, "data.json");

function nowISO() {
  return new Date().toISOString();
}

function todayKey(date = new Date()) {
  return date.toISOString().slice(0, 10);
}

function emptyDb() {
  return {
    users: [],
    profiles: {},
    players: {},
    playersByWebUserId: {},
    economies: {},
    inventories: {},
    farmStates: {},
    dailyLimits: {},
    transactions: [],
    sessions: [],
  };
}

function normalizeObject(value) {
  return value && typeof value === "object" && !Array.isArray(value) ? value : {};
}

function normalizeDb(db) {
  const source = db || {};
  return {
    ...emptyDb(),
    ...source,
    users: Array.isArray(source.users) ? source.users : [],
    profiles: normalizeObject(source.profiles),
    players: normalizeObject(source.players),
    playersByWebUserId: normalizeObject(source.playersByWebUserId),
    economies: normalizeObject(source.economies),
    inventories: normalizeObject(source.inventories),
    farmStates: normalizeObject(source.farmStates),
    dailyLimits: normalizeObject(source.dailyLimits),
    transactions: Array.isArray(source.transactions) ? source.transactions : [],
    sessions: Array.isArray(source.sessions) ? source.sessions : [],
  };
}

function generateId(prefix) {
  return `${prefix}_${Date.now()}_${Math.floor(Math.random() * 1e6)}`;
}

function toNumber(value, fallback = 0) {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : fallback;
}

function toInt(value, fallback = 0) {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? Math.trunc(parsed) : fallback;
}

function normalizeIdentity(value) {
  return String(value || "").trim().toLowerCase();
}

function findSlot(inventory, itemId) {
  return inventory.slots.find((slot) => slot.itemId === itemId) || null;
}

function findTransactionByIdempotency(db, idempotencyKey) {
  if (!idempotencyKey) return null;
  return db.transactions.find((tx) => tx.idempotencyKey === idempotencyKey) || null;
}

function makeDefaultEconomy() {
  return {
    version: 1,
    pos: 5000,
    upos: 0,
    updatedAt: nowISO(),
  };
}

function makeDefaultInventory() {
  return {
    version: 1,
    maxSlots: 50,
    slots: [
      { itemId: "hoe_01", quantity: 1 },
      { itemId: "axe_01", quantity: 1 },
      { itemId: "pickaxe_01", quantity: 1 },
      { itemId: "fishing_rod_01", quantity: 1 },
      { itemId: "watering_can_01", quantity: 1 },
      { itemId: "carrot_seed_01", quantity: 5 },
    ],
    updatedAt: nowISO(),
  };
}

function makeDefaultFarmState() {
  return {
    version: 1,
    buildings: [],
    tiles: [],
    animals: [],
    resources: [],
    updatedAt: nowISO(),
  };
}

function makeDefaultDailyLimits() {
  return {
    version: 1,
    limits: {},
    updatedAt: nowISO(),
  };
}

class JsonStore {
  constructor(dbPath) {
    this.mode = "json";
    this.dbPath = dbPath;
  }

  readAll() {
    try {
      if (!fs.existsSync(this.dbPath)) return emptyDb();
      const raw = fs.readFileSync(this.dbPath, "utf8");
      if (!raw.trim()) return emptyDb();
      return normalizeDb(JSON.parse(raw));
    } catch (e) {
      console.error("[store] Failed to read data file, using empty DB:", e.message);
      return emptyDb();
    }
  }

  writeAll(data) {
    fs.writeFileSync(this.dbPath, JSON.stringify(normalizeDb(data), null, 2), "utf8");
  }

  generateId(prefix) {
    return generateId(prefix);
  }

  ensurePlayerStateInDb(db, playerId) {
    if (!db.economies[playerId]) db.economies[playerId] = makeDefaultEconomy();
    if (!db.inventories[playerId]) db.inventories[playerId] = makeDefaultInventory();
    if (!db.farmStates[playerId]) db.farmStates[playerId] = makeDefaultFarmState();
    if (!db.dailyLimits[playerId]) db.dailyLimits[playerId] = makeDefaultDailyLimits();
    this.ensureDefaultDailyLimit(db.dailyLimits[playerId], "fishing", 10);
    this.ensureDefaultDailyLimit(db.dailyLimits[playerId], "mining", 10);
  }

  ensureDefaultDailyLimit(dailyLimits, limitKey, maxCount, periodKey = todayKey()) {
    if (!dailyLimits.limits || typeof dailyLimits.limits !== "object") {
      dailyLimits.limits = {};
    }

    const current = dailyLimits.limits[limitKey];
    if (!current || current.periodKey !== periodKey) {
      dailyLimits.limits[limitKey] = {
        limitKey,
        periodKey,
        used: 0,
        maxCount,
        remaining: maxCount,
        updatedAt: nowISO(),
      };
      dailyLimits.updatedAt = nowISO();
      return dailyLimits.limits[limitKey];
    }

    current.maxCount = toInt(current.maxCount, maxCount);
    current.used = Math.max(0, toInt(current.used, 0));
    current.remaining = Math.max(0, current.maxCount - current.used);
    return current;
  }

  findUserByName(username) {
    const key = normalizeIdentity(username);
    if (!key) return null;
    return this.readAll().users.find((u) => normalizeIdentity(u.username) === key) || null;
  }

  findUserByEmail(email) {
    const key = normalizeIdentity(email);
    if (!key) return null;
    return this.readAll().users.find((u) => normalizeIdentity(u.email) === key) || null;
  }

  findUserById(id) {
    return this.readAll().users.find((u) => u.id === id) || null;
  }

  createUser(user) {
    const db = this.readAll();
    db.users.push(user);
    this.ensurePlayerStateInDb(db, user.id);
    this.writeAll(db);
    return user;
  }

  getOrCreatePlayerForWebUser(webUser) {
    if (!webUser || !webUser.id) {
      throw new Error("webUser.id is required");
    }

    const db = this.readAll();
    let playerId = db.playersByWebUserId[webUser.id];
    if (!playerId) {
      playerId = generateId("p");
      db.playersByWebUserId[webUser.id] = playerId;
      db.players[playerId] = {
        id: playerId,
        webUserId: webUser.id,
        username: webUser.username || webUser.email || webUser.phone || webUser.id,
        displayName: webUser.displayName || webUser.username || "Player",
        authSource: webUser.authSource || "web",
        createdAt: nowISO(),
        updatedAt: nowISO(),
      };
    } else {
      const current = db.players[playerId] || { id: playerId, createdAt: nowISO() };
      db.players[playerId] = {
        ...current,
        webUserId: webUser.id,
        username: webUser.username || webUser.email || webUser.phone || current.username,
        displayName: webUser.displayName || webUser.username || current.displayName || "Player",
        authSource: webUser.authSource || current.authSource || "web",
        updatedAt: nowISO(),
      };
    }

    this.ensurePlayerStateInDb(db, playerId);
    this.writeAll(db);
    return db.players[playerId];
  }

  getPlayer(playerId) {
    return this.readAll().players[playerId] || null;
  }

  getProfile(userId) {
    return this.readAll().profiles[userId] || null;
  }

  setProfile(userId, profile) {
    const db = this.readAll();
    db.profiles[userId] = profile;
    this.ensurePlayerStateInDb(db, userId);
    this.writeAll(db);
    return profile;
  }

  ensurePlayerState(playerId) {
    const db = this.readAll();
    this.ensurePlayerStateInDb(db, playerId);
    this.writeAll(db);
    return {
      economy: db.economies[playerId],
      inventory: db.inventories[playerId],
      farmState: db.farmStates[playerId],
      dailyLimits: db.dailyLimits[playerId],
    };
  }

  getEconomy(playerId) {
    const db = this.readAll();
    this.ensurePlayerStateInDb(db, playerId);
    this.writeAll(db);
    return db.economies[playerId];
  }

  setEconomy(playerId, economy) {
    const db = this.readAll();
    const current = db.economies[playerId] || makeDefaultEconomy();
    const incoming = economy || {};
    db.economies[playerId] = {
      ...current,
      ...incoming,
      version: toInt(incoming.version, current.version || 1),
      pos: toNumber(incoming.pos, current.pos || 0),
      upos: toNumber(incoming.upos, current.upos || 0),
      updatedAt: nowISO(),
    };
    this.writeAll(db);
    return db.economies[playerId];
  }

  applyEconomyDelta(playerId, delta, meta) {
    const db = this.readAll();
    this.ensurePlayerStateInDb(db, playerId);

    const idempotencyKey = meta && meta.idempotencyKey;
    const existing = findTransactionByIdempotency(db, idempotencyKey);
    if (existing) {
      return {
        ok: true,
        economy: existing.economyAfter || db.economies[playerId],
        transaction: existing,
        duplicate: true,
      };
    }

    const economy = db.economies[playerId];
    const deltaPos = toNumber(delta && delta.pos, 0);
    const deltaUpos = toNumber(delta && delta.upos, 0);

    if (economy.pos + deltaPos < 0 || economy.upos + deltaUpos < 0) {
      return { ok: false, error: "INSUFFICIENT_BALANCE", economy };
    }

    economy.pos += deltaPos;
    economy.upos += deltaUpos;
    economy.updatedAt = nowISO();

    const transaction = {
      id: generateId("tx"),
      playerId,
      type: (meta && meta.type) || "adjust",
      ref: (meta && meta.ref) || "",
      idempotencyKey: idempotencyKey || "",
      deltaPos,
      deltaUpos,
      economyAfter: { pos: economy.pos, upos: economy.upos },
      createdAt: nowISO(),
    };
    db.transactions.push(transaction);
    this.writeAll(db);
    return { ok: true, economy, transaction };
  }

  getInventory(playerId) {
    const db = this.readAll();
    this.ensurePlayerStateInDb(db, playerId);
    this.writeAll(db);
    return db.inventories[playerId];
  }

  setInventory(playerId, inventory) {
    const db = this.readAll();
    db.inventories[playerId] = {
      ...(db.inventories[playerId] || makeDefaultInventory()),
      ...(inventory || {}),
      updatedAt: nowISO(),
    };
    if (!Array.isArray(db.inventories[playerId].slots)) db.inventories[playerId].slots = [];
    this.ensurePlayerStateInDb(db, playerId);
    this.writeAll(db);
    return db.inventories[playerId];
  }

  adjustInventoryItem(playerId, itemId, quantityDelta, meta) {
    const db = this.readAll();
    this.ensurePlayerStateInDb(db, playerId);

    const idempotencyKey = meta && meta.idempotencyKey;
    const existing = findTransactionByIdempotency(db, idempotencyKey);
    if (existing) {
      return {
        ok: true,
        inventory: existing.inventoryAfter || db.inventories[playerId],
        transaction: existing,
        duplicate: true,
      };
    }

    const inventory = db.inventories[playerId];
    const deltaQty = toInt(quantityDelta, 0);
    let slot = findSlot(inventory, itemId);

    if (!slot && deltaQty < 0) {
      return { ok: false, error: "ITEM_NOT_FOUND", inventory };
    }

    if (!slot) {
      if (inventory.slots.length >= inventory.maxSlots) inventory.maxSlots += 1;
      slot = { itemId, quantity: 0 };
      inventory.slots.push(slot);
    }

    if (slot.quantity + deltaQty < 0) {
      return { ok: false, error: "INSUFFICIENT_ITEM", inventory };
    }

    slot.quantity += deltaQty;
    inventory.slots = inventory.slots.filter((s) => s.quantity > 0);
    inventory.updatedAt = nowISO();

    const transaction = {
      id: generateId("itx"),
      playerId,
      type: (meta && meta.type) || "inventory_adjust",
      ref: (meta && meta.ref) || itemId,
      idempotencyKey: idempotencyKey || "",
      itemId,
      quantityDelta: deltaQty,
      inventoryAfter: JSON.parse(JSON.stringify(inventory)),
      createdAt: nowISO(),
    };
    db.transactions.push(transaction);
    this.writeAll(db);
    return { ok: true, inventory, transaction };
  }

  getFarmState(playerId) {
    const db = this.readAll();
    this.ensurePlayerStateInDb(db, playerId);
    this.writeAll(db);
    return db.farmStates[playerId];
  }

  setFarmState(playerId, farmState) {
    const db = this.readAll();
    db.farmStates[playerId] = {
      ...(db.farmStates[playerId] || makeDefaultFarmState()),
      ...(farmState || {}),
      updatedAt: nowISO(),
    };
    this.writeAll(db);
    return db.farmStates[playerId];
  }

  getDailyLimits(playerId) {
    const db = this.readAll();
    this.ensurePlayerStateInDb(db, playerId);
    this.writeAll(db);
    return db.dailyLimits[playerId];
  }

  consumeDailyLimit(playerId, limitKey, amount, options = {}) {
    const db = this.readAll();
    this.ensurePlayerStateInDb(db, playerId);

    const idempotencyKey = options.idempotencyKey || "";
    const existing = findTransactionByIdempotency(db, idempotencyKey);
    if (existing) {
      return {
        ok: true,
        daily_limits: existing.dailyLimitsAfter || db.dailyLimits[playerId],
        limit: existing.limitAfter || null,
        transaction: existing,
        duplicate: true,
      };
    }

    const key = String(limitKey || "").trim();
    if (!key) {
      return { ok: false, error: "MISSING_LIMIT_KEY", daily_limits: db.dailyLimits[playerId] };
    }

    const consumeAmount = Math.max(1, toInt(amount, 1));
    const maxCount = Math.max(1, toInt(options.maxCount, 10));
    const periodKey = String(options.periodKey || todayKey());
    const dailyLimits = db.dailyLimits[playerId];
    const limit = this.ensureDefaultDailyLimit(dailyLimits, key, maxCount, periodKey);

    if (limit.used + consumeAmount > limit.maxCount) {
      return {
        ok: false,
        error: "DAILY_LIMIT_EXCEEDED",
        daily_limits: dailyLimits,
        limit,
      };
    }

    limit.used += consumeAmount;
    limit.remaining = Math.max(0, limit.maxCount - limit.used);
    limit.updatedAt = nowISO();
    dailyLimits.updatedAt = nowISO();

    const transaction = {
      id: generateId("ltx"),
      playerId,
      type: (options.type || `${key}_daily_limit`).trim(),
      ref: options.ref || key,
      idempotencyKey,
      limitKey: key,
      amount: consumeAmount,
      periodKey,
      limitAfter: { ...limit },
      dailyLimitsAfter: JSON.parse(JSON.stringify(dailyLimits)),
      createdAt: nowISO(),
    };
    db.transactions.push(transaction);
    this.writeAll(db);

    return { ok: true, daily_limits: dailyLimits, limit, transaction };
  }
}

function createActiveStore() {
  if (STORE_MODE === "json") {
    return new JsonStore(DB_PATH);
  }

  if (STORE_MODE === "postgres") {
    return createPostgresStore({
      databaseUrl: process.env.DATABASE_URL || process.env.POSTGRES_URL || "",
    });
  }

  throw new Error(`Unknown STORE_MODE: ${STORE_MODE}`);
}

const activeStore = createActiveStore();

module.exports = {
  mode: activeStore.mode,
  JsonStore,
  generateId,
  readAll: activeStore.readAll.bind(activeStore),
  writeAll: activeStore.writeAll.bind(activeStore),
  findUserByName: activeStore.findUserByName.bind(activeStore),
  findUserByEmail: activeStore.findUserByEmail.bind(activeStore),
  findUserById: activeStore.findUserById.bind(activeStore),
  createUser: activeStore.createUser.bind(activeStore),
  getOrCreatePlayerForWebUser: activeStore.getOrCreatePlayerForWebUser.bind(activeStore),
  getPlayer: activeStore.getPlayer.bind(activeStore),
  getProfile: activeStore.getProfile.bind(activeStore),
  setProfile: activeStore.setProfile.bind(activeStore),
  ensurePlayerState: activeStore.ensurePlayerState.bind(activeStore),
  getEconomy: activeStore.getEconomy.bind(activeStore),
  setEconomy: activeStore.setEconomy.bind(activeStore),
  applyEconomyDelta: activeStore.applyEconomyDelta.bind(activeStore),
  getInventory: activeStore.getInventory.bind(activeStore),
  setInventory: activeStore.setInventory.bind(activeStore),
  adjustInventoryItem: activeStore.adjustInventoryItem.bind(activeStore),
  getFarmState: activeStore.getFarmState.bind(activeStore),
  setFarmState: activeStore.setFarmState.bind(activeStore),
  getDailyLimits: activeStore.getDailyLimits.bind(activeStore),
  consumeDailyLimit: activeStore.consumeDailyLimit.bind(activeStore),
};
