# 🛡️ SECURITY & ANTI-CHEAT — Y WONDER GREEN FARM

> Tài liệu kỹ thuật **nội bộ team**. Mục tiêu: bảo vệ dữ liệu tiền/inventory/giao dịch khi game lên online.
> Phiên bản: 0.1 — 16/06/2026. Trạng thái hiện tại: **game đang offline (PlayerPrefs)** → mọi giá trị client tự sửa được. Tài liệu này đặt ra mục tiêu cho đợt 2–3.
> Liên quan: `TECHNICAL_DESIGN.md`, `DB_SCHEMA.md`.

---

## 1. Mô hình mối đe doạ (Threat Model)
| Tài sản | Đe doạ | Hậu quả |
|---|---|---|
| Tiền POS/UPOS | Client tự sửa PlayerPrefs / chỉnh request | Lạm phát kinh tế, phá doanh thu (UPOS = tiền thật) |
| Inventory | Tự thêm item/seed/dụng cụ | Bỏ qua vòng kinh tế |
| Giao dịch mua/bán/nâng cấp | Replay, double-spend | Nhân tiền/đồ |
| Lãi Heo Đất | **Chỉnh giờ máy** để đáo hạn sớm | Nhân tiền |
| Lượt câu cá (10/ngày) | Reset cục bộ | Bỏ giới hạn |
| IAP (nạp UPOS) | Receipt giả | Mất tiền thật của khách |
| Token đăng nhập | Lộ/đoán | Chiếm tài khoản |

## 2. Nguyên tắc cốt lõi: **SERVER-AUTHORITATIVE**
> Quy tắc vàng: **Client gửi Ý ĐỊNH, không gửi KẾT QUẢ.**

| ❌ KHÔNG được (client) | ✅ ĐÚNG (server quyết) |
|---|---|
| "Đặt POS của tôi = 99999" | "Tôi muốn BÁN item X số lượng N" → server tra giá, cộng tiền, trả số dư mới |
| "Thêm 100 gỗ vào túi" | "Tôi chặt cây Y" → server validate vị trí/cooldown, cộng loot |
| "Tutorial xong, +50 POS" | Server tự cộng theo bước tutorial đã xác minh |
| "Heo Đất đáo hạn, +lãi" | Server tính lãi theo `mature_at` (giờ server), không tin giờ client |

→ Hiện trạng cần sửa: `EconomyManager` cho client cộng/trừ trực tiếp; `PiggyBank` chạy giờ giả lập (1 ngày=5s). Đợt 2 chuyển các thao tác này thành **endpoint giao dịch**.

## 3. Chống các đòn cụ thể
### 3.1 Double-spend / Replay
- Mỗi giao dịch gửi kèm **`idempotency_key`** (UUID client sinh).
- Server lưu vào `transactions.idempotency_key` (UNIQUE) → gửi lại = trả kết quả cũ, không cộng đôi.

### 3.2 Chỉnh giờ máy (Heo Đất, lượt câu/ngày, cây-thú)
- **Mọi mốc thời gian dùng GIỜ SERVER**, không dùng `DateTime.Now` của máy.
- Cần `TimeManager` đồng bộ giờ server lúc đăng nhập; cây lớn / thú đói / lãi tính từ timestamp server (`planted_at`, `fed_at`, `mature_at`).

### 3.3 Validate đầu vào server-side
- Giá mua/bán tra từ `item_catalog` (server), KHÔNG tin giá client gửi.
- Kiểm số dư đủ trước khi trừ; kiểm số lượng hợp lệ (>0, không vượt stack).
- Cooldown hành động (chặt/đào/câu) kiểm ở server tránh spam.

### 3.4 IAP (nạp UPOS bằng tiền thật)
- **Bắt buộc validate receipt** với Google Play Billing server-side trước khi cộng UPOS.
- Lưu `purchase_token` chống dùng lại; chỉ cộng khi Google xác nhận PURCHASED.
- ⚠️ Hiện code mua bundle chỉ `Debug.Log` — **tuyệt đối không** cộng tiền client-side khi làm thật.

## 4. Auth & Token
| Hạng mục | Hiện tại (stub) | Production |
|---|---|---|
| JWT secret | Cứng trong code | Qua biến môi trường (ENV), đủ dài/ngẫu nhiên |
| TTL | 30 ngày | Access token ngắn (vài giờ) + **refresh token** |
| Mật khẩu | bcrypt cost 8 | bcrypt cost ≥10 |
| Truyền tải | HTTP localhost | **HTTPS bắt buộc** |
| Reset mật khẩu | Chưa có | Qua email (UI ForgotPassword đã có) — cần chốt định danh G3 |
| Rate-limit | Không | Giới hạn /auth/* (chống brute-force) |

## 5. Bảo vệ tầng vận chuyển & lưu trữ
- **HTTPS/TLS** cho mọi request production (token đi trong header).
- KHÔNG log mật khẩu / token ra console production.
- DB: mật khẩu chỉ lưu hash; backup định kỳ; least-privilege cho DB user.
- CORS: production siết origin (stub đang mở `*`).

## 6. Phòng thủ phía client (lớp phụ — không thay server)
- Client validation chỉ để UX (báo "không đủ tiền" sớm), **không thay server check**.
- Không nhúng secret/khoá API trong build client.
- Cache local (PlayerPrefs) chỉ là bản chiếu offline; **nguồn sự thật là server**. Khi online lại, server ghi đè theo logic merge.

## 7. Việc cần làm (checklist cho đợt 2–3)
- [ ] Chuyển Economy sang endpoint giao dịch server-authoritative.
- [ ] Thêm bảng `transactions` + idempotency_key.
- [ ] `TimeManager` đồng bộ giờ server; chuyển PiggyBank/cây/thú sang giờ server.
- [ ] Nối PiggyBank vào EconomyManager (hiện tách rời).
- [ ] Đổi POS `int` → `long`/BIGINT.
- [ ] JWT secret qua ENV + HTTPS + refresh token + rate-limit /auth.
- [ ] IAP receipt validation server-side trước khi cộng UPOS.
- [ ] Validate input + cooldown hành động ở server.

## 8. Phạm vi đợt 1 (chấp nhận được vì offline/demo)
Server stub hiện **KHÔNG** đạt các tiêu chí trên (secret cứng, không HTTPS, không rate-limit) — **chỉ dùng dev/test local**. Đã ghi rõ trong `server/README.md`. Production phải đáp ứng mục 2–5 trước khi mở cho người chơi thật.
