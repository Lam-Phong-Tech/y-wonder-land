const crypto = require("crypto");
const fs = require("fs");
const path = require("path");

const EXPECTED_HASHES = new Map([
  ["prisma/schema.prisma", "91b3b57ad1c484b30196e2e5633104a1ade571f9f65cdb81c579eacf0d532800"],
  ["lib/actions/convert.ts", "26a80e16c3116baef52947cd388cdaef3e339c2505560e9859562e357d9a4c30"],
  ["lib/actions/admin.ts", "2082fba1ded776cc025c8d4d936ba6ba7f7f38d5aa013b23494dc7bc8cb053a3"],
  ["lib/queries.ts", "faa0cbbb1d4a8abda7f9c9ce236310c0d398cc0eb329f4380ea71e3b1376d703"],
  ["lib/game-point-sync.ts", "0eeb8f865cf2a4af05b75ce49f38997ba3daa0d05c70237c576cb4c8e3bb3d8d"],
  ["app/api/cron/game-point-sync/route.ts", "d0a47dd19ebce4a6c74eaca380974af85c2aa3511026fc370a3421a7ee3f178f"],
  ["components/PointConvertActions.tsx", "dd3dcb8a1146b3367acd0579cf25a1d7375bec80e4843bb2b8c67d75f8691785"],
  ["components/Wallet1Section.tsx", "a54d8e150fba7247622f2efae557af40d40c239378f06c07733d0d80d0165980"],
  ["app/[locale]/(app)/wallet/page.tsx", "a9bf6872ad404b3cc203aaae256e4db3a7324d0b974b5b3ac4d16398a916d3cb"],
  ["app/api/game/balance/route.ts", "2344c64085682e1c8d6473850571998fe296ec9c6cb539aa6bbd496591861780"],
  ["app/api/game/credit/route.ts", "49fcd80b368414d2269bd1cafa7ad5f2523d01bf7b4bba1b2ec083442644f7ee"],
  ["app/api/notifications/poll/route.ts", "e60c11abb41463bb66790470d9d7afcb4627d3ac07893aec14bc8b3994af71a6"],
]);

function fail(message) {
  throw new Error(message);
}

function sha256(value) {
  return crypto.createHash("sha256").update(value).digest("hex");
}

function read(root, relative) {
  const target = path.join(root, relative);
  const stat = fs.lstatSync(target);
  if (!stat.isFile() || stat.isSymbolicLink()) fail(`Unsafe source file: ${relative}`);
  return fs.readFileSync(target, "utf8");
}

function write(root, relative, value) {
  const target = path.join(root, relative);
  fs.mkdirSync(path.dirname(target), { recursive: true });
  fs.writeFileSync(target, value, "utf8");
}

function overlay(name) {
  return fs.readFileSync(path.join(__dirname, name), "utf8").replace(/\r\n/g, "\n");
}

function eolFor(source) {
  return source.includes("\r\n") ? "\r\n" : "\n";
}

function withEol(value, source) {
  return value.replace(/\r\n/g, "\n").replace(/\n/g, eolFor(source));
}

function replaceOnce(source, needle, replacement, label) {
  const first = source.indexOf(needle);
  if (first < 0) fail(`Missing ${label}`);
  if (source.indexOf(needle, first + needle.length) >= 0) fail(`Ambiguous ${label}`);
  return source.slice(0, first) + replacement + source.slice(first + needle.length);
}

function replaceRange(source, startNeedle, endNeedle, replacement, label) {
  const start = source.indexOf(startNeedle);
  if (start < 0) fail(`Missing ${label} start`);
  const end = source.indexOf(endNeedle, start + startNeedle.length);
  if (end < 0) fail(`Missing ${label} end`);
  return source.slice(0, start) + replacement + source.slice(end);
}

function patchSchema(source) {
  if (source.includes("model GamePointLinkedAccount") || source.includes("model GamePointConversion")) {
    fail("Point authority schema is already present");
  }
  const relationAnchor = "  gamePointSyncOutbox GamePointSyncOutbox[]";
  source = replaceOnce(source, relationAnchor, [
    relationAnchor,
    "  gamePointLink GamePointLinkedAccount?",
    "  gamePointConversions GamePointConversion[]",
  ].join(eolFor(source)), "User Point authority relation anchor");
  const models = `

model GamePointLinkedAccount {
  userId       String   @id
  gamePlayerId String   @unique
  user         User     @relation(fields: [userId], references: [id], onDelete: Cascade)
  linkedBy     String
  note         String?
  linkedAt     DateTime @default(now())
}

model GamePointConversion {
  id                  String   @id
  requestId           String   @unique
  sourceTransactionId String   @unique
  outboxId            String   @unique
  userId              String
  user                User     @relation(fields: [userId], references: [id], onDelete: Cascade)
  usdtMicros          String
  pointMicros         String
  rateVersionId       String
  rateVersion         PointExchangeRateVersion @relation(fields: [rateVersionId], references: [id])
  rateMicros           String
  roundingRemainder    String   @default("0")
  status              String   @default("PENDING")
  lastError           String?
  sentAt              DateTime?
  createdAt           DateTime @default(now())
  updatedAt           DateTime @updatedAt
  @@index([userId, createdAt])
  @@index([status, createdAt])
}

model PointExchangeRateVersion {
  id             String   @id @default(cuid())
  pair           String
  rateMicros     String
  isActive       Boolean  @default(true)
  effectiveAt    DateTime @default(now())
  createdBy      String
  sourceRateId   String?
  createdAt      DateTime @default(now())
  conversions    GamePointConversion[]
  @@index([pair, effectiveAt])
}
`;
  return source.replace(/\s*$/, "") + withEol(models, source);
}

function patchConvert(source) {
  if (source.includes("@/lib/game-point-authority")) fail("Point conversion is already authority-aware");
  const syncImport = `import {
  dispatchGamePointSyncOutboxById,
  GAME_POINT_SYNC_SOURCE,
  pointToGameAmountText,
} from "@/lib/game-point-sync";`;
  const patchedSyncImport = `import {
  dispatchGamePointSyncOutboxById,
  GAME_POINT_SYNC_SOURCE,
} from "@/lib/game-point-sync";`;
  const authorityImport = `import {
  gamePointConversionTransactionId,
  isGamePointLinkedAccount,
  normalizeGamePointConversionRequestId,
  normalizeUsdtMicros,
  resolveGamePointConversionAuthority,
} from "@/lib/game-point-authority";`;
  const rateImport = `import {
  getActiveUsdtPointRate,
  pointMicrosToAmountText,
  pointMicrosToNumber,
  quoteUsdtToPointMicros,
} from "@/lib/point-rate";`;
  source = replaceOnce(
    source,
    withEol(syncImport, source),
    withEol(`${patchedSyncImport}\n${authorityImport}\n${rateImport}`, source),
    "Point sync import"
  );

  const pointStart = source.indexOf("export async function convertPointToUsdtAction");
  const pointEnd = source.indexOf("// ---- USDT", pointStart);
  if (pointStart < 0 || pointEnd < 0) fail("Cannot isolate Point to USDT action");
  let pointAction = source.slice(pointStart, pointEnd);
  const userAnchor = "    const userId = me.id;";
  pointAction = replaceOnce(pointAction, userAnchor, withEol(`${userAnchor}
    if (await isGamePointLinkedAccount(userId)) {
      return { ok: false, error: "GAME_POINT_TO_USDT_REQUIRES_GAME_DEBIT" };
    }`, pointAction), "Point to USDT linked-account guard");
  source = source.slice(0, pointStart) + pointAction + source.slice(pointEnd);

  const actionStart = source.indexOf("export async function convertUsdtToPointAction");
  const actionEnd = source.indexOf("// ---- USDT", actionStart);
  if (actionStart < 0 || actionEnd < 0) fail("Cannot isolate USDT to Point action");
  const replacement = withEol(overlay("convert-usdt-to-point-action.tsfrag") + "\n\n", source);
  return source.slice(0, actionStart) + replacement + source.slice(actionEnd);
}

function patchAdmin(source) {
  if (source.includes("replaceActiveUsdtPointRate")) fail("Admin rate action is already versioned");
  source = replaceOnce(
    source,
    'import { db } from "@/lib/db";',
    withEol('import { db } from "@/lib/db";\nimport { replaceActiveUsdtPointRate } from "@/lib/point-rate";', source),
    "admin Point rate import"
  );
  const start = source.indexOf("// Save exchange rate");
  const end = source.indexOf("// Approve KYC", start);
  if (start < 0 || end < 0) fail("Cannot isolate saveExchangeRateAction");
  const replacement = withEol(`// Save exchange rate and append an immutable Point settlement version.
export async function saveExchangeRateAction(gxlToVnd: number, usdtToGxl: number): Promise<Result> {
  try {
    const admin = await requireAdmin();
    if (!Number.isFinite(gxlToVnd) || !Number.isFinite(usdtToGxl)
        || gxlToVnd <= 0 || usdtToGxl <= 0) {
      return { ok: false, error: "Tỷ giá phải lớn hơn 0" };
    }

    const previous = await db.pointExchangeRateVersion.findFirst({
      where: { pair: "USDT_POINT", isActive: true },
      orderBy: [{ effectiveAt: "desc" }, { createdAt: "desc" }],
      select: { id: true, rateMicros: true },
    });
    const next = await db.$transaction(async (txdb) => {
      await txdb.exchangeRate.upsert({
        where: { fromCurrency_toCurrency_isActive: { fromCurrency: "GXL", toCurrency: "VND", isActive: true } },
        update: { rate: gxlToVnd },
        create: { fromCurrency: "GXL", toCurrency: "VND", rate: gxlToVnd, isActive: true },
      });
      const sourceRate = await txdb.exchangeRate.upsert({
        where: { fromCurrency_toCurrency_isActive: { fromCurrency: "USDT", toCurrency: "GXL", isActive: true } },
        update: { rate: usdtToGxl },
        create: { fromCurrency: "USDT", toCurrency: "GXL", rate: usdtToGxl, isActive: true },
      });
      return replaceActiveUsdtPointRate(txdb, usdtToGxl, admin.id, sourceRate.id);
    });

    await logAudit(admin, "UPDATE_EXCHANGE_RATE", undefined, {
      gxlToVnd,
      usdtToGxl,
      previousPointRateVersionId: previous?.id || null,
      previousPointRateMicros: previous?.rateMicros || null,
      pointRateVersionId: next.id,
      pointRateMicros: next.rateMicros,
    });
    revalidatePath("/backend/admin/rate");
    revalidatePath("/wallet");
    return { ok: true };
  } catch (e: any) {
    return { ok: false, error: e.message };
  }
}

`, source);
  return source.slice(0, start) + replacement + source.slice(end);
}

function patchQueries(source) {
  if (source.includes("getGamePointWalletView")) fail("Queries already use the game Point authority");
  source = replaceOnce(
    source,
    'import { db } from "./db";',
    withEol('import { db } from "./db";\nimport { getGamePointWalletView } from "./game-point-authority";', source),
    "queries db import"
  );
  const start = source.indexOf("export async function getUserWithWallet");
  const end = source.indexOf("export async function getProjects", start);
  if (start < 0 || end < 0) fail("Cannot isolate getUserWithWallet");
  const replacement = withEol(`export async function getUserWithWallet(userId: string) {
  const data = await db.user.findUnique({
    where: { id: userId },
    include: { wallet: true },
  });
  if (!data) return null;

  const gamePointAuthority = await getGamePointWalletView(userId);
  if (!data.wallet || !gamePointAuthority.linked) return { ...data, gamePointAuthority };
  return {
    ...data,
    wallet: {
      ...data.wallet,
      balanceGXL: gamePointAuthority.available ? Number(gamePointAuthority.point || 0) : 0,
      lockedGXL: 0,
    },
    gamePointAuthority,
  };
}

`, source);
  return source.slice(0, start) + replacement + source.slice(end);
}

function patchPointConvertActions(source) {
  source = replaceOnce(
    source,
    "const POINT_PRICE = 0.06; // USDT per Point\n",
    "",
    "legacy fixed Point price"
  );
  source = replaceOnce(
    source,
    'import { useState, useTransition } from "react";',
    'import { useEffect, useState, useTransition } from "react";',
    "PointConvertActions React import"
  );
  source = replaceOnce(
    source,
    'import { transferPointAction } from "@/lib/actions/transfer";',
    withEol(`import { transferPointAction } from "@/lib/actions/transfer";
import {
  clearGamePointConversionIntent,
  getOrCreateGamePointConversionIntent,
  readGamePointConversionIntent,
} from "@/lib/game-point-conversion-intent";`, source),
    "PointConvertActions intent import"
  );
  const firstStart = source.indexOf("export function PointConvertActions");
  const firstEnd = source.indexOf("// Internal P2P Point transfer", firstStart);
  if (firstStart < 0 || firstEnd < 0) fail("Cannot isolate PointConvertActions component");
  const firstReplacement = withEol(`export function PointConvertActions({
  userId,
  locale,
  balancePoint,
  balanceUsdt,
  pointPerUsdt,
  gamePointLinked = false,
  gamePointAvailable = true,
}: {
  userId: string;
  locale: string;
  balancePoint: number;
  balanceUsdt: number;
  pointPerUsdt: number;
  gamePointLinked?: boolean;
  gamePointAvailable?: boolean;
}) {
  const t = (vi: string, en: string) => (locale === "vi" ? vi : en);
  const [open, setOpen] = useState(false);
  const [xferOpen, setXferOpen] = useState(false);
  return (
    <>
      <button
        onClick={() => setOpen(true)}
        disabled={gamePointLinked && !gamePointAvailable}
        className="btn btn-grass py-[9px] px-[14px] text-[13px] whitespace-nowrap"
      >
        {gamePointLinked ? t("Nạp Point từ USDT", "Top up Point from USDT") : t("Đổi Point ↔ USDT", "Swap Point ↔ USDT")}
      </button>
      {!gamePointLinked && (
        <button onClick={() => setXferOpen(true)} className="btn btn-ghost py-[9px] px-[14px] text-[13px] whitespace-nowrap">
          {t("Chuyển Point", "Send Point")}
        </button>
      )}
      {open && (
        <ConvertModal
          userId={userId}
          locale={locale}
          balancePoint={balancePoint}
          balanceUsdt={balanceUsdt}
          pointPerUsdt={pointPerUsdt}
          gamePointLinked={gamePointLinked}
          onClose={() => setOpen(false)}
        />
      )}
      {!gamePointLinked && xferOpen && (
        <TransferModal locale={locale} balancePoint={balancePoint} onClose={() => setXferOpen(false)} />
      )}
    </>
  );
}

`, source);
  source = source.slice(0, firstStart) + firstReplacement + source.slice(firstEnd);

  const modalStart = source.indexOf("function ConvertModal");
  if (modalStart < 0) fail("Cannot find ConvertModal");
  let modal = source.slice(modalStart);
  const signatureEnd = modal.indexOf("  const t =");
  if (signatureEnd < 0) fail("Cannot isolate ConvertModal signature");
  const signature = withEol(`function ConvertModal({
  userId,
  locale,
  balancePoint,
  balanceUsdt,
  pointPerUsdt,
  gamePointLinked,
  onClose,
}: {
  userId: string;
  locale: string;
  balancePoint: number;
  balanceUsdt: number;
  pointPerUsdt: number;
  gamePointLinked: boolean;
  onClose: () => void;
}) {
`, modal);
  modal = signature + modal.slice(signatureEnd);
  modal = replaceOnce(
    modal,
    '  const [dir, setDir] = useState<"P2U" | "U2P">("P2U");',
    '  const [dir, setDir] = useState<"P2U" | "U2P">(gamePointLinked ? "U2P" : "P2U");',
    "ConvertModal initial direction"
  );
  modal = replaceOnce(
    modal,
    "  const out = isP2U ? amtNum * POINT_PRICE * (1 - POINT_USDT_FEE_PCT / 100) : amtNum / POINT_PRICE;",
    "  const out = isP2U ? (amtNum / pointPerUsdt) * (1 - POINT_USDT_FEE_PCT / 100) : amtNum * pointPerUsdt;",
    "ConvertModal dynamic Point rate calculation"
  );
  modal = replaceOnce(
    modal,
    '{t(`Tỷ giá: 1 Point = ${POINT_PRICE} USDT · đổi tức thì. Phí đổi Point → USDT: 10% (chiều USDT → Point miễn phí). Muốn rút tiền: đổi Point → USDT rồi rút ở ví USDT.`,\n           `Rate: 1 Point = ${POINT_PRICE} USDT · instant. Point → USDT fee: 10% (USDT → Point is free). To cash out: convert Point → USDT then withdraw from the USDT wallet.`)}',
    '{t(`Tỷ giá hiện tại: 1 USDT = ${pointPerUsdt} Point. Phí đổi Point → USDT: 10%.`,\n           `Current rate: 1 USDT = ${pointPerUsdt} Point. Point → USDT fee: 10%.`)}',
    "ConvertModal dynamic Point rate copy"
  );
  modal = replaceOnce(
    modal,
    "  const [pending, startTransition] = useTransition();",
    withEol(`  const [pending, startTransition] = useTransition();
  useEffect(() => {
    const intent = readGamePointConversionIntent(userId);
    if (intent) setAmt(String(intent.amount));
  }, [userId]);`, modal),
    "ConvertModal durable intent restore"
  );
  modal = replaceRange(modal, "  const submit = () => {", "\n\n  return (", withEol(`  const submit = () => {
    if (amtNum <= 0 || amtNum > max + 1e-9) {
      toast.push({ type: "warn", msg: t("Số lượng không hợp lệ", "Invalid amount") });
      return;
    }
    startTransition(async () => {
      const intent = isP2U ? null : getOrCreateGamePointConversionIntent(userId, amtNum);
      const res = isP2U
        ? await convertPointToUsdtAction(amtNum)
        : await convertUsdtToPointAction(intent!.amount, intent!.requestId);
      if (!res.ok) {
        toast.push({ type: "warn", msg: t("Đổi thất bại", "Convert failed"), sub: res.error });
        return;
      }
      if ((res as any).pending) {
        toast.push({
          type: "warn",
          msg: t("Giao dịch đang xử lý", "Conversion pending"),
          sub: t("USDT đã được giữ chỗ; hệ thống sẽ tự thử lại cùng giao dịch.", "USDT is reserved; the same conversion will retry automatically."),
        });
        onClose();
        router.refresh();
        return;
      }
      if (intent) clearGamePointConversionIntent(userId, intent.requestId);
      toast.push({
        type: "ok",
        msg: isP2U ? t("Đã đổi Point → USDT", "Converted Point → USDT") : t("Đã đổi USDT → Point", "Converted USDT → Point"),
        sub: isP2U ? "+" + usd((res as any).usdt || 0) : "+" + nf.format(Math.round((res as any).point || 0)) + " Point",
      });
      onClose();
      router.refresh();
    });
  };`, modal), "ConvertModal submit");
  modal = replaceOnce(
    modal,
    "] as const).map(([k, label]) => (",
    "] as const).filter(([k]) => !gamePointLinked || k === \"U2P\").map(([k, label]) => (",
    "ConvertModal linked direction filter"
  );
  return source.slice(0, modalStart) + modal;
}

function patchWallet1Section(source) {
  source = replaceOnce(
    source,
    'import { convertUsdtToPointAction, buyYwhWithUsdtAction } from "@/lib/actions/convert";',
    withEol(`import { convertUsdtToPointAction, buyYwhWithUsdtAction } from "@/lib/actions/convert";
import {
  clearGamePointConversionIntent,
  getOrCreateGamePointConversionIntent,
  readGamePointConversionIntent,
} from "@/lib/game-point-conversion-intent";`, source),
    "Wallet1 intent import"
  );
  source = replaceOnce(
    source,
    withEol(`export function Wallet1Section({
  locale, balanceUsdt, eligibility, networks, hasWithdrawPassword,
}: {
  locale: string; balanceUsdt: number; eligibility: Eligibility | null;
  networks: Network[]; hasWithdrawPassword: boolean;
}) {`, source),
    withEol(`export function Wallet1Section({
  userId, locale, balanceUsdt, pointPerUsdt, eligibility, networks, hasWithdrawPassword,
}: {
  userId: string; locale: string; balanceUsdt: number; pointPerUsdt: number; eligibility: Eligibility | null;
  networks: Network[]; hasWithdrawPassword: boolean;
}) {`, source),
    "Wallet1 user id prop"
  );
  source = replaceOnce(
    source,
    '<SwapModal locale={locale} balanceUsdt={balanceUsdt} onClose={() => setSwapOpen(false)} />',
    '<SwapModal userId={userId} locale={locale} balanceUsdt={balanceUsdt} pointPerUsdt={pointPerUsdt} onClose={() => setSwapOpen(false)} />',
    "Wallet1 SwapModal user id"
  );
  const start = source.indexOf("function SwapModal");
  const end = source.indexOf("function Cell", start);
  if (start < 0 || end < 0) fail("Cannot isolate Wallet1 SwapModal");
  let modal = source.slice(start, end);
  modal = replaceOnce(
    modal,
    "function SwapModal({ locale, balanceUsdt, onClose }: { locale: string; balanceUsdt: number; onClose: () => void }) {",
    "function SwapModal({ userId, locale, balanceUsdt, pointPerUsdt, onClose }: { userId: string; locale: string; balanceUsdt: number; pointPerUsdt: number; onClose: () => void }) {",
    "Wallet1 SwapModal user id signature"
  );
  modal = replaceOnce(
    modal,
    "  const out = amtNum / 0.06; // both Point and YWH priced at 0.06 USDT",
    "  const out = target === \"POINT\" ? amtNum * pointPerUsdt : amtNum / 0.06;",
    "Wallet1 dynamic Point rate calculation"
  );
  modal = replaceOnce(
    modal,
    '{t("Giá 0,06 USDT/coin. Point dùng ngay; YWH bị khoá theo lịch vesting (10% mở sau 30 ngày; 90% khoá 6 tháng rồi mở 7.5%/tháng).", "Price 0.06 USDT/coin. Point is instant; YWH is locked by the vesting schedule (10% after 30 days; 90% has a 6-month cliff then 7.5%/month).")}',
    '{t(`Point: 1 USDT = ${pointPerUsdt} Point. YWH: 0,06 USDT/YWH và áp dụng lịch khoá.`, `Point: 1 USDT = ${pointPerUsdt} Point. YWH: 0.06 USDT/YWH with vesting.`)}',
    "Wallet1 dynamic Point rate copy"
  );
  modal = replaceOnce(
    modal,
    "  const [pending, startTransition] = useTransition();",
    withEol(`  const [pending, startTransition] = useTransition();
  useEffect(() => {
    const intent = readGamePointConversionIntent(userId);
    if (intent) setAmt(String(intent.amount));
  }, [userId]);`, modal),
    "Wallet1 durable intent restore"
  );
  modal = replaceRange(modal, "  const submit = () => {", "\n\n  return (", withEol(`  const submit = () => {
    if (amtNum <= 0 || amtNum > balanceUsdt + 1e-9) {
      toast.push({ type: "warn", msg: t("Số USDT không hợp lệ", "Invalid USDT amount") });
      return;
    }
    startTransition(async () => {
      const intent = target === "POINT"
        ? getOrCreateGamePointConversionIntent(userId, amtNum)
        : null;
      const res = target === "POINT"
        ? await convertUsdtToPointAction(intent!.amount, intent!.requestId)
        : await buyYwhWithUsdtAction(amtNum);
      if (!res.ok) {
        toast.push({ type: "warn", msg: t("Đổi thất bại", "Swap failed"), sub: res.error });
        return;
      }
      if (target === "POINT" && (res as any).pending) {
        toast.push({
          type: "warn",
          msg: t("Giao dịch đang xử lý", "Conversion pending"),
          sub: t("USDT đã được giữ chỗ; hệ thống sẽ tự thử lại cùng giao dịch.", "USDT is reserved; the same conversion will retry automatically."),
        });
        onClose();
        router.refresh();
        return;
      }
      if (intent) clearGamePointConversionIntent(userId, intent.requestId);
      toast.push({
        type: "ok",
        msg: target === "POINT" ? t("Đã đổi USDT → Point", "Swapped USDT → Point") : t("Đã đổi USDT → YWH (đã khoá)", "Swapped USDT → YWH (locked)"),
        sub: target === "POINT"
          ? nf.format((res as any).point || 0) + " Point"
          : nf.format(Math.round(out)) + " YWH",
      });
      onClose();
      router.refresh();
    });
  };`, modal), "Wallet1 SwapModal submit");
  return source.slice(0, start) + modal + source.slice(end);
}

function patchWalletPage(source) {
  source = replaceOnce(
    source,
    'import { db } from "@/lib/db";',
    withEol('import { db } from "@/lib/db";\nimport { getActiveUsdtPointRate } from "@/lib/point-rate";', source),
    "wallet Point rate import"
  );
  source = replaceOnce(
    source,
    "const [data, transactions, rateRow, paymentNetworks, userRow, ywhLocks, commissions, earlyBonus] = await Promise.all([",
    "const [data, transactions, rateRow, pointRate, paymentNetworks, userRow, ywhLocks, commissions, earlyBonus] = await Promise.all([",
    "wallet Point rate result"
  );
  source = replaceOnce(
    source,
    '    db.exchangeRate.findFirst({ where: { fromCurrency: "GXL", toCurrency: "VND", isActive: true } }),',
    withEol(`    db.exchangeRate.findFirst({ where: { fromCurrency: "GXL", toCurrency: "VND", isActive: true } }),
    getActiveUsdtPointRate(),`, source),
    "wallet Point rate query"
  );
  source = replaceOnce(
    source,
    withEol(`<Wallet1Section
          locale={locale}`, source),
    withEol(`<Wallet1Section
          userId={user.id}
          pointPerUsdt={Number(pointRate.rateMicros) / 1_000_000}
          locale={locale}`, source),
    "wallet Wallet1Section user id"
  );
  const anchor = '<PointConvertActions locale={locale} balancePoint={w.balanceGXL} balanceUsdt={w.balanceUsdt} />';
  const replacement = `<PointConvertActions
              userId={user.id}
              locale={locale}
              balancePoint={w.balanceGXL}
              balanceUsdt={w.balanceUsdt}
              pointPerUsdt={Number(pointRate.rateMicros) / 1_000_000}
              gamePointLinked={Boolean(data?.gamePointAuthority?.linked)}
              gamePointAvailable={data?.gamePointAuthority?.available !== false}
            />`;
  return replaceOnce(source, anchor, withEol(replacement, source), "wallet PointConvertActions props");
}

function patchWebRoot(root, migrationId) {
  for (const [relative, expected] of EXPECTED_HASHES) {
    const source = read(root, relative);
    const actual = sha256(Buffer.from(source, "utf8"));
    if (actual !== expected) fail(`Source hash mismatch for ${relative}: ${actual}`);
  }

  const migrationRoot = path.join(root, "prisma", "migrations", migrationId);
  if (fs.existsSync(migrationRoot)) fail(`Migration already exists: ${migrationId}`);
  if (fs.existsSync(path.join(root, "lib", "game-point-authority.ts"))) {
    fail("game-point-authority.ts already exists");
  }
  if (fs.existsSync(path.join(root, "lib", "point-rate.ts"))) {
    fail("point-rate.ts already exists");
  }

  write(root, "prisma/schema.prisma", patchSchema(read(root, "prisma/schema.prisma")));
  write(root, "lib/actions/convert.ts", patchConvert(read(root, "lib/actions/convert.ts")));
  write(root, "lib/actions/admin.ts", patchAdmin(read(root, "lib/actions/admin.ts")));
  write(root, "lib/queries.ts", patchQueries(read(root, "lib/queries.ts")));
  write(root, "components/PointConvertActions.tsx", patchPointConvertActions(read(root, "components/PointConvertActions.tsx")));
  write(root, "components/Wallet1Section.tsx", patchWallet1Section(read(root, "components/Wallet1Section.tsx")));
  write(root, "app/[locale]/(app)/wallet/page.tsx", patchWalletPage(read(root, "app/[locale]/(app)/wallet/page.tsx")));

  write(root, "lib/game-point-authority.ts", overlay("game-point-authority.ts"));
  write(root, "lib/point-rate.ts", overlay("point-rate.ts"));
  write(root, "lib/game-point-conversion-intent.ts", overlay("game-point-conversion-intent.ts"));
  write(root, "lib/game-point-sync.ts", overlay("game-point-sync.ts"));
  write(root, "app/api/cron/game-point-sync/route.ts", overlay("cron-route.ts"));
  write(root, "app/api/game/balance/route.ts", overlay("game-balance-route.ts"));
  write(root, "app/api/game/credit/route.ts", overlay("game-credit-route.ts"));
  write(root, "app/api/notifications/poll/route.ts", overlay("notification-poll-route.ts"));
  write(root, `prisma/migrations/${migrationId}/migration.sql`, overlay("migration.sql"));

  const convert = read(root, "lib/actions/convert.ts");
  if (!convert.includes("GAME_POINT_TO_USDT_REQUIRES_GAME_DEBIT")
      || !convert.includes("gamePointConversion.create")
      || !convert.includes("rateVersionId")
      || !convert.includes('status: authority.mode === "game" ? "PENDING" : "SUCCESS"')) {
    fail("Patched conversion is missing authority invariants");
  }
}

function main() {
  if (process.argv.length !== 4) {
    console.error("Usage: node apply-web-point-authority-patch.js <web-root> <migration-id>");
    process.exit(64);
  }
  const root = path.resolve(process.argv[2]);
  const migrationId = String(process.argv[3] || "");
  if (!/^\d{14}_game_point_authority$/.test(migrationId)) fail("Invalid migration id");
  patchWebRoot(root, migrationId);
  console.log("WEB_POINT_AUTHORITY_PATCH=success");
}

if (require.main === module) {
  try {
    main();
  } catch (error) {
    console.error(`WEB_POINT_AUTHORITY_PATCH=failed: ${error.message}`);
    process.exit(1);
  }
}

module.exports = {
  EXPECTED_HASHES,
  patchAdmin,
  patchConvert,
  patchPointConvertActions,
  patchQueries,
  patchSchema,
  patchWallet1Section,
  patchWalletPage,
  patchWebRoot,
};
