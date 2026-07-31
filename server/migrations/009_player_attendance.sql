-- Điểm danh 15 ngày TÂN THỦ: chuyển sổ từ máy người chơi lên server.
--
-- Trước migration này, toàn bộ tiến độ điểm danh nằm trong PlayerPrefs của thiết bị
-- (YW_AttendanceClaimedDays / YW_AttendanceLastDate) trong khi PHẦN THƯỞNG lại được
-- đồng bộ lên server thật. Sổ ở máy, tiền ở server -> ba lỗ:
--   1. Vặn đồng hồ / đổi múi giờ thiết bị là qua "ngày mới", ăn trọn 15 ngày trong vài phút.
--   2. Cài lại game -> bộ đếm về 0 -> quay vòng 15 ngày lại từ đầu, lặp vô hạn.
--   3. Đổi máy -> người chơi thật mất sạch tiến độ.
--
-- claimed_days     = vị trí hiện tại trong chuỗi (0..15).
-- max_rewarded_day = ngày CAO NHẤT đã từng được trả thưởng (0..15).
--
-- Vì sao cần hai cột chứ không phải một: khách chốt 31/07/2026 "nghỉ giữa chừng là mất chuỗi".
-- Nếu chỉ có claimed_days thì mất chuỗi = quay về ngày 1 = được trả lại quà ngày 1, và người
-- chơi chỉ cần điểm danh cách ngày là in tiền mãi mãi. max_rewarded_day khoá điều đó lại:
-- chuỗi tụt về 1 thật (phải leo lại đủ 15 ngày liên tiếp mới chạm được quà ngày 15), nhưng
-- những ngày đã trả thưởng rồi thì không trả lần hai. Mốc 15 ngày là quà tân thủ, một lần.
--
-- last_claim_date theo NGÀY GAME (giờ Asia/Ho_Chi_Minh, xem periodKey trong postgresStore.js),
-- lưu chuỗi 'YYYY-MM-DD' cho khớp với period_key của player_daily_limits.

create table if not exists player_attendance (
    player_id text primary key references game_players(id) on delete cascade,
    claimed_days integer not null default 0 check (claimed_days >= 0),
    max_rewarded_day integer not null default 0 check (max_rewarded_day >= 0),
    last_claim_date text not null default '',
    updated_at timestamptz not null default now()
);
