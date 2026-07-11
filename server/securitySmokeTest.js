const fs = require("fs");
const net = require("net");
const os = require("os");
const path = require("path");
const { spawn } = require("child_process");
const WebSocket = require("ws");
const {
  validateProductionConfig,
  validateRegistrationBody,
} = require("./security");

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

function testConfigurationGate() {
  let rejected = false;
  try {
    validateProductionConfig({
      NODE_ENV: "production",
      HOST: "0.0.0.0",
      STORE_MODE: "json",
      WEB_AUTH_MODE: "mock",
      ADMIN_DASHBOARD_ENABLED: "true",
      DEMO_ACCOUNTS_ENABLED: "true",
      JWT_SECRET: "short",
    });
  } catch (error) {
    rejected = String(error.message).includes("Unsafe production configuration");
  }
  assert(rejected, "Unsafe production configuration was not rejected.");

  validateProductionConfig({
    NODE_ENV: "production",
    HOST: "127.0.0.1",
    STORE_MODE: "postgres",
    WEB_AUTH_MODE: "disabled",
    ADMIN_DASHBOARD_ENABLED: "false",
    DEMO_ACCOUNTS_ENABLED: "false",
    JWT_SECRET: "security-smoke-secret-with-more-than-32-characters",
  });
}

function testRegistrationValidation() {
  const weak = validateRegistrationBody({
    username: "Security01",
    email: "security@example.test",
    password: "weakpass",
  });
  assert(!weak.ok && weak.error === "WEAK_PASSWORD", "Weak password was not rejected.");

  const valid = validateRegistrationBody({
    username: "Security01",
    email: "security@example.test",
    password: "Strong@123",
  });
  assert(valid.ok, "A valid registration payload was rejected.");
}

async function waitForServer(child, port) {
  let output = "";
  let errorOutput = "";
  child.stdout.on("data", (chunk) => { output += chunk.toString("utf8"); });
  child.stderr.on("data", (chunk) => { errorOutput += chunk.toString("utf8"); });

  const deadline = Date.now() + 15_000;
  while (Date.now() < deadline) {
    if (child.exitCode != null) {
      throw new Error(`Security test server exited early (${child.exitCode}): ${errorOutput || output}`);
    }
    try {
      const response = await fetch(`http://127.0.0.1:${port}/health`);
      if (response.ok) return;
    } catch (error) {
      // Server is still starting.
    }
    await new Promise((resolve) => setTimeout(resolve, 100));
  }

  throw new Error(`Security test server did not become ready: ${errorOutput || output}`);
}

async function request(baseUrl, method, route, body, headers = {}) {
  const response = await fetch(`${baseUrl}${route}`, {
    method,
    headers,
    body,
  });
  let payload = null;
  try {
    payload = await response.json();
  } catch (error) {
    payload = null;
  }
  return { response, payload };
}

async function postJson(baseUrl, route, body) {
  return request(baseUrl, "POST", route, JSON.stringify(body), { "Content-Type": "application/json" });
}

async function register(baseUrl, suffix) {
  const username = `Secure_${suffix}`;
  const result = await postJson(baseUrl, "/auth/register", {
    username,
    email: `${username.toLowerCase()}@example.test`,
    password: "Strong@123",
  });
  assert(result.response.status === 200, `Register ${username} returned ${result.response.status}.`);
  return { username, password: "Strong@123", token: result.payload.token };
}

function openRealtime(baseUrl, token) {
  const url = new URL("/realtime", baseUrl);
  url.protocol = "ws:";
  url.searchParams.set("token", token);
  const socket = new WebSocket(url);

  return new Promise((resolve, reject) => {
    const timer = setTimeout(() => reject(new Error("Realtime connection timed out.")), 5000);
    socket.once("open", () => {
      clearTimeout(timer);
      resolve(socket);
    });
    socket.once("error", (error) => {
      clearTimeout(timer);
      reject(error);
    });
  });
}

function waitForClose(socket, label) {
  return new Promise((resolve, reject) => {
    const timer = setTimeout(() => reject(new Error(`${label} close timed out.`)), 5000);
    socket.once("close", (code) => {
      clearTimeout(timer);
      resolve(code);
    });
  });
}

async function testRealtimeGuards(baseUrl, token) {
  const rateLimited = await openRealtime(baseUrl, token);
  const rateClose = waitForClose(rateLimited, "Message-rate");
  for (let index = 0; index < 31; index += 1) {
    rateLimited.send(JSON.stringify({ type: "ping", index }));
  }
  assert(await rateClose === 1008, "Realtime message-rate guard did not close with 1008.");

  const oversized = await openRealtime(baseUrl, token);
  const payloadClose = waitForClose(oversized, "Oversized-payload");
  oversized.send(JSON.stringify({ type: "ping", padding: "X".repeat(2048) }));
  assert(await payloadClose === 1009, "Realtime payload guard did not close with 1009.");
}

async function runIntegrationTest() {
  const port = await reservePort();
  const tempDir = fs.mkdtempSync(path.join(os.tmpdir(), "yw-security-"));
  const dataPath = path.join(tempDir, "data.json");
  const child = spawn(process.execPath, ["index.js"], {
    cwd: __dirname,
    env: {
      ...process.env,
      NODE_ENV: "test",
      HOST: "127.0.0.1",
      PORT: String(port),
      STORE_MODE: "json",
      YW_DATA_PATH: dataPath,
      JWT_SECRET: "security-smoke-secret-with-more-than-32-characters",
      WEB_AUTH_MODE: "disabled",
      ADMIN_DASHBOARD_ENABLED: "false",
      DEMO_ACCOUNTS_ENABLED: "false",
      HTTP_ACCESS_LOG: "false",
      CORS_ALLOWED_ORIGINS: "https://allowed.example",
      JSON_BODY_LIMIT: "2kb",
      BCRYPT_ROUNDS: "8",
      AUTH_IP_RATE_LIMIT_MAX: "100",
      AUTH_IDENTITY_RATE_LIMIT_MAX: "3",
      AUTH_IDENTITY_RATE_LIMIT_WINDOW_MS: "60000",
      AUTH_REGISTER_RATE_LIMIT_MAX: "3",
      AUTH_REGISTER_RATE_LIMIT_WINDOW_MS: "60000",
      REALTIME_MAX_PAYLOAD_BYTES: "1024",
      REALTIME_MESSAGE_RATE_MAX: "30",
      REALTIME_MESSAGE_RATE_WINDOW_MS: "60000",
    },
    stdio: ["ignore", "pipe", "pipe"],
  });

  try {
    await waitForServer(child, port);
    const baseUrl = `http://127.0.0.1:${port}`;

    const health = await fetch(`${baseUrl}/health`, {
      headers: { Origin: "https://blocked.example", "X-Request-ID": "security-smoke" },
    });
    assert(health.status === 200, "Health check failed.");
    assert(health.headers.get("x-request-id") === "security-smoke", "X-Request-ID was not propagated.");
    assert(health.headers.get("x-content-type-options") === "nosniff", "nosniff header is missing.");
    assert(health.headers.get("x-frame-options") === "DENY", "X-Frame-Options header is missing.");
    assert(!health.headers.get("x-powered-by"), "Express X-Powered-By header is still exposed.");
    assert(!health.headers.get("access-control-allow-origin"), "Blocked CORS origin was allowed.");

    const invalidJson = await request(baseUrl, "POST", "/auth/login", "{", { "Content-Type": "application/json" });
    assert(invalidJson.response.status === 400, "Invalid JSON did not return 400.");
    assert(invalidJson.payload.error === "INVALID_JSON", "Invalid JSON error code is incorrect.");

    const oversized = await postJson(baseUrl, "/auth/login", {
      username: "Oversized01",
      password: "X".repeat(4096),
    });
    assert(oversized.response.status === 413, "Oversized JSON did not return 413.");
    assert(oversized.payload.error === "PAYLOAD_TOO_LARGE", "Oversized JSON error code is incorrect.");

    const accountA = await register(baseUrl, "A01");
    const accountB = await register(baseUrl, "B01");
    await register(baseUrl, "C01");
    const registerBlocked = await postJson(baseUrl, "/auth/register", {
      username: "Secure_D01",
      email: "secure_d01@example.test",
      password: "Strong@123",
    });
    assert(registerBlocked.response.status === 429, "Registration rate limit did not return 429.");
    assert(registerBlocked.payload.error === "RATE_LIMITED", "Registration rate-limit error is incorrect.");

    for (let attempt = 0; attempt < 3; attempt += 1) {
      const failed = await postJson(baseUrl, "/auth/login", {
        username: accountA.username,
        password: "Wrong@123",
      });
      assert(failed.response.status === 401, `Wrong password attempt ${attempt + 1} did not return 401.`);
    }

    const loginBlocked = await postJson(baseUrl, "/auth/login", {
      username: accountA.username,
      password: accountA.password,
    });
    assert(loginBlocked.response.status === 429, "Identity rate limit did not return 429.");
    assert(Number(loginBlocked.response.headers.get("retry-after")) > 0, "Retry-After header is missing.");

    const otherLogin = await postJson(baseUrl, "/auth/login", {
      username: accountB.username,
      password: accountB.password,
    });
    assert(otherLogin.response.status === 200 && otherLogin.payload.token, "Another account was incorrectly blocked.");
    await testRealtimeGuards(baseUrl, otherLogin.payload.token);
  } finally {
    if (child.exitCode == null) child.kill("SIGTERM");
    await new Promise((resolve) => setTimeout(resolve, 200));
    const resolvedTempRoot = `${path.resolve(os.tmpdir())}${path.sep}`;
    const resolvedTarget = path.resolve(tempDir);
    if (!resolvedTarget.startsWith(resolvedTempRoot) || !path.basename(resolvedTarget).startsWith("yw-security-")) {
      throw new Error(`Refusing to remove unexpected test directory: ${resolvedTarget}`);
    }
    fs.rmSync(resolvedTarget, { recursive: true, force: true });
  }
}

async function main() {
  testConfigurationGate();
  testRegistrationValidation();
  await runIntegrationTest();
  console.log("[security-smoke] PASS: production gate, validation, HTTP guards, auth rate limits, and realtime limits work.");
}

main().catch((error) => {
  console.error(`[security-smoke] FAIL: ${error.message}`);
  process.exitCode = 1;
});
