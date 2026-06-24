# Bản nháp Spec MUA / BÁN cho 13 Cửa hàng (NPC)

> Nguồn: `DanhSachCuaHang_Game3D.md` (mô tả chức năng) + danh mục item THẬT trong `ItemDataGenerator.cs`.
> Mục đích: biến file mô tả thành **bảng dữ liệu mua/bán** đổ vào hệ `ShopData` (data-driven).
>
> **Ghi chú giá:**
> - Giá có ID `*_01` = **lấy từ catalog game đang chạy** (ItemDataGenerator) — nhất quán, nhưng vẫn là DEMO chờ khách chốt.
> - **(tạm)** = bé tự đề xuất, CHƯA có trong file/khách → **cần khách xác nhận**.
> - **UPOS** = tiền nạp (premium), khác POS (tiền chơi). Các dịch vụ VIP dùng UPOS theo file.

---

## A. PHÂN LOẠI 13 CỬA HÀNG

| # | Cửa hàng | Loại hệ thống | Trạng thái code |
|---|---|---|---|
| 1 | Item Shop (Nông trại/Đảo) | 🛒 Shop MUA | ShopData (cần làm) |
| 2 | Farm Shop (Nông trại) | 🛒 Shop MUA | ShopData (cần làm) |
| 3 | Fish Shop (Đảo) | 🛒 Mua mồi + Bán cá | ShopData (cần làm) |
| 4 | Siêu thị Cá (Thành phố) | 🛒 Mua mồi + Bán cá | ShopData (cần làm) |
| 5 | Workshop | 🔧 Nâng cấp dụng cụ | ✅ WorkshopPopupController |
| 6 | Verdant Farm + Sukimoko | 🛒 Bán nông sản + Mua tiêu dùng | ShopData (cần làm) |
| 7 | Mini Garden + Hai Lúa | 🛒 Bán nông sản/sản phẩm + Mua vật tư | ShopData (cần làm) |
| 8 | KNX / Spa Cecilia / Nguyễn Phương | 💎 Thẻ VIP + 👗 dịch vụ thẩm mỹ/nội thất | Logic riêng (cần làm) |
| 9 | Hai Lúa (Nhà Nông Vàng) + Cơm Gà | 🛒 Mua vật tư + thực phẩm | ShopData (cần làm) |
| 10 | Maid Service | 💎 Thuê Maid VIP | Logic riêng (cần làm) |
| 11 | Pet Shop + Game Center | 💎 Mua Pet + 🎰 minigame | Logic riêng (cần làm) |
| 12 | Store + Beauty | 👗 Avatar/thời trang | Logic riêng (cần làm) |
| 13 | Heo Đất + Gift Post | 🐷 Ngân hàng + 🎁 quà tặng | Heo Đất ✅ / Gift Post cần làm |

➡️ **Chỉ nhóm 🛒 mới cần "bảng mua/bán" bên dưới.** Nhóm 🔧/💎/👗/🎰/🐷/🎁 là tính năng riêng (mô tả ở mục C).

---

## B. BẢNG MUA / BÁN (nhóm 🛒) — đổ vào ShopData

### 1. Item Shop — *BuyOnly* (vật tư trồng trọt/chăn nuôi)
| Item ID | Tên | Giá MUA (POS) |
|---|---|---|
| fertilizer_01 | Phân bón | 50 |
| vaccine_01 | Vắc-xin | 80 |
| medicine_01 | Thuốc trị | 100 |
| bait_01 | Mồi câu | 20 |

### 2. Farm Shop — *BuyOnly* (hạt giống + con giống)
| Item ID | Tên | Giá MUA (POS) |
|---|---|---|
| carrot_seed_01 | Hạt cà rốt | 10 |
| cabbage_seed_01 | Hạt cải | 15 |
| watermelon_seed_01 | Hạt dưa hấu | 30 |
| corn_seed_01 | Hạt bắp | 20 |
| pumpkin_seed_01 | Hạt bí ngô | 25 |
| grass_seed_01 | Cỏ voi | 5 |
| morning_glory_seed_01 | Hạt rau muống | 8 |
| sweet_potato_seed_01 | Dây khoai lang | 12 |
| chicken_01 | Gà | 500 |
| rabbit_01 | Thỏ | 400 |
| ostrich_01 | Đà điểu | 800 |
| goat_01 | Dê | 900 |
| cow_01 | Bò | 1500 |
| deer_01 | Hươu | 1800 |
| pig_01 | Heo | 1000 |
| duck_01 | Vịt | **8 (tạm)** — có data nuôi, chưa có giá shop |
| goose_01 | Ngỗng | **10 (tạm)** |

### 3 & 4. Fish Shop / Siêu thị Cá — *Both* (mua mồi · bán cá)
**MUA:**
| Item ID | Tên | Giá MUA (POS) |
|---|---|---|
| bait_01 | Mồi câu | 20 |

**BÁN (thu mua từ người chơi):**
| Item ID | Tên | Giá BÁN (POS) |
|---|---|---|
| fish_01 | Cá chép | 50 |
| fish_02 | Cá hiếm | 200 |
| gift_box_01 | Hộp quà đại dương | 500 |

> File ghi "cá 10–200 POS tùy độ hiếm" → khi có nhiều loài cá, bổ sung fish_03, fish_04... + giá theo độ hiếm.

### 6. Verdant Farm — *Both* (bán nông sản · mua tiêu dùng)
**BÁN (thu mua nông sản quy mô lớn):** tất cả nông sản (carrot_01, cabbage_01, watermelon_01, corn_01, pumpkin_01, morning_glory_01, sweet_potato_01) — xem giá ở bảng mục 7.
**MUA (vật phẩm tiêu dùng cao cấp):**
| Item ID | Tên | Giá MUA (POS) |
|---|---|---|
| bread_01 | Bánh mì | 20 |
| apple_01 | Táo đỏ | 10 |

### 7. Mini Garden — *SellOnly* (thu mua nông sản + sản phẩm chăn nuôi)
| Item ID | Tên | Giá BÁN (POS) | Nguồn |
|---|---|---|---|
| carrot_01 | Cà rốt | 15 | file + catalog |
| cabbage_01 | Rau cải | 20 | catalog |
| watermelon_01 | Dưa hấu | 45 | **file** (catalog ghi 50 → cần thống nhất) |
| corn_01 | Bắp ngô | 30 | catalog |
| pumpkin_01 | Bí ngô | 35 | catalog |
| morning_glory_01 | Rau muống | 10 | catalog |
| sweet_potato_01 | Khoai lang | 18 | catalog |
| egg_01 | Trứng gà | 25 | catalog (file: 5–30) |
| milk_01 | Sữa bò | 40 | catalog (file: 5–30 → cần thống nhất) |
| pork_01 | Thịt heo | 35 | catalog |

> ⚠️ **Mâu thuẫn cần khách chốt:** dưa hấu (file 45 vs code 50); sữa bò (file nói trứng/sữa 5–30 nhưng code để 40).

### 9. Hai Lúa (Nhà Nông Vàng) + Cơm Gà O — *BuyOnly*
| Item ID | Tên | Giá MUA (POS) | Quầy |
|---|---|---|---|
| fertilizer_01 | Phân bón hữu cơ | 50 | Hai Lúa |
| vaccine_01 | Vắc-xin gia súc | 80 | Hai Lúa |
| bread_01 | Bánh mì (tăng thể lực) | 20 | Cơm Gà |
| apple_01 | Táo (tăng thể lực) | 10 | Cơm Gà |

> "Thực phẩm tăng thể lực" của Cơm Gà: hiện chưa có cơ chế "thể lực/stamina" → **cần khách xác nhận** có hệ stamina không, hay chỉ là món ăn hồi máu.

---

## C. NHÓM TÍNH NĂNG RIÊNG (không dùng ShopData)

| Cửa hàng | Cơ chế cần | Số đã biết | Còn thiếu (xin khách) |
|---|---|---|---|
| **Workshop** | Nâng cấp dụng cụ (ĐÃ CÓ) | — | Bảng nguyên liệu/POS từng cấp |
| **Heo Đất** | Gửi tiết kiệm (ĐÃ CÓ) | 12 ngày +2%, 30 ngày +6%, 180 ngày +45% | — |
| **KNX (VIP)** | Mua thẻ VIP/tháng → mở đặc quyền Maid + kết bạn | 200 UPOS/tháng | Danh sách đặc quyền VIP đầy đủ |
| **Maid Service** | Thuê Robot Maid tự tưới/thu hoạch | 300 UPOS (vĩnh viễn) | Phạm vi tự động (bán kính, tốc độ) |
| **Pet Shop** | Mua Pet companion (trang trí) | 200–500 UPOS | Danh sách loài Pet + giá từng con |
| **Game Center** | Vòng quay minigame trúng POS/vật phẩm | — | Bảng phần thưởng + tỉ lệ + giá 1 lượt quay |
| **Store + Beauty** | Avatar customization (áo/quần/váy/tóc/phụ kiện) | — | Danh sách trang phục + giá (POS/UPOS) |
| **Spa Cecilia** | Dịch vụ thẩm mỹ | — | Làm gì cụ thể? (đổi diện mạo?) — xin khách |
| **Nguyễn Phương** | Nội thất trang trí nhà | — | Danh sách đồ nội thất + giá |
| **Gift Post** | Mua + gửi 25 loại quà cho bạn | "25 loại quà" | Danh sách 25 quà + giá từng món |

---

## D. KHUYẾN NGHỊ TRIỂN KHAI

1. **Trước mắt (chạy demo được ngay):** tạo `ShopData` (ScriptableObject) cho **7 shop nhóm 🛒**, đổ số ở mục B (số tạm) → game có 7 NPC bán/mua khác nhau ngay.
2. **Gửi khách chốt:** các ô **(tạm)** + mâu thuẫn ở mục B + toàn bộ "Còn thiếu" ở mục C.
3. **Sau khi có số thật:** chỉ sửa giá trong asset ScriptableObject — KHÔNG đụng code.
4. Nhóm 💎/👗/🎰/🎁 là **feature riêng**, lên task riêng — đừng gộp vào hệ shop.
