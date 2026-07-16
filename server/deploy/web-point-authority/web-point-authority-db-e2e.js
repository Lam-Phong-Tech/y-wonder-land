const crypto = require("crypto");
const { PrismaClient } = require("@prisma/client");

function assert(condition, message) {
  if (!condition) throw new Error(message);
}

function errorText(error) {
  return String(error && (error.message || error.code) || error || "UNKNOWN_ERROR");
}

async function expectFailure(action, marker, message) {
  try {
    await action();
  } catch (error) {
    if (!marker || errorText(error).includes(marker)) return;
    throw new Error(`${message}: ${errorText(error)}`);
  }
  throw new Error(message);
}

async function main() {
  const db = new PrismaClient();
  const suffix = `${Date.now()}_${crypto.randomBytes(4).toString("hex")}`;
  const linkedUserId = `authority_linked_${suffix}`;
  const legacyUserId = `authority_legacy_${suffix}`;
  const linkedWalletId = `authority_wallet_linked_${suffix}`;
  const legacyWalletId = `authority_wallet_legacy_${suffix}`;
  const linkedGamePlayerId = `game_player_linked_${suffix}`;
  const txId = `gpc_${crypto.randomUUID().replace(/-/g, "")}`;
  const requestId = crypto.randomUUID();
  const outboxId = `authority_outbox_${suffix}`;
  const conversionId = `authority_conversion_${suffix}`;

  try {
    const activeRate = await db.pointExchangeRateVersion.findFirst({
      where: { pair: "USDT_POINT", isActive: true },
      orderBy: [{ effectiveAt: "desc" }, { createdAt: "desc" }],
    });
    assert(activeRate && /^[1-9]\d*$/.test(activeRate.rateMicros),
      "Migration did not seed an exact active USDT to Point rate");
    const pointMicros = ((60000n * BigInt(activeRate.rateMicros)) / 1000000n).toString();
    const roundingRemainder = ((60000n * BigInt(activeRate.rateMicros)) % 1000000n).toString();
    const pointAmount = `${BigInt(pointMicros) / 1000000n}.${String(BigInt(pointMicros) % 1000000n).padStart(6, "0")}`;

    await db.user.createMany({
      data: [
        {
          id: linkedUserId,
          email: `${linkedUserId}@example.test`,
          username: linkedUserId,
          fullName: "Point Authority Linked Test",
          refCode: `PAL${suffix}`,
        },
        {
          id: legacyUserId,
          email: `${legacyUserId}@example.test`,
          username: legacyUserId,
          fullName: "Point Authority Legacy Test",
          refCode: `PAU${suffix}`,
        },
      ],
    });
    await db.wallet.createMany({
      data: [
        { id: linkedWalletId, userId: linkedUserId, balanceGXL: 5, balanceUsdt: 10 },
        { id: legacyWalletId, userId: legacyUserId, balanceGXL: 7, balanceUsdt: 10 },
      ],
    });

    const guardedUser = await db.user.findUnique({ where: { id: linkedUserId } });
    const guardedWallet = await db.wallet.findUnique({ where: { userId: linkedUserId } });
    assert(guardedUser && guardedWallet && guardedWallet.balanceGXL === 5,
      "Non-zero legacy Point guard fixture is invalid");
    await expectFailure(
      () => db.gamePointLinkedAccount.create({
        data: {
          userId: linkedUserId,
          gamePlayerId: linkedGamePlayerId,
          linkedBy: "isolated-e2e",
        },
      }),
      "",
      "A non-zero legacy Point wallet was linked"
    );
    const rejectedLink = await db.gamePointLinkedAccount.findUnique({
      where: { userId: linkedUserId },
    });
    assert(!rejectedLink, "Rejected non-zero legacy wallet left a linked-account row");

    await db.wallet.update({ where: { userId: linkedUserId }, data: { balanceGXL: 0 } });
    await db.gamePointLinkedAccount.create({
      data: {
        userId: linkedUserId,
        gamePlayerId: linkedGamePlayerId,
        linkedBy: "isolated-e2e",
      },
    });
    await db.wallet.update({ where: { userId: legacyUserId }, data: { balanceGXL: 0 } });
    await expectFailure(
      () => db.gamePointLinkedAccount.create({
        data: {
          userId: legacyUserId,
          gamePlayerId: linkedGamePlayerId,
          linkedBy: "isolated-e2e",
        },
      }),
      "",
      "One game player was linked to two web accounts"
    );
    await db.wallet.update({ where: { userId: legacyUserId }, data: { balanceGXL: 7 } });
    await db.wallet.update({ where: { userId: linkedUserId }, data: { balanceUsdt: 9.99 } });
    await expectFailure(
      () => db.wallet.update({
        where: { userId: linkedUserId },
        data: { balanceGXL: { increment: 1 } },
      }),
      "",
      "A linked account accepted a legacy Point mutation"
    );
    const frozenWallet = await db.wallet.findUnique({ where: { userId: linkedUserId } });
    assert(frozenWallet && frozenWallet.balanceGXL === 0 && frozenWallet.lockedGXL === 0,
      "Rejected legacy Point mutation changed the linked wallet");

    await db.$transaction(async (tx) => {
      const debit = await tx.wallet.updateMany({
        where: { userId: linkedUserId, balanceUsdt: { gte: 0.06 } },
        data: { balanceUsdt: { decrement: 0.06 } },
      });
      assert(debit.count === 1, "Synthetic conversion did not debit exactly one wallet");
      await tx.transaction.create({
        data: {
          id: txId,
          userId: linkedUserId,
          type: "SWAP",
          amount: Number(pointMicros) / 1000000,
          currency: "GXL",
          status: "PENDING",
          metadata: JSON.stringify({
            from: "USDT",
            to: "POINT",
            requestId,
            usdtMicros: "60000",
            pointMicros,
            rateVersionId: activeRate.id,
            rateMicros: activeRate.rateMicros,
            roundingRemainder,
            authority: "game",
          }),
        },
      });
      await tx.gamePointSyncOutbox.create({
        data: {
          id: outboxId,
          sourceTransactionId: txId,
          userId: linkedUserId,
          pointAmount,
          occurredAt: new Date(),
          source: "ywonder-web-usdt-to-point",
        },
      });
      await tx.gamePointConversion.create({
        data: {
          id: conversionId,
          requestId,
          sourceTransactionId: txId,
          outboxId,
          userId: linkedUserId,
          usdtMicros: "60000",
          pointMicros,
          rateVersionId: activeRate.id,
          rateMicros: activeRate.rateMicros,
          roundingRemainder,
        },
      });
    });

    const firstWallet = await db.wallet.findUnique({ where: { userId: linkedUserId } });
    assert(firstWallet && Math.abs(firstWallet.balanceUsdt - 9.93) < 0.000000001,
      "Synthetic conversion debited the wrong USDT amount");
    assert(firstWallet && firstWallet.balanceGXL === 0,
      "Synthetic game conversion created a second spendable web Point balance");

    const replay = await db.gamePointConversion.findUnique({ where: { requestId } });
    assert(replay && replay.sourceTransactionId === txId,
      "Same request ID did not resolve to the original conversion");
    const replayWallet = await db.wallet.findUnique({ where: { userId: linkedUserId } });
    assert(replayWallet && Math.abs(replayWallet.balanceUsdt - firstWallet.balanceUsdt) < 0.000000001,
      "Idempotent lookup debited USDT a second time");

    await expectFailure(
      () => db.gamePointConversion.create({
        data: {
          id: `authority_conversion_pending_${suffix}`,
          requestId: crypto.randomUUID(),
          sourceTransactionId: `authority_tx_pending_${suffix}`,
          outboxId: `authority_outbox_pending_${suffix}`,
          userId: linkedUserId,
          usdtMicros: "60000",
          pointMicros,
          rateVersionId: activeRate.id,
          rateMicros: activeRate.rateMicros,
          roundingRemainder,
        },
      }),
      "",
      "A second unresolved conversion was accepted for one account"
    );

    const settledAt = new Date();
    await db.$transaction([
      db.gamePointSyncOutbox.update({
        where: { id: outboxId },
        data: { status: "SENT", attempts: 1, sentAt: settledAt },
      }),
      db.gamePointConversion.update({
        where: { id: conversionId },
        data: { status: "SENT", sentAt: settledAt },
      }),
      db.transaction.update({ where: { id: txId }, data: { status: "SUCCESS" } }),
    ]);
    await db.gamePointConversion.create({
      data: {
        id: `authority_conversion_after_sent_${suffix}`,
        requestId: crypto.randomUUID(),
        sourceTransactionId: `authority_tx_after_sent_${suffix}`,
        outboxId: `authority_outbox_after_sent_${suffix}`,
        userId: linkedUserId,
        usdtMicros: "60000",
        pointMicros,
        rateVersionId: activeRate.id,
        rateMicros: activeRate.rateMicros,
        roundingRemainder,
      },
    });

    await db.wallet.update({
      where: { userId: legacyUserId },
      data: { balanceGXL: { increment: 1 } },
    });
    const legacyWallet = await db.wallet.findUnique({ where: { userId: legacyUserId } });
    assert(legacyWallet && legacyWallet.balanceGXL === 8,
      "Legacy unlinked web Point behavior changed");

    console.log("WEB_POINT_AUTHORITY_DB_E2E=pass");
  } finally {
    await db.gamePointConversion.deleteMany({ where: { userId: { in: [linkedUserId, legacyUserId] } } });
    await db.gamePointSyncOutbox.deleteMany({ where: { userId: { in: [linkedUserId, legacyUserId] } } });
    await db.transaction.deleteMany({ where: { userId: { in: [linkedUserId, legacyUserId] } } });
    await db.gamePointLinkedAccount.deleteMany({ where: { userId: { in: [linkedUserId, legacyUserId] } } });
    await db.wallet.deleteMany({ where: { userId: { in: [linkedUserId, legacyUserId] } } });
    await db.user.deleteMany({ where: { id: { in: [linkedUserId, legacyUserId] } } });
    await db.$disconnect();
  }
}

main().catch((error) => {
  console.error(`WEB_POINT_AUTHORITY_DB_E2E=fail: ${errorText(error)}`);
  process.exitCode = 1;
});
