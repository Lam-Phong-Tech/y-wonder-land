# Đối chiếu 4 file vật nuôi — ĐỌC FILE NÀY TRƯỚC

> Có **4 file Excel vật nuôi** trong repo, số liệu mâu thuẫn nhau. File này chốt
> file nào dùng cho việc gì, và **hai cái bẫy** đã làm sai số ít nhất một lần.
> Soạn 29/07/2026, đối chiếu bằng script đọc thẳng ô Excel (kể cả công thức).

---

## 1. Bốn file là gì

| File | Vị trí | Thang tiền | Sửa lỗi 4 con? | Vai trò |
|---|---|---|---|---|
| `VatNuoi.xlsx` | gốc repo | **150** Point/USDT | ❌ chưa | Bản gốc thang 150 |
| `VatNuoi2.xlsx` | `Docs_KichBan/` | **26** Point/USDT | ❌ chưa | Bản thang 26, có sheet *Thuyết minh cách tính* |
| `SuaLai4VatNuoi.xlsx` | `Docs_KichBan/` | **26** Point/USDT | ✅ **rồi** | Chỉ 4 dòng: Hươu · Dê · Ngỗng · Thỏ |
| `VatNuoi3.xlsx` | `Docs_KichBan/` | **150** Point/USDT | ❌ chưa | Khách gửi 29/07 |

Hai trục khác nhau, **độc lập với nhau** — đây là lý do đọc dễ lú:

- **Trục A — thang tiền:** 1 USDT = 26 Point hay 150 Point.
- **Trục B — lỗi "Tổng Product 1":** đã sửa cho 4 con hay chưa.

---

## 2. `VatNuoi3` = `VatNuoi` + chỉnh RIÊNG con bò sữa

So từng ô (41 cột × 17 dòng): **đúng 12 ô khác nhau, toàn bộ nằm ở dòng 6 — Bò sữa.**
Chín con còn lại giống nhau từng chữ số.

| Ô | `VatNuoi.xlsx` | `VatNuoi3.xlsx` |
|---|---|---|
| Thức ăn chính | 2 Cỏ Voi | **4 Cỏ Voi** |
| Thức ăn phụ | 4 Khoai Lang | **2 Khoai Lang** |
| Giá sữa bò | 233,22 | **305** |
| Giá thịt bò | 1.510 | **1.975** |
| Lượng thức ăn cả vòng | 540 | **1.080** |
| Chi phí thức ăn | 20.250 | **40.500** |
| Doanh thu | 164.125 | **214.650** |
| Tổng chi phí | 65.647,75 | **85.897,75** |
| Tổng lợi nhuận | 98.477,25 | **128.752,25** |
| Tỷ suất | 250,01% | 249,89% |

Nói cách khác: khách **tăng gấp đôi khẩu phần bò sữa**, rồi nâng giá sữa/thịt vừa đủ để
tỷ suất đứng yên ở ~250%.

---

## 3. ⚠️ BẪY 1 — "Giá Product 1" SAI ở 4 con

Cột `Y` (Tổng Product 1 cả vòng) **phải bằng** `Số lượng Pro1 × Tổng lần thu`.
Ở 4 con dưới đây, `VatNuoi2` / `VatNuoi` / `VatNuoi3` **quên nhân số lượng mỗi lần thu**:

| Con | Mỗi lần thu | Số lần thu | `Y` **đúng** | `Y` trong file | Sai hệ số |
|---|---|---|---|---|---|
| Hươu | 2 nhung | 2 | **4** | 2 | ×2 |
| Dê con | 2 sữa | 60 | **120** | 60 | ×2 |
| Ngỗng con | 2 trứng | 30 | **60** | 30 | ×2 |
| Thỏ con | 8 lông | 2 | **16** | 2 | ×8 |

Sáu con còn lại (Bò · Rùa · Heo · Đà điểu · Gà · Vịt) đúng, vì `Số lượng Pro1 = 1`
hoặc công thức đã nhân sẵn.

**Vì sao nguy hiểm:** cột `Z` (Giá Product 1) được tính ngược `= AC / Y`. Chia cho `Y` sai
thì `Z` **bị thổi phồng đúng bằng hệ số đó**. Doanh thu tổng vẫn đúng, nhưng **đơn giá thì sai**.

`SuaLai4VatNuoi.xlsx` sinh ra chính là để sửa 4 dòng này:

| Con | Sản phẩm | Giá SAI (VatNuoi2) | Giá ĐÚNG (SuaLai4) |
|---|---|---|---|
| Hươu | Nhung hươu | 24.735 | **12.368** |
| Dê con | Sữa dê | 24 | **12** |
| Ngỗng con | Trứng ngỗng | 28 | **14** |
| Thỏ con | Lông thỏ | 172 | **21** |

❗ **`VatNuoi3` KHÔNG mang bản sửa này theo.** Nó vẫn để `Y` sai, nên `Z` vẫn phồng:
nhung hươu ghi **99.745** trong khi đơn giá thật (thang 150) phải là **49.872,5**.

Nhập thẳng số của `VatNuoi3` vào game ⇒ người chơi bán nhung hươu được **gấp đôi**,
lông thỏ **gấp 8 lần**.

---

## 4. ⚠️ BẪY 2 — Thang tiền 26 vs 150

| | Bò sữa (con giống) | Hươu (con giống) |
|---|---|---|
| Thang 26 (`VatNuoi2`, `SuaLai4`) | 7.800 | 10.400 |
| Thang 150 (`VatNuoi`, `VatNuoi3`) | 44.997,75 | 59.997 |

Giá trị thật **không đổi** — bò vẫn là 300 USDT ≈ 8.010.000đ. Chỉ có **đơn vị Point nhỏ đi
5,769 lần** (150 ÷ 26). Giống chuyện đổi mệnh giá tiền.

Hằng số nằm ở ô rời **dưới bảng**, không ô nào trong bảng trỏ tới nên rất dễ bỏ sót:

| File | Ô | Giá trị |
|---|---|---|
| `VatNuoi2` | `D17` | 26 |
| `SuaLai4` | `C11` | 26 |
| `VatNuoi` · `VatNuoi3` | `E16` | 0,006667 → **150** |
| `VatNuoi` · `VatNuoi3` | `E17` | 26.700 (VND/USDT) |

---

## 5. Game hiện đang chạy bộ nào

Đọc thẳng từ asset:

| Dữ liệu | Giá trong game | Khớp file nào |
|---|---|---|
| `Animal_cow_01.buyPrice` | 7.800 | thang **26** |
| `Animal_deer_01.buyPrice` | 10.400 | thang **26** |
| `deer_velvet_01.sellPrice` | 12.368 | **SuaLai4** (đã sửa) ✅ |
| `goat_milk_01.sellPrice` | 12 | **SuaLai4** ✅ |
| `goose_egg_01.sellPrice` | 14 | **SuaLai4** ✅ |
| `rabbit_fur_01.sellPrice` | 21 | **SuaLai4** ✅ |
| `milk_01.sellPrice` | 50 | `VatNuoi2` |
| `vaccine_01` · `medicine_01` | 30 · 70 | mọi file (giống nhau) |

➜ **Game = `VatNuoi2` + bản sửa `SuaLai4`, thang 26.** Bộ này đang **đúng và nhất quán**.

---

## 6. Muốn áp `VatNuoi3` thì phải làm gì

1. **Đừng chép cột `Z` (Giá Product 1) của 4 con Hươu · Dê · Ngỗng · Thỏ.**
   Tự tính lại: `Giá đúng = AC ÷ (Số lượng Pro1 × Tổng lần thu)`.
2. Đổi thang tiền cho **toàn bộ** game, không riêng vật nuôi — nếu không, cá / đá quý /
   nông sản / vé / tiền khởi điểm vẫn ở thang 26 và lệch 5,77 lần.
3. Đổi khẩu phần bò sữa **2 → 4 Cỏ Voi** (chỉ `VatNuoi3` có, `VatNuoi` thì không).
4. 8 loại cây làm thức ăn hiện `sellPrice = 0` trong game — `VatNuoi3` gán giá cho chúng
   (cỏ voi 37,5 · khoai lang 48 · dưa hấu 27 · bắp ngô 15 · bí ngô 3,5 · bắp cải 27 ·
   cà rốt 22 · rau muống 30). Cần chốt đây là giá **bán** hay giá **mua**.

---

## 7. Câu phải hỏi khách trước khi đụng dữ liệu

1. **Thang 150 Point/USDT áp cho toàn bộ game hay chỉ bảng vật nuôi?**
2. **`VatNuoi3` có thay thế `SuaLai4VatNuoi` không?** Nếu có thì lỗi ×2/×8 ở 4 con
   quay trở lại — cần khách xác nhận là cố ý hay sót.
3. Giá 8 loại cây trong cột *"Tính theo giá trồng được"* là **giá bán ra** hay **giá mua vào**?
