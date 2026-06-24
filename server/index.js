// index.js — Server stub REST cho YWONDERLAND (Đợt 1: Profile + Tutorial).
// CHỈ dùng cho dev/test ở local. KHÔNG bảo mật cho production (JWT secret cứng, không rate-limit).
const express = require("express");
const cors = require("cors");
const bcrypt = require("bcryptjs");
const jwt = require("jsonwebtoken");
const store = require("./store");

const PORT = process.env.PORT || 3000;
const JWT_SECRET = process.env.JWT_SECRET || "ywonderland_dev_secret_change_me";
const TOKEN_TTL = "30d";

const app = express();
app.use(cors());
app.use(express.json());

// ── Helpers ──
function nowISO() {
  return new Date().toISOString();
}

function makeDefaultProfile() {
  return {
    version: 1,
    name: "Player",
    gender: "male",
    avatarId: "",
    level: 1,
    exp: 0.0,
    tutorialCompleted: false,
    createdAt: nowISO(),
    updatedAt: nowISO(),
  };
}

function signToken(userId) {
  return jwt.sign({ uid: userId }, JWT_SECRET, { expiresIn: TOKEN_TTL });
}

// Middleware: xác thực Bearer token -> gắn req.userId
function auth(req, res, next) {
  const header = req.headers["authorization"] || "";
  const token = header.startsWith("Bearer ") ? header.slice(7) : null;
  if (!token) return res.status(401).json({ error: "Thiếu token" });
  try {
    const payload = jwt.verify(token, JWT_SECRET);
    req.userId = payload.uid;
    next();
  } catch (e) {
    return res.status(401).json({ error: "Token không hợp lệ" });
  }
}

// ── Health check ──
app.get("/", (req, res) => res.json({ ok: true, service: "ywonderland-stub" }));

// ── Auth ──
app.post("/auth/register", (req, res) => {
  const { username, password } = req.body || {};
  if (!username || !password)
    return res.status(400).json({ error: "Thiếu username/password" });
  if (store.findUserByName(username))
    return res.status(409).json({ error: "Username đã tồn tại" });

  const id = "u_" + Date.now() + "_" + Math.floor(Math.random() * 1e6);
  store.createUser({
    id,
    username,
    password_hash: bcrypt.hashSync(password, 8),
    created_at: nowISO(),
  });

  // Tạo profile mặc định gắn tên đăng nhập
  const profile = makeDefaultProfile();
  profile.name = username;
  store.setProfile(id, profile);

  console.log(`[auth] Đăng ký mới: ${username} (${id})`);
  res.json({ token: signToken(id), userId: id });
});

app.post("/auth/login", (req, res) => {
  const { username, password } = req.body || {};
  if (!username || !password)
    return res.status(400).json({ error: "Thiếu username/password" });

  const user = store.findUserByName(username);
  if (!user || !bcrypt.compareSync(password, user.password_hash))
    return res.status(401).json({ error: "Sai username hoặc mật khẩu" });

  console.log(`[auth] Đăng nhập: ${username} (${user.id})`);
  res.json({ token: signToken(user.id), userId: user.id });
});

// ── Player profile ──
app.get("/player/profile", auth, (req, res) => {
  let profile = store.getProfile(req.userId);
  if (!profile) {
    profile = makeDefaultProfile();
    store.setProfile(req.userId, profile);
  }
  res.json({ player_profile: profile });
});

app.put("/player/profile", auth, (req, res) => {
  const incoming = (req.body && req.body.player_profile) || null;
  if (!incoming)
    return res.status(400).json({ error: "Thiếu player_profile trong body" });

  const current = store.getProfile(req.userId) || makeDefaultProfile();
  const merged = { ...current, ...incoming, updatedAt: nowISO() };
  store.setProfile(req.userId, merged);

  console.log(
    `[profile] Cập nhật ${req.userId}: tutorialCompleted=${merged.tutorialCompleted}`
  );
  res.json({ ok: true, updatedAt: merged.updatedAt });
});

app.listen(PORT, () => {
  console.log(`[ywonderland-stub] listening on :${PORT}`);
});
