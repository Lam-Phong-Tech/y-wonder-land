"use strict";

const crypto = require("crypto");
const fs = require("fs");
const path = require("path");

const ACCOUNT_REF_PATTERN = /^[a-f0-9]{24}$/;
const SHA256_PATTERN = /^[a-f0-9]{64}$/;
const SUPPORTED_REPORT_SCHEMA_VERSION = 2;
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

function integerValue(value, field, options = {}) {
  if (value == null && options.nullable) return null;
  assertCondition(typeof value === "string" && /^-?\d+$/.test(value), `INVALID_${field}`);
  return BigInt(value);
}

function stringArray(value, field) {
  assertCondition(Array.isArray(value) && value.every((item) => typeof item === "string"),
    `INVALID_${field}`);
  return value;
}

function validateReport(report) {
  assertCondition(report && typeof report === "object" && !Array.isArray(report), "INVALID_REPORT");
  assertNoRawIdentityFields(report);
  assertCondition(report.schemaVersion === SUPPORTED_REPORT_SCHEMA_VERSION,
    "UNSUPPORTED_REPORT_SCHEMA_VERSION");
  assertCondition(report.mode === "READ_ONLY_DRY_RUN", "REPORT_NOT_READ_ONLY_DRY_RUN");
  assertCondition(report.automaticMigrationAllowed === false, "REPORT_ALLOWS_AUTOMATIC_MIGRATION");
  assertCondition(report.migrationStatementsGenerated === 0, "REPORT_CONTAINS_MIGRATION_STATEMENTS");
  assertCondition(report.databaseMutationsPerformed === false, "REPORT_DATABASE_MUTATION_FLAGGED");
  assertCondition(typeof report.referenceKeyId === "string" && /^[a-f0-9]{16}$/.test(report.referenceKeyId),
    "INVALID_REPORT_REFERENCE_KEY_ID");
  assertCondition(Number.isFinite(Date.parse(report.generatedAt)), "INVALID_REPORT_GENERATED_AT");
  assertCondition(Array.isArray(report.accounts), "INVALID_REPORT_ACCOUNTS");
  assertCondition(report.summary && typeof report.summary === "object", "INVALID_REPORT_SUMMARY");
  assertCondition(report.summary.accountCount === report.accounts.length, "REPORT_ACCOUNT_COUNT_MISMATCH");

  const seenRefs = new Set();
  for (const account of report.accounts) {
    assertCondition(account && typeof account === "object", "INVALID_REPORT_ACCOUNT");
    assertCondition(ACCOUNT_REF_PATTERN.test(String(account.accountRef || "")), "INVALID_ACCOUNT_REF");
    assertCondition(!seenRefs.has(account.accountRef), "DUPLICATE_ACCOUNT_REF");
    seenRefs.add(account.accountRef);
    assertCondition(Array.isArray(account.gamePlayerRefs)
      && account.gamePlayerRefs.every((ref) => ACCOUNT_REF_PATTERN.test(String(ref))),
    "INVALID_GAME_PLAYER_REFS");
    stringArray(account.blockingIssues, "BLOCKING_ISSUES");
    stringArray(account.reviewReasons, "REVIEW_REASONS");
    assertCondition(account.balances && typeof account.balances === "object", "INVALID_ACCOUNT_BALANCES");
    integerValue(account.balances.webPointMicros, "WEB_POINT_MICROS");
    integerValue(account.balances.webLockedPointMicros, "WEB_LOCKED_POINT_MICROS");
    integerValue(account.balances.gamePointMicros, "GAME_POINT_MICROS", { nullable: true });
    integerValue(account.balances.gameOpeningPointMicros, "GAME_OPENING_POINT_MICROS", { nullable: true });
    assertCondition(account.evidence && typeof account.evidence === "object", "INVALID_ACCOUNT_EVIDENCE");
    assertCondition(Array.isArray(account.evidence.webPointTransactions), "INVALID_WEB_TRANSACTION_EVIDENCE");
    assertCondition(Array.isArray(account.evidence.outboxes), "INVALID_OUTBOX_EVIDENCE");
    for (const outbox of account.evidence.outboxes) {
      assertCondition(ACCOUNT_REF_PATTERN.test(String(outbox.sourceRef || "")), "INVALID_OUTBOX_SOURCE_REF");
      integerValue(outbox.pointMicros, "OUTBOX_POINT_MICROS");
      const remediationStatus = String(outbox.syntheticRemediationStatus || "NONE");
      assertCondition(["NONE", "REVERSED", "ROLLED_BACK"].includes(remediationStatus),
        "INVALID_OUTBOX_SYNTHETIC_REMEDIATION_STATUS");
      const operationRef = outbox.syntheticRemediationOperationRef == null
        ? null
        : String(outbox.syntheticRemediationOperationRef);
      const rollbackRef = outbox.syntheticRemediationRollbackRef == null
        ? null
        : String(outbox.syntheticRemediationRollbackRef);
      if (remediationStatus === "NONE") {
        assertCondition(operationRef == null && rollbackRef == null,
          "UNEXPECTED_OUTBOX_SYNTHETIC_REMEDIATION_REF");
      } else {
        assertCondition(ACCOUNT_REF_PATTERN.test(operationRef || ""),
          "INVALID_OUTBOX_SYNTHETIC_REMEDIATION_REF");
        assertCondition(remediationStatus === "ROLLED_BACK"
          ? ACCOUNT_REF_PATTERN.test(rollbackRef || "")
          : rollbackRef == null, "INVALID_OUTBOX_SYNTHETIC_REMEDIATION_ROLLBACK_REF");
      }
    }
  }
}

function addDecision(decisions, decision) {
  if (!decisions.some((item) => item.key === decision.key)) decisions.push(decision);
}

function classifyAccount(account) {
  const reviewClasses = [];
  const decisions = [];
  const webPointMicros = integerValue(account.balances.webPointMicros, "WEB_POINT_MICROS");
  const gameOpeningPointMicros = integerValue(
    account.balances.gameOpeningPointMicros,
    "GAME_OPENING_POINT_MICROS",
    { nullable: true }
  );
  const webTransactions = account.evidence.webPointTransactions;
  const unmatchedOutboxes = account.evidence.outboxes.filter(
    (outbox) => outbox.webSourceTransactionMatched === false
      && String(outbox.syntheticRemediationStatus || "NONE") !== "REVERSED"
  );
  const remediatedOutboxes = account.evidence.outboxes.filter(
    (outbox) => outbox.webSourceTransactionMatched === false
      && String(outbox.syntheticRemediationStatus || "NONE") === "REVERSED"
  );
  const unmatchedOutboxMicros = unmatchedOutboxes.reduce(
    (sum, outbox) => sum + integerValue(outbox.pointMicros, "OUTBOX_POINT_MICROS"),
    0n
  );
  const remediatedOutboxMicros = remediatedOutboxes.reduce(
    (sum, outbox) => sum + integerValue(outbox.pointMicros, "OUTBOX_POINT_MICROS"),
    0n
  );

  if (account.blockingIssues.includes("LEGACY_SUB_MICRO_VALUE_PRESENT")) {
    reviewClasses.push("LEGACY_SUB_MICRO_PRECISION");
    addDecision(decisions, {
      key: "legacySubMicroTreatment",
      owner: "FINANCE_OWNER",
      status: "PENDING",
      recommendation: "DEFER_ACCOUNT_LINK_UNTIL_EXPLICIT_PRECISION_POLICY",
      allowedValues: [
        "APPROVE_ROUND_HALF_EVEN_WITH_RESIDUAL_AUDIT",
        "CORRECT_SOURCE_LEDGER_AND_RERUN",
        "DEFER_ACCOUNT_LINK",
      ],
      rationale: "A sub-micro legacy value cannot be silently discarded or made spendable without approval.",
    });
  }

  const otherBlockingIssues = account.blockingIssues.filter(
    (issue) => issue !== "LEGACY_SUB_MICRO_VALUE_PRESENT"
  );
  if (otherBlockingIssues.length > 0) {
    reviewClasses.push("UNRESOLVED_REPORT_INVARIANT");
    addDecision(decisions, {
      key: "blockingInvariantResolution",
      owner: "ENGINEERING_AND_FINANCE",
      status: "PENDING",
      recommendation: "CORRECT_SOURCE_AND_RERUN_DRY_RUN",
      allowedValues: ["CORRECT_SOURCE_AND_RERUN_DRY_RUN", "DEFER_ACCOUNT_LINK"],
      rationale: "A structural report invariant must be fixed before any account link or migration.",
    });
  }

  if (gameOpeningPointMicros != null && gameOpeningPointMicros !== 0n) {
    reviewClasses.push("GAME_OPENING_BALANCE");
    addDecision(decisions, {
      key: "gameOpeningBalanceTreatment",
      owner: "PRODUCT_AND_FINANCE_OWNER",
      status: "PENDING",
      recommendation: "DEFER_ACCOUNT_LINK_UNTIL_WITHDRAWAL_LIABILITY_IS_APPROVED",
      allowedValues: [
        "PRESERVE_AS_FULL_WITHDRAWABLE_POINT",
        "AUDITED_REVERSAL_BEFORE_LINK",
        "DEFER_ACCOUNT_LINK",
      ],
      rationale: "With one Point wallet, a preserved bootstrap seed may become withdrawable real value.",
    });
  }

  if (unmatchedOutboxes.length > 0) {
    reviewClasses.push("SYNTHETIC_CREDIT_WITHOUT_WEB_SOURCE");
    addDecision(decisions, {
      key: "syntheticCreditTreatment",
      owner: "FINANCE_OWNER",
      status: "PENDING",
      recommendation: "AUDITED_REVERSAL_BEFORE_OPEN",
      allowedValues: [
        "AUDITED_REVERSAL_BEFORE_OPEN",
        "PRESERVE_AS_EXPLICIT_TEST_ADJUSTMENT",
        "DEFER_ACCOUNT_LINK",
      ],
      rationale: "A delivered test credit without a successful web source transaction is not funded Point.",
    });
  }

  if (webPointMicros !== 0n) {
    reviewClasses.push(account.gamePlayerRefs.length > 0
      ? "MAPPED_LEGACY_WEB_BALANCE"
      : "UNMAPPED_LEGACY_WEB_BALANCE");
    addDecision(decisions, {
      key: "legacyWebBalanceTreatment",
      owner: "PRODUCT_AND_FINANCE_OWNER",
      status: "PENDING",
      recommendation: "KEEP_FROZEN_UNTIL_LEGACY_POINT_SEMANTICS_ARE_APPROVED",
      allowedValues: [
        "MIGRATE_ONCE_TO_GAME_LEDGER",
        "CLASSIFY_AS_NON_POINT_LEGACY_AND_KEEP_FROZEN",
        "DEFER_ACCOUNT_LINK",
      ],
      rationale: "Legacy web GXL/Point must not be bulk-added to a game balance or silently discarded.",
    });
  } else if (webTransactions.length > 0) {
    reviewClasses.push("ZERO_BALANCE_WITH_LEGACY_WEB_HISTORY");
    addDecision(decisions, {
      key: "legacyWebHistoryTreatment",
      owner: "FINANCE_OWNER",
      status: "PENDING",
      recommendation: "ARCHIVE_HISTORY_WITH_NO_BALANCE_MIGRATION_AFTER_REVIEW",
      allowedValues: [
        "ARCHIVE_HISTORY_WITH_NO_BALANCE_MIGRATION",
        "INVESTIGATE_LEDGER_BEFORE_LINK",
        "DEFER_ACCOUNT_LINK",
      ],
      rationale: "A zero current balance still requires confirmation that legacy history creates no liability.",
    });
  }

  if (decisions.length === 0) {
    reviewClasses.push("MANUAL_CLASSIFICATION_REQUIRED");
    addDecision(decisions, {
      key: "manualClassification",
      owner: "ENGINEERING_AND_FINANCE",
      status: "PENDING",
      recommendation: "DEFER_ACCOUNT_LINK",
      allowedValues: ["APPROVE_NO_BALANCE_ACTION", "DEFER_ACCOUNT_LINK"],
      rationale: "The source report requires review but did not match a known decision class.",
    });
  }

  return {
    accountRef: account.accountRef,
    gamePlayerRefs: [...account.gamePlayerRefs],
    linkedInWebAuthority: Boolean(account.linkedInWebAuthority),
    sourceStatus: account.status,
    blockingIssues: [...account.blockingIssues],
    reviewReasons: [...account.reviewReasons],
    reviewClasses,
    facts: {
      webPointMicros: account.balances.webPointMicros,
      webLockedPointMicros: account.balances.webLockedPointMicros,
      gamePointMicros: account.balances.gamePointMicros,
      gameOpeningPointMicros: account.balances.gameOpeningPointMicros,
      unmatchedOutboxMicros: unmatchedOutboxMicros.toString(),
      unmatchedOutboxSourceRefs: unmatchedOutboxes.map((outbox) => outbox.sourceRef).sort(),
      ...(remediatedOutboxes.length > 0 ? {
        remediatedOutboxMicros: remediatedOutboxMicros.toString(),
        remediatedOutboxSourceRefs: remediatedOutboxes.map((outbox) => outbox.sourceRef).sort(),
      } : {}),
      webPointTransactions: webTransactions,
      gameTransactionCount: account.evidence.gameTransactionCount,
      gameLedgerDeltaMicros: account.evidence.gameLedgerDeltaMicros,
      legacySubMicroNormalization: account.evidence.legacySubMicroNormalization,
    },
    requiredDecisions: decisions,
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

function buildPointWalletMigrationDecisionWorksheet(report, options = {}) {
  validateReport(report);
  const generatedAt = new Date(options.generatedAt || Date.now());
  assertCondition(Number.isFinite(generatedAt.getTime()), "INVALID_WORKSHEET_GENERATED_AT");
  const reportSha256 = String(options.reportSha256 || "").toLowerCase();
  assertCondition(SHA256_PATTERN.test(reportSha256), "INVALID_REPORT_SHA256");
  const generatorSha256 = String(options.generatorSha256 || "").toLowerCase();
  assertCondition(SHA256_PATTERN.test(generatorSha256), "INVALID_GENERATOR_SHA256");

  const accounts = report.accounts
    .filter((account) => account.status !== "NO_ACTION")
    .map(classifyAccount)
    .sort((a, b) => a.accountRef.localeCompare(b.accountRef));
  const classCounts = {};
  let pendingDecisionCount = 0;
  let totalReviewWebPointMicros = 0n;
  let totalGameOpeningPointMicros = 0n;
  let totalSyntheticCreditMicros = 0n;
  let totalRemediatedSyntheticCreditMicros = 0n;
  for (const account of accounts) {
    pendingDecisionCount += account.requiredDecisions.length;
    totalReviewWebPointMicros += integerValue(account.facts.webPointMicros, "WEB_POINT_MICROS");
    totalGameOpeningPointMicros += integerValue(
      account.facts.gameOpeningPointMicros,
      "GAME_OPENING_POINT_MICROS",
      { nullable: true }
    ) || 0n;
    totalSyntheticCreditMicros += integerValue(
      account.facts.unmatchedOutboxMicros,
      "UNMATCHED_OUTBOX_MICROS"
    );
    totalRemediatedSyntheticCreditMicros += integerValue(
      account.facts.remediatedOutboxMicros || "0",
      "REMEDIATED_OUTBOX_MICROS"
    );
    for (const reviewClass of account.reviewClasses) {
      classCounts[reviewClass] = (classCounts[reviewClass] || 0) + 1;
    }
  }

  return {
    schemaVersion: 1,
    generatedAt: generatedAt.toISOString(),
    mode: "DECISION_WORKSHEET",
    automaticMigrationAllowed: false,
    migrationStatementsGenerated: 0,
    databaseMutationsPerformed: false,
    generator: {
      sha256: generatorSha256,
    },
    sourceReport: {
      sha256: reportSha256,
      schemaVersion: report.schemaVersion,
      generatedAt: report.generatedAt,
      referenceKeyId: report.referenceKeyId,
      accountCount: report.summary.accountCount,
      statusCounts: report.summary.statusCounts,
    },
    summary: {
      reviewAccountCount: accounts.length,
      blockedAccountCount: accounts.filter((account) => account.sourceStatus === "BLOCKED").length,
      pendingDecisionCount,
      classCounts,
      totalReviewWebPointMicros: totalReviewWebPointMicros.toString(),
      totalGameOpeningPointMicros: totalGameOpeningPointMicros.toString(),
      totalSyntheticCreditMicros: totalSyntheticCreditMicros.toString(),
      totalRemediatedSyntheticCreditMicros: totalRemediatedSyntheticCreditMicros.toString(),
      migrationGate: "BLOCKED_PENDING_EXPLICIT_APPROVALS",
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

function renderPointWalletMigrationDecisionMarkdown(worksheet) {
  const lines = [
    "# Point Wallet Migration Decision Worksheet",
    "",
    `- Generated at: \`${worksheet.generatedAt}\``,
    `- Source report SHA-256: \`${worksheet.sourceReport.sha256}\``,
    `- Generator SHA-256: \`${worksheet.generator.sha256}\``,
    `- Source reference key ID: \`${worksheet.sourceReport.referenceKeyId}\``,
    "- Safety: read-only decision evidence; no migration SQL and no database mutation.",
    "",
    "## Gate Summary",
    "",
    `- Review accounts: **${worksheet.summary.reviewAccountCount}**`,
    `- Source-blocked accounts: **${worksheet.summary.blockedAccountCount}**`,
    `- Pending decision items: **${worksheet.summary.pendingDecisionCount}**`,
    `- Legacy web Point under review: **${formatFixed(worksheet.summary.totalReviewWebPointMicros, 6)} Point**`,
    `- Game opening balance under review: **${formatFixed(worksheet.summary.totalGameOpeningPointMicros, 6)} Point**`,
    `- Synthetic credit without web source: **${formatFixed(worksheet.summary.totalSyntheticCreditMicros, 6)} Point**`,
    `- Synthetic credit already reversed with audit: **${formatFixed(worksheet.summary.totalRemediatedSyntheticCreditMicros, 6)} Point**`,
    `- Migration gate: **${worksheet.summary.migrationGate}**`,
    "",
    "## Account Decision Table",
    "",
    "| Account ref | Game ref(s) | Source status | Review class(es) | Web Point | Game Point | Opening Point | Decision key(s) |",
    "|---|---|---|---|---:|---:|---:|---|",
  ];
  for (const account of worksheet.accounts) {
    lines.push(`| \`${account.accountRef}\` | ${markdownCell(account.gamePlayerRefs.join(", ") || "-")} | ${markdownCell(account.sourceStatus)} | ${markdownCell(account.reviewClasses.join(", "))} | ${formatFixed(account.facts.webPointMicros, 6)} | ${formatFixed(account.facts.gamePointMicros, 6)} | ${formatFixed(account.facts.gameOpeningPointMicros, 6)} | ${markdownCell(account.requiredDecisions.map((item) => item.key).join(", "))} |`);
  }
  lines.push("", "## Approval Records", "");
  for (const account of worksheet.accounts) {
    lines.push(`### \`${account.accountRef}\``, "");
    lines.push(`- Source status: \`${account.sourceStatus}\``);
    lines.push(`- Blocking issues: ${account.blockingIssues.map((item) => `\`${item}\``).join(", ") || "none"}`);
    lines.push(`- Review reasons: ${account.reviewReasons.map((item) => `\`${item}\``).join(", ") || "none"}`);
    lines.push(`- Synthetic source refs: ${account.facts.unmatchedOutboxSourceRefs.map((item) => `\`${item}\``).join(", ") || "none"}`);
    lines.push(`- Remediated synthetic source refs: ${(account.facts.remediatedOutboxSourceRefs || []).map((item) => `\`${item}\``).join(", ") || "none"}`);
    for (const decision of account.requiredDecisions) {
      lines.push(`- Decision \`${decision.key}\` (${decision.owner}): **PENDING**`);
      lines.push(`  - Recommendation: \`${decision.recommendation}\``);
      lines.push(`  - Allowed values: ${decision.allowedValues.map((item) => `\`${item}\``).join(", ")}`);
      lines.push(`  - Rationale: ${decision.rationale}`);
    }
    lines.push("- Selected value(s): _pending_", "- Approved by / at: _pending_", "- Evidence / notes: _pending_", "");
  }
  return `${lines.join("\n")}\n`;
}

function parseCliArgs(argv) {
  const allowed = new Set(["--expected-sha256", "--format", "--generated-at", "--output", "--report"]);
  const args = {};
  for (let index = 0; index < argv.length; index += 2) {
    const key = argv[index];
    const value = argv[index + 1];
    if (!allowed.has(key)) throw new Error("INVALID_ARGUMENT");
    if (!value) throw new Error("MISSING_ARGUMENT_VALUE");
    args[key.slice(2)] = value;
  }
  if (!args.report || !args.output || !args["expected-sha256"]) {
    throw new Error("REPORT_OUTPUT_AND_EXPECTED_SHA256_REQUIRED");
  }
  args.format = args.format || "markdown";
  if (!["json", "markdown"].includes(args.format)) throw new Error("INVALID_OUTPUT_FORMAT");
  return args;
}

if (require.main === module) {
  try {
    const args = parseCliArgs(process.argv.slice(2));
    const reportPath = path.resolve(args.report);
    const outputPath = path.resolve(args.output);
    assertCondition(reportPath !== outputPath, "OUTPUT_MUST_NOT_OVERWRITE_REPORT");
    const rawReport = fs.readFileSync(reportPath);
    const reportSha256 = crypto.createHash("sha256").update(rawReport).digest("hex");
    assertCondition(reportSha256 === String(args["expected-sha256"]).toLowerCase(),
      "REPORT_SHA256_MISMATCH");
    const report = JSON.parse(rawReport.toString("utf8"));
    const generatorSha256 = crypto.createHash("sha256")
      .update(fs.readFileSync(__filename))
      .digest("hex");
    const worksheet = buildPointWalletMigrationDecisionWorksheet(report, {
      reportSha256,
      generatorSha256,
      generatedAt: args["generated-at"],
    });
    const output = args.format === "json"
      ? `${JSON.stringify(worksheet, null, 2)}\n`
      : renderPointWalletMigrationDecisionMarkdown(worksheet);
    fs.writeFileSync(outputPath, output, { encoding: "utf8", flag: "wx", mode: 0o600 });
    if (process.platform !== "win32") fs.chmodSync(outputPath, 0o600);
    process.stdout.write(`[point-wallet-decision-worksheet] CREATED accounts=${worksheet.summary.reviewAccountCount} pending=${worksheet.summary.pendingDecisionCount}\n`);
  } catch (error) {
    process.stderr.write(`[point-wallet-decision-worksheet] ${error && error.message || "FAILED"}\n`);
    process.exitCode = 1;
  }
}

module.exports = {
  buildPointWalletMigrationDecisionWorksheet,
  renderPointWalletMigrationDecisionMarkdown,
  validateReport,
};
