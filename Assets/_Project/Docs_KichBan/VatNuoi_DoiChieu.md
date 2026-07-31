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

### 🔑 Cột USDT KHÔNG hề đổi — đây là lối ra

So cột `D` (USDT) giữa `VatNuoi2` và `VatNuoi3`: **giống hệt nhau, cả 10 con.**

| Con | USDT (cả 2 bản) | Con | USDT (cả 2 bản) |
|---|---|---|---|
| Bò sữa | 300 | Gà mái | 6 |
| Rùa con | 90 | Dê con | 50 |
| Heo con | 100 | Ngỗng con | 10 |
| Đà điểu | 170 | Thỏ con | 5 |
| Hươu | 400 | Vịt | 8 |

Nghĩa là **thiết kế giá trị thật chưa từng thay đổi**. Chỉ đổi cách quy ra Point.
➜ Nguồn sự thật nên là **cột USDT**, còn Point là số **suy ra** tại thời điểm build:

```
Giá Point = Giá USDT × tỷ giá ví
```

Làm vậy thì đổi tỷ giá lúc nào cũng được mà **không phải soạn lại bảng**, và
không bao giờ lặp lại tình trạng 4 file đá nhau như hiện nay.

### ⚠️ Tỷ giá THẬT của hệ thống là 26,5 — không phải 26, càng không phải 150

Ví Point là **tiền thật, quy đổi được với USDT trên web**, không phải tiền chỉ dùng trong game:

| Nguồn | Tỷ giá | Ghi chú |
|---|---|---|
| `docs/POINT_WALLET_BUSINESS_RULES.md` | **1 USDT = 26,5 Point** | có version, ghim vào từng source lot |
| `docs/ADR_POINT_WALLET_AUTHORITY.md` | **26,5 Point/USDT** | dùng cả cho định giá hoa hồng |
| Mốc VIP | **2.650 Point** | = 100 USDT × 26,5 |
| Bảng cân bằng game (VatNuoi2/SuaLai4) | 26 | **làm tròn xuống**, lệch ~1,9% |
| `VatNuoi3` | 150 | mâu thuẫn với ví |

Hệ quả: giá trong game hiện **rẻ hơn ~1,9%** so với ý định thiết kế
(bò 7.800 Point thay vì 300 × 26,5 = 7.950 Point).

**Đổi tỷ giá sang 150 KHÔNG phải quyết định cân bằng game — đó là quyết định tài chính.**
Nó định giá lại toàn bộ số dư người dùng đang có, mọi hoa hồng đang treo, và mốc VIP.
Không được đổi từ phía game.

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

## 7. ✅ ĐÃ CHỐT 29/07/2026 — GIỮ NGUYÊN `VatNuoi2` + `SuaLai4`

**Chủ dự án quyết định: dùng số liệu `VatNuoi2` (kèm bản sửa `SuaLai4`), KHÔNG áp `VatNuoi3`.**

Lý do: giá trên **web — nơi người chơi nạp tiền vào game — đã chốt từ trước** và đang chạy thật.
Khách hỏi lại `VatNuoi3` là do quên rằng số liệu đã thống nhất với web, không phải muốn đổi.

➜ **Không sửa một dòng dữ liệu nào.** Game hiện đã đúng bộ này (xem mục 5).
➜ Việc duy nhất còn lại: **làm rõ với khách rằng lợi nhuận trong bảng là CẢ VÒNG NUÔI**
(thu lượt đầu + vụ cuối), **không phải mỗi tháng**.

Chấp nhận sai lệch đã biết: bảng cân bằng dùng tỷ giá 26 trong khi ví dùng 26,5,
nên giá trong game rẻ hơn ~1,9% so với ý định thiết kế. Nhỏ, không sửa lúc này.

> ⚠️ Nếu sau này khách lại gửi bảng mới với tỷ giá khác — **hỏi lại web trước khi đụng game**.
> Point là tiền thật quy đổi được với USDT, đổi tỷ giá là quyết định tài chính chứ không
> phải cân bằng game.

---

## 8. Tham khảo — nếu về sau thật sự phải đổi

### Quy tắc chốt

> **Cột USDT là nguồn sự thật. Point là số suy ra: `Point = USDT × 26,5`.**

Giữ tỷ giá **26,5** theo hệ thống ví đang chạy. Bỏ qua con số 150 trong `VatNuoi3`
— nó chỉ là cách khách ghi lại **cùng một giá trị** bằng đơn vị khác.

### Lấy gì từ file nào

| Loại dữ liệu | Lấy từ | Lý do |
|---|---|---|
| Giá con giống | **cột USDT** (mọi file như nhau) × 26,5 | thiết kế chưa từng đổi |
| Đơn giá 4 con Hươu·Dê·Ngỗng·Thỏ | **`SuaLai4VatNuoi`** | file duy nhất đã sửa lỗi ×2/×8 |
| Đơn giá 6 con còn lại | `VatNuoi3` (quy về thang 26,5) | bản mới nhất |
| Khẩu phần bò sữa **4 Cỏ Voi** | **`VatNuoi3`** | chỉ file này có |
| Giá 8 cây làm thức ăn | `VatNuoi3` (quy về thang 26,5) | file duy nhất có giá cây |
| Chu kỳ · số lần thu · bệnh · EXP | `VatNuoi2` = `VatNuoi3` | giống nhau, game đã đúng |

### Cách quy đổi số của `VatNuoi3` về thang ví

```
Giá thang 26,5 = Giá trong VatNuoi3 × (26,5 ÷ 150) = × 0,176667
```

Ví dụ: sữa bò `305 × 0,176667 = 53,9` Point · thịt bò `1.975 × 0,176667 = 348,9` Point.

⚠️ Riêng 4 con Hươu·Dê·Ngỗng·Thỏ **phải chia lại số lượng trước**:
`Đơn giá = Doanh thu Pro1 ÷ (Số lượng Pro1 × Tổng lần thu)`, rồi mới nhân 0,176667.

---

### Nếu mở lại chuyện này, ba câu phải hỏi khách trước

1. **`VatNuoi3` có thay thế `SuaLai4VatNuoi` không?** `SuaLai4` ra **trước** `VatNuoi3`
   nhưng `VatNuoi3` lại không mang bản sửa theo. Cần biết là cố ý hay sót.
2. **Xác nhận tỷ giá ví.** Con số 150 trong `VatNuoi3` chỉ là cách ghi khác
   của cùng giá trị USDT — đổi tỷ giá ví là quyết định tài chính, không phải cân bằng game.
3. Giá 8 loại cây trong cột *"Tính theo giá trồng được"* là **giá bán ra** hay **giá mua vào**?

---

## 9. Doanh thu về LÚC NÀO — số để giải thích với khách

Đây là chỗ khách hiểu nhầm thành "lợi nhuận mỗi tháng". Số theo `VatNuoi2`:

| Con | Thu rải trong kỳ (Pro1) | Thu vụ cuối (Pro2) | % doanh thu dồn vào vụ cuối |
|---|---|---|---|
| Hươu | 49.470 (2 đợt: tháng 6 và 12) | 37.320 | 43% |
| Bò sữa | 19.000 (38 đợt) | 16.250 | 46% |
| Dê con | 1.440 (60 đợt) | 2.360 | 62% |
| Đà điểu | 12.270 (30 đợt) | 21.000 | 63% |
| Gà mái | 495 (45 đợt) | 1.550 | 76% |
| Ngỗng con | 840 (30 đợt) | 3.325 | 80% |
| Thỏ con | 344 (2 đợt) | 1.445 | 81% |
| Vịt | 202,5 (45 đợt) | 1.660 | 89% |
| **Heo con** | 7.042 — **1 đợt duy nhất ở CUỐI kỳ** | 14.600 | **100%** |
| **Rùa con** | 11.893 — **1 đợt duy nhất ở CUỐI kỳ** | 10.840 | **100%** |

**Heo và Rùa không thu được gì suốt 6 và 10 tháng đầu** — cả hai khoản đều rơi vào ngày cuối.
Đây là điểm đáng lưu ý nhất khi giải thích với khách.
