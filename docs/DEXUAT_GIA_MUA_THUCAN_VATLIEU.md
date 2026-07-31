# ĐỀ XUẤT — Cho phép MUA thức ăn chăn nuôi & vật liệu ở shop

> Soạn 29/07/2026. **Trạng thái: CHỜ KHÁCH CHỐT SỐ — chưa đụng vào data.**
> Vị trí bán đã do chủ dự án quyết (mục 4). Chỉ còn thiếu **bội số giá**.

---

## 1. Vấn đề

Người chơi muốn nuôi thú thì **bắt buộc phải trồng** thức ăn — không có đường mua tắt.
Tương tự, gỗ/đá **chỉ vào bằng chặt cây/đào đá**; hết gỗ giữa Build Mode là kẹt, không mua bù được.

Đề nghị của chủ dự án: *"thay vì mất công trồng thì người chơi có thể mua ngay ở shop để cho động vật ăn."*

---

## 2. Hiện trạng đã rà (29/07) — VERIFIED

Rà toàn bộ `Assets/Resources/Items/*.asset` + 8 asset trong `Assets/_Project/Data/Shops/`.

### 2.1 Nông sản ngắn ngày — KHÔNG mua được

Cả 8 món đều `buyPrice = 0` **và** không nằm trong `buyItemIds` của bất kỳ shop nào:

`carrot_01` · `cabbage_01` · `watermelon_01` · `corn_01` · `pumpkin_01` · `morning_glory_01` · `sweet_potato_01` · `grass_01`

Đây **đúng bằng** danh sách thức ăn của cả 10 con vật (đối chiếu `AnimalDefinition`).

### 2.2 Vật liệu — KHÔNG mua được

| Món | id | Giá BÁN hiện có | Giá MUA |
|---|---|---|---|
| Gỗ | `wood_01` | 8 | **0 — không bán ở shop nào** |
| Đá | `stone_01` | 12 | **0** |
| Gạch | `brick_01` | 10 | **0** |
| Sắt | `iron_01` | 50 | **0** |
| Quặng | `ore_01` | 30 | **0** |
| Nước tưới | `watering_water_01` | 0 (`canSell=0`) | **0** |

### 2.3 Hiện MUA được những gì

19 hạt giống · 10 con giống · 5 dụng cụ · phân bón/vắc-xin/thuốc/mồi câu/vé đào/vé vòng quay · bánh mì + táo (Verdant).

---

## 3. Hai điểm lệch phát hiện khi rà (không chặn, nhưng nên xử)

1. **"Cám" không tồn tại.** `foodAltName` của Gà/Hươu/Vịt/Đà điểu ghi "Cám" nhưng `foodAltAmount = 0`
   và **không có item nào tên Cám** trong `ItemDatabase`. Là chữ hiển thị suông, không cho ăn được.
   → Hỏi khách: bỏ hẳn chữ "Cám", hay tạo item Cám thật (mua ở shop) làm thức ăn thay thế?
2. **Cấu hình chết ở Mini Garden & Verdant.** Hai shop này liệt 8 nông sản trên trong `sellItemIds`,
   nhưng code bỏ qua vì `canSell = 0` (`ShopPopupController` dòng 577). Không gây lỗi, đúng luật khách
   chốt 22/06 *"nông sản ngắn ngày = thức ăn, KHÔNG bán"*, nhưng cấu hình gây hiểu nhầm khi đọc lại.

⚠️ **Luật 22/06 chỉ cấm BÁN, không nói gì về MUA.** Nên đề xuất này là bổ sung mới, không đảo quyết định cũ.

---

## 4. Vị trí bán — CHỦ DỰ ÁN ĐÃ CHỐT (29/07)

| Nhóm hàng | Shop | Asset |
|---|---|---|
| 8 nông sản làm thức ăn | Cửa hàng Hạt giống & Vật nuôi | `Shop_FarmShop.asset` |
| Gỗ · Đá (· Gạch) | Cửa hàng Vật phẩm | `Shop_ItemShop.asset` |

Lý do: thức ăn nằm cùng chỗ mua hạt và con giống → người chơi tìm một chỗ là đủ.

---

## 5. Bảng giá đề xuất — **CẦN KHÁCH CHỐT BỘI SỐ**

Giá vốn thật khi trồng = **giá hạt ÷ sản lượng mỗi vụ**. Mọi cây ngắn ngày đều mọc **1 ngày thật**
(`growthTimeSec = 86400`), thu **1 vụ** (`maxHarvests = 1`).

| Món | Hạt | Giá hạt | Thu/vụ | Vốn/đơn vị | Mua ×2 | Mua ×3 |
|---|---|---|---|---|---|---|
| Bí ngô | `pumpkin_seed_01` | 7 | 11 | 0.6 | **2** | **2** |
| Bắp ngô | `corn_seed_01` | 8 | 3 | 2.7 | **6** | **8** |
| Cà rốt | `carrot_seed_01` | 3 | 1 | 3 | **6** | **9** |
| Bắp cải | `cabbage_seed_01` | 3 | 1 | 3 | **6** | **9** |
| Dưa hấu | `watermelon_seed_01` | 3 | 1 | 3 | **6** | **9** |
| Rau muống | `morning_glory_seed_01` | 4 | 1 | 4 | **8** | **12** |
| Cỏ Voi | `grass_seed_01` | 12 | 2 | 6 | **12** | **18** |
| Khoai lang | `sweet_potato_seed_01` | 7 | 1 | 7 | **14** | **21** |
| Gỗ | — | — | — | bán 8 | **16** | **24** |
| Đá | — | — | — | bán 12 | **24** | **36** |
| Gạch | — | — | — | bán 10 | **20** | **30** |

### Nguyên tắc phải giữ

**Bội số bắt buộc > 1.** Nếu mua rẻ bằng hoặc rẻ hơn trồng thì trồng trọt mất sạch ý nghĩa —
người chơi vừa tiết kiệm tiền vừa khỏi chờ 1 ngày.

- **×2** — trồng vẫn lời rõ, người chơi bận vẫn mua nổi. *(bên em nghiêng về mức này)*
- **×3** — mua chỉ để chữa cháy, gần như bắt buộc phải trồng.

### Câu hỏi phụ cho khách

1. **Nước tưới** (`watering_water_01`) hiện lấy **miễn phí** ở giếng/nguồn nước.
   Có cần bán ở shop không? Nếu có thì giá bao nhiêu (đề xuất 1 Point/đơn vị)?
2. **Sắt & Quặng** chưa dùng vào việc gì trong game. Tạm **chưa** đưa vào shop, đúng không ạ?
3. Có giới hạn **số lượng mua mỗi ngày** cho thức ăn không, hay mua thoải mái?

---

## 6. Khi khách chốt xong thì làm gì (ước lượng nhỏ)

1. Sửa `buyPrice` của 8 nông sản + gỗ/đá/gạch trong generator `ItemDataGenerator.cs`, chạy lại
   `Generate Mock Items`.
2. Thêm id vào `buyItemIds` của `Shop_FarmShop` và `Shop_ItemShop` (qua `ShopDataGenerator` hoặc sửa tay).
3. Kiểm chạy: mở 2 shop, mua thử, xác nhận trừ tiền + vào kho + cho thú ăn được.

Không cần đụng server: giá nằm trong `ItemDatabase` phía client, giao dịch đi qua luồng mua bán sẵn có.
