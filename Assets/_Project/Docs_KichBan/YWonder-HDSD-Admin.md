<!-- Auto-converted from YWonder-HDSD-Admin.docx on 2026-07-14. The DOCX file remains the authoritative source. -->

# YWonderHub

**HƯỚNG DẪN SỬ DỤNG**

*DÀNH CHO QUẢN TRỊ VIÊN*

> ywonder.net

> Phiên bản 18/06/2026

## Phần 0 — Giới thiệu

Tài liệu này hướng dẫn Quản trị viên sử dụng YWonderHub — nền tảng đầu tư doanh nghiệp xanh, nhận lãi mỗi ngày và mời bạn nhận hoa hồng.

### Hai loại điểm

| Loại | Vai trò |
| --- | --- |
| USDT | Tiền nạp/rút chính. Giá tham chiếu: 1 Point = 1 YWH = [[0,06 USDT]]. |
| Point | Điểm tiện ích trong app — đầu tư doanh nghiệp, hoa hồng, đổi quà, nhiệm vụ. |
| YWH | Token YWonderHub — nạp USDT để khoá YWH theo lịch (vesting), sau đó bán/đổi ra USDT. Tổng phát hành [[2,6 tỷ]]. |
| Royalty Point | Điểm cổ tức — nhận khi staking; chia lợi nhuận theo quý. |

### Truy cập

Admin làm việc tại admin.ywonder.net (đăng nhập qua ywonder.net/vi/backend/login). Menu trái chứa toàn bộ công cụ quản trị.

## Phần 1 — Tài khoản & đăng nhập

### 1.1 Đăng nhập quản trị

1. Vào ywonder.net/vi/backend/login (hệ thống sẽ đưa bạn sang khu admin.ywonder.net).

2. Nhập email/ID + mật khẩu quản trị, bấm đăng nhập.

3. Nếu bật 2FA: nhập mã 6 số từ Google Authenticator.

LƯU Ý: Tài khoản Admin do SUPER_ADMIN tạo/cấp quyền (mục Người dùng → đổi vai trò). Admin không dùng khu người dùng — mọi thao tác nằm trong khu quản trị.

### 1.2 Bảo mật tài khoản admin

- Bật 2FA trong Hồ sơ → Bảo mật (bắt buộc khuyến nghị cho admin).

- Không chia sẻ phiên đăng nhập; đăng xuất khi rời máy.

## Phần 2 — Bảng điều khiển

Trang Tổng quan hiện: tổng người dùng, vốn lưu hành, doanh nghiệp đang mở, Nạp chờ duyệt, Yêu cầu rút tiền, hoạt động gần đây. Có nút Chạy trả lãi khi có khoản tới kỳ.

## Phần 3 — Duyệt nạp tiền

1. Vào Duyệt nạp tiền — danh sách chờ.

2. Đối chiếu giao dịch USDT on-chain (số tiền, mạng, mã GD).

3. Bấm Duyệt (nhập đúng số USDT thực nhận nếu cần) để cộng Point, hoặc Từ chối kèm lý do.

CẢNH BÁO: Hệ thống chặn số vượt ngưỡng (chống nhập nhầm). Mỗi mã giao dịch chỉ cộng một lần.

## Phần 4 — Duyệt rút tiền

1. Vào Duyệt rút tiền — yêu cầu Đang xử lý.

2. Kiểm tra thông tin nhận & chuyển tiền thực tế.

3. Bấm Duyệt hoàn tất, hoặc Từ chối (Point tự hoàn về ví người dùng).

## Phần 5 — Duyệt đề xuất doanh nghiệp

1. Vào Doanh nghiệp → lọc Chờ duyệt.

2. Xem GPKD 2 mặt + ảnh sản phẩm, kiểm tra thông tin KD.

3. Bấm Duyệt (mở công khai để đầu tư) hoặc Từ chối kèm lý do.

Admin cũng có thể tự tạo doanh nghiệp trực tiếp, sửa, đóng/mở, hoặc bật loại trừ hoa hồng.

## Phần 6 — Duyệt KYC

Vào Duyệt KYC → xem ảnh giấy tờ & thông tin → Duyệt hoặc Từ chối kèm lý do. Người dùng nhận thông báo.

LƯU Ý: Hệ thống tự động duyệt mọi hồ sơ KYC đã chờ ≥ 15 phút (cron chạy mỗi 5 phút). Muốn kiểm soát thủ công, admin duyệt/từ chối TRƯỚC khi đủ 15 phút.

## Phần 7 — Cấu hình hệ thống

- Cấu hình hoa hồng: % cho REGISTER/DEPOSIT/INVEST/PAYOUT và AGENT F1–F6 (không hồi tố giao dịch cũ).

- Cấp bậc đại lý: ngưỡng KPI + thưởng từng cấp Sao (1★–6★).

- Tỷ giá Point: quy đổi Point ↔ VND/USDT.

- Cổng nạp/rút: thêm/sửa mạng crypto — địa chỉ ví, QR, tỷ giá, phí.

- Người dùng: tìm kiếm, đổi vai trò, khóa/mở tài khoản.

## Phần 8 — Nhật ký & Báo cáo

- Nhật ký: ghi mọi hành động quản trị (ai/làm gì/khi nào) — tên hành động tiếng Việt rõ ràng.

- Báo cáo: doanh thu nạp, người dùng mới, vốn lưu hành, hoa hồng theo tháng.

LƯU Ý: Cron tự động: hoa hồng F1 đại lý trả ngay · F2–F6 trả ngày 10/20/30; lãi doanh nghiệp trả mỗi ngày; KYC tự duyệt sau 15 phút. Có thể bấm Chạy trả lãi thủ công.

## Phần 9 — Quản lý đại lý

1. Vào Quản lý đại lý — lọc Tất cả / Chờ duyệt / Đang hoạt động / Đã khoá.

2. + Tạo đại lý: tạo tài khoản đại lý mới (kích hoạt ngay; để trống mã giới thiệu = đại lý gốc).

3. Đại lý tự đăng ký nằm ở tab Chờ duyệt: bấm Duyệt để kích hoạt hoặc Từ chối.

4. Cấp bậc: đặt Sao 0–6 cho từng đại lý.

## Phần 10 — Ví USDT (duyệt nạp & rút)

1. Vào Ví USDT (nạp/rút) — 2 danh sách: Nạp USDT và Rút USDT chờ duyệt.

2. Nạp: đối chiếu on-chain → bấm Duyệt và nhập đúng số USDT thực nhận (đây là gốc tính hạn mức rút của nhà đầu tư), hoặc Từ chối.

3. Rút: kiểm tra địa chỉ ví nhận → chuyển USDT thực tế → bấm Duyệt (hoặc Từ chối để hoàn USDT về ví người dùng).

LƯU Ý: Luật rút nhà đầu tư (hệ thống tự áp): đang hồi vốn rút ≤ 20%/tháng vốn nạp (không quá vốn gốc); đã hồi đủ vốn → chỉ tiêu trong app 15%/tháng, không rút tiền mặt.

## Phần 11 — Duyệt nạp YWH

1. Vào Duyệt nạp YWH — yêu cầu nạp USDT để khoá YWH.

2. Đối chiếu USDT on-chain → bấm Duyệt để tạo lịch khoá YWH (10% mở bán sau 30 ngày; 90% khoá 6 tháng rồi nhả 7,5%/tháng), hoặc Từ chối kèm lý do.

## Phần 12 — Nuôi trồng (danh mục)

Vào Nuôi trồng — xem danh mục vật nuôi (giá USDT, số tháng, lãi/tháng). Bấm Cập nhật danh mục từ bảng để nạp/cập nhật 10 vật nuôi theo bảng giá đã chốt.

## Phần cuối — Câu hỏi thường gặp & xử lý sự cố

| Tình huống | Cách xử lý |
| --- | --- |
| Không đăng nhập được | Dùng đúng email hoặc ID đăng nhập + mật khẩu; bật 2FA thì nhập đúng mã 6 số; quên thì bấm "Quên mật khẩu?". |
| Đã duyệt nạp nhưng người dùng chưa thấy Point | Kiểm tra trạng thái giao dịch đã chuyển SUCCESS; người dùng tải lại trang Ví tiền. |
| Hoa hồng đại lý chi trả thế nào | F1 (8%) trả NGAY vào ví; F2–F6 trả tự động ngày 10/20/30. Khoản chưa đủ KPI sẽ không trả. |
| KYC có tự được duyệt không | Có — hệ thống tự duyệt hồ sơ chờ ≥ 15 phút (cron 5 phút/lần). Admin vẫn duyệt/từ chối thủ công trước đó nếu cần. |
| Email xác thực không gửi đi | Cần cấu hình [[SMTP]] trong .env máy chủ (SMTP_HOST/PORT/USER/PASS/FROM) rồi khởi động lại dịch vụ; chưa cấu hình thì tài khoản kích hoạt ngay không qua email. |
| Cần trả lãi ngay không đợi cron | Vào Tổng quan, bấm [[Chạy trả lãi]]. |

— Hết —
