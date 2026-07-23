const fs = require("fs");
const os = require("os");
const path = require("path");

function assert(condition, message) {
  if (!condition) throw new Error(message);
}

function rootLot(overrides = {}) {
  return {
    sourceEventId: "source-usdt-001",
    sourceEventIndex: 0,
    originType: "USDT",
    acquisitionType: "ORIGIN",
    pointAmountMicros: 100_000_000,
    commissionAsset: "USDT",
    sourceRatePair: "USDT_POINT",
    sourceRateVersionId: "rate-usdt-point-26.5-v1",
    pointMicrosPerSourceUnit: 26_500_000,
    commissionRateVersionId: "rate-usdt-point-26.5-v1",
    pointMicrosPerUsdt: 26_500_000,
    occurredAt: "2026-07-19T01:00:00.000Z",
    metadata: { usdtMicros: "3773585", quote: "26.5" },
    ...overrides,
  };
}

async function main() {
  const tempDir = fs.mkdtempSync(path.join(os.tmpdir(), "yw-point-source-ledger-"));
  const dataPath = path.join(tempDir, "data.json");
  process.env.STORE_MODE = "json";
  process.env.YW_DATA_PATH = dataPath;

  const {
    POINT_MICROS_SCALE,
    makeTransferredPointSourceLot,
    normalizePointSourceLot,
    planPointSourceFifo,
    validateTransferredPointSourceLot,
  } = require("./pointSourceLedger");
  const { JsonStore } = require("./store");

  assert(POINT_MICROS_SCALE === 1_000_000, "Point source ledger changed the fixed micro scale.");

  const senderId = "point-source-sender";
  const recipientId = "point-source-recipient";
  const store = new JsonStore(dataPath);
  store.writeAll({
    users: [
      { id: senderId, username: senderId },
      { id: recipientId, username: recipientId },
    ],
    economies: {
      [senderId]: { version: 1, pos: 5000, updatedAt: "2026-07-19T00:00:00.000Z" },
      [recipientId]: { version: 1, pos: 5000, updatedAt: "2026-07-19T00:00:00.000Z" },
    },
  });

  assert(store.getPointSourceLots(senderId).length === 0,
    "Existing Point balance was silently backfilled without migration approval.");

  const first = store.recordPointSourceLot(senderId, rootLot({
    metadata: { quote: "26.5", usdtMicros: "3773585" },
  }));
  assert(first.ok && !first.duplicate, "USDT source lot was not persisted.");
  assert(first.lot.rootLotId === first.lot.id && first.lot.parentLotId === "",
    "Origin lot lineage was not rooted in itself.");

  const replay = store.recordPointSourceLot(senderId, rootLot({
    metadata: { usdtMicros: "3773585", quote: "26.5" },
  }));
  assert(replay.ok && replay.duplicate && replay.lot.id === first.lot.id,
    "Equivalent source event replay was not idempotent.");

  const conflict = store.recordPointSourceLot(senderId, rootLot({
    pointAmountMicros: 101_000_000,
  }));
  assert(!conflict.ok && conflict.error === "POINT_SOURCE_IDEMPOTENCY_CONFLICT",
    "Changed source payload reused an existing event without conflict.");

  const rateConflict = store.recordPointSourceLot(senderId, rootLot({
    sourceRateVersionId: "rate-usdt-point-25-v0",
    pointMicrosPerSourceUnit: 25_000_000,
    commissionRateVersionId: "rate-usdt-point-25-v0",
    pointMicrosPerUsdt: 25_000_000,
  }));
  assert(!rateConflict.ok && rateConflict.error === "POINT_SOURCE_IDEMPOTENCY_CONFLICT",
    "Changed rate snapshot reused an existing source event without conflict.");

  const gameplay = store.recordPointSourceLot(senderId, rootLot({
    sourceEventId: "source-gameplay-001",
    originType: "GAMEPLAY",
    pointAmountMicros: 100_000_000,
    commissionAsset: "POINT",
    sourceRatePair: "",
    sourceRateVersionId: "",
    pointMicrosPerSourceUnit: null,
    commissionRateVersionId: "",
    pointMicrosPerUsdt: null,
    occurredAt: "2026-07-19T02:00:00.000Z",
    metadata: { reason: "farm_product_sale" },
  }));
  assert(gameplay.ok && gameplay.lot.commissionAsset === "POINT",
    "Gameplay Point source did not retain Point commission classification.");

  const ywh = store.recordPointSourceLot(senderId, rootLot({
    sourceEventId: "source-ywh-001",
    originType: "YWH",
    pointAmountMicros: 15_900_000,
    sourceRatePair: "YWH_POINT",
    sourceRateVersionId: "rate-ywh-point-1.59-v1",
    pointMicrosPerSourceUnit: 1_590_000,
    occurredAt: "2026-07-19T03:00:00.000Z",
    metadata: { ywhMicros: "10000000", ywhPointRateMicros: "1590000" },
  }));
  assert(ywh.ok && ywh.lot.pointMicrosPerUsdt === 26_500_000,
    "YWH source did not pin the USDT commission valuation rate.");

  const invalidExternal = store.recordPointSourceLot(senderId, rootLot({
    sourceEventId: "source-admin-invalid",
    originType: "ADMIN",
    sourceRatePair: "",
    sourceRateVersionId: "",
    pointMicrosPerSourceUnit: null,
    commissionRateVersionId: "",
    pointMicrosPerUsdt: null,
  }));
  assert(!invalidExternal.ok && invalidExternal.error === "POINT_SOURCE_USDT_RATE_REQUIRED",
    "External Point source was accepted without a rate snapshot.");

  const unattributed = store.recordPointSourceLot(senderId, rootLot({
    sourceEventId: "source-unattributed-001",
    originType: "UNATTRIBUTED",
    commissionAsset: "",
    sourceRatePair: "",
    sourceRateVersionId: "",
    pointMicrosPerSourceUnit: null,
    commissionRateVersionId: "",
    pointMicrosPerUsdt: null,
    pointAmountMicros: 1_000_000,
    occurredAt: "2026-07-19T04:00:00.000Z",
  }));
  assert(unattributed.ok && unattributed.lot.commissionAsset === "",
    "Unattributed Point source was silently assigned a commission asset.");

  const fifo = store.previewPointSourceFifo(senderId, 150_000_000);
  assert(fifo.ok && fifo.allocations.length === 2,
    "FIFO planner did not split a mixed-source spend.");
  assert(fifo.allocations[0].lotId === first.lot.id
      && fifo.allocations[0].allocatedPointMicros === 100_000_000
      && fifo.allocations[1].lotId === gameplay.lot.id
      && fifo.allocations[1].allocatedPointMicros === 50_000_000,
    "FIFO planner did not consume the oldest source lots first.");

  const insufficient = planPointSourceFifo(store.getPointSourceLots(senderId), 1_000_000_000);
  assert(!insufficient.ok && insufficient.error === "POINT_SOURCE_BALANCE_INSUFFICIENT",
    "FIFO planner accepted a spend larger than represented source lots.");

  const blockedClassification = store.previewPointSourceFifo(senderId, 216_000_000);
  assert(!blockedClassification.ok
      && blockedClassification.error === "POINT_SOURCE_CLASSIFICATION_REQUIRED"
      && blockedClassification.blockedLotId === unattributed.lot.id,
    "FIFO planner consumed unattributed Point without a source decision.");

  const transferLot = makeTransferredPointSourceLot(first.lot, {
    playerId: recipientId,
    sourceEventId: "point-transfer-001",
    sourceEventIndex: 0,
    pointAmountMicros: 40_000_000,
    occurredAt: "2026-07-19T05:00:00.000Z",
    metadata: { senderPlayerId: senderId },
  });
  assert(transferLot.originType === "USDT"
      && transferLot.parentLotId === first.lot.id
      && transferLot.rootLotId === first.lot.id
      && transferLot.sourceRateVersionId === first.lot.sourceRateVersionId
      && transferLot.commissionRateVersionId === first.lot.commissionRateVersionId
      && transferLot.pointMicrosPerUsdt === first.lot.pointMicrosPerUsdt,
    "Transferred Point did not preserve its original source and rate snapshot.");

  const directTransfer = store.recordPointSourceLot(recipientId, transferLot);
  assert(!directTransfer.ok && directTransfer.error === "POINT_TRANSFER_REQUIRES_ATOMIC_OPERATION",
    "Transfer child was persisted without atomically consuming the sender source lot.");

  const alteredTransfer = normalizePointSourceLot({
    ...transferLot,
    originType: "GAMEPLAY",
    commissionAsset: "POINT",
    sourceRatePair: "",
    sourceRateVersionId: "",
    pointMicrosPerSourceUnit: null,
    commissionRateVersionId: "",
    pointMicrosPerUsdt: null,
  });
  let alteredTransferError = "";
  try {
    validateTransferredPointSourceLot(first.lot, alteredTransfer);
  } catch (error) {
    alteredTransferError = error.message;
  }
  assert(alteredTransferError === "POINT_TRANSFER_SOURCE_MISMATCH",
    "Transferred Point was allowed to change source classification.");
  let lineageError = "";
  try {
    makeTransferredPointSourceLot(first.lot, {
      ...transferLot,
      pointAmountMicros: first.lot.remainingPointMicros + 1,
    });
  } catch (error) {
    lineageError = error.message;
  }
  assert(lineageError === "POINT_TRANSFER_SOURCE_INSUFFICIENT",
    "Transfer lineage helper allowed more Point than the parent lot contains.");

  const restarted = new JsonStore(dataPath);
  assert(restarted.getPointSourceLots(senderId).length === 4,
    "Point source lots did not survive a JSON-store restart.");
  assert(restarted.getPointSourceLots(recipientId).length === 0,
    "Blocked non-atomic transfer unexpectedly persisted a recipient source lot.");
  assert(restarted.getEconomy(senderId).pos === 5000,
    "Dormant source ledger changed the authoritative Point balance.");

  const migrationSql = fs.readFileSync(
    path.join(__dirname, "migrations", "007_point_source_ledger.sql"),
    "utf8"
  );
  const schemaSql = fs.readFileSync(path.join(__dirname, "schema.sql"), "utf8");
  for (const sql of [migrationSql, schemaSql]) {
    assert(/create table if not exists point_source_lots/i.test(sql),
      "Point source lot table is missing from a schema surface.");
    assert(/unique \(player_id, source_event_id, source_event_index\)/i.test(sql),
      "Point source event idempotency constraint is missing.");
    assert(/origin_type in \('USDT', 'YWH', 'GAMEPLAY', 'ADMIN', 'LEGACY', 'UNATTRIBUTED'\)/i.test(sql),
      "Point source origin constraint is incomplete.");
    assert(/remaining_point_micros >= 0 and remaining_point_micros <= point_amount_micros/i.test(sql),
      "Point source remaining-balance constraint is missing.");
    assert(/source_rate_pair = 'YWH_POINT'/i.test(sql)
        && /commission_rate_version_id is not null/i.test(sql),
      "Separate source and commission rate constraints are missing.");
  }
  const sourceTableBlock = (sql) => {
    const match = /create table if not exists point_source_lots \([\s\S]*?\n\);/i.exec(sql);
    return match ? match[0].replace(/\s+/g, " ").trim() : "";
  };
  assert(sourceTableBlock(migrationSql) === sourceTableBlock(schemaSql),
    "Fresh schema and upgrade migration define different Point source lot tables.");

  fs.rmSync(tempDir, { recursive: true, force: true });
  console.log("[point-source-ledger] PASS");
}

main().catch((error) => {
  console.error(`[point-source-ledger] FAIL: ${error.message}`);
  process.exitCode = 1;
});
