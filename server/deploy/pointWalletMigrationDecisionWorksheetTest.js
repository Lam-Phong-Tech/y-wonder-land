"use strict";

const assert = require("assert");
const {
  buildPointWalletMigrationDecisionWorksheet,
  renderPointWalletMigrationDecisionMarkdown,
  validateReport,
} = require("./pointWalletMigrationDecisionWorksheet");

const REPORT_SHA256 = "a".repeat(64);
const GENERATOR_SHA256 = "b".repeat(64);

function account(overrides = {}) {
  return {
    accountRef: "1".repeat(24),
    gamePlayerRefs: [],
    linkedInWebAuthority: false,
    status: "NO_ACTION",
    blockingIssues: [],
    reviewReasons: [],
    balances: {
      webPointMicros: "0",
      webLockedPointMicros: "0",
      gamePointMicros: null,
      gameOpeningPointMicros: null,
    },
    evidence: {
      webPointTransactions: [],
      outboxes: [],
      sentOutboxMicros: "0",
      matchedGameCreditMicros: "0",
      gameTransactionCount: 0,
      gameLedgerDeltaMicros: "0",
      legacySubMicroNormalization: {
        roundingMode: "ROUND_HALF_EVEN",
        valueCount: 0,
        totalResidualPointAttos: "0",
        maxAbsResidualPointAttos: "0",
      },
    },
    suggestedMigrationMicros: null,
    ...overrides,
  };
}

function fixture() {
  const accounts = [
    account(),
    account({
      accountRef: "2".repeat(24),
      status: "BLOCKED",
      blockingIssues: ["LEGACY_SUB_MICRO_VALUE_PRESENT"],
      reviewReasons: [
        "NONZERO_WEB_POINT_REQUIRES_CLASSIFICATION",
        "WEB_POINT_TRANSACTION_HISTORY_REQUIRES_REVIEW",
      ],
      balances: {
        webPointMicros: "666667",
        webLockedPointMicros: "0",
        gamePointMicros: null,
        gameOpeningPointMicros: null,
      },
      evidence: {
        webPointTransactions: [{
          type: "SWAP",
          currency: "GXL",
          status: "SUCCESS",
          count: 1,
          amountMicros: "316666667",
        }],
        outboxes: [],
        gameTransactionCount: 0,
        gameLedgerDeltaMicros: "0",
        legacySubMicroNormalization: {
          roundingMode: "ROUND_HALF_EVEN",
          valueCount: 2,
          totalResidualPointAttos: "-666666614400",
          maxAbsResidualPointAttos: "333333314400",
        },
      },
    }),
    account({
      accountRef: "3".repeat(24),
      gamePlayerRefs: ["4".repeat(24)],
      status: "MANUAL_RECONCILIATION_REQUIRED",
      reviewReasons: [
        "GAME_OPENING_BALANCE_REQUIRES_CLASSIFICATION",
        "NONZERO_WEB_POINT_REQUIRES_CLASSIFICATION",
        "OUTBOX_WITHOUT_WEB_SOURCE_TRANSACTION",
      ],
      balances: {
        webPointMicros: "12000000",
        webLockedPointMicros: "0",
        gamePointMicros: "5003000000",
        gameOpeningPointMicros: "5000000000",
      },
      evidence: {
        webPointTransactions: [{
          type: "COMMISSION",
          currency: "GXL",
          status: "SUCCESS",
          count: 2,
          amountMicros: "12000000",
        }],
        outboxes: [{
          sourceRef: "5".repeat(24),
          status: "SENT",
          attempts: 2,
          pointMicros: "3000000",
          webSourceTransactionMatched: false,
          gameCreditCount: 1,
        }],
        gameTransactionCount: 4,
        gameLedgerDeltaMicros: "3000000",
        legacySubMicroNormalization: {
          roundingMode: "ROUND_HALF_EVEN",
          valueCount: 0,
          totalResidualPointAttos: "0",
          maxAbsResidualPointAttos: "0",
        },
      },
    }),
    account({
      accountRef: "6".repeat(24),
      status: "UNMAPPED_LEGACY_REVIEW",
      reviewReasons: ["WEB_POINT_TRANSACTION_HISTORY_REQUIRES_REVIEW"],
      evidence: {
        webPointTransactions: [{
          type: "COMMISSION",
          currency: "GXL",
          status: "SUCCESS",
          count: 1,
          amountMicros: "6000000",
        }],
        outboxes: [],
        gameTransactionCount: 0,
        gameLedgerDeltaMicros: "0",
        legacySubMicroNormalization: {
          roundingMode: "ROUND_HALF_EVEN",
          valueCount: 0,
          totalResidualPointAttos: "0",
          maxAbsResidualPointAttos: "0",
        },
      },
    }),
  ];
  return {
    schemaVersion: 2,
    generatedAt: "2026-07-16T13:48:29.089Z",
    referenceKeyId: "1be9104c664c847e",
    mode: "READ_ONLY_DRY_RUN",
    automaticMigrationAllowed: false,
    migrationStatementsGenerated: 0,
    databaseMutationsPerformed: false,
    summary: {
      accountCount: accounts.length,
      statusCounts: {
        NO_ACTION: 1,
        BLOCKED: 1,
        MANUAL_RECONCILIATION_REQUIRED: 1,
        UNMAPPED_LEGACY_REVIEW: 1,
      },
    },
    accounts,
  };
}

function copy(value) {
  return JSON.parse(JSON.stringify(value));
}

function run() {
  const report = fixture();
  validateReport(report);
  const worksheet = buildPointWalletMigrationDecisionWorksheet(report, {
    reportSha256: REPORT_SHA256,
    generatorSha256: GENERATOR_SHA256,
    generatedAt: "2026-07-16T15:00:00.000Z",
  });

  assert.strictEqual(worksheet.mode, "DECISION_WORKSHEET");
  assert.strictEqual(worksheet.automaticMigrationAllowed, false);
  assert.strictEqual(worksheet.migrationStatementsGenerated, 0);
  assert.strictEqual(worksheet.databaseMutationsPerformed, false);
  assert.strictEqual(worksheet.generator.sha256, GENERATOR_SHA256);
  assert.strictEqual(worksheet.summary.reviewAccountCount, 3);
  assert.strictEqual(worksheet.summary.blockedAccountCount, 1);
  assert.strictEqual(worksheet.summary.pendingDecisionCount, 6);
  assert.strictEqual(worksheet.summary.totalReviewWebPointMicros, "12666667");
  assert.strictEqual(worksheet.summary.totalGameOpeningPointMicros, "5000000000");
  assert.strictEqual(worksheet.summary.totalSyntheticCreditMicros, "3000000");
  assert.strictEqual(worksheet.summary.totalRemediatedSyntheticCreditMicros, "0");
  assert.strictEqual(worksheet.summary.classCounts.LEGACY_SUB_MICRO_PRECISION, 1);
  assert.strictEqual(worksheet.summary.classCounts.GAME_OPENING_BALANCE, 1);
  assert.strictEqual(worksheet.summary.classCounts.SYNTHETIC_CREDIT_WITHOUT_WEB_SOURCE, 1);
  assert.strictEqual(worksheet.summary.classCounts.MAPPED_LEGACY_WEB_BALANCE, 1);
  assert.strictEqual(worksheet.summary.classCounts.UNMAPPED_LEGACY_WEB_BALANCE, 1);
  assert.strictEqual(worksheet.summary.classCounts.ZERO_BALANCE_WITH_LEGACY_WEB_HISTORY, 1);

  const mapped = worksheet.accounts.find((item) => item.accountRef === "3".repeat(24));
  assert(mapped.requiredDecisions.some((item) => item.key === "gameOpeningBalanceTreatment"));
  assert(mapped.requiredDecisions.some((item) => item.key === "syntheticCreditTreatment"));
  assert(mapped.requiredDecisions.some((item) => item.key === "legacyWebBalanceTreatment"));
  assert.deepStrictEqual(mapped.facts.unmatchedOutboxSourceRefs, ["5".repeat(24)]);
  assert.strictEqual(mapped.approval.status, "PENDING");

  const remediatedReport = copy(report);
  const remediatedAccount = remediatedReport.accounts.find(
    (item) => item.accountRef === "3".repeat(24)
  );
  remediatedAccount.evidence.outboxes[0].syntheticRemediationStatus = "REVERSED";
  remediatedAccount.evidence.outboxes[0].syntheticRemediationOperationRef = "7".repeat(24);
  remediatedAccount.evidence.outboxes[0].syntheticRemediationRollbackRef = null;
  const remediatedWorksheet = buildPointWalletMigrationDecisionWorksheet(remediatedReport, {
    reportSha256: REPORT_SHA256,
    generatorSha256: GENERATOR_SHA256,
    generatedAt: "2026-07-16T15:05:00.000Z",
  });
  const remediatedMapped = remediatedWorksheet.accounts.find(
    (item) => item.accountRef === "3".repeat(24)
  );
  assert(!remediatedMapped.requiredDecisions.some(
    (item) => item.key === "syntheticCreditTreatment"
  ));
  assert.strictEqual(remediatedMapped.facts.unmatchedOutboxMicros, "0");
  assert.strictEqual(remediatedMapped.facts.remediatedOutboxMicros, "3000000");
  assert.deepStrictEqual(remediatedMapped.facts.remediatedOutboxSourceRefs, ["5".repeat(24)]);
  assert.strictEqual(remediatedWorksheet.summary.pendingDecisionCount, 5);
  assert.strictEqual(remediatedWorksheet.summary.totalSyntheticCreditMicros, "0");
  assert.strictEqual(remediatedWorksheet.summary.totalRemediatedSyntheticCreditMicros, "3000000");
  assert(renderPointWalletMigrationDecisionMarkdown(remediatedWorksheet)
    .includes("Synthetic credit already reversed with audit: **3.000000 Point**"));

  const markdown = renderPointWalletMigrationDecisionMarkdown(worksheet);
  assert(markdown.includes("Point Wallet Migration Decision Worksheet"));
  assert(markdown.includes("5000.000000 Point"));
  assert(markdown.includes("AUDITED_REVERSAL_BEFORE_OPEN"));
  assert(markdown.includes("BLOCKED_PENDING_EXPLICIT_APPROVALS"));
  assert(!markdown.includes("suggestedMigrationMicros"));

  const rawIdentity = copy(report);
  rawIdentity.accounts[0].webUserId = "real-web-user";
  assert.throws(() => validateReport(rawIdentity), /RAW_IDENTIFIER_FIELD_PRESENT/);

  const unsafeMigration = copy(report);
  unsafeMigration.automaticMigrationAllowed = true;
  assert.throws(() => validateReport(unsafeMigration), /REPORT_ALLOWS_AUTOMATIC_MIGRATION/);

  const badReference = copy(report);
  badReference.accounts[0].accountRef = "raw-account-name";
  assert.throws(() => validateReport(badReference), /INVALID_ACCOUNT_REF/);

  const badRemediationEvidence = copy(report);
  badRemediationEvidence.accounts[2].evidence.outboxes[0].syntheticRemediationStatus = "REVERSED";
  assert.throws(() => validateReport(badRemediationEvidence),
    /INVALID_OUTBOX_SYNTHETIC_REMEDIATION_REF/);

  assert.throws(() => buildPointWalletMigrationDecisionWorksheet(report, {
    reportSha256: "not-a-sha",
    generatorSha256: GENERATOR_SHA256,
  }), /INVALID_REPORT_SHA256/);

  assert.throws(() => buildPointWalletMigrationDecisionWorksheet(report, {
    reportSha256: REPORT_SHA256,
    generatorSha256: "not-a-sha",
  }), /INVALID_GENERATOR_SHA256/);

  console.log("[point-wallet-decision-worksheet] PASS: decisions stay pending, anonymized, and non-mutating.");
}

run();
