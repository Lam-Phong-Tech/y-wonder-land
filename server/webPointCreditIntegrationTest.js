const fs = require("fs");
const net = require("net");
const os = require("os");
const path = require("path");
const { spawn } = require("child_process");
const WebSocket = require("ws");
const {
  buildWebPointCreditConfig,
  normalizeWebPointCreditBody,
  signWebPointCredit,
} = require("./webPointCredit");

const SECRET = "web-point-credit-integration-secret-32-plus";

function assert(condition, message) {
  if (!condition) throw new Error(message);
}

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

async function waitForServer(child, port, output) {
  const deadline = Date.now() + 15_000;
  while (Date.now() < deadline) {
    if (child.exitCode != null) {
      throw new Error(`Point-credit test server exited early (${child.exitCode}): ${output.text}`);
    }
    try {
      const response = await fetch(`http://127.0.0.1:${port}/health`);
      if (response.ok) return;
    } catch (error) {
      // Server is still starting.
    }
    await new Promise((resolve) => setTimeout(resolve, 100));
  }
  throw new Error(`Point-credit test server did not become ready: ${output.text}`);
}

async function startServer(dataPath) {
  const port = await reservePort();
  const output = { text: "" };
  const child = spawn(process.execPath, ["index.js"], {
    cwd: __dirname,
    env: {
      ...process.env,
      NODE_ENV: "test",
      HOST: "127.0.0.1",
      PORT: String(port),
      STORE_MODE: "json",
      YW_DATA_PATH: dataPath,
      JWT_SECRET: "point-credit-test-jwt-secret-with-32-plus-characters",
      WEB_AUTH_MODE: "mock",
      WEB_TOPUP_ENABLED: "true",
      WEB_TOPUP_SECRET: SECRET,
      WEB_TOPUP_MAX_POINTS: "1000000",
      WEB_TOPUP_MODE: "canary",
      WEB_TOPUP_ALLOWED_WEB_USER_IDS: "mock:point.tester@example.test",
      ADMIN_DASHBOARD_ENABLED: "false",
      DEMO_ACCOUNTS_ENABLED: "false",
      HTTP_ACCESS_LOG: "false",
      RATE_LIMIT_ENABLED: "false",
    },
    stdio: ["ignore", "pipe", "pipe"],
  });
  child.stdout.on("data", (chunk) => { output.text += chunk.toString("utf8"); });
  child.stderr.on("data", (chunk) => { output.text += chunk.toString("utf8"); });
  await waitForServer(child, port, output);
  return { child, baseUrl: `http://127.0.0.1:${port}`, output };
}

async function stopServer(server) {
  if (server.child.exitCode == null) server.child.kill("SIGTERM");
  const deadline = Date.now() + 5000;
  while (server.child.exitCode == null && Date.now() < deadline) {
    await new Promise((resolve) => setTimeout(resolve, 50));
  }
  if (server.child.exitCode == null) server.child.kill("SIGKILL");
}

function makeCredit(overrides = {}) {
  return {
    transaction_id: "web-order-test-001",
    web_user_id: "mock:point.tester@example.test",
    point_amount: "750.500000",
    occurred_at: "2026-07-15T00:00:00.000Z",
    source: "ywonder-web",
    username: "point.tester@example.test",
    display_name: "Point Tester",
    ...overrides,
  };
}

function signedHeaders(body, timestamp = Math.floor(Date.now() / 1000), secret = SECRET) {
  const normalized = normalizeWebPointCreditBody(body, 1_000_000);
  return {
    "Content-Type": "application/json",
    "X-YWonder-Timestamp": String(timestamp),
    "X-YWonder-Signature": signWebPointCredit(secret, String(timestamp), normalized),
  };
}

async function post(baseUrl, route, body, headers) {
  const response = await fetch(`${baseUrl}${route}`, {
    method: "POST",
    headers: headers || { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  let payload = null;
  try { payload = await response.json(); } catch (error) { payload = null; }
  return { status: response.status, payload };
}

async function connectRealtime(baseUrl, token) {
  const wsUrl = `${baseUrl.replace(/^http/i, "ws")}/realtime?token=${encodeURIComponent(token)}`;
  const socket = new WebSocket(wsUrl);
  await new Promise((resolve, reject) => {
    const timer = setTimeout(() => reject(new Error("Realtime connection timed out.")), 5000);
    const fail = (error) => {
      clearTimeout(timer);
      reject(error);
    };
    socket.once("error", fail);
    socket.on("message", (raw) => {
      let message;
      try { message = JSON.parse(raw.toString("utf8")); } catch (error) { return; }
      if (message.type !== "connected") return;
      clearTimeout(timer);
      socket.off("error", fail);
      resolve();
    });
  });
  return socket;
}

function waitForMessage(socket, predicate, label) {
  return new Promise((resolve, reject) => {
    const timer = setTimeout(() => {
      socket.off("message", onMessage);
      reject(new Error(`Timed out waiting for ${label}.`));
    }, 5000);
    const onMessage = (raw) => {
      let message;
      try { message = JSON.parse(raw.toString("utf8")); } catch (error) { return; }
      if (!predicate(message)) return;
      clearTimeout(timer);
      socket.off("message", onMessage);
      resolve(message);
    };
    socket.on("message", onMessage);
  });
}

async function main() {
  const dedicatedSecretConfig = buildWebPointCreditConfig({
    WEB_TOPUP_ENABLED: "true",
    GAME_API_SECRET: SECRET,
  });
  assert(dedicatedSecretConfig.secret === "",
    "Point credit reused GAME_API_SECRET instead of requiring WEB_TOPUP_SECRET.");

  const integerCompatibility = normalizeWebPointCreditBody(makeCredit({ point_amount: 2 }), 1_000_000);
  assert(integerCompatibility.pointAmount === "2.000000"
      && integerCompatibility.pointAmountMicros === 2_000_000,
    "Integer Point payload was not normalized to the exact micro-Point contract.");

  const tempDir = fs.mkdtempSync(path.join(os.tmpdir(), "yw-point-credit-"));
  const dataPath = path.join(tempDir, "data.json");
  let server = null;
  let socket = null;
  try {
    server = await startServer(dataPath);
    const body = makeCredit();

    const login = await post(server.baseUrl, "/auth/web-login", {
      username: body.username,
      password: "mock-password",
    });
    assert(login.status === 200 && login.payload.token, "Mock web account login failed.");
    socket = await connectRealtime(server.baseUrl, login.payload.token);

    const publicProbe = await post(
      server.baseUrl,
      "/game-api/internal/web/point-credit",
      body,
      signedHeaders(body)
    );
    assert(publicProbe.status === 404, "Internal Point-credit route leaked under /game-api.");

    const unsigned = await post(server.baseUrl, "/internal/web/point-credit", body);
    assert(unsigned.status === 401 && unsigned.payload.error === "INVALID_WEB_TOPUP_SIGNATURE",
      "Unsigned Point credit was not rejected.");

    const staleTimestamp = Math.floor(Date.now() / 1000) - 3600;
    const stale = await post(
      server.baseUrl,
      "/internal/web/point-credit",
      body,
      signedHeaders(body, staleTimestamp)
    );
    assert(stale.status === 401 && stale.payload.error === "WEB_TOPUP_REQUEST_EXPIRED",
      "Expired Point credit was not rejected.");

    const millisecondTimestamp = Date.now();
    const milliseconds = await post(
      server.baseUrl,
      "/internal/web/point-credit",
      body,
      signedHeaders(body, millisecondTimestamp)
    );
    assert(milliseconds.status === 401 && milliseconds.payload.error === "INVALID_WEB_TOPUP_SIGNATURE",
      "Millisecond timestamp was accepted even though the contract requires Unix seconds.");

    const wrongSecret = await post(
      server.baseUrl,
      "/internal/web/point-credit",
      body,
      signedHeaders(body, Math.floor(Date.now() / 1000), `${SECRET}-wrong`)
    );
    assert(wrongSecret.status === 401 && wrongSecret.payload.error === "INVALID_WEB_TOPUP_SIGNATURE",
      "Invalid Point-credit signature was not rejected.");

    const blockedCanary = makeCredit({
      transaction_id: "web-order-not-allowed",
      web_user_id: "mock:not-allowed@example.test",
      username: "not-allowed@example.test",
    });
    const blocked = await post(
      server.baseUrl,
      "/internal/web/point-credit",
      blockedCanary,
      signedHeaders(blockedCanary)
    );
    assert(blocked.status === 425 && blocked.payload.error === "WEB_TOPUP_CANARY_USER_NOT_ALLOWED",
      "Point-credit canary accepted a web user outside the allowlist.");

    const invalidAmount = makeCredit({ transaction_id: "web-order-invalid", point_amount: -1 });
    const invalid = await post(server.baseUrl, "/internal/web/point-credit", invalidAmount, {
      "Content-Type": "application/json",
      "X-YWonder-Timestamp": String(Math.floor(Date.now() / 1000)),
      "X-YWonder-Signature": "0".repeat(64),
    });
    assert(invalid.status === 400 && invalid.payload.error === "INVALID_POINT_AMOUNT",
      "Invalid Point amount was not rejected.");

    const tooPreciseAmount = makeCredit({
      transaction_id: "web-order-too-precise",
      point_amount: "1.0000001",
    });
    const tooPrecise = await post(server.baseUrl, "/internal/web/point-credit", tooPreciseAmount, {
      "Content-Type": "application/json",
      "X-YWonder-Timestamp": String(Math.floor(Date.now() / 1000)),
      "X-YWonder-Signature": "0".repeat(64),
    });
    assert(tooPrecise.status === 400 && tooPrecise.payload.error === "INVALID_POINT_AMOUNT",
      "Point amount with more than six decimals was not rejected.");

    const realtimeUpdate = waitForMessage(
      socket,
      (message) => message.type === "economy_updated" && message.reason === "web_topup",
      "realtime Point balance update"
    );
    const first = await post(server.baseUrl, "/internal/web/point-credit", body, signedHeaders(body));
    assert(first.status === 200 && first.payload.ok === true && first.payload.duplicate === false,
      "Valid Point credit failed.");
    assert(Number(first.payload.economy.pos) === 5750,
      "Whole Point portion of the first decimal credit was wrong.");
    const pushed = await realtimeUpdate;
    assert(Number(pushed.economy && pushed.economy.pos) === 5750,
      "Online player did not receive the authoritative Point balance update.");

    const duplicate = await post(server.baseUrl, "/internal/web/point-credit", body, signedHeaders(body));
    assert(duplicate.status === 200 && duplicate.payload.duplicate === true,
      "Retry was not recognized as an idempotent duplicate.");
    assert(Number(duplicate.payload.economy.pos) === 5750, "Duplicate Point credit was applied twice.");

    const conflictBody = makeCredit({ point_amount: "751.000000" });
    const conflict = await post(
      server.baseUrl,
      "/internal/web/point-credit",
      conflictBody,
      signedHeaders(conflictBody)
    );
    assert(conflict.status === 409 && conflict.payload.error === "IDEMPOTENCY_CONFLICT",
      "Changed payload with the same transaction ID was not rejected.");

    const carryBody = makeCredit({
      transaction_id: "web-order-test-002",
      point_amount: "0.500000",
    });
    const carry = await post(
      server.baseUrl,
      "/internal/web/point-credit",
      carryBody,
      signedHeaders(carryBody)
    );
    assert(carry.status === 200 && Number(carry.payload.economy.pos) === 5751,
      "Fractional Point remainder was not carried into the next credit.");

    const integerBody = makeCredit({
      transaction_id: "web-order-test-003",
      point_amount: 2,
    });
    const integerCredit = await post(
      server.baseUrl,
      "/internal/web/point-credit",
      integerBody,
      signedHeaders(integerBody)
    );
    assert(integerCredit.status === 200 && Number(integerCredit.payload.economy.pos) === 5753,
      "Integer Point payload stopped working after decimal support was added.");

    socket.close();
    socket = null;
    await stopServer(server);
    server = await startServer(dataPath);
    const afterRestart = await post(
      server.baseUrl,
      "/internal/web/point-credit",
      integerBody,
      signedHeaders(integerBody)
    );
    assert(afterRestart.status === 200 && afterRestart.payload.duplicate === true,
      "Point-credit idempotency did not survive a backend restart.");
    assert(Number(afterRestart.payload.economy.pos) === 5753,
      "Point balance changed after backend restart/retry.");

    await stopServer(server);
    server = null;
    const db = JSON.parse(fs.readFileSync(dataPath, "utf8"));
    const topups = (db.transactions || []).filter((tx) => tx.type === "web_topup_credit");
    assert(topups.length === 3, `Expected three persisted web top-up transactions, found ${topups.length}.`);
    const playerId = topups[0].playerId;
    assert(Number((db.webTopupPointMicrosRemainders || {})[playerId]) === 0,
      "Fractional Point remainder was not persisted and settled exactly.");
    assert(!JSON.stringify(db.economies || {}).toLowerCase().includes("upos"),
      "Retired UPoint data was written back into active economy storage.");

    console.log("[web-point-credit] PASS: signed decimal Point, canary allowlist, micro remainder carry, integer compatibility, realtime refresh, idempotency, and restart persistence work.");
  } finally {
    if (socket) socket.close();
    if (server) await stopServer(server);
    const resolvedTempRoot = `${path.resolve(os.tmpdir())}${path.sep}`;
    const resolvedTarget = path.resolve(tempDir);
    if (!resolvedTarget.startsWith(resolvedTempRoot) || !path.basename(resolvedTarget).startsWith("yw-point-credit-")) {
      throw new Error(`Refusing to remove unexpected test directory: ${resolvedTarget}`);
    }
    fs.rmSync(resolvedTarget, { recursive: true, force: true });
  }
}

main().catch((error) => {
  console.error(`[web-point-credit] FAIL: ${error.message}`);
  process.exitCode = 1;
});
