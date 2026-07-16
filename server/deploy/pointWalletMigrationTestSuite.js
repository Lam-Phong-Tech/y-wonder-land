"use strict";

const assert = require("assert");
const fs = require("fs");
const path = require("path");
const { spawnSync } = require("child_process");

const deployRoot = __dirname;
const runnerSource = fs.readFileSync(
  path.join(deployRoot, "run-point-wallet-migration-dry-run.sh"),
  "utf8"
);

assert(runnerSource.includes('POINT_MIGRATION_GAME_EXPORT_USER'),
  "Runner does not support PostgreSQL peer-auth service users.");
assert(runnerSource.includes('runuser -u "${game_export_user}" --preserve-environment'),
  "Runner does not drop only the PostgreSQL exporter to the service user.");
assert(runnerSource.includes("env -u POINT_MIGRATION_REPORT_KEY"),
  "Runner leaks the report HMAC key into the service-user exporter.");

function run(command, args, label) {
  const result = spawnSync(command, args, {
    cwd: deployRoot,
    encoding: "utf8",
    env: process.env,
  });
  if (result.stdout) process.stdout.write(result.stdout);
  if (result.stderr) process.stderr.write(result.stderr);
  assert.strictEqual(result.status, 0, `${label} failed with exit code ${result.status}`);
}

function findPython() {
  const configured = String(process.env.PYTHON_BIN || "").trim();
  if (configured) {
    const result = spawnSync(configured, ["--version"], { encoding: "utf8" });
    if (result.status !== 0) throw new Error("PYTHON_BIN_IS_NOT_A_WORKING_PYTHON_3_RUNTIME");
    return { command: configured, prefix: [] };
  }
  const candidates = process.platform === "win32"
    ? [["python", []], ["py", ["-3"]], ["python3", []]]
    : [["python3", []], ["python", []]];
  for (const [command, prefix] of candidates) {
    const result = spawnSync(command, [...prefix, "--version"], { encoding: "utf8" });
    if (result.status === 0) return { command, prefix };
  }
  throw new Error("PYTHON_3_RUNTIME_REQUIRED_FOR_SQLITE_EXPORT_TEST");
}

run(process.execPath, [path.join(deployRoot, "pointWalletMigrationReportTest.js")], "report test");
run(process.execPath, [path.join(deployRoot, "exportGamePointMigrationSnapshotTest.js")], "game export test");
const python = findPython();
run(
  python.command,
  [...python.prefix, path.join(deployRoot, "exportWebPointMigrationSnapshotTest.py")],
  "web export test"
);

console.log("[point-wallet-migration-suite] PASS: report and both read-only exporters are pinned.");
