"use strict";

const crypto = require("crypto");
const fs = require("fs");
const path = require("path");
const { Client } = require("pg");
const {
  validatePointWalletRemediationOperationApproval,
} = require("./pointWalletRemediationOperationApproval");

const POINT_MICROS = 1_000_000n;
const REPORT_DOMAIN = "ywonder-point-migration-report-v1";
const REF_PATTERN = /^[a-f0-9]{24}$/;
const SHA256_PATTERN = /^[a-f0-9]{64}$/;
const OPERATION_PATTERN = /^point-remediation:[a-f0-9]{32}$/;

function assertCondition(condition, code) {
  if (!condition) throw new Error(code);
}

function objectValue(value, field) {
  assertCondition(value && typeof value === "object" && !Array.isArray(value), `INVALID_${field}`);
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
  const text = String(value || "").toLowerCase();
  assertCondition(SHA256_PATTERN.test(text), `INVALID_${field}_SHA256`);
  return text;
}

function publicRef(referenceKey, kind, rawValue) {
  return crypto.createHmac("sha256", referenceKey)
    .update(`${REPORT_DOMAIN}\0${kind}\0${rawValue}`, "utf8")
    .digest("hex")
    .slice(0, 24);
}

function stableValue(value) {
  if (Array.isArray(value)) return value.map(stableValue);
  if (!value || typeof value !== "object") return value;
  return Object.fromEntries(Object.keys(value).sort().map((key) => [key, stableValue(value[key])]));
}

function stableSignature(value) {
  return crypto.createHash("sha256").update(JSON.stringify(stableValue(value))).digest("hex");
}

function verifiedJson(filePath, expectedSha256, field) {
  const raw = fs.readFileSync(filePath);
  const actualSha256 = crypto.createHash("sha256").update(raw).digest("hex");
  assertCondition(actualSha256 === sha256Value(expectedSha256, field), `${field}_SHA256_MISMATCH`);
  return { value: objectValue(JSON.parse(raw.toString("utf8")), field), sha256: actualSha256 };
}

function syntheticPlanValue(plan) {
  const plans = arrayValue(plan.syntheticReversalPlans, "SYNTHETIC_REVERSAL_PLANS");
  assertCondition(plans.length === 1 && plan.summary.syntheticReversalAccountCount === 1,
    "SYNTHETIC_REVERSAL_PLAN_COUNT_NOT_ONE");
  const item = objectValue(plans[0], "SYNTHETIC_REVERSAL_PLAN");
  assertCondition(REF_PATTERN.test(String(item.accountRef || "")), "INVALID_SYNTHETIC_ACCOUNT_REF");
  assertCondition(REF_PATTERN.test(String(item.gamePlayerRef || "")), "INVALID_SYNTHETIC_PLAYER_REF");
  assertCondition(OPERATION_PATTERN.test(String(item.proposedOperationId || "")),
    "INVALID_SYNTHETIC_OPERATION_ID");
  assertCondition(item.operationStatus === "NOT_AUTHORIZED", "SOURCE_SYNTHETIC_OPERATION_STATUS_INVALID");
  const currentMicros = integerValue(item.currentGamePointMicros,
    "SYNTHETIC_PLAN_CURRENT_MICROS", { nonNegative: true });
  const reversalMicros = integerValue(item.reversalPointMicros,
    "SYNTHETIC_PLAN_REVERSAL_MICROS", { positive: true });
  const expectedAfterMicros = integerValue(item.expectedGamePointMicrosAfter,
    "SYNTHETIC_PLAN_EXPECTED_AFTER_MICROS", { nonNegative: true });
  assertCondition(reversalMicros === integerValue(plan.summary.syntheticReversalPointMicros,
    "SYNTHETIC_SUMMARY_REVERSAL_MICROS", { positive: true }),
  "SYNTHETIC_PLAN_SUMMARY_AMOUNT_MISMATCH");
  assertCondition(currentMicros - reversalMicros === expectedAfterMicros,
    "SYNTHETIC_PLAN_BALANCE_ARITHMETIC_MISMATCH");
  const sources = arrayValue(item.sources, "SYNTHETIC_SOURCES");
  assertCondition(sources.length === plan.summary.syntheticReversalSourceCount,
    "SYNTHETIC_SOURCE_COUNT_MISMATCH");
  const seen = new Set();
  const seenCredits = new Set();
  let sourceMicros = 0n;
  for (const source of sources) {
    assertCondition(REF_PATTERN.test(String(source.sourceRef || ""))
      && REF_PATTERN.test(String(source.gameCreditRef || "")), "INVALID_SYNTHETIC_SOURCE_REF");
    assertCondition(!seen.has(source.sourceRef), "DUPLICATE_SYNTHETIC_SOURCE_REF");
    seen.add(source.sourceRef);
    assertCondition(!seenCredits.has(source.gameCreditRef), "DUPLICATE_SYNTHETIC_GAME_CREDIT_REF");
    seenCredits.add(source.gameCreditRef);
    assertCondition(source.evidenceStatus === "VERIFIED_DELIVERED_WITHOUT_WEB_TRANSACTION",
      "INVALID_SYNTHETIC_SOURCE_EVIDENCE_STATUS");
    sourceMicros += integerValue(source.pointMicros, "SYNTHETIC_SOURCE_MICROS", { positive: true });
  }
  assertCondition(sourceMicros === reversalMicros, "SYNTHETIC_SOURCE_AMOUNT_SUM_MISMATCH");
  return item;
}

function resolveWebEvidence(webSnapshot, planItem, referenceKey) {
  assertCondition(webSnapshot.schemaVersion === 2, "UNSUPPORTED_WEB_SNAPSHOT_SCHEMA");
  const userIds = new Set();
  for (const row of arrayValue(webSnapshot.users, "WEB_USERS")) userIds.add(String(row.userId || ""));
  for (const row of arrayValue(webSnapshot.wallets, "WEB_WALLETS")) userIds.add(String(row.userId || ""));
  for (const row of arrayValue(webSnapshot.transactions, "WEB_TRANSACTIONS")) userIds.add(String(row.userId || ""));
  for (const row of arrayValue(webSnapshot.outboxes, "WEB_OUTBOXES")) userIds.add(String(row.userId || ""));
  for (const row of arrayValue(webSnapshot.links, "WEB_LINKS")) userIds.add(String(row.userId || ""));
  const matchedUsers = [...userIds].filter(
    (userId) => publicRef(referenceKey, "web-user", userId) === planItem.accountRef
  );
  assertCondition(matchedUsers.length === 1, "SYNTHETIC_WEB_ACCOUNT_MAPPING_NOT_UNIQUE");
  const webUserId = matchedUsers[0];
  const transactions = new Map();
  for (const row of arrayValue(webSnapshot.transactions, "WEB_TRANSACTIONS")) {
    const transactionId = String(row.transactionId || row.id || "");
    assertCondition(transactionId && !transactions.has(transactionId), "DUPLICATE_WEB_TRANSACTION");
    transactions.set(transactionId, row);
  }
  const expectedByRef = new Map(planItem.sources.map((source) => [source.sourceRef, source]));
  const sources = [];
  for (const row of arrayValue(webSnapshot.outboxes, "WEB_OUTBOXES")) {
    const rawSource = String(row.sourceTransactionId || "");
    const sourceRef = publicRef(referenceKey, "source", rawSource);
    if (!expectedByRef.has(sourceRef)) continue;
    assertCondition(String(row.userId || "") === webUserId, "SYNTHETIC_OUTBOX_USER_MISMATCH");
    assertCondition(String(row.status || "").toUpperCase() === "SENT", "SYNTHETIC_OUTBOX_NOT_SENT");
    assertCondition(!transactions.has(rawSource), "SYNTHETIC_WEB_SOURCE_TRANSACTION_NOW_EXISTS");
    const expected = expectedByRef.get(sourceRef);
    assertCondition(integerValue(row.pointMicros, "OUTBOX_POINT_MICROS", { positive: true })
      === integerValue(expected.pointMicros, "PLAN_SOURCE_POINT_MICROS", { positive: true }),
    "SYNTHETIC_OUTBOX_AMOUNT_DRIFT");
    sources.push({
      sourceTransactionId: rawSource,
      sourceRef,
      gameCreditRef: expected.gameCreditRef,
      pointMicros: String(expected.pointMicros),
    });
  }
  sources.sort((a, b) => a.sourceRef.localeCompare(b.sourceRef));
  assertCondition(sources.length === expectedByRef.size, "SYNTHETIC_OUTBOX_EVIDENCE_COUNT_MISMATCH");
  return { webUserId, sources };
}

function mappedPlayerFromRows(rows, webUserId, planItem, referenceKey) {
  const accountMatches = rows.filter(
    (row) => publicRef(referenceKey, "web-user", String(row.web_user_id || "")) === planItem.accountRef
  );
  assertCondition(accountMatches.length === 1, "SYNTHETIC_GAME_ACCOUNT_MAPPING_NOT_UNIQUE");
  const row = accountMatches[0];
  assertCondition(String(row.web_user_id) === webUserId, "SYNTHETIC_WEB_GAME_IDENTITY_MISMATCH");
  assertCondition(publicRef(referenceKey, "player", String(row.player_id)) === planItem.gamePlayerRef,
    "SYNTHETIC_GAME_PLAYER_REF_MISMATCH");
  return row;
}

function verifyCreditRows(rows, evidence, planItem, referenceKey, expectedPlayerId) {
  const bySource = new Map();
  for (const row of rows) {
    const source = String(row.ref || "");
    if (!bySource.has(source)) bySource.set(source, []);
    bySource.get(source).push(row);
  }
  const sourcePlanByRef = new Map(planItem.sources.map((source) => [source.sourceRef, source]));
  for (const source of evidence.sources) {
    const credits = bySource.get(source.sourceTransactionId) || [];
    assertCondition(credits.length === 1, "SYNTHETIC_GAME_CREDIT_COUNT_NOT_ONE");
    const credit = credits[0];
    assertCondition(String(credit.player_id || "") === String(expectedPlayerId),
      "SYNTHETIC_GAME_CREDIT_PLAYER_MISMATCH");
    assertCondition(String(credit.type || "").toLowerCase() === "web_topup_credit",
      "SYNTHETIC_GAME_CREDIT_TYPE_MISMATCH");
    const expected = sourcePlanByRef.get(source.sourceRef);
    assertCondition(publicRef(referenceKey, "game-transaction", String(credit.transaction_id))
      === expected.gameCreditRef, "SYNTHETIC_GAME_CREDIT_REF_MISMATCH");
    const details = objectValue(credit.details_json || {}, "SYNTHETIC_GAME_CREDIT_DETAILS");
    const amount = integerValue(details.pointAmountMicros, "GAME_CREDIT_POINT_MICROS", { positive: true });
    const before = integerValue(details.pointMicrosRemainderBefore,
      "GAME_CREDIT_REMAINDER_BEFORE", { nonNegative: true });
    const after = integerValue(details.pointMicrosRemainderAfter,
      "GAME_CREDIT_REMAINDER_AFTER", { nonNegative: true });
    const delta = integerValue(credit.delta_pos, "GAME_CREDIT_DELTA_POINT");
    assertCondition(amount === integerValue(expected.pointMicros, "PLAN_CREDIT_POINT_MICROS", { positive: true }),
      "SYNTHETIC_GAME_CREDIT_AMOUNT_MISMATCH");
    assertCondition(before < POINT_MICROS && after < POINT_MICROS,
      "SYNTHETIC_GAME_CREDIT_REMAINDER_OUT_OF_RANGE");
    assertCondition(delta === (before + amount) / POINT_MICROS
      && after === (before + amount) % POINT_MICROS,
    "SYNTHETIC_GAME_CREDIT_REMAINDER_ARITHMETIC_MISMATCH");
  }
}

function rollbackOperationId(operationId, planSha256) {
  const digest = crypto.createHash("sha256")
    .update(`point-remediation-rollback-v1\0${operationId}\0${planSha256}`)
    .digest("hex").slice(0, 32);
  return `point-remediation-rollback:${digest}`;
}

function operationPayload(action, planItem, planSha256, approvalSha256) {
  return {
    action,
    operationId: action === "APPLY"
      ? planItem.proposedOperationId
      : rollbackOperationId(planItem.proposedOperationId, planSha256),
    originalOperationId: planItem.proposedOperationId,
    remediationPlanSha256: planSha256,
    operationApprovalSha256: approvalSha256,
    accountRef: planItem.accountRef,
    gamePlayerRef: planItem.gamePlayerRef,
    pointMicros: String(planItem.reversalPointMicros),
    sourceRefs: planItem.sources.map((source) => source.sourceRef).sort(),
    gameCreditRefs: planItem.sources.map((source) => source.gameCreditRef).sort(),
  };
}

function verifyStoredOperation(row, payload, signature, expectedType, expectedPlayerId) {
  assertCondition(row && row.id === payload.operationId && row.idempotency_key === payload.operationId,
    "STORED_REMEDIATION_OPERATION_ID_MISMATCH");
  assertCondition(String(row.player_id || "") === String(expectedPlayerId),
    "STORED_REMEDIATION_PLAYER_MISMATCH");
  assertCondition(row.type === expectedType && row.ref === payload.originalOperationId,
    "STORED_REMEDIATION_OPERATION_TYPE_MISMATCH");
  assertCondition(row.request_signature === signature, "STORED_REMEDIATION_SIGNATURE_MISMATCH");
  assertCondition(integerValue(row.delta_upos, "STORED_REMEDIATION_DELTA_UPOINT") === 0n
    && row.item_id == null && row.quantity_delta == null,
  "STORED_REMEDIATION_NON_POINT_DELTA_MISMATCH");
  const details = objectValue(row.details_json || {}, "STORED_REMEDIATION_DETAILS");
  const expectedKind = payload.action === "APPLY"
    ? "SYNTHETIC_CREDIT_REVERSAL"
    : "SYNTHETIC_CREDIT_REVERSAL_ROLLBACK";
  assertCondition(details.remediationPlanSha256 === payload.remediationPlanSha256
    && details.operationApprovalSha256 === payload.operationApprovalSha256
    && details.originalOperationId === payload.originalOperationId
    && details.remediationKind === expectedKind
    && details.pointAmountMicros === payload.pointMicros
    && JSON.stringify(details.sourceRefs) === JSON.stringify(payload.sourceRefs)
    && JSON.stringify(details.gameCreditRefs) === JSON.stringify(payload.gameCreditRefs),
  "STORED_REMEDIATION_DETAILS_MISMATCH");
  const beforeRemainder = integerValue(details.pointMicrosRemainderBefore,
    "STORED_REMEDIATION_REMAINDER_BEFORE", { nonNegative: true });
  const afterRemainder = integerValue(details.pointMicrosRemainderAfter,
    "STORED_REMEDIATION_REMAINDER_AFTER", { nonNegative: true });
  assertCondition(beforeRemainder < POINT_MICROS && afterRemainder < POINT_MICROS,
    "STORED_REMEDIATION_REMAINDER_OUT_OF_RANGE");
  const result = objectValue(row.result_json || {}, "STORED_REMEDIATION_RESULT");
  const before = objectValue(result.economyBefore, "STORED_REMEDIATION_ECONOMY_BEFORE");
  const after = objectValue(result.economyAfter, "STORED_REMEDIATION_ECONOMY_AFTER");
  assertCondition(result.ok === true && result.operationId === payload.operationId
    && result.action === payload.action,
  "STORED_REMEDIATION_RESULT_MISMATCH");
  const beforePoint = integerValue(before.point, "STORED_REMEDIATION_POINT_BEFORE", { nonNegative: true });
  const afterPoint = integerValue(after.point, "STORED_REMEDIATION_POINT_AFTER", { nonNegative: true });
  assertCondition(integerValue(before.pointMicrosRemainder,
    "STORED_REMEDIATION_RESULT_REMAINDER_BEFORE", { nonNegative: true }) === beforeRemainder
    && integerValue(after.pointMicrosRemainder,
      "STORED_REMEDIATION_RESULT_REMAINDER_AFTER", { nonNegative: true }) === afterRemainder,
  "STORED_REMEDIATION_RESULT_REMAINDER_MISMATCH");
  const beforeMicros = beforePoint * POINT_MICROS + beforeRemainder;
  const afterMicros = afterPoint * POINT_MICROS + afterRemainder;
  const signedMicros = payload.action === "APPLY"
    ? -integerValue(payload.pointMicros, "STORED_REMEDIATION_POINT_MICROS", { positive: true })
    : integerValue(payload.pointMicros, "STORED_REMEDIATION_POINT_MICROS", { positive: true });
  assertCondition(afterMicros - beforeMicros === signedMicros
    && integerValue(row.delta_pos, "STORED_REMEDIATION_DELTA_POINT") === afterPoint - beforePoint,
  "STORED_REMEDIATION_BALANCE_ARITHMETIC_MISMATCH");
}

async function queryExistingOperation(client, operationId) {
  const result = await client.query(
    `/* point_remediation_existing */
     select * from game_transactions where id=$1 or idempotency_key=$1 order by id`,
    [operationId]
  );
  assertCondition(result.rows.length <= 1, "DUPLICATE_REMEDIATION_OPERATION_ROWS");
  return result.rows[0] || null;
}

async function mappedRows(client) {
  const result = await client.query(
    `/* point_remediation_mapped_players */
     select p.id as player_id, p.web_user_id
     from game_players p
     where p.web_user_id is not null and btrim(p.web_user_id) <> ''
     order by p.web_user_id, p.id`
  );
  return result.rows;
}

async function lockPlayer(client, playerId) {
  const result = await client.query(
    `/* point_remediation_lock_player */
     select p.id as player_id, p.web_user_id, p.active_session_id,
            e.pos, e.web_point_micros_remainder
     from game_players p
     join player_economy e on e.player_id=p.id
     where p.id=$1
     for update of p,e`,
    [playerId]
  );
  assertCondition(result.rows.length === 1, "SYNTHETIC_PLAYER_OR_ECONOMY_MISSING");
  return result.rows[0];
}

async function loadCredits(client, rawSources) {
  const result = await client.query(
    `/* point_remediation_source_credits */
     select id as transaction_id, player_id, type, ref, delta_pos, details_json
     from game_transactions
     where ref = any($1::text[])
     order by ref, created_at, id`,
    [rawSources]
  );
  return result.rows;
}

async function writeReceipt(outputPath, receipt) {
  const resolved = path.resolve(outputPath);
  fs.writeFileSync(resolved, `${JSON.stringify(receipt, null, 2)}\n`, {
    encoding: "utf8", flag: "wx", mode: 0o600,
  });
  if (process.platform !== "win32") fs.chmodSync(resolved, 0o600);
}

async function executeSyntheticReversal(client, webSnapshot, plan, approvalContext, options) {
  const referenceKey = String(options.referenceKey || "");
  assertCondition(Buffer.byteLength(referenceKey, "utf8") >= 32, "REPORT_REFERENCE_KEY_TOO_SHORT");
  const referenceKeyId = crypto.createHash("sha256").update(referenceKey).digest("hex").slice(0, 16);
  assertCondition(plan.sources.currentReport.referenceKeyId === referenceKeyId, "REFERENCE_KEY_ID_MISMATCH");
  const planItem = syntheticPlanValue(plan);
  const evidence = resolveWebEvidence(webSnapshot, planItem, referenceKey);
  const action = options.action;
  assertCondition(action === "apply" || action === "rollback", "INVALID_SYNTHETIC_REVERSAL_ACTION");
  const payload = operationPayload(action === "apply" ? "APPLY" : "ROLLBACK", planItem,
    approvalContext.planSha256, approvalContext.approvalSha256);
  const signature = stableSignature(payload);
  const operationType = action === "apply"
    ? "point_remediation_reversal"
    : "point_remediation_reversal_rollback";
  let transactionOpen = false;
  try {
    await client.query("BEGIN TRANSACTION ISOLATION LEVEL SERIALIZABLE");
    transactionOpen = true;
    await client.query("select pg_advisory_xact_lock(hashtext($1)::bigint)", [payload.operationId]);
    const mapped = await mappedRows(client);
    const target = mappedPlayerFromRows(mapped, evidence.webUserId, planItem, referenceKey);
    const existing = await queryExistingOperation(client, payload.operationId);
    if (existing) {
      verifyStoredOperation(existing, payload, signature, operationType, target.player_id);
      await client.query("COMMIT");
      transactionOpen = false;
      return { idempotent: true, databaseMutated: false, payload, transaction: existing };
    }

    const locked = await lockPlayer(client, target.player_id);
    assertCondition(String(locked.web_user_id) === evidence.webUserId,
      "LOCKED_SYNTHETIC_IDENTITY_MISMATCH");
    assertCondition(!String(locked.active_session_id || "").trim(),
      "SYNTHETIC_PLAYER_HAS_ACTIVE_SESSION");
    const currentPos = integerValue(locked.pos, "CURRENT_GAME_POINT", { nonNegative: true });
    const currentRemainder = integerValue(locked.web_point_micros_remainder,
      "CURRENT_GAME_REMAINDER", { nonNegative: true });
    assertCondition(currentRemainder < POINT_MICROS, "CURRENT_GAME_REMAINDER_OUT_OF_RANGE");
    const currentMicros = currentPos * POINT_MICROS + currentRemainder;
    const reversalMicros = integerValue(planItem.reversalPointMicros,
      "SYNTHETIC_REVERSAL_MICROS", { positive: true });
    const plannedCurrent = integerValue(planItem.currentGamePointMicros,
      "PLANNED_CURRENT_GAME_MICROS", { nonNegative: true });

    const credits = await loadCredits(client, evidence.sources.map((source) => source.sourceTransactionId));
    verifyCreditRows(credits, evidence, planItem, referenceKey, target.player_id);
    const signedMicros = action === "apply" ? -reversalMicros : reversalMicros;
    const targetMicros = currentMicros + signedMicros;
    assertCondition(targetMicros >= 0n, "SYNTHETIC_REVERSAL_WOULD_NEGATE_BALANCE");
    if (action === "apply") {
      assertCondition(currentMicros === plannedCurrent, "SYNTHETIC_CURRENT_BALANCE_DRIFT");
      assertCondition(targetMicros === integerValue(planItem.expectedGamePointMicrosAfter,
        "PLANNED_GAME_MICROS_AFTER", { nonNegative: true }), "SYNTHETIC_EXPECTED_BALANCE_MISMATCH");
    } else {
      const original = await queryExistingOperation(client, planItem.proposedOperationId);
      assertCondition(Boolean(original), "SYNTHETIC_REVERSAL_NOT_APPLIED");
      const originalPayload = operationPayload("APPLY", planItem,
        approvalContext.planSha256, approvalContext.approvalSha256);
      verifyStoredOperation(original, originalPayload, stableSignature(originalPayload),
        "point_remediation_reversal", target.player_id);
      const plannedAfter = integerValue(planItem.expectedGamePointMicrosAfter,
        "PLANNED_GAME_MICROS_AFTER", { nonNegative: true });
      assertCondition(currentMicros === plannedAfter, "SYNTHETIC_ROLLBACK_BALANCE_DRIFT");
      assertCondition(targetMicros === plannedCurrent, "SYNTHETIC_ROLLBACK_TARGET_MISMATCH");
    }

    const nextPos = targetMicros / POINT_MICROS;
    const nextRemainder = targetMicros % POINT_MICROS;
    const update = await client.query(
      `/* point_remediation_update_economy */
       update player_economy
       set pos=$2, web_point_micros_remainder=$3, updated_at=now()
       where player_id=$1 and pos=$4 and web_point_micros_remainder=$5
       returning pos, web_point_micros_remainder`,
      [target.player_id, nextPos.toString(), nextRemainder.toString(),
        currentPos.toString(), currentRemainder.toString()]
    );
    assertCondition(update.rows.length === 1, "SYNTHETIC_ECONOMY_COMPARE_AND_SWAP_FAILED");
    const deltaPos = nextPos - currentPos;
    const details = {
      remediationKind: action === "apply"
        ? "SYNTHETIC_CREDIT_REVERSAL"
        : "SYNTHETIC_CREDIT_REVERSAL_ROLLBACK",
      remediationPlanSha256: approvalContext.planSha256,
      operationApprovalSha256: approvalContext.approvalSha256,
      originalOperationId: planItem.proposedOperationId,
      pointAmountMicros: reversalMicros.toString(),
      pointMicrosRemainderBefore: currentRemainder.toString(),
      pointMicrosRemainderAfter: nextRemainder.toString(),
      sourceRefs: payload.sourceRefs,
      gameCreditRefs: payload.gameCreditRefs,
    };
    const resultJson = {
      ok: true,
      operationId: payload.operationId,
      action: payload.action,
      economyBefore: { point: currentPos.toString(), pointMicrosRemainder: currentRemainder.toString() },
      economyAfter: { point: nextPos.toString(), pointMicrosRemainder: nextRemainder.toString() },
    };
    const inserted = await client.query(
      `/* point_remediation_insert_ledger */
       insert into game_transactions
       (id,player_id,type,ref,idempotency_key,request_signature,delta_pos,delta_upos,
        item_id,quantity_delta,details_json,result_json,created_at)
       values ($1,$2,$3,$4,$5,$6,$7,0,null,null,$8::jsonb,$9::jsonb,$10)
       returning *`,
      [
        payload.operationId, target.player_id, operationType, planItem.proposedOperationId,
        payload.operationId, signature, deltaPos.toString(), JSON.stringify(details),
        JSON.stringify(resultJson), options.occurredAt,
      ]
    );
    assertCondition(inserted.rows.length === 1, "SYNTHETIC_REMEDIATION_LEDGER_INSERT_FAILED");
    await client.query("COMMIT");
    transactionOpen = false;
    return {
      idempotent: false,
      databaseMutated: true,
      payload,
      transaction: inserted.rows[0],
      before: { pointMicros: currentMicros.toString() },
      after: { pointMicros: targetMicros.toString() },
    };
  } catch (error) {
    if (transactionOpen) {
      try { await client.query("ROLLBACK"); } catch { /* Preserve the original failure. */ }
    }
    throw error;
  }
}

function parseArgs(argv) {
  const allowed = new Set([
    "--action", "--approval", "--approval-sha256", "--occurred-at", "--output",
    "--plan", "--plan-sha256", "--web-sha256", "--web-snapshot",
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
  for (const key of [
    "action", "approval", "approval-sha256", "occurred-at", "output", "plan",
    "plan-sha256", "web-sha256", "web-snapshot",
  ]) assertCondition(Boolean(args[key]), "REQUIRED_ARGUMENT_MISSING");
  assertCondition(args.action === "apply" || args.action === "rollback", "INVALID_ACTION");
  assertCondition(Number.isFinite(Date.parse(args["occurred-at"])), "INVALID_OCCURRED_AT");
  return args;
}

async function main() {
  const args = parseArgs(process.argv.slice(2));
  const referenceKey = String(process.env.POINT_MIGRATION_REPORT_KEY || "");
  const connectionString = String(process.env.DATABASE_URL || "").trim();
  assertCondition(connectionString, "DATABASE_URL_REQUIRED");
  const plan = verifiedJson(args.plan, args["plan-sha256"], "REMEDIATION_PLAN");
  const approval = verifiedJson(args.approval, args["approval-sha256"], "OPERATION_APPROVAL");
  const web = verifiedJson(args["web-snapshot"], args["web-sha256"], "WEB_SNAPSHOT");
  const approvalContext = validatePointWalletRemediationOperationApproval(
    approval.value, plan.value, {
      approvalSha256: approval.sha256,
      planSha256: plan.sha256,
    }
  );
  const client = new Client({ connectionString, statement_timeout: 30_000, query_timeout: 35_000 });
  await client.connect();
  try {
    const result = await executeSyntheticReversal(client, web.value, plan.value, approvalContext, {
      action: args.action,
      occurredAt: args["occurred-at"],
      referenceKey,
    });
    const planItem = syntheticPlanValue(plan.value);
    const receipt = {
      schemaVersion: 1,
      generatedAt: args["occurred-at"],
      mode: "POINT_WALLET_SYNTHETIC_REVERSAL_RECEIPT",
      action: args.action.toUpperCase(),
      idempotentReplay: result.idempotent,
      databaseMutationsPerformed: result.databaseMutated,
      containsRawIdentities: false,
      sources: {
        remediationPlanSha256: plan.sha256,
        operationApprovalSha256: approval.sha256,
        webSnapshotSha256: web.sha256,
      },
      operation: {
        operationId: result.payload.operationId,
        originalOperationId: planItem.proposedOperationId,
        accountRef: planItem.accountRef,
        gamePlayerRef: planItem.gamePlayerRef,
        pointMicros: String(planItem.reversalPointMicros),
        sourceRefs: result.payload.sourceRefs,
        gameCreditRefs: result.payload.gameCreditRefs,
      },
      before: result.before || null,
      after: result.after || null,
      requiredNextGate: "FRESH_READ_ONLY_RECONCILIATION",
    };
    await writeReceipt(args.output, receipt);
    process.stdout.write(`[point-wallet-synthetic-reversal] action=${args.action} idempotent=${result.idempotent} mutated=${result.databaseMutated} operation=${result.payload.operationId}\n`);
  } finally {
    await client.end();
  }
}

if (require.main === module) {
  main().catch((error) => {
    process.stderr.write(`[point-wallet-synthetic-reversal] ${error && error.message || "FAILED"}\n`);
    process.exitCode = 1;
  });
}

module.exports = {
  executeSyntheticReversal,
  operationPayload,
  publicRef,
  resolveWebEvidence,
  rollbackOperationId,
  stableSignature,
  syntheticPlanValue,
  verifyCreditRows,
};
