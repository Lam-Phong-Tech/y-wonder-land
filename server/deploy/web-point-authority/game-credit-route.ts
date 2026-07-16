import { timingSafeEqual } from "crypto";
import { NextResponse } from "next/server";
import { db } from "@/lib/db";
import { isGamePointLinkedAccount } from "@/lib/game-point-authority";

export const dynamic = "force-dynamic";

function authed(req: Request): boolean {
  const secret = String(process.env.GAME_API_SECRET || "");
  const supplied = String(req.headers.get("authorization") || "");
  const expected = `Bearer ${secret}`;
  if (!secret || supplied.length !== expected.length) return false;
  return timingSafeEqual(Buffer.from(supplied), Buffer.from(expected));
}

export async function POST(req: Request) {
  if (!authed(req)) return NextResponse.json({ ok: false, error: "Unauthorized" }, { status: 401 });

  let body: any;
  try {
    body = await req.json();
  } catch {
    return NextResponse.json({ ok: false, error: "Invalid JSON" }, { status: 400 });
  }
  const uid = String(body?.uid || "").trim().toLowerCase();
  const amount = Number(body?.amount);
  const ref = String(body?.ref || "").trim();
  const reason = String(body?.reason || "GAME").slice(0, 60);
  if (!uid || !ref) return NextResponse.json({ ok: false, error: "Missing uid/ref" }, { status: 400 });
  if (!Number.isFinite(amount) || amount <= 0 || amount > 100_000_000) {
    return NextResponse.json({ ok: false, error: "Invalid amount" }, { status: 400 });
  }

  const user = await db.user.findFirst({
    where: { OR: [{ refCode: uid }, { username: uid }] },
    select: { id: true },
  });
  if (!user) return NextResponse.json({ ok: false, error: "User not found" }, { status: 404 });
  if (await isGamePointLinkedAccount(user.id)) {
    return NextResponse.json({
      ok: false,
      error: "GAME_POINT_LEDGER_IS_AUTHORITATIVE",
    }, { status: 409 });
  }

  const extRef = `GAME:${ref}`;
  const duplicate = await db.transaction.findFirst({
    where: { externalRef: extRef },
    select: { id: true },
  });
  if (duplicate) {
    const wallet = await db.wallet.findUnique({
      where: { userId: user.id },
      select: { balanceGXL: true },
    });
    return NextResponse.json({ ok: true, idempotent: true, point: wallet?.balanceGXL || 0 });
  }

  await db.$transaction([
    db.wallet.upsert({
      where: { userId: user.id },
      update: { balanceGXL: { increment: amount } },
      create: { userId: user.id, balanceGXL: amount },
    }),
    db.transaction.create({
      data: {
        userId: user.id,
        type: "PAYOUT",
        amount,
        currency: "GXL",
        status: "SUCCESS",
        externalRef: extRef,
        metadata: JSON.stringify({ source: "GAME", reason }),
      },
    }),
  ]);

  const wallet = await db.wallet.findUnique({
    where: { userId: user.id },
    select: { balanceGXL: true },
  });
  return NextResponse.json({ ok: true, point: wallet?.balanceGXL || 0 });
}
