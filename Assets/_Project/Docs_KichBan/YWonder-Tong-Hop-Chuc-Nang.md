<!-- Auto-converted from YWonder-Tong-Hop-Chuc-Nang.docx on 2026-07-14. The DOCX file remains the authoritative source. -->

# YWonderHub

**Tổng hợp tính năng hệ thống**

*Hiện trạng triển khai trên production (ywonder.net)*

> Tổng số tính năng: 91

> Đang chạy (Live): 74 — trong đó vừa cập nhật: 8

> Phase 2 (đã có khung, mở rộng): 17

> Cập nhật: 11/06/2026

## Cách đọc bảng

### Cột Trạng thái

- Live (xanh lá) — đã triển khai & đang chạy trên production

- Mới (xanh dương) — vừa thêm/sửa & deploy trong đợt rà soát 11/06/2026

- Phase 2 (tím) — đã có khung trong hệ thống, sẽ mở rộng/hoàn thiện sau

### Cột Vai trò

- Người dùng — thành viên (MEMBER) thao tác

- Đại lý — tính năng riêng cho AGENT

- Admin — quản trị viên thao tác

- Hệ thống — tự động chạy (cron, engine, middleware)

### Mô hình token 2 lớp

- Point (định danh nội bộ GXL) — token tiện ích, số dư chính

- Royalty Point (định danh nội bộ GXLR) — token cổ tức từ staking

## Tóm tắt theo module

| Module | Tổng | Live | Mới | Phase 2 |
| --- | --- | --- | --- | --- |
| 1. Auth & Tài khoản | 9 | 9 | 0 | 0 |
| 2. Ví tiền (Wallet) | 8 | 8 | 1 | 0 |
| 3. Partnerhub & Hoa hồng | 9 | 9 | 1 | 0 |
| 4. Gian hàng & Đầu tư (Startup) | 11 | 11 | 1 | 0 |
| 5. Hồ sơ & KYC | 6 | 6 | 0 | 0 |
| 6. Đa ngôn ngữ & Định tuyến | 4 | 4 | 0 | 0 |
| 7. Thông báo | 4 | 4 | 1 | 0 |
| 8. Admin Panel | 13 | 13 | 2 | 0 |
| 9. Bảo mật & Vận hành | 10 | 10 | 2 | 0 |
| 10. Phase 2 — Nhiệm vụ (Quest) | 4 | 0 | 0 | 4 |
| 11. Phase 2 — Đổi quà (Redeem) | 5 | 0 | 0 | 5 |
| 12. Phase 2 — Token GXLR & Staking | 4 | 0 | 0 | 4 |
| 13. Phase 2 — Cổ tức & Presale | 4 | 0 | 0 | 4 |
| TỔNG CỘNG | 91 | 74 | 8 | 17 |

## Chi tiết từng module

### 1. Auth & Tài khoản

Đăng ký, đăng nhập, xác thực — nền tảng cho mọi tính năng khác.

| STT | Tính năng | Mô tả | Trạng thái | Vai trò |
| --- | --- | --- | --- | --- |
| 1.1 | Đăng ký tài khoản | Form gồm Họ tên + ID đăng nhập (username) + email + mật khẩu + nhập lại. Validate: họ tên chỉ chữ cái & khoảng trắng (≤50), username 4–32 ký tự a-z0-9._ | Live | Người dùng |
| 1.2 | Đăng ký qua mã mời | URL /register/[refCode] tự điền mã; BẮT BUỘC có mã mời hợp lệ mới đăng ký được; tự gán referredById | Live | Người dùng |
| 1.3 | Mã giới thiệu = username | Mã mời/giới thiệu chính là username của tài khoản (thống nhất một mã duy nhất) | Live | Người dùng |
| 1.4 | Xác thực email | Gửi link xác thực (hiệu lực 24h). Tài khoản ở trạng thái PENDING tới khi bấm xác thực mới kích hoạt & mới trả hoa hồng giới thiệu | Live | Người dùng |
| 1.5 | Đăng nhập linh hoạt | Đăng nhập bằng email HOẶC username + mật khẩu | Live | Người dùng |
| 1.6 | 2FA (TOTP) | Google Authenticator: QR setup, xác thực 6 số, chấp nhận ±60s lệch đồng hồ | Live | Người dùng |
| 1.7 | Quên / đặt lại mật khẩu | Gửi link reset qua email, đặt lại mật khẩu mới | Live | Người dùng |
| 1.8 | Cảnh báo đăng nhập | Tạo thông báo mỗi lần đăng nhập thành công (bật/tắt trong hồ sơ) | Live | Người dùng |
| 1.9 | Mật khẩu rút tiền riêng | Mật khẩu thứ 2 chỉ dùng cho rút tiền; sai 5 lần → khóa rút 15 phút (chống rút trộm khi lộ phiên đăng nhập) | Live | Người dùng |

### 2. Ví tiền (Wallet)

Quản lý số dư Point (GXL) & Royalty Point (GXLR), nạp/rút USDT.

| STT | Tính năng | Mô tả | Trạng thái | Vai trò |
| --- | --- | --- | --- | --- |
| 2.1 | Hiển thị số dư | Point khả dụng + Point đang khóa (đầu tư) + Royalty Point + tổng hoa hồng đã nhận | Live | Người dùng |
| 2.2 | Nạp USDT đa mạng | Cấu hình nhiều mạng (BEP20/TRC20…), hiển thị địa chỉ ví + QR; user bấm "Đã chuyển — báo admin" | Live | Người dùng |
| 2.3 | Admin duyệt nạp | Admin verify giao dịch on-chain, quy đổi & cộng Point; có ngưỡng chặn số khổng lồ (chống fat-finger) | Live | Admin |
| 2.4 | Rút tiền | Form rút + mật khẩu rút + giới hạn tần suất; trừ Point tạm, tạo yêu cầu PENDING chờ admin | Live | Người dùng |
| 2.5 | Admin duyệt/từ chối rút | Duyệt (đánh dấu đã rút) hoặc từ chối kèm lý do (hoàn Point về ví) | Live | Admin |
| 2.6 | Lịch sử giao dịch | Bảng đầy đủ: nạp, rút, đầu tư, lãi, hoa hồng — kèm trạng thái & ghi chú | Live | Người dùng |
| 2.7 | Xuất CSV | Tải lịch sử dạng CSV: có BOM UTF-8 (Excel đọc đúng tiếng Việt), nhãn loại/trạng thái đã Việt hóa, ngày dd/mm/yyyy | Live | Người dùng |
| 2.8 | Guard chống trừ âm số dư | Trừ số dư có điều kiện nguyên tử (balance ≥ amount tại thời điểm ghi) → double-click/đồng thời không thể làm âm ví | Mới | Hệ thống |

### 3. Partnerhub & Hoa hồng

Mạng lưới giới thiệu + engine chia hoa hồng trực tiếp và chuỗi đại lý.

| STT | Tính năng | Mô tả | Trạng thái | Vai trò |
| --- | --- | --- | --- | --- |
| 3.1 | Link & QR mời | Link giới thiệu theo refCode (=username) + mã QR, nút copy | Live | Người dùng |
| 3.2 | Danh sách F1 | Bảng tuyến dưới trực tiếp: trạng thái, KYC, ngày tham gia. Tên dài tự rút gọn (ellipsis + hover) không vỡ layout | Mới | Người dùng |
| 3.3 | Lịch sử hoa hồng | Bảng chi tiết: từ ai, sự kiện gì (REGISTER/DEPOSIT/INVEST/PAYOUT), bao nhiêu | Live | Người dùng |
| 3.4 | Hoa hồng trực tiếp F1 | Mọi vai trò đều nhận: REGISTER 10.000 Point (cố định), DEPOSIT 5%, INVEST 3%, PAYOUT 10% — trả NGAY (realtime) | Live | Hệ thống |
| 3.5 | Chuỗi đại lý F1–F6 | Chỉ AGENT: F1 8% + F2–F6 mỗi cấp 1%. Đi lên tối đa 6 cấp, bỏ qua upline là MEMBER | Live | Đại lý |
| 3.6 | Điều kiện KPI theo cấp | Mỗi cấp F chỉ trả nếu upline đủ số tuyến dưới active (5/25/125/625/3125/5625) | Live | Hệ thống |
| 3.7 | Chi trả hoa hồng đại lý | Hoa hồng chuỗi được xếp hàng (chưa trả ngay), cron quét trả vào ngày 10/20/30 hằng tháng vào ví | Live | Hệ thống |
| 3.8 | Loại trừ hoa hồng | Cờ excludedFromCommission trên gian hàng/sản phẩm (vd. nhóm "Vật nuôi") → bỏ qua toàn bộ chuỗi đại lý | Live | Admin |
| 3.9 | Cấp bậc đại lý (Sao) | Hệ thống danh hiệu 1★–6★ với ngưỡng KPI + thưởng hằng tháng (admin cấu hình) | Live | Admin |

### 4. Gian hàng & Đầu tư (Startup)

Cổng đề xuất/đầu tư gian hàng, trả lãi hằng ngày, hoàn vốn khi đáo hạn.

| STT | Tính năng | Mô tả | Trạng thái | Vai trò |
| --- | --- | --- | --- | --- |
| 4.1 | Danh sách & chi tiết gian hàng | Lưới gian hàng kèm ROI, kỳ hạn, mức tối thiểu; trang chi tiết mô tả + tiến độ huy động | Live | Người dùng |
| 4.2 | Đề xuất gian hàng | Member/Agent gửi đề xuất: tên, mô tả, ROI, kỳ hạn, thông tin kinh doanh | Live | Người dùng |
| 4.3 | GPKD 2 mặt + ảnh sản phẩm | Tải Giấy phép kinh doanh mặt trước & mặt sau (bắt buộc) + tối thiểu 6 ảnh sản phẩm; ảnh tự nén client-side | Live | Người dùng |
| 4.4 | Phản hồi gửi đề xuất | Toast báo thành công/lỗi rõ ràng + thông báo cho admin chờ duyệt | Live | Người dùng |
| 4.5 | Admin duyệt/từ chối đề xuất | Xem GPKD + ảnh inline; duyệt (PENDING_REVIEW → OPEN) hoặc từ chối kèm lý do; thông báo người gửi | Live | Admin |
| 4.6 | Đầu tư | Nhập số Point, xác nhận; trừ vốn, khóa lockedGXL, tạo Investment + giao dịch — tất cả trong 1 transaction | Live | Người dùng |
| 4.7 | Chặn vốn min/max & totalCap | Bắt buộc ≥ mức tối thiểu, ≤ maxInvest/lượt, và KHÔNG vượt mục tiêu vốn dự án (guard nguyên tử chống vượt khi đồng thời) | Mới | Hệ thống |
| 4.8 | Gian hàng của tôi & lịch sử lãi | Danh sách khoản đầu tư đang chạy + lịch sử lãi/hoàn vốn | Live | Người dùng |
| 4.9 | Cron trả lãi hằng ngày | Mỗi ngày nhỏ giọt lãi vào ví theo (expectedPayout-vốn)/kỳ hạn; idempotent qua daysCredited | Live | Hệ thống |
| 4.10 | Hoàn vốn khi đáo hạn | Hết kỳ hạn: hoàn vốn gốc, đóng gian hàng (COMPLETED), chốt hoa hồng PAYOUT trên lợi nhuận | Live | Hệ thống |
| 4.11 | Admin CRUD gian hàng | Tạo/sửa/đóng-mở/xóa gian hàng; toggle loại trừ hoa hồng | Live | Admin |

### 5. Hồ sơ & KYC

Thông tin cá nhân, định danh, cài đặt bảo mật.

| STT | Tính năng | Mô tả | Trạng thái | Vai trò |
| --- | --- | --- | --- | --- |
| 5.1 | Thông tin cá nhân | Xem/sửa thông tin, đổi mật khẩu đăng nhập | Live | Người dùng |
| 5.2 | Nộp KYC đa loại giấy tờ | CCCD / Hộ chiếu / Giấy khai sinh — ảnh mặt trước/sau/selfie tùy loại + thông tin định danh | Live | Người dùng |
| 5.3 | Admin duyệt KYC | Xem ảnh giấy tờ, duyệt/từ chối kèm lý do; cập nhật trạng thái KYC tài khoản | Live | Admin |
| 5.4 | Quản lý 2FA | Bật/tắt 2FA (TOTP) trong hồ sơ | Live | Người dùng |
| 5.5 | Thiết lập mật khẩu rút | Đặt/đổi mật khẩu rút tiền riêng | Live | Người dùng |
| 5.6 | Đổi ngôn ngữ | Chuyển VN/EN, lưu vào hồ sơ | Live | Người dùng |

### 6. Đa ngôn ngữ & Định tuyến

Song ngữ VN/EN và tách vùng theo vai trò qua subdomain.

| STT | Tính năng | Mô tả | Trạng thái | Vai trò |
| --- | --- | --- | --- | --- |
| 6.1 | Song ngữ VN/EN | next-intl: định tuyến /vi /en, file dịch cho mọi trang | Live | Hệ thống |
| 6.2 | Chuyển ngôn ngữ realtime | Đổi ngôn ngữ trên topbar không cần tải lại | Live | Người dùng |
| 6.3 | Routing 3 subdomain theo vai trò | ywonder.net (member) · agent.ywonder.net (đại lý) · admin.ywonder.net (admin); middleware tự điều hướng đúng vùng | Live | Hệ thống |
| 6.4 | Trang dùng chung cross-role | Thông báo & Hồ sơ truy cập được từ mọi vai trò mà không bị đẩy về subdomain khác | Live | Hệ thống |

### 7. Thông báo

Thông báo trong app và realtime.

| STT | Tính năng | Mô tả | Trạng thái | Vai trò |
| --- | --- | --- | --- | --- |
| 7.1 | Thông báo trong app | Chuông trên topbar (badge số chưa đọc) + trang /notifications, đánh dấu đã đọc | Live | Người dùng |
| 7.2 | Đẩy realtime (Pusher) | Đẩy hoa hồng/lãi/payout về trình duyệt tức thời | Live | Hệ thống |
| 7.3 | Fan-out cho admin | Một sự kiện (rút tiền, KYC, đề xuất…) gửi thông báo tới mọi admin | Live | Hệ thống |
| 7.4 | Menu thông báo đúng vai trò | Admin bấm chuông không còn rớt sang menu người dùng — sidebar chọn menu theo VAI TRÒ | Mới | Admin |

### 8. Admin Panel

Trung tâm quản trị cho ADMIN / SUPER_ADMIN.

| STT | Tính năng | Mô tả | Trạng thái | Vai trò |
| --- | --- | --- | --- | --- |
| 8.1 | Bảng điều khiển | Tổng quan KPI: người dùng, doanh thu, gian hàng, hoạt động | Live | Admin |
| 8.2 | Quản lý người dùng | Tìm kiếm, lọc, xem chi tiết, đổi vai trò (MEMBER/AGENT/ADMIN) | Live | Admin |
| 8.3 | Khóa/mở tài khoản | Khóa/mở; không cho tự khóa hoặc khóa tài khoản quản trị | Live | Admin |
| 8.4 | Quản lý gian hàng | Duyệt đề xuất, CRUD, mở/đóng, loại trừ hoa hồng | Live | Admin |
| 8.5 | Giao dịch & Duyệt nạp/rút | Xem toàn bộ giao dịch; hàng chờ duyệt nạp & duyệt rút | Live | Admin |
| 8.6 | Cấu hình hoa hồng | Chỉnh % cho REGISTER/DEPOSIT/INVEST/PAYOUT + AGENT_F1..F6 | Live | Admin |
| 8.7 | Cấp bậc đại lý | Cấu hình ngưỡng KPI + thưởng cho từng cấp Sao | Live | Admin |
| 8.8 | Tỷ giá Point | Cấu hình GXL ↔ VND/USDT | Live | Admin |
| 8.9 | Cổng nạp/rút | CRUD mạng lưới crypto (BEP20/TRC20…), địa chỉ ví, QR, tỷ giá, phí | Live | Admin |
| 8.10 | Duyệt KYC | Hàng chờ KYC, duyệt/từ chối | Live | Admin |
| 8.11 | Báo cáo | Báo cáo doanh thu/hoạt động | Live | Admin |
| 8.12 | Nhật ký (Audit log) | Ghi & xem mọi hành động admin; tên hành động đã chuẩn hóa song ngữ + tự "đẹp hóa" mã lạ | Mới | Admin |
| 8.13 | Menu admin theo vai trò | Sidebar hiển thị menu admin kể cả khi ở trang dùng chung (thông báo/hồ sơ) | Mới | Admin |

### 9. Bảo mật & Vận hành

Lớp bảo vệ và hạ tầng chạy nền xuyên suốt.

| STT | Tính năng | Mô tả | Trạng thái | Vai trò |
| --- | --- | --- | --- | --- |
| 9.1 | Hash mật khẩu (bcrypt) | Mật khẩu lưu dạng băm bcrypt | Live | Hệ thống |
| 9.2 | Phiên đăng nhập (NextAuth v5) | JWT session, phân tách theo vai trò + subdomain | Live | Hệ thống |
| 9.3 | Giới hạn tần suất | Rate limit cho rút tiền, verify USDT, tạo yêu cầu… chống spam/brute-force | Live | Hệ thống |
| 9.4 | Idempotency nạp tiền | Chống credit trùng theo txHash on-chain | Live | Hệ thống |
| 9.5 | Transaction ACID + guard | Mọi thao tác ví bọc trong transaction; guard số dư/totalCap nguyên tử | Mới | Hệ thống |
| 9.6 | Ngưỡng chống số khổng lồ | Chặn nạp/rút/credit vượt ngưỡng (phòng fat-finger, từng có sự cố 5e17) | Live | Hệ thống |
| 9.7 | Health check động | Endpoint /api/health probe DB mỗi request (đã sửa khỏi cache tĩnh) — cho UptimeRobot/monitor | Mới | Hệ thống |
| 9.8 | Cron tự động | Trả lãi gian hàng (ngày), hoa hồng đại lý (10/20/30), cổ tức/vesting/đối soát | Live | Hệ thống |
| 9.9 | Nhật ký truy vết | Audit log mọi hành động quản trị (ai, làm gì, lúc nào) | Live | Hệ thống |
| 9.10 | Deploy VPS + nginx | Next.js standalone sau nginx SSL, systemd service, SQLite file-based | Live | Hệ thống |

### 10. Phase 2 — Nhiệm vụ (Quest)

Nhiệm vụ hằng ngày/tuần tăng giữ chân người dùng.

| STT | Tính năng | Mô tả | Trạng thái | Vai trò |
| --- | --- | --- | --- | --- |
| 10.1 | Quản lý quest | Admin tạo quest: loại, điều kiện, phần thưởng | Phase 2 | Admin |
| 10.2 | Giao diện nhiệm vụ | Trang /quest theo Daily/Weekly/Event | Phase 2 | Người dùng |
| 10.3 | Theo dõi tiến độ & nhận thưởng | Hook vào sự kiện cập nhật tiến độ; claim cộng Point | Phase 2 | Hệ thống |
| 10.4 | Chuỗi đăng nhập (streak) | Thưởng khi đăng nhập liên tục | Phase 2 | Hệ thống |

### 11. Phase 2 — Đổi quà (Redeem)

Đổi Point lấy voucher/sản phẩm thật.

| STT | Tính năng | Mô tả | Trạng thái | Vai trò |
| --- | --- | --- | --- | --- |
| 11.1 | Quản lý sản phẩm đổi | Admin tạo sản phẩm: giá Point, kho, đối tác | Phase 2 | Admin |
| 11.2 | Cửa hàng đổi quà | Lưới sản phẩm theo danh mục | Phase 2 | Người dùng |
| 11.3 | Đổi Point → đơn | Trừ Point, tạo đơn đổi quà + mã đổi độc nhất | Phase 2 | Người dùng |
| 11.4 | Mã QR đơn | Sinh QR cho đối tác quét xác nhận | Phase 2 | Hệ thống |
| 11.5 | Đơn của tôi | Danh sách đơn, trạng thái, mã QR | Phase 2 | Người dùng |

### 12. Phase 2 — Token GXLR & Staking

Royalty Point (GXLR) và staking.

| STT | Tính năng | Mô tả | Trạng thái | Vai trò |
| --- | --- | --- | --- | --- |
| 12.1 | Staking Point → GXLR | Khóa Point để nhận GXLR theo APY | Phase 2 | Người dùng |
| 12.2 | Theo dõi GXLR | Số dư GXLR, tổng đã nhận trong ví | Phase 2 | Người dùng |
| 12.3 | Trang Token | Trang /token: cổ tức & thông tin GXLR | Phase 2 | Người dùng |
| 12.4 | Tokenomics (Burn) | Trang /tokenomics; sự kiện đốt token (BurnEvent) | Phase 2 | Hệ thống |

### 13. Phase 2 — Cổ tức & Presale

Chia cổ tức cho holder và bán token theo vòng.

| STT | Tính năng | Mô tả | Trạng thái | Vai trò |
| --- | --- | --- | --- | --- |
| 13.1 | Chia cổ tức (Royalty) | Cron theo quý chia lợi nhuận cho holder GXLR theo tỷ lệ nắm giữ | Phase 2 | Hệ thống |
| 13.2 | Lịch sử nhận cổ tức | Trang xem các kỳ đã nhận | Phase 2 | Người dùng |
| 13.3 | Vòng Presale | Admin tạo vòng: giá, số lượng, vesting; user mua bằng USDT | Phase 2 | Người dùng |
| 13.4 | Lịch vesting | Tự mở khóa GXLR theo lịch (cron vesting) | Phase 2 | Hệ thống |

## Cập nhật gần đây — đợt rà soát 11/06/2026

Rà soát theo bảng QA của khách (11 mục): 7 mục đã chạy sẵn trên prod (bảng QA cũ), các mục còn lại + lỗi phát hiện thêm đã được sửa & deploy:

1. Menu admin theo vai trò — Sidebar chọn menu theo VAI TRÒ thay vì đường dẫn → admin bấm chuông không còn rớt vào menu người dùng.

2. Rút gọn tên ở Partnerhub — Cột "Thành viên" (Danh sách F1 + Lịch sử hoa hồng) tự rút gọn khi tên quá dài, không vỡ layout.

3. Chuẩn hóa nhật ký (Audit log) — Bổ sung 12 nhãn hành động còn thiếu + tự "đẹp hóa" mã lạ → tên nhật ký hợp lệ, đồng nhất.

4. Health check động — /api/health từng bị cache tĩnh (giá trị đóng băng lúc build) → ép render mỗi request, nay probe DB thật.

5. Chống trừ âm số dư — Đầu tư & rút tiền dùng guard nguyên tử (balance ≥ amount) → double-click/đồng thời không làm âm ví.

6. Chặn vốn đầu tư — Đầu tư enforce maxInvest/lượt + không vượt mục tiêu vốn (totalCap) dự án, kiểm nguyên tử khi đồng thời.

7. Gỡ code chết rủi ro — Bỏ verifyUsdtDepositAction (không nơi nào gọi, tiềm ẩn cộng nhầm ví); luồng nạp thật notifyPending → approveDeposit không đổi.

Đã verify trên prod: build PASS, service active, health động, smoke test 200/redirect đúng, log sạch.
