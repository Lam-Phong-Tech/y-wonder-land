"use strict";

const assert = require("assert");
const crypto = require("crypto");
const fs = require("fs");
const os = require("os");
const path = require("path");
const { spawnSync } = require("child_process");
const {
  buildPointWalletMigrationApprovedWorksheet,
  renderPointWalletMigrationApprovedMarkdown,
  validatePolicy,
  validateWorksheet,
} = require("./pointWalletMigrationDecisionApproval");

const WORKSHEET_SHA256 = "a".repeat(64);
const POLICY_SHA256 = "b".repeat(64);
const APPLICATOR_SHA256 = "c".repeat(64);
const APPROVED_AT = "2026-07-16T15:07:04.247Z";

function decision(key, allowedValues) {
  return {
    key,
    owner: "FINANCE_OWNER",
    status: "PENDING",
    recommendation: allowedValues[0],
    allowedValues,
    rationale: "Fixture rationale.",
  };
}

function account(accountRef, sourceStatus, requiredDecisions, gamePlayerRefs = []) {
  return {
    accountRef,
    gamePlayerRefs,
    linkedInWebAuthority: gamePlayerRefs.length > 0,
    sourceStatus,
    blockingIssues: sourceStatus === "BLOCKED" ? ["LEGACY_SUB_MICRO_VALUE_PRESENT"] : [],
    reviewReasons: ["FIXTURE_REVIEW"],
    reviewClasses: ["FIXTURE_CLASS"],
    facts: {
      webPointMicros: "1000000",
      webLockedPointMicros: "0",
      gamePointMicros: gamePlayerRefs.length > 0 ? "5003000000" : null,
      gameOpeningPointMicros: gamePlayerRefs.length > 0 ? "5000000000" : null,
      unmatchedOutboxMicros: gamePlayerRefs.length > 0 ? "3000000" : "0",
      unmatchedOutboxSourceRefs: [],
      webPointTransactions: [],
      gameTransactionCount: 0,
      gameLedgerDeltaMicros: "0",
      legacySubMicroNormalization: {
        roundingMode: "ROUND_HALF_EVEN",
        valueCount: 0,
        totalResidualPointAttos: "0",
        maxAbsResidualPointAttos: "0",
      },
    },
    requiredDecisions,
    approval: {
      status: "PENDING",
      selectedValues: {},
      approvedBy: null,
      approvedAt: null,
      evidenceReference: null,
      notes: null,
    },
  };
}

function worksheetFixture() {
  const accounts = [
    account("1".repeat(24), "BLOCKED", [
      decision("legacySubMicroTreatment", [
        "APPROVE_ROUND_HALF_EVEN_WITH_RESIDUAL_AUDIT",
        "CORRECT_SOURCE_LEDGER_AND_RERUN",
        "DEFER_ACCOUNT_LINK",
      ]),
      decision("legacyWebBalanceTreatment", [
        "MIGRATE_ONCE_TO_GAME_LEDGER",
        "CLASSIFY_AS_NON_POINT_LEGACY_AND_KEEP_FROZEN",
        "DEFER_ACCOUNT_LINK",
      ]),
    ]),
    account("2".repeat(24), "MANUAL_RECONCILIATION_REQUIRED", [
      decision("gameOpeningBalanceTreatment", [
        "PRESERVE_AS_FULL_WITHDRAWABLE_POINT",
        "AUDITED_REVERSAL_BEFORE_LINK",
        "DEFER_ACCOUNT_LINK",
      ]),
      decision("syntheticCreditTreatment", [
        "AUDITED_REVERSAL_BEFORE_OPEN",
        "PRESERVE_AS_EXPLICIT_TEST_ADJUSTMENT",
        "DEFER_ACCOUNT_LINK",
      ]),
      decision("legacyWebBalanceTreatment", [
        "MIGRATE_ONCE_TO_GAME_LEDGER",
        "CLASSIFY_AS_NON_POINT_LEGACY_AND_KEEP_FROZEN",
        "DEFER_ACCOUNT_LINK",
      ]),
    ], ["3".repeat(24)]),
    account("4".repeat(24), "UNMAPPED_LEGACY_REVIEW", [
      decision("legacyWebHistoryTreatment", [
        "ARCHIVE_HISTORY_WITH_NO_BALANCE_MIGRATION",
        "INVESTIGATE_LEDGER_BEFORE_LINK",
        "DEFER_ACCOUNT_LINK",
      ]),
    ]),
  ];
  return {
    schemaVersion: 1,
    generatedAt: "2026-07-16T14:30:41.000Z",
    mode: "DECISION_WORKSHEET",
    automaticMigrationAllowed: false,
    migrationStatementsGenerated: 0,
    databaseMutationsPerformed: false,
    generator: { sha256: "d".repeat(64) },
    sourceReport: {
      sha256: "e".repeat(64),
      schemaVersion: 2,
      generatedAt: "2026-07-16T13:48:29.089Z",
      referenceKeyId: "1be9104c664c847e",
      accountCount: 159,
      statusCounts: { BLOCKED: 3 },
    },
    summary: {
      reviewAccountCount: accounts.length,
      blockedAccountCount: 1,
      pendingDecisionCount: 6,
      classCounts: { FIXTURE_CLASS: 3 },
      totalReviewWebPointMicros: "3000000",
      totalGameOpeningPointMicros: "5000000000",
      totalSyntheticCreditMicros: "3000000",
      migrationGate: "BLOCKED_PENDING_EXPLICIT_APPROVALS",
    },
    accounts,
  };
}

function policyFixture() {
  return {
    schemaVersion: 1,
    mode: "POINT_WALLET_MIGRATION_POLICY_APPROVAL",
    approvedAt: APPROVED_AT,
    approvedByRole: "PROJECT_OWNER",
    approvalReference: "OWNER_CHAT_APPROVAL_2026-07-16",
    decisions: {
      gameOpeningBalanceTreatment: "DEFER_ACCOUNT_LINK",
      syntheticCreditTreatment: "AUDITED_REVERSAL_BEFORE_OPEN",
      legacyWebBalanceTreatment: "DEFER_ACCOUNT_LINK",
      legacyWebHistoryTreatment: "ARCHIVE_HISTORY_WITH_NO_BALANCE_MIGRATION",
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
}

function copy(value) {
  return JSON.parse(JSON.stringify(value));
}

function sha256(value) {
  return crypto.createHash("sha256").update(value).digest("hex");
}

function runCliTest(worksheet, policy) {
  const tempRoot = fs.mkdtempSync(path.join(os.tmpdir(), "point-wallet-approval-test-"));
  try {
    const worksheetPath = path.join(tempRoot, "worksheet.json");
    const policyPath = path.join(tempRoot, "policy.json");
    const outputPath = path.join(tempRoot, "approved.json");
    const wrongChecksumOutputPath = path.join(tempRoot, "wrong-checksum.json");
    const wrongPolicyChecksumOutputPath = path.join(tempRoot, "wrong-policy-checksum.json");
    const worksheetRaw = `${JSON.stringify(worksheet, null, 2)}\n`;
    const policyRaw = `${JSON.stringify(policy, null, 2)}\n`;
    fs.writeFileSync(worksheetPath, worksheetRaw, "utf8");
    fs.writeFileSync(policyPath, policyRaw, "utf8");
    const commonArgs = [
      __filename.replace(/Test\.js$/, ".js"),
      "--worksheet", worksheetPath,
      "--worksheet-sha256", sha256(worksheetRaw),
      "--policy", policyPath,
      "--policy-sha256", sha256(policyRaw),
      "--generated-at", "2026-07-16T15:30:00.000Z",
      "--format", "json",
    ];
    const first = spawnSync(process.execPath, [...commonArgs, "--output", outputPath], {
      encoding: "utf8",
    });
    assert.strictEqual(first.status, 0, first.stderr);
    const approved = JSON.parse(fs.readFileSync(outputPath, "utf8"));
    assert.strictEqual(approved.summary.approvedDecisionCount, 6);
    assert.strictEqual(approved.summary.migrationGate, "BLOCKED_NO_BALANCE_MIGRATION_AUTHORIZED");

    const overwrite = spawnSync(process.execPath, [...commonArgs, "--output", outputPath], {
      encoding: "utf8",
    });
    assert.notStrictEqual(overwrite.status, 0);
    assert(overwrite.stderr.includes("EEXIST"));

    const wrongChecksumArgs = [...commonArgs];
    const checksumIndex = wrongChecksumArgs.indexOf("--worksheet-sha256") + 1;
    wrongChecksumArgs[checksumIndex] = "f".repeat(64);
    const wrongChecksum = spawnSync(
      process.execPath,
      [...wrongChecksumArgs, "--output", wrongChecksumOutputPath],
      { encoding: "utf8" }
    );
    assert.notStrictEqual(wrongChecksum.status, 0);
    assert(wrongChecksum.stderr.includes("WORKSHEET_SHA256_MISMATCH"));
    assert.strictEqual(fs.existsSync(wrongChecksumOutputPath), false);

    const wrongPolicyChecksumArgs = [...commonArgs];
    const policyChecksumIndex = wrongPolicyChecksumArgs.indexOf("--policy-sha256") + 1;
    wrongPolicyChecksumArgs[policyChecksumIndex] = "f".repeat(64);
    const wrongPolicyChecksum = spawnSync(
      process.execPath,
      [...wrongPolicyChecksumArgs, "--output", wrongPolicyChecksumOutputPath],
      { encoding: "utf8" }
    );
    assert.notStrictEqual(wrongPolicyChecksum.status, 0);
    assert(wrongPolicyChecksum.stderr.includes("POLICY_SHA256_MISMATCH"));
    assert.strictEqual(fs.existsSync(wrongPolicyChecksumOutputPath), false);

    const duplicateArgument = spawnSync(
      process.execPath,
      [...commonArgs, "--format", "markdown", "--output", wrongPolicyChecksumOutputPath],
      { encoding: "utf8" }
    );
    assert.notStrictEqual(duplicateArgument.status, 0);
    assert(duplicateArgument.stderr.includes("DUPLICATE_ARGUMENT"));
  } finally {
    fs.rmSync(tempRoot, { recursive: true, force: true });
  }
}

function run() {
  const worksheet = worksheetFixture();
  const policy = policyFixture();
  assert.strictEqual(validateWorksheet(worksheet), 6);
  validatePolicy(policy, new Set(Object.keys(policy.decisions)));

  const approved = buildPointWalletMigrationApprovedWorksheet(worksheet, policy, {
    worksheetSha256: WORKSHEET_SHA256,
    policySha256: POLICY_SHA256,
    applicatorSha256: APPLICATOR_SHA256,
    generatedAt: "2026-07-16T15:30:00.000Z",
  });
  assert.strictEqual(approved.mode, "APPROVED_DECISION_WORKSHEET");
  assert.strictEqual(approved.automaticMigrationAllowed, false);
  assert.strictEqual(approved.migrationStatementsGenerated, 0);
  assert.strictEqual(approved.databaseMutationsPerformed, false);
  assert.strictEqual(approved.summary.pendingDecisionCount, 0);
  assert.strictEqual(approved.summary.approvedDecisionCount, 6);
  assert.strictEqual(approved.summary.decisionGate, "APPROVED");
  assert.strictEqual(approved.summary.migrationGate, "BLOCKED_NO_BALANCE_MIGRATION_AUTHORIZED");
  assert.strictEqual(approved.operations.accountLink.status, "DEFERRED");
  assert.strictEqual(approved.operations.balanceMigration.status, "NOT_AUTHORIZED");
  assert.strictEqual(approved.operations.syntheticCreditReversal.status, "NOT_EXECUTED");
  assert.strictEqual(approved.operations.legacySubMicroNormalization.status, "NOT_EXECUTED");
  assert.strictEqual(approved.operations.databaseMutation.status, "NOT_EXECUTED");
  assert.strictEqual(approved.operations.deployment.status, "NOT_EXECUTED");
  assert(approved.accounts.every((item) => item.approval.status === "APPROVED"));
  assert(approved.accounts.every((item) =>
    item.requiredDecisions.every((itemDecision) => itemDecision.status === "APPROVED")));
  assert.strictEqual(
    approved.accounts[1].approval.selectedValues.gameOpeningBalanceTreatment,
    "DEFER_ACCOUNT_LINK"
  );

  const markdown = renderPointWalletMigrationApprovedMarkdown(approved);
  assert(markdown.includes("Approved decision items: **6**"));
  assert(markdown.includes("BLOCKED_NO_BALANCE_MIGRATION_AUTHORIZED"));
  assert(markdown.includes("AUDITED_REVERSAL_BEFORE_OPEN"));
  assert(markdown.includes("no database mutation"));

  const invalidSelection = copy(policy);
  invalidSelection.decisions.syntheticCreditTreatment = "EXECUTE_NOW";
  assert.throws(() => buildPointWalletMigrationApprovedWorksheet(worksheet, invalidSelection, {
    worksheetSha256: WORKSHEET_SHA256,
    policySha256: POLICY_SHA256,
    applicatorSha256: APPLICATOR_SHA256,
  }), /POLICY_VALUE_NOT_ALLOWED_syntheticCreditTreatment/);

  const missingDecision = copy(policy);
  delete missingDecision.decisions.legacyWebHistoryTreatment;
  assert.throws(() => buildPointWalletMigrationApprovedWorksheet(worksheet, missingDecision, {
    worksheetSha256: WORKSHEET_SHA256,
    policySha256: POLICY_SHA256,
    applicatorSha256: APPLICATOR_SHA256,
  }), /POLICY_DECISION_KEYS_MISMATCH/);

  const unexpectedDecision = copy(policy);
  unexpectedDecision.decisions.unreviewedAction = "ALLOW";
  assert.throws(() => buildPointWalletMigrationApprovedWorksheet(worksheet, unexpectedDecision, {
    worksheetSha256: WORKSHEET_SHA256,
    policySha256: POLICY_SHA256,
    applicatorSha256: APPLICATOR_SHA256,
  }), /UNEXPECTED_POLICY_DECISION_KEY/);

  const unsafePolicy = copy(policy);
  unsafePolicy.constraints.authorizesDatabaseMutation = true;
  assert.throws(() => buildPointWalletMigrationApprovedWorksheet(worksheet, unsafePolicy, {
    worksheetSha256: WORKSHEET_SHA256,
    policySha256: POLICY_SHA256,
    applicatorSha256: APPLICATOR_SHA256,
  }), /POLICY_AUTHORIZES_DATABASE_MUTATION/);

  const hiddenConstraint = copy(policy);
  hiddenConstraint.constraints.authorizesUnreviewedOperation = true;
  assert.throws(() => buildPointWalletMigrationApprovedWorksheet(worksheet, hiddenConstraint, {
    worksheetSha256: WORKSHEET_SHA256,
    policySha256: POLICY_SHA256,
    applicatorSha256: APPLICATOR_SHA256,
  }), /UNEXPECTED_POLICY_CONSTRAINT/);

  const hiddenTopLevelField = copy(policy);
  hiddenTopLevelField.executeImmediately = true;
  assert.throws(() => buildPointWalletMigrationApprovedWorksheet(worksheet, hiddenTopLevelField, {
    worksheetSha256: WORKSHEET_SHA256,
    policySha256: POLICY_SHA256,
    applicatorSha256: APPLICATOR_SHA256,
  }), /UNEXPECTED_POLICY_FIELD/);

  const rawIdentity = copy(policy);
  rawIdentity.webUserId = "raw-user-id";
  assert.throws(() => buildPointWalletMigrationApprovedWorksheet(worksheet, rawIdentity, {
    worksheetSha256: WORKSHEET_SHA256,
    policySha256: POLICY_SHA256,
    applicatorSha256: APPLICATOR_SHA256,
  }), /RAW_IDENTIFIER_FIELD_PRESENT/);

  const alreadyApproved = copy(worksheet);
  alreadyApproved.accounts[0].requiredDecisions[0].status = "APPROVED";
  assert.throws(() => validateWorksheet(alreadyApproved), /WORKSHEET_DECISION_NOT_PENDING/);

  const duplicateDecision = copy(worksheet);
  duplicateDecision.accounts[0].requiredDecisions.push(
    copy(duplicateDecision.accounts[0].requiredDecisions[0])
  );
  duplicateDecision.summary.pendingDecisionCount += 1;
  assert.throws(() => validateWorksheet(duplicateDecision),
    /DUPLICATE_WORKSHEET_ACCOUNT_DECISION_KEY/);

  const prepopulatedApproval = copy(worksheet);
  prepopulatedApproval.accounts[0].approval.approvedBy = "UNREVIEWED_ACTOR";
  assert.throws(() => validateWorksheet(prepopulatedApproval),
    /WORKSHEET_HAS_PREPOPULATED_APPROVAL_METADATA/);

  runCliTest(worksheet, policy);
  console.log("[point-wallet-decision-approval] PASS: policy is pinned without authorizing operations.");
}

run();
