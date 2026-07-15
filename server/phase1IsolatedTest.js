const fs = require("fs");
const net = require("net");
const os = require("os");
const path = require("path");
const { spawn } = require("child_process");

async function reservePort() {
  return new Promise((resolve, reject) => {
    const server = net.createServer();
    server.once("error", reject);
    server.listen(0, "127.0.0.1", () => {
      const port = server.address().port;
      server.close((error) => error ? reject(error) : resolve(port));
    });
  });
}

async function waitForHealth(child, port, output) {
  const deadline = Date.now() + 15_000;
  while (Date.now() < deadline) {
    if (child.exitCode != null) {
      throw new Error(`Isolated backend exited (${child.exitCode}): ${output.text}`);
    }
    try {
      const response = await fetch(`http://127.0.0.1:${port}/health`);
      if (response.ok) return;
    } catch (error) {
      // Backend is still starting.
    }
    await new Promise((resolve) => setTimeout(resolve, 100));
  }
  throw new Error(`Isolated backend did not become healthy: ${output.text}`);
}

async function stopChild(child) {
  if (!child || child.exitCode != null) return;
  child.kill("SIGTERM");
  const deadline = Date.now() + 5000;
  while (child.exitCode == null && Date.now() < deadline) {
    await new Promise((resolve) => setTimeout(resolve, 50));
  }
  if (child.exitCode == null) child.kill("SIGKILL");
}

async function runPhase1(port) {
  return new Promise((resolve, reject) => {
    const child = spawn(process.execPath, ["phase1SmokeTest.js"], {
      cwd: __dirname,
      env: {
        ...process.env,
        PHASE1_TEST_BASE_URL: `http://127.0.0.1:${port}`,
      },
      stdio: "inherit",
    });
    child.once("error", reject);
    child.once("exit", (code, signal) => {
      if (code === 0) resolve();
      else reject(new Error(`Phase 1 smoke failed (code=${code}, signal=${signal || "none"}).`));
    });
  });
}

async function main() {
  const port = await reservePort();
  const tempDir = fs.mkdtempSync(path.join(os.tmpdir(), "yw-phase1-isolated-"));
  const dataPath = path.join(tempDir, "data.json");
  const output = { text: "" };
  let server;
  try {
    server = spawn(process.execPath, ["index.js"], {
      cwd: __dirname,
      env: {
        ...process.env,
        NODE_ENV: "test",
        HOST: "127.0.0.1",
        PORT: String(port),
        STORE_MODE: "json",
        YW_DATA_PATH: dataPath,
        JWT_SECRET: "phase1-isolated-test-jwt-secret-32-characters",
        WEB_AUTH_MODE: "mock",
        ADMIN_DASHBOARD_ENABLED: "false",
        DEMO_ACCOUNTS_ENABLED: "false",
        LOCAL_REGISTRATION_ENABLED: "true",
        RATE_LIMIT_ENABLED: "false",
        HTTP_ACCESS_LOG: "false",
      },
      stdio: ["ignore", "pipe", "pipe"],
    });
    server.stdout.on("data", (chunk) => { output.text += chunk.toString("utf8"); });
    server.stderr.on("data", (chunk) => { output.text += chunk.toString("utf8"); });
    await waitForHealth(server, port, output);
    await runPhase1(port);
    console.log("[phase1-isolated] PASS: temporary JSON backend started, tested, and preserved no local state.");
  } finally {
    await stopChild(server);
    const tempRoot = `${path.resolve(os.tmpdir())}${path.sep}`;
    const target = path.resolve(tempDir);
    if (!target.startsWith(tempRoot) || !path.basename(target).startsWith("yw-phase1-isolated-")) {
      throw new Error(`Refusing to remove unexpected test directory: ${target}`);
    }
    fs.rmSync(target, { recursive: true, force: true });
  }
}

main().catch((error) => {
  console.error(`[phase1-isolated] FAIL: ${error.message}`);
  process.exitCode = 1;
});
