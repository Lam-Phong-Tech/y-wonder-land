const { URL } = require("url");
const WebSocket = require("ws");

const DEFAULT_SHARED_ROOMS = ["city", "mine"];
const MAX_ROOM_PLAYERS = Number(process.env.REALTIME_MAX_ROOM_PLAYERS || 20);
const RESOURCE_RESPAWN_SEC = Math.max(0.1, Number(process.env.REALTIME_RESOURCE_RESPAWN_SEC || 20));
const RESOURCE_MAX_DISTANCE = Math.max(1, Number(process.env.REALTIME_RESOURCE_MAX_DISTANCE || 6));
const MAX_RESOURCES_PER_ROOM = Math.max(1, Number(process.env.REALTIME_MAX_RESOURCES_PER_ROOM || 250));
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

function attachRealtimeServer(server, options) {
  const verifyToken = options && options.verifyToken;
  const store = options && options.store;
  if (typeof verifyToken !== "function") {
    throw new Error("attachRealtimeServer requires verifyToken(token)");
  }
  if (!store || typeof store.applyResourceHarvest !== "function") {
    throw new Error("attachRealtimeServer requires store.applyResourceHarvest(...)");
  }

  const wss = new WebSocket.Server({ noServer: true });
  const clients = new Map();
  const rooms = new Map();
  const resourcesByRoom = new Map();

  function getRoomSet(room) {
    if (!rooms.has(room)) rooms.set(room, new Set());
    return rooms.get(room);
  }

  function broadcastAll(payload, except) {
    for (const ws of clients.keys()) {
      if (ws !== except) send(ws, payload);
    }
  }

  function broadcastRoom(room, payload, except) {
    const set = rooms.get(room);
    if (!set) return;
    for (const ws of set) {
      if (ws !== except) send(ws, payload);
    }
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
    setTimeout(() => {
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

      send(otherWs, {
        type: "error",
        code: "SESSION_REPLACED",
        message: "Tai khoan nay da dang nhap o thiet bi khac.",
        sentAt: nowISO(),
      });
      leaveRoom(otherWs, true);
      clients.delete(otherWs);
      try {
        otherWs.close(4008, "SESSION_REPLACED");
      } catch (e) {
        // Ignore close errors; the socket may already be closing.
      }
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

  function handleResourceHarvest(ws, msg) {
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
    const storeResult = store.applyResourceHarvest(client.playerId, rewards, {
      type: "resource_harvest",
      ref: `${client.room}:${resourceId}`,
      idempotencyKey,
      dailyLimit: resource.resourceType === "rock"
        ? { key: "mining", amount: 1, maxCount: 10 }
        : null,
    });

    if (!storeResult.ok) {
      reject(storeResult.error || "RESOURCE_REWARD_FAILED", {
        inventory: storeResult.inventory,
        daily_limits: storeResult.daily_limits,
        limit: storeResult.limit,
      });
      return;
    }

    resource.available = false;
    resource.respawnAt = Date.now() + RESOURCE_RESPAWN_SEC * 1000;
    resource.claimedBy = client.playerId;
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

    ws.on("message", (raw) => {
      let msg = null;
      try {
        msg = JSON.parse(raw.toString("utf8"));
      } catch (e) {
        send(ws, { type: "error", code: "BAD_JSON" });
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
          handleResourceHarvest(ws, msg);
          break;
        case "ping":
          send(ws, { type: "pong", sentAt: nowISO() });
          break;
        default:
          send(ws, { type: "error", code: "UNKNOWN_MESSAGE_TYPE", messageType: msg.type || "" });
          break;
      }
    });

    ws.on("close", () => {
      leaveRoom(ws);
      clients.delete(ws);
    });
  });

  server.on("upgrade", (request, socket, head) => {
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
    let auth;
    try {
      auth = verifyToken(token);
    } catch (e) {
      socket.write("HTTP/1.1 401 Unauthorized\r\n\r\n");
      socket.destroy();
      return;
    }

    wss.handleUpgrade(request, socket, head, (ws) => {
      wss.emit("connection", ws, request, auth);
    });
  });

  console.log(
    `[realtime] enabled rooms=${Array.from(SHARED_ROOMS).join(",")} maxPlayers=${MAX_ROOM_PLAYERS}`
  );

  return { wss, clients, rooms, resourcesByRoom };
}

module.exports = { attachRealtimeServer };
