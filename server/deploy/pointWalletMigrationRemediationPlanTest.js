"use strict";

const assert = require("assert");
const crypto = require("crypto");
const fs = require("fs");
const os = require("os");
const path = require("path");
const { spawnSync } = require("child_process");
const {
  buildPointWalletMigrationRemediationPlan,
  renderPointWalletMigrationRemediationMarkdown,
} = require("./pointWalletMigrationRemediationPlan");
const { buildPointWalletMigrationReport } = require("./pointWalletMigrationReport");
const {
  buildPointWalletMigrationDecisionWorksheet,
} = require("./pointWalletMigrationDecisionWorksheet");
const {
  buildPointWalletMigrationApprovedWorksheet,
} = require("./pointWalletMigrationDecisionApproval");

const REFERENCE_KEY = "remediation-plan-test-key-with-at-least-32-characters";
const APPROVED_SHA256 = "a".repeat(64);
const WEB_SHA256 = "b".repeat(64);
const GAME_SHA256 = "c".repeat(64);
const PLANNER_SHA256 = "d".repeat(64);

function rawFixture() {
  const web = {
    schemaVersion: 2,
    users: [
      { userId: "raw-web-synthetic" },
      { userId: "raw-web-residual" },
    ],
    wallets: [
      {
        userId: "raw-web-synthetic",
        pointMicros: "0",
        pointLegacyResidualAttos: "0",
        lockedPointMicros: "0",
        lockedPointLegacyResidualAttos: "0",
      },
      {
        userId: "raw-web-residual",
        pointMicros: "666667",
        pointLegacyResidualAttos: "-333333314400",
        lockedPointMicros: "0",
        lockedPointLegacyResidualAttos: "0",
      },
    ],
    transactions: [
      {
        transactionId: "raw-web-residual-transaction",
        userId: "raw-web-residual",
        type: "SWAP",
        currency: "GXL",
        status: "SUCCESS",
        amountMicros: "316666667",
        amountLegacyResidualAttos: "-333333300000",
      },
    ],
    outboxes: [1, 2, 3].map((index) => ({
      userId: "raw-web-synthetic",
      sourceTransactionId: `raw-synthetic-source-${index}`,
      pointMicros: "1000000",
      status: "SENT",
      attempts: String(index),
    })),
    links: [
      { userId: "raw-web-synthetic", gamePlayerId: "raw-game-player-synthetic" },
    ],
  };
  const game = {
    schemaVersion: 1,
    players: [
      {
        playerId: "raw-game-player-synthetic",
        webUserId: "raw-web-synthetic",
        point: "5003",
        pointMicrosRemainder: "0",
      },
    ],
    transactions: [1, 2, 3].map((index) => ({
      transactionId: `raw-game-credit-${index}`,
      playerId: "raw-game-player-synthetic",
      type: "web_topup_credit",
      ref: `raw-synthetic-source-${index}`,
      deltaPoint: "1",
      pointAmountMicros: "1000000",
      pointMicrosRemainderBefore: "0",
      pointMicrosRemainderAfter: "0",
    })),
  };
  return { web, game };
}

function approvedFixture(web, game) {
  const report = buildPointWalletMigrationReport(web, game, {
    referenceKey: REFERENCE_KEY,
    generatedAt: "2026-07-16T13:48:29.089Z",
  });
  const worksheet = buildPointWalletMigrationDecisionWorksheet(report, {
    reportSha256: "e".repeat(64),
    generatorSha256: "f".repeat(64),
    generatedAt: "2026-07-16T14:30:41.000Z",
  });
  const policy = {
    schemaVersion: 1,
    mode: "POINT_WALLET_MIGRATION_POLICY_APPROVAL",
    approvedAt: "2026-07-16T15:07:04.247Z",
    approvedByRole: "PROJECT_OWNER",
    approvalReference: "OWNER_CHAT_APPROVAL_2026-07-16",
    decisions: {
      gameOpeningBalanceTreatment: "DEFER_ACCOUNT_LINK",
      syntheticCreditTreatment: "AUDITED_REVERSAL_BEFORE_OPEN",
      legacyWebBalanceTreatment: "DEFER_ACCOUNT_LINK",
      legacySubMicroTreatment: "APPROVE_ROUND_HALF_EVEN_WITH_RESIDUAL_AUDIT",
    },
    constraints: {
      authorizesDatabaseMutation: false,
      authorizesDeployment: false,
      authorizesBalanceMigration: false,
      authorizesSyntheticReversalExecution: false,
      requiresSeparateOperationalChangeApproval: true,
    },
  };
  return buildPointWalletMigrationApprovedWorksheet(worksheet, policy, {
    worksheetSha256: "1".repeat(64),
    policySha256: "2".repeat(64),
    applicatorSha256: "3".repeat(64),
    generatedAt: "2026-07-16T15:23:24.000Z",
  });
}

function options(overrides = {}) {
  return {
    referenceKey: REFERENCE_KEY,
    approvedSha256: APPROVED_SHA256,
    webSnapshotSha256: WEB_SHA256,
    gameSnapshotSha256: GAME_SHA256,
    plannerSha256: PLANNER_SHA256,
    generatedAt: "2026-07-16T16:00:00.000Z",
    ...overrides,
  };
}

function copy(value) {
  return JSON.parse(JSON.stringify(value));
}

function sha256(value) {
  return crypto.createHash("sha256").update(value).digest("hex");
}

function runCliTest(web, game, approved) {
  const tempRoot = fs.mkdtempSync(path.join(os.tmpdir(), "point-remediation-plan-test-"));
  try {
    const webPath = path.join(tempRoot, "web.json");
    const gamePath = path.join(tempRoot, "game.json");
    const approvedPath = path.join(tempRoot, "approved.json");
    const jsonOutputPath = path.join(tempRoot, "plan.json");
    const markdownOutputPath = path.join(tempRoot, "plan.md");
    const invalidOutputPath = path.join(tempRoot, "invalid.json");
    const webRaw = `${JSON.stringify(web, null, 2)}\n`;
    const gameRaw = `${JSON.stringify(game, null, 2)}\n`;
    const approvedRaw = `${JSON.stringify(approved, null, 2)}\n`;
    fs.writeFileSync(webPath, webRaw, "utf8");
    fs.writeFileSync(gamePath, gameRaw, "utf8");
    fs.writeFileSync(approvedPath, approvedRaw, "utf8");
    const common = [
      __filename.replace(/Test\.js$/, ".js"),
      "--approved", approvedPath,
      "--approved-sha256", sha256(approvedRaw),
      "--web-snapshot", webPath,
      "--web-sha256", sha256(webRaw),
      "--game-snapshot", gamePath,
      "--game-sha256", sha256(gameRaw),
      "--generated-at", "2026-07-16T16:00:00.000Z",
    ];
    const env = { ...process.env, POINT_MIGRATION_REPORT_KEY: REFERENCE_KEY };
    const jsonRun = spawnSync(
      process.execPath,
      [...common, "--format", "json", "--output", jsonOutputPath],
      { encoding: "utf8", env }
    );
    assert.strictEqual(jsonRun.status, 0, jsonRun.stderr);
    const plan = JSON.parse(fs.readFileSync(jsonOutputPath, "utf8"));
    assert.strictEqual(plan.summary.syntheticReversalPointMicros, "3000000");
    assert.strictEqual(plan.summary.residualValueCount, 2);
    const markdownRun = spawnSync(
      process.execPath,
      [...common, "--format", "markdown", "--output", markdownOutputPath],
      { encoding: "utf8", env }
    );
    assert.strictEqual(markdownRun.status, 0, markdownRun.stderr);
    assert(fs.readFileSync(markdownOutputPath, "utf8").includes("NOT_AUTHORIZED"));

    const overwrite = spawnSync(
      process.execPath,
      [...common, "--format", "json", "--output", jsonOutputPath],
      { encoding: "utf8", env }
    );
    assert.notStrictEqual(overwrite.status, 0);
    assert(overwrite.stderr.includes("EEXIST"));

    const badChecksumArgs = [...common];
    badChecksumArgs[badChecksumArgs.indexOf("--approved-sha256") + 1] = "9".repeat(64);
    const badChecksum = spawnSync(
      process.execPath,
      [...badChecksumArgs, "--format", "json", "--output", invalidOutputPath],
      { encoding: "utf8", env }
    );
    assert.notStrictEqual(badChecksum.status, 0);
    assert(badChecksum.stderr.includes("APPROVED_ARTIFACT_SHA256_MISMATCH"));
    assert.strictEqual(fs.existsSync(invalidOutputPath), false);

    const duplicateArgument = spawnSync(
      process.execPath,
      [...common, "--format", "json", "--format", "markdown", "--output", invalidOutputPath],
      { encoding: "utf8", env }
    );
    assert.notStrictEqual(duplicateArgument.status, 0);
    assert(duplicateArgument.stderr.includes("DUPLICATE_ARGUMENT"));
  } finally {
    fs.rmSync(tempRoot, { recursive: true, force: true });
  }
}

function validateRunnerSource() {
  const source = fs.readFileSync(
    path.join(__dirname, "run-point-wallet-remediation-dry-run.sh"),
    "utf8"
  );
  assert(source.includes("web.first.raw.json") && source.includes("web.second.raw.json"));
  assert(source.includes("game.first.raw.json") && source.includes("game.second.raw.json"));
  assert(source.includes('cmp -s "${web_snapshot_first}" "${web_snapshot_second}"'));
  assert(source.includes('cmp -s "${game_snapshot_first}" "${game_snapshot_second}"'));
  assert(source.includes('rm -rf -- "${run_root}"'));
  assert(source.includes("env -u POINT_MIGRATION_REPORT_KEY"));
  assert(source.includes("-u PGPASSWORD"));
  assert(source.includes("POINT_MIGRATION_GAME_EXPORT_USER"));
  assert(source.includes("Approved decision artifact checksum mismatch."));
  assert(source.includes("DATABASE_MUTATIONS_PERFORMED=no"));
  assert(source.includes("EXECUTION_STATEMENTS_GENERATED=0"));
  assert(source.includes("RAW_SNAPSHOTS_RETAINED=no"));
  assert(!/\b(insert|update|delete|psql)\b/i.test(source),
    "Runner contains a database write primitive.");
}

function run() {
  const { web, game } = rawFixture();
  const approved = approvedFixture(web, game);
  const plan = buildPointWalletMigrationRemediationPlan(web, game, approved, options());
  assert.strictEqual(plan.mode, "READ_ONLY_REMEDIATION_PLAN");
  assert.strictEqual(plan.automaticExecutionAllowed, false);
  assert.strictEqual(plan.executionStatementsGenerated, 0);
  assert.strictEqual(plan.databaseMutationsPerformed, false);
  assert.strictEqual(plan.containsRawIdentities, false);
  assert.strictEqual(plan.summary.syntheticReversalAccountCount, 1);
  assert.strictEqual(plan.summary.syntheticReversalSourceCount, 3);
  assert.strictEqual(plan.summary.syntheticReversalPointMicros, "3000000");
  assert.strictEqual(plan.summary.residualAccountCount, 1);
  assert.strictEqual(plan.summary.residualValueCount, 2);
  assert.strictEqual(plan.summary.totalResidualPointAttos, "-666666614400");
  assert.strictEqual(plan.summary.normalizationDeltaPointAttos, "666666614400");
  assert.strictEqual(plan.summary.currentBlockedAccountCount, 1);
  assert.strictEqual(plan.summary.expectedBlockedAccountCountAfterAuthorizedResidualNormalization, 0);
  assert.strictEqual(plan.summary.postExecutionGate, "FRESH_READ_ONLY_DRY_RUN_REQUIRED");
  assert.strictEqual(plan.authorization.syntheticReversal, "NOT_AUTHORIZED");
  assert.strictEqual(plan.authorization.residualNormalization, "NOT_AUTHORIZED");
  const synthetic = plan.syntheticReversalPlans[0];
  assert.strictEqual(synthetic.currentGamePointMicros, "5003000000");
  assert.strictEqual(synthetic.reversalPointMicros, "3000000");
  assert.strictEqual(synthetic.expectedGamePointMicrosAfter, "5000000000");
  assert.strictEqual(synthetic.sources.length, 3);
  assert(synthetic.proposedOperationId.startsWith("point-remediation:"));
  const residual = plan.legacyResidualPlans[0];
  assert.strictEqual(residual.valueCount, 2);
  assert.strictEqual(residual.values.filter((value) => value.sourceKind === "WEB_WALLET_BALANCE").length, 1);
  assert.strictEqual(residual.values.filter((value) => value.sourceKind === "WEB_TRANSACTION_AMOUNT").length, 1);
  assert(residual.values.every((value) => value.operationStatus === "NOT_AUTHORIZED"));

  const serialized = JSON.stringify(plan);
  for (const rawIdentity of [
    "raw-web-synthetic",
    "raw-web-residual",
    "raw-game-player-synthetic",
    "raw-synthetic-source-1",
    "raw-web-residual-transaction",
    "raw-game-credit-1",
  ]) {
    assert(!serialized.includes(rawIdentity), `Raw identity leaked: ${rawIdentity}`);
  }
  const markdown = renderPointWalletMigrationRemediationMarkdown(plan);
  assert(markdown.includes("3.000000 Point"));
  assert(markdown.includes("READY_FOR_SEPARATE_OPERATIONAL_APPROVAL"));
  assert(markdown.includes("no SQL, database mutation"));

  const repeated = buildPointWalletMigrationRemediationPlan(web, game, approved, options({
    generatedAt: "2026-07-16T17:00:00.000Z",
  }));
  assert.strictEqual(
    repeated.syntheticReversalPlans[0].proposedOperationId,
    synthetic.proposedOperationId
  );
  assert.strictEqual(
    repeated.legacyResidualPlans[0].proposedOperationId,
    residual.proposedOperationId
  );

  const balanceDrift = copy(game);
  balanceDrift.players[0].point = "5004";
  assert.throws(() => buildPointWalletMigrationRemediationPlan(
    web,
    balanceDrift,
    approved,
    options()
  ), /DRIFT/);

  const nowFunded = copy(web);
  nowFunded.transactions.push({
    transactionId: "raw-synthetic-source-1",
    userId: "raw-web-synthetic",
    type: "SWAP",
    currency: "GXL",
    status: "SUCCESS",
    amountMicros: "1000000",
    amountLegacyResidualAttos: "0",
  });
  assert.throws(() => buildPointWalletMigrationRemediationPlan(
    nowFunded,
    game,
    approved,
    options()
  ), /DRIFT/);

  const residualDrift = copy(web);
  residualDrift.wallets[1].pointLegacyResidualAttos = "-333333314399";
  assert.throws(() => buildPointWalletMigrationRemediationPlan(
    residualDrift,
    game,
    approved,
    options()
  ), /DRIFT/);

  const duplicateCredit = copy(game);
  duplicateCredit.transactions.push({
    ...duplicateCredit.transactions[0],
    transactionId: "raw-game-credit-duplicate",
  });
  assert.throws(() => buildPointWalletMigrationRemediationPlan(
    web,
    duplicateCredit,
    approved,
    options()
  ), /DRIFT/);

  const unsafeApproved = copy(approved);
  unsafeApproved.operations.syntheticCreditReversal.status = "EXECUTED";
  assert.throws(() => buildPointWalletMigrationRemediationPlan(
    web,
    game,
    unsafeApproved,
    options()
  ), /APPROVED_OPERATION_STATUS_INVALID/);

  const hiddenApprovedField = copy(approved);
  hiddenApprovedField.executeImmediately = true;
  assert.throws(() => buildPointWalletMigrationRemediationPlan(
    web,
    game,
    hiddenApprovedField,
    options()
  ), /UNEXPECTED_APPROVED_ARTIFACT_FIELD/);

  assert.throws(() => buildPointWalletMigrationRemediationPlan(web, game, approved, options({
    referenceKey: "different-reference-key-with-at-least-32-characters",
  })), /REFERENCE_KEY_ID_MISMATCH/);

  runCliTest(web, game, approved);
  validateRunnerSource();
  console.log("[point-wallet-remediation-plan] PASS: exact remediation stays anonymized and non-mutating.");
}

run();
