const crypto = require("crypto");

const POINT_MICROS_SCALE = 1_000_000;

const POINT_SOURCE_ORIGINS = Object.freeze({
  USDT: "USDT",
  YWH: "YWH",
  GAMEPLAY: "GAMEPLAY",
  ADMIN: "ADMIN",
  LEGACY: "LEGACY",
  UNATTRIBUTED: "UNATTRIBUTED",
});

const POINT_LOT_ACQUISITIONS = Object.freeze({
  ORIGIN: "ORIGIN",
  TRANSFER: "TRANSFER",
  MIGRATION: "MIGRATION",
});

const POINT_COMMISSION_ASSETS = Object.freeze({
  USDT: "USDT",
  POINT: "POINT",
});

const EXTERNAL_ORIGINS = new Set([
  POINT_SOURCE_ORIGINS.USDT,
  POINT_SOURCE_ORIGINS.YWH,
  POINT_SOURCE_ORIGINS.ADMIN,
  POINT_SOURCE_ORIGINS.LEGACY,
]);

function requiredText(value, field, maxLength = 128) {
  const text = String(value || "").trim();
  if (!text) throw new Error(`MISSING_${field}`);
  if (text.length > maxLength || /[\r\n\0]/.test(text)) {
    throw new Error(`INVALID_${field}`);
  }
  return text;
}

function optionalText(value, field, maxLength = 128) {
  const text = String(value || "").trim();
  if (!text) return "";
  if (text.length > maxLength || /[\r\n\0]/.test(text)) {
    throw new Error(`INVALID_${field}`);
  }
  return text;
}

function positiveSafeInteger(value, field) {
  const number = Number(value);
  if (!Number.isSafeInteger(number) || number < 1) {
    throw new Error(`INVALID_${field}`);
  }
  return number;
}

function nonNegativeSafeInteger(value, field) {
  const number = Number(value);
  if (!Number.isSafeInteger(number) || number < 0) {
    throw new Error(`INVALID_${field}`);
  }
  return number;
}

function normalizedDate(value, field) {
  const timestamp = Date.parse(String(value || ""));
  if (!Number.isFinite(timestamp)) throw new Error(`INVALID_${field}`);
  return new Date(timestamp).toISOString();
}

function plainMetadata(value) {
  if (value == null) return {};
  if (!value || typeof value !== "object" || Array.isArray(value)) {
    throw new Error("INVALID_POINT_SOURCE_METADATA");
  }
  return JSON.parse(JSON.stringify(value));
}

function stableValue(value) {
  if (Array.isArray(value)) return value.map(stableValue);
  if (!value || typeof value !== "object") return value;
  return Object.keys(value).sort().reduce((output, key) => {
    output[key] = stableValue(value[key]);
    return output;
  }, {});
}

function makePointSourceLotId(playerId, sourceEventId, sourceEventIndex = 0) {
  const canonical = JSON.stringify([
    "ywonder-point-source-lot-v1",
    requiredText(playerId, "PLAYER_ID"),
    requiredText(sourceEventId, "SOURCE_EVENT_ID"),
    nonNegativeSafeInteger(sourceEventIndex, "SOURCE_EVENT_INDEX"),
  ]);
  return `psl_${crypto.createHash("sha256").update(canonical, "utf8").digest("hex").slice(0, 40)}`;
}

function normalizePointSourceLot(input) {
  const source = input && typeof input === "object" ? input : {};
  const playerId = requiredText(source.playerId, "PLAYER_ID");
  const sourceEventId = requiredText(source.sourceEventId, "SOURCE_EVENT_ID");
  const sourceEventIndex = nonNegativeSafeInteger(
    source.sourceEventIndex == null ? 0 : source.sourceEventIndex,
    "SOURCE_EVENT_INDEX"
  );
  const id = optionalText(source.id, "POINT_SOURCE_LOT_ID")
    || makePointSourceLotId(playerId, sourceEventId, sourceEventIndex);
  const originType = requiredText(source.originType, "POINT_SOURCE_ORIGIN", 32).toUpperCase();
  const acquisitionType = requiredText(
    source.acquisitionType || POINT_LOT_ACQUISITIONS.ORIGIN,
    "POINT_LOT_ACQUISITION",
    32
  ).toUpperCase();

  if (!Object.values(POINT_SOURCE_ORIGINS).includes(originType)) {
    throw new Error("INVALID_POINT_SOURCE_ORIGIN");
  }
  if (!Object.values(POINT_LOT_ACQUISITIONS).includes(acquisitionType)) {
    throw new Error("INVALID_POINT_LOT_ACQUISITION");
  }

  const pointAmountMicros = positiveSafeInteger(
    source.pointAmountMicros,
    "POINT_AMOUNT_MICROS"
  );
  const remainingPointMicros = source.remainingPointMicros == null
    ? pointAmountMicros
    : nonNegativeSafeInteger(source.remainingPointMicros, "REMAINING_POINT_MICROS");
  if (remainingPointMicros > pointAmountMicros) {
    throw new Error("POINT_SOURCE_REMAINING_EXCEEDS_AMOUNT");
  }

  const parentLotId = optionalText(source.parentLotId, "PARENT_POINT_SOURCE_LOT_ID");
  let rootLotId = optionalText(source.rootLotId, "ROOT_POINT_SOURCE_LOT_ID");
  if (acquisitionType === POINT_LOT_ACQUISITIONS.TRANSFER) {
    if (!parentLotId || !rootLotId) throw new Error("POINT_TRANSFER_LINEAGE_REQUIRED");
  } else {
    if (parentLotId) throw new Error("POINT_SOURCE_PARENT_FORBIDDEN");
    rootLotId = rootLotId || id;
    if (rootLotId !== id) throw new Error("POINT_SOURCE_ROOT_MISMATCH");
  }
  if (acquisitionType !== POINT_LOT_ACQUISITIONS.MIGRATION
      && remainingPointMicros !== pointAmountMicros) {
    throw new Error("POINT_SOURCE_INITIAL_REMAINING_MISMATCH");
  }

  let commissionAsset = optionalText(
    source.commissionAsset,
    "POINT_COMMISSION_ASSET",
    16
  ).toUpperCase();
  const sourceRatePair = optionalText(source.sourceRatePair, "POINT_SOURCE_RATE_PAIR", 32)
    .toUpperCase();
  const sourceRateVersionId = optionalText(
    source.sourceRateVersionId,
    "POINT_SOURCE_RATE_VERSION_ID"
  );
  const pointMicrosPerSourceUnit = source.pointMicrosPerSourceUnit == null
      || source.pointMicrosPerSourceUnit === ""
    ? null
    : positiveSafeInteger(source.pointMicrosPerSourceUnit, "POINT_MICROS_PER_SOURCE_UNIT");
  const commissionRateVersionId = optionalText(
    source.commissionRateVersionId,
    "POINT_COMMISSION_RATE_VERSION_ID"
  );
  const pointMicrosPerUsdt = source.pointMicrosPerUsdt == null || source.pointMicrosPerUsdt === ""
    ? null
    : positiveSafeInteger(source.pointMicrosPerUsdt, "POINT_MICROS_PER_USDT");
  const hasAnySourceRate = Boolean(sourceRatePair || sourceRateVersionId
    || pointMicrosPerSourceUnit != null);
  const hasCompleteSourceRate = Boolean(sourceRatePair && sourceRateVersionId
    && pointMicrosPerSourceUnit != null);
  if (hasAnySourceRate !== hasCompleteSourceRate) {
    throw new Error("POINT_SOURCE_RATE_INCOMPLETE");
  }

  if (EXTERNAL_ORIGINS.has(originType)) {
    commissionAsset = commissionAsset || POINT_COMMISSION_ASSETS.USDT;
    if (commissionAsset !== POINT_COMMISSION_ASSETS.USDT
        || !commissionRateVersionId || pointMicrosPerUsdt == null) {
      throw new Error("POINT_SOURCE_USDT_RATE_REQUIRED");
    }
    if (originType === POINT_SOURCE_ORIGINS.USDT
        || originType === POINT_SOURCE_ORIGINS.YWH) {
      const requiredPair = `${originType}_POINT`;
      if (!hasCompleteSourceRate || sourceRatePair !== requiredPair) {
        throw new Error("POINT_SOURCE_CONVERSION_RATE_REQUIRED");
      }
    } else if (hasAnySourceRate) {
      throw new Error("POINT_NON_CONVERSION_SOURCE_RATE_FORBIDDEN");
    }
  } else if (originType === POINT_SOURCE_ORIGINS.GAMEPLAY) {
    commissionAsset = commissionAsset || POINT_COMMISSION_ASSETS.POINT;
    if (commissionAsset !== POINT_COMMISSION_ASSETS.POINT
        || hasAnySourceRate || commissionRateVersionId || pointMicrosPerUsdt != null) {
      throw new Error("POINT_GAMEPLAY_SOURCE_RATE_FORBIDDEN");
    }
  } else {
    if (commissionAsset || hasAnySourceRate
        || commissionRateVersionId || pointMicrosPerUsdt != null) {
      throw new Error("POINT_UNATTRIBUTED_SOURCE_MUST_STAY_BLOCKED");
    }
    commissionAsset = "";
  }

  return {
    id,
    playerId,
    sourceEventId,
    sourceEventIndex,
    originType,
    acquisitionType,
    commissionAsset,
    pointAmountMicros,
    remainingPointMicros,
    sourceRatePair,
    sourceRateVersionId,
    pointMicrosPerSourceUnit,
    commissionRateVersionId,
    pointMicrosPerUsdt,
    parentLotId,
    rootLotId,
    occurredAt: normalizedDate(source.occurredAt, "POINT_SOURCE_OCCURRED_AT"),
    createdAt: source.createdAt
      ? normalizedDate(source.createdAt, "POINT_SOURCE_CREATED_AT")
      : "",
    metadata: plainMetadata(source.metadata),
  };
}

function pointSourceLotRequestSignature(input) {
  const lot = normalizePointSourceLot(input);
  return JSON.stringify(stableValue({
    id: lot.id,
    playerId: lot.playerId,
    sourceEventId: lot.sourceEventId,
    sourceEventIndex: lot.sourceEventIndex,
    originType: lot.originType,
    acquisitionType: lot.acquisitionType,
    commissionAsset: lot.commissionAsset,
    pointAmountMicros: lot.pointAmountMicros,
    sourceRatePair: lot.sourceRatePair,
    sourceRateVersionId: lot.sourceRateVersionId,
    pointMicrosPerSourceUnit: lot.pointMicrosPerSourceUnit,
    commissionRateVersionId: lot.commissionRateVersionId,
    pointMicrosPerUsdt: lot.pointMicrosPerUsdt,
    parentLotId: lot.parentLotId,
    rootLotId: lot.rootLotId,
    occurredAt: lot.occurredAt,
    metadata: lot.metadata,
  }));
}

function validateTransferredPointSourceLot(parentInput, childInput) {
  const parent = normalizePointSourceLot(parentInput);
  const child = normalizePointSourceLot(childInput);
  if (child.acquisitionType !== POINT_LOT_ACQUISITIONS.TRANSFER
      || child.parentLotId !== parent.id
      || child.rootLotId !== parent.rootLotId
      || child.originType !== parent.originType
      || child.commissionAsset !== parent.commissionAsset
      || child.sourceRatePair !== parent.sourceRatePair
      || child.sourceRateVersionId !== parent.sourceRateVersionId
      || child.pointMicrosPerSourceUnit !== parent.pointMicrosPerSourceUnit
      || child.commissionRateVersionId !== parent.commissionRateVersionId
      || child.pointMicrosPerUsdt !== parent.pointMicrosPerUsdt) {
    throw new Error("POINT_TRANSFER_SOURCE_MISMATCH");
  }
  if (child.pointAmountMicros > parent.remainingPointMicros) {
    throw new Error("POINT_TRANSFER_SOURCE_INSUFFICIENT");
  }
  return child;
}

function makeTransferredPointSourceLot(parentInput, input) {
  const parent = normalizePointSourceLot(parentInput);
  const source = input && typeof input === "object" ? input : {};
  return validateTransferredPointSourceLot(parent, {
    id: source.id,
    playerId: source.playerId,
    sourceEventId: source.sourceEventId,
    sourceEventIndex: source.sourceEventIndex,
    originType: parent.originType,
    acquisitionType: POINT_LOT_ACQUISITIONS.TRANSFER,
    commissionAsset: parent.commissionAsset,
    pointAmountMicros: source.pointAmountMicros,
    sourceRatePair: parent.sourceRatePair,
    sourceRateVersionId: parent.sourceRateVersionId,
    pointMicrosPerSourceUnit: parent.pointMicrosPerSourceUnit,
    commissionRateVersionId: parent.commissionRateVersionId,
    pointMicrosPerUsdt: parent.pointMicrosPerUsdt,
    parentLotId: parent.id,
    rootLotId: parent.rootLotId,
    occurredAt: source.occurredAt,
    metadata: source.metadata,
  });
}

function planPointSourceFifo(lotsInput, requestedPointMicros) {
  const amountMicros = positiveSafeInteger(requestedPointMicros, "POINT_SPEND_MICROS");
  const lots = (Array.isArray(lotsInput) ? lotsInput : [])
    .map(normalizePointSourceLot)
    .filter((lot) => lot.remainingPointMicros > 0)
    .sort((left, right) => left.occurredAt.localeCompare(right.occurredAt)
      || left.createdAt.localeCompare(right.createdAt)
      || left.id.localeCompare(right.id));

  const availablePointMicros = lots.reduce((sum, lot) => {
    if (sum > Number.MAX_SAFE_INTEGER - lot.remainingPointMicros) {
      throw new Error("POINT_SOURCE_BALANCE_OVERFLOW");
    }
    return sum + lot.remainingPointMicros;
  }, 0);
  if (availablePointMicros < amountMicros) {
    return {
      ok: false,
      error: "POINT_SOURCE_BALANCE_INSUFFICIENT",
      requestedPointMicros: amountMicros,
      availablePointMicros,
      allocations: [],
    };
  }

  let remaining = amountMicros;
  const allocations = [];
  for (const lot of lots) {
    if (remaining === 0) break;
    if (lot.originType === POINT_SOURCE_ORIGINS.UNATTRIBUTED || !lot.commissionAsset) {
      return {
        ok: false,
        error: "POINT_SOURCE_CLASSIFICATION_REQUIRED",
        requestedPointMicros: amountMicros,
        availablePointMicros,
        blockedLotId: lot.id,
        allocations: [],
      };
    }
    const allocatedPointMicros = Math.min(remaining, lot.remainingPointMicros);
    allocations.push({
      sequence: allocations.length,
      lotId: lot.id,
      originType: lot.originType,
      commissionAsset: lot.commissionAsset,
      sourceRatePair: lot.sourceRatePair,
      sourceRateVersionId: lot.sourceRateVersionId,
      pointMicrosPerSourceUnit: lot.pointMicrosPerSourceUnit,
      commissionRateVersionId: lot.commissionRateVersionId,
      pointMicrosPerUsdt: lot.pointMicrosPerUsdt,
      allocatedPointMicros,
    });
    remaining -= allocatedPointMicros;
  }

  return {
    ok: true,
    requestedPointMicros: amountMicros,
    availablePointMicros,
    allocations,
  };
}

module.exports = {
  POINT_MICROS_SCALE,
  POINT_SOURCE_ORIGINS,
  POINT_LOT_ACQUISITIONS,
  POINT_COMMISSION_ASSETS,
  makePointSourceLotId,
  normalizePointSourceLot,
  pointSourceLotRequestSignature,
  validateTransferredPointSourceLot,
  makeTransferredPointSourceLot,
  planPointSourceFifo,
};
