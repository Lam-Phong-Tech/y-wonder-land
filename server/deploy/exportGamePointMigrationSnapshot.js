"use strict";

const { Client } = require("pg");

const RAW_EXPORT_ACK = "I_UNDERSTAND_THIS_OUTPUT_CONTAINS_RAW_WALLET_IDENTITIES";
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
         t.delta_pos,
         t.details_json ->> 'pointAmountMicros' as point_amount_micros,
         t.details_json ->> 'pointMicrosRemainderBefore' as point_micros_remainder_before,
         t.details_json ->> 'pointMicrosRemainderAfter' as point_micros_remainder_after
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
    return {
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
  });

  return { schemaVersion: 1, players, transactions };
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
