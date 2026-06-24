# 📚 TỔNG KẾT TOÀN BỘ TÀI LIỆU DỰ ÁN CẦN CÓ — YWONDERLAND

> Lập 16/06/2026. Chia theo **AI ĐẢM NHIỆM**. Ưu tiên: 🔴 chặn việc làm · 🟡 cần trước bàn giao · ⚪ làm sau.
> Tài liệu chi tiết điểm mù: xem `DiemMu_CanXinKhach.md`.

---

## 🟦 NHÓM A — KHÁCH / BA CUNG CẤP (mình không bịa được)

> Đây là phần **nhắn BA xin khách**. Toàn là "luật chơi" và "doanh thu" — chỉ khách mới quyết được.

| # | Tài liệu | Nội dung | Mức |
|---|---|---|---|
| A1 | **Bảng cân bằng game (Game Balance Sheet)** | Giá mua/bán từng item (~45), thời gian–sản lượng–EXP từng cây & thú, công thức nâng cấp dụng cụ, droprate cá | 🔴 |
| A2 | **Bảng EXP & Level** | EXP mỗi cấp, level cap, mở khoá gì theo từng level | 🔴 |
| A3 | **Danh mục 12–13 cửa hàng chi tiết** | Mỗi quầy bán gì, giá riêng, NPC nào phụ trách | 🔴 |
| A4 | **Tài liệu API web của khách** | Base URL, cách đăng nhập chung, schema tài khoản, ai làm API phía web | 🔴 |
| A5 | **Quyết định nền tảng & nghiệp vụ** | Đăng nhập bằng gì (username/email/social)? Có IAP/realtime/push không? Build Android-only hay cả iOS/PC? | 🔴 |
| A6 | **Danh sách Quest** | Nhiệm vụ thật (daily/weekly/main): điều kiện + phần thưởng + người giao | 🟡 |
| A7 | **Tài liệu Art/Audio** | Danh sách model MVP ưu tiên, icon thật, **ai làm âm thanh** | 🟡 |
| A8 | **Thông tin Publish** | Min SDK, đã có Google Play Console chưa, máy test thật | 🔴 |

---

## 🟩 NHÓM B — TEAM TỰ VIẾT (việc của dev, KHÔNG xin khách)

> Đây là tài liệu **kỹ thuật**, anh + bé tự soạn dựa trên code. Khách không viết hộ được.

| # | Tài liệu | Nội dung | Mức |
|---|---|---|---|
| B1 | **TDD — Technical Design Doc** | Kiến trúc backend thật (REST), chọn stack (DB/ngôn ngữ/host), cách Unity ↔ server | 🔴 |
| B2 | **ERD / DB Schema thật** | Bảng dữ liệu, quan hệ, index — thay blueprint UGS cũ | 🔴 |
| B3 | **Security & Anti-cheat tối thiểu** | Server giữ tiền/inventory (hiện client tự sửa được = lỗ hổng), validate IAP | 🔴 |
| B4 | **Build & Release Runbook** | Tạo keystore, ký AAB, quy trình lên Play Console, versioning | 🔴 |
| B5 | **Test Plan / QA Checklist** | Kịch bản test trước bàn giao | 🟡 |
| B6 | **Sequence/Flow Diagram** | Luồng đăng nhập, sync save, giao dịch tiền | 🟡 |
| B7 | **Mobile Performance Budget** | Target FPS, draw call, RAM, đổ bóng (game đang tắt bóng thủ công) | 🟡 |
| B8 | **Asset/Art Pipeline + Naming Convention** | Cho 2 artist — quy ước đặt tên, format, scale | 🟡 |
| B9 | **Localization Plan** | Game đang hardcode tiếng Việt trong code | ⚪ |
| B10 | **Analytics & LiveOps** | Đo gì, vận hành sau ra mắt | ⚪ |

---

## 🟨 NHÓM C — VIẾT LẠI (đã có nhưng lỗi thời/mâu thuẫn — team tự sửa)

| # | Tài liệu | Vấn đề |
|---|---|---|
| C1 | `docs/DATA_SCHEMA.md` | Vẫn là blueprint UGS cũ → viết lại theo REST (gộp vào B2) |
| C2 | `docs/API_CONTRACTS.md` | Phần lớn là endpoint UGS không dùng → viết lại theo REST (gộp vào B1) |
| C3 | `docs/ARCHITECTURE.md` + `CONTEXT_RECOVERY.md` | Còn ghi Unity 2022 + UGS, mâu thuẫn thực tế Unity 6.3 + REST |
| C4 | **Bản hợp nhất mâu thuẫn** | Chốt 1 lần: số shop (12/13), backend, nền tảng build, thuật ngữ tiền tệ (POS/UPOS/Cá vàng/GEMS) |

---

## 📌 ƯU TIÊN TRONG 2 TUẦN MVP
- **Xin khách ngay (NHÓM A):** A1, A4, A5, A8 (4 cái 🔴 — chặn cả tiến độ).
- **Team tự viết gọn (NHÓM B):** B1+B2 (TDD+ERD), B3 (security), B4 (build runbook) — mỗi cái 1–2 trang.
- **Để sau:** A6/A7, B5–B10, phần lớn NHÓM C.

---

## ✉️ CÂU NHẮN GỢI Ý CHO BA
> "Để hoàn thiện dữ liệu thật + lên store, bên em cần khách cung cấp 4 nhóm tài liệu: **(1)** bảng số liệu cân bằng game (giá item, thời gian cây/thú, nâng cấp, EXP-level), **(2)** danh mục chi tiết 12–13 shop, **(3)** thông tin API web + cách đăng nhập tài khoản, **(4)** thông tin publish Android (Play Console, máy test). Các tài liệu kỹ thuật còn lại bên em tự viết. Mong khách ưu tiên nhóm (1),(3),(4) vì đang chặn tiến độ ạ."
