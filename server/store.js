// Minimal JSON-file store for dev/test backend.
// This is not production storage: no transactions, no locking, no DB indexes.
const fs = require("fs");
const path = require("path");

const DB_PATH = process.env.YW_DATA_PATH || path.join(__dirname, "data.json");

function nowISO() {
  return new Date().toISOString();
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
    transactions: [],
    sessions: [],
  };
}

function normalizeDb(db) {
  const source = db || {};
  return {
    ...emptyDb(),
    ...source,
    users: Array.isArray(source.users) ? source.users : [],
    profiles: source.profiles && typeof source.profiles === "object" ? source.profiles : {},
    players: source.players && typeof source.players === "object" ? source.players : {},
    playersByWebUserId:
      source.playersByWebUserId && typeof source.playersByWebUserId === "object"
        ? source.playersByWebUserId
        : {},
    economies: source.economies && typeof source.economies === "object" ? source.economies : {},
    inventories: source.inventories && typeof source.inventories === "object" ? source.inventories : {},
    farmStates: source.farmStates && typeof source.farmStates === "object" ? source.farmStates : {},
    transactions: Array.isArray(source.transactions) ? source.transactions : [],
    sessions: Array.isArray(source.sessions) ? source.sessions : [],
  };
}

function readAll() {
  try {
    if (!fs.existsSync(DB_PATH)) return emptyDb();
    const raw = fs.readFileSync(DB_PATH, "utf8");
    if (!raw.trim()) return emptyDb();
    return normalizeDb(JSON.parse(raw));
  } catch (e) {
    console.error("[store] Failed to read data.json, using empty DB:", e.message);
    return emptyDb();
  }
}

function writeAll(data) {
  fs.writeFileSync(DB_PATH, JSON.stringify(normalizeDb(data), null, 2), "utf8");
}

function generateId(prefix) {
  return `${prefix}_${Date.now()}_${Math.floor(Math.random() * 1e6)}`;
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

function ensurePlayerStateInDb(db, playerId) {
  if (!db.economies[playerId]) db.economies[playerId] = makeDefaultEconomy();
  if (!db.inventories[playerId]) db.inventories[playerId] = makeDefaultInventory();
  if (!db.farmStates[playerId]) db.farmStates[playerId] = makeDefaultFarmState();
}

function findSlot(inventory, itemId) {
  return inventory.slots.find((slot) => slot.itemId === itemId) || null;
}

module.exports = {
  readAll,
  writeAll,
  generateId,

  // Legacy local users. Kept for current Unity demo compatibility.
  findUserByName(username) {
    return readAll().users.find((u) => u.username === username) || null;
  },
  findUserById(id) {
    return readAll().users.find((u) => u.id === id) || null;
  },
  createUser(user) {
    const db = readAll();
    db.users.push(user);
    ensurePlayerStateInDb(db, user.id);
    writeAll(db);
    return user;
  },

  // Web-user -> game-player mapping. This is the bridge we can build before web API is ready.
  getOrCreatePlayerForWebUser(webUser) {
    if (!webUser || !webUser.id) {
      throw new Error("webUser.id is required");
    }

    const db = readAll();
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
      db.players[playerId] = {
        ...(db.players[playerId] || { id: playerId, createdAt: nowISO() }),
        webUserId: webUser.id,
        username: webUser.username || webUser.email || webUser.phone || db.players[playerId].username,
        displayName: webUser.displayName || webUser.username || db.players[playerId].displayName || "Player",
        authSource: webUser.authSource || db.players[playerId].authSource || "web",
        updatedAt: nowISO(),
      };
    }

    ensurePlayerStateInDb(db, playerId);
    writeAll(db);
    return db.players[playerId];
  },
  getPlayer(playerId) {
    return readAll().players[playerId] || null;
  },

  // Profiles (1 profile / playerId or legacy userId)
  getProfile(userId) {
    return readAll().profiles[userId] || null;
  },
  setProfile(userId, profile) {
    const db = readAll();
    db.profiles[userId] = profile;
    ensurePlayerStateInDb(db, userId);
    writeAll(db);
    return profile;
  },

  ensurePlayerState(playerId) {
    const db = readAll();
    ensurePlayerStateInDb(db, playerId);
    writeAll(db);
    return {
      economy: db.economies[playerId],
      inventory: db.inventories[playerId],
      farmState: db.farmStates[playerId],
    };
  },

  getEconomy(playerId) {
    const db = readAll();
    ensurePlayerStateInDb(db, playerId);
    writeAll(db);
    return db.economies[playerId];
  },
  setEconomy(playerId, economy) {
    const db = readAll();
    db.economies[playerId] = {
      ...(db.economies[playerId] || makeDefaultEconomy()),
      ...(economy || {}),
      version: Number((economy && economy.version) || (db.economies[playerId] && db.economies[playerId].version) || 1),
      pos: Number((economy && economy.pos) || 0),
      upos: Number((economy && economy.upos) || 0),
      updatedAt: nowISO(),
    };
    writeAll(db);
    return db.economies[playerId];
  },
  applyEconomyDelta(playerId, delta, meta) {
    const db = readAll();
    ensurePlayerStateInDb(db, playerId);
    const economy = db.economies[playerId];
    const deltaPos = Number((delta && delta.pos) || 0);
    const deltaUpos = Number((delta && delta.upos) || 0);

    if (economy.pos + deltaPos < 0 || economy.upos + deltaUpos < 0) {
      return { ok: false, error: "INSUFFICIENT_BALANCE", economy };
    }

    const idempotencyKey = meta && meta.idempotencyKey;
    if (idempotencyKey) {
      const existing = db.transactions.find((tx) => tx.idempotencyKey === idempotencyKey);
      if (existing) {
        return { ok: true, economy, transaction: existing, duplicate: true };
      }
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
      createdAt: nowISO(),
    };
    db.transactions.push(transaction);
    writeAll(db);
    return { ok: true, economy, transaction };
  },

  getInventory(playerId) {
    const db = readAll();
    ensurePlayerStateInDb(db, playerId);
    writeAll(db);
    return db.inventories[playerId];
  },
  setInventory(playerId, inventory) {
    const db = readAll();
    db.inventories[playerId] = {
      ...(db.inventories[playerId] || makeDefaultInventory()),
      ...(inventory || {}),
      updatedAt: nowISO(),
    };
    if (!Array.isArray(db.inventories[playerId].slots)) db.inventories[playerId].slots = [];
    writeAll(db);
    return db.inventories[playerId];
  },
  adjustInventoryItem(playerId, itemId, quantityDelta, meta) {
    const db = readAll();
    ensurePlayerStateInDb(db, playerId);
    const inventory = db.inventories[playerId];
    const deltaQty = Number(quantityDelta || 0);
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
      itemId,
      quantityDelta: deltaQty,
      createdAt: nowISO(),
    };
    db.transactions.push(transaction);
    writeAll(db);
    return { ok: true, inventory, transaction };
  },

  getFarmState(playerId) {
    const db = readAll();
    ensurePlayerStateInDb(db, playerId);
    writeAll(db);
    return db.farmStates[playerId];
  },
  setFarmState(playerId, farmState) {
    const db = readAll();
    db.farmStates[playerId] = {
      ...(db.farmStates[playerId] || makeDefaultFarmState()),
      ...(farmState || {}),
      updatedAt: nowISO(),
    };
    writeAll(db);
    return db.farmStates[playerId];
  },
};
