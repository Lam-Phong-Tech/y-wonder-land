const WebSocket = require("ws");

const baseUrl = (process.env.REALTIME_TEST_BASE_URL || process.env.BASE_URL || "http://127.0.0.1:3000").replace(/\/+$/, "");
const room = process.env.REALTIME_TEST_ROOM || "city";
const password = process.env.REALTIME_TEST_PASSWORD || "demo";
const authPath = process.env.REALTIME_TEST_AUTH_PATH || "/auth/web-login";
const accounts = (process.env.REALTIME_TEST_ACCOUNTS || "DemoRealtime01,DemoRealtime02,DemoRealtime03")
  .split(",")
  .map((value) => value.trim())
  .filter(Boolean);

if (accounts.length < 2) {
  throw new Error("REALTIME_TEST_ACCOUNTS must contain at least two usernames.");
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

async function postJson(path, body) {
  const response = await fetch(makeHttpUrl(path), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body || {}),
  });

  let payload = null;
  try {
    payload = await response.json();
  } catch {
    payload = null;
  }

  if (!response.ok) {
    const error = payload && (payload.error || payload.message);
    throw new Error(`${path} failed ${response.status}: ${error || "UNKNOWN_ERROR"}`);
  }

  return payload;
}

async function login(username) {
  const payload = await postJson(authPath, { username, password });
  if (!payload.token || !payload.playerId) {
    throw new Error(`Login response for ${username} is missing token/playerId.`);
  }

  return {
    username,
    token: payload.token,
    playerId: payload.playerId,
    webUserId: payload.webUserId,
  };
}

class RealtimeTestClient {
  constructor(account) {
    this.account = account;
    this.messages = [];
    this.waiters = [];
    this.closeInfo = null;
    this.closeWaiters = [];
    this.socket = null;
  }

  connect() {
    return new Promise((resolve, reject) => {
      const socket = new WebSocket(makeRealtimeUrl(this.account.token));
      this.socket = socket;

      const timer = setTimeout(() => {
        reject(new Error(`Realtime connect timeout for ${this.account.username}`));
        socket.close();
      }, 5000);

      socket.on("open", () => {
        clearTimeout(timer);
        resolve();
      });

      socket.on("message", (raw) => {
        let message = null;
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

      socket.on("close", (code, reason) => {
        this.closeInfo = { code, reason: reason.toString("utf8") };
        for (const waiter of this.closeWaiters.splice(0)) {
          clearTimeout(waiter.timer);
          waiter.resolve(this.closeInfo);
        }
      });
    });
  }

  send(payload) {
    if (!this.socket || this.socket.readyState !== WebSocket.OPEN) {
      throw new Error(`Socket is not open for ${this.account.username}.`);
    }

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

  waitForClose(label, timeoutMs = 5000) {
    if (this.closeInfo) return Promise.resolve(this.closeInfo);

    return new Promise((resolve, reject) => {
      const waiter = { resolve, reject };
      waiter.timer = setTimeout(() => {
        this.closeWaiters = this.closeWaiters.filter((item) => item !== waiter);
        reject(new Error(`Timeout waiting for ${label} on ${this.account.username}`));
      }, timeoutMs);
      this.closeWaiters.push(waiter);
    });
  }

  close() {
    if (this.socket) this.socket.close();
  }
}

async function testSingleAccountSession(account) {
  const first = new RealtimeTestClient(account);
  const replacement = new RealtimeTestClient(account);
  try {
    await first.connect();
    await first.waitFor((msg) => msg.type === "connected", "first same-account connection");

    await replacement.connect();
    await replacement.waitFor((msg) => msg.type === "connected", "replacement same-account connection");
    await first.waitFor(
      (msg) => msg.type === "error" && msg.code === "SESSION_REPLACED",
      "SESSION_REPLACED"
    );
    const close = await first.waitForClose("same-account close 4008");
    if (close.code !== 4008) {
      throw new Error(`Expected same-account close code 4008, got ${close.code}.`);
    }
  } finally {
    first.close();
    replacement.close();
  }
}

async function main() {
  console.log(`[realtime-smoke] Base URL: ${baseUrl}`);
  console.log(`[realtime-smoke] Accounts: ${accounts.join(", ")}`);
  console.log(`[realtime-smoke] Auth path: ${authPath}`);

  const first = await login(accounts[0]);
  const second = await login(accounts[1]);
  const third = accounts[2] ? await login(accounts[2]) : null;
  const loginSummary = [first, second, third]
    .filter(Boolean)
    .map((account) => `${account.username}=${account.playerId}`)
    .join(", ");
  console.log(`[realtime-smoke] Login ok: ${loginSummary}`);

  const clientA = new RealtimeTestClient(first);
  const clientB = new RealtimeTestClient(second);
  const clientC = third ? new RealtimeTestClient(third) : null;

  try {
    await clientA.connect();
    await clientB.connect();
    if (clientC) await clientC.connect();

    const connectedWaits = [
      clientA.waitFor((msg) => msg.type === "connected", "connected"),
      clientB.waitFor((msg) => msg.type === "connected", "connected"),
    ];
    if (clientC) connectedWaits.push(clientC.waitFor((msg) => msg.type === "connected", "connected"));
    await Promise.all(connectedWaits);

    clientA.send({ type: "join", room, name: first.username, gender: "male" });
    await clientA.waitFor((msg) => msg.type === "welcome" && msg.room === room, `welcome ${room}`);

    clientB.send({ type: "join", room, name: second.username, gender: "female" });
    const welcomeB = await clientB.waitFor((msg) => msg.type === "welcome" && msg.room === room, `welcome ${room}`);
    if (!Array.isArray(welcomeB.players) || !welcomeB.players.some((player) => player.playerId === first.playerId)) {
      throw new Error("Second client welcome packet does not include first player.");
    }

    await clientA.waitFor((msg) => msg.type === "player_joined" && msg.player && msg.player.playerId === second.playerId, "player_joined");

    const chatText = `hello-${Date.now()}`;
    clientA.send({ type: "chat", message: chatText });
    await clientB.waitFor((msg) => msg.type === "chat" && msg.playerId === first.playerId && msg.message === chatText, "chat from first client");
    if (clientC) {
      await clientC.waitFor((msg) => msg.type === "chat" && msg.playerId === first.playerId && msg.message === chatText, "global chat to no-room client");

      const noRoomChatText = `global-no-room-${Date.now()}`;
      clientC.send({ type: "chat", message: noRoomChatText });
      await clientA.waitFor(
        (msg) => msg.type === "chat" && msg.playerId === third.playerId && msg.name === third.username && msg.message === noRoomChatText,
        "global chat from no-room client"
      );
    }

    clientB.send({
      type: "player_state",
      position: { x: 1, y: 2, z: 3 },
      yaw: 45,
      animation: "Walk",
    });
    await clientA.waitFor((msg) => msg.type === "player_state" && msg.playerId === second.playerId && msg.animation === "Walk", "player_state from second client");

    clientA.send({ type: "join", room: "farm", name: first.username, gender: "male" });
    await clientA.waitFor((msg) => msg.type === "error" && msg.code === "ROOM_NOT_SHARED" && msg.room === "farm", "farm rejected");

    console.log("[realtime-smoke] PASS: auth, join, welcome, global chat, player_state, and farm rejection all work.");
  } finally {
    clientA.close();
    clientB.close();
    if (clientC) clientC.close();
  }

  await testSingleAccountSession(first);
  console.log("[realtime-smoke] PASS: same-account replacement closes the older socket with code 4008.");
}

main().catch((error) => {
  console.error(`[realtime-smoke] FAIL: ${error.message}`);
  process.exitCode = 1;
});
