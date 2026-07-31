"use strict";

// MỘT định nghĩa "ngày game" duy nhất cho cả server.
//
// Trước file này hai kho chấm ngày khác nhau: store.js (JSON) cắt theo UTC bằng
// toISOString(), còn postgresStore.js cắt theo Asia/Ho_Chi_Minh. Bản chạy thật là postgres
// nên người chơi không thấy, nhưng chạy kho JSON (máy dev) thì lượt câu cá / vòng quay /
// điểm danh reset lệch 7 tiếng so với production — thử ở nhà đúng, lên server sai.
//
// Đổi múi giờ vận hành thì đặt biến môi trường GAME_TIMEZONE, đừng sửa rải rác trong code.

const DEFAULT_TIMEZONE = "Asia/Ho_Chi_Minh";

function gameTimeZone() {
  return process.env.GAME_TIMEZONE || DEFAULT_TIMEZONE;
}

// Trả 'YYYY-MM-DD' theo giờ vận hành. Dùng cho period_key của daily-limit và ngày điểm danh.
function gameDayKey(date = new Date()) {
  const parts = new Intl.DateTimeFormat("en-US", {
    timeZone: gameTimeZone(),
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).formatToParts(date);
  const values = Object.fromEntries(parts.map((part) => [part.type, part.value]));
  return `${values.year}-${values.month}-${values.day}`;
}

// Lùi một ngày lịch. Nhận/trả 'YYYY-MM-DD'; trả '' nếu chuỗi vào không hợp lệ.
// Tính bằng mốc UTC giữa trưa để đổi ngày lịch không bị lệch do giờ mùa hè.
function previousDayKey(dayKey) {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(String(dayKey || ""))) return "";
  const [year, month, day] = dayKey.split("-").map(Number);
  const stamp = Date.UTC(year, month - 1, day, 12, 0, 0);
  if (!Number.isFinite(stamp)) return "";
  const previous = new Date(stamp - 24 * 60 * 60 * 1000);
  const pad = (value) => String(value).padStart(2, "0");
  return `${previous.getUTCFullYear()}-${pad(previous.getUTCMonth() + 1)}-${pad(previous.getUTCDate())}`;
}

module.exports = { DEFAULT_TIMEZONE, gameTimeZone, gameDayKey, previousDayKey };
