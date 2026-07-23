const crypto = require("crypto");

const CELL_KEY_PATTERN = /^-?\d+_-?\d+$/;

function clone(value) {
  return value == null ? value : JSON.parse(JSON.stringify(value));
}

function normalizeCellKeys(value) {
  if (!Array.isArray(value)) return [];
  return value.map((entry) => String(entry || "").trim());
}

function normalizeBuildName(value) {
  return String(value || "")
    .trim()
    .toLowerCase()
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/đ/g, "d");
}

function isPenBuildItem(item) {
  return item && normalizeBuildName(item.itemName) === "chuong";
}

function parseBuildState(farmState) {
  if (!farmState || Number(farmState.snapshot_schema) < 1) {
    return { ok: false, error: "FARM_STATE_NOT_READY" };
  }

  const raw = farmState.build_state_json;
  if (typeof raw !== "string" || raw.length === 0) {
    return { ok: false, error: "FARM_STATE_NOT_READY" };
  }

  try {
    const build = JSON.parse(raw);
    if (!build || typeof build !== "object" || !Array.isArray(build.items)) {
      return { ok: false, error: "INVALID_BUILD_STATE" };
    }
    if (!Array.isArray(build.animals)) build.animals = [];
    return { ok: true, build };
  } catch {
    return { ok: false, error: "INVALID_BUILD_STATE" };
  }
}

function prepareAnimalPlacement(currentFarmState, placement, rule, options = {}) {
  const itemId = String(placement && placement.itemId || "").trim();
  const cellKeys = normalizeCellKeys(placement && placement.cellKeys);
  const requiredSlots = Number(rule && rule.penSlots);
  const maxHarvests = Number(rule && rule.maxHarvests);

  if (!itemId || !Number.isInteger(requiredSlots) || requiredSlots < 1 || requiredSlots > 9) {
    return { ok: false, error: "INVALID_ANIMAL_PLACEMENT" };
  }
  if (!Number.isInteger(maxHarvests) || maxHarvests < 1) {
    return { ok: false, error: "INVALID_ANIMAL_RULE" };
  }
  if (cellKeys.length !== requiredSlots) {
    return { ok: false, error: "INVALID_PEN_SLOT_COUNT" };
  }
  if (new Set(cellKeys).size !== cellKeys.length || cellKeys.some((key) => !CELL_KEY_PATTERN.test(key))) {
    return { ok: false, error: "INVALID_PEN_CELL" };
  }

  const parsed = parseBuildState(currentFarmState);
  if (!parsed.ok) return parsed;

  const penCells = new Set(
    parsed.build.items
      .filter(isPenBuildItem)
      .map((item) => String(item.cellKey || "").trim())
      .filter(Boolean)
  );
  if (cellKeys.some((key) => !penCells.has(key))) {
    return { ok: false, error: "INVALID_PEN_CELL" };
  }

  const occupiedCells = new Set();
  for (const animal of parsed.build.animals) {
    for (const key of normalizeCellKeys(animal && animal.cellKeys)) {
      if (key) occupiedCells.add(key);
    }
  }
  if (cellKeys.some((key) => occupiedCells.has(key))) {
    return { ok: false, error: "PEN_CELL_OCCUPIED" };
  }

  const now = options.now instanceof Date ? options.now : new Date();
  const instanceId = String(options.instanceId || `animal_${crypto.randomUUID()}`).trim();
  const animal = {
    instanceId,
    itemId,
    cellKeys,
    feedRefUnix: now.getTime() / 1000,
    produceRefUnix: now.getTime() / 1000,
    hasBeenFed: false,
    harvestsRemaining: maxHarvests,
    hasProductReady: false,
    isVaccinated: false,
  };
  parsed.build.animals.push(animal);

  const farmState = clone(currentFarmState) || {};
  farmState.snapshot_schema = Math.max(1, Number(farmState.snapshot_schema) || 1);
  farmState.build_state_json = JSON.stringify(parsed.build);
  farmState.client_saved_at = now.toISOString();
  if (Number.isInteger(Number(farmState.legacy_content_score))) {
    farmState.legacy_content_score = Number(farmState.legacy_content_score) + 1;
  }

  return { ok: true, farmState, animal };
}

module.exports = {
  prepareAnimalPlacement,
};
