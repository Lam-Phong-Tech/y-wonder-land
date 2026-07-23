<!-- Auto-converted from YWonder-Co-Che-Ky-Thuat.docx on 2026-07-14. The DOCX file remains the authoritative source. -->

# YWonderHub

**TÀI LIỆU CƠ CHẾ KỸ THUẬT**

*Tự động duyệt KYC · Đổi tên dự án · Chi trả hoa hồng F1/F2–F6*

> ywonder.net

> Cập nhật & triển khai: 13/06/2026

## Tổng quan

Tài liệu mô tả chi tiết cơ chế của 3 thay đổi đã triển khai lên production ngày 13/06/2026. Mỗi cơ chế gồm: thành phần, luồng chạy từng bước, lý do thiết kế, và cách kiểm chứng.

| # | Cơ chế | Bản chất |
| --- | --- | --- |
| 1 | Tự động duyệt KYC 15 phút | Tác vụ định kỳ (cron) duyệt hồ sơ đã chờ đủ giờ |
| 2 | Đổi tên dự án | Đổi trường tên hiển thị, giữ nguyên định danh kỹ thuật |
| 3 | Hoa hồng F1 ngay / F2–F6 ngày 10·20·30 | Tách cấp F1 trả ngay khỏi chuỗi trả theo lịch |

## Cơ chế 1 — Tự động duyệt KYC trong 15 phút

### 1.1 Thành phần

| Thành phần | Vị trí | Vai trò |
| --- | --- | --- |
| Hàm xử lý | runKycAutoApproveJob(15) | Tìm & duyệt hồ sơ đã chờ ≥ 15 phút |
| Endpoint | POST /api/cron/kyc-approve | Cổng gọi hàm, bảo vệ bằng khoá bí mật |
| Lịch cron | */5 * * * * | Cứ 5 phút tự gọi endpoint (crontab user greenxland) |

### 1.2 Luồng chạy từng bước

1. Người dùng nộp KYC → tạo bản ghi KycRequest với status = PENDING, lưu mốc submittedAt.

2. Mỗi 5 phút, cron trên VPS chạy lệnh curl gọi endpoint kèm header Authorization: Bearer CRON_SECRET.

3. Endpoint kiểm tra khoá bí mật (chống gọi trái phép) → gọi hàm xử lý.

4. Hàm tính mốc cutoff = bây giờ − 15 phút, tìm mọi hồ sơ PENDING có submittedAt ≤ cutoff (đã chờ ≥ 15 phút).

5. Với từng hồ sơ, trong một transaction: đặt User.kycStatus = APPROVED, đánh dấu hồ sơ APPROVED + reviewedBy = "AUTO", và gửi thông báo cho người dùng.

6. Trả về số hồ sơ đã duyệt (ghi log tại /var/log/greenxland/cron-kyc.log).

### 1.3 Lý do thiết kế

- Vì sao cron + ngưỡng thời gian (thay vì "hẹn giờ 15 phút lúc nộp"): web app không có cơ chế hẹn-giờ-chạy-sau đáng tin (app restart là mất hẹn). Cron quét định kỳ thì bền — dù app restart, lần quét sau vẫn gom hết hồ sơ tồn đọng.

- Idempotent: chạy lại không duyệt trùng — hồ sơ đã APPROVED thì không còn lọt điều kiện PENDING.

- Thời gian thực tế: cron 5 phút + ngưỡng 15 phút → hồ sơ được duyệt trong khoảng 15–20 phút sau khi nộp.

CẢNH BÁO: Cơ chế này bỏ qua bước kiểm tra danh tính thật. Admin vẫn có thể duyệt/từ chối thủ công TRƯỚC khi đủ 15 phút (admin từ chối → hồ sơ hết PENDING → cron bỏ qua). Muốn tắt: xoá dòng cron kyc-approve.

## Cơ chế 2 — Đổi tên dự án

Mỗi dự án (bảng Project) có 2 nhóm trường tách biệt:

- Định danh kỹ thuật — slug (vd. coastal-wind): dùng cho URL và liên kết khoản đầu tư. KHÔNG đổi.

- Tên hiển thị — nameVi / nameEn: cái người dùng nhìn thấy. Đây là phần được đổi.

### 2.1 Cách thực hiện

Chạy 6 câu lệnh UPDATE Project SET nameVi='…', nameEn='…' WHERE slug='…' — chỉ thay tên hiển thị, khớp theo slug. Đặt cả 2 ngôn ngữ = tên thương hiệu.

| slug (định danh — giữ nguyên) | Tên mới (hiển thị) |
| --- | --- |
| coastal-wind | YWonder Green Farm |
| organic-compost | YWonder Hub |
| agritech-vertical | YWonder Gems |
| carbon-forest | YWonder Mall |
| dalat-eco-village | YWonder Health |
| ninh-thuan-solar | YWonder Land |

### 2.2 Vì sao an toàn

- Không đụng slug/URL/khoản đầu tư → mọi đầu tư, link, lịch sử nguyên vẹn; chỉ tên đổi.

- Trang "Tất cả doanh nghiệp" đọc nameVi/nameEn từ DB mỗi lần tải (trang cần đăng nhập = render động) → tên mới hiện ngay khi tải lại.

- Đã backup DB trước khi đổi (rollback được).

LƯU Ý: Hiện chỉ đổi TÊN. Phần mô tả / lĩnh vực / ảnh vẫn theo chủ đề cũ (điện gió, phân hữu cơ…) — có thể cập nhật riêng nếu cần.

## Cơ chế 3 — Hoa hồng: F1 nhận ngay · F2–F6 ngày 10·20·30

### 3.1 Phân biệt 2 loại hoa hồng

a) Hoa hồng trực tiếp F1 (mọi vai trò): khi F1 — người mời trực tiếp — nạp/đầu tư/nhận lãi, người giới thiệu được cộng % ngay vào ví (paid=true). Loại này VỐN đã trả ngay từ trước, không đổi.

b) Hoa hồng chuỗi đại lý F1→F6 (chỉ AGENT): hàm creditAgentChainCommission đi ngược lên cây giới thiệu tối đa 6 cấp. Mỗi cấp, nếu upline là đại lý + active + đủ KPI thì sinh hoa hồng AGENT_F{cấp}.

### 3.2 Thay đổi — tách xử lý theo cấp

| Cấp | Tỷ lệ | Cơ chế chi trả |
| --- | --- | --- |
| AGENT_F1 | 8% | paid=true + cộng ví + ghi giao dịch → TRẢ NGAY, rút liền |
| AGENT_F2…F6 | 1% / cấp | paid=false (xếp hàng) → cron quét trả vào 10·20·30 |

### 3.3 Cron chi trả F2–F6

Cron agent-payout chạy lúc 0 1 10,20,30 * * (ngày 10, 20, 30) gọi runAgentPayoutJob: quét mọi hoa hồng paid=false thuộc chuỗi AGENT_F*, gộp theo từng đại lý, cộng tổng vào ví và đánh dấu đã trả.

CƠ CHẾ: Vì AGENT_F1 giờ đã paid=true ngay từ đầu → cron 10/20/30 không quét lại nó → KHÔNG trả trùng. Chỉ F2–F6 (paid=false) được trả theo lịch.

### 3.4 Điều kiện KPI (vẫn áp dụng)

Mỗi cấp chỉ được trả khi đại lý đủ số tuyến dưới active tại cấp đó:

| Cấp | F1 | F2 | F3 | F4 | F5 | F6 |
| --- | --- | --- | --- | --- | --- | --- |
| Số tuyến dưới active cần đạt | 5 | 25 | 125 | 625 | 3.125 | 5.625 |

Tóm gọn: đại lý mời trực tiếp F1 → khi F1 giao dịch, đại lý nhận hoa hồng trực tiếp + AGENT_F1 (8%) ngay lập tức; các tầng sâu hơn (F2–F6) gom trả vào 10/20/30.

## Phụ lục — Tham chiếu kỹ thuật & kiểm chứng

### A. Tệp & thành phần liên quan

| Hạng mục | Vị trí |
| --- | --- |
| Hàm auto-duyệt KYC | lib/actions/cron.ts → runKycAutoApproveJob |
| Endpoint KYC | app/api/cron/kyc-approve/route.ts |
| Hàm hoa hồng chuỗi | lib/agent-commission.ts → creditAgentChainCommission |
| Cron chi trả đại lý | lib/actions/cron.ts → runAgentPayoutJob |
| Lịch cron | crontab -u greenxland (KHÔNG phải root) |

### B. Lịch cron của YWonder

| Tác vụ | Lịch (giờ UTC) | Endpoint |
| --- | --- | --- |
| Trả lãi doanh nghiệp (hằng ngày) | 0 0 * * * | /api/cron/payout |
| Tự duyệt KYC | */5 * * * * | /api/cron/kyc-approve |
| Chi trả hoa hồng đại lý | 0 1 10,20,30 * * | /api/cron/agent-payout |
| Vesting / Reconcile / Royalty | theo lịch riêng | /api/cron/* |

### C. Cách kiểm chứng

- KYC: nộp 1 hồ sơ thử → đợi ~15–20 phút → hồ sơ tự "Đã duyệt" + có thông báo. Log: /var/log/greenxland/cron-kyc.log.

- Tên dự án: mở "Tất cả doanh nghiệp" → thấy 6 tên YWonder mới.

- Hoa hồng: cho 1 F1 của đại lý nạp/đầu tư → ví đại lý tăng ngay (trực tiếp + AGENT_F1 8%); F2–F6 hiện ở "Lịch sử hoa hồng" dạng chờ, vào ví ngày 10/20/30.

— Hết —
