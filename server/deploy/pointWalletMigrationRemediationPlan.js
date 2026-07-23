"use strict";

const crypto = require("crypto");
const fs = require("fs");
const path = require("path");
const {
  buildPointWalletMigrationReport,
  normalizeGameSnapshot,
  normalizeWebSnapshot,
} = require("./pointWalletMigrationReport");

const POINT_MICROS = 1_000_000n;
const MICRO_POINT_ATTOS = 1_000_000_000_000n;
const REPORT_DOMAIN = "ywonder-point-migration-report-v1";
const OPERATION_DOMAIN = "ywonder-point-remediation-operation-v1";
const REF_PATTERN = /^[a-f0-9]{24}$/;
const SHA256_PATTERN = /^[a-f0-9]{64}$/;
const FORBIDDEN_RAW_IDENTITY_KEYS = new Set([
  "email",
  "phone",
  "playerId",
  "player_id",
  "sourceTransactionId",
  "transactionId",
  "userId",
  "user_id",
  "username",
  "webUserId",
  "web_user_id",
]);

function assertCondition(condition, code) {
  if (!condition) throw new Error(code);
}

function objectValue(value, field) {
  assertCondition(value && typeof value === "object" && !Array.isArray(value),
    `INVALID_${field}`);
  return value;
}

function arrayValue(value, field) {
  assertCondition(Array.isArray(value), `INVALID_${field}`);
  return value;
}

function integerValue(value, field, options = {}) {
  const text = String(value == null ? "" : value).trim();
  assertCondition(/^-?(0|[1-9]\d*)$/.test(text), `INVALID_${field}`);
  const parsed = BigInt(text);
  if (options.nonNegative) assertCondition(parsed >= 0n, `INVALID_${field}`);
  if (options.positive) assertCondition(parsed > 0n, `INVALID_${field}`);
  return parsed;
}

function sha256Value(value, field) {
  const normalized = String(value || "").toLowerCase();
  assertCondition(SHA256_PATTERN.test(normalized), `INVALID_${field}_SHA256`);
  return normalized;
}

function assertNoRawIdentityFields(value) {
  if (Array.isArray(value)) {
    for (const child of value) assertNoRawIdentityFields(child);
    return;
  }
  if (!value || typeof value !== "object") return;
  for (const [key, child] of Object.entries(value)) {
    assertCondition(!FORBIDDEN_RAW_IDENTITY_KEYS.has(key), "RAW_IDENTIFIER_FIELD_PRESENT");
    assertNoRawIdentityFields(child);
  }
}

function publicRef(referenceKey, kind, rawValue) {
  return crypto
    .createHmac("sha256", referenceKey)
    .update(`${REPORT_DOMAIN}\0${kind}\0${rawValue}`, "utf8")
    .digest("hex")
    .slice(0, 24);
}

function operationId(approvedSha256, accountRef, evidenceRefs) {
  const digest = crypto.createHash("sha256")
    .update(`${OPERATION_DOMAIN}\0${approvedSha256}\0${accountRef}\0${evidenceRefs.join(",")}`)
    .digest("hex")
    .slice(0, 32);
  return `point-remediation:${digest}`;
}

function stableValue(value) {
  if (Array.isArray(value)) return value.map(stableValue);
  if (!value || typeof value !== "object") return value;
  return Object.fromEntries(Object.keys(value).sort().map((key) => [key, stableValue(value[key])]));
}

function stableJson(value) {
  return JSON.stringify(stableValue(value));
}

function validateApprovedArtifact(approved) {
  objectValue(approved, "APPROVED_ARTIFACT");
  assertNoRawIdentityFields(approved);
  const approvedKeys = Object.keys(approved).sort();
  const expectedApprovedKeys = [
    "accounts",
    "approvalApplicator",
    "automaticMigrationAllowed",
    "databaseMutationsPerformed",
    "generatedAt",
    "generator",
    "migrationStatementsGenerated",
    "mode",
    "operations",
    "policyApproval",
    "schemaVersion",
    "sourceReport",
    "sourceWorksheet",
    "summary",
  ];
  assertCondition(stableJson(approvedKeys) === stableJson(expectedApprovedKeys),
    "UNEXPECTED_APPROVED_ARTIFACT_FIELD");
  assertCondition(approved.schemaVersion === 1, "UNSUPPORTED_APPROVED_ARTIFACT_SCHEMA_VERSION");
  assertCondition(approved.mode === "APPROVED_DECISION_WORKSHEET", "INVALID_APPROVED_MODE");
  assertCondition(Number.isFinite(Date.parse(approved.generatedAt)),
    "INVALID_APPROVED_ARTIFACT_GENERATED_AT");
  assertCondition(approved.automaticMigrationAllowed === false,
    "APPROVED_ARTIFACT_ALLOWS_AUTOMATIC_MIGRATION");
  assertCondition(approved.migrationStatementsGenerated === 0,
    "APPROVED_ARTIFACT_CONTAINS_MIGRATION_STATEMENTS");
  assertCondition(approved.databaseMutationsPerformed === false,
    "APPROVED_ARTIFACT_DATABASE_MUTATION_FLAGGED");

  const sourceReport = objectValue(approved.sourceReport, "APPROVED_SOURCE_REPORT");
  sha256Value(sourceReport.sha256, "APPROVED_SOURCE_REPORT");
  assertCondition(sourceReport.schemaVersion === 2, "UNSUPPORTED_APPROVED_SOURCE_REPORT_SCHEMA");
  assertCondition(typeof sourceReport.referenceKeyId === "string"
    && /^[a-f0-9]{16}$/.test(sourceReport.referenceKeyId),
  "INVALID_APPROVED_REFERENCE_KEY_ID");
  assertCondition(Number.isInteger(sourceReport.accountCount) && sourceReport.accountCount >= 0,
    "INVALID_APPROVED_SOURCE_ACCOUNT_COUNT");
  objectValue(sourceReport.statusCounts, "APPROVED_SOURCE_STATUS_COUNTS");

  const sourceWorksheet = objectValue(approved.sourceWorksheet, "APPROVED_SOURCE_WORKSHEET");
  sha256Value(sourceWorksheet.sha256, "APPROVED_SOURCE_WORKSHEET");
  sha256Value(sourceWorksheet.generatorSha256, "APPROVED_WORKSHEET_GENERATOR");
  sha256Value(objectValue(approved.approvalApplicator, "APPROVAL_APPLICATOR").sha256,
    "APPROVAL_APPLICATOR");
  const policyApproval = objectValue(approved.policyApproval, "POLICY_APPROVAL");
  sha256Value(policyApproval.sha256, "POLICY_APPROVAL");
  const constraints = objectValue(policyApproval.constraints, "POLICY_CONSTRAINTS");
  const constraintKeys = Object.keys(constraints).sort();
  const expectedConstraintKeys = [
    "authorizesBalanceMigration",
    "authorizesDatabaseMutation",
    "authorizesDeployment",
    "authorizesSyntheticReversalExecution",
    "requiresSeparateOperationalChangeApproval",
  ];
  assertCondition(stableJson(constraintKeys) === stableJson(expectedConstraintKeys),
    "UNEXPECTED_POLICY_CONSTRAINT");
  assertCondition(constraints.authorizesDatabaseMutation === false,
    "POLICY_AUTHORIZES_DATABASE_MUTATION");
  assertCondition(constraints.authorizesDeployment === false, "POLICY_AUTHORIZES_DEPLOYMENT");
  assertCondition(constraints.authorizesBalanceMigration === false,
    "POLICY_AUTHORIZES_BALANCE_MIGRATION");
  assertCondition(constraints.authorizesSyntheticReversalExecution === false,
    "POLICY_AUTHORIZES_REVERSAL_EXECUTION");
  assertCondition(constraints.requiresSeparateOperationalChangeApproval === true,
    "POLICY_MISSING_OPERATIONAL_APPROVAL_GATE");

  const summary = objectValue(approved.summary, "APPROVED_SUMMARY");
  assertCondition(summary.decisionGate === "APPROVED", "APPROVED_DECISION_GATE_NOT_APPROVED");
  assertCondition(summary.pendingDecisionCount === 0, "APPROVED_DECISIONS_STILL_PENDING");
  assertCondition(summary.migrationGate === "BLOCKED_NO_BALANCE_MIGRATION_AUTHORIZED",
    "APPROVED_MIGRATION_GATE_NOT_BLOCKED");
  const operations = objectValue(approved.operations, "APPROVED_OPERATIONS");
  const expectedOperations = {
    accountLink: "DEFERRED",
    balanceMigration: "NOT_AUTHORIZED",
    syntheticCreditReversal: "NOT_EXECUTED",
    legacySubMicroNormalization: "NOT_EXECUTED",
    databaseMutation: "NOT_EXECUTED",
    deployment: "NOT_EXECUTED",
  };
  assertCondition(stableJson(Object.keys(operations).sort())
    === stableJson(Object.keys(expectedOperations).sort()), "UNEXPECTED_APPROVED_OPERATION");
  const selectedPolicy = new Map();
  for (const [key, status] of Object.entries(expectedOperations)) {
    assertCondition(objectValue(operations[key], `APPROVED_OPERATION_${key}`).status === status,
      `APPROVED_OPERATION_STATUS_INVALID_${key}`);
  }

  const accounts = arrayValue(approved.accounts, "APPROVED_ACCOUNTS");
  assertCondition(summary.reviewAccountCount === accounts.length,
    "APPROVED_REVIEW_ACCOUNT_COUNT_MISMATCH");
  const seenAccountRefs = new Set();
  let decisionCount = 0;
  for (const account of accounts) {
    objectValue(account, "APPROVED_ACCOUNT");
    assertCondition(REF_PATTERN.test(String(account.accountRef || "")),
      "INVALID_APPROVED_ACCOUNT_REF");
    assertCondition(!seenAccountRefs.has(account.accountRef), "DUPLICATE_APPROVED_ACCOUNT_REF");
    seenAccountRefs.add(account.accountRef);
    assertCondition(Array.isArray(account.gamePlayerRefs)
      && account.gamePlayerRefs.every((ref) => REF_PATTERN.test(String(ref))),
    "INVALID_APPROVED_GAME_PLAYER_REFS");
    objectValue(account.facts, "APPROVED_ACCOUNT_FACTS");
    const approval = objectValue(account.approval, "APPROVED_ACCOUNT_APPROVAL");
    assertCondition(approval.status === "APPROVED", "ACCOUNT_POLICY_NOT_APPROVED");
    const selectedValues = objectValue(approval.selectedValues, "APPROVED_SELECTED_VALUES");
    const seenDecisionKeys = new Set();
    for (const decision of arrayValue(account.requiredDecisions, "APPROVED_REQUIRED_DECISIONS")) {
      objectValue(decision, "APPROVED_DECISION");
      assertCondition(typeof decision.key === "string" && decision.key.length > 0,
        "INVALID_APPROVED_DECISION_KEY");
      assertCondition(!seenDecisionKeys.has(decision.key), "DUPLICATE_APPROVED_DECISION_KEY");
      seenDecisionKeys.add(decision.key);
      assertCondition(decision.status === "APPROVED", "DECISION_NOT_APPROVED");
      assertCondition(Array.isArray(decision.allowedValues)
        && decision.allowedValues.includes(decision.selectedValue),
      "APPROVED_DECISION_VALUE_NOT_ALLOWED");
      assertCondition(selectedValues[decision.key] === decision.selectedValue,
        "APPROVED_SELECTED_VALUE_MISMATCH");
      if (selectedPolicy.has(decision.key)) {
        assertCondition(selectedPolicy.get(decision.key) === decision.selectedValue,
          "INCONSISTENT_APPROVED_POLICY_SELECTION");
      } else {
        selectedPolicy.set(decision.key, decision.selectedValue);
      }
      decisionCount += 1;
    }
    assertCondition(Object.keys(selectedValues).length === seenDecisionKeys.size,
      "APPROVED_SELECTED_VALUE_COUNT_MISMATCH");
  }
  assertCondition(summary.approvedDecisionCount === decisionCount,
    "APPROVED_DECISION_COUNT_MISMATCH");
  const requiredSafeSelections = {
    gameOpeningBalanceTreatment: "DEFER_ACCOUNT_LINK",
    syntheticCreditTreatment: "AUDITED_REVERSAL_BEFORE_OPEN",
    legacyWebBalanceTreatment: "DEFER_ACCOUNT_LINK",
    legacyWebHistoryTreatment: "ARCHIVE_HISTORY_WITH_NO_BALANCE_MIGRATION",
    legacySubMicroTreatment: "APPROVE_ROUND_HALF_EVEN_WITH_RESIDUAL_AUDIT",
  };
  for (const [key, expectedValue] of Object.entries(requiredSafeSelections)) {
    if (selectedPolicy.has(key)) {
      assertCondition(selectedPolicy.get(key) === expectedValue,
        `UNSAFE_APPROVED_POLICY_SELECTION_${key}`);
    }
  }
}

function reportFacts(account) {
  const unmatchedOutboxes = account.evidence.outboxes.filter(
    (outbox) => outbox.webSourceTransactionMatched === false
  );
  const unmatchedOutboxMicros = unmatchedOutboxes.reduce(
    (sum, outbox) => sum + integerValue(outbox.pointMicros, "OUTBOX_POINT_MICROS"),
    0n
  );
  return {
    webPointMicros: account.balances.webPointMicros,
    webLockedPointMicros: account.balances.webLockedPointMicros,
    gamePointMicros: account.balances.gamePointMicros,
    gameOpeningPointMicros: account.balances.gameOpeningPointMicros,
    unmatchedOutboxMicros: unmatchedOutboxMicros.toString(),
    unmatchedOutboxSourceRefs: unmatchedOutboxes.map((outbox) => outbox.sourceRef).sort(),
    webPointTransactions: account.evidence.webPointTransactions,
    gameTransactionCount: account.evidence.gameTransactionCount,
    gameLedgerDeltaMicros: account.evidence.gameLedgerDeltaMicros,
    legacySubMicroNormalization: account.evidence.legacySubMicroNormalization,
  };
}

function validateCurrentReportAgainstApproval(currentReport, approved) {
  assertCondition(currentReport.referenceKeyId === approved.sourceReport.referenceKeyId,
    "REFERENCE_KEY_ID_MISMATCH");
  assertCondition(currentReport.summary.accountCount === approved.sourceReport.accountCount,
    "SOURCE_ACCOUNT_COUNT_DRIFT");
  assertCondition(stableJson(currentReport.summary.statusCounts)
    === stableJson(approved.sourceReport.statusCounts), "SOURCE_STATUS_COUNTS_DRIFT");
  const currentReviewAccounts = currentReport.accounts.filter((account) => account.status !== "NO_ACTION");
  assertCondition(currentReviewAccounts.length === approved.accounts.length,
    "REVIEW_ACCOUNT_COUNT_DRIFT");
  const currentByRef = new Map(currentReviewAccounts.map((account) => [account.accountRef, account]));
  assertCondition(currentByRef.size === currentReviewAccounts.length, "CURRENT_REPORT_DUPLICATE_ACCOUNT_REF");
  for (const approvedAccount of approved.accounts) {
    const current = currentByRef.get(approvedAccount.accountRef);
    assertCondition(Boolean(current), "APPROVED_ACCOUNT_MISSING_FROM_CURRENT_REPORT");
    assertCondition(stableJson(current.gamePlayerRefs) === stableJson(approvedAccount.gamePlayerRefs),
      "APPROVED_GAME_PLAYER_REFS_DRIFT");
    assertCondition(current.linkedInWebAuthority === approvedAccount.linkedInWebAuthority,
      "APPROVED_LINK_STATUS_DRIFT");
    assertCondition(current.status === approvedAccount.sourceStatus, "APPROVED_SOURCE_STATUS_DRIFT");
    assertCondition(stableJson(current.blockingIssues) === stableJson(approvedAccount.blockingIssues),
      "APPROVED_BLOCKING_ISSUES_DRIFT");
    assertCondition(stableJson(current.reviewReasons) === stableJson(approvedAccount.reviewReasons),
      "APPROVED_REVIEW_REASONS_DRIFT");
    assertCondition(stableJson(reportFacts(current)) === stableJson(approvedAccount.facts),
      "APPROVED_ACCOUNT_FACTS_DRIFT");
  }
}

function selectedDecision(account, key) {
  const decision = account.requiredDecisions.find((item) => item.key === key);
  return decision ? decision.selectedValue : null;
}

function buildRawUserIndex(web, game, referenceKey) {
  const rawIds = new Set(web.userIds);
  for (const webUserId of game.playersByWebUserId.keys()) rawIds.add(webUserId);
  const byRef = new Map();
  for (const rawId of rawIds) {
    const accountRef = publicRef(referenceKey, "web-user", rawId);
    assertCondition(!byRef.has(accountRef), "ACCOUNT_REFERENCE_COLLISION");
    byRef.set(accountRef, rawId);
  }
  return byRef;
}

function buildSyntheticPlans(approved, web, game, rawUserByRef, approvedSha256, referenceKey) {
  const plans = [];
  for (const account of approved.accounts) {
    const treatment = selectedDecision(account, "syntheticCreditTreatment");
    if (treatment == null) continue;
    assertCondition(treatment === "AUDITED_REVERSAL_BEFORE_OPEN",
      "SYNTHETIC_POLICY_DOES_NOT_REQUIRE_REVERSAL");
    const rawUserId = rawUserByRef.get(account.accountRef);
    assertCondition(Boolean(rawUserId), "SYNTHETIC_ACCOUNT_RAW_MAPPING_MISSING");
    const players = game.playersByWebUserId.get(rawUserId) || [];
    assertCondition(players.length === 1, "SYNTHETIC_ACCOUNT_GAME_MAPPING_NOT_UNIQUE");
    const player = players[0];
    const gamePlayerRef = publicRef(referenceKey, "player", player.playerId);
    assertCondition(account.gamePlayerRefs.length === 1 && account.gamePlayerRefs[0] === gamePlayerRef,
      "SYNTHETIC_GAME_PLAYER_REF_MISMATCH");
    const expectedSourceRefs = new Set(account.facts.unmatchedOutboxSourceRefs);
    const outboxes = (web.outboxes.get(rawUserId) || []).filter((outbox) =>
      expectedSourceRefs.has(publicRef(referenceKey, "source", outbox.sourceTransactionId)));
    assertCondition(outboxes.length === expectedSourceRefs.size,
      "SYNTHETIC_OUTBOX_EVIDENCE_COUNT_MISMATCH");
    const sources = [];
    let reversalMicros = 0n;
    for (const outbox of outboxes) {
      const sourceRef = publicRef(referenceKey, "source", outbox.sourceTransactionId);
      assertCondition(!web.transactionsById.has(outbox.sourceTransactionId),
        "SYNTHETIC_SOURCE_NOW_HAS_WEB_TRANSACTION");
      assertCondition(outbox.status === "SENT", "SYNTHETIC_OUTBOX_NOT_SENT");
      const credits = game.webCreditsByRef.get(outbox.sourceTransactionId) || [];
      assertCondition(credits.length === 1, "SYNTHETIC_GAME_CREDIT_COUNT_NOT_ONE");
      const credit = credits[0];
      assertCondition(credit.playerId === player.playerId, "SYNTHETIC_GAME_CREDIT_PLAYER_MISMATCH");
      assertCondition(credit.pointAmountMicros === outbox.pointMicros,
        "SYNTHETIC_GAME_CREDIT_AMOUNT_MISMATCH");
      assertCondition(credit.pointMicrosRemainderBefore != null
        && credit.pointMicrosRemainderAfter != null,
      "SYNTHETIC_GAME_CREDIT_REMAINDER_EVIDENCE_MISSING");
      const total = credit.pointMicrosRemainderBefore + credit.pointAmountMicros;
      assertCondition(credit.deltaPoint === total / POINT_MICROS
        && credit.pointMicrosRemainderAfter === total % POINT_MICROS,
      "SYNTHETIC_GAME_CREDIT_REMAINDER_MISMATCH");
      reversalMicros += outbox.pointMicros;
      sources.push({
        sourceRef,
        gameCreditRef: publicRef(referenceKey, "game-transaction", credit.transactionId),
        pointMicros: outbox.pointMicros.toString(),
        evidenceStatus: "VERIFIED_DELIVERED_WITHOUT_WEB_TRANSACTION",
      });
    }
    sources.sort((a, b) => a.sourceRef.localeCompare(b.sourceRef));
    assertCondition(reversalMicros === integerValue(account.facts.unmatchedOutboxMicros,
      "APPROVED_SYNTHETIC_MICROS"), "SYNTHETIC_REVERSAL_TOTAL_MISMATCH");
    assertCondition(player.pointMicros >= reversalMicros, "SYNTHETIC_REVERSAL_EXCEEDS_BALANCE");
    const expectedMicros = player.pointMicros - reversalMicros;
    const proposedOperationId = operationId(
      approvedSha256,
      account.accountRef,
      sources.map((source) => source.sourceRef)
    );
    const allGameTransactions = [...game.transactions.values()].flat();
    assertCondition(!allGameTransactions.some((transaction) => transaction.ref === proposedOperationId),
      "SYNTHETIC_REVERSAL_OPERATION_ALREADY_PRESENT");
    plans.push({
      accountRef: account.accountRef,
      gamePlayerRef,
      policy: treatment,
      proposedOperationId,
      operationStatus: "NOT_AUTHORIZED",
      currentGamePointMicros: player.pointMicros.toString(),
      reversalPointMicros: reversalMicros.toString(),
      expectedGamePointMicrosAfter: expectedMicros.toString(),
      currentGameWholePoint: player.point.toString(),
      currentGameRemainderMicros: player.pointMicrosRemainder.toString(),
      expectedGameWholePointAfter: (expectedMicros / POINT_MICROS).toString(),
      expectedGameRemainderMicrosAfter: (expectedMicros % POINT_MICROS).toString(),
      sources,
      requiredExecutionPreconditions: [
        "SEPARATE_OPERATIONAL_APPROVAL",
        "FRESH_DOUBLE_SNAPSHOT_MATCH",
        "IMMUTABLE_OPERATION_ID_UNUSED",
        "ATOMIC_BALANCE_AND_LEDGER_TRANSACTION",
        "POST_WRITE_READ_ONLY_RECONCILIATION",
      ],
    });
  }
  return plans.sort((a, b) => a.accountRef.localeCompare(b.accountRef));
}

function residualValue(value) {
  const roundedMicros = value.roundedMicros;
  const residualAttos = value.residualAttos;
  const normalizedAttos = roundedMicros * MICRO_POINT_ATTOS;
  const sourceAttos = normalizedAttos + residualAttos;
  return {
    ...value.output,
    roundedPointMicros: roundedMicros.toString(),
    sourcePointAttos: sourceAttos.toString(),
    normalizedPointAttos: normalizedAttos.toString(),
    residualPointAttos: residualAttos.toString(),
    normalizationDeltaPointAttos: (-residualAttos).toString(),
    proposedTreatment: "ROUND_HALF_EVEN_WITH_APPEND_ONLY_RESIDUAL_AUDIT",
    operationStatus: "NOT_AUTHORIZED",
  };
}

function buildResidualPlans(approved, web, rawUserByRef, approvedSha256, referenceKey) {
  const plans = [];
  for (const account of approved.accounts) {
    const treatment = selectedDecision(account, "legacySubMicroTreatment");
    if (treatment == null) continue;
    assertCondition(treatment === "APPROVE_ROUND_HALF_EVEN_WITH_RESIDUAL_AUDIT",
      "RESIDUAL_POLICY_NOT_APPROVED");
    const rawUserId = rawUserByRef.get(account.accountRef);
    assertCondition(Boolean(rawUserId), "RESIDUAL_ACCOUNT_RAW_MAPPING_MISSING");
    const values = [];
    const wallet = web.wallets.get(rawUserId);
    if (wallet && wallet.pointLegacyResidualAttos !== 0n) {
      values.push(residualValue({
        roundedMicros: wallet.pointMicros,
        residualAttos: wallet.pointLegacyResidualAttos,
        output: {
          valueRef: publicRef(referenceKey, "legacy-value", `${rawUserId}\0wallet\0balanceGXL`),
          sourceKind: "WEB_WALLET_BALANCE",
          sourceField: "balanceGXL",
        },
      }));
    }
    if (wallet && wallet.lockedPointLegacyResidualAttos !== 0n) {
      values.push(residualValue({
        roundedMicros: wallet.lockedPointMicros,
        residualAttos: wallet.lockedPointLegacyResidualAttos,
        output: {
          valueRef: publicRef(referenceKey, "legacy-value", `${rawUserId}\0wallet\0lockedGXL`),
          sourceKind: "WEB_WALLET_LOCKED_BALANCE",
          sourceField: "lockedGXL",
        },
      }));
    }
    for (const transaction of web.transactions.get(rawUserId) || []) {
      if (transaction.amountLegacyResidualAttos === 0n) continue;
      values.push(residualValue({
        roundedMicros: transaction.amountMicros,
        residualAttos: transaction.amountLegacyResidualAttos,
        output: {
          valueRef: publicRef(
            referenceKey,
            "legacy-value",
            `${rawUserId}\0transaction\0${transaction.transactionId}\0amount`
          ),
          transactionRef: publicRef(referenceKey, "web-transaction", transaction.transactionId),
          sourceKind: "WEB_TRANSACTION_AMOUNT",
          sourceField: "amount",
        },
      }));
    }
    values.sort((a, b) => a.valueRef.localeCompare(b.valueRef));
    assertCondition(values.length > 0, "APPROVED_RESIDUAL_ACCOUNT_HAS_NO_CURRENT_VALUES");
    const totalResidualAttos = values.reduce(
      (sum, value) => sum + integerValue(value.residualPointAttos, "RESIDUAL_POINT_ATTOS"),
      0n
    );
    const maxAbsResidualAttos = values.reduce((maximum, value) => {
      const residual = integerValue(value.residualPointAttos, "RESIDUAL_POINT_ATTOS");
      const absolute = residual < 0n ? -residual : residual;
      return absolute > maximum ? absolute : maximum;
    }, 0n);
    const approvedResidual = objectValue(
      account.facts.legacySubMicroNormalization,
      "APPROVED_RESIDUAL_FACTS"
    );
    assertCondition(approvedResidual.roundingMode === "ROUND_HALF_EVEN",
      "APPROVED_RESIDUAL_ROUNDING_MODE_MISMATCH");
    assertCondition(approvedResidual.valueCount === values.length,
      "APPROVED_RESIDUAL_VALUE_COUNT_MISMATCH");
    assertCondition(integerValue(approvedResidual.totalResidualPointAttos,
      "APPROVED_TOTAL_RESIDUAL_ATTOS") === totalResidualAttos,
    "APPROVED_TOTAL_RESIDUAL_MISMATCH");
    assertCondition(integerValue(approvedResidual.maxAbsResidualPointAttos,
      "APPROVED_MAX_RESIDUAL_ATTOS") === maxAbsResidualAttos,
    "APPROVED_MAX_RESIDUAL_MISMATCH");
    plans.push({
      accountRef: account.accountRef,
      policy: treatment,
      proposedOperationId: operationId(
        approvedSha256,
        account.accountRef,
        values.map((value) => value.valueRef)
      ),
      operationStatus: "NOT_AUTHORIZED",
      valueCount: values.length,
      totalResidualPointAttos: totalResidualAttos.toString(),
      normalizationDeltaPointAttos: (-totalResidualAttos).toString(),
      maxAbsResidualPointAttos: maxAbsResidualAttos.toString(),
      values,
      requiredExecutionPreconditions: [
        "SEPARATE_OPERATIONAL_APPROVAL",
        "ROOT_ONLY_BACKUP_WITH_CHECKSUM",
        "APPEND_ONLY_ORIGINAL_VALUE_AND_RESIDUAL_AUDIT",
        "ATOMIC_SOURCE_NORMALIZATION",
        "POST_WRITE_READ_ONLY_RECONCILIATION",
      ],
    });
  }
  return plans.sort((a, b) => a.accountRef.localeCompare(b.accountRef));
}

function buildPointWalletMigrationRemediationPlan(webInput, gameInput, approved, options = {}) {
  validateApprovedArtifact(approved);
  assertCondition(webInput && webInput.schemaVersion === 2,
    "UNSUPPORTED_WEB_SNAPSHOT_SCHEMA_VERSION");
  assertCondition(gameInput && gameInput.schemaVersion === 1,
    "UNSUPPORTED_GAME_SNAPSHOT_SCHEMA_VERSION");
  const referenceKey = String(options.referenceKey || "");
  assertCondition(Buffer.byteLength(referenceKey, "utf8") >= 32,
    "REPORT_REFERENCE_KEY_TOO_SHORT");
  const generatedAt = new Date(options.generatedAt || Date.now());
  assertCondition(Number.isFinite(generatedAt.getTime()), "INVALID_REMEDIATION_PLAN_GENERATED_AT");
  const approvedSha256 = sha256Value(options.approvedSha256, "APPROVED_ARTIFACT");
  const webSnapshotSha256 = sha256Value(options.webSnapshotSha256, "WEB_SNAPSHOT");
  const gameSnapshotSha256 = sha256Value(options.gameSnapshotSha256, "GAME_SNAPSHOT");
  const plannerSha256 = sha256Value(options.plannerSha256, "PLANNER");
  const referenceKeyId = crypto.createHash("sha256").update(referenceKey).digest("hex").slice(0, 16);
  assertCondition(referenceKeyId === approved.sourceReport.referenceKeyId,
    "REFERENCE_KEY_ID_MISMATCH");

  const currentReport = buildPointWalletMigrationReport(webInput, gameInput, {
    referenceKey,
    generatedAt: generatedAt.toISOString(),
  });
  validateCurrentReportAgainstApproval(currentReport, approved);
  const web = normalizeWebSnapshot(webInput);
  const game = normalizeGameSnapshot(gameInput);
  const rawUserByRef = buildRawUserIndex(web, game, referenceKey);
  const syntheticReversalPlans = buildSyntheticPlans(
    approved,
    web,
    game,
    rawUserByRef,
    approvedSha256,
    referenceKey
  );
  const legacyResidualPlans = buildResidualPlans(
    approved,
    web,
    rawUserByRef,
    approvedSha256,
    referenceKey
  );

  const totalSyntheticMicros = syntheticReversalPlans.reduce(
    (sum, plan) => sum + integerValue(plan.reversalPointMicros, "REVERSAL_POINT_MICROS"),
    0n
  );
  const syntheticSourceCount = syntheticReversalPlans.reduce(
    (sum, plan) => sum + plan.sources.length,
    0
  );
  assertCondition(totalSyntheticMicros === integerValue(
    approved.summary.totalSyntheticCreditMicros,
    "APPROVED_TOTAL_SYNTHETIC_MICROS"
  ), "APPROVED_SYNTHETIC_SUMMARY_MISMATCH");
  const residualValueCount = legacyResidualPlans.reduce((sum, plan) => sum + plan.valueCount, 0);
  const totalResidualAttos = legacyResidualPlans.reduce(
    (sum, plan) => sum + integerValue(plan.totalResidualPointAttos, "TOTAL_RESIDUAL_ATTOS"),
    0n
  );
  assertCondition(legacyResidualPlans.length === currentReport.summary.legacySubMicroAccountCount,
    "CURRENT_RESIDUAL_ACCOUNT_COUNT_MISMATCH");
  assertCondition(residualValueCount === currentReport.summary.legacySubMicroValueCount,
    "CURRENT_RESIDUAL_VALUE_COUNT_MISMATCH");
  assertCondition(totalResidualAttos === integerValue(
    currentReport.summary.totalLegacyResidualPointAttos,
    "CURRENT_TOTAL_RESIDUAL_ATTOS"
  ), "CURRENT_TOTAL_RESIDUAL_MISMATCH");

  const plannedResidualAccountRefs = new Set(
    legacyResidualPlans.map((plan) => plan.accountRef)
  );
  const expectedBlockedAccountCount = currentReport.accounts.filter((account) =>
    account.blockingIssues.some((issue) => !(
      issue === "LEGACY_SUB_MICRO_VALUE_PRESENT"
      && plannedResidualAccountRefs.has(account.accountRef)
    ))
  ).length;
  const currentBlockedAccountCount = currentReport.summary.statusCounts.BLOCKED || 0;

  const remediationPlanGate = expectedBlockedAccountCount === 0
    ? "READY_FOR_SEPARATE_OPERATIONAL_APPROVAL"
    : "BLOCKED_BY_UNPLANNED_INVARIANTS";
  const plan = {
    schemaVersion: 1,
    generatedAt: generatedAt.toISOString(),
    mode: "READ_ONLY_REMEDIATION_PLAN",
    automaticExecutionAllowed: false,
    executionStatementsGenerated: 0,
    databaseMutationsPerformed: false,
    containsRawIdentities: false,
    planner: {
      sha256: plannerSha256,
    },
    sources: {
      approvedDecisionArtifact: {
        sha256: approvedSha256,
        sourceWorksheetSha256: approved.sourceWorksheet.sha256,
        policySha256: approved.policyApproval.sha256,
      },
      webSnapshot: {
        sha256: webSnapshotSha256,
        schemaVersion: webInput.schemaVersion,
      },
      gameSnapshot: {
        sha256: gameSnapshotSha256,
        schemaVersion: gameInput.schemaVersion,
      },
      currentReport: {
        referenceKeyId,
        accountCount: currentReport.summary.accountCount,
        statusCounts: currentReport.summary.statusCounts,
      },
    },
    summary: {
      syntheticReversalAccountCount: syntheticReversalPlans.length,
      syntheticReversalSourceCount: syntheticSourceCount,
      syntheticReversalPointMicros: totalSyntheticMicros.toString(),
      residualAccountCount: legacyResidualPlans.length,
      residualValueCount,
      totalResidualPointAttos: totalResidualAttos.toString(),
      normalizationDeltaPointAttos: (-totalResidualAttos).toString(),
      currentBlockedAccountCount,
      expectedBlockedAccountCountAfterAuthorizedResidualNormalization: expectedBlockedAccountCount,
      postExecutionGate: "FRESH_READ_ONLY_DRY_RUN_REQUIRED",
      remediationPlanGate,
    },
    authorization: {
      syntheticReversal: "NOT_AUTHORIZED",
      residualNormalization: "NOT_AUTHORIZED",
      accountLink: "DEFERRED",
      balanceMigration: "NOT_AUTHORIZED",
      deployment: "NOT_AUTHORIZED",
    },
    syntheticReversalPlans,
    legacyResidualPlans,
  };
  assertNoRawIdentityFields(plan);
  return plan;
}

function formatMicros(value) {
  const integer = BigInt(value);
  const negative = integer < 0n;
  const absolute = negative ? -integer : integer;
  return `${negative ? "-" : ""}${absolute / POINT_MICROS}.${(absolute % POINT_MICROS)
    .toString().padStart(6, "0")}`;
}

function formatAttos(value) {
  const scale = 1_000_000_000_000_000_000n;
  const integer = BigInt(value);
  const negative = integer < 0n;
  const absolute = negative ? -integer : integer;
  return `${negative ? "-" : ""}${absolute / scale}.${(absolute % scale)
    .toString().padStart(18, "0")}`;
}

function renderPointWalletMigrationRemediationMarkdown(plan) {
  const lines = [
    "# Point Wallet Remediation Dry-Run Plan",
    "",
    `- Generated at: \`${plan.generatedAt}\``,
    `- Planner SHA-256: \`${plan.planner.sha256}\``,
    `- Approved artifact SHA-256: \`${plan.sources.approvedDecisionArtifact.sha256}\``,
    "- Safety: read-only plan; no SQL, database mutation, reversal, normalization, link, migration, deployment, or restart was executed.",
    "",
    "## Gate Summary",
    "",
    `- Synthetic reversal: **${formatMicros(plan.summary.syntheticReversalPointMicros)} Point** across **${plan.summary.syntheticReversalSourceCount}** source(s).`,
    `- Legacy residuals: **${plan.summary.residualValueCount}** value(s) across **${plan.summary.residualAccountCount}** account(s).`,
    `- Signed residual total: **${formatAttos(plan.summary.totalResidualPointAttos)} Point**.`,
    `- Current blocked accounts: **${plan.summary.currentBlockedAccountCount}**.`,
    `- Projected blocked accounts after authorized residual normalization: **${plan.summary.expectedBlockedAccountCountAfterAuthorizedResidualNormalization}**.`,
    `- Plan gate: **${plan.summary.remediationPlanGate}**.`,
    `- Post-execution gate: **${plan.summary.postExecutionGate}**.`,
    "",
    "## Authorization",
    "",
    "| Operation | Status |",
    "|---|---|",
    ...Object.entries(plan.authorization).map(([key, status]) => `| ${key} | **${status}** |`),
    "",
    "## Synthetic Credit Reversal",
    "",
    "| Account ref | Player ref | Operation ID | Current Point | Reversal | Expected Point | Sources | Status |",
    "|---|---|---|---:|---:|---:|---:|---|",
  ];
  for (const item of plan.syntheticReversalPlans) {
    lines.push(`| \`${item.accountRef}\` | \`${item.gamePlayerRef}\` | \`${item.proposedOperationId}\` | ${formatMicros(item.currentGamePointMicros)} | ${formatMicros(item.reversalPointMicros)} | ${formatMicros(item.expectedGamePointMicrosAfter)} | ${item.sources.length} | **${item.operationStatus}** |`);
  }
  lines.push(
    "",
    "## Legacy Residual Normalization",
    "",
    "| Account ref | Operation ID | Values | Residual Point | Normalization delta | Status |",
    "|---|---|---:|---:|---:|---|"
  );
  for (const item of plan.legacyResidualPlans) {
    lines.push(`| \`${item.accountRef}\` | \`${item.proposedOperationId}\` | ${item.valueCount} | ${formatAttos(item.totalResidualPointAttos)} | ${formatAttos(item.normalizationDeltaPointAttos)} | **${item.operationStatus}** |`);
  }
  lines.push(
    "",
    "## Required Next Gate",
    "",
    "- Review this plan and approve each operational change separately.",
    "- Re-capture both ledgers immediately before execution and require an exact match.",
    "- Execute balance/ledger updates atomically with immutable operation IDs.",
    "- Preserve original legacy values and signed residuals in append-only audit evidence.",
    "- Run a fresh read-only migration dry-run after execution; this plan does not authorize migration.",
    ""
  );
  return `${lines.join("\n")}\n`;
}

function parseCliArgs(argv) {
  const allowed = new Set([
    "--approved",
    "--approved-sha256",
    "--format",
    "--game-sha256",
    "--game-snapshot",
    "--generated-at",
    "--output",
    "--web-sha256",
    "--web-snapshot",
  ]);
  const args = {};
  for (let index = 0; index < argv.length; index += 2) {
    const key = argv[index];
    const value = argv[index + 1];
    assertCondition(allowed.has(key), "INVALID_ARGUMENT");
    assertCondition(Boolean(value), "MISSING_ARGUMENT_VALUE");
    const name = key.slice(2);
    assertCondition(!Object.hasOwn(args, name), "DUPLICATE_ARGUMENT");
    args[name] = value;
  }
  for (const required of [
    "approved", "approved-sha256", "game-sha256", "game-snapshot", "output",
    "web-sha256", "web-snapshot",
  ]) {
    assertCondition(Boolean(args[required]), "REQUIRED_ARGUMENT_MISSING");
  }
  args.format = args.format || "json";
  assertCondition(["json", "markdown"].includes(args.format), "INVALID_OUTPUT_FORMAT");
  return args;
}

function readVerifiedJson(filePath, expectedSha256, field) {
  const raw = fs.readFileSync(filePath);
  const actualSha256 = crypto.createHash("sha256").update(raw).digest("hex");
  assertCondition(actualSha256 === sha256Value(expectedSha256, field), `${field}_SHA256_MISMATCH`);
  return { raw, sha256: actualSha256, value: JSON.parse(raw.toString("utf8")) };
}

if (require.main === module) {
  try {
    const args = parseCliArgs(process.argv.slice(2));
    const approvedPath = path.resolve(args.approved);
    const webSnapshotPath = path.resolve(args["web-snapshot"]);
    const gameSnapshotPath = path.resolve(args["game-snapshot"]);
    const outputPath = path.resolve(args.output);
    assertCondition(![approvedPath, webSnapshotPath, gameSnapshotPath].includes(outputPath),
      "OUTPUT_MUST_NOT_OVERWRITE_INPUT");
    const approved = readVerifiedJson(approvedPath, args["approved-sha256"], "APPROVED_ARTIFACT");
    const web = readVerifiedJson(webSnapshotPath, args["web-sha256"], "WEB_SNAPSHOT");
    const game = readVerifiedJson(gameSnapshotPath, args["game-sha256"], "GAME_SNAPSHOT");
    const plannerSha256 = crypto.createHash("sha256").update(fs.readFileSync(__filename)).digest("hex");
    const plan = buildPointWalletMigrationRemediationPlan(web.value, game.value, approved.value, {
      referenceKey: process.env.POINT_MIGRATION_REPORT_KEY,
      approvedSha256: approved.sha256,
      webSnapshotSha256: web.sha256,
      gameSnapshotSha256: game.sha256,
      plannerSha256,
      generatedAt: args["generated-at"],
    });
    const output = args.format === "json"
      ? `${JSON.stringify(plan, null, 2)}\n`
      : renderPointWalletMigrationRemediationMarkdown(plan);
    fs.writeFileSync(outputPath, output, { encoding: "utf8", flag: "wx", mode: 0o600 });
    if (process.platform !== "win32") fs.chmodSync(outputPath, 0o600);
    process.stdout.write(`[point-wallet-remediation-plan] CREATED reversal_micros=${plan.summary.syntheticReversalPointMicros} residual_values=${plan.summary.residualValueCount} blocked_projection=${plan.summary.expectedBlockedAccountCountAfterAuthorizedResidualNormalization}\n`);
  } catch (error) {
    process.stderr.write(`[point-wallet-remediation-plan] ${error && error.message || "FAILED"}\n`);
    process.exitCode = 1;
  }
}

module.exports = {
  buildPointWalletMigrationRemediationPlan,
  renderPointWalletMigrationRemediationMarkdown,
  validateApprovedArtifact,
};
