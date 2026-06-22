# 📊 RÀ SOÁT SỐ LIỆU KỊCH BẢN ↔ CODE — Y WONDER GREEN FARM

> Đối chiếu `VatNuoi.md` · `CayTrong.md` · `CachTinh.md` với generator code (`ItemDataGenerator.cs`).
> Lập **21/06/2026** (Dev). Mục đích: liệt kê điểm **MÂU THUẪN / CẦN CHỐT** để BA/Sếp duyệt thang số trước khi áp "y nguyên".
>
> ⚠️ **CẬP NHẬT 22/06:** Sếp đã chốt **giá MUA = USDT** + rau ngắn ngày = feed + cây lâu năm 3 loại. **ĐÍNH CHÍNH framing "kinh tế thủng / lời 300 lần" bên dưới là SAI** — con số đó là tổng-thu-cả-đời ÷ giá-mua, BỎ QUA thức ăn + thời gian. Tính cả thức ăn (như công thức sếp) thì lời thật ~250-400%, KHÔNG phải thủng. Bản hỏi khách đúng: `PhieuHoi_Khach_DeHieu.md`.

**Chú giải:** 🟡 cần sếp chốt · 🔴 thiếu data / lệch · 🟢 đã đúng, không cần sửa.

---

## A. TÓM TẮT — 9 ĐIỂM

| # | Vấn đề | Mô tả ngắn | Trạng thái |
|---|---|---|:--:|
| 1 | ⭐ **Giá MUA con giống/cây giống có 3 THANG SỐ** | Mỗi con có 3 số: *Định giá* (bò 44.997) – *USDT* (300) – *demo* code (1.500). Công thức lợi nhuận của sếp tính theo *Định giá*. Cần chốt dùng cột nào. | 🟡 CẦN SẾP CHỐT |
| 2 | **Độ lời khi mua rẻ (USDT) + bán theo doc** | Bò mua 300, thu cả đời (38 vụ) ≈ 164k. Tổng-thu ÷ giá-mua ≈ 547× — NHƯNG bỏ qua thức ăn + 9 tháng nuôi. Tính cả thức ăn (công thức sếp) → lời ~250-400%, KHÔNG thủng. | ✅ SẾP CHỐT USDT; chốt nốt giá BÁN |
| 3 | **Mâu thuẫn NỘI BỘ trong tài liệu** | Sữa bò: VatNuoi **233** vs CachTinh **305**. Dưa hấu giá bán: 45 vs 50. Code phải chọn 1 (đang theo VatNuoi). | 🟡 CẦN SẾP CHỐT |
| 4 | **Cây lâu năm: doc chỉ có 3/10 cây** | CayTrong.md chỉ có Sa Chi, Sầu Riêng, Chanh dây. Code để 10 cây (7 cây placeholder tự đặt). **Chanh dây bị BỎ SÓT** trong code. | 🔴 THIẾU DATA |
| 5 | **EXP thu hoạch (doc có) chưa vào game** | Doc có cột "EXP thu hoạch lần cuối" (bò 10.000...). Game CHƯA có hệ EXP/level nên chưa dùng. | 🔴 PHASE 2 |
| 6 | **Công thức bệnh/vaccine/thuốc/lợi nhuận chưa vào game** | CachTinh có tỉ lệ phát bệnh 0.3/0.4, số vaccine, chi phí thuốc, tỉ suất lợi nhuận. Game mô phỏng bệnh bằng "đói", không theo công thức. *(Đây là bảng tính RA giá — có thể không cần vào game.)* | 🟡 CHỜ Ý SẾP |
| 7 | **Asset `.asset` đang giữ số DEMO cũ (chưa bake)** | Generator đã khai đúng theo ngày thật, nhưng file asset còn 25s/40s (thú) & 45–60s (cây). PHẢI chạy lại generator trong Unity. | 🔴 HÀNH ĐỘNG (Dev) |
| 8 | ✅ **Chu kỳ/sản lượng/thức ăn VẬT NUÔI đã ĐÚNG y nguyên** | Cả 10 con: chu kỳ thu, số lần thu, thức ăn, sản phẩm, thịt, số ô, giá bán SP — KHỚP VatNuoi.md 100%. | 🟢 ĐÃ ĐÚNG |
| 9 | ✅ **Cây ngắn ngày (8 cây): giá/sản lượng/EXP đã đúng** | Giá hạt, giá bán, sản lượng, EXP khớp doc (2 giá làm tròn nhỏ). Thời gian lớn/tưới/POS là số tự đặt vì doc KHÔNG có cột đó. | 🟢 ĐÃ ĐÚNG (1 phần) |

---

## B. ⭐ GIÁ MUA CON GIỐNG — 3 THANG SỐ (cần chốt 1)

> *Định giá* giữ đúng tỉ suất lợi nhuận công thức sếp · *USDT* (✅ ĐÃ CHỌN) nhỏ dễ chơi, lời cao hơn công thức (~250-400%, không thủng) · *Demo* là số tạm cũ.

| # | Con vật | Định giá (gốc) | USDT | AnimalDefinition (đang dùng) | ItemDefinition / Shop (đang TÍNH TIỀN) | Giá bán SP chính (đã đúng doc) |
|---|---|--:|--:|--:|--:|--|
| 1 | Bò sữa | 44.997 | 300 | 300 | **1.500** | Sữa bò 233 |
| 2 | Rùa con | 13.499 | 90 | 90 | **THIẾU** | Mai rùa 38.860 |
| 3 | Heo con | 14.999 | 100 | 100 | **1.000** | Da heo 25.420 |
| 4 | Đà điểu V2 | 25.499 | 170 | 170 | **800** | Trứng ĐĐ 1.088 |
| 5 | Hươu | 59.997 | 400 | 400 | **1.800** | Nhung hươu 99.745 |
| 6 | Gà mái V2 | 900 | 6 | 6 | **500** | Trứng gà 28 |
| 7 | Dê con V2 | 7.500 | 50 | 50 | **900** | Sữa dê 100 |
| 8 | Ngỗng con V2 | 1.500 | 10 | 10 | **THIẾU** | Trứng ngỗng 65 |
| 9 | Thỏ con V2 | 750 | 5 | 5 | **400** | Lông thỏ 345 |
| 10 | Vịt V3 | 1.200 | 8 | 8 | **THIẾU** | Trứng vịt 12 |

**Ghi chú:**
- *Định giá / USDT / VND* trong doc là 3 cách quy đổi của **CÙNG 1 giá trị** (1 USDT = 26.700 VND; Định giá ≈ USDT × 150).
- Công thức "Tổng chi phí" & "Tỉ suất lợi nhuận" của sếp (CachTinh.md) **DÙNG cột *Định giá*** → muốn tỉ suất đúng "y nguyên" thì giá mua = Định giá.
- `AnimalDefinition` đang dùng *USDT* (số nhỏ); nhưng **SHOP tính tiền theo `ItemDefinition`** (số demo) → người chơi đang trả số DEMO, không phải doc.
- 3 con **Rùa / Ngỗng / Vịt THIẾU `ItemDefinition`** → hiện KHÔNG mua được ở shop dù danh sách có (cần thêm khi chốt thang giá).

**→ 3 lựa chọn cho sếp:**
- **(A) Cột *Định giá*** (bò 44.997): giữ đúng tỉ suất ~250%, kinh tế cân bằng nhưng giá cao, phải cày nhiều.
- **(B) Cột *USDT*** (bò 300) ← ✅ SẾP ĐÃ CHỌN: dễ chơi, khớp AnimalDefinition. Lời cao hơn công thức (~250-400% khi tính cả thức ăn), KHÔNG phải "thủng".
- **(C) Giữ giá demo** (bò 1.500): không theo doc, chỉ để bàn giao kịp, cân kinh tế sau.

---

## C. CÂY LÂU NĂM — DOC CHỈ CÓ 3/10 CÂY

| # | Cây | Giá game (gốc) | USDT | Giá bán SP (doc) | Code hiện tại | Trạng thái |
|---|---|--:|--:|--|--|:--:|
| 2 | Sa Chi | 40.498 | 270 | 1.118 (Hộp Sa Chi) | hạt demo 300; SP 1.118 ✓ | 🟡 SP đúng, hạt demo |
| 4 | Sầu Riêng | 104.995 | 700 | 3.575 (Hộp Sầu Riêng) | hạt demo 500; SP 3.575 ✓ | 🟡 SP đúng, hạt demo |
| 9 | Chanh dây | 8.999 | 60 | 325 (Hộp mứt chanh dây) | **KHÔNG có trong code** | 🔴 BỎ SÓT — cần thêm |
| — | 7 cây khác (chuối, dừa, cau, chà là, trà, măng tây, hồng sâm, sâm tiến vua) | — | — | — | placeholder tự đặt | 🔴 Doc KHÔNG có số |

> Cây ngắn ngày (8 cây): giá hạt / giá bán / sản lượng / EXP **đã áp đúng doc** (2 giá làm tròn nhỏ: cỏ voi 37.5→38, bí ngô 3.54→4). Thời gian lớn / chu kỳ tưới / POS = số tự đặt vì doc ngắn ngày **không có** các cột đó.

---

## D. 🟢 VẬT NUÔI — CÁC TRƯỜNG ĐÃ ÁP ĐÚNG "Y NGUYÊN" (KHỚP VatNuoi.md 100%)

> Phần này không cần sửa — để sếp yên tâm phần lõi chăn nuôi đã chuẩn.

| # | Con vật | Ô chuồng | Ăn/ngày | Thức ăn (chính + phụ) | Sản phẩm × SL | Chu kỳ thu (ngày) | Số lần thu | Thịt × SL |
|---|---|:--:|:--:|--|--|:--:|:--:|--|
| 1 | Gà mái V2 | 1 | 1 | 2 Bắp Ngô + Cám | Trứng gà ×1 | 2 | 45 | Thịt gà ×5 |
| 2 | Bò sữa | 9 | 1 | 2 Cỏ Voi + 4 Khoai Lang | Sữa bò ×10 | 7 | 38 | Thịt bò ×50 |
| 3 | Heo con | 9 | 1 | 2 Khoai lang + 2 Bí ngô | Da heo ×1 | 180 | 1 | Thịt heo ×50 |
| 4 | Đà điểu V2 | 1 | 1 | 4 Dưa Hấu + Cám | Trứng đà điểu ×1 | 6 | 30 | Thịt ĐĐ ×20 |
| 5 | Hươu | 9 | 1 | 5 Bắp Ngô + Cám | Nhung hươu ×2 | 180 | 2 | Thịt hươu ×40 |
| 6 | Dê con V2 | 9 | 1 | 2 Bí ngô + 2 Cỏ voi | Sữa dê ×2 | 3 | 60 | Thịt dê ×20 |
| 7 | Thỏ con V2 | 1 | 1 | 1 Cà rốt + 1 Bắp ngô | Lông thỏ ×8 | 40 | 2 | Thịt thỏ ×5 |
| 8 | Ngỗng con V2 | 1 | 1 | 2 Bắp Cải + 3 Bắp Ngô | Trứng ngỗng ×2 | 3 | 30 | Thịt ngỗng ×5 |
| 9 | Vịt V3 | 1 | 0.5 | 1 Bắp Ngô + Cám | Trứng vịt ×1 | 1 | 45 | Thịt vịt ×5 |
| 10 | Rùa con | 1 | 7 | 7 Rau Muống + 12 Dưa hấu | Mai rùa ×1 | 300 | 1 | Thịt rùa ×10 |

> ⚠️ **Lưu ý:** bản rà soát tự động ban đầu báo "lệch chu kỳ 6 con" là do **đọc nhầm cột "Số tháng nuôi" thành "Chu kỳ thu hoạch"**. Đối chiếu trực tiếp generator ↔ VatNuoi.md xác nhận **cả 10 con đã đúng**.

---

## E. KẾT LUẬN

1. **Phần lõi chăn nuôi + cây ngắn ngày: ĐÃ áp đúng "y nguyên".** Không cần sửa.
2. **Nút thắt chờ sếp:** thang giá MUA (mục B) — quyết định cả nền kinh tế.
3. **Thiếu data khách:** Chanh dây + 7 cây lâu năm + 3 con thiếu ItemDefinition.
4. **Hành động Dev (không chờ ai):** chạy lại 4 generator để bake số mới (asset đang giữ số demo cũ).
5. Sau khi sếp chốt thang giá → áp y nguyên + thêm Chanh dây + 3 con thiếu trong 1 lần.
