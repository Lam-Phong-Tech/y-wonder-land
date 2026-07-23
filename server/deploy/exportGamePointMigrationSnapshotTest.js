"use strict";

const assert = require("assert");
const fs = require("fs");
const path = require("path");
const {
  buildGameSnapshot,
  exportGameSnapshot,
} = require("./exportGamePointMigrationSnapshot");

function fixturePlayers() {
  return [{
    player_id: "player-1",
    web_user_id: "web-user-1",
    economy_player_id: "player-1",
    pos: "5001",
    web_point_micros_remainder: "500000",
  }];
}

function fixtureTransactions() {
  return [{
    transaction_id: "game-tx-1",
    player_id: "player-1",
    type: "web_topup_credit",
    ref: "source-1",
    delta_pos: "1",
    point_amount_micros: "1500000",
    point_micros_remainder_before: "0",
    point_micros_remainder_after: "500000",
  }];
}

async function run() {
  const queries = [];
  const client = {
    async query(sql) {
      queries.push(sql);
      if (/^\s*select p\.id/i.test(sql)) return { rows: fixturePlayers() };
      if (/^\s*select t\.id/i.test(sql)) return { rows: fixtureTransactions() };
      return { rows: [] };
    },
  };
  const snapshot = await exportGameSnapshot(client);
  assert.deepStrictEqual(snapshot, {
    schemaVersion: 2,
    players: [{
      playerId: "player-1",
      webUserId: "web-user-1",
      point: "5001",
      pointMicrosRemainder: "500000",
    }],
    transactions: [{
      transactionId: "game-tx-1",
      playerId: "player-1",
      type: "web_topup_credit",
      ref: "source-1",
      deltaPoint: "1",
      pointAmountMicros: "1500000",
      pointMicrosRemainderBefore: "0",
      pointMicrosRemainderAfter: "500000",
      remediation: null,
    }],
  });
  assert.match(queries[0], /REPEATABLE READ READ ONLY/);
  assert.strictEqual(queries.at(-1), "COMMIT");
  for (const sql of queries.slice(0, -1)) {
    assert(!/\b(insert|update|delete|merge|truncate|alter|drop|create|grant|revoke)\b/i.test(sql),
      `Mutation keyword found in game exporter query: ${sql}`);
  }

  const failedQueries = [];
  await assert.rejects(
    () => exportGameSnapshot({
      async query(sql) {
        failedQueries.push(sql);
        if (/^\s*select p\.id/i.test(sql)) return { rows: fixturePlayers() };
        if (/^\s*select t\.id/i.test(sql)) throw new Error("synthetic read failure");
        return { rows: [] };
      },
    }),
    /synthetic read failure/
  );
  assert.strictEqual(failedQueries.at(-1), "ROLLBACK");

  assert.throws(
    () => buildGameSnapshot([{ ...fixturePlayers()[0], economy_player_id: null }], []),
    /GAME_PLAYER_MISSING_ECONOMY/
  );
  assert.throws(
    () => buildGameSnapshot([{ ...fixturePlayers()[0], web_point_micros_remainder: "1000000" }], []),
    /INVALID_GAME_POINT_MICROS_REMAINDER/
  );

  const operationId = `point-remediation:${"a".repeat(32)}`;
  const remediationRow = {
    transaction_id: operationId,
    player_id: "player-1",
    type: "point_remediation_reversal",
    ref: operationId,
    idempotency_key: operationId,
    request_signature: "b".repeat(64),
    delta_pos: "-3",
    point_amount_micros: "3000000",
    point_micros_remainder_before: "0",
    point_micros_remainder_after: "0",
    remediation_kind: "SYNTHETIC_CREDIT_REVERSAL",
    remediation_plan_sha256: "c".repeat(64),
    operation_approval_sha256: "d".repeat(64),
    remediation_original_operation_id: operationId,
    remediation_source_refs: ["e".repeat(24)],
  };
  const remediation = buildGameSnapshot(fixturePlayers(), [remediationRow]).transactions[0];
  assert.deepStrictEqual(remediation.remediation, {
    action: "APPLY",
    originalOperationId: operationId,
    planSha256: "c".repeat(64),
    approvalSha256: "d".repeat(64),
    requestSignature: "b".repeat(64),
    sourceRefs: ["e".repeat(24)],
  });
  assert.throws(
    () => buildGameSnapshot(fixturePlayers(), [{
      ...remediationRow,
      remediation_source_refs: ["e".repeat(24), "e".repeat(24)],
    }]),
    /DUPLICATE_GAME_REMEDIATION_SOURCE_REF/
  );
  assert.throws(
    () => buildGameSnapshot(fixturePlayers(), [{ ...remediationRow, delta_pos: "-2" }]),
    /GAME_REMEDIATION_BALANCE_ARITHMETIC_MISMATCH/
  );
  assert.throws(
    () => buildGameSnapshot(fixturePlayers(), [{
      ...fixtureTransactions()[0],
      remediation_source_refs: ["e".repeat(24)],
    }]),
    /UNEXPECTED_GAME_REMEDIATION_METADATA/
  );

  const runner = fs.readFileSync(path.join(__dirname, "run-point-wallet-migration-dry-run.sh"), "utf8");
  assert(runner.includes("umask 077"));
  assert(runner.includes("cmp -s \"${web_snapshot_first}\" \"${web_snapshot_second}\""));
  assert(runner.includes("cmp -s \"${game_snapshot_first}\" \"${game_snapshot_second}\""));
  assert(runner.includes("RAW_SNAPSHOTS_RETAINED=no"));
  assert(runner.includes("rm -rf -- \"${run_root}\""));
  assert(!/\bpsql\b|\bsqlite3\b/.test(runner), "Runner must not expose an ad-hoc database mutation shell.");

  console.log("[game-point-migration-export] PASS: PostgreSQL export is repeatable-read, read-only, and rolls back on error.");
}

run().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
