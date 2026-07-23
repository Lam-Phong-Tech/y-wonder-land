"use strict";

const crypto = require("crypto");

const SHA256_PATTERN = /^[a-f0-9]{64}$/;
const EXPECTED_EXECUTION_ORDER = [
  "LEGACY_RESIDUAL_NORMALIZATION",
  "SYNTHETIC_CREDIT_REVERSAL",
];
const EXPECTED_CONSTRAINTS = {
  authorizesOnlyListedOperations: true,
  authorizesDatabaseMutation: true,
  authorizesCompensatingRollback: true,
  authorizesAccountLink: false,
  authorizesBalanceMigration: false,
  authorizesDeployment: false,
  authorizesServiceRestart: false,
  authorizesRealPayment: false,
  requiresChecksummedBackup: true,
  requiresFreshPreflight: true,
  requiresAtomicWriteAndAudit: true,
  requiresPostWriteReconciliation: true,
};
const FORBIDDEN_IDENTITY_KEYS = new Set([
  "email", "phone", "playerId", "player_id", "sourceTransactionId", "transactionId",
  "userId", "user_id", "username", "webUserId", "web_user_id",
]);

function assertCondition(condition, code) {
  if (!condition) throw new Error(code);
}

function objectValue(value, field) {
  assertCondition(value && typeof value === "object" && !Array.isArray(value), `INVALID_${field}`);
  return value;
}

function exactKeys(value, expected, code) {
  const actual = Object.keys(objectValue(value, code)).sort();
  assertCondition(JSON.stringify(actual) === JSON.stringify([...expected].sort()), code);
}

function sha256Value(value, field) {
  const normalized = String(value || "").toLowerCase();
  assertCondition(SHA256_PATTERN.test(normalized), `INVALID_${field}_SHA256`);
  return normalized;
}

function integerText(value, field, options = {}) {
  const text = String(value == null ? "" : value).trim();
  assertCondition(/^-?(0|[1-9]\d*)$/.test(text), `INVALID_${field}`);
  const integer = BigInt(text);
  if (options.nonNegative) assertCondition(integer >= 0n, `INVALID_${field}`);
  if (options.positive) assertCondition(integer > 0n, `INVALID_${field}`);
  return text;
}

function assertNoRawIdentityFields(value) {
  if (Array.isArray(value)) {
    for (const child of value) assertNoRawIdentityFields(child);
    return;
  }
  if (!value || typeof value !== "object") return;
  for (const [key, child] of Object.entries(value)) {
    assertCondition(!FORBIDDEN_IDENTITY_KEYS.has(key), "OPERATION_APPROVAL_CONTAINS_RAW_IDENTITY");
    assertNoRawIdentityFields(child);
  }
}

function validatePointWalletRemediationOperationApproval(approvalInput, planInput, options = {}) {
  const approval = objectValue(approvalInput, "OPERATION_APPROVAL");
  const plan = objectValue(planInput, "REMEDIATION_PLAN");
  assertNoRawIdentityFields(approval);
  exactKeys(approval, [
    "schemaVersion", "mode", "approvedAt", "approvedByRole", "approvalReference",
    "remediationPlan", "executionOrder", "operations", "constraints",
  ], "UNEXPECTED_OPERATION_APPROVAL_FIELD");
  assertCondition(approval.schemaVersion === 1, "UNSUPPORTED_OPERATION_APPROVAL_SCHEMA");
  assertCondition(approval.mode === "POINT_WALLET_REMEDIATION_OPERATION_APPROVAL",
    "INVALID_OPERATION_APPROVAL_MODE");
  assertCondition(Number.isFinite(Date.parse(approval.approvedAt)), "INVALID_OPERATION_APPROVED_AT");
  assertCondition(approval.approvedByRole === "PROJECT_OWNER", "INVALID_OPERATION_APPROVER_ROLE");
  assertCondition(/^OWNER_CHAT_APPROVAL_[A-Z0-9_-]+$/.test(String(approval.approvalReference || "")),
    "INVALID_OPERATION_APPROVAL_REFERENCE");

  const approvalSha256 = sha256Value(options.approvalSha256, "OPERATION_APPROVAL");
  const planSha256 = sha256Value(options.planSha256, "REMEDIATION_PLAN");
  const source = objectValue(approval.remediationPlan, "APPROVED_REMEDIATION_PLAN");
  exactKeys(source, ["sha256", "plannerSha256", "generatedAt"],
    "UNEXPECTED_APPROVED_REMEDIATION_PLAN_FIELD");
  assertCondition(sha256Value(source.sha256, "APPROVED_REMEDIATION_PLAN") === planSha256,
    "APPROVED_REMEDIATION_PLAN_SHA_MISMATCH");
  assertCondition(sha256Value(source.plannerSha256, "APPROVED_REMEDIATION_PLANNER")
    === sha256Value(plan.planner && plan.planner.sha256, "PLAN_PLANNER"),
  "APPROVED_REMEDIATION_PLANNER_SHA_MISMATCH");
  assertCondition(source.generatedAt === plan.generatedAt, "APPROVED_REMEDIATION_GENERATED_AT_MISMATCH");

  assertCondition(plan.schemaVersion === 1 && plan.mode === "READ_ONLY_REMEDIATION_PLAN",
    "INVALID_AUTHORIZED_REMEDIATION_PLAN");
  assertCondition(plan.automaticExecutionAllowed === false
    && plan.executionStatementsGenerated === 0
    && plan.databaseMutationsPerformed === false
    && plan.containsRawIdentities === false,
  "AUTHORIZED_REMEDIATION_PLAN_SAFETY_FLAGS_INVALID");
  assertCondition(JSON.stringify(approval.executionOrder) === JSON.stringify(EXPECTED_EXECUTION_ORDER),
    "INVALID_REMEDIATION_EXECUTION_ORDER");

  exactKeys(approval.constraints, Object.keys(EXPECTED_CONSTRAINTS),
    "UNEXPECTED_OPERATION_APPROVAL_CONSTRAINT");
  for (const [key, expected] of Object.entries(EXPECTED_CONSTRAINTS)) {
    assertCondition(approval.constraints[key] === expected, `INVALID_OPERATION_APPROVAL_CONSTRAINT_${key}`);
  }

  const operations = objectValue(approval.operations, "APPROVED_OPERATIONS");
  exactKeys(operations, ["legacyResidualNormalization", "syntheticCreditReversal"],
    "UNEXPECTED_AUTHORIZED_OPERATION");
  const residual = objectValue(operations.legacyResidualNormalization,
    "AUTHORIZED_LEGACY_RESIDUAL_NORMALIZATION");
  exactKeys(residual, [
    "authorized", "action", "roundingMode", "accountCount", "valueCount",
    "totalResidualPointAttos", "normalizationDeltaPointAttos",
  ], "UNEXPECTED_RESIDUAL_AUTHORIZATION_FIELD");
  assertCondition(residual.authorized === true, "RESIDUAL_NORMALIZATION_NOT_AUTHORIZED");
  assertCondition(residual.action === "ROUND_HALF_EVEN_WITH_APPEND_ONLY_RESIDUAL_AUDIT"
    && residual.roundingMode === "ROUND_HALF_EVEN", "INVALID_RESIDUAL_AUTHORIZED_ACTION");
  assertCondition(residual.accountCount === plan.summary.residualAccountCount,
    "AUTHORIZED_RESIDUAL_ACCOUNT_COUNT_MISMATCH");
  assertCondition(residual.valueCount === plan.summary.residualValueCount,
    "AUTHORIZED_RESIDUAL_VALUE_COUNT_MISMATCH");
  assertCondition(integerText(residual.totalResidualPointAttos, "AUTHORIZED_TOTAL_RESIDUAL_ATTOS")
    === integerText(plan.summary.totalResidualPointAttos, "PLAN_TOTAL_RESIDUAL_ATTOS"),
  "AUTHORIZED_TOTAL_RESIDUAL_ATTOS_MISMATCH");
  assertCondition(integerText(residual.normalizationDeltaPointAttos,
    "AUTHORIZED_NORMALIZATION_DELTA_ATTOS")
    === integerText(plan.summary.normalizationDeltaPointAttos, "PLAN_NORMALIZATION_DELTA_ATTOS"),
  "AUTHORIZED_NORMALIZATION_DELTA_ATTOS_MISMATCH");

  const synthetic = objectValue(operations.syntheticCreditReversal,
    "AUTHORIZED_SYNTHETIC_CREDIT_REVERSAL");
  exactKeys(synthetic, ["authorized", "action", "accountCount", "sourceCount", "pointMicros"],
    "UNEXPECTED_SYNTHETIC_AUTHORIZATION_FIELD");
  assertCondition(synthetic.authorized === true, "SYNTHETIC_REVERSAL_NOT_AUTHORIZED");
  assertCondition(synthetic.action === "AUDITED_SYNTHETIC_CREDIT_REVERSAL",
    "INVALID_SYNTHETIC_AUTHORIZED_ACTION");
  assertCondition(synthetic.accountCount === plan.summary.syntheticReversalAccountCount,
    "AUTHORIZED_SYNTHETIC_ACCOUNT_COUNT_MISMATCH");
  assertCondition(synthetic.sourceCount === plan.summary.syntheticReversalSourceCount,
    "AUTHORIZED_SYNTHETIC_SOURCE_COUNT_MISMATCH");
  assertCondition(integerText(synthetic.pointMicros, "AUTHORIZED_SYNTHETIC_MICROS", { positive: true })
    === integerText(plan.summary.syntheticReversalPointMicros, "PLAN_SYNTHETIC_MICROS", { positive: true }),
  "AUTHORIZED_SYNTHETIC_MICROS_MISMATCH");

  assertCondition(plan.authorization.syntheticReversal === "NOT_AUTHORIZED"
    && plan.authorization.residualNormalization === "NOT_AUTHORIZED"
    && plan.authorization.accountLink === "DEFERRED"
    && plan.authorization.balanceMigration === "NOT_AUTHORIZED"
    && plan.authorization.deployment === "NOT_AUTHORIZED",
  "SOURCE_PLAN_AUTHORIZATION_POSTURE_INVALID");
  return { approval, approvalSha256, plan, planSha256 };
}

function operationPayloadSignature(payload) {
  return crypto.createHash("sha256").update(JSON.stringify(payload)).digest("hex");
}

module.exports = {
  EXPECTED_CONSTRAINTS,
  EXPECTED_EXECUTION_ORDER,
  operationPayloadSignature,
  validatePointWalletRemediationOperationApproval,
};
