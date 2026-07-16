"use strict";

const assert = require("assert");
const { buildPointWalletMigrationReport } = require("./pointWalletMigrationReport");

const REFERENCE_KEY = "migration-report-test-key-with-at-least-32-characters";

function accountByStatus(report, status) {
  return report.accounts.find((account) => account.status === status);
}

function run() {
  const web = {
    users: [
      { userId: "web-canary" },
      { userId: "web-commission" },
      { userId: "web-ready" },
      { userId: "web-legacy" },
      { userId: "web-duplicate" },
    ],
    wallets: [
      { userId: "web-canary", pointMicros: "0", lockedPointMicros: "0" },
      { userId: "web-commission", pointMicros: "12000000", lockedPointMicros: "0" },
      { userId: "web-ready", pointMicros: "0", lockedPointMicros: "0" },
      {
        userId: "web-legacy",
        pointMicros: "3410666667",
        pointLegacyResidualAttos: "-333333314400",
        lockedPointMicros: "0",
      },
      { userId: "web-duplicate", pointMicros: "0", lockedPointMicros: "0" },
    ],
    transactions: [
      {
        transactionId: "web-tx-commission",
        userId: "web-commission",
        type: "REFERRAL_COMMISSION",
        currency: "GXL",
        status: "COMPLETED",
        amountMicros: "12000000",
      },
      {
        transactionId: "web-tx-legacy-swap",
        userId: "web-legacy",
        type: "SWAP",
        currency: "GXL",
        status: "SUCCESS",
        amountMicros: "316666667",
        amountLegacyResidualAttos: "-333333300000",
      },
    ],
    outboxes: [
      {
        userId: "web-canary",
        sourceTransactionId: "source-canary-1",
        pointMicros: "3000000",
        status: "SENT",
        attempts: "7",
      },
    ],
    links: [],
  };
  const game = {
    players: [
      { playerId: "player-canary", webUserId: "web-canary", point: "5003", pointMicrosRemainder: "0" },
      { playerId: "player-commission", webUserId: "web-commission", point: "5000", pointMicrosRemainder: "0" },
      { playerId: "player-ready", webUserId: "web-ready", point: "0", pointMicrosRemainder: "0" },
      { playerId: "player-dup-a", webUserId: "web-duplicate", point: "0", pointMicrosRemainder: "0" },
      { playerId: "player-dup-b", webUserId: "web-duplicate", point: "0", pointMicrosRemainder: "0" },
      { playerId: "player-orphan", webUserId: "web-missing", point: "5000", pointMicrosRemainder: "0" },
    ],
    transactions: [
      {
        transactionId: "game-tx-canary-1",
        playerId: "player-canary",
        type: "web_topup_credit",
        ref: "source-canary-1",
        deltaPoint: "3",
        pointAmountMicros: "3000000",
        pointMicrosRemainderBefore: "0",
        pointMicrosRemainderAfter: "0",
      },
    ],
  };

  const report = buildPointWalletMigrationReport(web, game, {
    referenceKey: REFERENCE_KEY,
    generatedAt: "2026-07-16T00:00:00.000Z",
  });

  assert.strictEqual(report.mode, "READ_ONLY_DRY_RUN");
  assert.strictEqual(report.automaticMigrationAllowed, false);
  assert.strictEqual(report.migrationStatementsGenerated, 0);
  assert.strictEqual(report.databaseMutationsPerformed, false);
  assert.strictEqual(report.summary.accountCount, 6);
  assert.strictEqual(report.summary.totalWebPointMicros, "3422666667");
  assert.strictEqual(report.summary.statusCounts.READY_TO_LINK, 1);
  assert.strictEqual(report.summary.statusCounts.MANUAL_RECONCILIATION_REQUIRED, 2);
  assert.strictEqual(report.summary.statusCounts.UNMAPPED_LEGACY_REVIEW, undefined);
  assert.strictEqual(report.summary.statusCounts.BLOCKED, 3);
  assert.strictEqual(report.summary.legacySubMicroAccountCount, 1);
  assert.strictEqual(report.summary.legacySubMicroValueCount, 2);
  assert.strictEqual(report.summary.totalLegacyResidualPointAttos, "-666666614400");
  assert.strictEqual(report.summary.maxAbsLegacyResidualPointAttos, "333333314400");

  const ready = accountByStatus(report, "READY_TO_LINK");
  assert(ready && ready.reviewReasons.length === 0 && ready.blockingIssues.length === 0);

  const canary = report.accounts.find((account) =>
    account.evidence.sentOutboxMicros === "3000000");
  assert(canary, "Canary evidence missing");
  assert.strictEqual(canary.status, "MANUAL_RECONCILIATION_REQUIRED");
  assert.strictEqual(canary.balances.gameOpeningPointMicros, "5000000000");
  assert.strictEqual(canary.evidence.matchedGameCreditMicros, "3000000");
  assert(canary.reviewReasons.includes("GAME_OPENING_BALANCE_REQUIRES_CLASSIFICATION"));
  assert(canary.reviewReasons.includes("OUTBOX_WITHOUT_WEB_SOURCE_TRANSACTION"));

  const duplicateMapping = report.accounts.find((account) =>
    account.blockingIssues.includes("DUPLICATE_GAME_MAPPING"));
  assert(duplicateMapping, "Duplicate game mapping was not blocked");
  const orphanMapping = report.accounts.find((account) =>
    account.blockingIssues.includes("GAME_MAPPING_WITHOUT_WEB_USER"));
  assert(orphanMapping, "Game mapping without an authoritative web user was not blocked");
  assert(orphanMapping.blockingIssues.includes("GAME_MAPPING_WITHOUT_WEB_WALLET"));
  const legacySubMicro = report.accounts.find((account) =>
    account.blockingIssues.includes("LEGACY_SUB_MICRO_VALUE_PRESENT"));
  assert(legacySubMicro, "Legacy sub-micro evidence was not blocked");
  assert.strictEqual(legacySubMicro.status, "BLOCKED");
  assert.deepStrictEqual(legacySubMicro.evidence.legacySubMicroNormalization, {
    roundingMode: "ROUND_HALF_EVEN",
    valueCount: 2,
    totalResidualPointAttos: "-666666614400",
    maxAbsResidualPointAttos: "333333314400",
  });
  for (const account of report.accounts) {
    assert.strictEqual(account.suggestedMigrationMicros, null);
  }

  const serialized = JSON.stringify(report);
  for (const rawIdentity of [
    "web-canary",
    "web-commission",
    "web-ready",
    "web-legacy",
    "web-duplicate",
    "web-missing",
    "player-canary",
    "source-canary-1",
    "web-tx-commission",
    "web-tx-legacy-swap",
    "game-tx-canary-1",
  ]) {
    assert(!serialized.includes(rawIdentity), `Raw identity leaked: ${rawIdentity}`);
  }

  assert.throws(
    () => buildPointWalletMigrationReport(web, game, { referenceKey: "short" }),
    /REPORT_REFERENCE_KEY_TOO_SHORT/
  );
  assert.throws(
    () => buildPointWalletMigrationReport({ ...web, wallets: [...web.wallets, web.wallets[0]] }, game, {
      referenceKey: REFERENCE_KEY,
    }),
    /DUPLICATE_WEB_WALLET/
  );
  assert.throws(
    () => buildPointWalletMigrationReport({
      ...web,
      transactions: [...web.transactions, web.transactions[0]],
    }, game, { referenceKey: REFERENCE_KEY }),
    /DUPLICATE_WEB_TRANSACTION/
  );
  assert.throws(
    () => buildPointWalletMigrationReport(web, {
      ...game,
      transactions: [...game.transactions, game.transactions[0]],
    }, { referenceKey: REFERENCE_KEY }),
    /DUPLICATE_GAME_TRANSACTION/
  );
  assert.throws(
    () => buildPointWalletMigrationReport({
      ...web,
      links: [
        { userId: "web-ready", gamePlayerId: "player-ready" },
        { userId: "web-canary", gamePlayerId: "player-ready" },
      ],
    }, game, { referenceKey: REFERENCE_KEY }),
    /DUPLICATE_WEB_LINK_PLAYER/
  );
  const orphanWebRecord = buildPointWalletMigrationReport({
    ...web,
    wallets: [...web.wallets, {
      userId: "web-wallet-without-user",
      pointMicros: "1",
      lockedPointMicros: "0",
    }],
  }, game, { referenceKey: REFERENCE_KEY });
  assert(orphanWebRecord.accounts.some((account) =>
    account.blockingIssues.includes("WEB_RECORD_WITHOUT_WEB_USER")));

  const negativeWallet = buildPointWalletMigrationReport({
    ...web,
    wallets: web.wallets.map((wallet) => wallet.userId === "web-ready"
      ? { ...wallet, pointMicros: "-1" }
      : wallet),
  }, game, { referenceKey: REFERENCE_KEY });
  assert(negativeWallet.accounts.some((account) =>
    account.blockingIssues.includes("NEGATIVE_WEB_POINT_BALANCE")));

  const crossAccountWebSource = buildPointWalletMigrationReport({
    ...web,
    transactions: [...web.transactions, {
      transactionId: "source-canary-1",
      userId: "web-commission",
      type: "SWAP",
      currency: "GXL",
      status: "SUCCESS",
      amountMicros: "3000000",
    }],
  }, game, { referenceKey: REFERENCE_KEY });
  assert(crossAccountWebSource.accounts.some((account) =>
    account.blockingIssues.includes("OUTBOX_WEB_SOURCE_USER_MISMATCH")));

  const crossAccountGameCredit = buildPointWalletMigrationReport(web, {
    ...game,
    transactions: game.transactions.map((transaction) => ({
      ...transaction,
      playerId: "player-commission",
    })),
  }, { referenceKey: REFERENCE_KEY });
  assert(crossAccountGameCredit.accounts.some((account) =>
    account.blockingIssues.includes("GAME_CREDIT_PLAYER_MISMATCH")));
  assert(crossAccountGameCredit.accounts.some((account) =>
    account.blockingIssues.includes("GAME_CREDIT_WEB_USER_MISMATCH")));

  const duplicateCrossPlayerCredit = buildPointWalletMigrationReport(web, {
    ...game,
    transactions: [...game.transactions, {
      ...game.transactions[0],
      transactionId: "game-tx-canary-duplicate-player",
      playerId: "player-commission",
    }],
  }, { referenceKey: REFERENCE_KEY });
  assert(duplicateCrossPlayerCredit.accounts.some((account) =>
    account.blockingIssues.includes("DUPLICATE_GAME_CREDIT_FOR_SOURCE")));

  const badRemainderMath = buildPointWalletMigrationReport(web, {
    ...game,
    transactions: game.transactions.map((transaction) => ({
      ...transaction,
      pointMicrosRemainderAfter: "1",
    })),
  }, { referenceKey: REFERENCE_KEY });
  assert(badRemainderMath.accounts.some((account) =>
    account.blockingIssues.includes("GAME_CREDIT_REMAINDER_ARITHMETIC_MISMATCH")));
  assert.throws(
    () => buildPointWalletMigrationReport({
      ...web,
      outboxes: [{
        ...web.outboxes[0],
        attempts: (BigInt(Number.MAX_SAFE_INTEGER) + 1n).toString(),
      }],
    }, game, { referenceKey: REFERENCE_KEY }),
    /INVALID_OUTBOX_ATTEMPTS/
  );
  assert.throws(
    () => buildPointWalletMigrationReport({
      ...web,
      wallets: web.wallets.map((wallet) => wallet.userId === "web-ready"
        ? { ...wallet, pointLegacyResidualAttos: "500000000001" }
        : wallet),
    }, game, { referenceKey: REFERENCE_KEY }),
    /INVALID_WALLET_POINT_LEGACY_RESIDUAL_ATTOS/
  );

  console.log("[point-wallet-migration-report] PASS: report is anonymized, read-only, fail-closed, and never proposes automatic balance migration.");
}

run();
