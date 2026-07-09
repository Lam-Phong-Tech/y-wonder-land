const WebSocket = require("ws");

const baseUrl = (process.env.PHASE1_TEST_BASE_URL || process.env.BASE_URL || "http://127.0.0.1:3000").replace(/\/+$/, "");
const suffix = (process.env.PHASE1_TEST_SUFFIX || Date.now().toString(36).slice(-6)).toLowerCase();
const password = process.env.PHASE1_TEST_PASSWORD || "Phase1@123";
const accounts = [
  { username: `P1A_${suffix}`, email: `p1a_${suffix}@example.test` },
  { username: `P1B_${suffix}`, email: `p1b_${suffix}@example.test` },
];

function assert(condition, message) {
  if (!condition) throw new Error(message);
}

function makeHttpUrl(path) {
  return `${baseUrl}${path.startsWith("/") ? path : `/${path}`}`;
}

function makeRealtimeUrl(token) {
  const url = new URL(makeHttpUrl("/realtime"));
  url.protocol = url.protocol === "https:" ? "wss:" : "ws:";
  url.searchParams.set("token", token);
  return url.toString();
}

async function requestJson(method, path, body, token, allowedStatuses = [200]) {
  const headers = { "Content-Type": "application/json" };
  if (token) headers.Authorization = `Bearer ${token}`;

  const response = await fetch(makeHttpUrl(path), {
    method,
    headers,
    body: body == null ? undefined : JSON.stringify(body),
  });

  let payload = null;
  try {
    payload = await response.json();
  } catch {
    payload = null;
  }

  if (!allowedStatuses.includes(response.status)) {
    const error = payload && (payload.error || payload.message);
    throw new Error(`${method} ${path} failed ${response.status}: ${error || "UNKNOWN_ERROR"}`);
  }

  return { status: response.status, payload };
}

async function getJson(path, token, allowedStatuses) {
  return requestJson("GET", path, null, token, allowedStatuses);
}

async function postJson(path, body, token, allowedStatuses) {
  return requestJson("POST", path, body, token, allowedStatuses);
}

async function putJson(path, body, token, allowedStatuses) {
  return requestJson("PUT", path, body, token, allowedStatuses);
}

async function register(account) {
  const { payload } = await postJson("/auth/register", {
    username: account.username,
    email: account.email,
    password,
  });
  assert(payload.token, `Register ${account.username} missing token.`);
  assert(payload.userId || payload.playerId, `Register ${account.username} missing userId/playerId.`);
  return { ...account, token: payload.token, playerId: payload.playerId || payload.userId };
}

async function login(account, loginPassword = password, allowedStatuses) {
  const { status, payload } = await postJson("/auth/login", {
    username: account.username,
    password: loginPassword,
  }, null, allowedStatuses);
  if (status !== 200) return { status, payload };
  assert(payload.token, `Login ${account.username} missing token.`);
  return { ...account, token: payload.token, playerId: payload.playerId || payload.userId };
}

class RealtimeClient {
  constructor(account) {
    this.account = account;
    this.messages = [];
    this.waiters = [];
    this.socket = null;
  }

  connect() {
    return new Promise((resolve, reject) => {
      const socket = new WebSocket(makeRealtimeUrl(this.account.token));
      this.socket = socket;
      const timer = setTimeout(() => {
        socket.close();
        reject(new Error(`Realtime connect timeout for ${this.account.username}`));
      }, 5000);

      socket.on("open", () => {
        clearTimeout(timer);
        resolve();
      });
      socket.on("message", (raw) => {
        let message;
        try {
          message = JSON.parse(raw.toString("utf8"));
        } catch {
          message = { type: "BAD_JSON", raw: raw.toString("utf8") };
        }
        this.messages.push(message);
        this.flushWaiters();
      });
      socket.on("error", (error) => {
        clearTimeout(timer);
        reject(error);
      });
    });
  }

  send(payload) {
    assert(this.socket && this.socket.readyState === WebSocket.OPEN, `Socket not open for ${this.account.username}.`);
    this.socket.send(JSON.stringify(payload));
  }

  waitFor(predicate, label, timeoutMs = 5000) {
    const existing = this.messages.find(predicate);
    if (existing) return Promise.resolve(existing);

    return new Promise((resolve, reject) => {
      const waiter = { predicate, label, resolve, reject };
      waiter.timer = setTimeout(() => {
        this.waiters = this.waiters.filter((item) => item !== waiter);
        reject(new Error(`Timeout waiting for ${label} on ${this.account.username}`));
      }, timeoutMs);
      this.waiters.push(waiter);
    });
  }

  flushWaiters() {
    for (const waiter of [...this.waiters]) {
      const match = this.messages.find(waiter.predicate);
      if (!match) continue;
      clearTimeout(waiter.timer);
      this.waiters = this.waiters.filter((item) => item !== waiter);
      waiter.resolve(match);
    }
  }

  close() {
    if (this.socket) this.socket.close();
  }
}

async function testPersistence(account) {
  const start = await getJson("/player/bootstrap", account.token);
  const startPos = Number(start.payload.economy && start.payload.economy.pos);
  assert(Number.isFinite(startPos), "Bootstrap economy.pos is not numeric.");

  const economyKey = `phase1-economy-${suffix}`;
  const firstEconomy = await postJson("/player/economy/apply", {
    delta_pos: 123,
    delta_upos: 0,
    type: "phase1_smoke",
    ref: suffix,
    idempotency_key: economyKey,
  }, account.token);
  const duplicateEconomy = await postJson("/player/economy/apply", {
    delta_pos: 123,
    type: "phase1_smoke",
    ref: suffix,
    idempotency_key: economyKey,
  }, account.token);
  assert(duplicateEconomy.payload.duplicate === true, "Economy idempotency did not mark duplicate.");
  assert(Number(firstEconomy.payload.economy.pos) === startPos + 123, "Economy delta did not apply once.");

  const inventoryKey = `phase1-inventory-${suffix}`;
  await postJson("/player/inventory/adjust", {
    item_id: "phase1_test_item",
    quantity_delta: 2,
    type: "phase1_smoke",
    idempotency_key: inventoryKey,
  }, account.token);
  const duplicateInventory = await postJson("/player/inventory/adjust", {
    item_id: "phase1_test_item",
    quantity_delta: 2,
    type: "phase1_smoke",
    idempotency_key: inventoryKey,
  }, account.token);
  assert(duplicateInventory.payload.duplicate === true, "Inventory idempotency did not mark duplicate.");

  await putJson("/player/farm-state", {
    farm_state: {
      version: 1,
      phase1SmokeMarker: suffix,
      note: "server persisted this farm marker",
    },
  }, account.token);

  const relogged = await login(account);
  const boot = await getJson("/player/bootstrap", relogged.token);
  const slot = (boot.payload.inventory.slots || []).find((item) => item.itemId === "phase1_test_item");
  assert(Number(boot.payload.economy.pos) === startPos + 123, "Relogin bootstrap did not keep economy.");
  assert(slot && slot.quantity === 2, "Relogin bootstrap did not keep inventory.");
  assert(boot.payload.farm_state.phase1SmokeMarker === suffix, "Relogin bootstrap did not keep farm_state.");
  return relogged;
}

async function testRealtime(a, b) {
  const clientA = new RealtimeClient(a);
  const clientB = new RealtimeClient(b);
  try {
    await Promise.all([clientA.connect(), clientB.connect()]);
    await Promise.all([
      clientA.waitFor((msg) => msg.type === "connected", "connected"),
      clientB.waitFor((msg) => msg.type === "connected", "connected"),
    ]);
    clientA.send({ type: "join", room: "city", name: a.username, gender: "male" });
    clientB.send({ type: "join", room: "city", name: b.username, gender: "female" });
    await Promise.all([
      clientA.waitFor((msg) => msg.type === "welcome" && msg.room === "city", "welcome city"),
      clientB.waitFor((msg) => msg.type === "welcome" && msg.room === "city", "welcome city"),
    ]);

    const chatText = `phase1 hello ${suffix}`;
    clientA.send({ type: "chat", message: chatText });
    await clientB.waitFor((msg) => msg.type === "chat" && msg.message === chatText, "chat broadcast");
  } finally {
    clientA.close();
    clientB.close();
  }
}

async function main() {
  console.log(`[phase1-smoke] Base URL: ${baseUrl}`);
  const health = await getJson("/health");
  assert(health.payload.ok === true, "Health check failed.");

  const registered = [];
  for (const account of accounts) {
    registered.push(await register(account));
  }

  const duplicate = await postJson("/auth/register", {
    username: accounts[0].username,
    email: `other_${suffix}@example.test`,
    password,
  }, null, [409]);
  assert(duplicate.payload.error === "USERNAME_EXISTS", "Duplicate username did not return USERNAME_EXISTS.");

  const badPassword = await login(accounts[0], "Wrong@123", [401]);
  assert(badPassword.status === 401, "Wrong password did not return 401.");

  const loggedA = await testPersistence(registered[0]);
  const loggedB = await login(registered[1]);
  await testRealtime(loggedA, loggedB);

  console.log("[phase1-smoke] PASS: register, login, bootstrap persistence, idempotency, farm-state, and realtime chat work.");
}

main().catch((error) => {
  console.error(`[phase1-smoke] FAIL: ${error.message}`);
  process.exitCode = 1;
});
