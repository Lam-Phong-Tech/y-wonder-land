const crypto = require("crypto");
const { Pool } = require("pg");
const { prepareAnimalPlacement } = require("./farmAnimalPlacement");
const { FISHING_FREE_PER_DAY, FISHING_BAIT_ID, rollFish } = require("./fishingTable");
const {
  normalizePointSourceLot,
  pointSourceLotRequestSignature,
  planPointSourceFifo,
} = require("./pointSourceLedger");

function nowISO() {
  return new Date().toISOString();
}

function toInt(value, fallback = 0) {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? Math.trunc(parsed) : fallback;
}

function toNumber(value, fallback = 0) {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : fallback;
}

function toSafeInteger(value, fieldName) {
  const parsed = Number(value);
  if (!Number.isSafeInteger(parsed)) {
    throw new Error(`PostgreSQL ${fieldName} is outside JavaScript's safe integer range.`);
  }
  return parsed;
}

function clone(value) {
  return value == null ? value : JSON.parse(JSON.stringify(value));
}

function normalizeIdentity(value) {
  return String(value || "").trim().toLowerCase();
}

function makeId(prefix) {
  return `${prefix}_${Date.now()}_${crypto.randomUUID().replace(/-/g, "").slice(0, 12)}`;
}

function periodKey(date = new Date()) {
  const timeZone = process.env.GAME_TIMEZONE || "Asia/Ho_Chi_Minh";
  const parts = new Intl.DateTimeFormat("en-US", {
    timeZone,
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).formatToParts(date);
  const values = Object.fromEntries(parts.map((part) => [part.type, part.value]));
  return `${values.year}-${values.month}-${values.day}`;
}

function makeDefaultEconomy() {
  // Tài khoản mới KHÔNG được cấp tiền (khách yêu cầu). Trước đây mặc định 5000 -> đã sửa 0.
  return { version: 1, pos: 0, updatedAt: nowISO() };
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

function accountFromRow(row) {
  if (!row) return null;
  return {
    id: row.id,
    player_id: row.player_id,
    username: row.username,
    email: row.email || "",
    phone: row.phone || "",
    password_hash: row.password_hash,
    status: row.status,
    soft_deleted: Boolean(row.soft_deleted),
    created_at: new Date(row.created_at).toISOString(),
    updated_at: new Date(row.updated_at).toISOString(),
  };
}

function playerFromRow(row) {
  if (!row) return null;
  return {
    id: row.id,
    webUserId: row.web_user_id || "",
    username: row.username,
    displayName: row.display_name,
    authSource: row.auth_source,
    createdAt: new Date(row.created_at).toISOString(),
    updatedAt: new Date(row.updated_at).toISOString(),
  };
}

function profileFromRow(row) {
  if (!row) return null;
  return {
    ...(row.profile_json || {}),
    version: toInt(row.version, 1),
    name: row.name,
    gender: row.gender,
    avatarId: row.avatar_id || "",
    level: toInt(row.level, 1),
    exp: toNumber(row.exp, 0),
    characterCreated: Boolean(row.character_created),
    tutorialCompleted: Boolean(row.tutorial_completed),
    createdAt: new Date(row.created_at).toISOString(),
    updatedAt: new Date(row.updated_at).toISOString(),
  };
}

function economyFromRow(row) {
  if (!row) return null;
  return {
    version: toInt(row.version, 1),
    pos: toSafeInteger(row.pos, "economy.pos"),
    updatedAt: new Date(row.updated_at).toISOString(),
  };
}

function slotFromRow(row) {
  const slot = {
    itemId: row.item_id,
    quantity: toInt(row.quantity, 0),
  };
  if (row.slot_tab) slot.slotTab = row.slot_tab;
  if (row.equipped) slot.equipped = true;
  if (row.durability != null) slot.durability = toInt(row.durability, 0);
  return slot;
}

function transactionFromRow(row) {
  if (!row) return null;
  return {
    ...(row.details_json || {}),
    id: row.id,
    playerId: row.player_id,
    type: row.type,
    ref: row.ref || "",
    idempotencyKey: row.idempotency_key || "",
    requestSignature: row.request_signature || "",
    deltaPos: toSafeInteger(row.delta_pos, "transaction.delta_pos"),
    itemId: row.item_id || undefined,
    quantityDelta: row.quantity_delta == null ? undefined : toInt(row.quantity_delta, 0),
    createdAt: new Date(row.created_at).toISOString(),
  };
}

function pointReservationFromRow(row) {
  if (!row) return null;
  return {
    id: row.id,
    playerId: row.player_id,
    webUserId: row.web_user_id,
    expectedPlayerId: row.expected_player_id,
    pointAmount: toSafeInteger(row.point_amount, "point_reservation.point_amount"),
    purpose: row.purpose,
    source: row.source,
    occurredAt: new Date(row.occurred_at).toISOString(),
    status: row.status,
    capturedAt: row.captured_at ? new Date(row.captured_at).toISOString() : null,
    releasedAt: row.released_at ? new Date(row.released_at).toISOString() : null,
    createdAt: new Date(row.created_at).toISOString(),
    updatedAt: new Date(row.updated_at).toISOString(),
  };
}

function pointSourceLotFromRow(row) {
  if (!row) return null;
  return {
    id: row.id,
    playerId: row.player_id,
    sourceEventId: row.source_event_id,
    sourceEventIndex: toSafeInteger(row.source_event_index, "point_source_lot.source_event_index"),
    originType: row.origin_type,
    acquisitionType: row.acquisition_type,
    commissionAsset: row.commission_asset || "",
    pointAmountMicros: toSafeInteger(row.point_amount_micros, "point_source_lot.point_amount_micros"),
    remainingPointMicros: toSafeInteger(
      row.remaining_point_micros,
      "point_source_lot.remaining_point_micros"
    ),
    sourceRatePair: row.source_rate_pair || "",
    sourceRateVersionId: row.source_rate_version_id || "",
    pointMicrosPerSourceUnit: row.point_micros_per_source_unit == null
      ? null
      : toSafeInteger(
        row.point_micros_per_source_unit,
        "point_source_lot.point_micros_per_source_unit"
      ),
    commissionRateVersionId: row.commission_rate_version_id || "",
    pointMicrosPerUsdt: row.point_micros_per_usdt == null
      ? null
      : toSafeInteger(row.point_micros_per_usdt, "point_source_lot.point_micros_per_usdt"),
    parentLotId: row.parent_lot_id || "",
    rootLotId: row.root_lot_id,
    occurredAt: new Date(row.occurred_at).toISOString(),
    metadata: clone(row.metadata_json) || {},
    requestSignature: row.request_signature,
    createdAt: new Date(row.created_at).toISOString(),
    updatedAt: new Date(row.updated_at).toISOString(),
  };
}

function browserAuthRequestFromRow(row) {
  if (!row) return null;
  return {
    requestIdHash: row.request_id_hash,
    pkceChallenge: row.pkce_challenge,
    intent: row.intent,
    status: row.status,
    webUserId: row.web_user_id || "",
    webUser: clone(row.web_user_json) || {},
    expiresAt: new Date(row.expires_at).toISOString(),
    approvedAt: row.approved_at ? new Date(row.approved_at).toISOString() : null,
    consumedAt: row.consumed_at ? new Date(row.consumed_at).toISOString() : null,
    createdAt: new Date(row.created_at).toISOString(),
  };
}

class PostgresStore {
  constructor(options = {}) {
    this.mode = "postgres";
    this.databaseUrl = options.databaseUrl || "";
    if (!options.pool && !this.databaseUrl) {
      throw new Error("STORE_MODE=postgres requires DATABASE_URL or POSTGRES_URL.");
    }

    const sslMode = String(process.env.PGSSL || "").toLowerCase();
    this.pool = options.pool || new Pool({
      connectionString: this.databaseUrl,
      max: Math.max(1, toInt(process.env.PG_POOL_MAX, 10)),
      idleTimeoutMillis: Math.max(1000, toInt(process.env.PG_IDLE_TIMEOUT_MS, 30000)),
      ssl: sslMode === "require" ? { rejectUnauthorized: false } : undefined,
    });
  }

  generateId(prefix) {
    return makeId(prefix);
  }

  async close() {
    await this.pool.end();
  }

  async healthCheck() {
    await this.pool.query("select 1");
    return { ok: true, mode: this.mode };
  }

  async withTransaction(callback) {
    const client = await this.pool.connect();
    try {
      await client.query("begin");
      const result = await callback(client);
      await client.query("commit");
      return result;
    } catch (error) {
      try {
        await client.query("rollback");
      } catch (rollbackError) {
        console.error("[postgres-store] rollback failed:", rollbackError.message);
      }
      throw error;
    } finally {
      client.release();
    }
  }

  async lockIdempotency(client, idempotencyKey) {
    if (!idempotencyKey) return;
    await client.query("select pg_advisory_xact_lock(hashtext($1)::bigint)", [idempotencyKey]);
  }

  async findStoredTransaction(client, idempotencyKey) {
    if (!idempotencyKey) return null;
    const result = await client.query(
      "select * from game_transactions where idempotency_key = $1 limit 1",
      [idempotencyKey]
    );
    return result.rows[0] || null;
  }

  duplicateResult(row, requestSignature = "") {
    if (requestSignature && row.request_signature && row.request_signature !== requestSignature) {
      return { ok: false, error: "IDEMPOTENCY_CONFLICT" };
    }
    return {
      ...(clone(row.result_json) || {}),
      ok: true,
      transaction: transactionFromRow(row),
      duplicate: true,
    };
  }

  async insertTransaction(client, transaction, result) {
    const details = { ...transaction };
    delete details.id;
    delete details.playerId;
    delete details.type;
    delete details.ref;
    delete details.idempotencyKey;
    delete details.requestSignature;
    delete details.deltaPos;
    delete details.itemId;
    delete details.quantityDelta;
    delete details.createdAt;

    await client.query(
      `insert into game_transactions
       (id, player_id, type, ref, idempotency_key, request_signature,
        delta_pos, item_id, quantity_delta, details_json, result_json, created_at)
       values ($1,$2,$3,$4,$5,$6,$7,$8,$9,$10::jsonb,$11::jsonb,$12)`,
      [
        transaction.id,
        transaction.playerId,
        transaction.type,
        transaction.ref || "",
        transaction.idempotencyKey || null,
        transaction.requestSignature || "",
        transaction.deltaPos || 0,
        transaction.itemId || null,
        transaction.quantityDelta == null ? null : transaction.quantityDelta,
        JSON.stringify(details),
        JSON.stringify(result || {}),
        transaction.createdAt || nowISO(),
      ]
    );
  }

  async ensurePlayerStateWithClient(client, playerId) {
    const player = await client.query("select id from game_players where id = $1", [playerId]);
    if (player.rowCount === 0) throw new Error(`PLAYER_NOT_FOUND:${playerId}`);

    await client.query(
      // Khách chốt: tài khoản mới bắt đầu với 0 Point (không cấp tiền). Trước là 5000.
      `insert into player_economy (player_id, version, pos)
       values ($1,1,0) on conflict (player_id) do nothing`,
      [playerId]
    );

    const meta = await client.query(
      `insert into player_inventory_meta (player_id, version, max_slots)
       values ($1,1,50) on conflict (player_id) do nothing returning player_id`,
      [playerId]
    );
    if (meta.rowCount > 0) {
      const defaults = makeDefaultInventory().slots;
      for (const slot of defaults) {
        await client.query(
          `insert into player_inventory (player_id, item_id, quantity)
           values ($1,$2,$3) on conflict (player_id,item_id) do nothing`,
          [playerId, slot.itemId, slot.quantity]
        );
      }
    }

    await client.query(
      `insert into player_farm_state (player_id, version, state_json)
       values ($1,1,$2::jsonb) on conflict (player_id) do nothing`,
      [playerId, JSON.stringify(makeDefaultFarmState())]
    );

    const currentPeriod = periodKey();
    for (const key of ["fishing", "mining"]) {
      await client.query(
        `insert into player_daily_limits
         (player_id, limit_key, period_key, used_count, max_count, version)
         values ($1,$2,$3,0,10,1)
         on conflict (player_id,limit_key,period_key) do nothing`,
        [playerId, key, currentPeriod]
      );
    }
  }

  async readAll() {
    const [accounts, players, profiles, economies, metas, slots, farms, limits, transactions] = await Promise.all([
      this.pool.query("select * from game_accounts order by created_at"),
      this.pool.query("select * from game_players order by created_at"),
      this.pool.query("select * from player_profiles"),
      this.pool.query("select * from player_economy"),
      this.pool.query("select * from player_inventory_meta"),
      this.pool.query("select * from player_inventory order by player_id,item_id"),
      this.pool.query("select * from player_farm_state"),
      this.pool.query("select * from player_daily_limits order by player_id,limit_key,period_key"),
      this.pool.query("select * from game_transactions order by created_at"),
    ]);

    const db = {
      users: accounts.rows.map(accountFromRow),
      profiles: {},
      players: {},
      playersByWebUserId: {},
      economies: {},
      inventories: {},
      farmStates: {},
      dailyLimits: {},
      transactions: transactions.rows.map(transactionFromRow),
      sessions: [],
    };

    for (const row of players.rows) {
      const player = playerFromRow(row);
      db.players[player.id] = player;
      if (player.webUserId) db.playersByWebUserId[player.webUserId] = player.id;
    }
    for (const row of profiles.rows) db.profiles[row.player_id] = profileFromRow(row);
    for (const row of economies.rows) db.economies[row.player_id] = economyFromRow(row);
    for (const row of metas.rows) {
      db.inventories[row.player_id] = {
        version: toInt(row.version, 1),
        maxSlots: toInt(row.max_slots, 50),
        slots: [],
        updatedAt: new Date(row.updated_at).toISOString(),
      };
    }
    for (const row of slots.rows) {
      if (!db.inventories[row.player_id]) {
        db.inventories[row.player_id] = { version: 1, maxSlots: 50, slots: [], updatedAt: nowISO() };
      }
      db.inventories[row.player_id].slots.push(slotFromRow(row));
    }
    for (const row of farms.rows) {
      db.farmStates[row.player_id] = {
        ...(row.state_json || {}),
        version: toInt(row.version, 1),
        updatedAt: new Date(row.updated_at).toISOString(),
      };
    }
    for (const row of limits.rows) {
      if (!db.dailyLimits[row.player_id]) {
        db.dailyLimits[row.player_id] = { version: 1, limits: {}, updatedAt: new Date(row.updated_at).toISOString() };
      }
      const limit = {
        limitKey: row.limit_key,
        periodKey: row.period_key,
        used: toInt(row.used_count, 0),
        maxCount: toInt(row.max_count, 10),
        remaining: Math.max(0, toInt(row.max_count, 10) - toInt(row.used_count, 0)),
        updatedAt: new Date(row.updated_at).toISOString(),
      };
      const current = db.dailyLimits[row.player_id].limits[limit.limitKey];
      if (!current || current.periodKey <= limit.periodKey) {
        db.dailyLimits[row.player_id].limits[limit.limitKey] = limit;
      }
    }
    return db;
  }

  async writeAll() {
    throw new Error("PostgresStore.writeAll is disabled; use the versioned import script instead.");
  }

  async findUserByName(username) {
    const key = normalizeIdentity(username);
    if (!key) return null;
    const result = await this.pool.query(
      `select * from game_accounts
       where lower(username) = $1 and soft_deleted = false and status = 'active' limit 1`,
      [key]
    );
    return accountFromRow(result.rows[0]);
  }

  async findUserByEmail(email) {
    const key = normalizeIdentity(email);
    if (!key) return null;
    const result = await this.pool.query(
      `select * from game_accounts
       where lower(email) = $1 and soft_deleted = false and status = 'active' limit 1`,
      [key]
    );
    return accountFromRow(result.rows[0]);
  }

  async findUserById(id) {
    const result = await this.pool.query(
      "select * from game_accounts where id = $1 and soft_deleted = false and status = 'active' limit 1",
      [id]
    );
    return accountFromRow(result.rows[0]);
  }

  async createUser(user) {
    return this.withTransaction(async (client) => {
      const createdAt = user.created_at || nowISO();
      const updatedAt = user.updated_at || createdAt;
      await client.query(
        `insert into game_players
         (id, web_user_id, username, display_name, auth_source, created_at, updated_at)
         values ($1,null,$2,$2,'local',$3,$4)`,
        [user.id, user.username, createdAt, updatedAt]
      );
      const result = await client.query(
        `insert into game_accounts
         (id, player_id, username, email, phone, password_hash, status, soft_deleted, created_at, updated_at)
         values ($1,$1,$2,$3,$4,$5,$6,$7,$8,$9) returning *`,
        [
          user.id,
          user.username,
          user.email || "",
          user.phone || "",
          user.password_hash,
          user.status || "active",
          Boolean(user.soft_deleted),
          createdAt,
          updatedAt,
        ]
      );
      await this.ensurePlayerStateWithClient(client, user.id);
      return accountFromRow(result.rows[0]);
    });
  }

  /// Đặt lại mật khẩu cho MỘT account. Chỉ dùng cho seed tài khoản demo — luồng
  /// người chơi thật không gọi tới, và hàm này không nhận mật khẩu thô, chỉ nhận hash.
  async setAccountPassword(accountId, passwordHash) {
    if (!accountId || !passwordHash) return null;
    const result = await this.pool.query(
      `update game_accounts set password_hash = $2, updated_at = now()
       where id = $1 and soft_deleted = false returning *`,
      [accountId, passwordHash]
    );
    return accountFromRow(result.rows[0]);
  }

  async getOrCreatePlayerForWebUserWithClient(client, webUser) {
    await client.query("select pg_advisory_xact_lock(hashtext($1)::bigint)", [`web:${webUser.id}`]);
    const existing = await client.query(
      "select * from game_players where web_user_id = $1 for update",
      [webUser.id]
    );
    let row;
    if (existing.rowCount === 0) {
      const playerId = makeId("p");
      const username = webUser.username || webUser.email || webUser.phone || webUser.id;
      const displayName = webUser.displayName || webUser.username || "Player";
      const inserted = await client.query(
        `insert into game_players
         (id, web_user_id, username, display_name, auth_source)
         values ($1,$2,$3,$4,$5) returning *`,
        [playerId, webUser.id, username, displayName, webUser.authSource || "web"]
      );
      row = inserted.rows[0];
    } else {
      const current = existing.rows[0];
      const updated = await client.query(
        `update game_players set username=$2, display_name=$3, auth_source=$4, updated_at=now()
         where id=$1 returning *`,
        [
          current.id,
          webUser.username || webUser.email || webUser.phone || current.username,
          webUser.displayName || webUser.username || current.display_name || "Player",
          webUser.authSource || current.auth_source || "web",
        ]
      );
      row = updated.rows[0];
    }
    await this.ensurePlayerStateWithClient(client, row.id);
    return playerFromRow(row);
  }

  async getOrCreatePlayerForWebUser(webUser) {
    if (!webUser || !webUser.id) throw new Error("webUser.id is required");
    return this.withTransaction((client) => this.getOrCreatePlayerForWebUserWithClient(client, webUser));
  }

  async getWebPointBalance(webUserId) {
    return this.withTransaction(async (client) => {
      const playerResult = await client.query(
        "select * from game_players where web_user_id = $1",
        [String(webUserId || "").trim()]
      );
      if (playerResult.rowCount === 0) return null;
      const player = playerFromRow(playerResult.rows[0]);
      const economy = await this.getEconomyWithClient(client, player.id);
      if (!economy) return null;
      return { player, economy };
    });
  }

  async recordPointSourceLotWithClient(client, playerId, input) {
    await this.ensurePlayerStateWithClient(client, playerId);

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

    const eventLock = `point_source_lot:${playerId}:${lot.sourceEventId}:${lot.sourceEventIndex}`;
    await this.lockIdempotency(client, eventLock);
    const existingResult = await client.query(
      `select * from point_source_lots
       where player_id=$1 and source_event_id=$2 and source_event_index=$3`,
      [playerId, lot.sourceEventId, lot.sourceEventIndex]
    );
    if (existingResult.rowCount > 0) {
      const existing = pointSourceLotFromRow(existingResult.rows[0]);
      if (existing.requestSignature !== requestSignature) {
        return { ok: false, error: "POINT_SOURCE_IDEMPOTENCY_CONFLICT" };
      }
      return { ok: true, duplicate: true, lot: existing };
    }

    const idResult = await client.query("select * from point_source_lots where id=$1", [lot.id]);
    if (idResult.rowCount > 0) {
      return { ok: false, error: "POINT_SOURCE_IDEMPOTENCY_CONFLICT" };
    }

    const inserted = await client.query(
      `insert into point_source_lots
       (id,player_id,source_event_id,source_event_index,origin_type,acquisition_type,
        commission_asset,point_amount_micros,remaining_point_micros,source_rate_pair,
        source_rate_version_id,point_micros_per_source_unit,commission_rate_version_id,
        point_micros_per_usdt,parent_lot_id,root_lot_id,occurred_at,request_signature,
        metadata_json,created_at,updated_at)
       values ($1,$2,$3,$4,$5,$6,$7,$8,$9,$10,$11,$12,$13,$14,$15,$16,$17,$18,$19::jsonb,now(),now())
       returning *`,
      [
        lot.id,
        lot.playerId,
        lot.sourceEventId,
        lot.sourceEventIndex,
        lot.originType,
        lot.acquisitionType,
        lot.commissionAsset || null,
        lot.pointAmountMicros,
        lot.remainingPointMicros,
        lot.sourceRatePair || null,
        lot.sourceRateVersionId || null,
        lot.pointMicrosPerSourceUnit,
        lot.commissionRateVersionId || null,
        lot.pointMicrosPerUsdt,
        lot.parentLotId || null,
        lot.rootLotId,
        lot.occurredAt,
        requestSignature,
        JSON.stringify(lot.metadata),
      ]
    );
    return { ok: true, duplicate: false, lot: pointSourceLotFromRow(inserted.rows[0]) };
  }

  async recordPointSourceLot(playerId, input) {
    return this.withTransaction((client) => this.recordPointSourceLotWithClient(client, playerId, input));
  }

  async getPointSourceLotsWithClient(client, playerId, options = {}) {
    const includeDepleted = options.includeDepleted !== false;
    const result = await client.query(
      `select * from point_source_lots
       where player_id=$1${includeDepleted ? "" : " and remaining_point_micros > 0"}
       order by occurred_at,created_at,id`,
      [playerId]
    );
    return result.rows.map(pointSourceLotFromRow);
  }

  async getPointSourceLots(playerId, options = {}) {
    return this.withTransaction((client) => this.getPointSourceLotsWithClient(client, playerId, options));
  }

  async previewPointSourceFifo(playerId, pointAmountMicros) {
    return this.withTransaction(async (client) => planPointSourceFifo(
      await this.getPointSourceLotsWithClient(client, playerId, { includeDepleted: false }),
      pointAmountMicros
    ));
  }

  async createBrowserAuthRequest(record) {
    await this.pool.query(
      "delete from browser_auth_requests where expires_at < now() - interval '1 hour'"
    );
    try {
      await this.pool.query(
        `insert into browser_auth_requests
         (request_id_hash, pkce_challenge, intent, status, expires_at, created_at)
         values ($1,$2,$3,'pending',$4,$5)`,
        [
          record.requestIdHash,
          record.pkceChallenge,
          record.intent || "login",
          record.expiresAt,
          record.createdAt || nowISO(),
        ]
      );
      return { ok: true };
    } catch (error) {
      if (error && error.code === "23505") {
        return { ok: false, error: "BROWSER_AUTH_REQUEST_EXISTS" };
      }
      throw error;
    }
  }

  async approveBrowserAuthRequest(requestIdHash, webUser) {
    return this.withTransaction(async (client) => {
      const result = await client.query(
        "select * from browser_auth_requests where request_id_hash=$1 for update",
        [requestIdHash]
      );
      if (result.rowCount === 0) return { ok: false, error: "BROWSER_AUTH_REQUEST_NOT_FOUND" };
      const record = browserAuthRequestFromRow(result.rows[0]);

      if (Date.parse(record.expiresAt) <= Date.now()) {
        await client.query(
          "update browser_auth_requests set status='expired' where request_id_hash=$1",
          [requestIdHash]
        );
        return { ok: false, error: "BROWSER_AUTH_EXPIRED" };
      }
      if (record.status === "consumed") return { ok: false, error: "BROWSER_AUTH_CONSUMED" };
      if (record.status === "approved") {
        return record.webUserId === webUser.id
          ? { ok: true, duplicate: true }
          : { ok: false, error: "BROWSER_AUTH_ALREADY_APPROVED" };
      }
      if (record.status !== "pending") return { ok: false, error: "BROWSER_AUTH_INVALID_STATE" };

      await client.query(
        `update browser_auth_requests
         set status='approved', web_user_id=$2, web_user_json=$3::jsonb, approved_at=now()
         where request_id_hash=$1`,
        [requestIdHash, webUser.id, JSON.stringify(webUser)]
      );
      return { ok: true, duplicate: false };
    });
  }

  async exchangeBrowserAuthRequest(requestIdHash, presentedChallenge) {
    return this.withTransaction(async (client) => {
      const result = await client.query(
        "select * from browser_auth_requests where request_id_hash=$1 for update",
        [requestIdHash]
      );
      if (result.rowCount === 0) return { ok: false, error: "BROWSER_AUTH_REQUEST_NOT_FOUND" };
      const record = browserAuthRequestFromRow(result.rows[0]);

      if (Date.parse(record.expiresAt) <= Date.now()) {
        await client.query(
          "update browser_auth_requests set status='expired' where request_id_hash=$1",
          [requestIdHash]
        );
        return { ok: false, error: "BROWSER_AUTH_EXPIRED" };
      }
      if (record.status === "pending") return { ok: false, error: "BROWSER_AUTH_PENDING" };
      if (record.status === "consumed") return { ok: false, error: "BROWSER_AUTH_CONSUMED" };
      if (record.status !== "approved") return { ok: false, error: "BROWSER_AUTH_INVALID_STATE" };
      if (record.pkceChallenge !== presentedChallenge) {
        return { ok: false, error: "BROWSER_AUTH_PKCE_MISMATCH" };
      }

      const consumed = await client.query(
        `update browser_auth_requests set status='consumed', consumed_at=now()
         where request_id_hash=$1 returning *`,
        [requestIdHash]
      );
      return { ok: true, request: browserAuthRequestFromRow(consumed.rows[0]) };
    });
  }

  async getPlayer(playerId) {
    const result = await this.pool.query("select * from game_players where id = $1", [playerId]);
    return playerFromRow(result.rows[0]);
  }

  async setActivePlayerSession(playerId, sessionId) {
    const result = await this.pool.query(
      `update game_players
       set active_session_id=$2, active_session_updated_at=now(), updated_at=now()
       where id=$1 returning id, active_session_id`,
      [playerId, String(sessionId || "")]
    );
    if (result.rowCount === 0) throw new Error(`PLAYER_NOT_FOUND:${playerId}`);
    return { playerId: result.rows[0].id, sessionId: result.rows[0].active_session_id };
  }

  async isActivePlayerSession(playerId, sessionId) {
    if (!playerId || !sessionId) return false;
    const result = await this.pool.query(
      "select 1 from game_players where id=$1 and active_session_id=$2",
      [playerId, sessionId]
    );
    return result.rowCount > 0;
  }

  async clearActivePlayerSession(playerId, sessionId) {
    const params = [playerId];
    let condition = "id=$1";
    if (sessionId) {
      params.push(sessionId);
      condition += " and active_session_id=$2";
    }
    const result = await this.pool.query(
      `update game_players
       set active_session_id=null, active_session_updated_at=now(), updated_at=now()
       where ${condition}`,
      params
    );
    return result.rowCount > 0;
  }

  async getProfile(playerId) {
    const result = await this.pool.query("select * from player_profiles where player_id = $1", [playerId]);
    return profileFromRow(result.rows[0]);
  }

  async setProfile(playerId, profile) {
    return this.withTransaction(async (client) => {
      await this.ensurePlayerStateWithClient(client, playerId);
      const incoming = { ...(profile || {}), updatedAt: nowISO() };
      const result = await client.query(
        `insert into player_profiles
         (player_id, version, name, gender, avatar_id, level, exp,
          character_created, tutorial_completed, profile_json, created_at, updated_at)
         values ($1,$2,$3,$4,$5,$6,$7,$8,$9,$10::jsonb,$11,$12)
         on conflict (player_id) do update set
           version=excluded.version, name=excluded.name, gender=excluded.gender,
           avatar_id=excluded.avatar_id, level=excluded.level, exp=excluded.exp,
           character_created=excluded.character_created,
           tutorial_completed=excluded.tutorial_completed,
           profile_json=excluded.profile_json, updated_at=excluded.updated_at
         returning *`,
        [
          playerId,
          toInt(incoming.version, 1),
          incoming.name || "Player",
          incoming.gender || "male",
          incoming.avatarId || incoming.avatar_id || "",
          toInt(incoming.level, 1),
          toNumber(incoming.exp, 0),
          Boolean(incoming.characterCreated ?? incoming.character_created),
          Boolean(incoming.tutorialCompleted ?? incoming.tutorial_completed),
          JSON.stringify(incoming),
          incoming.createdAt || incoming.created_at || nowISO(),
          incoming.updatedAt,
        ]
      );
      return profileFromRow(result.rows[0]);
    });
  }

  async ensurePlayerState(playerId) {
    return this.withTransaction(async (client) => {
      await this.ensurePlayerStateWithClient(client, playerId);
      return {
        economy: await this.getEconomyWithClient(client, playerId),
        inventory: await this.getInventoryWithClient(client, playerId),
        farmState: await this.getFarmStateWithClient(client, playerId),
        dailyLimits: await this.getDailyLimitsWithClient(client, playerId),
      };
    });
  }

  async getEconomyWithClient(client, playerId, lock = false) {
    const result = await client.query(
      `select * from player_economy where player_id = $1${lock ? " for update" : ""}`,
      [playerId]
    );
    return economyFromRow(result.rows[0]);
  }

  async getEconomy(playerId) {
    return this.withTransaction(async (client) => {
      await this.ensurePlayerStateWithClient(client, playerId);
      return this.getEconomyWithClient(client, playerId);
    });
  }

  async setEconomy(playerId, economy) {
    return this.withTransaction(async (client) => {
      await this.ensurePlayerStateWithClient(client, playerId);
      const current = await this.getEconomyWithClient(client, playerId, true);
      const incoming = economy || {};
      const result = await client.query(
        `update player_economy set version=$2, pos=$3, updated_at=now()
         where player_id=$1 returning *`,
        [
          playerId,
          toInt(incoming.version, current.version || 1),
          toSafeInteger(incoming.pos == null ? current.pos : incoming.pos, "economy.pos"),
        ]
      );
      return economyFromRow(result.rows[0]);
    });
  }

  async applyEconomyDelta(playerId, delta, meta = {}) {
    return this.withTransaction(async (client) => {
      await this.ensurePlayerStateWithClient(client, playerId);
      const deltaPos = toSafeInteger(toNumber(delta && delta.pos, 0), "delta.pos");
      const idempotencyKey = String(meta.idempotencyKey || "").trim();
      const requestSignature = JSON.stringify({ op: "economy", playerId, deltaPos, type: meta.type || "adjust", ref: meta.ref || "" });
      await this.lockIdempotency(client, idempotencyKey);
      const existing = await this.findStoredTransaction(client, idempotencyKey);
      if (existing) return this.duplicateResult(existing, requestSignature);

      const economy = await this.getEconomyWithClient(client, playerId, true);
      if (economy.pos + deltaPos < 0) {
        return { ok: false, error: "INSUFFICIENT_BALANCE", economy };
      }
      const updated = await client.query(
        `update player_economy set pos=pos+$2, updated_at=now()
         where player_id=$1 returning *`,
        [playerId, deltaPos]
      );
      const economyAfter = economyFromRow(updated.rows[0]);
      const transaction = {
        id: makeId("tx"), playerId, type: meta.type || "adjust", ref: meta.ref || "",
        idempotencyKey, requestSignature, deltaPos,
        economyAfter: { pos: economyAfter.pos }, createdAt: nowISO(),
      };
      const response = { ok: true, economy: economyAfter, duplicate: false };
      await this.insertTransaction(client, transaction, response);
      return { ...response, transaction };
    });
  }

  async creditWebTopup(webUser, pointAmountMicros, meta = {}) {
    if (!webUser || !webUser.id) return { ok: false, error: "INVALID_WEB_TOPUP" };
    const amountMicros = toSafeInteger(pointAmountMicros, "web_topup.point_amount_micros");
    const pointAmount = String(meta.pointAmount || "").trim();
    const transactionId = String(meta.transactionId || "").trim();
    const source = String(meta.source || "ywonder-web").trim();
    const expectedPlayerId = String(meta.expectedPlayerId || "").trim();
    if (!transactionId || !pointAmount || amountMicros < 1) {
      return { ok: false, error: "INVALID_WEB_TOPUP" };
    }

    return this.withTransaction(async (client) => {
      let player;
      if (expectedPlayerId) {
        const mapped = await client.query(
          "select * from game_players where web_user_id = $1 for update",
          [webUser.id]
        );
        if (mapped.rowCount !== 1 || mapped.rows[0].id !== expectedPlayerId) {
          return { ok: false, error: "GAME_POINT_IDENTITY_MISMATCH" };
        }
        player = playerFromRow(mapped.rows[0]);
        await this.ensurePlayerStateWithClient(client, player.id);
      } else {
        player = await this.getOrCreatePlayerForWebUserWithClient(client, webUser);
      }
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
      await this.lockIdempotency(client, idempotencyKey);
      const existing = await this.findStoredTransaction(client, idempotencyKey);
      if (existing) {
        const duplicate = this.duplicateResult(existing, requestSignature);
        if (!duplicate.ok) return duplicate;
        const currentEconomy = await this.getEconomyWithClient(client, player.id);
        return { ...duplicate, player, economy: currentEconomy };
      }

      const locked = await client.query(
        "select * from player_economy where player_id=$1 for update",
        [player.id]
      );
      const economy = economyFromRow(locked.rows[0]);
      const remainderBefore = toSafeInteger(
        locked.rows[0].web_point_micros_remainder,
        "economy.web_point_micros_remainder"
      );
      const totalMicros = remainderBefore + amountMicros;
      if (!Number.isSafeInteger(totalMicros)) {
        return { ok: false, error: "POINT_BALANCE_OVERFLOW" };
      }
      const deltaPos = Math.floor(totalMicros / 1_000_000);
      const remainderAfter = totalMicros % 1_000_000;
      if (economy.pos > Number.MAX_SAFE_INTEGER - deltaPos) {
        return { ok: false, error: "POINT_BALANCE_OVERFLOW" };
      }
      const updated = await client.query(
        `update player_economy
         set pos=pos+$2, web_point_micros_remainder=$3, updated_at=now()
         where player_id=$1 returning *`,
        [player.id, deltaPos, remainderAfter]
      );
      const economyAfter = economyFromRow(updated.rows[0]);
      const transaction = {
        id: makeId("wtx"),
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
        economyAfter,
        createdAt: nowISO(),
      };
      const response = { ok: true, duplicate: false, economy: economyAfter };
      await this.insertTransaction(client, transaction, response);
      return { ...response, player, transaction };
    });
  }

  async applyWebPointReservation(operation, input) {
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

    const requestSignature = JSON.stringify({
      reservationId,
      webUserId,
      expectedPlayerId,
      pointAmount,
      purpose,
      source,
      occurredAt,
    });

    return this.withTransaction(async (client) => {
      await this.lockIdempotency(client, `point_reservation:${reservationId}`);
      const playerResult = await client.query(
        "select * from game_players where web_user_id=$1 for update",
        [webUserId]
      );
      if (playerResult.rowCount !== 1 || playerResult.rows[0].id !== expectedPlayerId) {
        return { ok: false, error: "GAME_POINT_IDENTITY_MISMATCH" };
      }
      const player = playerFromRow(playerResult.rows[0]);
      await this.ensurePlayerStateWithClient(client, player.id);

      const storedResult = await client.query(
        "select * from point_wallet_reservations where id=$1 for update",
        [reservationId]
      );
      let stored = storedResult.rows[0] || null;
      if (stored && (stored.player_id !== player.id
          || stored.web_user_id !== webUserId
          || stored.expected_player_id !== expectedPlayerId
          || stored.request_signature !== requestSignature)) {
        return { ok: false, error: "IDEMPOTENCY_CONFLICT" };
      }

      const economy = await this.getEconomyWithClient(client, player.id, true);
      const idempotencyKey = `web_point_${op}:${source}:${reservationId}`;
      const existingTransaction = await this.findStoredTransaction(client, idempotencyKey);
      const duplicateResult = (reservation) => ({
        ok: true,
        duplicate: true,
        player,
        economy,
        reservation: pointReservationFromRow(reservation),
        transaction: transactionFromRow(existingTransaction),
      });

      if (op === "reserve") {
        if (stored) return duplicateResult(stored);
        if (economy.pos < pointAmount) {
          return { ok: false, error: "INSUFFICIENT_BALANCE", economy };
        }

        const updatedEconomyResult = await client.query(
          `update player_economy set pos=pos-$2, updated_at=now()
           where player_id=$1 returning *`,
          [player.id, pointAmount]
        );
        const economyAfter = economyFromRow(updatedEconomyResult.rows[0]);
        const inserted = await client.query(
          `insert into point_wallet_reservations
           (id,player_id,web_user_id,expected_player_id,point_amount,purpose,source,
            occurred_at,request_signature,status,created_at,updated_at)
           values ($1,$2,$3,$4,$5,$6,$7,$8,$9,'RESERVED',now(),now()) returning *`,
          [
            reservationId,
            player.id,
            webUserId,
            expectedPlayerId,
            pointAmount,
            purpose,
            source,
            occurredAt,
            requestSignature,
          ]
        );
        stored = inserted.rows[0];
        const transaction = {
          id: makeId("wpr"),
          playerId: player.id,
          type: "web_point_reserve",
          ref: reservationId,
          idempotencyKey,
          requestSignature: JSON.stringify({ op, requestSignature }),
          deltaPos: -pointAmount,
          economyAfter,
          createdAt: nowISO(),
        };
        const response = {
          ok: true,
          duplicate: false,
          economy: economyAfter,
          reservation: pointReservationFromRow(stored),
        };
        await this.insertTransaction(client, transaction, response);
        return { ...response, player, transaction };
      }

      if (!stored) return { ok: false, error: "POINT_RESERVATION_NOT_FOUND" };
      const targetStatus = op === "capture" ? "CAPTURED" : "RELEASED";
      if (stored.status === targetStatus) return duplicateResult(stored);
      if (stored.status !== "RESERVED") {
        return { ok: false, error: "POINT_RESERVATION_STATE_CONFLICT" };
      }

      let economyAfter = economy;
      let deltaPos = 0;
      if (op === "release") {
        if (economy.pos > Number.MAX_SAFE_INTEGER - pointAmount) {
          return { ok: false, error: "POINT_BALANCE_OVERFLOW" };
        }
        const updatedEconomyResult = await client.query(
          `update player_economy set pos=pos+$2, updated_at=now()
           where player_id=$1 returning *`,
          [player.id, pointAmount]
        );
        economyAfter = economyFromRow(updatedEconomyResult.rows[0]);
        deltaPos = pointAmount;
      }

      const transitioned = await client.query(
        `update point_wallet_reservations
         set status=$2,
             captured_at=case when $2='CAPTURED' then now() else captured_at end,
             released_at=case when $2='RELEASED' then now() else released_at end,
             updated_at=now()
         where id=$1 and status='RESERVED' returning *`,
        [reservationId, targetStatus]
      );
      if (transitioned.rowCount !== 1) {
        throw new Error("POINT_RESERVATION_TRANSITION_RACE");
      }
      stored = transitioned.rows[0];
      const transaction = {
        id: makeId("wpr"),
        playerId: player.id,
        type: `web_point_${op}`,
        ref: reservationId,
        idempotencyKey,
        requestSignature: JSON.stringify({ op, requestSignature }),
        deltaPos,
        economyAfter,
        createdAt: nowISO(),
      };
      const response = {
        ok: true,
        duplicate: false,
        economy: economyAfter,
        reservation: pointReservationFromRow(stored),
      };
      await this.insertTransaction(client, transaction, response);
      return { ...response, player, transaction };
    });
  }

  async getInventoryWithClient(client, playerId, lock = false) {
    const meta = await client.query(
      `select * from player_inventory_meta where player_id=$1${lock ? " for update" : ""}`,
      [playerId]
    );
    const slots = await client.query(
      "select * from player_inventory where player_id=$1 and quantity>0 order by item_id",
      [playerId]
    );
    const row = meta.rows[0];
    return {
      version: toInt(row.version, 1),
      maxSlots: toInt(row.max_slots, 50),
      slots: slots.rows.map(slotFromRow),
      updatedAt: new Date(row.updated_at).toISOString(),
    };
  }

  async getInventory(playerId) {
    return this.withTransaction(async (client) => {
      await this.ensurePlayerStateWithClient(client, playerId);
      return this.getInventoryWithClient(client, playerId);
    });
  }

  async replaceInventoryWithClient(client, playerId, inventory) {
    await this.ensurePlayerStateWithClient(client, playerId);
    const incoming = inventory || makeDefaultInventory();
    const slots = Array.isArray(incoming.slots) ? incoming.slots : [];
    const maxSlots = Math.max(toInt(incoming.maxSlots ?? incoming.max_slots, 50), slots.length);
    await client.query(
      `update player_inventory_meta set version=$2, max_slots=$3, updated_at=now() where player_id=$1`,
      [playerId, toInt(incoming.version, 1), maxSlots]
    );
    await client.query("delete from player_inventory where player_id=$1", [playerId]);
    for (const rawSlot of slots) {
      const itemId = String(rawSlot.itemId || rawSlot.item_id || "").trim();
      const quantity = Math.max(0, toInt(rawSlot.quantity, 0));
      if (!itemId || quantity <= 0) continue;
      await client.query(
        `insert into player_inventory
         (player_id,item_id,quantity,slot_tab,equipped,durability,updated_at)
         values ($1,$2,$3,$4,$5,$6,now())`,
        [
          playerId, itemId, quantity, rawSlot.slotTab || rawSlot.slot_tab || "",
          Boolean(rawSlot.equipped), rawSlot.durability == null ? null : toInt(rawSlot.durability, 0),
        ]
      );
    }
    return this.getInventoryWithClient(client, playerId);
  }

  async setInventory(playerId, inventory) {
    return this.withTransaction((client) => this.replaceInventoryWithClient(client, playerId, inventory));
  }

  async adjustInventoryItem(playerId, itemId, quantityDelta, meta = {}) {
    return this.withTransaction(async (client) => {
      await this.ensurePlayerStateWithClient(client, playerId);
      const id = String(itemId || "").trim();
      const deltaQty = toInt(quantityDelta, 0);
      const idempotencyKey = String(meta.idempotencyKey || "").trim();
      const requestSignature = JSON.stringify({ op: "inventory", playerId, itemId: id, deltaQty, type: meta.type || "inventory_adjust", ref: meta.ref || id });
      await this.lockIdempotency(client, idempotencyKey);
      const existing = await this.findStoredTransaction(client, idempotencyKey);
      if (existing) return this.duplicateResult(existing, requestSignature);

      const inventory = await this.getInventoryWithClient(client, playerId, true);
      const slotResult = await client.query(
        "select * from player_inventory where player_id=$1 and item_id=$2 for update",
        [playerId, id]
      );
      const slot = slotResult.rows[0];
      const currentQuantity = slot ? toInt(slot.quantity, 0) : 0;
      if (!slot && deltaQty < 0) return { ok: false, error: "ITEM_NOT_FOUND", inventory };
      if (currentQuantity + deltaQty < 0) return { ok: false, error: "INSUFFICIENT_ITEM", inventory };

      if (currentQuantity + deltaQty === 0) {
        await client.query("delete from player_inventory where player_id=$1 and item_id=$2", [playerId, id]);
      } else if (slot) {
        await client.query(
          "update player_inventory set quantity=quantity+$3, updated_at=now() where player_id=$1 and item_id=$2",
          [playerId, id, deltaQty]
        );
      } else {
        if (inventory.slots.length >= inventory.maxSlots) {
          await client.query("update player_inventory_meta set max_slots=max_slots+1 where player_id=$1", [playerId]);
        }
        await client.query(
          "insert into player_inventory (player_id,item_id,quantity) values ($1,$2,$3)",
          [playerId, id, deltaQty]
        );
      }
      await client.query(
        `update player_inventory_meta
         set max_slots=greatest(max_slots,(select count(*)::integer from player_inventory where player_id=$1)),
             updated_at=now()
         where player_id=$1`,
        [playerId]
      );
      const inventoryAfter = await this.getInventoryWithClient(client, playerId);
      const transaction = {
        id: makeId("itx"), playerId, type: meta.type || "inventory_adjust", ref: meta.ref || id,
        idempotencyKey, requestSignature, itemId: id, quantityDelta: deltaQty,
        inventoryAfter: clone(inventoryAfter), createdAt: nowISO(),
      };
      const response = { ok: true, inventory: inventoryAfter, duplicate: false };
      await this.insertTransaction(client, transaction, response);
      return { ...response, transaction };
    });
  }

  async transactShop(playerId, offer, quantity, options = {}) {
    const normalizedQuantity = toInt(quantity, 0);
    const shopId = String(offer && offer.shopId || "").trim();
    const mode = String(offer && offer.mode || "").trim();
    const itemId = String(offer && offer.itemId || "").trim();
    const unitPrice = Number(offer && offer.unitPrice);
    const idempotencyKey = String(options.idempotencyKey || "").trim();
    if (!idempotencyKey) return { ok: false, error: "MISSING_IDEMPOTENCY_KEY" };
    if (!shopId || !itemId || !["buy", "sell"].includes(mode)) return { ok: false, error: "INVALID_SHOP_REQUEST" };
    if (normalizedQuantity < 1 || normalizedQuantity > 999) return { ok: false, error: "INVALID_QUANTITY" };
    if (!Number.isSafeInteger(unitPrice) || unitPrice < 0) return { ok: false, error: "INVALID_ITEM_PRICE" };
    const totalPrice = unitPrice * normalizedQuantity;
    if (!Number.isSafeInteger(totalPrice)) return { ok: false, error: "INVALID_ITEM_PRICE" };
    const requestSignature = `${shopId}|${mode}|${itemId}|${normalizedQuantity}|${unitPrice}`;

    return this.withTransaction(async (client) => {
      await this.ensurePlayerStateWithClient(client, playerId);
      await this.lockIdempotency(client, idempotencyKey);
      const existing = await this.findStoredTransaction(client, idempotencyKey);
      if (existing) return this.duplicateResult(existing, requestSignature);

      const economy = await this.getEconomyWithClient(client, playerId, true);
      const inventory = await this.getInventoryWithClient(client, playerId, true);
      const slotResult = await client.query(
        "select * from player_inventory where player_id=$1 and item_id=$2 for update",
        [playerId, itemId]
      );
      const slot = slotResult.rows[0];
      const currentQuantity = slot ? toInt(slot.quantity, 0) : 0;
      let deltaPos;
      let quantityDelta;
      if (mode === "buy") {
        if (economy.pos < totalPrice) return { ok: false, error: "INSUFFICIENT_BALANCE", economy, inventory };
        deltaPos = -totalPrice;
        quantityDelta = normalizedQuantity;
      } else {
        if (!slot || currentQuantity < normalizedQuantity) return { ok: false, error: "INSUFFICIENT_ITEM", economy, inventory };
        deltaPos = totalPrice;
        quantityDelta = -normalizedQuantity;
      }

      await client.query("update player_economy set pos=pos+$2,updated_at=now() where player_id=$1", [playerId, deltaPos]);
      const nextQuantity = currentQuantity + quantityDelta;
      if (nextQuantity <= 0) {
        await client.query("delete from player_inventory where player_id=$1 and item_id=$2", [playerId, itemId]);
      } else if (slot) {
        await client.query("update player_inventory set quantity=$3,updated_at=now() where player_id=$1 and item_id=$2", [playerId, itemId, nextQuantity]);
      } else {
        if (inventory.slots.length >= inventory.maxSlots) {
          await client.query("update player_inventory_meta set max_slots=max_slots+1 where player_id=$1", [playerId]);
        }
        await client.query("insert into player_inventory (player_id,item_id,quantity) values ($1,$2,$3)", [playerId, itemId, nextQuantity]);
      }
      await client.query("update player_inventory_meta set updated_at=now() where player_id=$1", [playerId]);
      const economyAfter = await this.getEconomyWithClient(client, playerId);
      const inventoryAfter = await this.getInventoryWithClient(client, playerId);
      const transaction = {
        id: makeId("stx"), playerId, type: mode === "buy" ? "shop_buy" : "shop_sell",
        ref: `${shopId}:${itemId}`, idempotencyKey, requestSignature, shopId, mode, itemId,
        quantity: normalizedQuantity, unitPrice, totalPrice, deltaPos, quantityDelta,
        economyAfter: clone(economyAfter), inventoryAfter: clone(inventoryAfter), createdAt: nowISO(),
      };
      const response = { ok: true, economy: economyAfter, inventory: inventoryAfter, duplicate: false };
      await this.insertTransaction(client, transaction, response);
      return { ...response, transaction };
    });
  }

  async applyResourceHarvest(playerId, rewards, options = {}) {
    const normalizedRewards = (Array.isArray(rewards) ? rewards : [])
      .map((reward) => ({
        itemId: String(reward && reward.itemId || "").trim(),
        quantity: Math.max(0, toInt(reward && reward.quantity, 0)),
        displayName: String(reward && reward.displayName || "").trim(),
        kind: String(reward && reward.kind || "resource").trim(),
      }))
      .filter((reward) => reward.itemId && reward.quantity > 0);
    if (normalizedRewards.length === 0) return { ok: false, error: "MISSING_REWARDS" };

    return this.withTransaction(async (client) => {
      await this.ensurePlayerStateWithClient(client, playerId);
      const idempotencyKey = String(options.idempotencyKey || "").trim();
      const requestSignature = JSON.stringify({ op: "resource", playerId, rewards: normalizedRewards, dailyLimit: options.dailyLimit || null });
      await this.lockIdempotency(client, idempotencyKey);
      const existing = await this.findStoredTransaction(client, idempotencyKey);
      if (existing) return this.duplicateResult(existing, requestSignature);

      await this.getInventoryWithClient(client, playerId, true);
      let limit = null;
      if (options.dailyLimit) {
        const key = String(options.dailyLimit.key || "").trim();
        if (!key) return { ok: false, error: "MISSING_LIMIT_KEY" };
        const amount = Math.max(1, toInt(options.dailyLimit.amount, 1));
        const maxCount = Math.max(1, toInt(options.dailyLimit.maxCount, 10));
        const currentPeriod = String(options.dailyLimit.periodKey || periodKey());
        limit = await this.lockDailyLimit(client, playerId, key, currentPeriod, maxCount);
        if (limit.used + amount > limit.maxCount) {
          return { ok: false, error: "DAILY_LIMIT_EXCEEDED", daily_limits: await this.getDailyLimitsWithClient(client, playerId), limit };
        }
        await client.query(
          `update player_daily_limits set used_count=used_count+$4,max_count=$5,updated_at=now()
           where player_id=$1 and limit_key=$2 and period_key=$3`,
          [playerId, key, currentPeriod, amount, maxCount]
        );
      }

      for (const reward of normalizedRewards) {
        await client.query(
          `insert into player_inventory (player_id,item_id,quantity,updated_at)
           values ($1,$2,$3,now())
           on conflict (player_id,item_id) do update
           set quantity=player_inventory.quantity+excluded.quantity,updated_at=now()`,
          [playerId, reward.itemId, reward.quantity]
        );
      }
      await client.query(
        `update player_inventory_meta
         set max_slots=greatest(max_slots,(select count(*)::integer from player_inventory where player_id=$1)),
             updated_at=now()
         where player_id=$1`,
        [playerId]
      );
      const inventoryAfter = await this.getInventoryWithClient(client, playerId);
      const dailyLimitsAfter = await this.getDailyLimitsWithClient(client, playerId);
      if (options.dailyLimit) limit = dailyLimitsAfter.limits[String(options.dailyLimit.key || "").trim()] || limit;
      const transaction = {
        id: makeId("rtx"), playerId, type: String(options.type || "resource_harvest"),
        ref: String(options.ref || ""), idempotencyKey, requestSignature,
        rewards: normalizedRewards, inventoryAfter: clone(inventoryAfter),
        dailyLimitsAfter: clone(dailyLimitsAfter), limitAfter: limit ? { ...limit } : null,
        createdAt: nowISO(),
      };
      const response = { ok: true, inventory: inventoryAfter, daily_limits: dailyLimitsAfter, limit, rewards: normalizedRewards, duplicate: false };
      await this.insertTransaction(client, transaction, response);
      return { ...response, transaction };
    });
  }

  // Câu cá SERVER-AUTHORITATIVE (prod). Server tự bốc cá + tự quản lượt free/mồi; idempotent.
  async resolveFishingCatch(playerId, options = {}) {
    const idempotencyKey = String(options.idempotencyKey || "").trim();
    if (!idempotencyKey) return { ok: false, error: "MISSING_IDEMPOTENCY_KEY" };
    const currentPeriod = String(options.periodKey || periodKey());
    // Thưởng NGẪU NHIÊN nên KHÔNG đưa cá vào signature; replay trả nguyên result_json (đã có cá).
    const requestSignature = JSON.stringify({ op: "fishing", playerId });

    return this.withTransaction(async (client) => {
      await this.ensurePlayerStateWithClient(client, playerId);
      await this.lockIdempotency(client, idempotencyKey);
      const existing = await this.findStoredTransaction(client, idempotencyKey);
      if (existing) return this.duplicateResult(existing, requestSignature);

      const inventory = await this.getInventoryWithClient(client, playerId, true);
      const limit = await this.lockDailyLimit(client, playerId, "fishing", currentPeriod, FISHING_FREE_PER_DAY);

      let usedBait = false;
      if (limit.used < limit.maxCount) {
        await client.query(
          `update player_daily_limits set used_count=used_count+1,max_count=$4,updated_at=now()
           where player_id=$1 and limit_key=$2 and period_key=$3`,
          [playerId, "fishing", currentPeriod, FISHING_FREE_PER_DAY]
        );
      } else {
        // Hết free -> phải có 1 mồi bait_01.
        const baitRes = await client.query(
          "select * from player_inventory where player_id=$1 and item_id=$2 for update",
          [playerId, FISHING_BAIT_ID]
        );
        const baitQty = baitRes.rows[0] ? toInt(baitRes.rows[0].quantity, 0) : 0;
        if (baitQty < 1) {
          return {
            ok: false,
            error: "NO_FISHING_TURN",
            inventory,
            daily_limits: await this.getDailyLimitsWithClient(client, playerId, currentPeriod),
            limit,
          };
        }
        if (baitQty - 1 <= 0) {
          await client.query("delete from player_inventory where player_id=$1 and item_id=$2", [playerId, FISHING_BAIT_ID]);
        } else {
          await client.query(
            "update player_inventory set quantity=quantity-1,updated_at=now() where player_id=$1 and item_id=$2",
            [playerId, FISHING_BAIT_ID]
          );
        }
        usedBait = true;
      }

      // Server tự bốc cá (client không quyết được).
      const fish = rollFish();
      await client.query(
        `insert into player_inventory (player_id,item_id,quantity,updated_at)
         values ($1,$2,1,now())
         on conflict (player_id,item_id) do update
         set quantity=player_inventory.quantity+1,updated_at=now()`,
        [playerId, fish.itemId]
      );
      await client.query(
        `update player_inventory_meta
         set max_slots=greatest(max_slots,(select count(*)::integer from player_inventory where player_id=$1)),
             updated_at=now()
         where player_id=$1`,
        [playerId]
      );

      const inventoryAfter = await this.getInventoryWithClient(client, playerId);
      const dailyLimitsAfter = await this.getDailyLimitsWithClient(client, playerId, currentPeriod);
      const limitAfter = dailyLimitsAfter.limits.fishing || limit;
      const baitAfterSlot = inventoryAfter.slots.find((s) => s.itemId === FISHING_BAIT_ID);
      const baitRemaining = baitAfterSlot ? toInt(baitAfterSlot.quantity, 0) : 0;

      const response = {
        ok: true,
        fish,
        inventory: inventoryAfter,
        daily_limits: dailyLimitsAfter,
        limit: limitAfter,
        usedBait,
        baitRemaining,
        duplicate: false,
      };
      const transaction = {
        id: makeId("ftx"), playerId, type: "fishing_catch", ref: fish.itemId,
        idempotencyKey, requestSignature,
        fish, usedBait, baitRemaining,
        inventoryAfter: clone(inventoryAfter), dailyLimitsAfter: clone(dailyLimitsAfter),
        limitAfter: limitAfter ? { ...limitAfter } : null, createdAt: nowISO(),
      };
      await this.insertTransaction(client, transaction, response);
      return { ...response, transaction };
    });
  }

  // Đổi 1 Vé đào mỏ (mine_ticket_01) -> +1 lượt đào (tăng max_count daily-limit "mining"). Idempotent.
  async resolveMineTicketRedeem(playerId, options = {}) {
    const MINE_TICKET_ID = "mine_ticket_01";
    const MINING_MAX_PER_DAY = 10;

    const idempotencyKey = String(options.idempotencyKey || "").trim();
    if (!idempotencyKey) return { ok: false, error: "MISSING_IDEMPOTENCY_KEY" };
    const currentPeriod = String(options.periodKey || periodKey());
    const requestSignature = JSON.stringify({ op: "mine_ticket_redeem", playerId });

    return this.withTransaction(async (client) => {
      await this.ensurePlayerStateWithClient(client, playerId);
      await this.lockIdempotency(client, idempotencyKey);
      const existing = await this.findStoredTransaction(client, idempotencyKey);
      if (existing) return this.duplicateResult(existing, requestSignature);

      const inventory = await this.getInventoryWithClient(client, playerId, true);

      // Cần 1 vé đào mine_ticket_01.
      const ticketRes = await client.query(
        "select * from player_inventory where player_id=$1 and item_id=$2 for update",
        [playerId, MINE_TICKET_ID]
      );
      const ticketQty = ticketRes.rows[0] ? toInt(ticketRes.rows[0].quantity, 0) : 0;
      if (ticketQty < 1) {
        return {
          ok: false,
          error: "NO_MINE_TICKET",
          inventory,
          daily_limits: await this.getDailyLimitsWithClient(client, playerId, currentPeriod),
        };
      }
      if (ticketQty - 1 <= 0) {
        await client.query("delete from player_inventory where player_id=$1 and item_id=$2", [playerId, MINE_TICKET_ID]);
      } else {
        await client.query(
          "update player_inventory set quantity=quantity-1,updated_at=now() where player_id=$1 and item_id=$2",
          [playerId, MINE_TICKET_ID]
        );
      }

      // +1 lượt đào: đảm bảo có row rồi tăng max_count.
      await this.lockDailyLimit(client, playerId, "mining", currentPeriod, MINING_MAX_PER_DAY);
      await client.query(
        `update player_daily_limits set max_count=max_count+1,updated_at=now()
         where player_id=$1 and limit_key=$2 and period_key=$3`,
        [playerId, "mining", currentPeriod]
      );

      const inventoryAfter = await this.getInventoryWithClient(client, playerId);
      const dailyLimitsAfter = await this.getDailyLimitsWithClient(client, playerId, currentPeriod);
      const limitAfter = dailyLimitsAfter.limits.mining || null;
      const miningTurnsRemaining = limitAfter
        ? Math.max(0, toInt(limitAfter.maxCount, 0) - toInt(limitAfter.used, 0))
        : 0;
      const ticketSlotAfter = inventoryAfter.slots.find((s) => s.itemId === MINE_TICKET_ID);
      const ticketRemaining = ticketSlotAfter ? toInt(ticketSlotAfter.quantity, 0) : 0;

      const response = {
        ok: true,
        miningTurnsRemaining,
        ticketRemaining,
        inventory: inventoryAfter,
        daily_limits: dailyLimitsAfter,
        limit: limitAfter,
        duplicate: false,
      };
      const transaction = {
        id: makeId("mtx"), playerId, type: "mine_ticket_redeem", ref: MINE_TICKET_ID,
        idempotencyKey, requestSignature,
        miningTurnsRemaining, ticketRemaining,
        inventoryAfter: clone(inventoryAfter), dailyLimitsAfter: clone(dailyLimitsAfter),
        limitAfter: limitAfter ? { ...limitAfter } : null, createdAt: nowISO(),
      };
      await this.insertTransaction(client, transaction, response);
      return { ...response, transaction };
    });
  }

  // 1 lượt quay: 3 free/ngày (daily-limit "spin") rồi trừ 1 spin_ticket_01. Server đếm theo ngày server. Idempotent.
  async resolveWheelSpin(playerId, options = {}) {
    const SPIN_TICKET_ID = "spin_ticket_01";
    const SPIN_FREE_PER_DAY = 3;

    const idempotencyKey = String(options.idempotencyKey || "").trim();
    if (!idempotencyKey) return { ok: false, error: "MISSING_IDEMPOTENCY_KEY" };
    const currentPeriod = String(options.periodKey || periodKey());
    const requestSignature = JSON.stringify({ op: "wheel_spin", playerId });

    return this.withTransaction(async (client) => {
      await this.ensurePlayerStateWithClient(client, playerId);
      await this.lockIdempotency(client, idempotencyKey);
      const existing = await this.findStoredTransaction(client, idempotencyKey);
      if (existing) return this.duplicateResult(existing, requestSignature);

      const inventory = await this.getInventoryWithClient(client, playerId, true);
      const limit = await this.lockDailyLimit(client, playerId, "spin", currentPeriod, SPIN_FREE_PER_DAY);

      let usedTicket = false;
      if (limit.used < limit.maxCount) {
        await client.query(
          `update player_daily_limits set used_count=used_count+1,max_count=$4,updated_at=now()
           where player_id=$1 and limit_key=$2 and period_key=$3`,
          [playerId, "spin", currentPeriod, SPIN_FREE_PER_DAY]
        );
      } else {
        const ticketRes = await client.query(
          "select * from player_inventory where player_id=$1 and item_id=$2 for update",
          [playerId, SPIN_TICKET_ID]
        );
        const ticketQty = ticketRes.rows[0] ? toInt(ticketRes.rows[0].quantity, 0) : 0;
        if (ticketQty < 1) {
          return {
            ok: false,
            error: "NO_SPIN_TURN",
            inventory,
            daily_limits: await this.getDailyLimitsWithClient(client, playerId, currentPeriod),
            limit,
          };
        }
        if (ticketQty - 1 <= 0) {
          await client.query("delete from player_inventory where player_id=$1 and item_id=$2", [playerId, SPIN_TICKET_ID]);
        } else {
          await client.query(
            "update player_inventory set quantity=quantity-1,updated_at=now() where player_id=$1 and item_id=$2",
            [playerId, SPIN_TICKET_ID]
          );
        }
        usedTicket = true;
      }

      const inventoryAfter = await this.getInventoryWithClient(client, playerId);
      const dailyLimitsAfter = await this.getDailyLimitsWithClient(client, playerId, currentPeriod);
      const limitAfter = dailyLimitsAfter.limits.spin || limit;
      const spinsRemaining = limitAfter
        ? Math.max(0, toInt(limitAfter.maxCount, 0) - toInt(limitAfter.used, 0))
        : 0;
      const ticketSlotAfter = inventoryAfter.slots.find((s) => s.itemId === SPIN_TICKET_ID);
      const ticketRemaining = ticketSlotAfter ? toInt(ticketSlotAfter.quantity, 0) : 0;

      const response = {
        ok: true,
        usedTicket,
        spinsRemaining,
        ticketRemaining,
        inventory: inventoryAfter,
        daily_limits: dailyLimitsAfter,
        limit: limitAfter,
        duplicate: false,
      };
      const transaction = {
        id: makeId("stx"), playerId, type: "wheel_spin", ref: usedTicket ? SPIN_TICKET_ID : "free",
        idempotencyKey, requestSignature,
        usedTicket, spinsRemaining, ticketRemaining,
        inventoryAfter: clone(inventoryAfter), dailyLimitsAfter: clone(dailyLimitsAfter),
        limitAfter: limitAfter ? { ...limitAfter } : null, createdAt: nowISO(),
      };
      await this.insertTransaction(client, transaction, response);
      return { ...response, transaction };
    });
  }

  async getFarmStateWithClient(client, playerId) {
    const result = await client.query("select * from player_farm_state where player_id=$1", [playerId]);
    const row = result.rows[0];
    return {
      ...(row.state_json || {}),
      version: toInt(row.version, 1),
      updatedAt: new Date(row.updated_at).toISOString(),
    };
  }

  async getFarmState(playerId) {
    return this.withTransaction(async (client) => {
      await this.ensurePlayerStateWithClient(client, playerId);
      return this.getFarmStateWithClient(client, playerId);
    });
  }

  async setFarmState(playerId, farmState) {
    return this.withTransaction(async (client) => {
      await this.ensurePlayerStateWithClient(client, playerId);
      const current = await this.getFarmStateWithClient(client, playerId);
      const incoming = { ...current, ...(farmState || {}), updatedAt: nowISO() };
      const result = await client.query(
        `update player_farm_state set version=$2,state_json=$3::jsonb,updated_at=now()
         where player_id=$1 returning *`,
        [playerId, toInt(incoming.version, 1), JSON.stringify(incoming)]
      );
      const row = result.rows[0];
      return { ...(row.state_json || {}), version: toInt(row.version, 1), updatedAt: new Date(row.updated_at).toISOString() };
    });
  }

  async compareAndSetFarmState(playerId, expectedVersion, farmState) {
    return this.withTransaction(async (client) => {
      await this.ensurePlayerStateWithClient(client, playerId);
      const current = await this.getFarmStateWithClient(client, playerId);
      const incoming = { ...current, ...(farmState || {}) };
      delete incoming.version;
      delete incoming.updatedAt;

      const result = await client.query(
        `update player_farm_state
         set version=version+1,state_json=$3::jsonb,updated_at=now()
         where player_id=$1 and version=$2 returning *`,
        [playerId, expectedVersion, JSON.stringify(incoming)]
      );
      if (result.rowCount === 0) {
        return {
          ok: false,
          error: "FARM_STATE_CONFLICT",
          farm_state: await this.getFarmStateWithClient(client, playerId),
        };
      }

      const row = result.rows[0];
      return {
        ok: true,
        farm_state: {
          ...(row.state_json || {}),
          version: toInt(row.version, 1),
          updatedAt: new Date(row.updated_at).toISOString(),
        },
      };
    });
  }

  async placeFarmAnimal(playerId, placement, options = {}) {
    const itemId = String(placement && placement.itemId || "").trim();
    const cellKeys = Array.isArray(placement && placement.cellKeys)
      ? placement.cellKeys.map((key) => String(key || "").trim())
      : [];
    const rule = placement && placement.rule;
    const idempotencyKey = String(options.idempotencyKey || "").trim();
    const expectedVersion = toInt(options.expectedVersion, 0);
    const requestSignature = JSON.stringify({
      op: "farm_animal_place",
      playerId,
      itemId,
      cellKeys,
      expectedVersion,
    });

    return this.withTransaction(async (client) => {
      await this.ensurePlayerStateWithClient(client, playerId);
      await this.lockIdempotency(client, idempotencyKey);
      const existing = await this.findStoredTransaction(client, idempotencyKey);
      if (existing) return this.duplicateResult(existing, requestSignature);

      const inventory = await this.getInventoryWithClient(client, playerId, true);
      const slotResult = await client.query(
        "select * from player_inventory where player_id=$1 and item_id=$2 for update",
        [playerId, itemId]
      );
      const slot = slotResult.rows[0];

      const farmResult = await client.query(
        "select * from player_farm_state where player_id=$1 for update",
        [playerId]
      );
      const farmRow = farmResult.rows[0];
      const currentFarm = {
        ...(farmRow.state_json || {}),
        version: toInt(farmRow.version, 1),
        updatedAt: new Date(farmRow.updated_at).toISOString(),
      };

      if (currentFarm.version !== expectedVersion) {
        return {
          ok: false,
          error: "FARM_STATE_CONFLICT",
          inventory,
          farm_state: currentFarm,
        };
      }
      if (!slot || toInt(slot.quantity, 0) < 1) {
        return {
          ok: false,
          error: "INSUFFICIENT_ITEM",
          inventory,
          farm_state: currentFarm,
        };
      }

      const prepared = prepareAnimalPlacement(currentFarm, { itemId, cellKeys }, rule);
      if (!prepared.ok) {
        return {
          ok: false,
          error: prepared.error,
          inventory,
          farm_state: currentFarm,
        };
      }

      const nextQuantity = toInt(slot.quantity, 0) - 1;
      if (nextQuantity === 0) {
        await client.query(
          "delete from player_inventory where player_id=$1 and item_id=$2",
          [playerId, itemId]
        );
      } else {
        await client.query(
          "update player_inventory set quantity=$3,updated_at=now() where player_id=$1 and item_id=$2",
          [playerId, itemId, nextQuantity]
        );
      }
      await client.query(
        `update player_inventory_meta
         set version=version+1,updated_at=now()
         where player_id=$1`,
        [playerId]
      );

      const storedState = { ...prepared.farmState };
      delete storedState.version;
      delete storedState.updatedAt;
      const updatedFarmResult = await client.query(
        `update player_farm_state
         set version=version+1,state_json=$2::jsonb,updated_at=now()
         where player_id=$1 returning *`,
        [playerId, JSON.stringify(storedState)]
      );
      const updatedFarmRow = updatedFarmResult.rows[0];
      const inventoryAfter = await this.getInventoryWithClient(client, playerId);
      const farmStateAfter = {
        ...(updatedFarmRow.state_json || {}),
        version: toInt(updatedFarmRow.version, 1),
        updatedAt: new Date(updatedFarmRow.updated_at).toISOString(),
      };
      const response = {
        ok: true,
        duplicate: false,
        inventory: inventoryAfter,
        farm_state: farmStateAfter,
        animal: prepared.animal,
      };
      const transaction = {
        id: makeId("fatx"),
        playerId,
        type: "farm_animal_place",
        ref: prepared.animal.instanceId,
        idempotencyKey,
        requestSignature,
        itemId,
        quantityDelta: -1,
        cellKeys,
        animal: prepared.animal,
        createdAt: nowISO(),
      };
      await this.insertTransaction(client, transaction, response);
      return { ...response, transaction };
    });
  }

  async lockDailyLimit(client, playerId, limitKey, currentPeriod, maxCount) {
    await client.query(
      `insert into player_daily_limits
       (player_id,limit_key,period_key,used_count,max_count,version)
       values ($1,$2,$3,0,$4,1)
       on conflict (player_id,limit_key,period_key) do nothing`,
      [playerId, limitKey, currentPeriod, maxCount]
    );
    const result = await client.query(
      `select * from player_daily_limits
       where player_id=$1 and limit_key=$2 and period_key=$3 for update`,
      [playerId, limitKey, currentPeriod]
    );
    const row = result.rows[0];
    return {
      limitKey: row.limit_key,
      periodKey: row.period_key,
      used: toInt(row.used_count, 0),
      maxCount: toInt(row.max_count, maxCount),
      remaining: Math.max(0, toInt(row.max_count, maxCount) - toInt(row.used_count, 0)),
      updatedAt: new Date(row.updated_at).toISOString(),
    };
  }

  async getDailyLimitsWithClient(client, playerId, requestedPeriod = periodKey()) {
    const result = await client.query(
      `select * from player_daily_limits where player_id=$1 and period_key=$2 order by limit_key`,
      [playerId, requestedPeriod]
    );
    const output = { version: 1, limits: {}, updatedAt: nowISO() };
    for (const row of result.rows) {
      output.version = Math.max(output.version, toInt(row.version, 1));
      output.updatedAt = new Date(row.updated_at).toISOString();
      output.limits[row.limit_key] = {
        limitKey: row.limit_key,
        periodKey: row.period_key,
        used: toInt(row.used_count, 0),
        maxCount: toInt(row.max_count, 10),
        remaining: Math.max(0, toInt(row.max_count, 10) - toInt(row.used_count, 0)),
        updatedAt: new Date(row.updated_at).toISOString(),
      };
    }
    return output;
  }

  async getDailyLimits(playerId) {
    return this.withTransaction(async (client) => {
      await this.ensurePlayerStateWithClient(client, playerId);
      return this.getDailyLimitsWithClient(client, playerId);
    });
  }

  async setDailyLimits(playerId, dailyLimits) {
    return this.withTransaction(async (client) => {
      await this.ensurePlayerStateWithClient(client, playerId);
      const incoming = dailyLimits || {};
      const limits = incoming.limits && typeof incoming.limits === "object" ? incoming.limits : {};
      await client.query("delete from player_daily_limits where player_id=$1", [playerId]);
      for (const [fallbackKey, raw] of Object.entries(limits)) {
        const key = String(raw.limitKey || fallbackKey).trim();
        if (!key) continue;
        const currentPeriod = String(raw.periodKey || periodKey());
        const maxCount = Math.max(1, toInt(raw.maxCount, 10));
        const used = Math.max(0, Math.min(maxCount, toInt(raw.used, 0)));
        await client.query(
          `insert into player_daily_limits
           (player_id,limit_key,period_key,used_count,max_count,version,updated_at)
           values ($1,$2,$3,$4,$5,$6,now())`,
          [playerId, key, currentPeriod, used, maxCount, toInt(incoming.version, 1)]
        );
      }
      await this.ensurePlayerStateWithClient(client, playerId);
      return this.getDailyLimitsWithClient(client, playerId);
    });
  }

  async consumeDailyLimit(playerId, limitKey, amount, options = {}) {
    return this.withTransaction(async (client) => {
      await this.ensurePlayerStateWithClient(client, playerId);
      const key = String(limitKey || "").trim();
      if (!key) return { ok: false, error: "MISSING_LIMIT_KEY" };
      const consumeAmount = Math.max(1, toInt(amount, 1));
      const maxCount = Math.max(1, toInt(options.maxCount, 10));
      const currentPeriod = String(options.periodKey || periodKey());
      const idempotencyKey = String(options.idempotencyKey || "").trim();
      const requestSignature = JSON.stringify({ op: "daily_limit", playerId, key, consumeAmount, maxCount, currentPeriod });
      await this.lockIdempotency(client, idempotencyKey);
      const existing = await this.findStoredTransaction(client, idempotencyKey);
      if (existing) return this.duplicateResult(existing, requestSignature);

      let limit = await this.lockDailyLimit(client, playerId, key, currentPeriod, maxCount);
      if (limit.used + consumeAmount > limit.maxCount) {
        return { ok: false, error: "DAILY_LIMIT_EXCEEDED", daily_limits: await this.getDailyLimitsWithClient(client, playerId, currentPeriod), limit };
      }
      await client.query(
        `update player_daily_limits set used_count=used_count+$4,max_count=$5,updated_at=now()
         where player_id=$1 and limit_key=$2 and period_key=$3`,
        [playerId, key, currentPeriod, consumeAmount, maxCount]
      );
      const dailyLimits = await this.getDailyLimitsWithClient(client, playerId, currentPeriod);
      limit = dailyLimits.limits[key];
      const transaction = {
        id: makeId("ltx"), playerId, type: String(options.type || `${key}_daily_limit`).trim(),
        ref: options.ref || key, idempotencyKey, requestSignature,
        limitKey: key, amount: consumeAmount, periodKey: currentPeriod,
        limitAfter: { ...limit }, dailyLimitsAfter: clone(dailyLimits), createdAt: nowISO(),
      };
      const response = { ok: true, daily_limits: dailyLimits, limit, duplicate: false };
      await this.insertTransaction(client, transaction, response);
      return { ...response, transaction };
    });
  }

  async deletePlayer(playerId) {
    const result = await this.pool.query("delete from game_players where id=$1", [playerId]);
    return result.rowCount > 0;
  }
}

function createPostgresStore(options) {
  return new PostgresStore(options);
}

module.exports = {
  PostgresStore,
  createPostgresStore,
};
