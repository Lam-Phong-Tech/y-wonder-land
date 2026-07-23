"use strict";

const crypto = require("crypto");
const fs = require("fs");

const POINT_MICROS = 1_000_000n;
const HALF_MICRO_POINT_ATTOS = 500_000_000_000n;
const REPORT_DOMAIN = "ywonder-point-migration-report-v1";
const PUBLIC_REF_PATTERN = /^[a-f0-9]{24}$/;
const SHA256_PATTERN = /^[a-f0-9]{64}$/;
const APPLY_OPERATION_PATTERN = /^point-remediation:[a-f0-9]{32}$/;
const ROLLBACK_OPERATION_PATTERN = /^point-remediation-rollback:[a-f0-9]{32}$/;

function asArray(value, field) {
  if (value == null) return [];
  if (!Array.isArray(value)) throw new Error(`INVALID_${field.toUpperCase()}`);
  return value;
}

function asObject(value, field) {
  if (!value || typeof value !== "object" || Array.isArray(value)) {
    throw new Error(`INVALID_${field.toUpperCase()}`);
  }
  return value;
}

function requiredId(value, field) {
  const text = String(value || "").trim();
  if (!text || text.length > 256 || /[\r\n\0]/.test(text)) {
    throw new Error(`INVALID_${field.toUpperCase()}`);
  }
  return text;
}

function optionalText(value, field, maxLength = 256) {
  const text = String(value || "").trim();
  if (text.length > maxLength || /[\r\n\0]/.test(text)) {
    throw new Error(`INVALID_${field.toUpperCase()}`);
  }
  return text;
}

function requiredPattern(value, field, pattern) {
  const text = String(value || "").trim();
  if (!pattern.test(text)) throw new Error(`INVALID_${field.toUpperCase()}`);
  return text;
}

function integerValue(value, field, options = {}) {
  const text = String(value == null ? "" : value).trim();
  if (!/^-?(0|[1-9]\d*)$/.test(text)) throw new Error(`INVALID_${field.toUpperCase()}`);
  const parsed = BigInt(text);
  if (options.nonNegative && parsed < 0n) throw new Error(`INVALID_${field.toUpperCase()}`);
  if (options.positive && parsed <= 0n) throw new Error(`INVALID_${field.toUpperCase()}`);
  return parsed;
}

function legacyResidualAttos(value, field) {
  if (value == null || String(value).trim() === "") return 0n;
  const residual = integerValue(value, field);
  const absolute = residual < 0n ? -residual : residual;
  if (absolute > HALF_MICRO_POINT_ATTOS) {
    throw new Error(`INVALID_${field.toUpperCase()}`);
  }
  return residual;
}

function publicRef(referenceKey, kind, value) {
  return crypto
    .createHmac("sha256", referenceKey)
    .update(`${REPORT_DOMAIN}\0${kind}\0${value}`, "utf8")
    .digest("hex")
    .slice(0, 24);
}

function addGrouped(map, key, value) {
  if (!map.has(key)) map.set(key, []);
  map.get(key).push(value);
}

function uniqueRecord(map, key, value, errorCode) {
  if (map.has(key)) throw new Error(errorCode);
  map.set(key, value);
}

function normalizeWebSnapshot(input) {
  const snapshot = asObject(input, "web_snapshot");
  const declaredUserIds = new Set();
  const userIds = new Set();
  const wallets = new Map();
  const links = new Map();
  const transactions = new Map();
  const outboxes = new Map();
  const linkedGamePlayerIds = new Set();
  const transactionIds = new Set();
  const transactionsById = new Map();

  for (const raw of asArray(snapshot.users, "web_users")) {
    const user = asObject(raw, "web_user");
    const userId = requiredId(user.userId ?? user.id, "web_user_id");
    declaredUserIds.add(userId);
    userIds.add(userId);
  }

  for (const raw of asArray(snapshot.wallets, "web_wallets")) {
    const wallet = asObject(raw, "web_wallet");
    const userId = requiredId(wallet.userId, "wallet_user_id");
    userIds.add(userId);
    uniqueRecord(wallets, userId, {
      pointMicros: integerValue(wallet.pointMicros, "wallet_point_micros"),
      pointLegacyResidualAttos: legacyResidualAttos(
        wallet.pointLegacyResidualAttos,
        "wallet_point_legacy_residual_attos"
      ),
      lockedPointMicros: integerValue(wallet.lockedPointMicros, "wallet_locked_point_micros"),
      lockedPointLegacyResidualAttos: legacyResidualAttos(
        wallet.lockedPointLegacyResidualAttos,
        "wallet_locked_point_legacy_residual_attos"
      ),
    }, "DUPLICATE_WEB_WALLET");
  }

  for (const raw of asArray(snapshot.links, "web_links")) {
    const link = asObject(raw, "web_link");
    const userId = requiredId(link.userId, "link_user_id");
    const gamePlayerId = requiredId(link.gamePlayerId, "link_game_player_id");
    if (linkedGamePlayerIds.has(gamePlayerId)) throw new Error("DUPLICATE_WEB_LINK_PLAYER");
    linkedGamePlayerIds.add(gamePlayerId);
    userIds.add(userId);
    uniqueRecord(links, userId, { gamePlayerId }, "DUPLICATE_WEB_LINK");
  }

  for (const raw of asArray(snapshot.transactions, "web_transactions")) {
    const tx = asObject(raw, "web_transaction");
    const transactionId = requiredId(tx.transactionId ?? tx.id, "web_transaction_id");
    if (transactionIds.has(transactionId)) throw new Error("DUPLICATE_WEB_TRANSACTION");
    transactionIds.add(transactionId);
    const userId = requiredId(tx.userId, "web_transaction_user_id");
    userIds.add(userId);
    const normalized = {
      transactionId,
      userId,
      type: optionalText(tx.type, "web_transaction_type", 128).toUpperCase() || "UNKNOWN",
      currency: optionalText(tx.currency, "web_transaction_currency", 32).toUpperCase() || "UNKNOWN",
      status: optionalText(tx.status, "web_transaction_status", 64).toUpperCase() || "UNKNOWN",
      amountMicros: integerValue(tx.amountMicros, "web_transaction_amount_micros"),
      amountLegacyResidualAttos: legacyResidualAttos(
        tx.amountLegacyResidualAttos,
        "web_transaction_amount_legacy_residual_attos"
      ),
    };
    transactionsById.set(transactionId, normalized);
    addGrouped(transactions, userId, normalized);
  }

  const outboxSourceIds = new Set();
  const outboxesBySource = new Map();
  for (const raw of asArray(snapshot.outboxes, "web_outboxes")) {
    const row = asObject(raw, "web_outbox");
    const userId = requiredId(row.userId, "web_outbox_user_id");
    const sourceTransactionId = requiredId(row.sourceTransactionId, "outbox_source_transaction_id");
    if (outboxSourceIds.has(sourceTransactionId)) throw new Error("DUPLICATE_WEB_OUTBOX_SOURCE");
    outboxSourceIds.add(sourceTransactionId);
    userIds.add(userId);
    const normalized = {
      userId,
      sourceTransactionId,
      pointMicros: integerValue(row.pointMicros, "outbox_point_micros", { positive: true }),
      status: optionalText(row.status, "outbox_status", 32).toUpperCase() || "UNKNOWN",
      attempts: (() => {
        const attempts = integerValue(row.attempts ?? "0", "outbox_attempts", { nonNegative: true });
        if (attempts > BigInt(Number.MAX_SAFE_INTEGER)) throw new Error("INVALID_OUTBOX_ATTEMPTS");
        return Number(attempts);
      })(),
    };
    outboxesBySource.set(sourceTransactionId, normalized);
    addGrouped(outboxes, userId, normalized);
  }

  return {
    declaredUserIds,
    userIds,
    wallets,
    links,
    transactions,
    transactionsById,
    outboxes,
    outboxesBySource,
  };
}

function normalizeGameSnapshot(input) {
  const snapshot = asObject(input, "game_snapshot");
  const schemaVersion = Number(snapshot.schemaVersion == null ? 1 : snapshot.schemaVersion);
  if (!Number.isInteger(schemaVersion) || schemaVersion < 1 || schemaVersion > 2) {
    throw new Error("UNSUPPORTED_GAME_SNAPSHOT_SCHEMA_VERSION");
  }
  const players = new Map();
  const playersByWebUserId = new Map();
  const transactions = new Map();
  const transactionIds = new Set();
  const webCreditsByRef = new Map();

  for (const raw of asArray(snapshot.players, "game_players")) {
    const player = asObject(raw, "game_player");
    const playerId = requiredId(player.playerId ?? player.id, "game_player_id");
    const webUserId = requiredId(player.webUserId, "game_web_user_id");
    const point = integerValue(player.point, "game_point", { nonNegative: true });
    const remainder = integerValue(
      player.pointMicrosRemainder ?? "0",
      "game_point_micros_remainder",
      { nonNegative: true }
    );
    if (remainder >= POINT_MICROS) throw new Error("INVALID_GAME_POINT_MICROS_REMAINDER");
    const normalized = {
      playerId,
      webUserId,
      point,
      pointMicrosRemainder: remainder,
      pointMicros: point * POINT_MICROS + remainder,
    };
    uniqueRecord(players, playerId, normalized, "DUPLICATE_GAME_PLAYER");
    addGrouped(playersByWebUserId, webUserId, normalized);
  }

  for (const raw of asArray(snapshot.transactions, "game_transactions")) {
    const tx = asObject(raw, "game_transaction");
    const transactionId = requiredId(tx.transactionId ?? tx.id, "game_transaction_id");
    if (transactionIds.has(transactionId)) throw new Error("DUPLICATE_GAME_TRANSACTION");
    transactionIds.add(transactionId);
    const playerId = requiredId(tx.playerId, "game_transaction_player_id");
    if (!players.has(playerId)) throw new Error("GAME_TRANSACTION_PLAYER_NOT_IN_SNAPSHOT");
    const pointAmountText = String(tx.pointAmountMicros == null ? "" : tx.pointAmountMicros).trim();
    const remainderBeforeText = String(
      tx.pointMicrosRemainderBefore == null ? "" : tx.pointMicrosRemainderBefore
    ).trim();
    const remainderAfterText = String(
      tx.pointMicrosRemainderAfter == null ? "" : tx.pointMicrosRemainderAfter
    ).trim();
    const remainderBefore = remainderBeforeText
      ? integerValue(remainderBeforeText, "game_transaction_remainder_before", { nonNegative: true })
      : null;
    const remainderAfter = remainderAfterText
      ? integerValue(remainderAfterText, "game_transaction_remainder_after", { nonNegative: true })
      : null;
    if (remainderBefore != null && remainderBefore >= POINT_MICROS) {
      throw new Error("INVALID_GAME_TRANSACTION_REMAINDER_BEFORE");
    }
    if (remainderAfter != null && remainderAfter >= POINT_MICROS) {
      throw new Error("INVALID_GAME_TRANSACTION_REMAINDER_AFTER");
    }
    const normalized = {
      transactionId,
      playerId,
      type: optionalText(tx.type, "game_transaction_type", 128).toLowerCase() || "unknown",
      ref: optionalText(tx.ref, "game_transaction_ref", 256),
      deltaPoint: integerValue(tx.deltaPoint ?? "0", "game_transaction_delta_point"),
      pointAmountMicros: pointAmountText
        ? integerValue(pointAmountText, "game_transaction_point_amount_micros", { positive: true })
        : null,
      pointMicrosRemainderBefore: remainderBefore,
      pointMicrosRemainderAfter: remainderAfter,
      remediation: null,
    };
    const isApply = normalized.type === "point_remediation_reversal";
    const isRollback = normalized.type === "point_remediation_reversal_rollback";
    if (tx.remediation != null) {
      if (!isApply && !isRollback) throw new Error("UNEXPECTED_GAME_REMEDIATION_METADATA");
      const remediation = asObject(tx.remediation, "game_transaction_remediation");
      const expectedAction = isApply ? "APPLY" : "ROLLBACK";
      const operationPattern = isApply ? APPLY_OPERATION_PATTERN : ROLLBACK_OPERATION_PATTERN;
      if (!operationPattern.test(transactionId)) {
        throw new Error("INVALID_GAME_REMEDIATION_OPERATION_ID");
      }
      const originalOperationId = requiredPattern(
        remediation.originalOperationId,
        "game_remediation_original_operation_id",
        APPLY_OPERATION_PATTERN
      );
      if (normalized.ref !== originalOperationId || (isApply && transactionId !== originalOperationId)) {
        throw new Error("INVALID_GAME_REMEDIATION_ORIGINAL_OPERATION");
      }
      if (String(remediation.action || "") !== expectedAction) {
        throw new Error("INVALID_GAME_REMEDIATION_ACTION");
      }
      const sourceRefs = asArray(remediation.sourceRefs, "game_remediation_source_refs")
        .map((value) => requiredPattern(value, "game_remediation_source_ref", PUBLIC_REF_PATTERN))
        .sort();
      if (sourceRefs.length === 0) throw new Error("EMPTY_GAME_REMEDIATION_SOURCE_REFS");
      if (new Set(sourceRefs).size !== sourceRefs.length) {
        throw new Error("DUPLICATE_GAME_REMEDIATION_SOURCE_REF");
      }
      const planSha256 = requiredPattern(
        remediation.planSha256,
        "game_remediation_plan_sha256",
        SHA256_PATTERN
      );
      const approvalSha256 = requiredPattern(
        remediation.approvalSha256,
        "game_remediation_approval_sha256",
        SHA256_PATTERN
      );
      requiredPattern(remediation.requestSignature, "game_remediation_request_signature", SHA256_PATTERN);
      if (normalized.pointAmountMicros == null
          || normalized.pointMicrosRemainderBefore == null
          || normalized.pointMicrosRemainderAfter == null) {
        throw new Error("GAME_REMEDIATION_EXACT_EVIDENCE_MISSING");
      }
      const actualDeltaMicros = normalized.deltaPoint * POINT_MICROS
        + normalized.pointMicrosRemainderAfter
        - normalized.pointMicrosRemainderBefore;
      const expectedDeltaMicros = normalized.pointAmountMicros * (isApply ? -1n : 1n);
      if (actualDeltaMicros !== expectedDeltaMicros) {
        throw new Error("GAME_REMEDIATION_BALANCE_ARITHMETIC_MISMATCH");
      }
      normalized.remediation = {
        action: expectedAction,
        originalOperationId,
        planSha256,
        approvalSha256,
        requestSignature: String(remediation.requestSignature),
        sourceRefs,
      };
    } else if (schemaVersion >= 2 && (isApply || isRollback)) {
      throw new Error("GAME_REMEDIATION_EVIDENCE_MISSING");
    }
    addGrouped(transactions, playerId, normalized);
    if (normalized.type === "web_topup_credit" && normalized.ref) {
      addGrouped(webCreditsByRef, normalized.ref, normalized);
    }
  }

  return { schemaVersion, players, playersByWebUserId, transactions, webCreditsByRef };
}

function summarizeWebTransactions(rows) {
  const groups = new Map();
  for (const row of rows) {
    const key = JSON.stringify([row.type, row.currency, row.status]);
    if (!groups.has(key)) {
      groups.set(key, {
        type: row.type,
        currency: row.currency,
        status: row.status,
        count: 0,
        amountMicros: 0n,
      });
    }
    const group = groups.get(key);
    group.count += 1;
    group.amountMicros += row.amountMicros;
  }
  return [...groups.values()]
    .sort((a, b) => JSON.stringify([a.type, a.currency, a.status])
      .localeCompare(JSON.stringify([b.type, b.currency, b.status])))
    .map((group) => ({ ...group, amountMicros: group.amountMicros.toString() }));
}

function gameTransactionDeltaMicros(row) {
  if (row.type === "web_topup_credit" && row.pointAmountMicros != null) {
    return row.pointAmountMicros;
  }
  if (row.pointMicrosRemainderBefore != null && row.pointMicrosRemainderAfter != null) {
    return row.deltaPoint * POINT_MICROS
      + row.pointMicrosRemainderAfter
      - row.pointMicrosRemainderBefore;
  }
  return row.deltaPoint * POINT_MICROS;
}

function sameStringArray(left, right) {
  return left.length === right.length && left.every((value, index) => value === right[index]);
}

function buildSyntheticRemediationIndex(options) {
  const {
    gameTransactions,
    outboxes,
    transactionsById,
    gameCreditByRef,
    player,
    referenceKey,
    blockingIssues,
  } = options;
  const outboxesByPublicRef = new Map();
  for (const outbox of outboxes) {
    outboxesByPublicRef.set(publicRef(referenceKey, "source", outbox.sourceTransactionId), outbox);
  }

  const groups = new Map();
  for (const transaction of gameTransactions) {
    const apply = transaction.type === "point_remediation_reversal";
    const rollback = transaction.type === "point_remediation_reversal_rollback";
    if (!apply && !rollback) continue;
    if (!transaction.remediation) {
      blockingIssues.push("GAME_REMEDIATION_EVIDENCE_MISSING");
      continue;
    }
    const operationId = transaction.remediation.originalOperationId;
    if (!groups.has(operationId)) groups.set(operationId, { apply: [], rollback: [] });
    groups.get(operationId)[apply ? "apply" : "rollback"].push(transaction);
  }

  const sourceOwners = new Map();
  for (const [operationId, group] of groups.entries()) {
    for (const transaction of [...group.apply, ...group.rollback]) {
      for (const sourceRef of transaction.remediation.sourceRefs) {
        if (!sourceOwners.has(sourceRef)) sourceOwners.set(sourceRef, new Set());
        sourceOwners.get(sourceRef).add(operationId);
      }
    }
  }
  const reusedSources = new Set([...sourceOwners.entries()]
    .filter(([, owners]) => owners.size > 1)
    .map(([sourceRef]) => sourceRef));
  if (reusedSources.size > 0) blockingIssues.push("SYNTHETIC_REMEDIATION_SOURCE_REUSED");

  const bySourceRef = new Map();
  for (const [operationId, group] of groups.entries()) {
    const issues = [];
    if (group.apply.length !== 1) {
      issues.push(group.apply.length === 0
        ? "SYNTHETIC_REMEDIATION_APPLY_MISSING"
        : "DUPLICATE_SYNTHETIC_REMEDIATION_APPLY");
    }
    if (group.rollback.length > 1) issues.push("DUPLICATE_SYNTHETIC_REMEDIATION_ROLLBACK");
    const apply = group.apply.length === 1 ? group.apply[0] : null;
    const rollback = group.rollback.length === 1 ? group.rollback[0] : null;
    const evidence = apply || rollback;
    if (!evidence) continue;

    if (rollback && apply) {
      if (!sameStringArray(rollback.remediation.sourceRefs, apply.remediation.sourceRefs)
          || rollback.pointAmountMicros !== apply.pointAmountMicros
          || rollback.remediation.planSha256 !== apply.remediation.planSha256
          || rollback.remediation.approvalSha256 !== apply.remediation.approvalSha256) {
        issues.push("SYNTHETIC_REMEDIATION_ROLLBACK_EVIDENCE_MISMATCH");
      }
    }
    const sourceRefs = evidence.remediation.sourceRefs;
    if (sourceRefs.some((sourceRef) => reusedSources.has(sourceRef))) {
      issues.push("SYNTHETIC_REMEDIATION_SOURCE_REUSED");
    }
    let sourceMicros = 0n;
    for (const sourceRef of sourceRefs) {
      const outbox = outboxesByPublicRef.get(sourceRef);
      if (!outbox) {
        issues.push("SYNTHETIC_REMEDIATION_SOURCE_UNKNOWN");
        continue;
      }
      sourceMicros += outbox.pointMicros;
      const sourceTransaction = transactionsById.get(outbox.sourceTransactionId) || null;
      if (sourceTransaction) issues.push("SYNTHETIC_REMEDIATION_SOURCE_NOW_FUNDED");
      if (outbox.status !== "SENT") issues.push("SYNTHETIC_REMEDIATION_OUTBOX_NOT_SENT");
      const credits = gameCreditByRef.get(outbox.sourceTransactionId) || [];
      if (credits.length !== 1
          || credits[0].playerId !== player.playerId
          || credits[0].pointAmountMicros !== outbox.pointMicros) {
        issues.push("SYNTHETIC_REMEDIATION_GAME_CREDIT_MISMATCH");
      }
    }
    if (sourceMicros !== evidence.pointAmountMicros) {
      issues.push("SYNTHETIC_REMEDIATION_AMOUNT_MISMATCH");
    }
    const uniqueIssues = [...new Set(issues)].sort();
    blockingIssues.push(...uniqueIssues);
    if (uniqueIssues.length > 0 || !apply) continue;

    const status = rollback ? "ROLLED_BACK" : "REVERSED";
    const operationRef = publicRef(referenceKey, "game-transaction", apply.transactionId);
    const rollbackRef = rollback
      ? publicRef(referenceKey, "game-transaction", rollback.transactionId)
      : null;
    for (const sourceRef of sourceRefs) {
      bySourceRef.set(sourceRef, { status, operationRef, rollbackRef, operationId });
    }
  }
  return bySourceRef;
}

function buildPointWalletMigrationReport(webInput, gameInput, options = {}) {
  const referenceKey = String(options.referenceKey || "");
  if (Buffer.byteLength(referenceKey, "utf8") < 32) throw new Error("REPORT_REFERENCE_KEY_TOO_SHORT");
  const generatedAt = new Date(options.generatedAt || Date.now());
  if (!Number.isFinite(generatedAt.getTime())) throw new Error("INVALID_REPORT_GENERATED_AT");

  const web = normalizeWebSnapshot(webInput);
  const game = normalizeGameSnapshot(gameInput);
  const allWebUserIds = new Set(web.userIds);
  for (const webUserId of game.playersByWebUserId.keys()) allWebUserIds.add(webUserId);

  const accounts = [];
  const statusCounts = {};
  let totalWebPointMicros = 0n;
  let totalWebLockedPointMicros = 0n;
  let totalMappedGamePointMicros = 0n;
  let legacySubMicroAccountCount = 0;
  let legacySubMicroValueCount = 0;
  let totalLegacyResidualPointAttos = 0n;
  let maxAbsLegacyResidualPointAttos = 0n;
  let remediatedSyntheticOutboxCount = 0;
  let totalRemediatedSyntheticPointMicros = 0n;

  for (const webUserId of [...allWebUserIds].sort()) {
    const wallet = web.wallets.get(webUserId) || {
      pointMicros: 0n,
      pointLegacyResidualAttos: 0n,
      lockedPointMicros: 0n,
      lockedPointLegacyResidualAttos: 0n,
    };
    const hasWallet = web.wallets.has(webUserId);
    const link = web.links.get(webUserId) || null;
    const players = game.playersByWebUserId.get(webUserId) || [];
    const webTransactions = web.transactions.get(webUserId) || [];
    const outboxes = web.outboxes.get(webUserId) || [];
    const blockingIssues = [];
    const reviewReasons = [];
    const gamePlayerRefs = players.map((player) => publicRef(referenceKey, "player", player.playerId));

    totalWebPointMicros += wallet.pointMicros;
    totalWebLockedPointMicros += wallet.lockedPointMicros;

    const hasWebRecord = hasWallet || Boolean(link) || webTransactions.length > 0 || outboxes.length > 0;
    if (!web.declaredUserIds.has(webUserId) && hasWebRecord) {
      blockingIssues.push("WEB_RECORD_WITHOUT_WEB_USER");
    }
    if (!web.declaredUserIds.has(webUserId) && players.length > 0) {
      blockingIssues.push("GAME_MAPPING_WITHOUT_WEB_USER");
    }
    if (players.length > 0 && !hasWallet) blockingIssues.push("GAME_MAPPING_WITHOUT_WEB_WALLET");
    if (players.length > 1) blockingIssues.push("DUPLICATE_GAME_MAPPING");
    if (link && players.length !== 1) blockingIssues.push("WEB_LINK_WITHOUT_UNIQUE_GAME_MAPPING");
    if (link && players.length === 1 && link.gamePlayerId !== players[0].playerId) {
      blockingIssues.push("WEB_LINK_PLAYER_MISMATCH");
    }
    if (wallet.pointMicros < 0n) blockingIssues.push("NEGATIVE_WEB_POINT_BALANCE");
    if (wallet.lockedPointMicros < 0n) blockingIssues.push("NEGATIVE_WEB_LOCKED_POINT_BALANCE");
    if (wallet.lockedPointMicros !== 0n) blockingIssues.push("LOCKED_WEB_POINT_PRESENT");

    const legacyResiduals = [
      wallet.pointLegacyResidualAttos,
      wallet.lockedPointLegacyResidualAttos,
      ...webTransactions.map((row) => row.amountLegacyResidualAttos),
    ].filter((value) => value !== 0n);
    const accountLegacyResidualAttos = legacyResiduals.reduce((sum, value) => sum + value, 0n);
    const accountMaxAbsLegacyResidualAttos = legacyResiduals.reduce((maximum, value) => {
      const absolute = value < 0n ? -value : value;
      return absolute > maximum ? absolute : maximum;
    }, 0n);
    if (legacyResiduals.length > 0) {
      blockingIssues.push("LEGACY_SUB_MICRO_VALUE_PRESENT");
      legacySubMicroAccountCount += 1;
      legacySubMicroValueCount += legacyResiduals.length;
      totalLegacyResidualPointAttos += accountLegacyResidualAttos;
      if (accountMaxAbsLegacyResidualAttos > maxAbsLegacyResidualPointAttos) {
        maxAbsLegacyResidualPointAttos = accountMaxAbsLegacyResidualAttos;
      }
    }

    const player = players.length === 1 ? players[0] : null;
    const gameTransactions = player ? (game.transactions.get(player.playerId) || []) : [];
    const gameCreditByRef = new Map();
    let gameLedgerDeltaMicros = 0n;
    let previousCreditRemainderAfter = null;
    for (const tx of gameTransactions) {
      gameLedgerDeltaMicros += gameTransactionDeltaMicros(tx);
      if (tx.type === "web_topup_credit") {
        if (tx.pointAmountMicros == null
            || tx.pointMicrosRemainderBefore == null
            || tx.pointMicrosRemainderAfter == null) {
          blockingIssues.push("GAME_CREDIT_EXACT_EVIDENCE_MISSING");
        } else {
          const total = tx.pointMicrosRemainderBefore + tx.pointAmountMicros;
          if (tx.deltaPoint !== total / POINT_MICROS
              || tx.pointMicrosRemainderAfter !== total % POINT_MICROS) {
            blockingIssues.push("GAME_CREDIT_REMAINDER_ARITHMETIC_MISMATCH");
          }
          if (previousCreditRemainderAfter != null
              && tx.pointMicrosRemainderBefore !== previousCreditRemainderAfter) {
            blockingIssues.push("GAME_CREDIT_REMAINDER_CHAIN_MISMATCH");
          }
          previousCreditRemainderAfter = tx.pointMicrosRemainderAfter;
        }
        if (tx.ref) addGrouped(gameCreditByRef, tx.ref, tx);
      }
    }
    if (player && previousCreditRemainderAfter != null
        && player.pointMicrosRemainder !== previousCreditRemainderAfter) {
      blockingIssues.push("GAME_CURRENT_REMAINDER_MISMATCH");
    }

    const syntheticRemediationBySourceRef = player
      ? buildSyntheticRemediationIndex({
        gameTransactions,
        outboxes,
        transactionsById: web.transactionsById,
        gameCreditByRef,
        player,
        referenceKey,
        blockingIssues,
      })
      : new Map();

    const outboxSourceIds = new Set(outboxes.map((row) => row.sourceTransactionId));
    const webTransactionById = new Map(webTransactions.map((row) => [row.transactionId, row]));
    let sentOutboxMicros = 0n;
    let matchedGameCreditMicros = 0n;
    let remediatedSyntheticPointMicros = 0n;
    const outboxEvidence = [];
    for (const outbox of outboxes) {
      const sourceRef = publicRef(referenceKey, "source", outbox.sourceTransactionId);
      const remediation = syntheticRemediationBySourceRef.get(sourceRef) || null;
      const remediationStatus = remediation ? remediation.status : "NONE";
      const credits = gameCreditByRef.get(outbox.sourceTransactionId) || [];
      const globalSourceTransaction = web.transactionsById.get(outbox.sourceTransactionId) || null;
      const sourceTransaction = webTransactionById.get(outbox.sourceTransactionId) || null;
      const globalGameCredits = game.webCreditsByRef.get(outbox.sourceTransactionId) || [];
      if (globalSourceTransaction && globalSourceTransaction.userId !== webUserId) {
        blockingIssues.push("OUTBOX_WEB_SOURCE_USER_MISMATCH");
      }
      if (globalGameCredits.some((credit) => !player || credit.playerId !== player.playerId)) {
        blockingIssues.push("GAME_CREDIT_PLAYER_MISMATCH");
      }
      if (!sourceTransaction) {
        if (remediationStatus !== "REVERSED") {
          reviewReasons.push("OUTBOX_WITHOUT_WEB_SOURCE_TRANSACTION");
        } else {
          remediatedSyntheticOutboxCount += 1;
          remediatedSyntheticPointMicros += outbox.pointMicros;
          totalRemediatedSyntheticPointMicros += outbox.pointMicros;
        }
      } else {
        if (sourceTransaction.amountMicros !== outbox.pointMicros) {
          blockingIssues.push("OUTBOX_WEB_SOURCE_AMOUNT_MISMATCH");
        }
        if (sourceTransaction.status !== "SUCCESS") {
          blockingIssues.push("OUTBOX_WEB_SOURCE_NOT_SUCCESSFUL");
        }
        if (sourceTransaction.type !== "SWAP") {
          reviewReasons.push("OUTBOX_WEB_SOURCE_TYPE_UNEXPECTED");
        }
      }
      if (outbox.status !== "SENT") {
        blockingIssues.push("UNSETTLED_WEB_OUTBOX");
      } else {
        sentOutboxMicros += outbox.pointMicros;
        if (credits.length === 0) blockingIssues.push("SENT_OUTBOX_MISSING_GAME_CREDIT");
        if (credits.length > 1) blockingIssues.push("DUPLICATE_GAME_CREDIT_FOR_SOURCE");
        if (credits.length === 1) {
          if (credits[0].pointAmountMicros == null) {
            blockingIssues.push("GAME_CREDIT_AMOUNT_UNKNOWN");
          } else if (credits[0].pointAmountMicros !== outbox.pointMicros) {
            blockingIssues.push("OUTBOX_GAME_CREDIT_AMOUNT_MISMATCH");
          } else {
            matchedGameCreditMicros += credits[0].pointAmountMicros;
          }
        }
      }
      outboxEvidence.push({
        sourceRef,
        status: outbox.status,
        attempts: outbox.attempts,
        pointMicros: outbox.pointMicros.toString(),
        webSourceTransactionMatched: Boolean(sourceTransaction),
        gameCreditCount: credits.length,
        syntheticRemediationStatus: remediationStatus,
        syntheticRemediationOperationRef: remediation ? remediation.operationRef : null,
        syntheticRemediationRollbackRef: remediation ? remediation.rollbackRef : null,
      });
    }

    for (const [sourceTransactionId, credits] of gameCreditByRef.entries()) {
      const globalOutbox = web.outboxesBySource.get(sourceTransactionId) || null;
      const globalCredits = game.webCreditsByRef.get(sourceTransactionId) || [];
      if (globalOutbox && globalOutbox.userId !== webUserId) {
        blockingIssues.push("GAME_CREDIT_WEB_USER_MISMATCH");
      } else if (!outboxSourceIds.has(sourceTransactionId)) {
        reviewReasons.push("GAME_CREDIT_WITHOUT_WEB_OUTBOX");
      }
      if (credits.length > 1 || globalCredits.length > 1) {
        blockingIssues.push("DUPLICATE_GAME_CREDIT_FOR_SOURCE");
      }
    }

    let gamePointMicros = null;
    let gameOpeningPointMicros = null;
    if (player) {
      gamePointMicros = player.pointMicros;
      gameOpeningPointMicros = player.pointMicros - gameLedgerDeltaMicros;
      totalMappedGamePointMicros += player.pointMicros;
      if (gameOpeningPointMicros < 0n) blockingIssues.push("NEGATIVE_GAME_OPENING_BALANCE");
      if (gameOpeningPointMicros !== 0n) reviewReasons.push("GAME_OPENING_BALANCE_REQUIRES_CLASSIFICATION");
    }
    if (wallet.pointMicros !== 0n) reviewReasons.push("NONZERO_WEB_POINT_REQUIRES_CLASSIFICATION");
    if (webTransactions.length > 0) reviewReasons.push("WEB_POINT_TRANSACTION_HISTORY_REQUIRES_REVIEW");

    const uniqueBlockingIssues = [...new Set(blockingIssues)].sort();
    const uniqueReviewReasons = [...new Set(reviewReasons)].sort();
    let status;
    if (uniqueBlockingIssues.length > 0) {
      status = "BLOCKED";
    } else if (wallet.pointMicros !== 0n || wallet.lockedPointMicros !== 0n || uniqueReviewReasons.length > 0) {
      status = players.length === 0 ? "UNMAPPED_LEGACY_REVIEW" : "MANUAL_RECONCILIATION_REQUIRED";
    } else if (players.length === 1) {
      status = link ? "ALREADY_LINKED" : "READY_TO_LINK";
    } else {
      status = "NO_ACTION";
    }

    statusCounts[status] = (statusCounts[status] || 0) + 1;
    accounts.push({
      accountRef: publicRef(referenceKey, "web-user", webUserId),
      gamePlayerRefs,
      linkedInWebAuthority: Boolean(link),
      status,
      blockingIssues: uniqueBlockingIssues,
      reviewReasons: uniqueReviewReasons,
      balances: {
        webPointMicros: wallet.pointMicros.toString(),
        webLockedPointMicros: wallet.lockedPointMicros.toString(),
        gamePointMicros: gamePointMicros == null ? null : gamePointMicros.toString(),
        gameOpeningPointMicros: gameOpeningPointMicros == null ? null : gameOpeningPointMicros.toString(),
      },
      evidence: {
        webPointTransactions: summarizeWebTransactions(webTransactions),
        outboxes: outboxEvidence.sort((a, b) => a.sourceRef.localeCompare(b.sourceRef)),
        sentOutboxMicros: sentOutboxMicros.toString(),
        matchedGameCreditMicros: matchedGameCreditMicros.toString(),
        remediatedSyntheticPointMicros: remediatedSyntheticPointMicros.toString(),
        gameTransactionCount: gameTransactions.length,
        gameLedgerDeltaMicros: gameLedgerDeltaMicros.toString(),
        legacySubMicroNormalization: {
          roundingMode: "ROUND_HALF_EVEN",
          valueCount: legacyResiduals.length,
          totalResidualPointAttos: accountLegacyResidualAttos.toString(),
          maxAbsResidualPointAttos: accountMaxAbsLegacyResidualAttos.toString(),
        },
      },
      suggestedMigrationMicros: null,
    });
  }

  accounts.sort((a, b) => a.accountRef.localeCompare(b.accountRef));
  return {
    schemaVersion: 2,
    generatedAt: generatedAt.toISOString(),
    referenceKeyId: crypto.createHash("sha256").update(referenceKey).digest("hex").slice(0, 16),
    mode: "READ_ONLY_DRY_RUN",
    automaticMigrationAllowed: false,
    migrationStatementsGenerated: 0,
    databaseMutationsPerformed: false,
    summary: {
      accountCount: accounts.length,
      statusCounts,
      totalWebPointMicros: totalWebPointMicros.toString(),
      totalWebLockedPointMicros: totalWebLockedPointMicros.toString(),
      totalMappedGamePointMicros: totalMappedGamePointMicros.toString(),
      legacySubMicroAccountCount,
      legacySubMicroValueCount,
      totalLegacyResidualPointAttos: totalLegacyResidualPointAttos.toString(),
      maxAbsLegacyResidualPointAttos: maxAbsLegacyResidualPointAttos.toString(),
      remediatedSyntheticOutboxCount,
      totalRemediatedSyntheticPointMicros: totalRemediatedSyntheticPointMicros.toString(),
    },
    accounts,
  };
}

function parseCliArgs(argv) {
  const args = {};
  for (let index = 0; index < argv.length; index += 1) {
    const key = argv[index];
    if (!["--web-snapshot", "--game-snapshot", "--generated-at"].includes(key)) {
      throw new Error("INVALID_ARGUMENT");
    }
    const value = argv[index + 1];
    if (!value) throw new Error("MISSING_ARGUMENT_VALUE");
    args[key.slice(2)] = value;
    index += 1;
  }
  if (!args["web-snapshot"] || !args["game-snapshot"]) throw new Error("MISSING_SNAPSHOT_PATH");
  return args;
}

if (require.main === module) {
  try {
    const args = parseCliArgs(process.argv.slice(2));
    const webSnapshot = JSON.parse(fs.readFileSync(args["web-snapshot"], "utf8"));
    const gameSnapshot = JSON.parse(fs.readFileSync(args["game-snapshot"], "utf8"));
    const report = buildPointWalletMigrationReport(webSnapshot, gameSnapshot, {
      referenceKey: process.env.POINT_MIGRATION_REPORT_KEY,
      generatedAt: args["generated-at"],
    });
    process.stdout.write(`${JSON.stringify(report, null, 2)}\n`);
  } catch (error) {
    process.stderr.write(`[point-wallet-migration-report] ${error && error.message || "FAILED"}\n`);
    process.exitCode = 1;
  }
}

module.exports = {
  POINT_MICROS,
  buildPointWalletMigrationReport,
  normalizeGameSnapshot,
  normalizeWebSnapshot,
};
