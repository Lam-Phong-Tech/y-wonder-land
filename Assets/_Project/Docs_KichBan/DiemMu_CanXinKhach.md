# 🕳️ ĐIỂM MÙ TÀI LIỆU — DANH SÁCH CẦN BA / KHÁCH CUNG CẤP

> Lập ngày 16/06/2026. Mục đích: gửi phòng **Marketing/BA** để xin khách làm rõ các thông số/spec còn thiếu.
> Cơ sở: đối chiếu **3 lớp** — (1) Kịch bản + Danh sách cửa hàng khách gửi, (2) tài liệu kỹ thuật nội bộ `docs/`, (3) **code thực tế** đã làm.
> Nguyên tắc đọc bảng: **🔴 chặn code** (không có là không làm được) · **🟡 chặn cân bằng/hoàn thiện** · **⚪ chặn bàn giao/online**.

---

## 0. ƯU TIÊN CAO NHẤT — phải chốt trước (gating)

| # | Vấn đề | Vì sao gấp | Hỏi ai |
|---|---|---|---|
| G1 | **Kiến trúc backend chốt 1 hướng**: tài liệu đang có **3 hướng mâu thuẫn** — Photon/Mirror + UGS + REST riêng. Cần chốt: DB (PostgreSQL hay MongoDB?), ngôn ngữ (Node/Go/Python?), host ở đâu, **có multiplayer/realtime thật không**, có push (FCM) không, có IAP thật không. | Không chốt thì không code lưu trữ thật được; ảnh hưởng toàn bộ online. | Khách + BA |
| G2 | **API web của khách**: web "đã có nhưng chưa có API". Cần: base URL, cơ chế đăng nhập chung (token/OAuth?), schema tài khoản, **ai làm API phía web**. | Chặn toàn bộ luồng "đăng nhập bằng account web" — là yêu cầu bắt buộc khi bàn giao. | BA (xin khách) |
| G3 | **Định danh đăng nhập thống nhất**: code register dùng **username (tên nhân vật)**, nhưng UI Quên-mật-khẩu lại đòi **email** → mâu thuẫn. Đăng nhập bằng gì: username / email / SĐT / social (Google/Apple)? | Quyết định cấu trúc tài khoản, không sửa được về sau dễ. | Khách |
| G4 | **Android publish**: min SDK bao nhiêu? Đã có tài khoản Google Play Console chưa? Máy test thật là máy nào? Build chỉ Android hay cả iOS/PC? | Là Definition of Done khi bàn giao; keystore phải tạo sớm (mất = không update được). | Khách + BA |

---

## 1. KINH TẾ / SHOP 🔴 (thiếu nhiều nhất — chặn code cứng)

| Cần gì | Hiện trạng | Mức |
|---|---|---|
| **Bảng giá đầy đủ TỪNG vật phẩm (~45 item)**: tên, mô tả, **giá mua**, **giá bán**, ảnh icon | Danh sách cửa hàng chỉ ghi **khoảng (range)** "10–200 POS", code đang để **giá tạm tay** + **icon emoji** | 🔴 |
| Giá bán **nông sản còn lại** (cải, rau muống, bí ngô, bắp, khoai, cỏ voi) + **11 sản phẩm cây lâu năm** | Chỉ cà rốt (15) & dưa hấu (45) có giá | 🔴 |
| **Dữ liệu 12–13 shop**: tên NPC/quầy, danh mục bán mỗi quầy, giá mua/bán riêng | Code mới có **1 shop mock** ("Hai Lúa"), dùng chung template | 🔴 |
| **Số shop chốt**: tài liệu chỗ ghi 12, chỗ 13, danh sách lại gộp đôi | Mâu thuẫn nội bộ | 🟡 |
| **Bảng nâng cấp dụng cụ (Tiệm Rèn)**: cấp tối đa, chi phí từng cấp, **bonus thật mỗi cấp** | Code đang dùng công thức tạm (POS=lvl×500…) và **bonus chưa ảnh hưởng gameplay** | 🟡 |
| **Giá mồi câu** (X POS / Y UPOS) | Để trống trong kịch bản | 🔴 |
| **Gói UPOS / Bundle nạp**: giá tiền thật (VNĐ), bonus kèm, skin kèm | Ghi "(+bonus)", "(+skin)" không có số | 🟡 |
| **Vốn khởi nghiệp + bộ đồ khởi đầu** người chơi mới | Code tạm: **5000 POS** + cuốc + xô + 5 hạt cà rốt | 🟡 |
| **Thuật ngữ tiền tệ thống nhất**: đang lẫn lộn **POS / UPOS / "Cá vàng" / "Kim cương" / GEMS / GOLD** giữa các module | Quest ghi "Cá vàng", economy ghi POS, schema ghi GEMS/GOLD | 🔴 |

---

## 2. NÔNG TRẠI / CÂY TRỒNG 🔴

| Cần gì | Hiện trạng | Mức |
|---|---|---|
| **Thời gian sinh trưởng + tưới + sản lượng + EXP + thưởng POS** cho từng cây (8 cây) | Định tính ("rau muống ngắn nhất"…), code để **DEMO 30–60s** | 🔴 |
| **Cây lâu năm (10 loại)**: thời gian mỗi stage, số lần ra quả, sản lượng/lần, giá | Hoàn toàn không có số | 🔴 |
| **Cơ chế héo/chết khi quên tưới — định lượng**: trễ bao lâu thì héo, mất bao nhiêu % EXP/sản lượng, có chết hẳn không | Chỉ mô tả "mất một phần" | 🟡 |
| **Phân bón**: rút ngắn thời gian / tăng sản lượng bao nhiêu %? | Không có | 🟡 |

---

## 3. CHĂN NUÔI / ĐỘNG VẬT 🔴

| Cần gì | Hiện trạng | Mức |
|---|---|---|
| **Vòng đời từng vật nuôi**: chu kỳ ra sản phẩm, số vụ trước khi hết tuổi, sản lượng | Định tính, code để DEMO 30–45s | 🔴 |
| **Cơ chế đói/bệnh/chết — định lượng**: cữ ăn mấy giờ, bao lâu không ăn thì chết, xác suất bệnh, chu kỳ tiêm vacxin | Mô tả định tính ("ăn đúng cữ nếu không chết") | 🟡 |
| **Sức chứa chuồng**: mỗi chuồng mấy ô / mấy con? Trần số chuồng tối đa (vượt 10 = 50 UPOS/chuồng nhưng tối đa bao nhiêu?) | Code tạm `maxCapacity=3`; mâu thuẫn "4x4 khi cuốc" vs "tối đa 10 chuồng/người" | 🔴 |
| **Sản phẩm đặc biệt** (nhung hươu, lông thỏ, trứng đà điểu/ngỗng…): định nghĩa item + giá | Có model nhưng chưa có item/giá đầu ra | 🟡 |

---

## 4. XÂY DỰNG / TILE / MAP 🟡

| Cần gì | Hiện trạng | Mức |
|---|---|---|
| **Hệ tọa độ đảo thống nhất**: "đảo 900 ô", "cuốc ra 4x4", "chuồng tối đa 10" — kích thước ô (mét) + origin chưa hợp nhất | 3 mô tả rời rạc | 🔴 |
| **Chi phí lát ô bằng búa** (cơ chế Minecraft mới) + nút HUD thật | Code tạm **4 đá + 4 gỗ/ô**, phím **G** | 🟡 |
| **3 scene chưa tồn tại** (Mỏ, Hải Phú, Mộc Nhi): có làm trong đợt này không? Nếu có cần layout + spawn point + asset | Chưa có | ⚪ |

---

## 5. CÂU CÁ 🟡

| Cần gì | Hiện trạng | Mức |
|---|---|---|
| **Danh sách loài cá** + tỉ lệ câu từng loại + giá bán từng loại | Chỉ "phổ thông/hiếm", giá range 10–200 | 🔴 |
| Mồi câu tăng tỉ lệ cá hiếm **bao nhiêu %** | Không có | 🟡 |
| "10 lượt free/ngày" **reset lúc nào** (00:00? giờ server?) | Chưa rõ | 🟡 |

---

## 6. NHÂN VẬT / LEVEL-EXP / QUEST 🔴

| Cần gì | Hiện trạng | Mức |
|---|---|---|
| **Bảng EXP đầy đủ mỗi level + Level Cap** | Chỉ 2 mốc ví dụ (Lv1→2=100, Lv2→3=250). **Hệ Level/EXP chưa code** (có TODO trong code) | 🔴 |
| **Bảng unlock theo level** (level nào mở gì) | Chỉ biết Lv10 Mỏ, Lv40 Hải Phú, Lv60 Mộc Nhi | 🟡 |
| **Danh sách Quest thật** (daily/weekly/main): điều kiện, phần thưởng, người giao | Chỉ 4 quest mock trong code | 🟡 |
| **Phần thưởng tutorial** chính thức | Code tạm +50 POS +20 EXP + ít hạt | ⚪ |

---

## 7. ART / AUDIO ⚪

| Cần gì | Hiện trạng | Mức |
|---|---|---|
| **Danh sách model MVP chốt** (trong ~46 model) + map model nào ưu tiên | Chỉ "bộ tối thiểu", đếm số model con lệch (10 vs 11) | 🟡 |
| **Ai làm Sound?** Cần voiceover cinematic + BGM + SFX nhưng ê-kíp không có sound designer | Trách nhiệm bỏ ngỏ | 🟡 |
| **Icon vật phẩm thật** (Sprite) thay emoji tạm | Đang dùng emoji | 🟡 |
| **Cosmetic**: danh sách tóc/áo/phụ kiện + giá + cách gắn lên rig | Không có spec | ⚪ |

---

## 8. BACKEND / DATA CONTRACT ⚪ (cho lưu trữ thật)

> Hiện **Economy / Inventory / Farm / Animal / Resource / Fishing / Build layout / Quest** đều lưu tạm bằng **PlayerPrefs (local)** — sẽ **mất khi đổi thiết bị** và **không chống cheat** được. Mỗi hệ thống cần 1 hợp đồng dữ liệu (schema + endpoint) chốt với backend:

- **Item catalog + currency POS/UPOS**: cần 1 "nguồn sự thật" duy nhất (hiện code đã đi trước tài liệu).
- **Heo Đất**: lãi tính server-side + đáo hạn tự cộng → cần **đồng bộ thời gian server** (chống chỉnh giờ máy). Hiện PiggyBank còn **tách rời** EconomyManager (gửi/rút chưa trừ tiền thật) — cần nối.
- **Inventory** thật có 6 tab + trang bị + độ bền, schema hiện chỉ có id/số lượng.
- **Migration plan**: tài liệu bắt buộc có nhưng **chưa tồn tại**.
- **Viết lại `docs/DATA_SCHEMA.md` + `docs/API_CONTRACTS.md` theo REST** (hiện vẫn là blueprint UGS cũ, chỉ dán nhãn cảnh báo).

---

## 📌 TÓM TẮT 1 DÒNG ĐỂ GỬI BA

> Cần khách cung cấp gấp: **(1) bảng giá + thông số cân bằng đầy đủ** (item/cây/thú/shop/nâng cấp/cá), **(2) bảng EXP-level + unlock**, **(3) chốt backend + API web + cách đăng nhập**, **(4) thông tin publish Android**. Đây đều là thứ team không tự bịa được vì ảnh hưởng trực tiếp luật chơi và doanh thu của khách.

---

## ⚠️ MÂU THUẪN NỘI BỘ TÀI LIỆU (cần BA hợp nhất)
- Số shop: **12 vs 13**.
- Backend: **Photon/Mirror vs UGS vs REST riêng** (3 nơi viết khác nhau).
- Nền tảng build: **iOS+Android+PC vs chỉ Android**.
- Engine version: docs ghi **Unity 6.3** nhưng CONTEXT_RECOVERY còn ghi **Unity 2022 + UGS**.
- Tiền tệ: **POS/UPOS vs GEMS/GOLD vs "Cá vàng"/"Kim cương"**.
- Hệ ô đất: **"4x4 khi cuốc" vs "chuồng ≤10" vs "đảo 900 ô"** chưa thành 1 hệ tọa độ.

---

## ✅ PHẦN ĐÃ ĐỦ SỐ ĐỂ CODE NGAY (không cần hỏi)
12 preset camera (có tọa độ/FOV) · chi phí xây cơ bản (đường/đèn/chuồng) · lãi suất Heo Đất (2%/6%/45%) · luật QTE câu cá cơ bản · validate đăng ký/mật khẩu.
