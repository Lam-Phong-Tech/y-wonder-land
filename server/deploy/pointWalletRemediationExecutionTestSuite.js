"use strict";

const assert = require("assert");
const path = require("path");
const { spawnSync } = require("child_process");

const deployRoot = __dirname;

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
  throw new Error("PYTHON_3_RUNTIME_REQUIRED_FOR_REMEDIATION_EXECUTION_TEST");
}

run(
  process.execPath,
  [path.join(deployRoot, "pointWalletSyntheticReversalTest.js")],
  "synthetic reversal test"
);
const python = findPython();
run(
  python.command,
  [...python.prefix, path.join(deployRoot, "pointWalletResidualNormalizationTest.py")],
  "legacy residual normalization test"
);

console.log("[point-wallet-remediation-execution-suite] PASS: both approved operations are fail-closed and compensatable.");
