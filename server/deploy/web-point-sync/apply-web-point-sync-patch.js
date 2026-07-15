const fs = require("fs");
const path = require("path");

const webRoot = path.resolve(process.argv[2] || "");
const migrationId = String(process.argv[3] || "").trim();
if (!webRoot || !/^\d{14}_game_point_sync_outbox$/.test(migrationId)) {
  throw new Error("Usage: node apply-web-point-sync-patch.js <web-root> <YYYYMMDDHHMMSS_game_point_sync_outbox>");
}

function read(relativePath) {
  return fs.readFileSync(path.join(webRoot, relativePath), "utf8").replace(/\r\n/g, "\n");
}

function write(relativePath, content) {
  const target = path.join(webRoot, relativePath);
  fs.mkdirSync(path.dirname(target), { recursive: true });
  fs.writeFileSync(target, content.endsWith("\n") ? content : `${content}\n`, "utf8");
}

function copyOverlay(name, target) {
  write(target, fs.readFileSync(path.join(__dirname, name), "utf8").replace(/\r\n/g, "\n"));
}

let schema = read("prisma/schema.prisma");
if (schema.includes("model GamePointSyncOutbox")) throw new Error("Prisma outbox model already exists.");
const userRelation = /^([ \t]*transactions[ \t]+Transaction\[\][ \t]*)$/m;
if (!userRelation.test(schema)) throw new Error("Cannot find User.transactions relation anchor.");
schema = schema.replace(userRelation, "$1\n  gamePointSyncOutbox GamePointSyncOutbox[]");
schema += `

model GamePointSyncOutbox {
  id                  String   @id @default(cuid())
  sourceTransactionId String   @unique
  userId              String
  user                User     @relation(fields: [userId], references: [id], onDelete: Cascade)
  pointAmount         String
  occurredAt          DateTime
  source              String   @default("ywonder-web-usdt-to-point")
  status              String   @default("PENDING")
  attempts            Int      @default(0)
  lastError           String?
  nextAttemptAt       DateTime @default(now())
  sentAt              DateTime?
  createdAt           DateTime @default(now())
  updatedAt           DateTime @updatedAt

  @@index([status, nextAttemptAt])
  @@index([userId, createdAt])
}
`;
write("prisma/schema.prisma", schema);

let convert = read("lib/actions/convert.ts");
if (convert.includes("gamePointSyncOutbox")) throw new Error("USDT to Point action is already patched.");
const importAnchor = 'import { db } from "@/lib/db";';
if (!convert.includes(importAnchor)) throw new Error("Cannot find convert action import anchor.");
convert = convert.replace(
  importAnchor,
  `${importAnchor}\nimport {\n  dispatchGamePointSyncOutboxById,\n  GAME_POINT_SYNC_SOURCE,\n  pointToGameAmountText,\n} from "@/lib/game-point-sync";`
);

const functionStart = convert.indexOf("export async function convertUsdtToPointAction");
const functionEnd = convert.indexOf("// ---- USDT", functionStart);
if (functionStart < 0 || functionEnd < 0) throw new Error("Cannot isolate USDT to Point action.");
let action = convert.slice(functionStart, functionEnd);
const pointAnchor = "    const point = usdtToPoint(usdt);";
if (!action.includes(pointAnchor)) throw new Error("Cannot find Point conversion anchor.");
action = action.replace(
  pointAnchor,
  `${pointAnchor}\n    const gamePointAmount = pointToGameAmountText(point);\n    let gamePointOutboxId: string | undefined;`
);

const transactionAnchor = `        await txdb.transaction.create({
          data: {
            userId,
            type: "SWAP",
            amount: point,
            currency: "GXL",
            status: "SUCCESS",
            metadata: JSON.stringify({ from: "USDT", to: "POINT", usdt, point, priceUsdt: POINT_PRICE_USDT }),
          },
        });`;
if (!action.includes(transactionAnchor)) throw new Error("Cannot find USDT to Point transaction anchor.");
action = action.replace(transactionAnchor, `        const swapTransaction = await txdb.transaction.create({
          data: {
            userId,
            type: "SWAP",
            amount: point,
            currency: "GXL",
            status: "SUCCESS",
            metadata: JSON.stringify({ from: "USDT", to: "POINT", usdt, point, priceUsdt: POINT_PRICE_USDT }),
          },
        });
        const outbox = await txdb.gamePointSyncOutbox.create({
          data: {
            sourceTransactionId: swapTransaction.id,
            userId,
            pointAmount: gamePointAmount,
            occurredAt: swapTransaction.createdAt,
            source: GAME_POINT_SYNC_SOURCE,
          },
        });
        gamePointOutboxId = outbox.id;`);

const revalidateAnchor = '    revalidatePath("/wallet");';
if (!action.includes(revalidateAnchor)) throw new Error("Cannot find post-conversion dispatch anchor.");
action = action.replace(revalidateAnchor, `    if (gamePointOutboxId) {
      try {
        await dispatchGamePointSyncOutboxById(gamePointOutboxId);
      } catch {
        // The durable outbox remains pending and the authenticated cron retries it.
        console.error(JSON.stringify({ event: "game_point_sync_dispatch_deferred" }));
      }
    }

${revalidateAnchor}`);
convert = `${convert.slice(0, functionStart)}${action}${convert.slice(functionEnd)}`;
write("lib/actions/convert.ts", convert);

copyOverlay("game-point-sync.ts", "lib/game-point-sync.ts");
copyOverlay("cron-route.ts", "app/api/cron/game-point-sync/route.ts");
copyOverlay("migration.sql", `prisma/migrations/${migrationId}/migration.sql`);

let crontab = read("deploy/crontab.txt");
if (!crontab.includes("/api/cron/game-point-sync")) {
  crontab += `

# Retry durable web -> game Point outbox every minute
* * * * * curl -sS -X POST -H "$HEADER" http://127.0.0.1:3033/api/cron/game-point-sync >> /var/log/greenxland/cron-game-point-sync.log 2>&1
`;
}
write("deploy/crontab.txt", crontab);

console.log("WEB_POINT_SYNC_PATCH=success");
