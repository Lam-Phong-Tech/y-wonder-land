const fs = require("fs");
const crypto = require("crypto");
const path = require("path");
const { Pool } = require("pg");
const { PostgresStore } = require("./postgresStore");
const { resolveAnimalPlacementRule } = require("./shopCatalog");

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
  const migrationSql = [
    "001_initial.sql",
    "002_browser_auth_requests.sql",
    "003_active_player_sessions.sql",
    "004_single_point_currency.sql",
    "005_web_topup_point_remainder.sql",
    "006_point_wallet_reservations.sql",
  ]
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
      store.applyEconomyDelta(userId, { pos: 125 }, { idempotencyKey: economyKey, type: "smoke" }),
      store.applyEconomyDelta(userId, { pos: 125 }, { idempotencyKey: economyKey, type: "smoke" }),
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

    const farmSaved = await store.compareAndSetFarmState(
      userId,
      1,
      { marker: suffix, tiles: [{ id: "tile-1" }] }
    );
    assert(farmSaved.ok && farmSaved.farm_state.version === 2,
      "Farm compare-and-set did not advance the revision.");
    const staleFarm = await store.compareAndSetFarmState(
      userId,
      1,
      { marker: "stale-overwrite" }
    );
    assert(!staleFarm.ok && staleFarm.error === "FARM_STATE_CONFLICT" && staleFarm.farm_state.marker === suffix,
      "Stale farm compare-and-set overwrote PostgreSQL state.");

    const inventoryBeforeAnimal = await store.getInventory(userId);
    await store.setInventory(userId, {
      ...inventoryBeforeAnimal,
      version: inventoryBeforeAnimal.version + 1,
      slots: [
        ...inventoryBeforeAnimal.slots,
        { itemId: "rabbit_01", quantity: 2 },
      ],
    });
    const farmReadyForAnimal = await store.compareAndSetFarmState(
      userId,
      2,
      {
        marker: suffix,
        snapshot_schema: 1,
        build_state_json: JSON.stringify({
          items: [
            { cellKey: "0_0", itemName: "Chuong", fx: 1, fy: 1 },
            { cellKey: "8_0", itemName: "Chuong", fx: 1, fy: 1 },
          ],
          animals: [],
        }),
        placed_tiles_json: JSON.stringify({ tiles: [] }),
        farm_tiles_json: JSON.stringify({ tiles: [] }),
        animal_state_json: JSON.stringify({ animals: [] }),
      }
    );
    assert(farmReadyForAnimal.ok && farmReadyForAnimal.farm_state.version === 3,
      "Farm was not prepared for atomic animal placement.");

    const rabbitRule = resolveAnimalPlacementRule("rabbit_01");
    assert(rabbitRule.ok, "Rabbit placement rule is missing.");
    const animalKey = `pg-animal-${suffix}`;
    const animalPlaced = await store.placeFarmAnimal(
      userId,
      { itemId: "rabbit_01", cellKeys: ["0_0"], rule: rabbitRule },
      { expectedVersion: 3, idempotencyKey: animalKey }
    );
    const animalDuplicate = await store.placeFarmAnimal(
      userId,
      { itemId: "rabbit_01", cellKeys: ["0_0"], rule: rabbitRule },
      { expectedVersion: 3, idempotencyKey: animalKey }
    );
    assert(animalPlaced.ok && !animalPlaced.duplicate
      && itemQuantity(animalPlaced.inventory, "rabbit_01") === 1,
    "Atomic animal placement did not consume exactly one rabbit.");
    assert(animalDuplicate.ok && animalDuplicate.duplicate
      && itemQuantity(animalDuplicate.inventory, "rabbit_01") === 1,
    "Animal placement retry was not idempotent.");
    assert(JSON.parse(animalPlaced.farm_state.build_state_json).animals.length === 1,
      "Atomic animal placement did not append exactly one animal.");

    const staleAnimal = await store.placeFarmAnimal(
      userId,
      { itemId: "rabbit_01", cellKeys: ["8_0"], rule: rabbitRule },
      { expectedVersion: 3, idempotencyKey: `pg-animal-stale-${suffix}` }
    );
    assert(!staleAnimal.ok && staleAnimal.error === "FARM_STATE_CONFLICT"
      && itemQuantity(staleAnimal.inventory, "rabbit_01") === 1,
    "Stale animal placement consumed inventory or bypassed farm CAS.");

    await store.setActivePlayerSession(userId, `session-a-${suffix}`);
    assert(await store.isActivePlayerSession(userId, `session-a-${suffix}`),
      "Active player session was not persisted.");
    await store.setActivePlayerSession(userId, `session-b-${suffix}`);
    assert(!await store.isActivePlayerSession(userId, `session-a-${suffix}`),
      "Previous player session remained active after rotation.");
    assert(await store.isActivePlayerSession(userId, `session-b-${suffix}`),
      "Replacement player session was not active.");

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

    const topupIdentity = {
      id: `web-topup-${suffix}`,
      username: `topup-${suffix}@example.test`,
      displayName: "Postgres Topup Smoke",
      authSource: "web",
    };
    const topupPlayer = await store.getOrCreatePlayerForWebUser(topupIdentity);
    const topupPlayerId = topupPlayer.id;
    const mismatchedTopup = await store.creditWebTopup(topupIdentity, 1_000_000, {
      pointAmount: "1.000000",
      transactionId: `topup-wrong-player-${suffix}`,
      expectedPlayerId: `wrong-${topupPlayerId}`,
      occurredAt: new Date().toISOString(),
      source: "postgres-smoke",
    });
    assert(!mismatchedTopup.ok && mismatchedTopup.error === "GAME_POINT_IDENTITY_MISMATCH",
      "PostgreSQL web top-up accepted the wrong pinned player.");
    const economyAfterMismatch = await store.getEconomy(topupPlayerId);
    assert(economyAfterMismatch.pos === 5000,
      "PostgreSQL identity mismatch changed the authoritative Point balance.");
    const topupFirst = await store.creditWebTopup(topupIdentity, 750_500_000, {
      pointAmount: "750.500000",
      transactionId: `topup-first-${suffix}`,
      expectedPlayerId: topupPlayerId,
      occurredAt: new Date().toISOString(),
      source: "postgres-smoke",
    });
    assert(topupFirst.ok && !topupFirst.duplicate && topupFirst.economy.pos === 5750,
      "PostgreSQL decimal web top-up did not credit the whole Point portion.");
    const topupDuplicate = await store.creditWebTopup(topupIdentity, 750_500_000, {
      pointAmount: "750.500000",
      transactionId: `topup-first-${suffix}`,
      expectedPlayerId: topupPlayerId,
      occurredAt: topupFirst.transaction.occurredAt,
      source: "postgres-smoke",
    });
    assert(topupDuplicate.ok && topupDuplicate.duplicate && topupDuplicate.economy.pos === 5750,
      "PostgreSQL web top-up retry was not idempotent.");
    const topupCarry = await store.creditWebTopup(topupIdentity, 500_000, {
      pointAmount: "0.500000",
      transactionId: `topup-carry-${suffix}`,
      expectedPlayerId: topupPlayerId,
      occurredAt: new Date().toISOString(),
      source: "postgres-smoke",
    });
    assert(topupCarry.ok && topupCarry.economy.pos === 5751,
      "PostgreSQL web top-up did not carry the fractional Point remainder exactly.");
    assert(topupFirst.player.id === topupPlayerId,
      "PostgreSQL web top-up credited a player other than the pinned identity.");
    const remainder = await pool.query(
      "select web_point_micros_remainder from player_economy where player_id=$1",
      [topupPlayerId]
    );
    assert(Number(remainder.rows[0].web_point_micros_remainder) === 0,
      "PostgreSQL fractional Point remainder was not settled to zero.");

    const reservationIdentity = {
      id: `pg-reservation-web-${suffix}`,
      username: `reservation_${suffix}@example.test`,
      displayName: "Reservation Smoke",
      authSource: "web",
    };
    const reservationPlayer = await store.getOrCreatePlayerForWebUser(reservationIdentity);
    const releasedReservation = {
      reservationId: `pg-reservation-release-${suffix}`,
      webUserId: reservationIdentity.id,
      expectedPlayerId: reservationPlayer.id,
      pointAmount: 100,
      purpose: "point_to_usdt",
      source: "postgres-smoke",
      occurredAt: new Date().toISOString(),
    };
    const reserveRace = await Promise.all([
      store.applyWebPointReservation("reserve", releasedReservation),
      store.applyWebPointReservation("reserve", releasedReservation),
    ]);
    assert(reserveRace.every((entry) => entry.ok)
      && reserveRace.filter((entry) => entry.duplicate).length === 1
      && reserveRace.every((entry) => entry.economy.pos === 4900),
    "Concurrent PostgreSQL reservation did not debit exactly once.");
    const releaseRace = await Promise.all([
      store.applyWebPointReservation("release", releasedReservation),
      store.applyWebPointReservation("release", releasedReservation),
    ]);
    assert(releaseRace.every((entry) => entry.ok)
      && releaseRace.filter((entry) => entry.duplicate).length === 1
      && releaseRace.every((entry) => entry.economy.pos === 5000),
    "Concurrent PostgreSQL release did not refund exactly once.");

    const capturedReservation = {
      ...releasedReservation,
      reservationId: `pg-reservation-capture-${suffix}`,
      pointAmount: 200,
    };
    const pendingCapture = await store.applyWebPointReservation("reserve", capturedReservation);
    assert(pendingCapture.ok && pendingCapture.economy.pos === 4800,
      "PostgreSQL capture fixture was not reserved.");

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
    const topupReplayAfterRestart = await store.creditWebTopup(topupIdentity, 750_500_000, {
      pointAmount: "750.500000",
      transactionId: `topup-first-${suffix}`,
      expectedPlayerId: topupPlayerId,
      occurredAt: topupFirst.transaction.occurredAt,
      source: "postgres-smoke",
    });
    const captureAfterRestart = await store.applyWebPointReservation("capture", capturedReservation);
    const captureReplayAfterRestart = await store.applyWebPointReservation("capture", capturedReservation);
    const releaseCaptured = await store.applyWebPointReservation("release", capturedReservation);
    assert(restoredProfile.customMarker === suffix, "Profile JSON did not survive pool restart.");
    assert(restoredEconomy.pos === startEconomy.pos + 25, "Economy did not preserve delta plus shop cost.");
    assert(itemQuantity(restoredInventory, "pg_test_item") === 3, "Inventory did not survive pool restart.");
    assert(itemQuantity(restoredInventory, "rabbit_01") === 1,
      "Atomic animal inventory did not survive pool restart.");
    assert(restoredFarm.marker === suffix, "Farm state did not survive pool restart.");
    assert(JSON.parse(restoredFarm.build_state_json).animals.length === 1,
      "Atomic animal farm state did not survive pool restart.");
    assert(restoredLimits.limits.mining.used === 1, "Mining daily limit did not survive pool restart.");
    assert(restoredLimits.limits.fishing.used === 1, "Fishing daily limit did not survive pool restart.");
    assert(browserReplay.error === "BROWSER_AUTH_CONSUMED",
      "Consumed browser auth request did not survive pool restart.");
    assert(topupReplayAfterRestart.ok && topupReplayAfterRestart.duplicate
      && topupReplayAfterRestart.economy.pos === 5751,
    "PostgreSQL web top-up idempotency or remainder did not survive pool restart.");
    assert(captureAfterRestart.ok && !captureAfterRestart.duplicate
      && captureAfterRestart.economy.pos === 4800
      && captureAfterRestart.reservation.status === "CAPTURED",
    "PostgreSQL reservation capture did not survive pool restart.");
    assert(captureReplayAfterRestart.ok && captureReplayAfterRestart.duplicate
      && captureReplayAfterRestart.economy.pos === 4800,
    "PostgreSQL capture replay was not idempotent.");
    assert(!releaseCaptured.ok && releaseCaptured.error === "POINT_RESERVATION_STATE_CONFLICT",
      "Captured PostgreSQL reservation was released.");

    const db = await store.readAll();
    assert(db.users.length === 1, "PostgreSQL admin snapshot is missing the local account.");
    assert(db.transactions.length === 12, "PostgreSQL transaction ledger count is incorrect.");
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
