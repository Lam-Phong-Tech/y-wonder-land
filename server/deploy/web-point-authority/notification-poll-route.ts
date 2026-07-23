import { NextResponse } from "next/server";
import { auth } from "@/auth";
import { db } from "@/lib/db";
import { getGamePointWalletView } from "@/lib/game-point-authority";

export const dynamic = "force-dynamic";

export async function GET() {
  const session = await auth();
  if (!session?.user) return NextResponse.json({ ok: false }, { status: 401 });
  const user = session.user as any;
  const userId = user.id as string;
  const isAdmin = user.role === "ADMIN" || user.role === "SUPER_ADMIN";

  const itemsPromise = db.notification.findMany({
    where: { userId, unread: true },
    orderBy: { createdAt: "desc" },
    take: 20,
    select: { id: true, emoji: true, textVi: true, textEn: true, link: true, createdAt: true },
  });

  if (isAdmin) {
    const pendingTypes = ["DEPOSIT", "USDT_DEPOSIT", "YWH_DEPOSIT", "WITHDRAW", "USDT_WITHDRAW"];
    const [items, pending, latestTx, latestAudit] = await Promise.all([
      itemsPromise,
      db.transaction.count({ where: { status: "PENDING", type: { in: pendingTypes } } }),
      db.transaction.findFirst({ orderBy: { createdAt: "desc" }, select: { createdAt: true } }),
      db.auditLog.findFirst({ orderBy: { createdAt: "desc" }, select: { createdAt: true } }),
    ]);
    const sync = `a:${pending}:${latestTx?.createdAt.getTime() || 0}:${latestAudit?.createdAt.getTime() || 0}`;
    return NextResponse.json({ ok: true, unreadCount: items.length, items, sync, isAdmin: true, pending });
  }

  const [items, wallet, latestTx, gamePoint] = await Promise.all([
    itemsPromise,
    db.wallet.findUnique({
      where: { userId },
      select: { balanceGXL: true, balanceUsdt: true, balanceYwh: true, lockedYwh: true },
    }),
    db.transaction.findFirst({
      where: { userId },
      orderBy: { createdAt: "desc" },
      select: { createdAt: true },
    }),
    getGamePointWalletView(userId),
  ]);
  const pointToken = gamePoint.linked
    ? (gamePoint.available ? String(gamePoint.point) : "unavailable")
    : String(wallet?.balanceGXL || 0);
  const sync = `u:${pointToken}:${wallet?.balanceUsdt || 0}:${wallet?.balanceYwh || 0}:${wallet?.lockedYwh || 0}:${latestTx?.createdAt.getTime() || 0}`;
  return NextResponse.json({ ok: true, unreadCount: items.length, items, sync, isAdmin: false });
}
