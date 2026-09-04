const { URL } = require("url");
const WebSocket = require("ws");

const DEFAULT_SHARED_ROOMS = ["city", "mine"];
const MAX_ROOM_PLAYERS = Number(process.env.REALTIME_MAX_ROOM_PLAYERS || 20);
const RESOURCE_RESPAWN_SEC = Math.max(0.1, Number(process.env.REALTIME_RESOURCE_RESPAWN_SEC || 20));
const RESOURCE_MAX_DISTANCE = Math.max(1, Number(process.env.REALTIME_RESOURCE_MAX_DISTANCE || 6));
const MAX_RESOURCES_PER_ROOM = Math.max(1, Number(process.env.REALTIME_MAX_RESOURCES_PER_ROOM || 250));
const MAX_CONNECTIONS = Math.max(20, Number(process.env.REALTIME_MAX_CONNECTIONS || 100));
const MAX_PAYLOAD_BYTES = Math.max(1024, Number(process.env.REALTIME_MAX_PAYLOAD_BYTES || 65536));
const MESSAGE_RATE_WINDOW_MS = Math.max(1000, Number(process.env.REALTIME_MESSAGE_RATE_WINDOW_MS || 10000));
const MESSAGE_RATE_MAX = Math.max(30, Number(process.env.REALTIME_MESSAGE_RATE_MAX || 300));
const MAX_BAD_MESSAGES = Math.max(1, Number(process.env.REALTIME_MAX_BAD_MESSAGES || 3));
const GEMSTONE_REWARDS = [
  { itemId: "gem_ruby_01", displayName: "Ruby", quantity: 1, weight: 1 },
  { itemId: "gem_amethyst_01", displayName: "Amethyst", quantity: 1, weight: 2 },
  { itemId: "gem_fire_quartz_01", displayName: "Fire Quartz", quantity: 2, weight: 5 },
  { itemId: "gem_green_calcite_01", displayName: "Green Calcite", quantity: 3, weight: 12 },
  { itemId: "gem_orange_calcite_01", displayName: "Orange Calcite", quantity: 4, weight: 30 },
  { itemId: "gem_kyanite_01", displayName: "Kyanite", quantity: 4, weight: 50 },
];
const SHARED_ROOMS = new Set(
  (process.env.REALTIME_SHARED_ROOMS || DEFAULT_SHARED_ROOMS.join(","))
    .split(",")
    .map((room) => room.trim())
    .filter(Boolean)
);

function nowISO() {
  return new Date().toISOString();
}

function makeId(prefix) {
  return `${prefix}_${Date.now()}_${Math.floor(Math.random() * 1e6)}`;
}

function safeText(value, fallback, maxLength) {
  const text = String(value || fallback || "").trim();
  return text.slice(0, maxLength);
}

function numberOr(value, fallback) {
  const num = Number(value);
  return Number.isFinite(num) ? num : fallback;
}

function send(ws, payload) {
  if (ws && ws.readyState === WebSocket.OPEN) {
    ws.send(JSON.stringify(payload));
  }
}

// Gui chuoi DA serialize san. Dung cho phat theo phong: mot goi vi tri phat cho
// 99 nguoi thi truoc day JSON.stringify chay 99 lan tren cung mot object.
// Voi 100 nguoi cung gui 6,67 lan/giay, do la ~66.000 lan serialize moi giay.
function sendRaw(ws, text) {
  if (ws && ws.readyState === WebSocket.OPEN) {
    ws.send(text);
  }
}

function attachRealtimeServer(server, options) {
  const verifyToken = options && options.verifyToken;
  const store = options && options.store;
  if (typeof verifyToken !== "function") {
    throw new Error("attachRealtimeServer requires verifyToken(token)");
  }
  if (!store || typeof store.applyResourceHarvest !== "function") {
    throw new Error("attachRealtimeServer requires store.applyResourceHarvest(...)");
  }

  const wss = new WebSocket.Server({ noServer: true, maxPayload: MAX_PAYLOAD_BYTES });
  const clients = new Map();
  const rooms = new Map();
  const resourcesByRoom = new Map();

  function getRoomSet(room) {
    if (!rooms.has(room)) rooms.set(room, new Set());
    return rooms.get(room);
  }

  function broadcastAll(payload, except) {
    const text = JSON.stringify(payload); // serialize MOT lan, khong phai moi nguoi mot lan
    for (const ws of clients.keys()) {
      if (ws !== except) sendRaw(ws, text);
    }
  }

  function broadcastRoom(room, payload, except) {
    const set = rooms.get(room);
    if (!set) return;
    const text = JSON.stringify(payload); // serialize MOT lan cho ca phong
    for (const ws of set) {
      if (ws !== except) sendRaw(ws, text);
    }
  }

  function notifyPlayer(playerId, payload) {
    if (!playerId || !payload) return 0;
    let delivered = 0;
    for (const [ws, client] of clients.entries()) {
      if (!client || client.playerId !== playerId) continue;
      if (ws.readyState !== WebSocket.OPEN) continue;
      send(ws, payload);
      delivered += 1;
    }
    return delivered;
  }

  function getRoomResources(room) {
    if (!resourcesByRoom.has(room)) resourcesByRoom.set(room, new Map());
    return resourcesByRoom.get(room);
  }

  function serializeResource(resource) {
    return {
      resourceId: resource.resourceId,
      resourceType: resource.resourceType,
      position: resource.position,
      available: resource.available,
      respawnAt: resource.respawnAt > 0 ? new Date(resource.respawnAt).toISOString() : "",
      respawnInSec: resource.respawnAt > 0
        ? Math.max(0, (resource.respawnAt - Date.now()) / 1000)
        : 0,
      cycle: resource.cycle,
      updatedAt: resource.updatedAt,
    };
  }

  function resourceSnapshot(room) {
    const roomResources = resourcesByRoom.get(room);
    if (!roomResources) return [];
    return Array.from(roomResources.values()).map(serializeResource);
  }

  function scheduleResourceRespawn(room, resource) {
    const expectedCycle = resource.cycle;
    const delayMs = Math.max(1, resource.respawnAt - Date.now());
    const respawnTimer = setTimeout(() => {
      const current = resourcesByRoom.get(room)?.get(resource.resourceId);
      if (!current || current.available || current.cycle !== expectedCycle) return;

      current.available = true;
      current.respawnAt = 0;
      current.cycle += 1;
      current.claimedBy = "";
      current.claimResult = null;
      current.updatedAt = nowISO();
      broadcastRoom(room, {
        type: "resource_state",
        resource: serializeResource(current),
        sentAt: nowISO(),
      });
    }, delayMs);
    respawnTimer.unref();
  }

  function rollGemstoneReward() {
    const totalWeight = GEMSTONE_REWARDS.reduce((sum, reward) => sum + reward.weight, 0);
    let roll = Math.random() * totalWeight;
    for (const reward of GEMSTONE_REWARDS) {
      if (roll < reward.weight) return { ...reward, kind: "bonus" };
      roll -= reward.weight;
    }
    return { ...GEMSTONE_REWARDS[GEMSTONE_REWARDS.length - 1], kind: "bonus" };
  }

  function makeResourceRewards(resourceType) {
    if (resourceType === "tree") {
      return [{ itemId: "wood_01", displayName: "Wood", quantity: 10, kind: "resource" }];
    }

    return [
      { itemId: "stone_01", displayName: "Stone", quantity: 10, kind: "resource" },
      rollGemstoneReward(),
    ];
  }

  function positionDistanceSquared(a, b) {
    const dx = numberOr(a && a.x, 0) - numberOr(b && b.x, 0);
    const dy = numberOr(a && a.y, 0) - numberOr(b && b.y, 0);
    const dz = numberOr(a && a.z, 0) - numberOr(b && b.z, 0);
    return dx * dx + dy * dy + dz * dz;
  }

  function playerSnapshot(ws) {
    const client = clients.get(ws);
    if (!client) return null;
    return {
      playerId: client.playerId,
      userId: client.userId,
      name: client.name,
      room: client.room,
      gender: client.gender,
      position: client.position,
      yaw: client.yaw,
      animation: client.animation,
      animationSpeed: client.animationSpeed,
      tool: client.tool,
      updatedAt: client.updatedAt,
    };
  }

  function playersInRoom(room, except) {
    const set = rooms.get(room);
    if (!set) return [];
    const players = [];
    for (const ws of set) {
      if (ws === except) continue;
      const snapshot = playerSnapshot(ws);
      if (snapshot) players.push(snapshot);
    }
    return players;
  }

  function disconnectExistingPlayerSession(playerId, incomingWs) {
    if (!playerId) return;

    for (const [otherWs, otherClient] of clients.entries()) {
      if (otherWs === incomingWs || !otherClient || otherClient.playerId !== playerId) {
        continue;
      }

      closeClientSession(otherWs, 4008, "SESSION_REPLACED", true);
    }
  }

  function closeClientSession(ws, code, reason, notifyReplacement) {
    const client = clients.get(ws);
    if (!client) return;
    if (notifyReplacement) {
      send(ws, {
        type: "error",
        code: "SESSION_REPLACED",
        message: "Tai khoan nay da dang nhap o thiet bi khac.",
        sentAt: nowISO(),
      });
    }
    leaveRoom(ws, true);
    clients.delete(ws);
    try {
      ws.close(code, reason);
    } catch (e) {
      // Ignore close errors; the socket may already be closing.
    }
  }

  function replacePlayerSession(playerId, activeSessionId) {
    if (!playerId || !activeSessionId) return;
    for (const [ws, client] of clients.entries()) {
      if (!client || client.playerId !== playerId || client.sessionId === activeSessionId) continue;
      closeClientSession(ws, 4008, "SESSION_REPLACED", true);
    }
  }

  function disconnectPlayerSession(playerId, sessionId, code = 4001, reason = "SIGNED_OUT") {
    if (!playerId) return;
    for (const [ws, client] of clients.entries()) {
      if (!client || client.playerId !== playerId) continue;
      if (sessionId && client.sessionId !== sessionId) continue;
      closeClientSession(ws, code, reason, false);
    }
  }

  function leaveRoom(ws, notify = true) {
    const client = clients.get(ws);
    if (!client || !client.room) return;

    const oldRoom = client.room;
    const set = rooms.get(oldRoom);
    if (set) {
      set.delete(ws);
      if (set.size === 0) rooms.delete(oldRoom);
    }
    client.room = "";

    if (notify) {
      broadcastRoom(oldRoom, {
        type: "player_left",
        playerId: client.playerId,
        room: oldRoom,
        sentAt: nowISO(),
      }, ws);
    }
  }

  function joinRoom(ws, room, name, gender) {
    const client = clients.get(ws);
    if (!client) return;

    if (!SHARED_ROOMS.has(room)) {
      send(ws, { type: "error", code: "ROOM_NOT_SHARED", room });
      leaveRoom(ws);
      return;
    }

    const set = getRoomSet(room);
    if (client.room !== room && set.size >= MAX_ROOM_PLAYERS) {
      send(ws, { type: "error", code: "ROOM_FULL", room, maxPlayers: MAX_ROOM_PLAYERS });
      return;
    }

    if (client.room && client.room !== room) {
      leaveRoom(ws);
    }

    client.room = room;
    client.name = safeText(name, client.name, 32);
    client.gender = gender === "female" ? "female" : "male";
    client.updatedAt = nowISO();
    set.add(ws);

    send(ws, {
      type: "welcome",
      selfId: client.playerId,
      room,
      maxPlayers: MAX_ROOM_PLAYERS,
      players: playersInRoom(room, ws),
      resources: resourceSnapshot(room),
      sentAt: nowISO(),
    });

    broadcastRoom(room, {
      type: "player_joined",
      player: playerSnapshot(ws),
      sentAt: nowISO(),
    }, ws);
  }

  function handleState(ws, msg) {
    const client = clients.get(ws);
    if (!client || !client.room) return;

    const pos = msg.position || msg.pos || {};
    client.position = {
      x: numberOr(pos.x, client.position.x),
      y: numberOr(pos.y, client.position.y),
      z: numberOr(pos.z, client.position.z),
    };
    client.hasPosition = true;
    client.yaw = numberOr(msg.yaw, client.yaw);
    client.animation = safeText(msg.animation || msg.anim, client.animation, 32);
    client.animationSpeed = Math.min(4, Math.max(0.1, numberOr(msg.animationSpeed, client.animationSpeed)));
    client.tool = safeText(msg.tool, client.tool, 32);
    client.updatedAt = nowISO();

    broadcastRoom(client.room, {
      type: "player_state",
      playerId: client.playerId,
      name: client.name,
      room: client.room,
      gender: client.gender,
      position: client.position,
      yaw: client.yaw,
      animation: client.animation,
      animationSpeed: client.animationSpeed,
      tool: client.tool,
      sentAt: client.updatedAt,
    }, ws);
  }

  function handleChat(ws, msg) {
    const client = clients.get(ws);
    if (!client) return;

    const text = safeText(msg.message || msg.text, "", 160);
    if (!text) return;

    const now = Date.now();
    client.chatTimestamps = client.chatTimestamps.filter((ts) => now - ts < 30000);
    if (client.chatTimestamps.length >= 5) {
      send(ws, { type: "error", code: "CHAT_RATE_LIMIT", message: "Too many messages." });
      return;
    }
    client.chatTimestamps.push(now);

    broadcastAll({
      type: "chat",
      playerId: client.playerId,
      name: client.name,
      room: client.room,
      message: text,
      sentAt: nowISO(),
    });
  }

  function handleEmote(ws, msg) {
    const client = clients.get(ws);
    if (!client || !client.room) return;

    const emote = msg.emote === "Pointing" ? "Pointing" : msg.emote === "Waving" ? "Waving" : "";
    if (!emote) return;

    broadcastRoom(client.room, {
      type: "emote",
      playerId: client.playerId,
      name: client.name,
      room: client.room,
      emote,
      duration: numberOr(msg.duration, 2),
      sentAt: nowISO(),
    }, ws);
  }

  function handleResourceManifest(ws, msg) {
    const client = clients.get(ws);
    if (!client || !client.room) return;

    const incoming = Array.isArray(msg.resources) ? msg.resources.slice(0, MAX_RESOURCES_PER_ROOM) : [];
    const roomResources = getRoomResources(client.room);
    const registered = [];

    for (const item of incoming) {
      const resourceId = safeText(item && item.resourceId, "", 160);
      if (!resourceId) continue;

      const resourceType = item && item.resourceType === "tree" ? "tree" : "rock";
      const pos = item && item.position || {};
      let resource = roomResources.get(resourceId);
      if (!resource) {
        if (roomResources.size >= MAX_RESOURCES_PER_ROOM) break;
        resource = {
          resourceId,
          resourceType,
          position: {
            x: numberOr(pos.x, 0),
            y: numberOr(pos.y, 0),
            z: numberOr(pos.z, 0),
          },
          available: true,
          respawnAt: 0,
          cycle: 1,
          claimedBy: "",
          claimResult: null,
          updatedAt: nowISO(),
        };
        roomResources.set(resourceId, resource);
        broadcastRoom(client.room, {
          type: "resource_state",
          resource: serializeResource(resource),
          sentAt: nowISO(),
        }, ws);
      }
      registered.push(serializeResource(resource));
    }

    send(ws, {
      type: "resource_snapshot",
      room: client.room,
      resources: registered,
      sentAt: nowISO(),
    });
  }

  async function handleResourceHarvest(ws, msg) {
    const client = clients.get(ws);
    if (!client || !client.room) return;

    const requestId = safeText(msg.requestId, makeId("harvest"), 120);
    const resourceId = safeText(msg.resourceId, "", 160);
    const resource = resourcesByRoom.get(client.room)?.get(resourceId);

    const reject = (code, extra = {}) => send(ws, {
      type: "resource_harvest_result",
      requestId,
      resourceId,
      accepted: false,
      code,
      ...extra,
      sentAt: nowISO(),
    });

    if (!resource) {
      reject("RESOURCE_NOT_REGISTERED");
      return;
    }

    if (!resource.available) {
      if (resource.claimedBy === client.playerId && resource.claimResult) {
        send(ws, { ...resource.claimResult, requestId, duplicate: true, sentAt: nowISO() });
      } else {
        reject("RESOURCE_UNAVAILABLE", { resource: serializeResource(resource) });
      }
      return;
    }

    if (client.hasPosition &&
        positionDistanceSquared(client.position, resource.position) > RESOURCE_MAX_DISTANCE * RESOURCE_MAX_DISTANCE) {
      reject("RESOURCE_TOO_FAR");
      return;
    }

    const rewards = makeResourceRewards(resource.resourceType);
    const idempotencyKey = `resource:${client.room}:${resourceId}:${resource.cycle}:${client.playerId}`;
    resource.available = false;
    resource.claimedBy = client.playerId;
    resource.updatedAt = nowISO();

    let storeResult;
    try {
      storeResult = await store.applyResourceHarvest(client.playerId, rewards, {
        type: "resource_harvest",
        ref: `${client.room}:${resourceId}`,
        idempotencyKey,
        dailyLimit: resource.resourceType === "rock"
          ? { key: "mining", amount: 1, maxCount: 10 }
          : null,
      });
    } catch (error) {
      resource.available = true;
      resource.claimedBy = "";
      resource.updatedAt = nowISO();
      console.error(`[realtime] resource reward failed ${resourceId}:`, error);
      reject("RESOURCE_REWARD_FAILED");
      return;
    }

    if (!storeResult.ok) {
      resource.available = true;
      resource.claimedBy = "";
      resource.updatedAt = nowISO();
      reject(storeResult.error || "RESOURCE_REWARD_FAILED", {
        inventory: storeResult.inventory,
        daily_limits: storeResult.daily_limits,
        limit: storeResult.limit,
      });
      return;
    }

    resource.respawnAt = Date.now() + RESOURCE_RESPAWN_SEC * 1000;
    resource.updatedAt = nowISO();

    const accepted = {
      type: "resource_harvest_result",
      requestId,
      resourceId,
      resourceType: resource.resourceType,
      accepted: true,
      code: "OK",
      rewards: storeResult.rewards,
      inventory: storeResult.inventory,
      daily_limits: storeResult.daily_limits,
      limit: storeResult.limit,
      duplicate: Boolean(storeResult.duplicate),
      sentAt: nowISO(),
    };
    resource.claimResult = accepted;
    send(ws, accepted);
    broadcastRoom(client.room, {
      type: "resource_state",
      resource: serializeResource(resource),
      harvestedBy: client.playerId,
      sentAt: nowISO(),
    });
    scheduleResourceRespawn(client.room, resource);
  }

  wss.on("connection", (ws, request, auth) => {
    const id = makeId("rt");
    const playerId = auth.uid || auth.userId || id;
    clients.set(ws, {
      connectionId: id,
      userId: auth.uid || auth.userId || "",
      playerId,
      sessionId: auth.sid || "",
      webUserId: auth.webUserId || "",
      name: safeText(auth.displayName || auth.username || auth.name, "Player", 32),
      gender: "male",
      room: "",
      position: { x: 0, y: 0, z: 0 },
      yaw: 0,
      animation: "Idle",
      animationSpeed: 1,
      tool: "None",
      hasPosition: false,
      chatTimestamps: [],
      messageTimestamps: [],
      badMessageCount: 0,
      updatedAt: nowISO(),
    });
    disconnectExistingPlayerSession(playerId, ws);

    send(ws, {
      type: "connected",
      connectionId: id,
      sharedRooms: Array.from(SHARED_ROOMS),
      maxPlayers: MAX_ROOM_PLAYERS,
      sentAt: nowISO(),
    });

    async function processMessage(raw) {
      const client = clients.get(ws);
      if (!client) return;

      const messageNow = Date.now();
      client.messageTimestamps = client.messageTimestamps.filter(
        (timestamp) => messageNow - timestamp < MESSAGE_RATE_WINDOW_MS
      );
      if (client.messageTimestamps.length >= MESSAGE_RATE_MAX) {
        send(ws, { type: "error", code: "MESSAGE_RATE_LIMIT" });
        ws.close(1008, "Message rate exceeded");
        return;
      }
      client.messageTimestamps.push(messageNow);

      let msg = null;
      try {
        msg = JSON.parse(raw.toString("utf8"));
      } catch (e) {
        client.badMessageCount += 1;
        send(ws, { type: "error", code: "BAD_JSON" });
        if (client.badMessageCount >= MAX_BAD_MESSAGES) ws.close(1008, "Invalid messages");
        return;
      }

      if (!msg || typeof msg !== "object" || Array.isArray(msg)) {
        client.badMessageCount += 1;
        send(ws, { type: "error", code: "BAD_MESSAGE" });
        if (client.badMessageCount >= MAX_BAD_MESSAGES) ws.close(1008, "Invalid messages");
        return;
      }

      switch (msg.type) {
        case "join":
          joinRoom(ws, safeText(msg.room, "", 32), msg.name, msg.gender);
          break;
        case "leave":
          leaveRoom(ws);
          break;
        case "player_state":
          handleState(ws, msg);
          break;
        case "chat":
          handleChat(ws, msg);
          break;
        case "emote":
          handleEmote(ws, msg);
          break;
        case "resource_manifest":
          handleResourceManifest(ws, msg);
          break;
        case "resource_harvest":
          await handleResourceHarvest(ws, msg);
          break;
        case "ping":
          send(ws, { type: "pong", sentAt: nowISO() });
          break;
        default:
          send(ws, { type: "error", code: "UNKNOWN_MESSAGE_TYPE", messageType: msg.type || "" });
          break;
      }
    }

    ws.on("message", (raw) => {
      processMessage(raw).catch((error) => {
        console.error(JSON.stringify({
          event: "realtime_message_error",
          connectionId: id,
          playerId,
          errorCode: error && (error.code || error.name) || "UNKNOWN_ERROR",
        }));
        send(ws, { type: "error", code: "MESSAGE_PROCESSING_FAILED" });
      });
    });

    ws.on("error", (error) => {
      console.warn(JSON.stringify({
        event: "realtime_socket_error",
        connectionId: id,
        playerId,
        errorCode: error && (error.code || error.name) || "UNKNOWN_ERROR",
      }));
    });

    ws.on("close", () => {
      leaveRoom(ws);
      clients.delete(ws);
    });
  });

  server.on("upgrade", async (request, socket, head) => {
    if (clients.size >= MAX_CONNECTIONS) {
      socket.write("HTTP/1.1 503 Service Unavailable\r\nConnection: close\r\n\r\n");
      socket.destroy();
      return;
    }

    let url;
    try {
      url = new URL(request.url, "http://localhost");
    } catch (e) {
      socket.destroy();
      return;
    }

    if (url.pathname !== "/realtime" && url.pathname !== "/game-api/realtime") {
      socket.destroy();
      return;
    }

    const token = url.searchParams.get("token") || "";
    if (!token || token.length > 4096) {
      socket.write("HTTP/1.1 401 Unauthorized\r\nConnection: close\r\n\r\n");
      socket.destroy();
      return;
    }

    let auth;
    try {
      auth = await verifyToken(token);
    } catch (e) {
      socket.write("HTTP/1.1 401 Unauthorized\r\nConnection: close\r\n\r\n");
      socket.destroy();
      return;
    }

    wss.handleUpgrade(request, socket, head, (ws) => {
      wss.emit("connection", ws, request, auth);
    });
  });

  console.log(
    `[realtime] enabled rooms=${Array.from(SHARED_ROOMS).join(",")} maxPlayers=${MAX_ROOM_PLAYERS} maxConnections=${MAX_CONNECTIONS}`
  );

  function close(code = 1012, reason = "Server restarting") {
    for (const ws of clients.keys()) {
      try {
        ws.close(code, reason);
      } catch (error) {
        ws.terminate();
      }
    }

    const terminateTimer = setTimeout(() => {
      for (const ws of clients.keys()) ws.terminate();
    }, 1000);
    terminateTimer.unref();
    wss.close();
  }

  return {
    wss,
    clients,
    rooms,
    resourcesByRoom,
    close,
    replacePlayerSession,
    disconnectPlayerSession,
    notifyPlayer,
  };
}

module.exports = { attachRealtimeServer };
