const fs = require("fs");
const path = require("path");
const {
  EXPECTED_HASHES,
  patchSchema,
} = require("./web-point-authority/apply-web-point-authority-patch");

const root = path.join(__dirname, "web-point-authority");

function read(name) {
  return fs.readFileSync(path.join(root, name), "utf8");
}

function assert(condition, message) {
  if (!condition) throw new Error(message);
}

function requireText(source, value, message) {
  const index = source.indexOf(value);
  assert(index >= 0, message);
  return index;
}

function requireBefore(source, first, second, message) {
  assert(requireText(source, first, `Missing ${first}`) < requireText(source, second, `Missing ${second}`), message);
}

const migration = read("migration.sql");
const authority = read("game-point-authority.ts");
const pointRate = read("point-rate.ts");
const browserIntent = read("game-point-conversion-intent.ts");
const conversion = read("convert-usdt-to-point-action.tsfrag");
const dispatcher = read("game-point-sync.ts");
const creditRoute = read("game-credit-route.ts");
const balanceRoute = read("game-balance-route.ts");
const runtimeE2E = read("web-point-authority-runtime-e2e.ts");
const pointUi = fs.readFileSync(path.join(root, "apply-web-point-authority-patch.js"), "utf8");
const validator = fs.readFileSync(path.join(__dirname, "validate-web-point-authority-candidate.sh"), "utf8");

assert(EXPECTED_HASHES.size === 12, "Web authority patch does not pin all production source files.");
requireText(migration, "GamePointLinkedAccount_require_zero_wallet", "Zero-balance link gate is missing.");
requireText(migration, "GamePointLinkedAccount_gamePlayerId_key", "Game player identity is not unique.");
requireText(migration, "Wallet_freeze_linked_point_update", "Linked Point freeze trigger is missing.");
requireText(migration, "GamePointConversion_one_unresolved_per_user", "Single unresolved conversion index is missing.");
requireText(migration, "where \"status\" not in ('SENT', 'REFUNDED')", "Resolved conversion states are not explicit.");
requireText(migration, 'create table "PointExchangeRateVersion"', "Immutable Point rate versions are missing.");
requireText(migration, "PointExchangeRateVersion_one_active_pair", "Multiple active Point rates are not prevented.");
requireText(migration, '"rateVersionId" text not null', "Conversion journal does not pin a rate version.");

requireText(pointRate, "decimalRateToMicrosText", "Admin rate is not normalized to exact micros.");
requireText(pointRate, "quoteUsdtToPointMicros", "Exact dynamic Point quote is missing.");
requireText(pointRate, "const raw = usdtMicros * rateMicros", "Point quote does not use integer arithmetic.");
requireText(pointRate, "roundingRemainder", "Point quote does not retain its rounding remainder.");
requireText(pointRate, "replaceActiveUsdtPointRate", "Admin rate version replacement is missing.");

requireText(authority, "GAME_POINT_SYNC_MUST_USE_LOOPBACK", "Balance read is not loopback-only.");
requireText(authority, "ywonder-point-balance-v1", "Balance read does not use a domain-separated signature.");
requireText(authority, "GAME_POINT_LINK_REQUIRED", "Allowed but unlinked accounts are not blocked.");
requireText(authority, "body.player_id !== linked.gamePlayerId", "Balance read does not enforce the pinned game player.");
requireText(authority, "GAME_POINT_IDENTITY_MISMATCH", "Balance identity mismatch lacks a stable error code.");
requireText(authority, "INVALID_GAME_POINT_CONVERSION_REQUEST_ID", "Browser conversion request IDs are not validated.");
requireText(authority, "normalizeUsdtMicros", "USDT conversion amount normalizer is missing.");
requireText(conversion, "usdtMicros", "Exact USDT micros are not written to the conversion journal.");
requireText(conversion, "getActiveUsdtPointRate", "Conversion does not read the Admin-controlled rate.");
requireText(conversion, "rateVersionId", "Conversion does not snapshot the rate version.");
requireText(conversion, "roundingRemainder", "Conversion does not snapshot exact rounding.");
requireBefore(
  conversion,
  "const existing = await txdb.transaction.findUnique",
  "const activeRate = await getActiveUsdtPointRate",
  "Conversion reads a new rate before checking an idempotent replay."
);

requireText(conversion, "gamePointConversionTransactionId", "Conversion transaction ID is not deterministic.");
requireText(conversion, "gamePointConversion.findUnique", "Conversion retry does not load the existing journal.");
requireText(conversion, "GAME_POINT_CONVERSION_ALREADY_PENDING", "Concurrent pending conversion guard is missing.");
requireText(conversion, 'status: authority.mode === "game" ? "PENDING" : "SUCCESS"', "Game conversion is marked successful before delivery.");
requireText(conversion, '? { balanceUsdt: { decrement: normalizedUsdt.amount } }', "Game-authoritative debit branch is missing.");
requireText(conversion, 'balanceGXL: { increment: point }', "Legacy compatibility branch was accidentally removed.");
requireBefore(
  conversion,
  'status: authority.mode === "game" ? "PENDING" : "SUCCESS"',
  "gamePointSyncOutbox.create",
  "Outbox is created before its pending transaction journal."
);

requireText(dispatcher, "gamePointLinkedAccount.findUnique", "Dispatcher does not require a linked account.");
requireText(dispatcher, "ywonder-point-credit-v2", "Dispatcher does not use the identity-pinned signature contract.");
requireText(dispatcher, "expected_player_id: input.expectedPlayerId", "Dispatcher omits the pinned game player.");
requireText(dispatcher, "body?.player_id !== input.expectedPlayerId", "Dispatcher accepts a response for another player.");
requireText(dispatcher, "validateCreditResponse", "Dispatcher accepts an unverified game response.");
requireText(dispatcher, "INVALID_GAME_POINT_SYNC_RESPONSE", "Dispatcher lacks a stable invalid-response code.");
requireText(dispatcher, 'status: { in: ["PENDING", "RETRY"] }', "A failed callback can regress a settled outbox.");
requireText(dispatcher, "if (changed.count === 0) return;", "Failure settlement ignores a concurrent successful callback.");
requireText(dispatcher, "GAME_POINT_SETTLEMENT_JOURNAL_MISSING", "Successful callback does not require all journal rows.");
requireText(dispatcher, "tx.transaction.update", "Dispatcher does not settle the web transaction status.");
requireText(dispatcher, "getGamePointSyncHealth", "Outbox health counters are missing.");

requireBefore(
  creditRoute,
  "isGamePointLinkedAccount(user.id)",
  "db.$transaction",
  "Legacy game credit mutates a linked account before checking authority."
);
requireText(creditRoute, "GAME_POINT_LEDGER_IS_AUTHORITATIVE", "Legacy credit route lacks a stable rejection code.");
requireText(balanceRoute, "getGamePointWalletView", "Legacy balance route does not read the game authority.");
requireText(balanceRoute, 'pointSource: gamePoint.linked ? "game" : "legacy-web"', "Balance response does not declare its source.");

assert((browserIntent.match(/crypto\.randomUUID\(\)/g) || []).length === 1,
  "Durable browser intent must generate exactly one idempotency request ID.");
requireText(browserIntent, "window.localStorage.setItem", "Browser conversion request ID is not durable across reloads.");
requireText(browserIntent, "if (existing) return existing;", "Browser retry does not reuse the durable request ID.");
assert(!pointUi.includes("crypto.randomUUID()"), "A UI entry point bypasses the durable conversion intent.");
assert((pointUi.match(/getOrCreateGamePointConversionIntent/g) || []).length >= 3,
  "Both USDT-to-Point UI entry points must use the durable conversion intent.");
requireText(pointUi, "gamePointLinked && !gamePointAvailable", "Linked Point UI does not fail closed on balance-read outage.");
requireText(pointUi, '!gamePointLinked && xferOpen', "Linked Point UI still exposes legacy transfer.");
requireText(pointUi, "if (!data) return null;", "Wallet query can infer a response without the authority contract.");
requireText(pointUi, "if (!data.wallet || !gamePointAuthority.linked)", "Wallet query omits authority for a wallet-less user.");
requireText(pointUi, "replaceActiveUsdtPointRate(txdb, usdtToGxl, admin.id, sourceRate.id)",
  "Admin save action does not append a Point rate version atomically.");
requireText(pointUi, "previousPointRateVersionId", "Admin rate audit omits the previous version.");
requireText(pointUi, "pointPerUsdt={Number(pointRate.rateMicros) / 1_000_000}",
  "Wallet UI does not receive the active Admin Point rate.");

requireText(validator, "flock -n 9", "Candidate validator has no single-run lock.");
requireText(validator, "?mode=ro", "Candidate validator does not open production SQLite read-only.");
requireText(validator, "resource_gate", "Candidate validator has no VPS resource gate.");
requireText(validator, "Overlay must contain exactly thirteen files.", "Candidate validator does not pin the expanded overlay.");
requireText(validator, "WEB_TOPUP_MODE=open", "Runtime E2E does not isolate dynamic synthetic identities.");
requireText(runtimeE2E, "ISOLATED_POST_COMMIT_RESPONSE_LOSS", "Runtime E2E omits post-commit response loss.");
requireText(runtimeE2E, "ISOLATED_CONCURRENT_RESPONSE_LOSS", "Runtime E2E omits concurrent callback settlement.");
requireText(runtimeE2E, "isolated-runtime-rate-change", "Runtime E2E omits a rate change after conversion commit.");
requireText(runtimeE2E, "replacementQuote.pointAmount !== EXPECTED_POINT_AMOUNT",
  "Runtime E2E does not prove the committed quote survives an Admin rate change.");
requireText(runtimeE2E, "INVALID_GAME_POINT_SYNC_RESPONSE", "Runtime E2E omits malformed success responses.");
requireText(runtimeE2E, 'scenario = "identity-mismatch"', "Runtime E2E omits a wrong-player response.");
requireText(runtimeE2E, 'current.outbox?.status === "FAILED"', "Runtime E2E omits permanent conflict quarantine.");
requireText(validator, "nice -n 10 ionice -c 2 -n 7", "Candidate build is not low priority.");
requireText(validator, "Production SQLite schema changed during validation.", "Live schema integrity check is missing.");
requireText(validator, "PRODUCTION_SERVICES_RESTARTED=no", "Validator does not report service isolation.");
requireText(validator, "REAL_PAYMENT_USED=no", "Validator does not report payment isolation.");
assert(!/systemctl\s+(stop|start|restart)/.test(validator),
  "Candidate validator can change a production service state.");
assert(!validator.includes('DATABASE_URL="file:${live_db}"'),
  "Candidate validator can point Prisma at production SQLite.");

const schemaFixture = `model User {
  id String @id
  gamePointSyncOutbox GamePointSyncOutbox[]
}

model GamePointSyncOutbox {
  id String @id
}
`;
const patchedSchema = patchSchema(schemaFixture);
requireText(patchedSchema, "gamePointLink GamePointLinkedAccount?", "Schema patch omitted the linked-account relation.");
requireText(patchedSchema, "gamePlayerId String   @unique", "Schema patch omitted the unique game-player pin.");
requireText(patchedSchema, "model GamePointConversion", "Schema patch omitted the conversion journal.");
requireText(patchedSchema, "model PointExchangeRateVersion", "Schema patch omitted immutable Point rates.");
requireText(patchedSchema, "rateVersionId       String", "Schema patch omitted conversion rate pinning.");

const combined = [migration, authority, pointRate, browserIntent, conversion, dispatcher, creditRoute, balanceRoute].join("\n");
const nonLoopbackAddresses = (combined.match(/\b(?:\d{1,3}\.){3}\d{1,3}\b/g) || [])
  .filter((address) => address !== "127.0.0.1");
assert(nonLoopbackAddresses.length === 0,
  "A non-loopback host address leaked into the authority overlay.");
assert(!/[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}/i.test(combined),
  "An email identity leaked into the authority overlay.");
assert(!/\b(?:password|passwd)\b\s*[:=]\s*["'][^"']+["']/i.test(combined),
  "A password literal leaked into the authority overlay.");

console.log("[web-point-authority] PASS: source pins, single-ledger freeze, exact journal, idempotency, pending settlement, route guards, and UI request IDs are present.");
