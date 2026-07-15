const assert = require("assert");
const fs = require("fs");
const path = require("path");
const {
  assertDisjointRoots,
  parseProductionDatabase,
  safeErrorMessage,
} = require("./test-web-point-sync-e2e-isolated");

function expectFailure(action, message) {
  let failed = false;
  try {
    action();
  } catch (error) {
    failed = true;
  }
  assert(failed, message);
}

const runId = "20260715123456_abcdef12";
const parsed = parseProductionDatabase(
  "postgresql://game_owner:not-a-real-secret@127.0.0.1:5432/ywonder_game?options=-c%20search_path%3Dpublic",
  runId,
  "game_owner"
);
assert.strictEqual(parsed.owner, "game_owner");
assert.strictEqual(parsed.productionName, "ywonder_game");
assert.strictEqual(parsed.temporaryName, `yw_point_e2e_${runId}`);
assert.strictEqual(new URL(parsed.isolatedUrl).pathname, `/yw_point_e2e_${runId}`);
assert.strictEqual(new URL(parsed.isolatedUrl).searchParams.has("options"), false);

expectFailure(
  () => parseProductionDatabase("postgresql://game_owner:x@db.example.test/ywonder_game", runId, "game_owner"),
  "Harness accepted a remote PostgreSQL host."
);
expectFailure(
  () => parseProductionDatabase("postgresql://game_owner:x@127.0.0.1/yw_point_e2e_prod", runId, "game_owner"),
  "Harness accepted a production database inside the E2E namespace."
);
expectFailure(
  () => parseProductionDatabase("postgresql://game_owner:x@127.0.0.1/ywonder_game", "unsafe-run-id", "game_owner"),
  "Harness accepted an unsafe run id."
);
const socketParsed = parseProductionDatabase(
  "postgresql:///ywonder_game?host=/var/run/postgresql",
  runId,
  "ywonder_game"
);
assert.strictEqual(socketParsed.owner, "ywonder_game");
assert.strictEqual(new URL(socketParsed.isolatedUrl).searchParams.get("host"), "/var/run/postgresql");
expectFailure(
  () => parseProductionDatabase("postgresql:///ywonder_game?host=/tmp/untrusted-pg", runId, "ywonder_game"),
  "Harness accepted an unapproved PostgreSQL Unix socket."
);
expectFailure(
  () => parseProductionDatabase("postgresql://other_role:x@127.0.0.1/ywonder_game", runId, "game_owner"),
  "Harness accepted a PostgreSQL owner that differs from the systemd service user."
);
expectFailure(
  () => assertDisjointRoots({ game: path.join("temp", "e2e"), web: path.join("temp", "e2e", "web") }),
  "Harness accepted nested stage roots."
);

const redacted = safeErrorMessage(
  new Error("failed postgresql://game_owner:super-secret@127.0.0.1:5432/ywonder_game")
);
assert(!redacted.includes("super-secret"), "Harness error leaked a PostgreSQL password.");

const runnerPath = path.join(__dirname, "run-web-point-sync-e2e-isolated.sh");
const harnessPath = path.join(__dirname, "test-web-point-sync-e2e-isolated.js");
const runner = fs.readFileSync(runnerPath, "utf8");
const harness = fs.readFileSync(harnessPath, "utf8");

assert(!/systemctl\s+(?:start|stop|restart|daemon-reload)\b/.test(runner),
  "Runner contains a production service mutation command.");
assert(runner.includes("resource_gate") && runner.includes("nice -n 10") && runner.includes("timeout --signal=TERM"),
  "Runner lost its resource or timeout guard.");
assert(runner.includes("flock -n 9") && runner.includes("/tmp/ywonder-web-point-e2e."),
  "Runner lost its single-run lock or isolated stage root.");
assert(runner.includes("data\\.json") && runner.includes("pem|key|p12|pfx"),
  "Runner no longer rejects player-data or credential files in the game archive.");
assert(runner.includes('E2E_RUN_ID="${run_id}"') && runner.includes("database.name")
    && runner.includes("dropdb --if-exists --force"),
  "Runner lost its marked-process or fallback database cleanup.");
assert(!harness.includes("search_path="), "Harness still uses a production-database schema fallback.");
assert(harness.includes("createTemporaryGameDatabase") && harness.includes("dropTemporaryGameDatabase"),
  "Harness no longer owns a dedicated temporary database lifecycle.");

const noLeakCheck = harness.indexOf('enterStage("verify-no-production-data-leak")');
const noMutationClaim = harness.indexOf('console.log("PRODUCTION_PLAYER_DATA_MUTATED=no")');
assert(noLeakCheck >= 0 && noMutationClaim > noLeakCheck,
  "Harness claims production safety before running explicit leak checks.");

console.log("[web-point-e2e-harness] PASS: isolation boundaries, cleanup, resource guards and claim ordering are intact.");
