"use strict";

const assert = require("assert");
const crypto = require("crypto");
const {
  executeSyntheticReversal,
  publicRef,
  rollbackOperationId,
} = require("./pointWalletSyntheticReversal");
const {
  EXPECTED_CONSTRAINTS,
  validatePointWalletRemediationOperationApproval,
} = require("./pointWalletRemediationOperationApproval");

const PLAN_SHA256 = "1".repeat(64);
const APPROVAL_SHA256 = "2".repeat(64);
const OCCURRED_AT = "2026-07-16T16:30:00.000Z";
const REFERENCE_KEY = "point-wallet-remediation-test-reference-key-2026";

function clone(value) {
  return JSON.parse(JSON.stringify(value));
}

class FakePgClient {
  constructor(state) {
    this.state = state;
    this.snapshot = null;
    this.rollbackCount = 0;
  }

  capture() {
    return clone({
      pos: this.state.pos,
      remainder: this.state.remainder,
      transactions: [...this.state.transactions.entries()],
    });
  }

  restore(snapshot) {
    this.state.pos = snapshot.pos;
    this.state.remainder = snapshot.remainder;
    this.state.transactions = new Map(snapshot.transactions);
  }

  async query(sql, params = []) {
    const statement = String(sql);
    if (statement.startsWith("BEGIN TRANSACTION")) {
      assert.strictEqual(this.snapshot, null, "nested transaction");
      this.snapshot = this.capture();
      return { rows: [] };
    }
    if (statement === "COMMIT") {
      this.snapshot = null;
      return { rows: [] };
    }
    if (statement === "ROLLBACK") {
      this.restore(this.snapshot);
      this.snapshot = null;
      this.rollbackCount += 1;
      return { rows: [] };
    }
    if (statement.includes("pg_advisory_xact_lock")) return { rows: [{}] };
    if (statement.includes("point_remediation_mapped_players")) {
      return { rows: clone(this.state.mappedPlayers) };
    }
    if (statement.includes("point_remediation_existing")) {
      const operationId = String(params[0]);
      const rows = [...this.state.transactions.values()]
        .filter((row) => row.id === operationId || row.idempotency_key === operationId)
        .sort((left, right) => String(left.id).localeCompare(String(right.id)));
      return { rows: clone(rows) };
    }
    if (statement.includes("point_remediation_lock_player")) {
      if (String(params[0]) !== this.state.playerId) return { rows: [] };
      return { rows: [{
        player_id: this.state.playerId,
        web_user_id: this.state.webUserId,
        active_session_id: this.state.activeSessionId,
        pos: this.state.pos,
        web_point_micros_remainder: this.state.remainder,
      }] };
    }
    if (statement.includes("point_remediation_source_credits")) {
      const requested = new Set(params[0].map(String));
      return { rows: clone(this.state.credits.filter((row) => requested.has(row.ref))) };
    }
    if (statement.includes("point_remediation_update_economy")) {
      const [playerId, nextPos, nextRemainder, currentPos, currentRemainder] = params.map(String);
      if (playerId !== this.state.playerId
          || currentPos !== String(this.state.pos)
          || currentRemainder !== String(this.state.remainder)) return { rows: [] };
      this.state.pos = nextPos;
      this.state.remainder = nextRemainder;
      return { rows: [{ pos: nextPos, web_point_micros_remainder: nextRemainder }] };
    }
    if (statement.includes("point_remediation_insert_ledger")) {
      const row = {
        id: String(params[0]),
        player_id: String(params[1]),
        type: String(params[2]),
        ref: String(params[3]),
        idempotency_key: String(params[4]),
        request_signature: String(params[5]),
        delta_pos: String(params[6]),
        delta_upos: 0,
        item_id: null,
        quantity_delta: null,
        details_json: JSON.parse(String(params[7])),
        result_json: JSON.parse(String(params[8])),
        created_at: String(params[9]),
      };
      assert(!this.state.transactions.has(row.id), "duplicate transaction insert");
      this.state.transactions.set(row.id, row);
      return { rows: [clone(row)] };
    }
    throw new Error(`UNEXPECTED_TEST_QUERY: ${statement}`);
  }
}

function fixture(overrides = {}) {
  const webUserId = "web-user-qa";
  const playerId = "game-player-qa";
  const sourceIds = ["canary-source-1", "canary-source-2", "canary-source-3"];
  const creditIds = ["credit-row-1", "credit-row-2", "credit-row-3"];
  const sources = sourceIds.map((sourceTransactionId, index) => ({
    sourceTransactionId,
    sourceRef: publicRef(REFERENCE_KEY, "source", sourceTransactionId),
    creditId: creditIds[index],
    gameCreditRef: publicRef(REFERENCE_KEY, "game-transaction", creditIds[index]),
    pointMicros: "1000000",
  }));
  const operationId = `point-remediation:${"a".repeat(32)}`;
  const referenceKeyId = crypto.createHash("sha256").update(REFERENCE_KEY).digest("hex").slice(0, 16);
  const plan = {
    schemaVersion: 1,
    generatedAt: "2026-07-16T15:56:41.000Z",
    mode: "READ_ONLY_REMEDIATION_PLAN",
    automaticExecutionAllowed: false,
    executionStatementsGenerated: 0,
    databaseMutationsPerformed: false,
    containsRawIdentities: false,
    planner: { sha256: "3".repeat(64) },
    summary: {
      syntheticReversalAccountCount: 1,
      syntheticReversalSourceCount: 3,
      syntheticReversalPointMicros: "3000000",
      residualAccountCount: 3,
      residualValueCount: 6,
      totalResidualPointAttos: "-666666328800",
      normalizationDeltaPointAttos: "666666328800",
    },
    authorization: {
      syntheticReversal: "NOT_AUTHORIZED",
      residualNormalization: "NOT_AUTHORIZED",
      accountLink: "DEFERRED",
      balanceMigration: "NOT_AUTHORIZED",
      deployment: "NOT_AUTHORIZED",
    },
    sources: { currentReport: { referenceKeyId } },
    syntheticReversalPlans: [{
      accountRef: publicRef(REFERENCE_KEY, "web-user", webUserId),
      gamePlayerRef: publicRef(REFERENCE_KEY, "player", playerId),
      proposedOperationId: operationId,
      operationStatus: "NOT_AUTHORIZED",
      currentGamePointMicros: "4370000000",
      reversalPointMicros: "3000000",
      expectedGamePointMicrosAfter: "4367000000",
      sources: sources.map((source) => ({
        sourceRef: source.sourceRef,
        gameCreditRef: source.gameCreditRef,
        pointMicros: source.pointMicros,
        evidenceStatus: "VERIFIED_DELIVERED_WITHOUT_WEB_TRANSACTION",
      })),
    }],
  };
  const approval = {
    schemaVersion: 1,
    mode: "POINT_WALLET_REMEDIATION_OPERATION_APPROVAL",
    approvedAt: "2026-07-16T16:06:22.236Z",
    approvedByRole: "PROJECT_OWNER",
    approvalReference: "OWNER_CHAT_APPROVAL_TEST",
    remediationPlan: {
      sha256: PLAN_SHA256,
      plannerSha256: plan.planner.sha256,
      generatedAt: plan.generatedAt,
    },
    executionOrder: ["LEGACY_RESIDUAL_NORMALIZATION", "SYNTHETIC_CREDIT_REVERSAL"],
    operations: {
      legacyResidualNormalization: {
        authorized: true,
        action: "ROUND_HALF_EVEN_WITH_APPEND_ONLY_RESIDUAL_AUDIT",
        roundingMode: "ROUND_HALF_EVEN",
        accountCount: 3,
        valueCount: 6,
        totalResidualPointAttos: "-666666328800",
        normalizationDeltaPointAttos: "666666328800",
      },
      syntheticCreditReversal: {
        authorized: true,
        action: "AUDITED_SYNTHETIC_CREDIT_REVERSAL",
        accountCount: 1,
        sourceCount: 3,
        pointMicros: "3000000",
      },
    },
    constraints: { ...EXPECTED_CONSTRAINTS },
  };
  const webSnapshot = {
    schemaVersion: 2,
    users: [{ userId: webUserId }],
    wallets: [],
    transactions: [],
    links: [],
    outboxes: sources.map((source) => ({
      userId: webUserId,
      sourceTransactionId: source.sourceTransactionId,
      pointMicros: source.pointMicros,
      status: "SENT",
    })),
  };
  const state = {
    webUserId,
    playerId,
    activeSessionId: null,
    pos: "4370",
    remainder: "0",
    mappedPlayers: [{ web_user_id: webUserId, player_id: playerId }],
    credits: sources.map((source) => ({
      transaction_id: source.creditId,
      player_id: playerId,
      type: "web_topup_credit",
      ref: source.sourceTransactionId,
      delta_pos: "1",
      details_json: {
        pointAmountMicros: source.pointMicros,
        pointMicrosRemainderBefore: "0",
        pointMicrosRemainderAfter: "0",
      },
    })),
    transactions: new Map(),
  };
  Object.assign(state, overrides.state || {});
  return {
    client: new FakePgClient(state),
    state,
    plan,
    webSnapshot,
    operationId,
    approval,
    approvalContext: validatePointWalletRemediationOperationApproval(approval, plan, {
      planSha256: PLAN_SHA256,
      approvalSha256: APPROVAL_SHA256,
    }),
  };
}

function options(action) {
  return { action, occurredAt: OCCURRED_AT, referenceKey: REFERENCE_KEY };
}

async function expectCode(callback, code) {
  await assert.rejects(callback, (error) => error && error.message === code);
}

async function run() {
  const primary = fixture();
  const applied = await executeSyntheticReversal(
    primary.client, primary.webSnapshot, primary.plan, primary.approvalContext, options("apply")
  );
  assert.strictEqual(applied.idempotent, false);
  assert.strictEqual(applied.before.pointMicros, "4370000000");
  assert.strictEqual(applied.after.pointMicros, "4367000000");
  assert.strictEqual(primary.state.pos, "4367");
  assert.strictEqual(primary.state.transactions.get(primary.operationId).delta_pos, "-3");

  const replay = await executeSyntheticReversal(
    primary.client, primary.webSnapshot, primary.plan, primary.approvalContext, options("apply")
  );
  assert.strictEqual(replay.idempotent, true);
  assert.strictEqual(primary.state.pos, "4367");
  assert.strictEqual(primary.state.transactions.size, 1);

  primary.state.pos = "4368";
  await expectCode(() => executeSyntheticReversal(
    primary.client, primary.webSnapshot, primary.plan, primary.approvalContext, options("rollback")
  ), "SYNTHETIC_ROLLBACK_BALANCE_DRIFT");
  assert.strictEqual(primary.state.pos, "4368");
  primary.state.pos = "4367";

  const rolledBack = await executeSyntheticReversal(
    primary.client, primary.webSnapshot, primary.plan, primary.approvalContext, options("rollback")
  );
  assert.strictEqual(rolledBack.idempotent, false);
  assert.strictEqual(primary.state.pos, "4370");
  const rollbackId = rollbackOperationId(primary.operationId, PLAN_SHA256);
  assert.strictEqual(primary.state.transactions.get(rollbackId).delta_pos, "3");

  const rollbackReplay = await executeSyntheticReversal(
    primary.client, primary.webSnapshot, primary.plan, primary.approvalContext, options("rollback")
  );
  assert.strictEqual(rollbackReplay.idempotent, true);
  assert.strictEqual(primary.state.pos, "4370");
  assert.strictEqual(primary.state.transactions.size, 2);

  const active = fixture({ state: { activeSessionId: "session-online" } });
  await expectCode(() => executeSyntheticReversal(
    active.client, active.webSnapshot, active.plan, active.approvalContext, options("apply")
  ), "SYNTHETIC_PLAYER_HAS_ACTIVE_SESSION");
  assert.strictEqual(active.state.pos, "4370");
  assert.strictEqual(active.state.transactions.size, 0);
  assert.strictEqual(active.client.rollbackCount, 1);

  const drifted = fixture({ state: { pos: "4369" } });
  await expectCode(() => executeSyntheticReversal(
    drifted.client, drifted.webSnapshot, drifted.plan, drifted.approvalContext, options("apply")
  ), "SYNTHETIC_CURRENT_BALANCE_DRIFT");
  assert.strictEqual(drifted.state.pos, "4369");
  assert.strictEqual(drifted.state.transactions.size, 0);

  const wrongCredit = fixture();
  wrongCredit.state.credits[0].player_id = "another-player";
  await expectCode(() => executeSyntheticReversal(
    wrongCredit.client, wrongCredit.webSnapshot, wrongCredit.plan,
    wrongCredit.approvalContext, options("apply")
  ), "SYNTHETIC_GAME_CREDIT_PLAYER_MISMATCH");
  assert.strictEqual(wrongCredit.state.pos, "4370");
  assert.strictEqual(wrongCredit.state.transactions.size, 0);

  const conflict = fixture();
  conflict.state.transactions.set(conflict.operationId, {
    id: conflict.operationId,
    player_id: conflict.state.playerId,
    type: "point_remediation_reversal",
    ref: conflict.operationId,
    idempotency_key: conflict.operationId,
    request_signature: "f".repeat(64),
  });
  await expectCode(() => executeSyntheticReversal(
    conflict.client, conflict.webSnapshot, conflict.plan, conflict.approvalContext, options("apply")
  ), "STORED_REMEDIATION_SIGNATURE_MISMATCH");
  assert.strictEqual(conflict.state.pos, "4370");

  process.stdout.write(
    "[point-wallet-synthetic-reversal] PASS: apply, replay, fail-closed drift, and rollback.\n"
  );
}

run().catch((error) => {
  process.stderr.write(`[point-wallet-synthetic-reversal] FAIL: ${error.stack || error.message}\n`);
  process.exitCode = 1;
});
