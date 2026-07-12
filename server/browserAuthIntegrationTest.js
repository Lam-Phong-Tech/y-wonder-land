const crypto = require("crypto");
const fs = require("fs");
const net = require("net");
const os = require("os");
const path = require("path");
const { spawn } = require("child_process");

function assert(condition, message) {
  if (!condition) throw new Error(message);
}

function base64Url(buffer) {
  return Buffer.from(buffer).toString("base64")
    .replace(/\+/g, "-")
    .replace(/\//g, "_")
    .replace(/=+$/g, "");
}

function challenge(verifier) {
  return base64Url(crypto.createHash("sha256").update(verifier, "utf8").digest());
}

function requestHash(requestId) {
  return crypto.createHash("sha256").update(requestId, "utf8").digest("hex");
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
      throw new Error(`Game server exited early (${child.exitCode}): ${output.value}`);
    }
    try {
      const response = await fetch(`http://127.0.0.1:${port}/health`);
      if (response.ok) return;
    } catch (error) {
      // Server is still starting.
    }
    await new Promise((resolve) => setTimeout(resolve, 100));
  }
  throw new Error(`Game server did not become ready: ${output.value}`);
}

async function postJson(baseUrl, route, body, token = "") {
  const headers = { "Content-Type": "application/json" };
  if (token) headers.Authorization = `Bearer ${token}`;
  const response = await fetch(`${baseUrl}${route}`, {
    method: "POST",
    headers,
    body: JSON.stringify(body || {}),
  });
  return { response, payload: await response.json() };
}

async function startRequest(baseUrl, verifier, intent = "login") {
  return postJson(baseUrl, "/auth/browser/start", {
    code_challenge: challenge(verifier),
    intent,
  });
}

async function approve(baseUrl, secret, requestId, user) {
  return postJson(baseUrl, "/auth/browser/approve", {
    requestId,
    webUser: user,
  }, secret);
}

async function exchange(baseUrl, requestId, verifier) {
  return postJson(baseUrl, "/auth/browser/exchange", {
    requestId,
    code_verifier: verifier,
  });
}

function expireJsonRequest(dataPath, requestId) {
  const db = JSON.parse(fs.readFileSync(dataPath, "utf8"));
  const key = requestHash(requestId);
  assert(db.browserAuthRequests && db.browserAuthRequests[key], "Could not locate browser request in JSON store.");
  db.browserAuthRequests[key].expiresAt = new Date(Date.now() - 1000).toISOString();
  fs.writeFileSync(dataPath, JSON.stringify(db, null, 2), "utf8");
}

async function run() {
  const gamePort = await reservePort();
  const approvalSecret = "browser-auth-integration-secret-2026";
  const tempDir = fs.mkdtempSync(path.join(os.tmpdir(), "yw-browser-auth-"));
  const dataPath = path.join(tempDir, "data.json");
  const output = { value: "" };
  const child = spawn(process.execPath, ["index.js"], {
    cwd: __dirname,
    env: {
      ...process.env,
      NODE_ENV: "test",
      HOST: "127.0.0.1",
      PORT: String(gamePort),
      STORE_MODE: "json",
      YW_DATA_PATH: dataPath,
      JWT_SECRET: "browser-auth-integration-jwt-secret-2026",
      WEB_AUTH_MODE: "http",
      AUTH_TRANSITION_MODE: "parallel",
      WEB_AUTH_LOGIN_URL: "https://ywonder.net/api/game/auth",
      WEB_AUTH_SECRET: approvalSecret,
      BROWSER_AUTH_ENABLED: "true",
      BROWSER_AUTH_LOGIN_URL: "https://ywonder.net/vi/login",
      BROWSER_AUTH_CALLBACK_URL: "https://ywonder.net/api/game/browser/callback",
      BROWSER_AUTH_TTL_MS: "60000",
      BROWSER_AUTH_POLL_INTERVAL_MS: "500",
      LOCAL_REGISTRATION_ENABLED: "true",
      ADMIN_DASHBOARD_ENABLED: "false",
      DEMO_ACCOUNTS_ENABLED: "false",
      HTTP_ACCESS_LOG: "false",
      RATE_LIMIT_ENABLED: "true",
      AUTH_IP_RATE_LIMIT_MAX: "12",
      AUTH_IP_RATE_LIMIT_WINDOW_MS: "60000",
      BROWSER_AUTH_START_RATE_LIMIT_MAX: "12",
      BROWSER_AUTH_START_RATE_LIMIT_WINDOW_MS: "60000",
      BROWSER_AUTH_EXCHANGE_RATE_LIMIT_MAX: "25",
      BROWSER_AUTH_EXCHANGE_RATE_LIMIT_WINDOW_MS: "60000",
    },
    stdio: ["ignore", "pipe", "pipe"],
  });
  child.stdout.on("data", (chunk) => { output.value += chunk.toString("utf8"); });
  child.stderr.on("data", (chunk) => { output.value += chunk.toString("utf8"); });

  const verifier = base64Url(crypto.randomBytes(32));
  try {
    await waitForServer(child, gamePort, output);
    const baseUrl = `http://127.0.0.1:${gamePort}`;

    const invalidStart = await postJson(baseUrl, "/auth/browser/start", {
      code_challenge: "invalid",
    });
    assert(invalidStart.response.status === 400, "Invalid PKCE challenge was accepted.");

    const started = await startRequest(baseUrl, verifier, "login");
    assert(started.response.status === 201, `Browser start returned ${started.response.status}.`);
    assert(started.payload.requestId && started.payload.requestId.length === 43, "Browser start requestId is invalid.");
    assert(started.payload.pollIntervalMs === 500, "Browser poll interval is incorrect.");
    const authUrl = new URL(started.payload.authUrl);
    assert(authUrl.origin === "https://ywonder.net", "Browser auth URL origin is incorrect.");
    assert(authUrl.pathname === "/api/game/browser/callback", "Browser entry URL path is incorrect.");
    assert(authUrl.searchParams.get("request") === started.payload.requestId, "Browser entry lost requestId.");
    assert(!started.payload.authUrl.includes(verifier), "Browser auth URL leaked PKCE verifier.");

    const pending = await exchange(baseUrl, started.payload.requestId, verifier);
    assert(pending.response.status === 202 && pending.payload.status === "pending",
      "Pending browser exchange did not return 202.");
    for (let poll = 0; poll < 20; poll += 1) {
      const repeatedPending = await exchange(baseUrl, started.payload.requestId, verifier);
      assert(repeatedPending.response.status === 202,
        `Browser exchange poll ${poll + 2} consumed the shared auth quota.`);
    }

    const noSecret = await approve(baseUrl, "", started.payload.requestId, { userId: "web-browser-1" });
    assert(noSecret.response.status === 401, "Browser approval accepted a missing secret.");
    const wrongSecret = await approve(baseUrl, "wrong-secret", started.payload.requestId, { userId: "web-browser-1" });
    assert(wrongSecret.response.status === 401, "Browser approval accepted a wrong secret.");

    const webUser = {
      userId: "web-browser-1",
      username: "browser-user",
      email: "browser@example.test",
      fullName: "Browser Tester",
      status: "active",
    };
    const approved = await approve(baseUrl, approvalSecret, started.payload.requestId, webUser);
    assert(approved.response.status === 200 && approved.payload.ok, "Valid browser approval failed.");
    const duplicateApproval = await approve(baseUrl, approvalSecret, started.payload.requestId, webUser);
    assert(duplicateApproval.response.status === 200 && duplicateApproval.payload.duplicate,
      "Duplicate approval was not idempotent.");

    const wrongVerifier = base64Url(crypto.randomBytes(32));
    const rejectedPkce = await exchange(baseUrl, started.payload.requestId, wrongVerifier);
    assert(rejectedPkce.response.status === 401 && rejectedPkce.payload.error === "BROWSER_AUTH_PKCE_MISMATCH",
      "Wrong PKCE verifier was not rejected.");

    const completed = await exchange(baseUrl, started.payload.requestId, verifier);
    assert(completed.response.status === 200 && completed.payload.token, "Browser exchange did not issue game token.");
    assert(completed.payload.webUserId === "web-browser-1", "Browser exchange webUserId is incorrect.");
    assert(completed.payload.playerId, "Browser exchange did not map playerId.");

    const bootstrapResponse = await fetch(`${baseUrl}/player/bootstrap`, {
      headers: { Authorization: `Bearer ${completed.payload.token}` },
    });
    const bootstrap = await bootstrapResponse.json();
    assert(bootstrapResponse.status === 200 && bootstrap.player_profile && bootstrap.economy,
      "Browser session could not bootstrap game data.");

    const replay = await exchange(baseUrl, started.payload.requestId, verifier);
    assert(replay.response.status === 409 && replay.payload.error === "BROWSER_AUTH_CONSUMED",
      "Consumed browser request was replayed.");

    const secondVerifier = base64Url(crypto.randomBytes(32));
    const second = await startRequest(baseUrl, secondVerifier, "register");
    assert(second.response.status === 201, "Register-intent browser start failed.");
    const secondEntry = new URL(second.payload.authUrl);
    assert(secondEntry.pathname === "/api/game/browser/callback", "Register entry path is incorrect.");
    assert(secondEntry.searchParams.get("intent") === "register", "Register intent was not preserved.");
    await approve(baseUrl, approvalSecret, second.payload.requestId, webUser);
    const secondCompleted = await exchange(baseUrl, second.payload.requestId, secondVerifier);
    assert(secondCompleted.response.status === 200, "Second browser login failed.");
    assert(secondCompleted.payload.playerId === completed.payload.playerId,
      "Same web account mapped to a different player through browser auth.");

    const lockedVerifier = base64Url(crypto.randomBytes(32));
    const locked = await startRequest(baseUrl, lockedVerifier);
    const lockedApproval = await approve(baseUrl, approvalSecret, locked.payload.requestId, {
      userId: "web-locked-browser",
      username: "locked-browser",
      status: "locked",
    });
    assert(lockedApproval.response.status === 403 && lockedApproval.payload.error === "WEB_ACCOUNT_LOCKED",
      "Locked browser account was approved.");

    const expiredVerifier = base64Url(crypto.randomBytes(32));
    const expired = await startRequest(baseUrl, expiredVerifier);
    expireJsonRequest(dataPath, expired.payload.requestId);
    const expiredExchange = await exchange(baseUrl, expired.payload.requestId, expiredVerifier);
    assert(expiredExchange.response.status === 410 && expiredExchange.payload.error === "BROWSER_AUTH_EXPIRED",
      "Expired browser request was not rejected.");

    const limitedVerifier = base64Url(crypto.randomBytes(32));
    const limited = await startRequest(baseUrl, limitedVerifier);
    assert(limited.response.status === 201,
      "Browser polling consumed the password/start IP limiter.");
    for (let poll = 0; poll < 25; poll += 1) {
      const allowed = await exchange(baseUrl, limited.payload.requestId, limitedVerifier);
      assert(allowed.response.status === 202,
        `Dedicated browser exchange limiter blocked poll ${poll + 1} too early.`);
    }
    const exchangeBlocked = await exchange(baseUrl, limited.payload.requestId, limitedVerifier);
    assert(exchangeBlocked.response.status === 429 && exchangeBlocked.payload.error === "RATE_LIMITED",
      "Dedicated browser exchange limiter did not return 429 at its configured boundary.");

    assert(!output.value.includes(approvalSecret), "Server logs exposed browser approval secret.");
    assert(!output.value.includes(verifier), "Server logs exposed PKCE verifier.");
    assert(!output.value.includes(started.payload.requestId), "Server logs exposed raw browser requestId.");
  } finally {
    if (child.exitCode == null) child.kill("SIGTERM");
    await new Promise((resolve) => setTimeout(resolve, 250));
    const tempRoot = `${path.resolve(os.tmpdir())}${path.sep}`;
    const target = path.resolve(tempDir);
    if (!target.startsWith(tempRoot) || !path.basename(target).startsWith("yw-browser-auth-")) {
      throw new Error(`Refusing to remove unexpected test directory: ${target}`);
    }
    fs.rmSync(target, { recursive: true, force: true });
  }
}

run()
  .then(() => console.log("[browser-auth-integration] PASS: PKCE browser start/approve/exchange, expiry, replay, access guard, bootstrap, and stable mapping work."))
  .catch((error) => {
    console.error(`[browser-auth-integration] FAIL: ${error.message}`);
    process.exitCode = 1;
  });
