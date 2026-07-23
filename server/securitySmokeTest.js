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

function removeTestDirectory(tempDir, expectedPrefix) {
  const resolvedTempRoot = `${path.resolve(os.tmpdir())}${path.sep}`;
  const resolvedTarget = path.resolve(tempDir);
  if (!resolvedTarget.startsWith(resolvedTempRoot)
      || !path.basename(resolvedTarget).startsWith(expectedPrefix)) {
    throw new Error(`Refusing to remove unexpected test directory: ${resolvedTarget}`);
  }
  fs.rmSync(resolvedTarget, { recursive: true, force: true });
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

  let unsafeWebAuthAccepted = false;
  try {
    validateProductionConfig({
      NODE_ENV: "production",
      HOST: "127.0.0.1",
      STORE_MODE: "postgres",
      WEB_AUTH_MODE: "http",
      WEB_AUTH_LOGIN_URL: "http://web-auth.example.test/login",
      WEB_AUTH_SECRET: "short",
      LOCAL_REGISTRATION_ENABLED: "true",
      ADMIN_DASHBOARD_ENABLED: "false",
      DEMO_ACCOUNTS_ENABLED: "false",
      JWT_SECRET: "security-smoke-secret-with-more-than-32-characters",
    });
    unsafeWebAuthAccepted = true;
  } catch (error) {
    assert(String(error.message).includes("WEB_AUTH_LOGIN_URL"), "Unsafe web-auth URL was not reported.");
    assert(String(error.message).includes("WEB_AUTH_SECRET"), "Short web-auth secret was not reported.");
    assert(String(error.message).includes("LOCAL_REGISTRATION_ENABLED"), "Local registration gate was not reported.");
  }
  assert(!unsafeWebAuthAccepted, "Unsafe production web-auth configuration was accepted.");

  validateProductionConfig({
    NODE_ENV: "production",
    HOST: "127.0.0.1",
    STORE_MODE: "postgres",
    WEB_AUTH_MODE: "http",
    WEB_AUTH_LOGIN_URL: "https://web-auth.example.test/login",
    WEB_AUTH_SECRET: "security-smoke-web-auth-secret",
    LOCAL_REGISTRATION_ENABLED: "false",
    ADMIN_DASHBOARD_ENABLED: "false",
    DEMO_ACCOUNTS_ENABLED: "false",
    JWT_SECRET: "security-smoke-secret-with-more-than-32-characters",
  });

  validateProductionConfig({
    NODE_ENV: "production",
    HOST: "127.0.0.1",
    STORE_MODE: "postgres",
    WEB_AUTH_MODE: "http",
    AUTH_TRANSITION_MODE: "parallel",
    WEB_AUTH_LOGIN_URL: "https://web-auth.example.test/login",
    WEB_AUTH_SECRET: "security-smoke-web-auth-secret",
    LOCAL_REGISTRATION_ENABLED: "true",
    ADMIN_DASHBOARD_ENABLED: "false",
    DEMO_ACCOUNTS_ENABLED: "false",
    JWT_SECRET: "security-smoke-secret-with-more-than-32-characters",
  });

  validateProductionConfig({
    NODE_ENV: "production",
    HOST: "127.0.0.1",
    STORE_MODE: "postgres",
    WEB_AUTH_MODE: "http",
    AUTH_TRANSITION_MODE: "parallel",
    WEB_AUTH_LOGIN_URL: "https://ywonder.net/api/game/auth",
    WEB_AUTH_SECRET: "security-smoke-web-auth-secret",
    BROWSER_AUTH_ENABLED: "true",
    BROWSER_AUTH_LOGIN_URL: "https://ywonder.net/vi/login",
    BROWSER_AUTH_CALLBACK_URL: "https://ywonder.net/api/game/browser/callback",
    LOCAL_REGISTRATION_ENABLED: "true",
    ADMIN_DASHBOARD_ENABLED: "false",
    DEMO_ACCOUNTS_ENABLED: "false",
    JWT_SECRET: "security-smoke-secret-with-more-than-32-characters",
  });

  let unsafeBrowserAuthAccepted = false;
  try {
    validateProductionConfig({
      NODE_ENV: "production",
      HOST: "127.0.0.1",
      STORE_MODE: "postgres",
      WEB_AUTH_MODE: "http",
      AUTH_TRANSITION_MODE: "parallel",
      WEB_AUTH_LOGIN_URL: "https://ywonder.net/api/game/auth",
      WEB_AUTH_SECRET: "security-smoke-web-auth-secret",
      BROWSER_AUTH_ENABLED: "true",
      BROWSER_AUTH_LOGIN_URL: "https://ywonder.net/vi/login",
      BROWSER_AUTH_CALLBACK_URL: "https://evil.example.test/callback",
      LOCAL_REGISTRATION_ENABLED: "true",
      ADMIN_DASHBOARD_ENABLED: "false",
      DEMO_ACCOUNTS_ENABLED: "false",
      JWT_SECRET: "security-smoke-secret-with-more-than-32-characters",
    });
    unsafeBrowserAuthAccepted = true;
  } catch (error) {
    assert(String(error.message).includes("same origin"),
      "Cross-origin browser callback returned the wrong error.");
    assert(String(error.message).includes("/api/game/browser/callback"),
      "Unexpected browser callback path was not reported.");
  }
  assert(!unsafeBrowserAuthAccepted, "Unsafe browser-auth callback was accepted.");

  let incompleteParallelAccepted = false;
  try {
    validateProductionConfig({
      NODE_ENV: "production",
      HOST: "127.0.0.1",
      STORE_MODE: "postgres",
      WEB_AUTH_MODE: "http",
      AUTH_TRANSITION_MODE: "parallel",
      WEB_AUTH_LOGIN_URL: "https://web-auth.example.test/login",
      WEB_AUTH_SECRET: "security-smoke-web-auth-secret",
      LOCAL_REGISTRATION_ENABLED: "false",
      ADMIN_DASHBOARD_ENABLED: "false",
      DEMO_ACCOUNTS_ENABLED: "false",
      JWT_SECRET: "security-smoke-secret-with-more-than-32-characters",
    });
    incompleteParallelAccepted = true;
  } catch (error) {
    assert(String(error.message).includes("AUTH_TRANSITION_MODE=parallel"),
      "Incomplete parallel auth configuration returned the wrong error.");
  }
  assert(!incompleteParallelAccepted, "Parallel auth without local registration was accepted.");

  let parallelWithoutWebAccepted = false;
  try {
    validateProductionConfig({
      NODE_ENV: "production",
      HOST: "127.0.0.1",
      STORE_MODE: "postgres",
      WEB_AUTH_MODE: "disabled",
      AUTH_TRANSITION_MODE: "parallel",
      LOCAL_REGISTRATION_ENABLED: "true",
      ADMIN_DASHBOARD_ENABLED: "false",
      DEMO_ACCOUNTS_ENABLED: "false",
      JWT_SECRET: "security-smoke-secret-with-more-than-32-characters",
    });
    parallelWithoutWebAccepted = true;
  } catch (error) {
    assert(String(error.message).includes("WEB_AUTH_MODE=disabled"),
      "Parallel auth without web auth returned the wrong error.");
  }
  assert(!parallelWithoutWebAccepted, "Parallel auth with disabled web auth was accepted.");

  let topupWithoutSecretAccepted = false;
  try {
    validateProductionConfig({
      NODE_ENV: "production",
      HOST: "127.0.0.1",
      STORE_MODE: "postgres",
      WEB_AUTH_MODE: "disabled",
      WEB_TOPUP_ENABLED: "true",
      WEB_TOPUP_SECRET: "short",
      ADMIN_DASHBOARD_ENABLED: "false",
      DEMO_ACCOUNTS_ENABLED: "false",
      JWT_SECRET: "security-smoke-secret-with-more-than-32-characters",
    });
    topupWithoutSecretAccepted = true;
  } catch (error) {
    assert(String(error.message).includes("WEB_TOPUP_SECRET"),
      "Missing Point-credit secret returned the wrong production error.");
  }
  assert(!topupWithoutSecretAccepted, "Production Point credit accepted a short secret.");

  let reusedGameSecretAccepted = false;
  try {
    validateProductionConfig({
      NODE_ENV: "production",
      HOST: "127.0.0.1",
      STORE_MODE: "postgres",
      WEB_AUTH_MODE: "disabled",
      WEB_TOPUP_ENABLED: "true",
      GAME_API_SECRET: "shared-game-api-secret-with-32-plus-characters",
      ADMIN_DASHBOARD_ENABLED: "false",
      DEMO_ACCOUNTS_ENABLED: "false",
      JWT_SECRET: "security-smoke-secret-with-more-than-32-characters",
    });
    reusedGameSecretAccepted = true;
  } catch (error) {
    assert(String(error.message).includes("WEB_TOPUP_SECRET"),
      "A reused game API secret returned the wrong Point-credit error.");
  }
  assert(!reusedGameSecretAccepted,
    "Production Point credit reused GAME_API_SECRET instead of a dedicated secret.");

  let topupWithClientGrantsAccepted = false;
  try {
    validateProductionConfig({
      NODE_ENV: "production",
      HOST: "127.0.0.1",
      STORE_MODE: "postgres",
      WEB_AUTH_MODE: "disabled",
      WEB_TOPUP_ENABLED: "true",
      WEB_TOPUP_SECRET: "security-smoke-topup-secret-with-32-plus-characters",
      WEB_TOPUP_MODE: "canary",
      WEB_TOPUP_ALLOWED_WEB_USER_IDS: "security-canary-web-user",
      ADMIN_DASHBOARD_ENABLED: "false",
      DEMO_ACCOUNTS_ENABLED: "false",
      JWT_SECRET: "security-smoke-secret-with-more-than-32-characters",
    });
    topupWithClientGrantsAccepted = true;
  } catch (error) {
    assert(String(error.message).includes("CLIENT_ASSET_GRANTS_BLOCKED_WEB_USER_IDS"),
      "Unsafe Point-credit client grant mode returned the wrong production error.");
  }
  assert(!topupWithClientGrantsAccepted,
    "Production Point credit started without isolating canary client asset grants.");

  validateProductionConfig({
    NODE_ENV: "production",
    HOST: "127.0.0.1",
    STORE_MODE: "postgres",
    WEB_AUTH_MODE: "disabled",
    WEB_TOPUP_ENABLED: "true",
    WEB_TOPUP_SECRET: "security-smoke-topup-secret-with-32-plus-characters",
    WEB_TOPUP_MODE: "canary",
    WEB_TOPUP_ALLOWED_WEB_USER_IDS: "security-canary-web-user",
    CLIENT_ASSET_GRANTS_ENABLED: "true",
    CLIENT_ASSET_GRANTS_BLOCKED_WEB_USER_IDS: "security-canary-web-user",
    ADMIN_DASHBOARD_ENABLED: "false",
    DEMO_ACCOUNTS_ENABLED: "false",
    JWT_SECRET: "security-smoke-secret-with-more-than-32-characters",
  });

  let mismatchedGrantCanaryAccepted = false;
  try {
    validateProductionConfig({
      NODE_ENV: "production",
      HOST: "127.0.0.1",
      STORE_MODE: "postgres",
      WEB_AUTH_MODE: "disabled",
      WEB_TOPUP_ENABLED: "true",
      WEB_TOPUP_SECRET: "security-smoke-topup-secret-with-32-plus-characters",
      WEB_TOPUP_MODE: "canary",
      WEB_TOPUP_ALLOWED_WEB_USER_IDS: "security-canary-web-user",
      CLIENT_ASSET_GRANTS_ENABLED: "true",
      CLIENT_ASSET_GRANTS_BLOCKED_WEB_USER_IDS: "different-web-user",
      ADMIN_DASHBOARD_ENABLED: "false",
      DEMO_ACCOUNTS_ENABLED: "false",
      JWT_SECRET: "security-smoke-secret-with-more-than-32-characters",
    });
    mismatchedGrantCanaryAccepted = true;
  } catch (error) {
    assert(String(error.message).includes("exactly match"),
      "Mismatched canary grant isolation returned the wrong production error.");
  }
  assert(!mismatchedGrantCanaryAccepted,
    "Production Point canary accepted a mismatched client-grant block list.");

  let openTopupWithClientGrantsAccepted = false;
  try {
    validateProductionConfig({
      NODE_ENV: "production",
      HOST: "127.0.0.1",
      STORE_MODE: "postgres",
      WEB_AUTH_MODE: "disabled",
      WEB_TOPUP_ENABLED: "true",
      WEB_TOPUP_SECRET: "security-smoke-topup-secret-with-32-plus-characters",
      WEB_TOPUP_MODE: "open",
      CLIENT_ASSET_GRANTS_ENABLED: "true",
      ADMIN_DASHBOARD_ENABLED: "false",
      DEMO_ACCOUNTS_ENABLED: "false",
      JWT_SECRET: "security-smoke-secret-with-more-than-32-characters",
    });
    openTopupWithClientGrantsAccepted = true;
  } catch (error) {
    assert(String(error.message).includes("CLIENT_ASSET_GRANTS_ENABLED"),
      "Open Point mode returned the wrong global client-grant error.");
  }
  assert(!openTopupWithClientGrantsAccepted,
    "Open Point mode started while global client asset grants were enabled.");

  let topupWithoutCanaryUserAccepted = false;
  try {
    validateProductionConfig({
      NODE_ENV: "production",
      HOST: "127.0.0.1",
      STORE_MODE: "postgres",
      WEB_AUTH_MODE: "disabled",
      WEB_TOPUP_ENABLED: "true",
      WEB_TOPUP_SECRET: "security-smoke-topup-secret-with-32-plus-characters",
      WEB_TOPUP_MODE: "canary",
      CLIENT_ASSET_GRANTS_ENABLED: "false",
      ADMIN_DASHBOARD_ENABLED: "false",
      DEMO_ACCOUNTS_ENABLED: "false",
      JWT_SECRET: "security-smoke-secret-with-more-than-32-characters",
    });
    topupWithoutCanaryUserAccepted = true;
  } catch (error) {
    assert(String(error.message).includes("WEB_TOPUP_ALLOWED_WEB_USER_IDS"),
      "Missing Point-credit canary account returned the wrong production error.");
  }
  assert(!topupWithoutCanaryUserAccepted,
    "Production Point credit started in canary mode without an account allowlist.");

  validateProductionConfig({
    NODE_ENV: "production",
    HOST: "127.0.0.1",
    STORE_MODE: "postgres",
    WEB_AUTH_MODE: "disabled",
    WEB_TOPUP_ENABLED: "true",
    WEB_TOPUP_SECRET: "security-smoke-topup-secret-with-32-plus-characters",
    WEB_TOPUP_MODE: "canary",
    WEB_TOPUP_ALLOWED_WEB_USER_IDS: "security-canary-web-user",
    CLIENT_ASSET_GRANTS_ENABLED: "false",
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

async function authorizedJson(baseUrl, method, route, body, token) {
  return request(baseUrl, method, route, body == null ? undefined : JSON.stringify(body), {
    "Content-Type": "application/json",
    Authorization: `Bearer ${token}`,
  });
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
      CLIENT_ASSET_GRANTS_ENABLED: "false",
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

    const bootstrap = await authorizedJson(baseUrl, "GET", "/player/bootstrap", null, otherLogin.payload.token);
    assert(bootstrap.response.status === 200, "Player bootstrap failed.");
    assert(!Object.prototype.hasOwnProperty.call(bootstrap.payload.economy || {}, "upos"),
      "Bootstrap still exposes retired UPoint balance.");

    const directEconomySet = await authorizedJson(baseUrl, "PUT", "/player/economy", {
      economy: { pos: 999999999 },
    }, otherLogin.payload.token);
    assert(directEconomySet.response.status === 405,
      "Authenticated client can still overwrite its Point balance.");
    assert(directEconomySet.payload.error === "ECONOMY_SERVER_AUTHORITATIVE",
      "Direct Point overwrite returned the wrong error.");

    const retiredUPoint = await authorizedJson(baseUrl, "POST", "/player/economy/apply", {
      delta_upos: 1,
      idempotency_key: "security-retired-upoint",
    }, otherLogin.payload.token);
    assert(retiredUPoint.response.status === 400 && retiredUPoint.payload.error === "UPOINT_RETIRED",
      "Legacy UPoint mutation was not rejected.");

    const positivePoint = await authorizedJson(baseUrl, "POST", "/player/economy/apply", {
      delta_pos: 1,
      idempotency_key: "security-positive-point",
    }, otherLogin.payload.token);
    assert(positivePoint.response.status === 403
      && positivePoint.payload.error === "CLIENT_POSITIVE_ECONOMY_DELTA_FORBIDDEN",
    "Authenticated client can still mint Point through a positive delta.");

    const positiveItem = await authorizedJson(baseUrl, "POST", "/player/inventory/adjust", {
      item_id: "fish_ca_com_01",
      quantity_delta: 1,
      idempotency_key: "security-positive-item",
    }, otherLogin.payload.token);
    assert(positiveItem.response.status === 403
      && positiveItem.payload.error === "CLIENT_POSITIVE_INVENTORY_DELTA_FORBIDDEN",
    "Authenticated client can still mint inventory through a positive delta.");

    const replaceInventory = await authorizedJson(baseUrl, "PUT", "/player/inventory", {
      inventory: { maxSlots: 999, slots: [{ itemId: "fish_ca_com_01", quantity: 999999 }] },
    }, otherLogin.payload.token);
    assert(replaceInventory.response.status === 405
      && replaceInventory.payload.error === "INVENTORY_SERVER_AUTHORITATIVE",
    "Authenticated client can still replace its complete inventory.");

    const debitPoint = await authorizedJson(baseUrl, "POST", "/player/economy/apply", {
      delta_pos: -1,
      idempotency_key: "security-debit-point",
    }, otherLogin.payload.token);
    assert(debitPoint.response.status === 200
      && Number(debitPoint.payload.economy.pos) === Number(bootstrap.payload.economy.pos) - 1,
    "Debit-only Point mutation stopped working in strict mode.");

    const carrotBefore = (bootstrap.payload.inventory.slots || [])
      .find((slot) => slot.itemId === "carrot_seed_01");
    const debitItem = await authorizedJson(baseUrl, "POST", "/player/inventory/adjust", {
      item_id: "carrot_seed_01",
      quantity_delta: -1,
      idempotency_key: "security-debit-item",
    }, otherLogin.payload.token);
    const carrotAfter = (debitItem.payload.inventory.slots || [])
      .find((slot) => slot.itemId === "carrot_seed_01");
    assert(debitItem.response.status === 200 && carrotBefore
      && Number(carrotAfter && carrotAfter.quantity) === Number(carrotBefore.quantity) - 1,
    "Debit-only inventory mutation stopped working in strict mode.");

    await testRealtimeGuards(baseUrl, otherLogin.payload.token);
  } finally {
    if (child.exitCode == null) child.kill("SIGTERM");
    await new Promise((resolve) => setTimeout(resolve, 200));
    removeTestDirectory(tempDir, "yw-security-");
  }
}

async function runCanaryGrantIsolationIntegrationTest() {
  const port = await reservePort();
  const tempDir = fs.mkdtempSync(path.join(os.tmpdir(), "yw-security-canary-"));
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
      JWT_SECRET: "security-canary-secret-with-more-than-32-characters",
      WEB_AUTH_MODE: "mock",
      ADMIN_DASHBOARD_ENABLED: "false",
      DEMO_ACCOUNTS_ENABLED: "false",
      HTTP_ACCESS_LOG: "false",
      RATE_LIMIT_ENABLED: "false",
      BCRYPT_ROUNDS: "8",
      CLIENT_ASSET_GRANTS_ENABLED: "true",
      CLIENT_ASSET_GRANTS_BLOCKED_WEB_USER_IDS: "mock:securitycanary01",
    },
    stdio: ["ignore", "pipe", "pipe"],
  });

  try {
    await waitForServer(child, port);
    const baseUrl = `http://127.0.0.1:${port}`;
    const canaryLogin = await postJson(baseUrl, "/auth/web-login", {
      username: "SecurityCanary01",
      password: "Strong@123",
    });
    const controlLogin = await postJson(baseUrl, "/auth/web-login", {
      username: "SecurityControl01",
      password: "Strong@123",
    });
    const storeMappedLogin = await postJson(baseUrl, "/auth/web-login", {
      username: "SecurityStoreMapped01",
      password: "Strong@123",
    });
    assert(canaryLogin.response.status === 200 && canaryLogin.payload.token,
      "Canary web login failed in client-grant isolation test.");
    assert(controlLogin.response.status === 200 && controlLogin.payload.token,
      "Control web login failed in client-grant isolation test.");
    assert(storeMappedLogin.response.status === 200 && storeMappedLogin.payload.token,
      "Store-mapped web login failed in client-grant isolation test.");

    const persisted = JSON.parse(fs.readFileSync(dataPath, "utf8"));
    persisted.players[storeMappedLogin.payload.playerId].webUserId = "mock:securitycanary01";
    fs.writeFileSync(dataPath, JSON.stringify(persisted, null, 2), "utf8");
    const authoritativeStoreBlock = await authorizedJson(
      baseUrl,
      "POST",
      "/player/economy/apply",
      {
        delta_pos: 1,
        idempotency_key: "security-authoritative-store-positive-point",
      },
      storeMappedLogin.payload.token
    );
    assert(authoritativeStoreBlock.response.status === 403
      && authoritativeStoreBlock.payload.error === "CLIENT_POSITIVE_ECONOMY_DELTA_FORBIDDEN",
    "Client-grant isolation trusted a non-blocked JWT claim over the authoritative player mapping.");

    const canaryBefore = await authorizedJson(
      baseUrl, "GET", "/player/bootstrap", null, canaryLogin.payload.token
    );
    const controlBefore = await authorizedJson(
      baseUrl, "GET", "/player/bootstrap", null, controlLogin.payload.token
    );

    const canaryPoint = await authorizedJson(baseUrl, "POST", "/player/economy/apply", {
      delta_pos: 7,
      idempotency_key: "security-canary-positive-point",
    }, canaryLogin.payload.token);
    assert(canaryPoint.response.status === 403
      && canaryPoint.payload.error === "CLIENT_POSITIVE_ECONOMY_DELTA_FORBIDDEN",
    "Canary web user can still mint Point through the generic delta endpoint.");

    const canaryItem = await authorizedJson(baseUrl, "POST", "/player/inventory/adjust", {
      item_id: "fish_ca_com_01",
      quantity_delta: 2,
      idempotency_key: "security-canary-positive-item",
    }, canaryLogin.payload.token);
    assert(canaryItem.response.status === 403
      && canaryItem.payload.error === "CLIENT_POSITIVE_INVENTORY_DELTA_FORBIDDEN",
    "Canary web user can still mint inventory through the generic delta endpoint.");

    const canaryAfter = await authorizedJson(
      baseUrl, "GET", "/player/bootstrap", null, canaryLogin.payload.token
    );
    assert(Number(canaryAfter.payload.economy.pos) === Number(canaryBefore.payload.economy.pos),
      "Rejected canary Point grant changed the authoritative balance.");
    assert(!(canaryAfter.payload.inventory.slots || [])
      .some((slot) => slot.itemId === "fish_ca_com_01" && Number(slot.quantity) > 0),
    "Rejected canary inventory grant changed the authoritative inventory.");

    const canaryDebit = await authorizedJson(baseUrl, "POST", "/player/economy/apply", {
      delta_pos: -1,
      idempotency_key: "security-canary-debit-point",
    }, canaryLogin.payload.token);
    assert(canaryDebit.response.status === 200
      && Number(canaryDebit.payload.economy.pos) === Number(canaryBefore.payload.economy.pos) - 1,
    "Canary grant isolation blocked a legitimate debit.");

    const controlPoint = await authorizedJson(baseUrl, "POST", "/player/economy/apply", {
      delta_pos: 7,
      idempotency_key: "security-control-positive-point",
    }, controlLogin.payload.token);
    assert(controlPoint.response.status === 200
      && Number(controlPoint.payload.economy.pos) === Number(controlBefore.payload.economy.pos) + 7,
    "Canary grant isolation blocked a non-canary Point reward.");

    const controlItem = await authorizedJson(baseUrl, "POST", "/player/inventory/adjust", {
      item_id: "fish_ca_com_01",
      quantity_delta: 2,
      idempotency_key: "security-control-positive-item",
    }, controlLogin.payload.token);
    const controlFish = (controlItem.payload.inventory.slots || [])
      .find((slot) => slot.itemId === "fish_ca_com_01");
    assert(controlItem.response.status === 200 && Number(controlFish && controlFish.quantity) === 2,
      "Canary grant isolation blocked a non-canary inventory reward.");
  } finally {
    if (child.exitCode == null) child.kill("SIGTERM");
    await new Promise((resolve) => setTimeout(resolve, 200));
    removeTestDirectory(tempDir, "yw-security-canary-");
  }
}

async function main() {
  testConfigurationGate();
  testRegistrationValidation();
  await runIntegrationTest();
  await runCanaryGrantIsolationIntegrationTest();
  console.log("[security-smoke] PASS: production gate, canary grant isolation, HTTP guards, auth rate limits, and realtime limits work.");
}

main().catch((error) => {
  console.error(`[security-smoke] FAIL: ${error.message}`);
  process.exitCode = 1;
});
