"use strict";

// Luật điểm danh 15 ngày tân thủ. Dùng CHUNG cho kho JSON và kho Postgres — bảng thưởng
// chỉ được phép tồn tại một bản ở đây, đừng chép lại sang store nào.
//
// Bản gốc của bảng thưởng nằm ở GetDayReward trong EventPopupController.cs. Từ nay server
// mới là nguồn sự thật; client chỉ vẽ lại những gì server trả về.

const { gameDayKey, previousDayKey } = require("./gameDay");

const ATTENDANCE_TOTAL_DAYS = 15;

// Chỉ ngày mốc mới có quà; ngày khác vẫn tính là điểm danh nhưng không thưởng.
const ATTENDANCE_REWARDS = {
  1: { point: 26 },
  3: { itemId: "wood_01", qty: 4 },
  5: { point: 26 },
  7: { itemId: "corn_01", qty: 10 },
  10: { itemId: "pumpkin_01", qty: 10 },
  11: { itemId: "wood_01", qty: 8 },
  15: { itemId: "rabbit_01", qty: 1 },
};

function rewardForDay(day) {
  const entry = ATTENDANCE_REWARDS[day];
  if (!entry) return { day, point: 0, itemId: "", qty: 0, isNothing: true };
  return {
    day,
    point: Number(entry.point || 0),
    itemId: String(entry.itemId || ""),
    qty: Number(entry.qty || 0),
    isNothing: false,
  };
}

// Chuỗi đứng ở đâu NẾU điểm danh ngay bây giờ.
// Khách chốt 31/07/2026: nghỉ một ngày là mất chuỗi, quay về ngày 1.
function resolveStreak(lastClaimDate, claimedDays, today = gameDayKey()) {
  const last = String(lastClaimDate || "");
  const current = Math.max(0, Number(claimedDays) || 0);

  if (last === today) {
    return { streak: current, claimedToday: true, streakReset: false };
  }
  if (last && last === previousDayKey(today)) {
    return { streak: current + 1, claimedToday: false, streakReset: false };
  }
  // Chưa bao giờ điểm danh, hoặc đã bỏ ít nhất một ngày -> bắt đầu lại từ ngày 1.
  return { streak: 1, claimedToday: false, streakReset: current > 0 };
}

// Chuỗi để HIỂN THỊ lưới 15 ô: đã bỏ ngày thì lưới phải sáng lại từ đầu ngay khi mở popup,
// chứ không đợi tới lúc bấm nút mới báo mất chuỗi.
function visibleStreak(lastClaimDate, claimedDays, today = gameDayKey()) {
  const last = String(lastClaimDate || "");
  const current = Math.max(0, Number(claimedDays) || 0);
  if (last === today) return current;
  if (last && last === previousDayKey(today)) return current;
  return 0;
}

// Còn nhận được nữa không. Hết 15 ngày ĐÃ TRẢ THƯỞNG là xong hẳn — quà tân thủ chỉ một lần.
function isTrackFinished(maxRewardedDay) {
  return (Number(maxRewardedDay) || 0) >= ATTENDANCE_TOTAL_DAYS;
}

// Gói trạng thái điểm danh trả về cho client. Luật mất chuỗi đã tính sẵn trong visibleDays
// và nextDay, nên client chỉ việc vẽ — không cần biết hôm qua là ngày nào.
// Dùng chung cho CẢ HAI kho; đừng viết lại bản riêng trong store nào.
function attendanceView(record, today = gameDayKey()) {
  const source = record || {};
  const claimedDays = Math.max(0, Number(source.claimedDays) || 0);
  const maxRewardedDay = Math.max(0, Number(source.maxRewardedDay) || 0);
  const lastClaimDate = String(source.lastClaimDate || "");
  const state = resolveStreak(lastClaimDate, claimedDays, today);
  const finished = isTrackFinished(maxRewardedDay);
  return {
    claimedDays,
    maxRewardedDay,
    lastClaimDate,
    visibleDays: visibleStreak(lastClaimDate, claimedDays, today),
    totalDays: ATTENDANCE_TOTAL_DAYS,
    claimedToday: state.claimedToday,
    nextDay: finished ? 0 : state.streak,
    canClaim: !finished && !state.claimedToday,
    finished,
    today,
  };
}

module.exports = {
  ATTENDANCE_TOTAL_DAYS,
  ATTENDANCE_REWARDS,
  rewardForDay,
  resolveStreak,
  visibleStreak,
  isTrackFinished,
  attendanceView,
};
