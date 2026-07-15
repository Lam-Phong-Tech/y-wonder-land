const fs = require("fs");
const os = require("os");
const path = require("path");
const { JsonStore } = require("./store");
const { resolveAnimalPlacementRule } = require("./shopCatalog");

function assert(condition, message) {
  if (!condition) throw new Error(message);
}

function quantity(inventory, itemId) {
  const slot = inventory.slots.find((entry) => entry.itemId === itemId);
  return slot ? slot.quantity : 0;
}

function penItems(count, startX = 0) {
  return Array.from({ length: count }, (_, index) => ({
    cellKey: `${startX + index * 8}_0`,
    itemName: "Chuồng",
    fx: 1,
    fy: 1,
  }));
}

function placement(store, playerId, itemId, cellKeys, expectedVersion, key) {
  const rule = resolveAnimalPlacementRule(itemId);
  assert(rule.ok, `missing animal rule for ${itemId}`);
  return store.placeFarmAnimal(
    playerId,
    { itemId, cellKeys, rule },
    { expectedVersion, idempotencyKey: key }
  );
}

function main() {
  const tempDir = fs.mkdtempSync(path.join(os.tmpdir(), "yw-farm-animal-"));
  const dataPath = path.join(tempDir, "data.json");
  const store = new JsonStore(dataPath);
  const playerId = "player_farm_animal_test";

  try {
    store.ensurePlayerState(playerId);
    store.setInventory(playerId, {
      version: 1,
      maxSlots: 50,
      slots: [
        { itemId: "rabbit_01", quantity: 2 },
        { itemId: "cow_01", quantity: 1 },
      ],
    });
    store.setFarmState(playerId, {
      version: 1,
      snapshot_schema: 1,
      build_state_json: JSON.stringify({ items: penItems(18), animals: [] }),
      placed_tiles_json: JSON.stringify({ tiles: [] }),
      farm_tiles_json: JSON.stringify({ tiles: [] }),
      animal_state_json: JSON.stringify({ animals: [] }),
      legacy_content_score: 18,
    });

    const first = placement(store, playerId, "rabbit_01", ["0_0"], 1, "animal-place-1");
    assert(first.ok && !first.duplicate, "first placement should succeed");
    assert(quantity(first.inventory, "rabbit_01") === 1, "first placement must consume one rabbit");
    assert(first.farm_state.version === 2, "first placement must advance farm version");
    const firstBuild = JSON.parse(first.farm_state.build_state_json);
    assert(firstBuild.animals.length === 1, "first placement must append one animal");
    assert(firstBuild.animals[0].instanceId, "server must create a stable animal instance id");
    assert(firstBuild.animals[0].harvestsRemaining === 2, "server must use rabbit harvest rules");

    const duplicate = placement(store, playerId, "rabbit_01", ["0_0"], 1, "animal-place-1");
    assert(duplicate.ok && duplicate.duplicate, "same idempotency request should be a duplicate success");
    assert(quantity(duplicate.inventory, "rabbit_01") === 1, "duplicate must not consume another rabbit");
    assert(JSON.parse(duplicate.farm_state.build_state_json).animals.length === 1,
      "duplicate must not append another animal");

    const keyConflict = placement(store, playerId, "rabbit_01", ["8_0"], 2, "animal-place-1");
    assert(!keyConflict.ok && keyConflict.error === "IDEMPOTENCY_CONFLICT",
      "reusing a key with another request must fail");

    const stale = placement(store, playerId, "rabbit_01", ["8_0"], 1, "animal-place-stale");
    assert(!stale.ok && stale.error === "FARM_STATE_CONFLICT",
      "stale farm version must fail without consuming inventory");
    assert(quantity(store.getInventory(playerId), "rabbit_01") === 1,
      "stale request must not consume inventory");

    const second = placement(store, playerId, "rabbit_01", ["8_0"], 2, "animal-place-2");
    assert(second.ok && quantity(second.inventory, "rabbit_01") === 0,
      "second valid placement should consume the last rabbit");
    assert(JSON.parse(second.farm_state.build_state_json).animals.length === 2,
      "second valid placement should append exactly one animal");

    const noItem = placement(store, playerId, "rabbit_01", ["16_0"], 3, "animal-place-no-item");
    assert(!noItem.ok && noItem.error === "INSUFFICIENT_ITEM",
      "placement without inventory must fail");
    assert(noItem.farm_state.version === 3, "failed placement must not advance farm version");

    store.setInventory(playerId, {
      version: 4,
      maxSlots: 50,
      slots: [
        { itemId: "rabbit_01", quantity: 1 },
        { itemId: "cow_01", quantity: 1 },
      ],
    });
    const occupied = placement(store, playerId, "rabbit_01", ["0_0"], 3, "animal-place-occupied");
    assert(!occupied.ok && occupied.error === "PEN_CELL_OCCUPIED",
      "occupied pen cell must be rejected");
    assert(quantity(store.getInventory(playerId), "rabbit_01") === 1,
      "occupied cell must not consume inventory");

    const invalidCell = placement(store, playerId, "rabbit_01", ["999_999"], 3, "animal-place-invalid");
    assert(!invalidCell.ok && invalidCell.error === "INVALID_PEN_CELL",
      "non-pen cell must be rejected");

    const wrongCowSlots = placement(store, playerId, "cow_01", ["16_0"], 3, "animal-place-cow-slots");
    assert(!wrongCowSlots.ok && wrongCowSlots.error === "INVALID_PEN_SLOT_COUNT",
      "large animal must require its authoritative slot count");
    assert(quantity(store.getInventory(playerId), "cow_01") === 1,
      "invalid slot count must not consume the cow");

    console.log("[farm-animal-placement] PASS: inventory/farm atomicity, CAS, rules, occupied cells, and idempotency work.");
  } finally {
    fs.rmSync(tempDir, { recursive: true, force: true });
  }
}

main();
