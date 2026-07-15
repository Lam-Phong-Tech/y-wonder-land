import { timingSafeEqual } from "crypto";
import { NextResponse } from "next/server";
import { retryPendingGamePointSync } from "@/lib/game-point-sync";

export const runtime = "nodejs";
export const dynamic = "force-dynamic";

function authorized(request: Request): boolean {
  const secret = String(process.env.CRON_SECRET || "");
  const supplied = String(request.headers.get("authorization") || "");
  const expected = `Bearer ${secret}`;
  if (!secret || supplied.length !== expected.length) return false;
  return timingSafeEqual(Buffer.from(supplied), Buffer.from(expected));
}

async function run(request: Request) {
  if (!authorized(request)) {
    return NextResponse.json({ ok: false, error: "Unauthorized" }, { status: 401 });
  }
  const result = await retryPendingGamePointSync(50);
  return NextResponse.json({ ok: true, ...result });
}

export async function GET(request: Request) {
  return run(request);
}

export async function POST(request: Request) {
  return run(request);
}
