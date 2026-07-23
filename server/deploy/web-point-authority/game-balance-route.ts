import { timingSafeEqual } from "crypto";
import { NextResponse } from "next/server";
import { db } from "@/lib/db";
import { getGamePointWalletView } from "@/lib/game-point-authority";

export const dynamic = "force-dynamic";

function authed(req: Request): boolean {
  const secret = String(process.env.GAME_API_SECRET || "");
  const supplied = String(req.headers.get("authorization") || "");
  const expected = `Bearer ${secret}`;
  if (!secret || supplied.length !== expected.length) return false;
  return timingSafeEqual(Buffer.from(supplied), Buffer.from(expected));
}

export async function GET(req: Request) {
  if (!authed(req)) return NextResponse.json({ ok: false, error: "Unauthorized" }, { status: 401 });
  const uid = (new URL(req.url).searchParams.get("uid") || "").trim().toLowerCase();
  if (!uid) return NextResponse.json({ ok: false, error: "Missing uid" }, { status: 400 });
  const user = await db.user.findFirst({
    where: { OR: [{ refCode: uid }, { username: uid }] },
    select: {
      id: true,
      wallet: {
        select: { balanceGXL: true, balanceUsdt: true, balanceYwh: true, lockedYwh: true },
      },
    },
  });
  if (!user) return NextResponse.json({ ok: false, error: "User not found" }, { status: 404 });

  const gamePoint = await getGamePointWalletView(user.id);
  if (gamePoint.linked && !gamePoint.available) {
    return NextResponse.json({ ok: false, error: "GAME_POINT_SYNC_UNAVAILABLE" }, { status: 503 });
  }
  const wallet = user.wallet;
  return NextResponse.json({
    ok: true,
    uid,
    point: gamePoint.linked ? gamePoint.point : (wallet?.balanceGXL || 0),
    pointSource: gamePoint.linked ? "game" : "legacy-web",
    usdt: wallet?.balanceUsdt || 0,
    ywh: wallet?.balanceYwh || 0,
    lockedYwh: wallet?.lockedYwh || 0,
  });
}
