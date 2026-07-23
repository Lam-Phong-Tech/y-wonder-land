const fs = require("fs");
const net = require("net");
const os = require("os");
const path = require("path");
const { spawn } = require("child_process");
const {
  buildWebPointCreditConfig,
  normalizeWebPointBalanceBody,
  normalizeWebPointReservationBody,
  signWebPointBalance,
  signWebPointReservation,
} = require("./webPointCredit");

const SECRET = "web-point-reservation-test-secret-32-plus";
const WEB_USER_ID = "mock:point.wallet@example.test";

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
      throw new Error(`Reservation test server exited early (${child.exitCode}): ${output.text}`);
    }
    try {
      const response = await fetch(`http://127.0.0.1:${port}/health`);
      if (response.ok) return;
    } catch {
      // Server is still starting.
    }
    await new Promise((resolve) => setTimeout(resolve, 100));
  }
  throw new Error(`Reservation test server did not become ready: ${output.text}`);
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
      JWT_SECRET: "point-reservation-jwt-secret-with-32-plus-characters",
      WEB_AUTH_MODE: "mock",
      WEB_TOPUP_ENABLED: "true",
      WEB_POINT_WALLET_DEBIT_ENABLED: "true",
      WEB_TOPUP_SECRET: SECRET,
      WEB_TOPUP_MAX_POINTS: "1000000",
      WEB_TOPUP_MODE: "canary",
      WEB_TOPUP_ALLOWED_WEB_USER_IDS: WEB_USER_ID,
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
  if (!server || server.child.exitCode != null) return;
  server.child.kill("SIGTERM");
  const deadline = Date.now() + 5000;
  while (server.child.exitCode == null && Date.now() < deadline) {
    await new Promise((resolve) => setTimeout(resolve, 50));
  }
  if (server.child.exitCode == null) server.child.kill("SIGKILL");
}

async function post(baseUrl, route, body, headers) {
  const response = await fetch(`${baseUrl}${route}`, {
    method: "POST",
    headers: headers || { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  let payload = null;
  try { payload = await response.json(); } catch { payload = null; }
  return { status: response.status, payload };
}

function makeReservation(playerId, reservationId, amount, overrides = {}) {
  return {
    reservation_id: reservationId,
    web_user_id: WEB_USER_ID,
    expected_player_id: playerId,
    point_amount: amount,
    purpose: "point_to_usdt",
    source: "ywonder-web",
    occurred_at: "2026-07-16T00:00:00.000Z",
    ...overrides,
  };
}

function signedReservationHeaders(operation, body, timestamp = Math.floor(Date.now() / 1000)) {
  const normalized = normalizeWebPointReservationBody(body, 1_000_000);
  return {
    "Content-Type": "application/json",
    "X-YWonder-Timestamp": String(timestamp),
    "X-YWonder-Signature": signWebPointReservation(
      SECRET,
      String(timestamp),
      operation,
      normalized
    ),
  };
}

function signedBalanceHeaders(body) {
  const timestamp = String(Math.floor(Date.now() / 1000));
  const normalized = normalizeWebPointBalanceBody(body);
  return {
    "Content-Type": "application/json",
    "X-YWonder-Timestamp": timestamp,
    "X-YWonder-Signature": signWebPointBalance(SECRET, timestamp, normalized),
  };
}

async function balance(baseUrl, requestId) {
  const body = { request_id: requestId, web_user_id: WEB_USER_ID };
  return post(baseUrl, "/internal/web/point-balance", body, signedBalanceHeaders(body));
}

async function command(baseUrl, operation, body) {
  return post(
    baseUrl,
    `/internal/web/point-${operation}`,
    body,
    signedReservationHeaders(operation, body)
  );
}

async function main() {
  const dormant = buildWebPointCreditConfig({ WEB_TOPUP_ENABLED: "true" });
  assert(dormant.debitEnabled === false, "Point debit routes are not dormant by default.");
  assert(normalizeWebPointReservationBody({
    reservation_id: "normalizer-1",
    web_user_id: WEB_USER_ID,
    expected_player_id: "player-1",
    point_amount: "2.000000",
    purpose: "point_to_usdt",
    source: "ywonder-web",
    occurred_at: "2026-07-16T00:00:00.000Z",
  }, 100).pointAmount === 2, "Whole Point reservation normalization failed.");
  let fractionalRejected = false;
  try {
    normalizeWebPointReservationBody({
      reservation_id: "normalizer-2",
      web_user_id: WEB_USER_ID,
      expected_player_id: "player-1",
      point_amount: "1.000001",
      purpose: "point_to_usdt",
      source: "ywonder-web",
      occurred_at: "2026-07-16T00:00:00.000Z",
    }, 100);
  } catch (error) {
    fractionalRejected = error.message === "POINT_RESERVATION_REQUIRES_WHOLE_POINT";
  }
  assert(fractionalRejected, "Fractional Point reservation was accepted.");

  const tempDir = fs.mkdtempSync(path.join(os.tmpdir(), "yw-point-reservation-"));
  const dataPath = path.join(tempDir, "data.json");
  let server = null;
  try {
    server = await startServer(dataPath);
    const login = await post(server.baseUrl, "/auth/web-login", {
      username: "point.wallet@example.test",
      password: "mock-password",
    });
    assert(login.status === 200, "Mock web account login failed.");

    const initial = await balance(server.baseUrl, "wallet-balance-initial");
    assert(initial.status === 200 && initial.payload.point === 5000, "Initial Point balance is wrong.");
    const playerId = initial.payload.player_id;

    const first = makeReservation(playerId, "wallet-reserve-release-001", 100);
    const unsigned = await post(server.baseUrl, "/internal/web/point-reserve", first);
    assert(unsigned.status === 401, "Unsigned reservation was accepted.");
    const wrongPlayer = makeReservation("player-wrong", "wallet-reserve-wrong-player", 100);
    const wrongPlayerResult = await command(server.baseUrl, "reserve", wrongPlayer);
    assert(wrongPlayerResult.status === 409
      && wrongPlayerResult.payload.error === "GAME_POINT_IDENTITY_MISMATCH",
    "Reservation was not pinned to the mapped player.");

    const reserved = await command(server.baseUrl, "reserve", first);
    assert(reserved.status === 200 && reserved.payload.economy.pos === 4900
      && reserved.payload.reservation.status === "RESERVED" && !reserved.payload.duplicate,
    "Point was not reserved exactly once.");
    const reserveReplay = await command(server.baseUrl, "reserve", first);
    assert(reserveReplay.status === 200 && reserveReplay.payload.duplicate
      && reserveReplay.payload.economy.pos === 4900,
    "Reservation replay debited Point twice.");

    const conflict = makeReservation(playerId, first.reservation_id, 101);
    const conflictResult = await command(server.baseUrl, "reserve", conflict);
    assert(conflictResult.status === 409 && conflictResult.payload.error === "IDEMPOTENCY_CONFLICT",
      "Same reservation ID with a different amount was accepted.");

    const released = await command(server.baseUrl, "release", first);
    assert(released.status === 200 && released.payload.economy.pos === 5000
      && released.payload.reservation.status === "RELEASED" && !released.payload.duplicate,
    "Release did not restore Point exactly once.");
    const releaseReplay = await command(server.baseUrl, "release", first);
    assert(releaseReplay.status === 200 && releaseReplay.payload.duplicate
      && releaseReplay.payload.economy.pos === 5000,
    "Release replay restored Point twice.");
    const captureReleased = await command(server.baseUrl, "capture", first);
    assert(captureReleased.status === 409
      && captureReleased.payload.error === "POINT_RESERVATION_STATE_CONFLICT",
    "A released reservation was captured.");

    const second = makeReservation(playerId, "wallet-reserve-capture-001", 200);
    const secondReserved = await command(server.baseUrl, "reserve", second);
    assert(secondReserved.status === 200 && secondReserved.payload.economy.pos === 4800,
      "Second reservation did not debit Point.");

    await stopServer(server);
    server = await startServer(dataPath);
    const afterRestart = await balance(server.baseUrl, "wallet-balance-after-restart");
    assert(afterRestart.status === 200 && afterRestart.payload.point === 4800,
      "Reserved Point was lost across restart.");
    const reserveAfterRestart = await command(server.baseUrl, "reserve", second);
    assert(reserveAfterRestart.status === 200 && reserveAfterRestart.payload.duplicate
      && reserveAfterRestart.payload.economy.pos === 4800,
    "Post-restart reserve replay debited twice.");
    const captured = await command(server.baseUrl, "capture", second);
    assert(captured.status === 200 && captured.payload.reservation.status === "CAPTURED"
      && captured.payload.economy.pos === 4800,
    "Reservation capture changed the balance a second time.");
    const captureReplay = await command(server.baseUrl, "capture", second);
    assert(captureReplay.status === 200 && captureReplay.payload.duplicate
      && captureReplay.payload.economy.pos === 4800,
    "Capture replay changed the balance.");
    const releaseCaptured = await command(server.baseUrl, "release", second);
    assert(releaseCaptured.status === 409
      && releaseCaptured.payload.error === "POINT_RESERVATION_STATE_CONFLICT",
    "A captured reservation was released.");

    const third = makeReservation(playerId, "wallet-reserve-race-001", 50);
    const thirdReserved = await command(server.baseUrl, "reserve", third);
    assert(thirdReserved.status === 200 && thirdReserved.payload.economy.pos === 4750,
      "Race fixture reservation failed.");
    const concurrent = await Promise.all([
      command(server.baseUrl, "release", third),
      command(server.baseUrl, "release", third),
    ]);
    assert(concurrent.every((entry) => entry.status === 200)
      && concurrent.filter((entry) => entry.payload.duplicate).length === 1
      && concurrent.every((entry) => entry.payload.economy.pos === 4800),
    "Concurrent release did not settle exactly once.");

    await stopServer(server);
    server = null;
    const db = JSON.parse(fs.readFileSync(dataPath, "utf8"));
    assert(db.pointWalletReservations[first.reservation_id].status === "RELEASED"
      && db.pointWalletReservations[second.reservation_id].status === "CAPTURED"
      && db.pointWalletReservations[third.reservation_id].status === "RELEASED",
    "Persisted reservation states are wrong.");
    const walletTransactions = db.transactions.filter((tx) => tx.type.startsWith("web_point_"));
    assert(walletTransactions.length === 6,
      "Reservation retries created extra ledger transactions.");
    assert(walletTransactions.reduce((sum, tx) => sum + tx.deltaPos, 0) === -200,
      "Reservation ledger delta does not reconcile with the final balance.");

    console.log("[web-point-reservation] PASS: signed reserve/capture/release is identity-pinned, idempotent, restart-safe, race-safe, and ledger-balanced.");
  } finally {
    if (server) await stopServer(server);
    const resolved = path.resolve(tempDir);
    const root = path.resolve(os.tmpdir()) + path.sep;
    if (resolved.startsWith(root) && path.basename(resolved).startsWith("yw-point-reservation-")) {
      fs.rmSync(resolved, { recursive: true, force: true });
    }
  }
}

main().catch((error) => {
  console.error(`[web-point-reservation] FAIL: ${error.message}`);
  process.exit(1);
});
