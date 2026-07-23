"use strict";

const crypto = require("crypto");
const fs = require("fs");
const path = require("path");

const ACCOUNT_REF_PATTERN = /^[a-f0-9]{24}$/;
const SHA256_PATTERN = /^[a-f0-9]{64}$/;
const POLICY_KEYS = new Set([
  "blockingInvariantResolution",
  "gameOpeningBalanceTreatment",
  "legacySubMicroTreatment",
  "legacyWebBalanceTreatment",
  "legacyWebHistoryTreatment",
  "manualClassification",
  "syntheticCreditTreatment",
]);
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

function assertNoRawIdentityFields(value) {
  if (Array.isArray(value)) {
    for (const item of value) assertNoRawIdentityFields(item);
    return;
  }
  if (!value || typeof value !== "object") return;
  for (const [key, child] of Object.entries(value)) {
    assertCondition(!FORBIDDEN_RAW_IDENTITY_KEYS.has(key), "RAW_IDENTIFIER_FIELD_PRESENT");
    assertNoRawIdentityFields(child);
  }
}

function validateSha256(value, field) {
  const normalized = String(value || "").toLowerCase();
  assertCondition(SHA256_PATTERN.test(normalized), `INVALID_${field}_SHA256`);
  return normalized;
}

function validateDecision(decision) {
  assertCondition(decision && typeof decision === "object" && !Array.isArray(decision),
    "INVALID_WORKSHEET_DECISION");
  assertCondition(typeof decision.key === "string" && POLICY_KEYS.has(decision.key),
    "UNSUPPORTED_WORKSHEET_DECISION_KEY");
  assertCondition(decision.status === "PENDING", "WORKSHEET_DECISION_NOT_PENDING");
  assertCondition(Array.isArray(decision.allowedValues) && decision.allowedValues.length > 0,
    "INVALID_WORKSHEET_ALLOWED_VALUES");
  assertCondition(decision.allowedValues.every((value) => typeof value === "string" && value.length > 0),
    "INVALID_WORKSHEET_ALLOWED_VALUE");
  assertCondition(new Set(decision.allowedValues).size === decision.allowedValues.length,
    "DUPLICATE_WORKSHEET_ALLOWED_VALUE");
}

function validateWorksheet(worksheet) {
  assertCondition(worksheet && typeof worksheet === "object" && !Array.isArray(worksheet),
    "INVALID_WORKSHEET");
  assertNoRawIdentityFields(worksheet);
  assertCondition(worksheet.schemaVersion === 1, "UNSUPPORTED_WORKSHEET_SCHEMA_VERSION");
  assertCondition(worksheet.mode === "DECISION_WORKSHEET", "INVALID_WORKSHEET_MODE");
  assertCondition(worksheet.automaticMigrationAllowed === false,
    "WORKSHEET_ALLOWS_AUTOMATIC_MIGRATION");
  assertCondition(worksheet.migrationStatementsGenerated === 0,
    "WORKSHEET_CONTAINS_MIGRATION_STATEMENTS");
  assertCondition(worksheet.databaseMutationsPerformed === false,
    "WORKSHEET_DATABASE_MUTATION_FLAGGED");
  assertCondition(Number.isFinite(Date.parse(worksheet.generatedAt)),
    "INVALID_WORKSHEET_GENERATED_AT");
  assertCondition(worksheet.generator && typeof worksheet.generator === "object",
    "INVALID_WORKSHEET_GENERATOR");
  validateSha256(worksheet.generator.sha256, "WORKSHEET_GENERATOR");
  assertCondition(worksheet.sourceReport && typeof worksheet.sourceReport === "object",
    "INVALID_WORKSHEET_SOURCE_REPORT");
  validateSha256(worksheet.sourceReport.sha256, "WORKSHEET_SOURCE_REPORT");
  assertCondition(worksheet.sourceReport.schemaVersion === 2,
    "UNSUPPORTED_WORKSHEET_SOURCE_REPORT_SCHEMA_VERSION");
  assertCondition(Number.isFinite(Date.parse(worksheet.sourceReport.generatedAt)),
    "INVALID_WORKSHEET_SOURCE_REPORT_GENERATED_AT");
  assertCondition(typeof worksheet.sourceReport.referenceKeyId === "string"
    && /^[a-f0-9]{16}$/.test(worksheet.sourceReport.referenceKeyId),
  "INVALID_WORKSHEET_REFERENCE_KEY_ID");
  assertCondition(worksheet.summary && typeof worksheet.summary === "object",
    "INVALID_WORKSHEET_SUMMARY");
  assertCondition(worksheet.summary.migrationGate === "BLOCKED_PENDING_EXPLICIT_APPROVALS",
    "WORKSHEET_MIGRATION_GATE_NOT_BLOCKED");
  assertCondition(Array.isArray(worksheet.accounts), "INVALID_WORKSHEET_ACCOUNTS");
  assertCondition(worksheet.summary.reviewAccountCount === worksheet.accounts.length,
    "WORKSHEET_ACCOUNT_COUNT_MISMATCH");

  let pendingDecisionCount = 0;
  let blockedAccountCount = 0;
  const seenAccountRefs = new Set();
  for (const account of worksheet.accounts) {
    assertCondition(account && typeof account === "object" && !Array.isArray(account),
      "INVALID_WORKSHEET_ACCOUNT");
    assertCondition(ACCOUNT_REF_PATTERN.test(String(account.accountRef || "")),
      "INVALID_WORKSHEET_ACCOUNT_REF");
    assertCondition(!seenAccountRefs.has(account.accountRef), "DUPLICATE_WORKSHEET_ACCOUNT_REF");
    seenAccountRefs.add(account.accountRef);
    assertCondition(Array.isArray(account.gamePlayerRefs)
      && account.gamePlayerRefs.every((ref) => ACCOUNT_REF_PATTERN.test(String(ref))),
    "INVALID_WORKSHEET_GAME_PLAYER_REFS");
    assertCondition(Array.isArray(account.requiredDecisions) && account.requiredDecisions.length > 0,
      "INVALID_WORKSHEET_REQUIRED_DECISIONS");
    const seenDecisionKeys = new Set();
    for (const decision of account.requiredDecisions) {
      validateDecision(decision);
      assertCondition(!seenDecisionKeys.has(decision.key),
        "DUPLICATE_WORKSHEET_ACCOUNT_DECISION_KEY");
      seenDecisionKeys.add(decision.key);
    }
    pendingDecisionCount += account.requiredDecisions.length;
    if (account.sourceStatus === "BLOCKED") blockedAccountCount += 1;
    assertCondition(account.approval && account.approval.status === "PENDING",
      "WORKSHEET_ACCOUNT_APPROVAL_NOT_PENDING");
    assertCondition(account.approval.selectedValues
      && typeof account.approval.selectedValues === "object"
      && !Array.isArray(account.approval.selectedValues)
      && Object.keys(account.approval.selectedValues).length === 0,
    "WORKSHEET_ALREADY_HAS_SELECTED_VALUES");
    for (const field of ["approvedBy", "approvedAt", "evidenceReference", "notes"]) {
      assertCondition(account.approval[field] == null, "WORKSHEET_HAS_PREPOPULATED_APPROVAL_METADATA");
    }
  }
  assertCondition(worksheet.summary.pendingDecisionCount === pendingDecisionCount,
    "WORKSHEET_PENDING_DECISION_COUNT_MISMATCH");
  assertCondition(worksheet.summary.blockedAccountCount === blockedAccountCount,
    "WORKSHEET_BLOCKED_ACCOUNT_COUNT_MISMATCH");
  return pendingDecisionCount;
}

function validatePolicy(policy, requiredDecisionKeys) {
  assertCondition(policy && typeof policy === "object" && !Array.isArray(policy), "INVALID_POLICY");
  assertNoRawIdentityFields(policy);
  const policyKeys = Object.keys(policy).sort();
  const expectedPolicyKeys = [
    "approvalReference",
    "approvedAt",
    "approvedByRole",
    "constraints",
    "decisions",
    "mode",
    "schemaVersion",
  ];
  assertCondition(JSON.stringify(policyKeys) === JSON.stringify(expectedPolicyKeys),
    "UNEXPECTED_POLICY_FIELD");
  assertCondition(policy.schemaVersion === 1, "UNSUPPORTED_POLICY_SCHEMA_VERSION");
  assertCondition(policy.mode === "POINT_WALLET_MIGRATION_POLICY_APPROVAL", "INVALID_POLICY_MODE");
  assertCondition(Number.isFinite(Date.parse(policy.approvedAt)), "INVALID_POLICY_APPROVED_AT");
  assertCondition(typeof policy.approvedByRole === "string"
    && /^[A-Z][A-Z0-9_]{2,63}$/.test(policy.approvedByRole),
  "INVALID_POLICY_APPROVED_BY_ROLE");
  assertCondition(typeof policy.approvalReference === "string"
    && /^[A-Z0-9][A-Z0-9_-]{7,127}$/.test(policy.approvalReference),
  "INVALID_POLICY_APPROVAL_REFERENCE");
  assertCondition(policy.decisions && typeof policy.decisions === "object"
    && !Array.isArray(policy.decisions), "INVALID_POLICY_DECISIONS");
  assertCondition(policy.constraints && typeof policy.constraints === "object"
    && !Array.isArray(policy.constraints), "INVALID_POLICY_CONSTRAINTS");
  const constraintKeys = Object.keys(policy.constraints).sort();
  const expectedConstraintKeys = [
    "authorizesBalanceMigration",
    "authorizesDatabaseMutation",
    "authorizesDeployment",
    "authorizesSyntheticReversalExecution",
    "requiresSeparateOperationalChangeApproval",
  ];
  assertCondition(JSON.stringify(constraintKeys) === JSON.stringify(expectedConstraintKeys),
    "UNEXPECTED_POLICY_CONSTRAINT");
  assertCondition(policy.constraints.authorizesDatabaseMutation === false,
    "POLICY_AUTHORIZES_DATABASE_MUTATION");
  assertCondition(policy.constraints.authorizesDeployment === false,
    "POLICY_AUTHORIZES_DEPLOYMENT");
  assertCondition(policy.constraints.authorizesBalanceMigration === false,
    "POLICY_AUTHORIZES_BALANCE_MIGRATION");
  assertCondition(policy.constraints.authorizesSyntheticReversalExecution === false,
    "POLICY_AUTHORIZES_SYNTHETIC_REVERSAL_EXECUTION");
  assertCondition(policy.constraints.requiresSeparateOperationalChangeApproval === true,
    "POLICY_MISSING_SEPARATE_OPERATIONAL_APPROVAL");

  const actualKeys = Object.keys(policy.decisions).sort();
  const expectedKeys = [...requiredDecisionKeys].sort();
  for (const key of actualKeys) {
    assertCondition(POLICY_KEYS.has(key), "UNEXPECTED_POLICY_DECISION_KEY");
  }
  assertCondition(JSON.stringify(actualKeys) === JSON.stringify(expectedKeys),
    "POLICY_DECISION_KEYS_MISMATCH");
  assertCondition(actualKeys.every((key) => typeof policy.decisions[key] === "string"),
    "INVALID_POLICY_DECISION_VALUE");
}

function buildPointWalletMigrationApprovedWorksheet(worksheet, policy, options = {}) {
  const approvedDecisionCount = validateWorksheet(worksheet);
  const worksheetSha256 = validateSha256(options.worksheetSha256, "WORKSHEET");
  const policySha256 = validateSha256(options.policySha256, "POLICY");
  const applicatorSha256 = validateSha256(options.applicatorSha256, "APPLICATOR");
  const generatedAt = new Date(options.generatedAt || Date.now());
  assertCondition(Number.isFinite(generatedAt.getTime()), "INVALID_APPROVED_WORKSHEET_GENERATED_AT");

  const requiredDecisionKeys = new Set();
  for (const account of worksheet.accounts) {
    for (const decision of account.requiredDecisions) requiredDecisionKeys.add(decision.key);
  }
  validatePolicy(policy, requiredDecisionKeys);

  const accounts = worksheet.accounts.map((account) => {
    const selectedValues = {};
    const requiredDecisions = account.requiredDecisions.map((decision) => {
      const selectedValue = policy.decisions[decision.key];
      assertCondition(decision.allowedValues.includes(selectedValue),
        `POLICY_VALUE_NOT_ALLOWED_${decision.key}`);
      selectedValues[decision.key] = selectedValue;
      return {
        ...decision,
        status: "APPROVED",
        selectedValue,
      };
    });
    return {
      ...account,
      requiredDecisions,
      approval: {
        status: "APPROVED",
        selectedValues,
        approvedByRole: policy.approvedByRole,
        approvedAt: policy.approvedAt,
        evidenceReference: policy.approvalReference,
        notes: "POLICY_ONLY_NO_OPERATION_EXECUTED",
      },
    };
  });

  return {
    ...worksheet,
    generatedAt: generatedAt.toISOString(),
    mode: "APPROVED_DECISION_WORKSHEET",
    automaticMigrationAllowed: false,
    migrationStatementsGenerated: 0,
    databaseMutationsPerformed: false,
    approvalApplicator: {
      sha256: applicatorSha256,
    },
    sourceWorksheet: {
      sha256: worksheetSha256,
      generatedAt: worksheet.generatedAt,
      generatorSha256: worksheet.generator.sha256,
    },
    policyApproval: {
      sha256: policySha256,
      approvedAt: policy.approvedAt,
      approvedByRole: policy.approvedByRole,
      approvalReference: policy.approvalReference,
      constraints: { ...policy.constraints },
    },
    summary: {
      ...worksheet.summary,
      pendingDecisionCount: 0,
      approvedDecisionCount,
      decisionGate: "APPROVED",
      migrationGate: "BLOCKED_NO_BALANCE_MIGRATION_AUTHORIZED",
    },
    operations: {
      accountLink: {
        status: "DEFERRED",
        reason: "POLICY_SELECTED_DEFER_ACCOUNT_LINK",
      },
      balanceMigration: {
        status: "NOT_AUTHORIZED",
      },
      syntheticCreditReversal: {
        status: "NOT_EXECUTED",
        selectedPolicy: policy.decisions.syntheticCreditTreatment || null,
        requiresSeparateOperationalChangeApproval: true,
      },
      legacySubMicroNormalization: {
        status: "NOT_EXECUTED",
        selectedPolicy: policy.decisions.legacySubMicroTreatment || null,
        residualAuditRequired: true,
      },
      databaseMutation: {
        status: "NOT_EXECUTED",
      },
      deployment: {
        status: "NOT_EXECUTED",
      },
    },
    accounts,
  };
}

function formatFixed(value, decimals) {
  if (value == null) return "-";
  const integer = BigInt(value);
  const negative = integer < 0n;
  const absolute = negative ? -integer : integer;
  const scale = 10n ** BigInt(decimals);
  const whole = absolute / scale;
  const fraction = (absolute % scale).toString().padStart(decimals, "0");
  return `${negative ? "-" : ""}${whole}.${fraction}`;
}

function markdownCell(value) {
  return String(value == null ? "-" : value).replace(/\|/g, "\\|").replace(/\r?\n/g, " ");
}

function renderPointWalletMigrationApprovedMarkdown(approved) {
  const lines = [
    "# Approved Point Wallet Migration Decision Worksheet",
    "",
    `- Generated at: \`${approved.generatedAt}\``,
    `- Source worksheet SHA-256: \`${approved.sourceWorksheet.sha256}\``,
    `- Policy SHA-256: \`${approved.policyApproval.sha256}\``,
    `- Applicator SHA-256: \`${approved.approvalApplicator.sha256}\``,
    `- Approval reference: \`${approved.policyApproval.approvalReference}\``,
    `- Approved by role / at: \`${approved.policyApproval.approvedByRole}\` / \`${approved.policyApproval.approvedAt}\``,
    "- Safety: policy approval only; no database mutation, deployment, migration, account link, or reversal was executed.",
    "",
    "## Gate Summary",
    "",
    `- Review accounts: **${approved.summary.reviewAccountCount}**`,
    `- Source-blocked accounts: **${approved.summary.blockedAccountCount}**`,
    `- Approved decision items: **${approved.summary.approvedDecisionCount}**`,
    `- Pending decision items: **${approved.summary.pendingDecisionCount}**`,
    `- Decision gate: **${approved.summary.decisionGate}**`,
    `- Migration gate: **${approved.summary.migrationGate}**`,
    `- Legacy web Point under review: **${formatFixed(approved.summary.totalReviewWebPointMicros, 6)} Point**`,
    `- Game opening balance under review: **${formatFixed(approved.summary.totalGameOpeningPointMicros, 6)} Point**`,
    `- Synthetic credit without web source: **${formatFixed(approved.summary.totalSyntheticCreditMicros, 6)} Point**`,
    "",
    "## Operational Status",
    "",
    "| Operation | Status |",
    "|---|---|",
    ...Object.entries(approved.operations).map(([key, operation]) =>
      `| ${markdownCell(key)} | **${markdownCell(operation.status)}** |`),
    "",
    "## Approved Policy",
    "",
    "| Decision key | Selected value |",
    "|---|---|",
  ];
  const policyRows = new Map();
  for (const account of approved.accounts) {
    for (const decision of account.requiredDecisions) {
      policyRows.set(decision.key, decision.selectedValue);
    }
  }
  for (const [key, value] of [...policyRows.entries()].sort(([a], [b]) => a.localeCompare(b))) {
    lines.push(`| \`${key}\` | \`${value}\` |`);
  }
  lines.push(
    "",
    "## Account Decision Table",
    "",
    "| Account ref | Source status | Decision key(s) | Selected value(s) | Approval |",
    "|---|---|---|---|---|"
  );
  for (const account of approved.accounts) {
    lines.push(`| \`${account.accountRef}\` | ${markdownCell(account.sourceStatus)} | ${markdownCell(account.requiredDecisions.map((item) => item.key).join(", "))} | ${markdownCell(account.requiredDecisions.map((item) => item.selectedValue).join(", "))} | **${account.approval.status}** |`);
  }
  lines.push(
    "",
    "## Required Follow-Up",
    "",
    "- Keep account linking and legacy balance migration deferred.",
    "- Prepare the synthetic credit reversal as a separate audited operational change.",
    "- Produce the residual audit before any sub-micro normalization is written.",
    "- Re-run the read-only reconciliation after each approved operational change.",
    ""
  );
  return `${lines.join("\n")}\n`;
}

function parseCliArgs(argv) {
  const allowed = new Set([
    "--format",
    "--generated-at",
    "--output",
    "--policy",
    "--policy-sha256",
    "--worksheet",
    "--worksheet-sha256",
  ]);
  const args = {};
  for (let index = 0; index < argv.length; index += 2) {
    const key = argv[index];
    const value = argv[index + 1];
    if (!allowed.has(key)) throw new Error("INVALID_ARGUMENT");
    if (!value) throw new Error("MISSING_ARGUMENT_VALUE");
    if (Object.hasOwn(args, key.slice(2))) throw new Error("DUPLICATE_ARGUMENT");
    args[key.slice(2)] = value;
  }
  if (!args.worksheet || !args["worksheet-sha256"] || !args.policy
    || !args["policy-sha256"] || !args.output) {
    throw new Error("WORKSHEET_POLICY_CHECKSUMS_AND_OUTPUT_REQUIRED");
  }
  args.format = args.format || "json";
  if (!["json", "markdown"].includes(args.format)) throw new Error("INVALID_OUTPUT_FORMAT");
  return args;
}

function readVerifiedJson(filePath, expectedSha256, field) {
  const raw = fs.readFileSync(filePath);
  const actualSha256 = crypto.createHash("sha256").update(raw).digest("hex");
  assertCondition(actualSha256 === validateSha256(expectedSha256, field), `${field}_SHA256_MISMATCH`);
  return {
    sha256: actualSha256,
    value: JSON.parse(raw.toString("utf8")),
  };
}

if (require.main === module) {
  try {
    const args = parseCliArgs(process.argv.slice(2));
    const worksheetPath = path.resolve(args.worksheet);
    const policyPath = path.resolve(args.policy);
    const outputPath = path.resolve(args.output);
    assertCondition(outputPath !== worksheetPath && outputPath !== policyPath,
      "OUTPUT_MUST_NOT_OVERWRITE_INPUT");
    const worksheet = readVerifiedJson(worksheetPath, args["worksheet-sha256"], "WORKSHEET");
    const policy = readVerifiedJson(policyPath, args["policy-sha256"], "POLICY");
    const applicatorSha256 = crypto.createHash("sha256")
      .update(fs.readFileSync(__filename))
      .digest("hex");
    const approved = buildPointWalletMigrationApprovedWorksheet(worksheet.value, policy.value, {
      worksheetSha256: worksheet.sha256,
      policySha256: policy.sha256,
      applicatorSha256,
      generatedAt: args["generated-at"],
    });
    const output = args.format === "json"
      ? `${JSON.stringify(approved, null, 2)}\n`
      : renderPointWalletMigrationApprovedMarkdown(approved);
    fs.writeFileSync(outputPath, output, { encoding: "utf8", flag: "wx", mode: 0o600 });
    if (process.platform !== "win32") fs.chmodSync(outputPath, 0o600);
    process.stdout.write(`[point-wallet-decision-approval] CREATED accounts=${approved.summary.reviewAccountCount} approved=${approved.summary.approvedDecisionCount} migration=${approved.summary.migrationGate}\n`);
  } catch (error) {
    process.stderr.write(`[point-wallet-decision-approval] ${error && error.message || "FAILED"}\n`);
    process.exitCode = 1;
  }
}

module.exports = {
  buildPointWalletMigrationApprovedWorksheet,
  renderPointWalletMigrationApprovedMarkdown,
  validatePolicy,
  validateWorksheet,
};
