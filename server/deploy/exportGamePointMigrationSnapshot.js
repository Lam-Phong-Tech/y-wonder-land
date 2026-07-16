"use strict";

const { Client } = require("pg");

const RAW_EXPORT_ACK = "I_UNDERSTAND_THIS_OUTPUT_CONTAINS_RAW_WALLET_IDENTITIES";
const POINT_MICROS = 1_000_000n;
const PUBLIC_REF_PATTERN = /^[a-f0-9]{24}$/;
const SHA256_PATTERN = /^[a-f0-9]{64}$/;
const APPLY_OPERATION_PATTERN = /^point-remediation:[a-f0-9]{32}$/;
const ROLLBACK_OPERATION_PATTERN = /^point-remediation-rollback:[a-f0-9]{32}$/;
const PLAYER_QUERY = `
  select p.id as player_id,
         p.web_user_id,
         e.player_id as economy_player_id,
         e.pos,
         e.web_point_micros_remainder
  from game_players p
  left join player_economy e on e.player_id = p.id
  where p.web_user_id is not null and btrim(p.web_user_id) <> ''
  order by p.web_user_id, p.id`;
const TRANSACTION_QUERY = `
  select t.id as transaction_id,
         t.player_id,
         t.type,
         t.ref,
         t.idempotency_key,
         t.request_signature,
         t.delta_pos,
         t.details_json ->> 'pointAmountMicros' as point_amount_micros,
         t.details_json ->> 'pointMicrosRemainderBefore' as point_micros_remainder_before,
         t.details_json ->> 'pointMicrosRemainderAfter' as point_micros_remainder_after,
         t.details_json ->> 'remediationKind' as remediation_kind,
         t.details_json ->> 'remediationPlanSha256' as remediation_plan_sha256,
         t.details_json ->> 'operationApprovalSha256' as operation_approval_sha256,
         t.details_json ->> 'originalOperationId' as remediation_original_operation_id,
         t.details_json -> 'sourceRefs' as remediation_source_refs
  from game_transactions t
  join game_players p on p.id = t.player_id
  where p.web_user_id is not null and btrim(p.web_user_id) <> ''
  order by t.player_id, t.created_at, t.id`;

function requiredText(value, field) {
  const text = String(value == null ? "" : value).trim();
  if (!text || text.length > 256 || /[\r\n\0]/.test(text)) {
    throw new Error(`INVALID_${field.toUpperCase()}`);
  }
  return text;
}

function optionalText(value, field, maxLength = 256) {
  const text = String(value == null ? "" : value).trim();
  if (text.length > maxLength || /[\r\n\0]/.test(text)) {
    throw new Error(`INVALID_${field.toUpperCase()}`);
  }
  return text;
}

function integerText(value, field, options = {}) {
  const text = String(value == null ? "" : value).trim();
  if (!/^-?(0|[1-9]\d*)$/.test(text)) throw new Error(`INVALID_${field.toUpperCase()}`);
  const parsed = BigInt(text);
  if (options.nonNegative && parsed < 0n) throw new Error(`INVALID_${field.toUpperCase()}`);
  return text;
}

function optionalIntegerText(value, field, options = {}) {
  const text = String(value == null ? "" : value).trim();
  return text ? integerText(text, field, options) : null;
}

function remediationEvidence(row, transaction) {
  const apply = transaction.type === "point_remediation_reversal";
  const rollback = transaction.type === "point_remediation_reversal_rollback";
  const sourceRefsValue = row.remediation_source_refs;
  const hasMetadata = [
    row.remediation_kind,
    row.remediation_plan_sha256,
    row.operation_approval_sha256,
    row.remediation_original_operation_id,
    sourceRefsValue,
  ].some((value) => value != null && value !== "");
  if (!apply && !rollback) {
    if (hasMetadata) throw new Error("UNEXPECTED_GAME_REMEDIATION_METADATA");
    return null;
  }

  const action = apply ? "APPLY" : "ROLLBACK";
  const expectedKind = apply
    ? "SYNTHETIC_CREDIT_REVERSAL"
    : "SYNTHETIC_CREDIT_REVERSAL_ROLLBACK";
  const operationPattern = apply ? APPLY_OPERATION_PATTERN : ROLLBACK_OPERATION_PATTERN;
  if (!operationPattern.test(transaction.transactionId)) {
    throw new Error("INVALID_GAME_REMEDIATION_OPERATION_ID");
  }
  const originalOperationId = requiredText(
    row.remediation_original_operation_id,
    "game_remediation_original_operation_id"
  );
  if (!APPLY_OPERATION_PATTERN.test(originalOperationId)
      || transaction.ref !== originalOperationId
      || (apply && transaction.transactionId !== originalOperationId)) {
    throw new Error("INVALID_GAME_REMEDIATION_ORIGINAL_OPERATION");
  }
  if (String(row.idempotency_key || "") !== transaction.transactionId) {
    throw new Error("INVALID_GAME_REMEDIATION_IDEMPOTENCY_KEY");
  }
  const requestSignature = String(row.request_signature || "").trim();
  const planSha256 = String(row.remediation_plan_sha256 || "").trim();
  const approvalSha256 = String(row.operation_approval_sha256 || "").trim();
  if (!SHA256_PATTERN.test(requestSignature)
      || !SHA256_PATTERN.test(planSha256)
      || !SHA256_PATTERN.test(approvalSha256)) {
    throw new Error("INVALID_GAME_REMEDIATION_CHECKSUM_EVIDENCE");
  }
  if (String(row.remediation_kind || "") !== expectedKind) {
    throw new Error("INVALID_GAME_REMEDIATION_KIND");
  }
  if (!Array.isArray(sourceRefsValue) || sourceRefsValue.length === 0
      || sourceRefsValue.some((value) => !PUBLIC_REF_PATTERN.test(String(value)))) {
    throw new Error("INVALID_GAME_REMEDIATION_SOURCE_REFS");
  }
  const sourceRefs = sourceRefsValue.map(String).sort();
  if (new Set(sourceRefs).size !== sourceRefs.length) {
    throw new Error("DUPLICATE_GAME_REMEDIATION_SOURCE_REF");
  }
  if (transaction.pointAmountMicros == null
      || transaction.pointMicrosRemainderBefore == null
      || transaction.pointMicrosRemainderAfter == null) {
    throw new Error("GAME_REMEDIATION_EXACT_EVIDENCE_MISSING");
  }
  const deltaMicros = BigInt(transaction.deltaPoint) * POINT_MICROS
    + BigInt(transaction.pointMicrosRemainderAfter)
    - BigInt(transaction.pointMicrosRemainderBefore);
  const expectedDelta = BigInt(transaction.pointAmountMicros) * (apply ? -1n : 1n);
  if (deltaMicros !== expectedDelta) {
    throw new Error("GAME_REMEDIATION_BALANCE_ARITHMETIC_MISMATCH");
  }

  return {
    action,
    originalOperationId,
    planSha256,
    approvalSha256,
    requestSignature,
    sourceRefs,
  };
}

function buildGameSnapshot(playerRows, transactionRows) {
  const playerIds = new Set();
  const players = playerRows.map((row) => {
    const playerId = requiredText(row.player_id, "game_player_id");
    if (playerIds.has(playerId)) throw new Error("DUPLICATE_GAME_PLAYER");
    playerIds.add(playerId);
    if (String(row.economy_player_id || "") !== playerId) {
      throw new Error("GAME_PLAYER_MISSING_ECONOMY");
    }
    const remainder = integerText(
      row.web_point_micros_remainder,
      "game_point_micros_remainder",
      { nonNegative: true }
    );
    if (BigInt(remainder) >= 1_000_000n) throw new Error("INVALID_GAME_POINT_MICROS_REMAINDER");
    return {
      playerId,
      webUserId: requiredText(row.web_user_id, "game_web_user_id"),
      point: integerText(row.pos, "game_point", { nonNegative: true }),
      pointMicrosRemainder: remainder,
    };
  });

  const transactions = transactionRows.map((row) => {
    const transactionId = requiredText(row.transaction_id, "game_transaction_id");
    const playerId = requiredText(row.player_id, "game_transaction_player_id");
    if (!playerIds.has(playerId)) throw new Error("GAME_TRANSACTION_PLAYER_NOT_IN_SNAPSHOT");
    const amount = String(row.point_amount_micros == null ? "" : row.point_amount_micros).trim();
    const transaction = {
      transactionId,
      playerId,
      type: optionalText(row.type, "game_transaction_type", 128) || "UNKNOWN",
      ref: optionalText(row.ref, "game_transaction_ref", 256),
      deltaPoint: integerText(row.delta_pos, "game_transaction_delta_point"),
      pointAmountMicros: amount
        ? integerText(amount, "game_transaction_point_amount_micros", { nonNegative: true })
        : null,
      pointMicrosRemainderBefore: optionalIntegerText(
        row.point_micros_remainder_before,
        "game_transaction_remainder_before",
        { nonNegative: true }
      ),
      pointMicrosRemainderAfter: optionalIntegerText(
        row.point_micros_remainder_after,
        "game_transaction_remainder_after",
        { nonNegative: true }
      ),
    };
    transaction.remediation = remediationEvidence(row, transaction);
    return transaction;
  });

  return { schemaVersion: 2, players, transactions };
}

async function exportGameSnapshot(client) {
  let transactionOpen = false;
  try {
    await client.query("BEGIN TRANSACTION ISOLATION LEVEL REPEATABLE READ READ ONLY");
    transactionOpen = true;
    const players = await client.query(PLAYER_QUERY);
    const transactions = await client.query(TRANSACTION_QUERY);
    const snapshot = buildGameSnapshot(players.rows, transactions.rows);
    await client.query("COMMIT");
    transactionOpen = false;
    return snapshot;
  } catch (error) {
    if (transactionOpen) {
      try {
        await client.query("ROLLBACK");
      } catch {
        // Preserve the original export error.
      }
    }
    throw error;
  }
}

async function main() {
  if (process.argv.length !== 2) throw new Error("INVALID_ARGUMENT");
  if (process.env.POINT_MIGRATION_RAW_EXPORT_ACK !== RAW_EXPORT_ACK) {
    throw new Error("RAW_EXPORT_ACK_REQUIRED");
  }
  const connectionString = String(process.env.DATABASE_URL || "").trim();
  if (!connectionString) throw new Error("DATABASE_URL_REQUIRED");
  const client = new Client({ connectionString, statement_timeout: 30_000, query_timeout: 35_000 });
  await client.connect();
  try {
    const snapshot = await exportGameSnapshot(client);
    process.stdout.write(`${JSON.stringify(snapshot)}\n`);
  } finally {
    await client.end();
  }
}

if (require.main === module) {
  main().catch((error) => {
    process.stderr.write(`[game-point-migration-export] ${error && error.message || "FAILED"}\n`);
    process.exitCode = 1;
  });
}

module.exports = {
  PLAYER_QUERY,
  RAW_EXPORT_ACK,
  TRANSACTION_QUERY,
  buildGameSnapshot,
  exportGameSnapshot,
};
