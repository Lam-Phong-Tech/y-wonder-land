const fs = require("fs");
const http = require("http");
const net = require("net");
const os = require("os");
const path = require("path");
const { spawn } = require("child_process");
const jwt = require("jsonwebtoken");

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

function readJsonBody(req) {
  return new Promise((resolve, reject) => {
    let body = "";
    req.setEncoding("utf8");
    req.on("data", (chunk) => {
      body += chunk;
      if (body.length > 16 * 1024) reject(new Error("Fake auth request was too large."));
    });
    req.on("end", () => {
      try {
        resolve(JSON.parse(body || "{}"));
      } catch (error) {
        reject(error);
      }
    });
    req.on("error", reject);
  });
}

function sendJson(res, status, payload) {
  const body = JSON.stringify(payload);
  res.writeHead(status, {
    "Content-Type": "application/json",
    "Content-Length": Buffer.byteLength(body),
  });
  res.end(body);
}

function makeWebUser(secret, overrides = {}) {
  const userId = overrides.userId || "web-active-1";
  const username = overrides.username || "active2025";
  return {
    ok: true,
    userId,
    username,
    fullName: overrides.fullName || "Active Tester",
    status: overrides.status || "active",
    locked: overrides.locked ?? false,
    softDeleted: overrides.softDeleted ?? false,
    gameToken: jwt.sign(
      { sub: userId, uid: userId, username },
      secret,
      { algorithm: "HS256", expiresIn: "5m" }
    ),
  };
}

async function startFakeWebAuth(port, secret) {
  const server = http.createServer(async (req, res) => {
    try {
      if (req.method !== "POST" || req.url !== "/login") {
        return sendJson(res, 404, { error: "NOT_FOUND" });
      }
      if (req.headers.authorization !== `Bearer ${secret}`) {
        return sendJson(res, 401, { error: "INVALID_SERVER_SECRET" });
      }

      const body = await readJsonBody(req);
      const identity = String(body.username || "").trim().toLowerCase();
      if (body.password !== "Correct@123") {
        return sendJson(res, 401, { message: "Internal credential detail must not reach Unity." });
      }

      if (identity === "locked@example.test") {
        return sendJson(res, 200, makeWebUser(secret, {
          userId: "web-locked-1",
          username: "locked-user",
          locked: true,
        }));
      }
      if (identity === "deleted@example.test") {
        return sendJson(res, 200, makeWebUser(secret, {
          userId: "web-deleted-1",
          username: "deleted-user",
          softDeleted: true,
        }));
      }
      if (identity === "inactive@example.test") {
        return sendJson(res, 200, makeWebUser(secret, {
          userId: "web-inactive-1",
          username: "inactive-user",
          status: "inactive",
        }));
      }
      if (identity === "upstream-locked@example.test") {
        return sendJson(res, 403, { error: "ACCOUNT_LOCKED", detail: "private upstream detail" });
      }
      if (identity === "unavailable@example.test") {
        return sendJson(res, 500, { message: "private upstream stack trace" });
      }
      return sendJson(res, 200, makeWebUser(secret));
    } catch (error) {
      return sendJson(res, 500, { error: "FAKE_AUTH_FAILURE" });
    }
  });

  await new Promise((resolve, reject) => {
    server.once("error", reject);
    server.listen(port, "127.0.0.1", resolve);
  });
  return server;
}

async function waitForGameServer(child, port) {
  let output = "";
  let errorOutput = "";
  child.stdout.on("data", (chunk) => { output += chunk.toString("utf8"); });
  child.stderr.on("data", (chunk) => { errorOutput += chunk.toString("utf8"); });

  const deadline = Date.now() + 15_000;
  while (Date.now() < deadline) {
    if (child.exitCode != null) {
      throw new Error(`Game server exited early (${child.exitCode}): ${errorOutput || output}`);
    }
    try {
      const response = await fetch(`http://127.0.0.1:${port}/health`);
      if (response.ok) return;
    } catch (error) {
      // Server is still starting.
    }
    await new Promise((resolve) => setTimeout(resolve, 100));
  }
  throw new Error(`Game server did not become ready: ${errorOutput || output}`);
}

async function postJson(baseUrl, route, body) {
  const response = await fetch(`${baseUrl}${route}`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  return { response, payload: await response.json() };
}

async function login(baseUrl, username, password = "Correct@123") {
  return postJson(baseUrl, "/auth/web-login", { username, password });
}

async function expectRejected(baseUrl, username, expectedStatus, expectedError) {
  const result = await login(baseUrl, username);
  assert(result.response.status === expectedStatus,
    `${username} returned ${result.response.status}, expected ${expectedStatus}.`);
  assert(result.payload.error === expectedError,
    `${username} returned ${result.payload.error}, expected ${expectedError}.`);
  assert(!result.payload.playerId && !result.payload.token,
    `${username} unexpectedly received a player or token.`);
}

async function run() {
  const webPort = await reservePort();
  const gamePort = await reservePort();
  const secret = "web-auth-integration-secret-2026";
  const tempDir = fs.mkdtempSync(path.join(os.tmpdir(), "yw-web-auth-"));
  const dataPath = path.join(tempDir, "data.json");
  const webServer = await startFakeWebAuth(webPort, secret);
  const gameServer = spawn(process.execPath, ["index.js"], {
    cwd: __dirname,
    env: {
      ...process.env,
      NODE_ENV: "test",
      HOST: "127.0.0.1",
      PORT: String(gamePort),
      STORE_MODE: "json",
      YW_DATA_PATH: dataPath,
      JWT_SECRET: "web-auth-integration-jwt-secret-with-32-chars",
      WEB_AUTH_MODE: "http",
      WEB_AUTH_LOGIN_URL: `http://127.0.0.1:${webPort}/login`,
      WEB_AUTH_SECRET: secret,
      LOCAL_REGISTRATION_ENABLED: "false",
      ADMIN_DASHBOARD_ENABLED: "false",
      DEMO_ACCOUNTS_ENABLED: "false",
      HTTP_ACCESS_LOG: "false",
      RATE_LIMIT_ENABLED: "false",
    },
    stdio: ["ignore", "pipe", "pipe"],
  });

  try {
    await waitForGameServer(gameServer, gamePort);
    const baseUrl = `http://127.0.0.1:${gamePort}`;

    const registration = await postJson(baseUrl, "/auth/register", {
      username: "BlockedRegister01",
      email: "blocked-register@example.test",
      password: "Strong@123",
    });
    assert(registration.response.status === 403, "Local registration was not disabled.");
    assert(registration.payload.error === "LOCAL_REGISTRATION_DISABLED",
      "Local registration returned the wrong error code.");

    const active = await login(baseUrl, "active@example.test");
    assert(active.response.status === 200 && active.payload.token, "Active web account could not log in.");
    assert(active.payload.webUserId === "web-active-1", "Active account webUserId is incorrect.");
    assert(active.payload.playerId, "Active account did not receive a playerId.");
    const gameTokenPayload = jwt.verify(
      active.payload.token,
      "web-auth-integration-jwt-secret-with-32-chars",
      { algorithms: ["HS256"] }
    );
    assert(gameTokenPayload.uid === active.payload.playerId,
      "Game JWT uid does not match the mapped playerId.");

    const bootstrapResponse = await fetch(`${baseUrl}/player/bootstrap`, {
      headers: { Authorization: `Bearer ${active.payload.token}` },
    });
    const bootstrap = await bootstrapResponse.json();
    assert(bootstrapResponse.status === 200, "Active account bootstrap failed.");
    assert(bootstrap.player_profile && bootstrap.economy && bootstrap.inventory && bootstrap.farm_state,
      "Bootstrap is missing required game data.");

    const relogin = await login(baseUrl, "active@example.test");
    assert(relogin.response.status === 200, "Active account relogin failed.");
    assert(relogin.payload.playerId === active.payload.playerId,
      "The same web account mapped to a different playerId.");

    const wrongPassword = await login(baseUrl, "active@example.test", "Wrong@123");
    assert(wrongPassword.response.status === 401, "Wrong web password did not return 401.");
    assert(wrongPassword.payload.error === "WEB_AUTH_INVALID_CREDENTIALS",
      "Wrong web password leaked or returned an unstable error.");

    await expectRejected(baseUrl, "locked@example.test", 403, "WEB_ACCOUNT_LOCKED");
    await expectRejected(baseUrl, "deleted@example.test", 403, "WEB_ACCOUNT_DELETED");
    await expectRejected(baseUrl, "inactive@example.test", 403, "WEB_ACCOUNT_INACTIVE");
    await expectRejected(baseUrl, "upstream-locked@example.test", 403, "WEB_ACCOUNT_LOCKED");
    await expectRejected(baseUrl, "unavailable@example.test", 502, "WEB_AUTH_UNAVAILABLE");
  } finally {
    if (gameServer.exitCode == null) gameServer.kill("SIGTERM");
    await new Promise((resolve) => webServer.close(resolve));
    await new Promise((resolve) => setTimeout(resolve, 200));
    const tempRoot = `${path.resolve(os.tmpdir())}${path.sep}`;
    const target = path.resolve(tempDir);
    if (!target.startsWith(tempRoot) || !path.basename(target).startsWith("yw-web-auth-")) {
      throw new Error(`Refusing to remove unexpected test directory: ${target}`);
    }
    fs.rmSync(target, { recursive: true, force: true });
  }
}

run()
  .then(() => console.log("[web-auth-integration] PASS: registration gate, web mapping, bootstrap, stable errors, and account-status guards work."))
  .catch((error) => {
    console.error(`[web-auth-integration] FAIL: ${error.message}`);
    process.exitCode = 1;
  });
