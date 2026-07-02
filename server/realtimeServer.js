const { URL } = require("url");
const WebSocket = require("ws");

const DEFAULT_SHARED_ROOMS = ["city", "mine"];
const MAX_ROOM_PLAYERS = Number(process.env.REALTIME_MAX_ROOM_PLAYERS || 20);
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
  if (typeof verifyToken !== "function") {
    throw new Error("attachRealtimeServer requires verifyToken(token)");
  }

  const wss = new WebSocket.Server({ noServer: true });
  const clients = new Map();
  const rooms = new Map();

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
    client.yaw = numberOr(msg.yaw, client.yaw);
    client.animation = safeText(msg.animation || msg.anim, client.animation, 32);
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

  wss.on("connection", (ws, request, auth) => {
    const id = makeId("rt");
    clients.set(ws, {
      connectionId: id,
      userId: auth.uid || auth.userId || "",
      playerId: auth.uid || auth.userId || id,
      webUserId: auth.webUserId || "",
      name: "Player",
      gender: "male",
      room: "",
      position: { x: 0, y: 0, z: 0 },
      yaw: 0,
      animation: "Idle",
      chatTimestamps: [],
      updatedAt: nowISO(),
    });

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

  return { wss, clients, rooms };
}

module.exports = { attachRealtimeServer };
