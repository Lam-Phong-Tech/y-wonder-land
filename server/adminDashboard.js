// Local developer dashboard for inspecting the JSON-backed game server.
// This is intentionally simple and dev-only. Do not expose it as a production admin panel.
const express = require("express");
const bcrypt = require("bcryptjs");
const store = require("./store");

function nowISO() {
  return new Date().toISOString();
}

function makeDefaultProfile(name) {
  return {
    version: 1,
    name: name || "Player",
    gender: "male",
    avatarId: "",
    level: 1,
    exp: 0,
    characterCreated: true,
    tutorialCompleted: true,
    createdAt: nowISO(),
    updatedAt: nowISO(),
  };
}

function normalizePlayerRows(db) {
  const legacyUsers = (db.users || []).map((user) => ({
    id: user.id,
    username: user.username,
    displayName: (db.profiles[user.id] && db.profiles[user.id].name) || user.username,
    source: "legacy",
    createdAt: user.created_at || "",
  }));

  const webPlayers = Object.values(db.players || {}).map((player) => ({
    id: player.id,
    username: player.username,
    displayName: player.displayName,
    source: player.authSource || "web",
    webUserId: player.webUserId || "",
    createdAt: player.createdAt || "",
  }));

  const seen = new Set();
  return [...legacyUsers, ...webPlayers].filter((row) => {
    if (!row.id || seen.has(row.id)) return false;
    seen.add(row.id);
    return true;
  });
}

function deletePlayerState(db, playerId) {
  db.users = (db.users || []).filter((user) => user.id !== playerId);
  delete db.profiles[playerId];
  delete db.economies[playerId];
  delete db.inventories[playerId];
  delete db.farmStates[playerId];
  delete db.dailyLimits[playerId];
  delete db.players[playerId];

  for (const [webUserId, mappedPlayerId] of Object.entries(db.playersByWebUserId || {})) {
    if (mappedPlayerId === playerId) {
      delete db.playersByWebUserId[webUserId];
    }
  }

  db.transactions = (db.transactions || []).filter((tx) => tx.playerId !== playerId);
  return db;
}

function buildHtml() {
  return `<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>Y Wonder Green Farm - Backend Admin</title>
  <style>
    :root {
      --bg: #111827;
      --panel: #1f2937;
      --panel2: #263244;
      --text: #f9fafb;
      --muted: #9ca3af;
      --line: #374151;
      --accent: #facc15;
      --danger: #ef4444;
      --ok: #22c55e;
      --blue: #38bdf8;
    }

    * { box-sizing: border-box; }
    body {
      margin: 0;
      font-family: Segoe UI, Arial, sans-serif;
      background: var(--bg);
      color: var(--text);
    }
    header {
      padding: 18px 24px;
      background: #0f172a;
      border-bottom: 1px solid var(--line);
      display: flex;
      justify-content: space-between;
      gap: 16px;
      align-items: center;
    }
    h1 { margin: 0; font-size: 22px; }
    h2 { margin: 0 0 12px; font-size: 16px; }
    main {
      display: grid;
      grid-template-columns: 360px 1fr;
      min-height: calc(100vh - 73px);
    }
    aside, section { padding: 18px; }
    aside {
      border-right: 1px solid var(--line);
      background: #151f30;
    }
    .warning {
      color: #fde68a;
      font-size: 13px;
      line-height: 1.45;
    }
    .grid {
      display: grid;
      grid-template-columns: repeat(4, minmax(120px, 1fr));
      gap: 12px;
      margin-bottom: 16px;
    }
    .card {
      background: var(--panel);
      border: 1px solid var(--line);
      border-radius: 10px;
      padding: 14px;
    }
    .metric { color: var(--muted); font-size: 12px; }
    .metric strong { display: block; color: var(--text); font-size: 24px; margin-top: 6px; }
    label { display: block; color: var(--muted); font-size: 12px; margin-bottom: 6px; }
    input, select, textarea {
      width: 100%;
      color: var(--text);
      background: #0f172a;
      border: 1px solid var(--line);
      border-radius: 8px;
      padding: 10px;
      font-family: Consolas, monospace;
    }
    textarea {
      min-height: 260px;
      resize: vertical;
      line-height: 1.35;
    }
    button {
      border: 0;
      border-radius: 8px;
      padding: 10px 12px;
      color: #111827;
      background: var(--accent);
      font-weight: 700;
      cursor: pointer;
    }
    button.secondary { background: var(--blue); }
    button.danger { background: var(--danger); color: #fff; }
    button.ghost { background: #334155; color: var(--text); }
    button:disabled { opacity: .45; cursor: not-allowed; }
    .row { display: flex; gap: 8px; align-items: center; }
    .stack { display: grid; gap: 10px; }
    .players {
      display: grid;
      gap: 8px;
      margin-top: 12px;
      max-height: 45vh;
      overflow: auto;
    }
    .player {
      text-align: left;
      color: var(--text);
      background: var(--panel2);
      border: 1px solid var(--line);
      border-radius: 8px;
      padding: 10px;
      cursor: pointer;
    }
    .player.active { border-color: var(--accent); }
    .player small { display: block; color: var(--muted); margin-top: 4px; }
    .tabs {
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
      margin-bottom: 12px;
    }
    .tabs button { color: var(--text); background: #334155; }
    .tabs button.active { color: #111827; background: var(--accent); }
    .status { min-height: 20px; color: var(--muted); font-size: 13px; }
    .status.ok { color: var(--ok); }
    .status.err { color: #fca5a5; }
    pre {
      max-height: 220px;
      overflow: auto;
      background: #0f172a;
      border: 1px solid var(--line);
      border-radius: 8px;
      padding: 12px;
      color: #d1d5db;
    }
    @media (max-width: 1000px) {
      main { grid-template-columns: 1fr; }
      aside { border-right: 0; border-bottom: 1px solid var(--line); }
      .grid { grid-template-columns: repeat(2, minmax(120px, 1fr)); }
    }
  </style>
</head>
<body>
  <header>
    <div>
      <h1>Y Wonder Green Farm - Backend Admin</h1>
      <div class="warning">Dev/local dashboard for demo and inspection. Do not expose this as production admin.</div>
    </div>
    <button class="ghost" onclick="loadDashboard()">Refresh</button>
  </header>
  <main>
    <aside>
      <div class="card stack">
        <h2>Create Demo User</h2>
        <div>
          <label>Username</label>
          <input id="newUsername" value="DemoAdmin01" />
        </div>
        <div>
          <label>Password</label>
          <input id="newPassword" value="12345678" />
        </div>
        <button onclick="createUser()">Create user</button>
      </div>

      <div class="card" style="margin-top: 14px;">
        <h2>Players</h2>
        <input id="playerSearch" placeholder="Search player..." oninput="renderPlayers()" />
        <div id="players" class="players"></div>
      </div>
    </aside>

    <section>
      <div class="grid">
        <div class="card metric">Players<strong id="mPlayers">0</strong></div>
        <div class="card metric">Profiles<strong id="mProfiles">0</strong></div>
        <div class="card metric">Inventory Slots<strong id="mItems">0</strong></div>
        <div class="card metric">Transactions<strong id="mTx">0</strong></div>
      </div>

      <div class="card">
        <div class="row" style="justify-content: space-between; margin-bottom: 12px;">
          <h2 id="selectedTitle">Select a player</h2>
          <button class="danger" id="deleteBtn" onclick="deletePlayer()" disabled>Delete player</button>
        </div>
        <div class="tabs" id="tabs"></div>
        <textarea id="editor" spellcheck="false" disabled></textarea>
        <div class="row" style="margin-top: 10px;">
          <button id="saveBtn" onclick="saveSection()" disabled>Save section</button>
          <button class="secondary" id="reloadBtn" onclick="selectPlayer(selectedPlayerId)" disabled>Reload player</button>
        </div>
        <div id="status" class="status" style="margin-top: 10px;"></div>
      </div>

      <div class="card" style="margin-top: 14px;">
        <h2>Raw Database Snapshot</h2>
        <pre id="raw">{}</pre>
      </div>
    </section>
  </main>

  <script>
    let db = {};
    let players = [];
    let selectedPlayerId = "";
    let selectedState = null;
    let selectedSection = "profile";

    const sections = ["profile", "economy", "inventory", "dailyLimits", "farmState"];

    function setStatus(message, kind) {
      const el = document.getElementById("status");
      el.textContent = message || "";
      el.className = "status " + (kind || "");
    }

    async function requestJson(url, options) {
      const response = await fetch(url, {
        headers: { "Content-Type": "application/json" },
        ...(options || {})
      });
      const text = await response.text();
      const data = text ? JSON.parse(text) : null;
      if (!response.ok) {
        throw new Error((data && (data.error || data.message)) || ("HTTP " + response.status));
      }
      return data;
    }

    async function loadDashboard() {
      setStatus("Loading...", "");
      const data = await requestJson("/admin/api/db");
      db = data.db;
      players = data.players;
      renderMetrics();
      renderPlayers();
      document.getElementById("raw").textContent = JSON.stringify(db, null, 2);
      setStatus("Loaded", "ok");
      if (selectedPlayerId) {
        await selectPlayer(selectedPlayerId);
      }
    }

    function renderMetrics() {
      const inventorySlots = Object.values(db.inventories || {})
        .reduce((sum, inv) => sum + ((inv.slots || []).length), 0);
      document.getElementById("mPlayers").textContent = players.length;
      document.getElementById("mProfiles").textContent = Object.keys(db.profiles || {}).length;
      document.getElementById("mItems").textContent = inventorySlots;
      document.getElementById("mTx").textContent = (db.transactions || []).length;
    }

    function renderPlayers() {
      const query = document.getElementById("playerSearch").value.toLowerCase();
      const root = document.getElementById("players");
      root.innerHTML = "";
      players
        .filter(p => !query || JSON.stringify(p).toLowerCase().includes(query))
        .forEach((player) => {
          const div = document.createElement("div");
          div.className = "player" + (player.id === selectedPlayerId ? " active" : "");
          div.onclick = () => selectPlayer(player.id);
          div.innerHTML = "<strong>" + escapeHtml(player.displayName || player.username || player.id) + "</strong>" +
            "<small>" + escapeHtml(player.id) + "</small>" +
            "<small>" + escapeHtml(player.source || "") + " " + escapeHtml(player.webUserId || "") + "</small>";
          root.appendChild(div);
        });
    }

    function escapeHtml(value) {
      return String(value || "").replace(/[&<>"']/g, (ch) => ({
        "&": "&amp;",
        "<": "&lt;",
        ">": "&gt;",
        '"': "&quot;",
        "'": "&#039;"
      }[ch]));
    }

    async function createUser() {
      const username = document.getElementById("newUsername").value.trim();
      const password = document.getElementById("newPassword").value;
      if (!username || !password) return setStatus("Username/password required", "err");
      const result = await requestJson("/admin/api/users", {
        method: "POST",
        body: JSON.stringify({ username, password })
      });
      await loadDashboard();
      await selectPlayer(result.userId);
      setStatus("Created " + result.userId, "ok");
    }

    async function selectPlayer(playerId) {
      selectedPlayerId = playerId;
      selectedState = await requestJson("/admin/api/players/" + encodeURIComponent(playerId));
      const player = players.find(p => p.id === playerId) || { id: playerId };
      document.getElementById("selectedTitle").textContent =
        (player.displayName || player.username || "Player") + " - " + playerId;
      document.getElementById("deleteBtn").disabled = false;
      document.getElementById("saveBtn").disabled = false;
      document.getElementById("reloadBtn").disabled = false;
      document.getElementById("editor").disabled = false;
      renderPlayers();
      renderTabs();
      renderEditor();
    }

    function renderTabs() {
      const tabs = document.getElementById("tabs");
      tabs.innerHTML = "";
      sections.forEach((section) => {
        const button = document.createElement("button");
        button.textContent = section;
        button.className = section === selectedSection ? "active" : "";
        button.onclick = () => {
          selectedSection = section;
          renderTabs();
          renderEditor();
        };
        tabs.appendChild(button);
      });
    }

    function renderEditor() {
      if (!selectedState) return;
      document.getElementById("editor").value = JSON.stringify(selectedState[selectedSection] || {}, null, 2);
    }

    async function saveSection() {
      if (!selectedPlayerId || !selectedSection) return;
      let parsed;
      try {
        parsed = JSON.parse(document.getElementById("editor").value || "{}");
      } catch (e) {
        return setStatus("Invalid JSON: " + e.message, "err");
      }

      const result = await requestJson(
        "/admin/api/players/" + encodeURIComponent(selectedPlayerId) + "/" + selectedSection,
        { method: "PUT", body: JSON.stringify({ value: parsed }) }
      );
      selectedState = result.player;
      await loadDashboard();
      setStatus("Saved " + selectedSection, "ok");
    }

    async function deletePlayer() {
      if (!selectedPlayerId) return;
      if (!confirm("Delete player " + selectedPlayerId + " from local JSON store?")) return;
      await requestJson("/admin/api/players/" + encodeURIComponent(selectedPlayerId), { method: "DELETE" });
      selectedPlayerId = "";
      selectedState = null;
      document.getElementById("selectedTitle").textContent = "Select a player";
      document.getElementById("editor").value = "";
      document.getElementById("editor").disabled = true;
      document.getElementById("deleteBtn").disabled = true;
      document.getElementById("saveBtn").disabled = true;
      document.getElementById("reloadBtn").disabled = true;
      document.getElementById("tabs").innerHTML = "";
      await loadDashboard();
      setStatus("Player deleted", "ok");
    }

    loadDashboard().catch(e => setStatus(e.message, "err"));
  </script>
</body>
</html>`;
}

function createAdminDashboardRouter() {
  const router = express.Router();

  router.get("/", (req, res) => {
    res.type("html").send(buildHtml());
  });

  router.get("/api/db", (req, res) => {
    try {
      const db = store.readAll();
      res.json({ ok: true, storeMode: store.mode, players: normalizePlayerRows(db), db });
    } catch (e) {
      res.status(500).json({ error: e.message });
    }
  });

  router.post("/api/users", (req, res) => {
    const body = req.body || {};
    const username = String(body.username || "").trim();
    const password = String(body.password || "");
    if (!username || !password) {
      return res.status(400).json({ error: "MISSING_USERNAME_OR_PASSWORD" });
    }

    if (store.findUserByName(username)) {
      return res.status(409).json({ error: "USERNAME_EXISTS" });
    }

    const userId = "u_" + Date.now() + "_" + Math.floor(Math.random() * 1e6);
    store.createUser({
      id: userId,
      username,
      password_hash: bcrypt.hashSync(password, 8),
      created_at: nowISO(),
    });
    store.setProfile(userId, makeDefaultProfile(username));

    res.json({ ok: true, userId });
  });

  router.get("/api/players/:playerId", (req, res) => {
    const playerId = req.params.playerId;
    store.ensurePlayerState(playerId);
    const db = store.readAll();
    res.json({
      ok: true,
      playerId,
      profile: db.profiles[playerId] || null,
      economy: db.economies[playerId] || null,
      inventory: db.inventories[playerId] || null,
      dailyLimits: db.dailyLimits[playerId] || null,
      farmState: db.farmStates[playerId] || null,
      transactions: (db.transactions || []).filter((tx) => tx.playerId === playerId),
    });
  });

  router.put("/api/players/:playerId/:section", (req, res) => {
    const playerId = req.params.playerId;
    const section = req.params.section;
    const value = req.body && req.body.value;
    if (!value || typeof value !== "object") {
      return res.status(400).json({ error: "VALUE_OBJECT_REQUIRED" });
    }

    const db = store.readAll();
    if (section === "profile") db.profiles[playerId] = { ...value, updatedAt: nowISO() };
    else if (section === "economy") db.economies[playerId] = { ...value, updatedAt: nowISO() };
    else if (section === "inventory") db.inventories[playerId] = { ...value, updatedAt: nowISO() };
    else if (section === "dailyLimits") db.dailyLimits[playerId] = { ...value, updatedAt: nowISO() };
    else if (section === "farmState") db.farmStates[playerId] = { ...value, updatedAt: nowISO() };
    else return res.status(404).json({ error: "UNKNOWN_SECTION" });

    store.writeAll(db);
    const refreshed = store.readAll();
    res.json({
      ok: true,
      player: {
        profile: refreshed.profiles[playerId] || null,
        economy: refreshed.economies[playerId] || null,
        inventory: refreshed.inventories[playerId] || null,
        dailyLimits: refreshed.dailyLimits[playerId] || null,
        farmState: refreshed.farmStates[playerId] || null,
      },
    });
  });

  router.delete("/api/players/:playerId", (req, res) => {
    const db = store.readAll();
    deletePlayerState(db, req.params.playerId);
    store.writeAll(db);
    res.json({ ok: true });
  });

  return router;
}

module.exports = {
  createAdminDashboardRouter,
};
