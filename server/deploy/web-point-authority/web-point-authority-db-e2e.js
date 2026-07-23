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

async function assertTriggerDefinition(db, name, fragments) {
  const rows = await db.$queryRawUnsafe(
    "select sql from sqlite_master where type = 'trigger' and name = ?",
    name
  );
  const sql = String(rows?.[0]?.sql || "");
  assert(sql, `Missing SQLite trigger ${name}`);
  for (const fragment of fragments) {
    assert(sql.includes(fragment), `SQLite trigger ${name} is missing ${fragment}`);
  }
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
  const debitId = `authority_debit_${suffix}`;
  const debitRequestId = crypto.randomUUID();
  const debitReservationId = `authority_reservation_${suffix}`;
  const debitTransactionId = `authority_debit_tx_${suffix}`;

  try {
    const activeRate = await db.pointExchangeRateVersion.findFirst({
      where: { pair: "USDT_POINT", isActive: true },
      orderBy: [{ effectiveAt: "desc" }, { createdAt: "desc" }],
    });
    assert(activeRate && /^[1-9]\d*$/.test(activeRate.rateMicros),
      "Migration did not seed an exact active USDT to Point rate");
    await assertTriggerDefinition(db, "GamePointConversion_block_active_debit_insert", [
      "GAME_POINT_WALLET_OPERATION_ALREADY_PENDING",
      "debit.\"status\" not in ('CAPTURED', 'RELEASED', 'REJECTED')",
    ]);
    await assertTriggerDefinition(db, "GamePointDebit_block_active_conversion_insert", [
      "GAME_POINT_WALLET_OPERATION_ALREADY_PENDING",
      "conversion.\"status\" not in ('SENT', 'REFUNDED')",
    ]);
    const pointMicros = ((60000n * BigInt(activeRate.rateMicros)) / 1000000n).toString();
    const roundingRemainder = ((60000n * BigInt(activeRate.rateMicros)) % 1000000n).toString();
    const pointAmount = `${BigInt(pointMicros) / 1000000n}.${String(BigInt(pointMicros) % 1000000n).padStart(6, "0")}`;
    const debitPointMicros = 100000000n;
    const debitGrossMicros = (debitPointMicros * 1000000n) / BigInt(activeRate.rateMicros);
    const debitFeeMicros = (debitGrossMicros * 1000n) / 10000n;
    const debitNetMicros = debitGrossMicros - debitFeeMicros;

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

    const controlConversionId = `authority_conversion_fk_control_${suffix}`;
    await db.gamePointConversion.create({
      data: {
        id: controlConversionId,
        requestId: crypto.randomUUID(),
        sourceTransactionId: `authority_tx_fk_control_${suffix}`,
        outboxId: `authority_outbox_fk_control_${suffix}`,
        userId: legacyUserId,
        usdtMicros: "60000",
        pointMicros,
        rateVersionId: activeRate.id,
        rateMicros: activeRate.rateMicros,
        roundingRemainder,
      },
    });
    await db.gamePointConversion.delete({ where: { id: controlConversionId } });

    const debitData = {
      id: debitId,
      requestId: debitRequestId,
      reservationId: debitReservationId,
      sourceTransactionId: debitTransactionId,
      userId: linkedUserId,
      gamePlayerId: linkedGamePlayerId,
      targetCurrency: "USDT",
      pointAmount: 100,
      pointMicros: debitPointMicros.toString(),
      grossTargetMicros: debitGrossMicros.toString(),
      feeBps: 1000,
      feeMicros: debitFeeMicros.toString(),
      netTargetMicros: debitNetMicros.toString(),
      rateVersionId: activeRate.id,
      rateMicros: activeRate.rateMicros,
      roundingRemainder: "0",
      feeRoundingRemainder: "0",
      requestFingerprint: crypto.createHash("sha256").update(debitRequestId).digest("hex"),
      purpose: "point_to_usdt",
      source: "ywonder-web",
      occurredAt: new Date(),
    };
    await db.gamePointDebit.create({ data: debitData });
    const storedDebit = await db.gamePointDebit.findUnique({ where: { requestId: debitRequestId } });
    assert(storedDebit && storedDebit.reservationId === debitReservationId
      && storedDebit.status === "RESERVE_PENDING",
    "Point debit journal did not preserve its reservation identity");
    await expectFailure(
      () => db.gamePointConversion.create({
        data: {
          id: `authority_conversion_blocked_by_debit_${suffix}`,
          requestId: crypto.randomUUID(),
          sourceTransactionId: `authority_tx_blocked_by_debit_${suffix}`,
          outboxId: `authority_outbox_blocked_by_debit_${suffix}`,
          userId: linkedUserId,
          usdtMicros: "60000",
          pointMicros,
          rateVersionId: activeRate.id,
          rateMicros: activeRate.rateMicros,
          roundingRemainder,
        },
      }),
      "",
      "USDT to Point started while Point debit was unresolved"
    );
    const blockedConversion = await db.gamePointConversion.findUnique({
      where: { id: `authority_conversion_blocked_by_debit_${suffix}` },
    });
    assert(!blockedConversion, "Blocked USDT to Point operation left a conversion row");
    await expectFailure(
      () => db.gamePointDebit.create({
        data: {
          ...debitData,
          id: `authority_debit_request_duplicate_${suffix}`,
          userId: legacyUserId,
          gamePlayerId: `legacy_player_${suffix}`,
          reservationId: `authority_reservation_request_duplicate_${suffix}`,
          sourceTransactionId: `authority_debit_tx_request_duplicate_${suffix}`,
        },
      }),
      "",
      "A duplicate Point debit request ID was accepted"
    );
    await expectFailure(
      () => db.gamePointDebit.create({
        data: {
          ...debitData,
          id: `authority_debit_reservation_duplicate_${suffix}`,
          requestId: crypto.randomUUID(),
          userId: legacyUserId,
          gamePlayerId: `legacy_player_${suffix}`,
          sourceTransactionId: `authority_debit_tx_reservation_duplicate_${suffix}`,
        },
      }),
      "",
      "A duplicate game reservation ID was accepted"
    );
    await expectFailure(
      () => db.gamePointDebit.create({
        data: {
          ...debitData,
          id: `authority_debit_transaction_duplicate_${suffix}`,
          requestId: crypto.randomUUID(),
          reservationId: `authority_reservation_transaction_duplicate_${suffix}`,
          userId: legacyUserId,
          gamePlayerId: `legacy_player_${suffix}`,
        },
      }),
      "",
      "A duplicate Point debit transaction ID was accepted"
    );
    await expectFailure(
      () => db.gamePointDebit.create({
        data: {
          ...debitData,
          id: `authority_debit_pending_${suffix}`,
          requestId: crypto.randomUUID(),
          reservationId: `authority_reservation_pending_${suffix}`,
          sourceTransactionId: `authority_debit_tx_pending_${suffix}`,
        },
      }),
      "",
      "A second unresolved Point debit was accepted for one account"
    );
    await db.gamePointDebit.update({
      where: { id: debitId },
      data: { status: "CAPTURED", capturedAt: new Date() },
    });
    await db.gamePointDebit.create({
      data: {
        ...debitData,
        id: `authority_debit_after_capture_${suffix}`,
        requestId: crypto.randomUUID(),
        reservationId: `authority_reservation_after_capture_${suffix}`,
        sourceTransactionId: `authority_debit_tx_after_capture_${suffix}`,
      },
    });
    await db.gamePointDebit.update({
      where: { id: `authority_debit_after_capture_${suffix}` },
      data: { status: "CAPTURED", capturedAt: new Date() },
    });

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
    await expectFailure(
      () => db.gamePointDebit.create({
        data: {
          ...debitData,
          id: `authority_debit_blocked_by_conversion_${suffix}`,
          requestId: crypto.randomUUID(),
          reservationId: `authority_reservation_blocked_by_conversion_${suffix}`,
          sourceTransactionId: `authority_debit_tx_blocked_by_conversion_${suffix}`,
        },
      }),
      "",
      "Point debit started while USDT to Point was unresolved"
    );
    const blockedDebit = await db.gamePointDebit.findUnique({
      where: { id: `authority_debit_blocked_by_conversion_${suffix}` },
    });
    assert(!blockedDebit, "Blocked Point debit left a saga row");

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
    await db.gamePointDebit.deleteMany({ where: { userId: { in: [linkedUserId, legacyUserId] } } });
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
