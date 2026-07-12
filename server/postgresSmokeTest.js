const fs = require("fs");
const crypto = require("crypto");
const path = require("path");
const { Pool } = require("pg");
const { PostgresStore } = require("./postgresStore");

function assert(condition, message) {
  if (!condition) throw new Error(message);
}

function itemQuantity(inventory, itemId) {
  const slot = (inventory.slots || []).find((entry) => entry.itemId === itemId);
  return slot ? Number(slot.quantity) : 0;
}

function makePool(connectionString, schema) {
  const sslMode = String(process.env.PGSSL || "").toLowerCase();
  return new Pool({
    connectionString,
    max: 6,
    options: `-c search_path=${schema},public`,
    ssl: sslMode === "require" ? { rejectUnauthorized: false } : undefined,
  });
}

async function main() {
  const connectionString = process.env.POSTGRES_TEST_DATABASE_URL || "";
  if (!connectionString) {
    throw new Error("POSTGRES_TEST_DATABASE_URL is required; production DATABASE_URL is not accepted for this test.");
  }

  const suffix = `${Date.now()}_${Math.floor(Math.random() * 1e6)}`;
  const schema = `ywtest_${suffix}`;
  const migrationSql = ["001_initial.sql", "002_browser_auth_requests.sql"]
    .map((file) => fs.readFileSync(path.join(__dirname, "migrations", file), "utf8"))
    .join("\n");
  const adminPool = makePool(connectionString, "public");
  let pool;
  let store;
  try {
    await adminPool.query(`create schema "${schema}"`);
    pool = makePool(connectionString, schema);
    await pool.query(migrationSql);
    store = new PostgresStore({ pool });

    const userId = `pg_user_${suffix}`;
    await store.createUser({
      id: userId,
      username: `PgSmoke_${suffix}`,
      email: `pg_${suffix}@example.test`,
      phone: "",
      password_hash: "bcrypt-placeholder-for-store-test",
      created_at: new Date().toISOString(),
      updated_at: new Date().toISOString(),
    });
    await store.setProfile(userId, {
      version: 1,
      name: "Postgres Smoke",
      gender: "male",
      level: 3,
      exp: 12,
      characterCreated: true,
      tutorialCompleted: true,
      customMarker: suffix,
    });

    const startEconomy = await store.getEconomy(userId);
    const economyKey = `pg-economy-${suffix}`;
    const economyResults = await Promise.all([
      store.applyEconomyDelta(userId, { pos: 125, upos: 0 }, { idempotencyKey: economyKey, type: "smoke" }),
      store.applyEconomyDelta(userId, { pos: 125, upos: 0 }, { idempotencyKey: economyKey, type: "smoke" }),
    ]);
    const economyAfter = await store.getEconomy(userId);
    assert(economyAfter.pos === startEconomy.pos + 125, "Concurrent economy retry applied more than once.");
    assert(economyResults.some((entry) => entry.duplicate === true), "Economy retry was not marked duplicate.");

    const inventoryKey = `pg-inventory-${suffix}`;
    await store.adjustInventoryItem(userId, "pg_test_item", 3, { idempotencyKey: inventoryKey });
    const duplicateInventory = await store.adjustInventoryItem(userId, "pg_test_item", 3, { idempotencyKey: inventoryKey });
    assert(duplicateInventory.duplicate === true, "Inventory retry was not marked duplicate.");

    const shopKey = `pg-shop-${suffix}`;
    const shopOffer = { shopId: "Shop_ItemShop", mode: "buy", itemId: "fertilizer_01", unitPrice: 50 };
    const shop = await store.transactShop(userId, shopOffer, 2, { idempotencyKey: shopKey });
    const shopDuplicate = await store.transactShop(userId, shopOffer, 2, { idempotencyKey: shopKey });
    assert(shop.ok && shopDuplicate.duplicate === true, "Shop transaction was not idempotent.");

    const harvestKey = `pg-resource-${suffix}`;
    const harvested = await store.applyResourceHarvest(
      userId,
      [{ itemId: "stone_01", quantity: 2, displayName: "Stone", kind: "rock" }],
      { idempotencyKey: harvestKey, dailyLimit: { key: "mining", amount: 1, maxCount: 10 } }
    );
    assert(harvested.ok && harvested.limit.used === 1, "Resource harvest did not consume mining limit atomically.");

    const dailyKey = `pg-daily-${suffix}`;
    const daily = await store.consumeDailyLimit(userId, "fishing", 1, { idempotencyKey: dailyKey, maxCount: 10 });
    const dailyDuplicate = await store.consumeDailyLimit(userId, "fishing", 1, { idempotencyKey: dailyKey, maxCount: 10 });
    assert(daily.ok && dailyDuplicate.duplicate === true, "Daily limit retry was not idempotent.");

    await store.setFarmState(userId, { version: 2, marker: suffix, tiles: [{ id: "tile-1" }] });

    const browserRequestHash = crypto.createHash("sha256").update(`browser-${suffix}`).digest("hex");
    const browserChallenge = crypto.createHash("sha256").update(`verifier-${suffix}`).digest("base64url");
    const browserCreated = await store.createBrowserAuthRequest({
      requestIdHash: browserRequestHash,
      pkceChallenge: browserChallenge,
      intent: "login",
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      createdAt: new Date().toISOString(),
    });
    assert(browserCreated.ok, "PostgreSQL browser auth request was not created.");
    const browserApproved = await store.approveBrowserAuthRequest(browserRequestHash, {
      id: `web-${suffix}`,
      username: `browser-${suffix}`,
      displayName: "Browser Postgres Smoke",
      authSource: "web-browser",
    });
    assert(browserApproved.ok, "PostgreSQL browser auth request was not approved.");
    const wrongBrowserExchange = await store.exchangeBrowserAuthRequest(browserRequestHash, "wrong-challenge");
    assert(wrongBrowserExchange.error === "BROWSER_AUTH_PKCE_MISMATCH",
      "PostgreSQL browser auth accepted a wrong PKCE challenge.");
    const browserExchanged = await store.exchangeBrowserAuthRequest(browserRequestHash, browserChallenge);
    assert(browserExchanged.ok && browserExchanged.request.webUserId === `web-${suffix}`,
      "PostgreSQL browser auth exchange failed.");
    const beforeRestartInventory = await store.getInventory(userId);
    assert(itemQuantity(beforeRestartInventory, "pg_test_item") === 3, "Inventory quantity is incorrect before restart.");
    assert(itemQuantity(beforeRestartInventory, "fertilizer_01") === 2, "Atomic shop item is missing.");
    assert(itemQuantity(beforeRestartInventory, "stone_01") === 2, "Resource reward is missing.");

    await pool.end();
    pool = makePool(connectionString, schema);
    store = new PostgresStore({ pool });
    const restoredProfile = await store.getProfile(userId);
    const restoredEconomy = await store.getEconomy(userId);
    const restoredInventory = await store.getInventory(userId);
    const restoredFarm = await store.getFarmState(userId);
    const restoredLimits = await store.getDailyLimits(userId);
    const browserReplay = await store.exchangeBrowserAuthRequest(browserRequestHash, browserChallenge);
    assert(restoredProfile.customMarker === suffix, "Profile JSON did not survive pool restart.");
    assert(restoredEconomy.pos === startEconomy.pos + 25, "Economy did not preserve delta plus shop cost.");
    assert(itemQuantity(restoredInventory, "pg_test_item") === 3, "Inventory did not survive pool restart.");
    assert(restoredFarm.marker === suffix, "Farm state did not survive pool restart.");
    assert(restoredLimits.limits.mining.used === 1, "Mining daily limit did not survive pool restart.");
    assert(restoredLimits.limits.fishing.used === 1, "Fishing daily limit did not survive pool restart.");
    assert(browserReplay.error === "BROWSER_AUTH_CONSUMED",
      "Consumed browser auth request did not survive pool restart.");

    const db = await store.readAll();
    assert(db.users.length === 1, "PostgreSQL admin snapshot is missing the local account.");
    assert(db.transactions.length === 5, "PostgreSQL transaction ledger count is incorrect.");
    console.log(`[postgres-smoke] PASS schema=${schema}`);
  } finally {
    if (pool) await pool.end().catch(() => {});
    if (process.env.POSTGRES_TEST_KEEP_SCHEMA !== "true") {
      await adminPool.query(`drop schema if exists "${schema}" cascade`).catch(() => {});
    } else {
      console.log(`[postgres-smoke] kept schema=${schema}`);
    }
    await adminPool.end();
  }
}

main().catch((error) => {
  console.error(`[postgres-smoke] FAIL: ${error.message}`);
  process.exitCode = 1;
});
