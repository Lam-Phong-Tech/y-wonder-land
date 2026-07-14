const WebSocket = require("ws");

const baseUrl = (process.env.PHASE1_LOAD_BASE_URL || process.env.BASE_URL || "http://127.0.0.1:3000")
  .replace(/\/+$/, "");
const clientCount = boundedInteger("PHASE1_LOAD_CLIENTS", 20, 2, 20);
const batchSize = boundedInteger("PHASE1_LOAD_BATCH_SIZE", 4, 1, 20);
const holdMs = boundedInteger("PHASE1_LOAD_HOLD_MS", 2000, 0, 30000);
const room = process.env.PHASE1_LOAD_ROOM || "city";
const password = process.env.PHASE1_LOAD_PASSWORD || "LoadTest@123";
const suffix = normalizeSuffix(process.env.PHASE1_LOAD_SUFFIX || Date.now().toString(36));
const accounts = Array.from({ length: clientCount }, (_, index) => {
  const number = String(index + 1).padStart(2, "0");
  return {
    username: `Load${suffix}${number}`,
    email: `load_${suffix}_${number}@example.test`,
  };
});

function boundedInteger(name, fallback, min, max) {
  const parsed = Number(process.env[name]);
  if (!Number.isInteger(parsed)) return fallback;
  return Math.min(max, Math.max(min, parsed));
}

function normalizeSuffix(value) {
  const normalized = String(value || "")
    .replace(/[^A-Za-z0-9]/g, "")
    .slice(-14);
  if (normalized.length >= 3) return normalized;
  return `${normalized}test`.slice(0, 3);
}

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
  const startedAt = performance.now();
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

  return {
    status: response.status,
    payload,
    durationMs: performance.now() - startedAt,
    rateLimitRemaining: response.headers.get("ratelimit-remaining"),
  };
}

function getJson(path, token, allowedStatuses) {
  return requestJson("GET", path, null, token, allowedStatuses);
}

function postJson(path, body, token, allowedStatuses) {
  return requestJson("POST", path, body, token, allowedStatuses);
}

async function mapInBatches(items, mapper) {
  const results = [];
  for (let offset = 0; offset < items.length; offset += batchSize) {
    const batch = items.slice(offset, offset + batchSize);
    results.push(...await Promise.all(batch.map(mapper)));
  }
  return results;
}

async function login(account) {
  const response = await postJson("/auth/login", {
    username: account.username,
    password,
  });
  assert(response.payload.token, `Login ${account.username} did not return a token.`);
  return {
    ...account,
    token: response.payload.token,
    playerId: response.payload.playerId || response.payload.userId,
    authDurationMs: response.durationMs,
    authMode: "login",
  };
}

async function registerOrLogin(account) {
  const response = await postJson("/auth/register", {
    username: account.username,
    email: account.email,
    password,
  }, null, [200, 409]);

  if (response.status === 409) {
    return login(account);
  }

  assert(response.payload.token, `Register ${account.username} did not return a token.`);
  return {
    ...account,
    token: response.payload.token,
    playerId: response.payload.playerId || response.payload.userId,
    authDurationMs: response.durationMs,
    authMode: "register",
    registerRateLimitRemaining: response.rateLimitRemaining,
  };
}

class RealtimeClient {
  constructor(account) {
    this.account = account;
    this.messages = [];
    this.waiters = [];
    this.socket = null;
    this.closeInfo = null;
    this.connectDurationMs = 0;
  }

  connect() {
    return new Promise((resolve, reject) => {
      const startedAt = performance.now();
      const socket = new WebSocket(makeRealtimeUrl(this.account.token));
      this.socket = socket;
      const timer = setTimeout(() => {
        socket.terminate();
        reject(new Error(`Realtime connect timeout for ${this.account.username}.`));
      }, 10000);

      socket.on("open", () => {
        clearTimeout(timer);
        this.connectDurationMs = performance.now() - startedAt;
        resolve();
      });
      socket.on("message", (raw) => {
        let message;
        try {
          message = JSON.parse(raw.toString("utf8"));
        } catch {
          message = { type: "BAD_JSON" };
        }
        this.messages.push(message);
        this.flushWaiters();
      });
      socket.on("close", (code, reason) => {
        this.closeInfo = { code, reason: reason.toString("utf8") };
        this.flushWaiters();
      });
      socket.on("error", (error) => {
        clearTimeout(timer);
        reject(error);
      });
    });
  }

  send(payload) {
    assert(this.socket && this.socket.readyState === WebSocket.OPEN,
      `Socket is not open for ${this.account.username}.`);
    this.socket.send(JSON.stringify(payload));
  }

  waitUntil(predicate, label, timeoutMs = 10000) {
    if (predicate(this.messages, this.closeInfo)) return Promise.resolve();
    return new Promise((resolve, reject) => {
      const waiter = { predicate, label, resolve, reject };
      waiter.timer = setTimeout(() => {
        this.waiters = this.waiters.filter((entry) => entry !== waiter);
        reject(new Error(`Timeout waiting for ${label} on ${this.account.username}.`));
      }, timeoutMs);
      this.waiters.push(waiter);
    });
  }

  waitForMessage(predicate, label, timeoutMs = 10000) {
    return this.waitUntil((messages) => messages.some(predicate), label, timeoutMs);
  }

  flushWaiters() {
    for (const waiter of [...this.waiters]) {
      if (!waiter.predicate(this.messages, this.closeInfo)) continue;
      clearTimeout(waiter.timer);
      this.waiters = this.waiters.filter((entry) => entry !== waiter);
      waiter.resolve();
    }
  }

  close() {
    if (!this.socket) return;
    if (this.socket.readyState === WebSocket.CONNECTING) {
      this.socket.terminate();
    } else if (this.socket.readyState === WebSocket.OPEN) {
      this.socket.close(1000, "Load test complete");
    }
  }
}

function percentile(values, ratio) {
  if (values.length === 0) return 0;
  const sorted = [...values].sort((a, b) => a - b);
  const index = Math.min(sorted.length - 1, Math.ceil(sorted.length * ratio) - 1);
  return sorted[index];
}

function formatMetrics(label, values) {
  return `${label}: p50=${percentile(values, 0.5).toFixed(1)}ms ` +
    `p95=${percentile(values, 0.95).toFixed(1)}ms max=${Math.max(...values).toFixed(1)}ms`;
}

async function main() {
  const clients = [];
  console.log(`[phase1-load] Base URL: ${baseUrl}`);
  console.log(`[phase1-load] clients=${clientCount} room=${room} suffix=${suffix}`);
  console.log(`[phase1-load] account prefix: Load${suffix}`);

  try {
    const healthBefore = await getJson("/health");
    assert(healthBefore.payload && healthBefore.payload.ok === true, "Health check before load failed.");

    const authStartedAt = performance.now();
    const authenticated = await mapInBatches(accounts, registerOrLogin);
    const authWallMs = performance.now() - authStartedAt;
    const playerIds = new Set(authenticated.map((account) => account.playerId));
    assert(playerIds.size === clientCount, "Authentication returned duplicate player IDs.");

    const bootstraps = await mapInBatches(authenticated, async (account) => {
      const response = await getJson("/player/bootstrap", account.token);
      assert(response.payload && response.payload.player_profile, `Bootstrap missing profile for ${account.username}.`);
      assert(response.payload.economy, `Bootstrap missing economy for ${account.username}.`);
      assert(response.payload.inventory, `Bootstrap missing inventory for ${account.username}.`);
      assert(response.payload.farm_state, `Bootstrap missing farm_state for ${account.username}.`);
      assert(response.payload.daily_limits, `Bootstrap missing daily_limits for ${account.username}.`);
      return response.durationMs;
    });

    for (const account of authenticated) clients.push(new RealtimeClient(account));
    await Promise.all(clients.map((client) => client.connect()));
    await Promise.all(clients.map((client) =>
      client.waitForMessage((message) => message.type === "connected", "connected")));

    for (const client of clients) {
      client.send({ type: "join", room, name: client.account.username, gender: "male" });
      await client.waitForMessage(
        (message) => message.type === "welcome" && message.room === room,
        `welcome ${room}`
      );
    }

    const welcomeMessages = clients
      .flatMap((client) => client.messages.filter((message) => message.type === "welcome" && message.room === room));
    const largestWelcomeRoster = Math.max(...welcomeMessages.map((message) =>
      Array.isArray(message.players) ? message.players.length : 0));
    assert(largestWelcomeRoster === clientCount - 1,
      `Expected the final room roster to contain ${clientCount - 1} peers, got ${largestWelcomeRoster}.`);
    assert(!clients.some((client) => client.messages.some((message) => message.code === "ROOM_FULL")),
      "A client received ROOM_FULL before the room reached its configured 20-player capacity.");

    clients.forEach((client, index) => client.send({
      type: "player_state",
      position: { x: index + 1, y: 0, z: index * 2 },
      yaw: index * 10,
      animation: index % 2 === 0 ? "Walk" : "Run",
      animationSpeed: index % 2 === 0 ? 1 : 1.5,
      tool: "None",
    }));

    const observer = clients[0];
    const expectedPeerIds = new Set(authenticated.slice(1).map((account) => account.playerId));
    await observer.waitUntil((messages) => {
      const seen = new Set(messages
        .filter((message) => message.type === "player_state")
        .map((message) => message.playerId));
      return [...expectedPeerIds].every((playerId) => seen.has(playerId));
    }, `${clientCount - 1} peer state broadcasts`);

    await clients[1].waitForMessage(
      (message) => message.type === "player_state" && message.playerId === authenticated[0].playerId,
      "observer state broadcast"
    );

    const chatMessage = `load test ${suffix}`;
    clients[0].send({ type: "chat", message: chatMessage });
    await Promise.all(clients.slice(1).map((client) => client.waitForMessage(
      (message) => message.type === "chat" && message.message === chatMessage,
      "global chat broadcast"
    )));

    clients.forEach((client) => client.send({ type: "ping" }));
    await Promise.all(clients.map((client) =>
      client.waitForMessage((message) => message.type === "pong", "pong")));

    if (holdMs > 0) await new Promise((resolve) => setTimeout(resolve, holdMs));
    assert(clients.every((client) => client.socket.readyState === WebSocket.OPEN && !client.closeInfo),
      "At least one realtime connection closed during the hold period.");

    const healthAfter = await getJson("/health");
    assert(healthAfter.payload && healthAfter.payload.ok === true, "Health check after load failed.");

    const registeredCount = authenticated.filter((account) => account.authMode === "register").length;
    console.log(`[phase1-load] auth wall=${authWallMs.toFixed(1)}ms registered=${registeredCount} reused=${clientCount - registeredCount}`);
    console.log(`[phase1-load] ${formatMetrics("auth", authenticated.map((account) => account.authDurationMs))}`);
    console.log(`[phase1-load] ${formatMetrics("bootstrap", bootstraps)}`);
    console.log(`[phase1-load] ${formatMetrics("websocket-connect", clients.map((client) => client.connectDurationMs))}`);
    console.log(`[phase1-load] cleanup prefix: Load${suffix}`);
    console.log(`[phase1-load] PASS: ${clientCount} accounts authenticated and bootstrapped; ` +
      `${clientCount} WebSockets joined ${room}, exchanged state/chat/ping, and stayed connected.`);
  } finally {
    clients.forEach((client) => client.close());
  }
}

main().catch((error) => {
  console.error(`[phase1-load] FAIL: ${error.message}`);
  process.exitCode = 1;
});
