# VatNuoi3.xlsx — Sheet duy nhất: `Vật nuôi`

> Khách gửi 29/07/2026. Chuyển từ `VatNuoi3.xlsx` sang Markdown, **giữ nguyên số gốc**.
> File chỉ có **1 sheet** — KHÔNG còn sheet *Thuyết minh cách tính* như `VatNuoi2.xlsx`.
> Công thức trong file đã được bóc ra ở mục [Công thức thật](#công-thức-thật-trong-file) bên dưới,
> vì đây mới là chỗ định nghĩa ý nghĩa các cột — đọc số không thôi rất dễ hiểu sai.

---

## 1. Hằng số quy đổi (ô rời nằm dưới bảng, rất dễ bỏ sót)

| Ô | Giá trị | Ý nghĩa |
|---|---|---|
| `E16` | 0.006667 | 1 Point = 0,006667 USDT → **1 USDT = 150 Point** |
| `E17` | 26.700 | **1 USDT = 26.700 VND** |
| `G16` | 15 | Không rõ dùng vào đâu, không ô nào tham chiếu tới |

⚠️ **`VatNuoi2.xlsx` dùng 1 USDT = 26 Point.** File này đổi thành **150** —
mọi giá con giống vì thế tăng đúng **×5,769** (= 150 ÷ 26).

---

## 2. Thông tin vật nuôi

| STT | Con giống | Định giá (Point) | USDT | Quy đổi VND | ? | Ô chuồng | Thức ăn 1 (chính) | Thức ăn 2 (thay thế) | Chu kỳ cho ăn (ngày) | EXP thu hoạch lần cuối |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | Bò sữa | 44.997,75 | 300 | 8.010.000 | 299,88 | 9 Slot | 4 Cỏ Voi | 2 Khoai Lang | 1 | 10.000 |
| 2 | Rùa con | 13.499,33 | 90 | 2.403.000 | 79,99 | 1 Slot | 7 Rau Muống | 12 Dưa hấu | 7 | 20.000 |
| 3 | Heo con | 14.999,25 | 100 | 2.670.000 | 99,88 | 9 Slot | 2 Khoai lang | 2 Bí ngô | 1 | 8.000 |
| 4 | Đà điểu V2 | 25.498,73 | 170 | 4.539.000 | 139,99 | 1 Slot | 4 Dưa Hấu | cám | 1 | 5.000 |
| 5 | Hươu | 59.997,00 | 400 | 10.680.000 | 2,99 | 9 Slot | 5 Bắp Ngô | CÁM | 1 | 15.000 |
| 6 | Gà mái V2 | 899,96 | 6 | 160.200 | 6,99 | 1 Slot | 2 Bắp Ngô | CÁM | 1 | 3.000 |
| 7 | Dê con V2 | 7.499,63 | 50 | 1.335.000 | 49,88 | 9 Slot | 2 Bí ngô | 2 Cỏ voi | 1 | 8.000 |
| 8 | Ngỗng con V2 (mới) | 1.499,93 | 10 | 267.000 | 11,99 | 1 Slot | 2 Bắp Cải | 3 Bắp Ngô | 1 | 4.500 |
| 9 | Thỏ con V2 (mới) | 749,96 | 5 | 133.500 | 4,99 | 1 Slot | 1 Cà rốt | 1 Bắp ngô | 1 | 1.500 |
| 10 | Vịt V3 (mới) | 1.199,94 | 8 | 213.600 | 6,99 | 1 Slot | 1 Bắp Ngô | CÁM | 0.5 | 1.000 |

## 3. Phòng / chữa bệnh

| Con giống | Thời điểm phát bệnh | Tỉ lệ phát bệnh | Số vắc-xin cần | Tổng chi phí vắc-xin | Tổng chi phí thuốc | Số thuốc trị bệnh cần |
|---|---|---|---|---|---|---|
| Bò sữa | 0,30 | 0,40 | 4 | 120 | 700 | 10 |
| Rùa con | 0,50 | 0,60 | 4 | 120 | 700 | 10 |
| Heo con | 0,50 | 0,30 | 4 | 120 | 630 | 9 |
| Đà điểu V2 | 0,30 | 0,40 | 4 | 120 | 700 | 10 |
| Hươu | 0,15 | 0,60 | 4 | 120 | 630 | 9 |
| Gà mái V2 | 0,15 | 0,60 | 3 | 90 | 210 | 3 |
| Dê con V2 | 0,30 | 0,30 | 4 | 120 | 700 | 10 |
| Ngỗng con V2 (mới) | 0,15 | 0,40 | 4 | 120 | 140 | 2 |
| Thỏ con V2 (mới) | 0,40 | 0,50 | 2 | 60 | 140 | 2 |
| Vịt V3 (mới) | 0,15 | 0,60 | 2 | 60 | 140 | 2 |

## 4. Sản phẩm thu hoạch

| Con giống | Product 1 | Số lượng Pro1 | Product 2 | Số lượng Pro2 | Chu kỳ thu hoạch (ngày) | Tổng lần thu | Số tháng nuôi |
|---|---|---|---|---|---|---|---|
| Bò sữa | Sữa bò | 10 | Thịt bò | 50 | 7 | 38 | 9 |
| Rùa con | Mai rùa | 1 | Thịt rùa | 10 | 300 | 1 | 10 |
| Heo con | Da heo | 1 | Thịt heo | 50 | 180 | 1 | 6 |
| Đà điểu V2 | Trứng đà điểu V2 | 1 | Thịt đà điểu V2 | 20 | 6 | 30 | 6 |
| Hươu | Nhung hươu | 2 | Thịt Huơu | 40 | 180 | 2 | 12 |
| Gà mái V2 | Trứng gà V2 | 1 | Thịt gà V2 | 5 | 2 | 45 | 3 |
| Dê con V2 | Sữa dê V2 | 2 | Thịt dê V2 | 20 | 3 | 60 | 6 |
| Ngỗng con V2 (mới) | Trứng ngỗng V2 | 2 | Thịt ngỗng V2 | 5 | 3 | 30 | 3 |
| Thỏ con V2 (mới) | Lông thỏ V2 | 8 | Thịt thỏ V2 | 5 | 40 | 2,0025 | 2,67 |
| Vịt V3 (mới) | Trứng vịt V3 | 1 | Thịt vịt V3 | 5 | 1 | 45 | 1,50 |

## 5. Tính toán thu hoạch

| Con giống | Tổng Product 1 (cả vòng) | Giá Product 1 | Tổng Product 2 (vụ cuối) | Giá Product 2 | Tổng giá trị thu lại (Pro1) | Tổng giá trị thu lại (Pro2) |
|---|---|---|---|---|---|---|
| Bò sữa | 380 | 305 | 50 | 1.975 | 115.900 | 98.750 |
| Rùa con | 1 | 38.860 | 10 | 3.428 | 38.860 | 34.280 |
| Heo con | 1 | 25.420 | 50 | 1.056 | 25.420 | 52.800 |
| Đà điểu V2 | 30 | 1.088 | 20 | 2.788,50 | 32.640 | 55.770 |
| Hươu | 2 | 99.745 | 40 | 3.762 | 199.490 | 150.480 |
| Gà mái V2 | 45 | 28 | 5 | 816 | 1.260 | 4.080 |
| Dê con V2 | 60 | 100 | 20 | 495 | 6.000 | 9.900 |
| Ngỗng con V2 (mới) | 30 | 65 | 5 | 1.569 | 1.950 | 7.845 |
| Thỏ con V2 (mới) | 2,0025 | 345 | 5 | 577 | 690 | 2.885 |
| Vịt V3 (mới) | 45 | 12 | 5 | 942 | 540 | 4.710 |

## 6. Cho ăn

| Con giống | Số lần cho ăn cả vòng | Số lượng thức ăn cả vòng | Thức ăn chính | Giá (tính theo giá trồng được) | Tổng chi phí thức ăn |
|---|---|---|---|---|---|
| Bò sữa | 270 | 1.080 | Cỏ voi | 37,50 | 40.500 |
| Rùa con | 42 | 294 | Rau muống | 30 | 8.820 |
| Heo con | 180 | 360 | Khoai lang | 48 | 17.280 |
| Đà điểu V2 | 180 | 720 | Dưa hấu | 27 | 19.440 |
| Hươu | 360 | 1.800 | Bắp ngô | 15 | 27.000 |
| Gà mái V2 | 90 | 180 | Bắp ngô | 15 | 2.700 |
| Dê con V2 | 180 | 360 | Bí ngô | 3,50 | 1.260 |
| Ngỗng con V2 (mới) | 90 | 180 | Bắp cải | 27 | 4.860 |
| Thỏ con V2 (mới) | 80 | 90 | Cà rốt | 22 | 1.980 |
| Vịt V3 (mới) | 90 | 180 | Bắp ngô | 15 | 2.700 |

## 7. Kết quả

| Con giống | Doanh thu | Tổng chi phí | Tổng lợi nhuận | Tỷ suất lợi nhuận thu hoạch tổng (%) | Tỷ suất lợi nhuận 1 tháng theo Product 1 (%) | Tỷ suất lợi nhuận dự kiến |
|---|---|---|---|---|---|---|
| Bò sữa | 214.650 | 85.897,75 | 128.752,25 | 249,89 | 15 | 1,31 |
| Rùa con | 73.140 | 22.859,33 | 50.280,67 | 319,96 | 17 | 3,43 |
| Heo con | 78.220 | 32.588,25 | 45.631,75 | 240,03 | 13 | 1,02 |
| Đà điểu V2 | 88.410 | 45.338,73 | 43.071,27 | 195,00 | 12 | 1,36 |
| Hươu | 349.970 | 87.495,00 | 262.475,00 | 399,99 | 19 | 0,48 |
| Gà mái V2 | 5.340 | 3.815,96 | 1.524,04 | 139,94 | 11 | 0,80 |
| Dê con V2 | 15.900 | 9.089,63 | 6.810,37 | 174,92 | 11 | 1,00 |
| Ngỗng con V2 (mới) | 9.795 | 6.535,93 | 3.259,07 | 149,86 | 10 | 1,01 |
| Thỏ con V2 (mới) | 3.575 | 2.859,96 | 715,04 | 125,00 | 9 | 0,55 |
| Vịt V3 (mới) | 5.250 | 4.043,94 | 1.206,06 | 129,82 | 9 | 0,60 |

---

## Công thức thật trong file

Bóc trực tiếp từ ô Excel. **Đây mới là định nghĩa của các cột** — nhìn số không thôi rất dễ hiểu sai.

| Cột | Công thức | Đọc là |
|---|---|---|
| `C` Định giá | `=D/E16` | USDT ÷ 0,006667 = **USDT × 150** |
| `E` Quy đổi VND | `=C*E16*E17` | Point → USDT → VND (× 26.700) |
| `O` Chi phí vắc-xin | `=N*30` | **1 mũi vắc-xin = 30 Point** |
| `P` Chi phí thuốc | `=Q*70` | **1 liều thuốc = 70 Point** |
| `W` Tổng lần thu | `=X*30/V` | (số tháng × 30 ngày) ÷ chu kỳ thu |
| `AF` Số lần cho ăn | `=X*30/J` | (số tháng × 30 ngày) ÷ chu kỳ cho ăn |
| `AG` Lượng thức ăn | `=AF × (số/lần)` | số lần cho ăn × khẩu phần mỗi lần |
| `AJ` Chi phí thức ăn | `=AG*AI` | lượng × giá cây trồng |
| `AK` Doanh thu | `=AC+AD` | thu Product 1 + thu Product 2 |
| `AL` **Tổng chi phí** | `=M*(C+O+P+AJ) + (1-M)*(AJ+C+O)` | con giống + vắc-xin + thức ăn + **kỳ vọng** tiền thuốc |
| `AM` Tổng lợi nhuận | `=AK-AL` | doanh thu − tổng chi phí |
| `AN` Tỷ suất thu hoạch tổng | `=AK/AL%` | **doanh thu ÷ tổng chi phí** |

### Ba điều phải nhớ về công thức

**1. `AL` ĐÃ bao gồm tiền mua con giống.** Cột `C` nằm trong cả hai vế. Nên "tổng lợi nhuận"
`AM` là lãi ròng thật, đã trừ vốn.

**2. `AN` KHÔNG phải tỷ suất lợi nhuận, và cũng không phải theo tháng.** Nó là
*doanh thu ÷ tổng chi phí* của **cả vòng nuôi**. Vì `AK = AM + AL` nên:

```
AN = (AM + AL) / AL = AM/AL + 100%
```

100% dôi ra chính là **tiền vốn quay về**, không phải lãi. Bò sữa `AN = 249,89%` nghĩa là
bỏ ra 1 đồng thu về 2,49 đồng → **lãi thật 149,89%**. Đặt tên cột là "tỷ suất lợi nhuận"
gây hiểu nhầm, nhưng phép tính thì không sai.

**3. Chi phí thuốc là KỲ VỌNG, không phải chi phí chắc chắn.** Khai triển `AL`:

```
AL = (con giống + vắc-xin + thức ăn) + tỉ_lệ_phát_bệnh × chi_phí_thuốc
```

Bò sữa: tỉ lệ bệnh 0,4 × 700 = **280 Point** được tính vào chi phí, dù thực tế người chơi
hoặc tốn đủ 700 (nếu bệnh) hoặc tốn 0 (nếu không bệnh).

---

## Những cột KHÔNG có công thức (gõ tay — sẽ không tự cập nhật)

| Cột | Giá trị | Rủi ro |
|---|---|---|
| `AO` Tỷ suất 1 tháng | 15 · 17 · 13 · 12 · 19 · 11 · 11 · 10 · 9 · 9 | **Số cứng.** Đổi giá bất kỳ ô nào, cột này vẫn đứng yên. |
| `AE` Tỷ suất lợi nhuận dự kiến | 1,31425 · 3,432 · 1,01515 · 1,36285 · 0,4835 · 0,7992 · 0,99935 · 1,0089 · 0,55 · 0,5982 | **Y HỆT `VatNuoi2.xlsx`** — chưa cập nhật theo giá mới, coi như số rác. |
| `Z` Giá Product 1 (một số dòng) | Rùa `=38860`, Bò `=Y*305` | Giá nhét thẳng vào công thức thay vì tham chiếu ô. |

Đối chiếu ngược `AO` bằng `(AC ÷ AL) ÷ X` thì ra đúng 15 / 17 / 13 / 12 / 19 / 11 / 11 / 10 / 9 / 9 —
tức công thức ngầm là **(doanh thu Product 1 ÷ tổng chi phí) ÷ số tháng nuôi**, và
**cố tình bỏ qua Product 2 (thịt/vụ cuối)**.

---

## So với `VatNuoi2.xlsx` — khác gì

Cách tính **không đổi một dòng nào**. Chỉ đổi số.

| | VatNuoi2 | VatNuoi3 | Gấp |
|---|---|---|---|
| **Tỷ giá** | 1 USDT = 26 Point | 1 USDT = **150 Point** | ×5,769 |
| Bò sữa (con giống) | 7.800 | 44.997,75 | ×5,769 |
| Hươu (con giống) | 10.400 | 59.997 | ×5,769 |
| Sữa bò | 50 | 305 | ×6,10 |
| Thịt bò | 325 | 1.975 | ×6,08 |
| Nhung hươu | 24.735 | 99.745 | ×4,03 |
| Mai rùa | 11.893 | 38.860 | ×3,27 |
| Trứng gà | 11 | 28 | ×2,55 |
| Lông thỏ | 172 | 345 | ×2,00 |
| Cỏ voi (thức ăn) | 11 | 37,5 | ×3,41 |
| Khoai lang | 17 | 48 | ×2,82 |
| Bắp ngô | 6 | 15 | ×2,50 |
| Cà rốt | 13 | 22 | ×1,69 |
| **Bò sữa ăn** | 2 Cỏ Voi / 4 Khoai Lang | **4 Cỏ Voi / 2 Khoai Lang** | đảo khẩu phần |

Con giống tăng đều ×5,769, nhưng **sản phẩm tăng không đều** (×2,00 đến ×6,10) và giá thức ăn
cũng tăng lệch nhau. Kết quả là hai cột `AN` và `AO` **gần như y nguyên**
(249 / 320 / 240 / 195 / 400 …) — nghĩa là bộ giá này được chỉnh tay để **giữ nguyên độ lời**,
chỉ phóng to con số tuyệt đối.

---

## Ảnh hưởng tới game (game đang chạy bộ VatNuoi2)

Đọc thẳng từ asset: `milk_01 = 50`, `beef_01 = 325`, `deer_meat_01 = 933` — đúng bộ `VatNuoi2`.
Muốn theo `VatNuoi3` thì phải sửa:

1. Giá mua **10 con giống** (`AnimalDefinition.buyPrice`).
2. Giá bán **20 sản phẩm** vật nuôi (`ItemDefinition.sellPrice`).
3. Giá **8 loại cây làm thức ăn** — hiện `sellPrice = 0` trong game, chưa từng có giá.
4. Khẩu phần **bò sữa 2 → 4 Cỏ Voi**.
5. Giá **vắc-xin 30 Point/mũi** và **thuốc 70 Point/liều** — hiện chưa có trong `ItemDatabase`.

⚠️ **Câu chưa có lời đáp:** tỷ giá đổi 26 → 150 là đổi cho **toàn bộ game** hay chỉ riêng vật nuôi?
Nếu chỉ vật nuôi thì cá, đá quý, nông sản, tiền khởi điểm… vẫn ở thang cũ, và mọi so sánh giá
trong game sẽ lệch 5,77 lần. Cần khách xác nhận trước khi áp số.
