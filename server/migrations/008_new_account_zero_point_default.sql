-- Khách chốt 22/07/2026: tài khoản MỚI bắt đầu với 0 Point (con số 5000 chỉ là placeholder
-- từ thời prototype, chưa bao giờ là luật của khách).
--
-- Commit 4c0dd8eb đã sửa phía code (`insert into player_economy ... values ($1,1,0)`), NHƯNG
-- cột `pos` vẫn giữ `default 5000` ở cấp DATABASE từ migration 001. Chừng nào default còn đó,
-- bất kỳ đường tạo dòng nào KHÔNG nêu cột `pos` sẽ được PostgreSQL tặng lại 5000 — lỗi im lặng,
-- không thấy được khi đọc code game.
--
-- Migration này chỉ đổi DEFAULT cho các dòng tạo về SAU. Nó KHÔNG đụng tới số dư đang có:
-- tài khoản cũ đã nhận 5000 vẫn giữ nguyên, muốn chỉnh phải làm bằng thao tác riêng có duyệt.

alter table player_economy alter column pos set default 0;
