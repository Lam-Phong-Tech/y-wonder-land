// Storage facade for the game backend.
// Default mode is a JSON-file store for local/dev. STORE_MODE=postgres selects
// the PostgreSQL adapter while preserving the same API contract.
const fs = require("fs");
const path = require("path");
const { prepareAnimalPlacement } = require("./farmAnimalPlacement");
const {
  normalizePointSourceLot,
  pointSourceLotRequestSignature,
  planPointSourceFifo,
} = require("./pointSourceLedger");

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
    playerSessions: {},
    economies: {},
    webTopupPointMicrosRemainders: {},
    pointWalletReservations: {},
    pointSourceLots: {},
    inventories: {},
    farmStates: {},
    dailyLimits: {},
    transactions: [],
    sessions: [],
    browserAuthRequests: {},
  };
}

function normalizeObject(value) {
  return value && typeof value === "object" && !Array.isArray(value) ? value : {};
}

function normalizeEconomy(value) {
  const economy = normalizeObject(value);
  return {
    version: toInt(economy.version, 1),
    pos: toNumber(economy.pos, 5000),
    updatedAt: economy.updatedAt || nowISO(),
  };
}

function normalizeTransaction(value) {
  const transaction = { ...normalizeObject(value) };
  delete transaction.deltaUpos;
  if (transaction.economyAfter) {
    transaction.economyAfter = normalizeEconomy(transaction.economyAfter);
  }
  return transaction;
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
    playerSessions: normalizeObject(source.playerSessions),
    economies: Object.fromEntries(
      Object.entries(normalizeObject(source.economies))
        .map(([playerId, economy]) => [playerId, normalizeEconomy(economy)])
    ),
    webTopupPointMicrosRemainders: normalizeObject(source.webTopupPointMicrosRemainders),
    pointWalletReservations: normalizeObject(source.pointWalletReservations),
    pointSourceLots: normalizeObject(source.pointSourceLots),
    inventories: normalizeObject(source.inventories),
    farmStates: normalizeObject(source.farmStates),
    dailyLimits: normalizeObject(source.dailyLimits),
    transactions: Array.isArray(source.transactions)
      ? source.transactions.map(normalizeTransaction)
      : [],
    sessions: Array.isArray(source.sessions) ? source.sessions : [],
    browserAuthRequests: normalizeObject(source.browserAuthRequests),
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

function pointSourceLotFromStored(value) {
  const source = normalizeObject(value);
  const lot = normalizePointSourceLot(source);
  return {
    ...lot,
    requestSignature: String(source.requestSignature || pointSourceLotRequestSignature(lot)),
    createdAt: source.createdAt || lot.occurredAt,
    updatedAt: source.updatedAt || source.createdAt || lot.occurredAt,
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

  close() {}

  healthCheck() {
    return { ok: true, mode: this.mode };
  }

  ensurePlayerStateInDb(db, playerId) {
    if (!db.economies[playerId]) db.economies[playerId] = makeDefaultEconomy();
    const remainder = Number(db.webTopupPointMicrosRemainders[playerId]);
    if (!Number.isSafeInteger(remainder) || remainder < 0 || remainder >= 1_000_000) {
      db.webTopupPointMicrosRemainders[playerId] = 0;
    }
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
    const usernameKey = normalizeIdentity(user && user.username);
    const emailKey = normalizeIdentity(user && user.email);
    if (db.users.some((current) => normalizeIdentity(current.username) === usernameKey)) {
      const error = new Error("USERNAME_EXISTS");
      error.code = "23505";
      error.constraint = "ux_game_accounts_username_ci";
      throw error;
    }
    if (emailKey && db.users.some((current) => normalizeIdentity(current.email) === emailKey)) {
      const error = new Error("EMAIL_EXISTS");
      error.code = "23505";
      error.constraint = "ux_game_accounts_email_ci";
      throw error;
    }
    db.users.push(user);
    if (!db.players[user.id]) {
      db.players[user.id] = {
        id: user.id,
        webUserId: "",
        username: user.username,
        displayName: user.username || "Player",
        authSource: "local",
        createdAt: user.created_at || nowISO(),
        updatedAt: user.updated_at || nowISO(),
      };
    }
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

  getWebPointBalance(webUserId) {
    const db = this.readAll();
    const playerId = db.playersByWebUserId[String(webUserId || "").trim()];
    if (!playerId) return null;
    const player = db.players[playerId];
    const economy = db.economies[playerId];
    if (!player || !economy) return null;
    return {
      player: JSON.parse(JSON.stringify(player)),
      economy: normalizeEconomy(economy),
    };
  }

  recordPointSourceLot(playerId, input) {
    const db = this.readAll();
    const knownPlayer = Boolean(db.players[playerId])
      || db.users.some((user) => user && user.id === playerId);
    if (!knownPlayer) return { ok: false, error: "PLAYER_NOT_FOUND" };

    let lot;
    let requestSignature;
    try {
      lot = normalizePointSourceLot({ ...(input || {}), playerId });
      requestSignature = pointSourceLotRequestSignature(lot);
    } catch (error) {
      return { ok: false, error: error.message || "INVALID_POINT_SOURCE_LOT" };
    }
    if (lot.acquisitionType === "TRANSFER") {
      return { ok: false, error: "POINT_TRANSFER_REQUIRES_ATOMIC_OPERATION" };
    }

    const existingRaw = Object.values(db.pointSourceLots).find((candidate) => (
      candidate
      && candidate.playerId === lot.playerId
      && candidate.sourceEventId === lot.sourceEventId
      && Number(candidate.sourceEventIndex || 0) === lot.sourceEventIndex
    ));
    if (existingRaw) {
      const existing = pointSourceLotFromStored(existingRaw);
      if (existing.requestSignature !== requestSignature) {
        return { ok: false, error: "POINT_SOURCE_IDEMPOTENCY_CONFLICT" };
      }
      return { ok: true, duplicate: true, lot: existing };
    }
    if (db.pointSourceLots[lot.id]) {
      return { ok: false, error: "POINT_SOURCE_IDEMPOTENCY_CONFLICT" };
    }

    const timestamp = nowISO();
    const stored = {
      ...lot,
      requestSignature,
      createdAt: timestamp,
      updatedAt: timestamp,
    };
    db.pointSourceLots[stored.id] = stored;
    this.writeAll(db);
    return { ok: true, duplicate: false, lot: pointSourceLotFromStored(stored) };
  }

  getPointSourceLots(playerId, options = {}) {
    const includeDepleted = options.includeDepleted !== false;
    return Object.values(this.readAll().pointSourceLots)
      .filter((candidate) => candidate && candidate.playerId === playerId)
      .map(pointSourceLotFromStored)
      .filter((lot) => includeDepleted || lot.remainingPointMicros > 0)
      .sort((left, right) => left.occurredAt.localeCompare(right.occurredAt)
        || left.createdAt.localeCompare(right.createdAt)
        || left.id.localeCompare(right.id));
  }

  previewPointSourceFifo(playerId, pointAmountMicros) {
    return planPointSourceFifo(
      this.getPointSourceLots(playerId, { includeDepleted: false }),
      pointAmountMicros
    );
  }

  applyWebPointReservation(operation, input) {
    const op = String(operation || "").trim().toLowerCase();
    const reservationId = String(input && input.reservationId || "").trim();
    const webUserId = String(input && input.webUserId || "").trim();
    const expectedPlayerId = String(input && input.expectedPlayerId || "").trim();
    const pointAmount = Number(input && input.pointAmount);
    const purpose = String(input && input.purpose || "").trim();
    const source = String(input && input.source || "").trim();
    const occurredAt = String(input && input.occurredAt || "").trim();
    if (!["reserve", "capture", "release"].includes(op)
        || !reservationId || !webUserId || !expectedPlayerId || !purpose || !source
        || !Number.isSafeInteger(pointAmount) || pointAmount < 1) {
      return { ok: false, error: "INVALID_POINT_RESERVATION" };
    }

    const db = this.readAll();
    const mappedPlayerId = db.playersByWebUserId[webUserId];
    if (mappedPlayerId !== expectedPlayerId || !db.players[mappedPlayerId]) {
      return { ok: false, error: "GAME_POINT_IDENTITY_MISMATCH" };
    }
    this.ensurePlayerStateInDb(db, mappedPlayerId);
    const player = JSON.parse(JSON.stringify(db.players[mappedPlayerId]));
    const economy = db.economies[mappedPlayerId];
    const requestSignature = JSON.stringify({
      reservationId,
      webUserId,
      expectedPlayerId,
      pointAmount,
      purpose,
      source,
      occurredAt,
    });
    let reservation = db.pointWalletReservations[reservationId];
    if (reservation && (reservation.playerId !== mappedPlayerId
        || reservation.requestSignature !== requestSignature)) {
      return { ok: false, error: "IDEMPOTENCY_CONFLICT" };
    }

    const transactionFor = (currentOperation) => findTransactionByIdempotency(
      db,
      `web_point_${currentOperation}:${source}:${reservationId}`
    );
    const result = (duplicate, transaction) => ({
      ok: true,
      duplicate,
      player,
      economy: normalizeEconomy(economy),
      reservation: JSON.parse(JSON.stringify(reservation)),
      transaction: transaction || null,
    });

    if (op === "reserve") {
      if (reservation) return result(true, transactionFor("reserve"));
      if (economy.pos < pointAmount) {
        return { ok: false, error: "INSUFFICIENT_BALANCE", economy: normalizeEconomy(economy) };
      }

      economy.pos -= pointAmount;
      economy.updatedAt = nowISO();
      reservation = {
        id: reservationId,
        playerId: mappedPlayerId,
        webUserId,
        expectedPlayerId,
        pointAmount,
        purpose,
        source,
        occurredAt,
        requestSignature,
        status: "RESERVED",
        createdAt: nowISO(),
        updatedAt: nowISO(),
      };
      db.pointWalletReservations[reservationId] = reservation;
      const transaction = {
        id: generateId("wpr"),
        playerId: mappedPlayerId,
        type: "web_point_reserve",
        ref: reservationId,
        idempotencyKey: `web_point_reserve:${source}:${reservationId}`,
        requestSignature: JSON.stringify({ op, requestSignature }),
        deltaPos: -pointAmount,
        economyAfter: { pos: economy.pos },
        createdAt: nowISO(),
      };
      db.transactions.push(transaction);
      this.writeAll(db);
      return result(false, transaction);
    }

    if (!reservation) return { ok: false, error: "POINT_RESERVATION_NOT_FOUND" };
    const targetStatus = op === "capture" ? "CAPTURED" : "RELEASED";
    if (reservation.status === targetStatus) return result(true, transactionFor(op));
    if (reservation.status !== "RESERVED") {
      return { ok: false, error: "POINT_RESERVATION_STATE_CONFLICT" };
    }

    let deltaPos = 0;
    if (op === "release") {
      if (economy.pos > Number.MAX_SAFE_INTEGER - pointAmount) {
        return { ok: false, error: "POINT_BALANCE_OVERFLOW" };
      }
      economy.pos += pointAmount;
      economy.updatedAt = nowISO();
      deltaPos = pointAmount;
      reservation.releasedAt = nowISO();
    } else {
      reservation.capturedAt = nowISO();
    }
    reservation.status = targetStatus;
    reservation.updatedAt = nowISO();
    const transaction = {
      id: generateId("wpr"),
      playerId: mappedPlayerId,
      type: `web_point_${op}`,
      ref: reservationId,
      idempotencyKey: `web_point_${op}:${source}:${reservationId}`,
      requestSignature: JSON.stringify({ op, requestSignature }),
      deltaPos,
      economyAfter: { pos: economy.pos },
      createdAt: nowISO(),
    };
    db.transactions.push(transaction);
    this.writeAll(db);
    return result(false, transaction);
  }

  createBrowserAuthRequest(record) {
    const db = this.readAll();
    if (db.browserAuthRequests[record.requestIdHash]) {
      return { ok: false, error: "BROWSER_AUTH_REQUEST_EXISTS" };
    }

    const now = Date.now();
    for (const [key, current] of Object.entries(db.browserAuthRequests)) {
      const expiresAt = Date.parse(current.expiresAt || "");
      if (Number.isFinite(expiresAt) && expiresAt + 60 * 60 * 1000 < now) {
        delete db.browserAuthRequests[key];
      }
    }

    db.browserAuthRequests[record.requestIdHash] = {
      requestIdHash: record.requestIdHash,
      pkceChallenge: record.pkceChallenge,
      intent: record.intent || "login",
      status: "pending",
      webUserId: "",
      webUser: {},
      expiresAt: record.expiresAt,
      approvedAt: null,
      consumedAt: null,
      createdAt: record.createdAt || nowISO(),
    };
    this.writeAll(db);
    return { ok: true };
  }

  approveBrowserAuthRequest(requestIdHash, webUser) {
    const db = this.readAll();
    const record = db.browserAuthRequests[requestIdHash];
    if (!record) return { ok: false, error: "BROWSER_AUTH_REQUEST_NOT_FOUND" };

    if (Date.parse(record.expiresAt) <= Date.now()) {
      record.status = "expired";
      this.writeAll(db);
      return { ok: false, error: "BROWSER_AUTH_EXPIRED" };
    }
    if (record.status === "consumed") return { ok: false, error: "BROWSER_AUTH_CONSUMED" };
    if (record.status === "approved") {
      return record.webUserId === webUser.id
        ? { ok: true, duplicate: true }
        : { ok: false, error: "BROWSER_AUTH_ALREADY_APPROVED" };
    }
    if (record.status !== "pending") return { ok: false, error: "BROWSER_AUTH_INVALID_STATE" };

    record.status = "approved";
    record.webUserId = webUser.id;
    record.webUser = JSON.parse(JSON.stringify(webUser));
    record.approvedAt = nowISO();
    this.writeAll(db);
    return { ok: true, duplicate: false };
  }

  exchangeBrowserAuthRequest(requestIdHash, presentedChallenge) {
    const db = this.readAll();
    const record = db.browserAuthRequests[requestIdHash];
    if (!record) return { ok: false, error: "BROWSER_AUTH_REQUEST_NOT_FOUND" };

    if (Date.parse(record.expiresAt) <= Date.now()) {
      record.status = "expired";
      this.writeAll(db);
      return { ok: false, error: "BROWSER_AUTH_EXPIRED" };
    }
    if (record.status === "pending") return { ok: false, error: "BROWSER_AUTH_PENDING" };
    if (record.status === "consumed") return { ok: false, error: "BROWSER_AUTH_CONSUMED" };
    if (record.status !== "approved") return { ok: false, error: "BROWSER_AUTH_INVALID_STATE" };
    if (record.pkceChallenge !== presentedChallenge) {
      return { ok: false, error: "BROWSER_AUTH_PKCE_MISMATCH" };
    }

    record.status = "consumed";
    record.consumedAt = nowISO();
    this.writeAll(db);
    return {
      ok: true,
      request: JSON.parse(JSON.stringify(record)),
    };
  }

  getPlayer(playerId) {
    return this.readAll().players[playerId] || null;
  }

  setActivePlayerSession(playerId, sessionId) {
    const db = this.readAll();
    if (!db.players[playerId]) {
      const legacyUser = db.users.find((user) => user.id === playerId);
      if (!legacyUser) throw new Error(`PLAYER_NOT_FOUND:${playerId}`);
      db.players[playerId] = {
        id: playerId,
        webUserId: "",
        username: legacyUser.username || "Player",
        displayName: legacyUser.username || "Player",
        authSource: "local",
        createdAt: legacyUser.created_at || nowISO(),
        updatedAt: nowISO(),
      };
    }
    db.playerSessions[playerId] = {
      sessionId: String(sessionId || ""),
      updatedAt: nowISO(),
    };
    this.writeAll(db);
    return { playerId, sessionId: db.playerSessions[playerId].sessionId };
  }

  isActivePlayerSession(playerId, sessionId) {
    if (!playerId || !sessionId) return false;
    const current = this.readAll().playerSessions[playerId];
    return Boolean(current && current.sessionId === sessionId);
  }

  clearActivePlayerSession(playerId, sessionId) {
    const db = this.readAll();
    const current = db.playerSessions[playerId];
    if (!current || (sessionId && current.sessionId !== sessionId)) return false;
    delete db.playerSessions[playerId];
    this.writeAll(db);
    return true;
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
      version: toInt(incoming.version, current.version || 1),
      pos: toNumber(incoming.pos, current.pos || 0),
      updatedAt: nowISO(),
    };
    this.writeAll(db);
    return db.economies[playerId];
  }

  applyEconomyDelta(playerId, delta, meta) {
    const db = this.readAll();
    this.ensurePlayerStateInDb(db, playerId);

    const idempotencyKey = meta && meta.idempotencyKey;
    const deltaPos = toNumber(delta && delta.pos, 0);
    const requestSignature = JSON.stringify({
      op: "economy",
      playerId,
      deltaPos,
      type: (meta && meta.type) || "adjust",
      ref: (meta && meta.ref) || "",
    });
    const existing = findTransactionByIdempotency(db, idempotencyKey);
    if (existing) {
      if (existing.playerId !== playerId || (existing.requestSignature && existing.requestSignature !== requestSignature)) {
        return { ok: false, error: "IDEMPOTENCY_CONFLICT" };
      }
      return {
        ok: true,
        economy: existing.economyAfter || db.economies[playerId],
        transaction: existing,
        duplicate: true,
      };
    }

    const economy = db.economies[playerId];

    if (economy.pos + deltaPos < 0) {
      return { ok: false, error: "INSUFFICIENT_BALANCE", economy };
    }

    economy.pos += deltaPos;
    economy.updatedAt = nowISO();

    const transaction = {
      id: generateId("tx"),
      playerId,
      type: (meta && meta.type) || "adjust",
      ref: (meta && meta.ref) || "",
      idempotencyKey: idempotencyKey || "",
      requestSignature,
      deltaPos,
      economyAfter: { pos: economy.pos },
      createdAt: nowISO(),
    };
    db.transactions.push(transaction);
    this.writeAll(db);
    return { ok: true, economy, transaction };
  }

  creditWebTopup(webUser, pointAmountMicros, meta = {}) {
    const amountMicros = Number(pointAmountMicros);
    const pointAmount = String(meta.pointAmount || "").trim();
    const transactionId = String(meta.transactionId || "").trim();
    const source = String(meta.source || "ywonder-web").trim();
    const expectedPlayerId = String(meta.expectedPlayerId || "").trim();
    if (!webUser || !webUser.id || !transactionId || !pointAmount
      || !Number.isSafeInteger(amountMicros) || amountMicros < 1) {
      return { ok: false, error: "INVALID_WEB_TOPUP" };
    }

    let player;
    let db;
    if (expectedPlayerId) {
      db = this.readAll();
      const mappedPlayerId = db.playersByWebUserId[webUser.id];
      if (mappedPlayerId !== expectedPlayerId || !db.players[mappedPlayerId]) {
        return { ok: false, error: "GAME_POINT_IDENTITY_MISMATCH" };
      }
      player = JSON.parse(JSON.stringify(db.players[mappedPlayerId]));
    } else {
      player = this.getOrCreatePlayerForWebUser(webUser);
      db = this.readAll();
    }
    this.ensurePlayerStateInDb(db, player.id);
    const idempotencyKey = `web_topup:${source}:${transactionId}`;
    const requestSignature = JSON.stringify({
      op: "web_topup_credit",
      source,
      transactionId,
      webUserId: webUser.id,
      expectedPlayerId,
      pointAmount,
      pointAmountMicros: amountMicros,
      occurredAt: String(meta.occurredAt || ""),
    });
    const existing = findTransactionByIdempotency(db, idempotencyKey);
    if (existing) {
      if (existing.playerId !== player.id || existing.requestSignature !== requestSignature) {
        return { ok: false, error: "IDEMPOTENCY_CONFLICT" };
      }
      return {
        ok: true,
        duplicate: true,
        player,
        economy: db.economies[player.id],
        transaction: existing,
      };
    }

    const economy = db.economies[player.id];
    const remainderBefore = db.webTopupPointMicrosRemainders[player.id];
    const totalMicros = remainderBefore + amountMicros;
    if (!Number.isSafeInteger(totalMicros)) {
      return { ok: false, error: "POINT_BALANCE_OVERFLOW" };
    }
    const deltaPos = Math.floor(totalMicros / 1_000_000);
    const remainderAfter = totalMicros % 1_000_000;
    if (!Number.isSafeInteger(economy.pos) || economy.pos > Number.MAX_SAFE_INTEGER - deltaPos) {
      return { ok: false, error: "POINT_BALANCE_OVERFLOW" };
    }
    economy.pos += deltaPos;
    economy.updatedAt = nowISO();
    db.webTopupPointMicrosRemainders[player.id] = remainderAfter;

    const transaction = {
      id: generateId("wtx"),
      playerId: player.id,
      type: "web_topup_credit",
      ref: transactionId,
      idempotencyKey,
      requestSignature,
      source,
      webUserId: webUser.id,
      expectedPlayerId: expectedPlayerId || undefined,
      occurredAt: String(meta.occurredAt || ""),
      pointAmount,
      pointAmountMicros: amountMicros,
      pointMicrosRemainderBefore: remainderBefore,
      pointMicrosRemainderAfter: remainderAfter,
      deltaPos,
      economyAfter: JSON.parse(JSON.stringify(economy)),
      createdAt: nowISO(),
    };
    db.transactions.push(transaction);
    this.writeAll(db);
    return { ok: true, duplicate: false, player, economy, transaction };
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

  transactShop(playerId, offer, quantity, options = {}) {
    const db = this.readAll();
    this.ensurePlayerStateInDb(db, playerId);

    const idempotencyKey = String(options.idempotencyKey || "").trim();
    if (!idempotencyKey) {
      return { ok: false, error: "MISSING_IDEMPOTENCY_KEY" };
    }

    const normalizedQuantity = toInt(quantity, 0);
    const shopId = String(offer && offer.shopId || "").trim();
    const mode = String(offer && offer.mode || "").trim();
    const itemId = String(offer && offer.itemId || "").trim();
    const unitPrice = Number(offer && offer.unitPrice);
    if (!shopId || !itemId || !["buy", "sell"].includes(mode)) {
      return { ok: false, error: "INVALID_SHOP_REQUEST" };
    }
    if (normalizedQuantity < 1 || normalizedQuantity > 999) {
      return { ok: false, error: "INVALID_QUANTITY" };
    }
    if (!Number.isSafeInteger(unitPrice) || unitPrice < 0) {
      return { ok: false, error: "INVALID_ITEM_PRICE" };
    }

    const requestSignature = `${shopId}|${mode}|${itemId}|${normalizedQuantity}|${unitPrice}`;
    const existing = findTransactionByIdempotency(db, idempotencyKey);
    if (existing) {
      if (existing.playerId !== playerId || existing.requestSignature !== requestSignature) {
        return { ok: false, error: "IDEMPOTENCY_CONFLICT" };
      }
      return {
        ok: true,
        economy: existing.economyAfter || db.economies[playerId],
        inventory: existing.inventoryAfter || db.inventories[playerId],
        transaction: existing,
        duplicate: true,
      };
    }

    const totalPrice = unitPrice * normalizedQuantity;
    if (!Number.isSafeInteger(totalPrice)) {
      return { ok: false, error: "INVALID_ITEM_PRICE" };
    }

    const economy = db.economies[playerId];
    const inventory = db.inventories[playerId];
    let slot = findSlot(inventory, itemId);
    let deltaPos;
    let quantityDelta;

    if (mode === "buy") {
      if (economy.pos < totalPrice) {
        return { ok: false, error: "INSUFFICIENT_BALANCE", economy, inventory };
      }

      if (!slot) {
        if (inventory.slots.length >= inventory.maxSlots) inventory.maxSlots += 1;
        slot = { itemId, quantity: 0 };
        inventory.slots.push(slot);
      }
      deltaPos = -totalPrice;
      quantityDelta = normalizedQuantity;
    } else {
      if (!slot || slot.quantity < normalizedQuantity) {
        return { ok: false, error: "INSUFFICIENT_ITEM", economy, inventory };
      }
      deltaPos = totalPrice;
      quantityDelta = -normalizedQuantity;
    }

    economy.pos += deltaPos;
    economy.updatedAt = nowISO();
    slot.quantity += quantityDelta;
    inventory.slots = inventory.slots.filter((entry) => entry.quantity > 0);
    inventory.updatedAt = nowISO();

    const transaction = {
      id: generateId("stx"),
      playerId,
      type: mode === "buy" ? "shop_buy" : "shop_sell",
      ref: `${shopId}:${itemId}`,
      idempotencyKey,
      requestSignature,
      shopId,
      mode,
      itemId,
      quantity: normalizedQuantity,
      unitPrice,
      totalPrice,
      deltaPos,
      quantityDelta,
      economyAfter: JSON.parse(JSON.stringify(economy)),
      inventoryAfter: JSON.parse(JSON.stringify(inventory)),
      createdAt: nowISO(),
    };
    db.transactions.push(transaction);
    this.writeAll(db);

    return { ok: true, economy, inventory, transaction, duplicate: false };
  }

  applyResourceHarvest(playerId, rewards, options = {}) {
    const db = this.readAll();
    this.ensurePlayerStateInDb(db, playerId);

    const idempotencyKey = String(options.idempotencyKey || "").trim();
    const existing = findTransactionByIdempotency(db, idempotencyKey);
    if (existing) {
      return {
        ok: true,
        inventory: existing.inventoryAfter || db.inventories[playerId],
        daily_limits: existing.dailyLimitsAfter || db.dailyLimits[playerId],
        limit: existing.limitAfter || null,
        rewards: existing.rewards || [],
        transaction: existing,
        duplicate: true,
      };
    }

    const normalizedRewards = (Array.isArray(rewards) ? rewards : [])
      .map((reward) => ({
        itemId: String(reward && reward.itemId || "").trim(),
        quantity: Math.max(0, toInt(reward && reward.quantity, 0)),
        displayName: String(reward && reward.displayName || "").trim(),
        kind: String(reward && reward.kind || "resource").trim(),
      }))
      .filter((reward) => reward.itemId && reward.quantity > 0);

    if (normalizedRewards.length === 0) {
      return {
        ok: false,
        error: "MISSING_REWARDS",
        inventory: db.inventories[playerId],
        daily_limits: db.dailyLimits[playerId],
      };
    }

    const dailyLimits = db.dailyLimits[playerId];
    let limit = null;
    const limitOptions = options.dailyLimit;
    if (limitOptions) {
      const key = String(limitOptions.key || "").trim();
      if (!key) {
        return {
          ok: false,
          error: "MISSING_LIMIT_KEY",
          inventory: db.inventories[playerId],
          daily_limits: dailyLimits,
        };
      }

      const amount = Math.max(1, toInt(limitOptions.amount, 1));
      const maxCount = Math.max(1, toInt(limitOptions.maxCount, 10));
      const periodKey = String(limitOptions.periodKey || todayKey());
      limit = this.ensureDefaultDailyLimit(dailyLimits, key, maxCount, periodKey);
      if (limit.used + amount > limit.maxCount) {
        return {
          ok: false,
          error: "DAILY_LIMIT_EXCEEDED",
          inventory: db.inventories[playerId],
          daily_limits: dailyLimits,
          limit,
        };
      }

      limit.used += amount;
      limit.remaining = Math.max(0, limit.maxCount - limit.used);
      limit.updatedAt = nowISO();
      dailyLimits.updatedAt = nowISO();
    }

    const inventory = db.inventories[playerId];
    for (const reward of normalizedRewards) {
      let slot = findSlot(inventory, reward.itemId);
      if (!slot) {
        if (inventory.slots.length >= inventory.maxSlots) inventory.maxSlots += 1;
        slot = { itemId: reward.itemId, quantity: 0 };
        inventory.slots.push(slot);
      }
      slot.quantity += reward.quantity;
    }
    inventory.updatedAt = nowISO();

    const transaction = {
      id: generateId("rtx"),
      playerId,
      type: String(options.type || "resource_harvest"),
      ref: String(options.ref || ""),
      idempotencyKey,
      rewards: normalizedRewards,
      inventoryAfter: JSON.parse(JSON.stringify(inventory)),
      dailyLimitsAfter: JSON.parse(JSON.stringify(dailyLimits)),
      limitAfter: limit ? { ...limit } : null,
      createdAt: nowISO(),
    };
    db.transactions.push(transaction);
    this.writeAll(db);

    return {
      ok: true,
      inventory,
      daily_limits: dailyLimits,
      limit,
      rewards: normalizedRewards,
      transaction,
    };
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

  compareAndSetFarmState(playerId, expectedVersion, farmState) {
    const db = this.readAll();
    this.ensurePlayerStateInDb(db, playerId);
    const current = db.farmStates[playerId];
    const currentVersion = Math.max(1, toInt(current.version, 1));
    if (currentVersion !== expectedVersion) {
      return {
        ok: false,
        error: "FARM_STATE_CONFLICT",
        farm_state: JSON.parse(JSON.stringify(current)),
      };
    }

    const incoming = { ...(farmState || {}) };
    delete incoming.version;
    delete incoming.updatedAt;
    db.farmStates[playerId] = {
      ...current,
      ...incoming,
      version: currentVersion + 1,
      updatedAt: nowISO(),
    };
    this.writeAll(db);
    return {
      ok: true,
      farm_state: JSON.parse(JSON.stringify(db.farmStates[playerId])),
    };
  }

  placeFarmAnimal(playerId, placement, options = {}) {
    const db = this.readAll();
    this.ensurePlayerStateInDb(db, playerId);

    const itemId = String(placement && placement.itemId || "").trim();
    const cellKeys = Array.isArray(placement && placement.cellKeys)
      ? placement.cellKeys.map((key) => String(key || "").trim())
      : [];
    const idempotencyKey = String(options.idempotencyKey || "").trim();
    const expectedVersion = toInt(options.expectedVersion, 0);
    const rule = placement && placement.rule;
    const requestSignature = JSON.stringify({
      op: "farm_animal_place",
      playerId,
      itemId,
      cellKeys,
      expectedVersion,
    });

    const existing = findTransactionByIdempotency(db, idempotencyKey);
    if (existing) {
      if (existing.playerId !== playerId || existing.requestSignature !== requestSignature) {
        return { ok: false, error: "IDEMPOTENCY_CONFLICT" };
      }
      return {
        ...(JSON.parse(JSON.stringify(existing.result || {}))),
        ok: true,
        transaction: existing,
        duplicate: true,
      };
    }

    const inventory = db.inventories[playerId];
    const currentFarm = db.farmStates[playerId];
    const currentVersion = Math.max(1, toInt(currentFarm.version, 1));
    if (currentVersion !== expectedVersion) {
      return {
        ok: false,
        error: "FARM_STATE_CONFLICT",
        inventory: JSON.parse(JSON.stringify(inventory)),
        farm_state: JSON.parse(JSON.stringify(currentFarm)),
      };
    }

    const slot = findSlot(inventory, itemId);
    if (!slot || slot.quantity < 1) {
      return {
        ok: false,
        error: "INSUFFICIENT_ITEM",
        inventory: JSON.parse(JSON.stringify(inventory)),
        farm_state: JSON.parse(JSON.stringify(currentFarm)),
      };
    }

    const prepared = prepareAnimalPlacement(currentFarm, { itemId, cellKeys }, rule);
    if (!prepared.ok) {
      return {
        ok: false,
        error: prepared.error,
        inventory: JSON.parse(JSON.stringify(inventory)),
        farm_state: JSON.parse(JSON.stringify(currentFarm)),
      };
    }

    slot.quantity -= 1;
    inventory.slots = inventory.slots.filter((entry) => entry.quantity > 0);
    inventory.version = Math.max(1, toInt(inventory.version, 1)) + 1;
    inventory.updatedAt = nowISO();

    db.farmStates[playerId] = {
      ...prepared.farmState,
      version: currentVersion + 1,
      updatedAt: nowISO(),
    };

    const inventoryAfter = JSON.parse(JSON.stringify(inventory));
    const farmStateAfter = JSON.parse(JSON.stringify(db.farmStates[playerId]));
    const response = {
      ok: true,
      duplicate: false,
      inventory: inventoryAfter,
      farm_state: farmStateAfter,
      animal: prepared.animal,
    };
    const transaction = {
      id: generateId("fatx"),
      playerId,
      type: "farm_animal_place",
      ref: prepared.animal.instanceId,
      idempotencyKey,
      requestSignature,
      itemId,
      quantityDelta: -1,
      cellKeys,
      inventoryAfter,
      farmStateAfter,
      result: JSON.parse(JSON.stringify(response)),
      createdAt: nowISO(),
    };
    db.transactions.push(transaction);
    this.writeAll(db);
    return { ...response, transaction };
  }

  getDailyLimits(playerId) {
    const db = this.readAll();
    this.ensurePlayerStateInDb(db, playerId);
    this.writeAll(db);
    return db.dailyLimits[playerId];
  }

  setDailyLimits(playerId, dailyLimits) {
    const db = this.readAll();
    db.dailyLimits[playerId] = {
      ...(db.dailyLimits[playerId] || makeDefaultDailyLimits()),
      ...(dailyLimits || {}),
      updatedAt: nowISO(),
    };
    if (!db.dailyLimits[playerId].limits || typeof db.dailyLimits[playerId].limits !== "object") {
      db.dailyLimits[playerId].limits = {};
    }
    this.ensureDefaultDailyLimit(db.dailyLimits[playerId], "fishing", 10);
    this.ensureDefaultDailyLimit(db.dailyLimits[playerId], "mining", 10);
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

  deletePlayer(playerId) {
    const db = this.readAll();
    const before = db.users.length + Object.keys(db.players).length;
    db.users = db.users.filter((user) => user.id !== playerId);
    delete db.profiles[playerId];
    delete db.economies[playerId];
    delete db.webTopupPointMicrosRemainders[playerId];
    for (const [lotId, lot] of Object.entries(db.pointSourceLots)) {
      if (lot && lot.playerId === playerId) delete db.pointSourceLots[lotId];
    }
    delete db.inventories[playerId];
    delete db.farmStates[playerId];
    delete db.dailyLimits[playerId];
    delete db.playerSessions[playerId];
    delete db.players[playerId];
    for (const [webUserId, mappedPlayerId] of Object.entries(db.playersByWebUserId)) {
      if (mappedPlayerId === playerId) delete db.playersByWebUserId[webUserId];
    }
    db.transactions = db.transactions.filter((tx) => tx.playerId !== playerId);
    this.writeAll(db);
    return before !== db.users.length + Object.keys(db.players).length;
  }
}

function createActiveStore() {
  if (STORE_MODE === "json") {
    return new JsonStore(DB_PATH);
  }

  if (STORE_MODE === "postgres") {
    const { createPostgresStore } = require("./postgresStore");
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
  getWebPointBalance: activeStore.getWebPointBalance.bind(activeStore),
  applyWebPointReservation: activeStore.applyWebPointReservation.bind(activeStore),
  recordPointSourceLot: activeStore.recordPointSourceLot.bind(activeStore),
  getPointSourceLots: activeStore.getPointSourceLots.bind(activeStore),
  previewPointSourceFifo: activeStore.previewPointSourceFifo.bind(activeStore),
  createBrowserAuthRequest: activeStore.createBrowserAuthRequest.bind(activeStore),
  approveBrowserAuthRequest: activeStore.approveBrowserAuthRequest.bind(activeStore),
  exchangeBrowserAuthRequest: activeStore.exchangeBrowserAuthRequest.bind(activeStore),
  getPlayer: activeStore.getPlayer.bind(activeStore),
  setActivePlayerSession: activeStore.setActivePlayerSession.bind(activeStore),
  isActivePlayerSession: activeStore.isActivePlayerSession.bind(activeStore),
  clearActivePlayerSession: activeStore.clearActivePlayerSession.bind(activeStore),
  getProfile: activeStore.getProfile.bind(activeStore),
  setProfile: activeStore.setProfile.bind(activeStore),
  ensurePlayerState: activeStore.ensurePlayerState.bind(activeStore),
  getEconomy: activeStore.getEconomy.bind(activeStore),
  setEconomy: activeStore.setEconomy.bind(activeStore),
  applyEconomyDelta: activeStore.applyEconomyDelta.bind(activeStore),
  creditWebTopup: activeStore.creditWebTopup.bind(activeStore),
  getInventory: activeStore.getInventory.bind(activeStore),
  setInventory: activeStore.setInventory.bind(activeStore),
  adjustInventoryItem: activeStore.adjustInventoryItem.bind(activeStore),
  transactShop: activeStore.transactShop.bind(activeStore),
  applyResourceHarvest: activeStore.applyResourceHarvest.bind(activeStore),
  getFarmState: activeStore.getFarmState.bind(activeStore),
  setFarmState: activeStore.setFarmState.bind(activeStore),
  compareAndSetFarmState: activeStore.compareAndSetFarmState.bind(activeStore),
  placeFarmAnimal: activeStore.placeFarmAnimal.bind(activeStore),
  getDailyLimits: activeStore.getDailyLimits.bind(activeStore),
  consumeDailyLimit: activeStore.consumeDailyLimit.bind(activeStore),
  setDailyLimits: activeStore.setDailyLimits.bind(activeStore),
  deletePlayer: activeStore.deletePlayer.bind(activeStore),
  close: typeof activeStore.close === "function" ? activeStore.close.bind(activeStore) : async () => {},
  healthCheck: activeStore.healthCheck.bind(activeStore),
};
