const fs = require("fs");
const path = require("path");
const { Pool } = require("pg");

function nowISO() {
  return new Date().toISOString();
}

function toInt(value, fallback = 0) {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? Math.trunc(parsed) : fallback;
}

function normalizeIdentity(value) {
  return String(value || "").trim().toLowerCase();
}

function asObject(value) {
  return value && typeof value === "object" && !Array.isArray(value) ? value : {};
}

function createPool() {
  const connectionString = process.env.DATABASE_URL || process.env.POSTGRES_URL || "";
  if (!connectionString) throw new Error("DATABASE_URL or POSTGRES_URL is required.");
  const sslMode = String(process.env.PGSSL || "").toLowerCase();
  return new Pool({
    connectionString,
    max: 2,
    ssl: sslMode === "require" ? { rejectUnauthorized: false } : undefined,
  });
}

function validateAccounts(users) {
  const usernames = new Map();
  const emails = new Map();
  const errors = [];
  for (const user of users) {
    const username = normalizeIdentity(user.username);
    const email = normalizeIdentity(user.email);
    if (!user.id || !username || !user.password_hash) {
      errors.push(`Invalid local account id=${user.id || "<missing>"}`);
      continue;
    }
    if (usernames.has(username)) errors.push(`Duplicate username: ${user.username}`);
    else usernames.set(username, user.id);
    if (email) {
      if (emails.has(email)) errors.push(`Duplicate email: ${user.email}`);
      else emails.set(email, user.id);
    }
  }
  if (errors.length > 0) throw new Error(errors.join("; "));
}

function collectPlayerIds(db) {
  const ids = new Set();
  for (const user of db.users) if (user && user.id) ids.add(user.id);
  for (const id of Object.keys(db.players)) ids.add(id);
  for (const section of [db.profiles, db.economies, db.inventories, db.farmStates, db.dailyLimits]) {
    for (const id of Object.keys(section)) ids.add(id);
  }
  return ids;
}

function makePlayer(db, playerId) {
  const existing = db.players[playerId] || {};
  const user = db.users.find((entry) => entry.id === playerId) || {};
  const profile = db.profiles[playerId] || {};
  return {
    id: playerId,
    webUserId: existing.webUserId || "",
    username: existing.username || user.username || profile.name || playerId,
    displayName: existing.displayName || profile.name || user.username || "Player",
    authSource: existing.authSource || (user.id ? "local" : "imported"),
    createdAt: existing.createdAt || user.created_at || profile.createdAt || nowISO(),
    updatedAt: existing.updatedAt || user.updated_at || profile.updatedAt || nowISO(),
  };
}

async function upsertPlayer(client, player) {
  await client.query(
    `insert into game_players
     (id,web_user_id,username,display_name,auth_source,created_at,updated_at)
     values ($1,$2,$3,$4,$5,$6,$7)
     on conflict (id) do update set
       web_user_id=excluded.web_user_id,username=excluded.username,
       display_name=excluded.display_name,auth_source=excluded.auth_source,
       updated_at=excluded.updated_at`,
    [
      player.id,
      player.webUserId || null,
      player.username,
      player.displayName,
      player.authSource,
      player.createdAt,
      player.updatedAt,
    ]
  );
}

async function upsertAccount(client, user) {
  await client.query(
    `insert into game_accounts
     (id,player_id,username,email,phone,password_hash,status,soft_deleted,created_at,updated_at)
     values ($1,$1,$2,$3,$4,$5,$6,$7,$8,$9)
     on conflict (id) do update set
       username=excluded.username,email=excluded.email,phone=excluded.phone,
       password_hash=excluded.password_hash,status=excluded.status,
       soft_deleted=excluded.soft_deleted,updated_at=excluded.updated_at`,
    [
      user.id,
      user.username,
      user.email || "",
      user.phone || "",
      user.password_hash,
      user.status || "active",
      Boolean(user.soft_deleted),
      user.created_at || nowISO(),
      user.updated_at || nowISO(),
    ]
  );
}

async function importProfile(client, playerId, profile) {
  const value = { ...profile };
  await client.query(
    `insert into player_profiles
     (player_id,version,name,gender,avatar_id,level,exp,character_created,
      tutorial_completed,profile_json,created_at,updated_at)
     values ($1,$2,$3,$4,$5,$6,$7,$8,$9,$10::jsonb,$11,$12)
     on conflict (player_id) do update set
       version=excluded.version,name=excluded.name,gender=excluded.gender,
       avatar_id=excluded.avatar_id,level=excluded.level,exp=excluded.exp,
       character_created=excluded.character_created,
       tutorial_completed=excluded.tutorial_completed,
       profile_json=excluded.profile_json,updated_at=excluded.updated_at`,
    [
      playerId,
      toInt(value.version, 1),
      value.name || "Player",
      value.gender || "male",
      value.avatarId || value.avatar_id || "",
      toInt(value.level, 1),
      Number(value.exp) || 0,
      Boolean(value.characterCreated ?? value.character_created),
      Boolean(value.tutorialCompleted ?? value.tutorial_completed),
      JSON.stringify(value),
      value.createdAt || value.created_at || nowISO(),
      value.updatedAt || value.updated_at || nowISO(),
    ]
  );
}

async function importEconomy(client, playerId, economy) {
  await client.query(
    `insert into player_economy (player_id,version,pos,updated_at)
     values ($1,$2,$3,$4)
     on conflict (player_id) do update set
       version=excluded.version,pos=excluded.pos,updated_at=excluded.updated_at`,
    [playerId, toInt(economy.version, 1), toInt(economy.pos, 5000), economy.updatedAt || nowISO()]
  );
}

async function importInventory(client, playerId, inventory) {
  const slots = Array.isArray(inventory.slots) ? inventory.slots : [];
  const maxSlots = Math.max(toInt(inventory.maxSlots ?? inventory.max_slots, 50), slots.length);
  await client.query(
    `insert into player_inventory_meta (player_id,version,max_slots,updated_at)
     values ($1,$2,$3,$4)
     on conflict (player_id) do update set
       version=excluded.version,max_slots=excluded.max_slots,updated_at=excluded.updated_at`,
    [playerId, toInt(inventory.version, 1), maxSlots, inventory.updatedAt || nowISO()]
  );
  await client.query("delete from player_inventory where player_id=$1", [playerId]);
  for (const slot of slots) {
    const itemId = String(slot.itemId || slot.item_id || "").trim();
    const quantity = Math.max(0, toInt(slot.quantity, 0));
    if (!itemId || quantity === 0) continue;
    await client.query(
      `insert into player_inventory
       (player_id,item_id,quantity,slot_tab,equipped,durability,updated_at)
       values ($1,$2,$3,$4,$5,$6,$7)`,
      [
        playerId,
        itemId,
        quantity,
        slot.slotTab || slot.slot_tab || "",
        Boolean(slot.equipped),
        slot.durability == null ? null : toInt(slot.durability, 0),
        inventory.updatedAt || nowISO(),
      ]
    );
  }
}

async function importFarmState(client, playerId, farmState) {
  await client.query(
    `insert into player_farm_state (player_id,version,state_json,updated_at)
     values ($1,$2,$3::jsonb,$4)
     on conflict (player_id) do update set
       version=excluded.version,state_json=excluded.state_json,updated_at=excluded.updated_at`,
    [playerId, toInt(farmState.version, 1), JSON.stringify(farmState), farmState.updatedAt || nowISO()]
  );
}

async function importDailyLimits(client, playerId, dailyLimits) {
  await client.query("delete from player_daily_limits where player_id=$1", [playerId]);
  const limits = asObject(dailyLimits.limits);
  for (const [fallbackKey, limit] of Object.entries(limits)) {
    const key = String(limit.limitKey || fallbackKey).trim();
    const maxCount = Math.max(1, toInt(limit.maxCount, 10));
    const used = Math.max(0, Math.min(maxCount, toInt(limit.used, 0)));
    await client.query(
      `insert into player_daily_limits
       (player_id,limit_key,period_key,used_count,max_count,version,updated_at)
       values ($1,$2,$3,$4,$5,$6,$7)`,
      [
        playerId,
        key,
        limit.periodKey || new Date().toISOString().slice(0, 10),
        used,
        maxCount,
        toInt(dailyLimits.version, 1),
        limit.updatedAt || dailyLimits.updatedAt || nowISO(),
      ]
    );
  }
}

async function importTransactions(client, transactions) {
  for (const transaction of transactions) {
    if (!transaction || !transaction.id || !transaction.playerId) continue;
    const economyAfter = transaction.economyAfter
      ? {
        version: toInt(transaction.economyAfter.version, 1),
        pos: toInt(transaction.economyAfter.pos, 5000),
        updatedAt: transaction.economyAfter.updatedAt || transaction.createdAt || nowISO(),
      }
      : transaction.economyAfter;
    const details = { ...transaction };
    delete details.deltaUpos;
    if (details.economyAfter) details.economyAfter = economyAfter;
    const result = {
      economy: economyAfter,
      inventory: transaction.inventoryAfter,
      daily_limits: transaction.dailyLimitsAfter,
      limit: transaction.limitAfter,
      rewards: transaction.rewards,
    };
    await client.query(
      `insert into game_transactions
       (id,player_id,type,ref,idempotency_key,request_signature,delta_pos,
        item_id,quantity_delta,details_json,result_json,created_at)
       values ($1,$2,$3,$4,$5,$6,$7,$8,$9,$10::jsonb,$11::jsonb,$12)
       on conflict (id) do nothing`,
      [
        transaction.id,
        transaction.playerId,
        transaction.type || "imported",
        transaction.ref || "",
        transaction.idempotencyKey || null,
        transaction.requestSignature || "",
        toInt(transaction.deltaPos, 0),
        transaction.itemId || null,
        transaction.quantityDelta == null ? null : toInt(transaction.quantityDelta, 0),
        JSON.stringify(details),
        JSON.stringify(result),
        transaction.createdAt || nowISO(),
      ]
    );
  }
}

async function main() {
  const dataPath = path.resolve(process.env.YW_IMPORT_JSON_PATH || process.env.YW_DATA_PATH || path.join(__dirname, "..", "data.json"));
  if (!fs.existsSync(dataPath)) throw new Error(`JSON source not found: ${dataPath}`);
  const source = JSON.parse(fs.readFileSync(dataPath, "utf8"));
  const db = {
    users: Array.isArray(source.users) ? source.users : [],
    players: asObject(source.players),
    profiles: asObject(source.profiles),
    economies: asObject(source.economies),
    inventories: asObject(source.inventories),
    farmStates: asObject(source.farmStates),
    dailyLimits: asObject(source.dailyLimits),
    transactions: Array.isArray(source.transactions) ? source.transactions : [],
  };
  validateAccounts(db.users);
  const playerIds = collectPlayerIds(db);
  console.log(`[db:import-json] validated users=${db.users.length} players=${playerIds.size} transactions=${db.transactions.length}`);
  if (process.env.IMPORT_DRY_RUN === "true") {
    console.log("[db:import-json] dry run complete; PostgreSQL was not changed");
    return;
  }

  const pool = createPool();
  const client = await pool.connect();
  try {
    await client.query("begin");
    for (const playerId of playerIds) await upsertPlayer(client, makePlayer(db, playerId));
    for (const user of db.users) await upsertAccount(client, user);
    for (const [playerId, value] of Object.entries(db.profiles)) await importProfile(client, playerId, value);
    for (const [playerId, value] of Object.entries(db.economies)) await importEconomy(client, playerId, value);
    for (const [playerId, value] of Object.entries(db.inventories)) await importInventory(client, playerId, value);
    for (const [playerId, value] of Object.entries(db.farmStates)) await importFarmState(client, playerId, value);
    for (const [playerId, value] of Object.entries(db.dailyLimits)) await importDailyLimits(client, playerId, value);
    await importTransactions(client, db.transactions);
    await client.query("commit");
    console.log("[db:import-json] import committed");
  } catch (error) {
    await client.query("rollback");
    throw error;
  } finally {
    client.release();
    await pool.end();
  }
}

main().catch((error) => {
  console.error(`[db:import-json] FAIL: ${error.message}`);
  process.exitCode = 1;
});
