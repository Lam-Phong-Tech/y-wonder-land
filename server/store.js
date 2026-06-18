// store.js — Lưu trữ tối giản bằng 1 file JSON (data.json).
// Dùng cho server stub dev/test. KHÔNG phải production (không transaction, không lock).
const fs = require("fs");
const path = require("path");

const DB_PATH = path.join(__dirname, "data.json");

function readAll() {
  try {
    if (!fs.existsSync(DB_PATH)) return { users: [], profiles: {} };
    const raw = fs.readFileSync(DB_PATH, "utf8");
    if (!raw.trim()) return { users: [], profiles: {} };
    return JSON.parse(raw);
  } catch (e) {
    console.error("[store] Đọc data.json lỗi, khởi tạo rỗng:", e.message);
    return { users: [], profiles: {} };
  }
}

function writeAll(data) {
  fs.writeFileSync(DB_PATH, JSON.stringify(data, null, 2), "utf8");
}

module.exports = {
  // ── Users ──
  findUserByName(username) {
    return readAll().users.find((u) => u.username === username) || null;
  },
  findUserById(id) {
    return readAll().users.find((u) => u.id === id) || null;
  },
  createUser(user) {
    const db = readAll();
    db.users.push(user);
    writeAll(db);
    return user;
  },

  // ── Profiles ── (1 profile / userId)
  getProfile(userId) {
    return readAll().profiles[userId] || null;
  },
  setProfile(userId, profile) {
    const db = readAll();
    db.profiles[userId] = profile;
    writeAll(db);
    return profile;
  },
};
