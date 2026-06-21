# Thiết kế: Hệ NPC Shop (Thành phố + Đảo) — mở shop khi CHẠM NHÀ

> Trạng thái: **BẢN THIẾT KẾ — chờ duyệt trước khi code.**
> Nguồn: `YWONDERLAND_KichBan3D_ChiTiet.md`, `DanhSachCuaHang_Game3D.md`, `VatNuoi.md`.
> Quyết định UX (đã chốt với sếp): **tự mở khi chạm nhà** · **giữ mở, đóng bằng nút X** · áp dụng cho **cả thành phố lẫn đảo riêng**.

---

## 1. MỤC TIÊU & THAY ĐỔI CƠ CHẾ

| | Cũ (kịch bản gốc) | Mới (sếp yêu cầu) |
|---|---|---|
| Mở shop | Click vào NPC | **Lại gần → nhân vật chạm vùng NHÀ NPC → popup tự hiện** |
| Đóng | Nút X | Nút X (đi ra KHÔNG đóng) |

Model NPC đã có đủ. Việc cần làm là **logic mở/đóng theo va chạm** + **mỗi shop một catalog riêng** (data-driven).

---

## 2. LUỒNG HOẠT ĐỘNG (Trigger nhà)

```
[Player đi vào vùng nhà]
        │  OnTriggerEnter(Player)
        ▼
   popup đang mở?  ── CÓ ──► (bỏ qua, không mở lại)
        │ KHÔNG
        ▼
   Mở popup shop của NPC (Show(catalog, mode))
        │
[Player đi ra]  OnTriggerExit(Player)
        ▼
   KHÔNG đóng popup — chỉ gỡ cờ "đang trong vùng"
        │
[Player bấm nút X]  ──► đóng popup
```

**Chống mở lặp (quan trọng):**
- Chỉ mở khi `popup KHÔNG đang hiển thị` (tránh bật lại liên tục mỗi frame / mỗi lần re-enter).
- Dùng `IsVisible()` của popup làm điều kiện, KHÔNG dựa vào cờ dính (sticky) — vì teleport đổi map tắt CharacterController khiến `OnTriggerExit` có thể không bắn (bài học từ `MapPortalTrigger`).
- Nếu player đóng X khi vẫn đứng trong vùng → muốn mở lại phải bước RA rồi VÀO lại.

---

## 3. KIẾN TRÚC & FILE

### Tái sử dụng (KHÔNG làm lại)
- `ShopPopupController` — **đã có** `Show(ShopData)`, tách `BuyOnly/SellOnly/Both`, tab lọc, mua/bán. Giữ nguyên.
- `MerchantNPC` — **đã có** enum `ServiceType {ShopBuy, ShopSell, Workshop, PiggyBank}` + mở đúng popup. Giữ làm "danh tính shop".
- `MapPortalTrigger` — **mẫu trigger vùng** có sẵn (đã fix lỗi cờ teleport). Học theo.

### Tạo mới / sửa
| File | Loại | Việc |
|---|---|---|
| `ShopDefinition.cs` | **MỚI** (ScriptableObject) | Khuôn dữ liệu 1 shop: `shopName`, `serviceType`, `buyItems[]`, `sellItems[]`, `hasSellTab`. Mỗi shop = 1 asset. |
| `ShopZoneTrigger.cs` | **MỚI** (MonoBehaviour) | Gắn lên nhà NPC (cần BoxCollider isTrigger). `OnTriggerEnter` → mở shop theo `ShopDefinition`. Học `MapPortalTrigger`. |
| `ShopPopupController.cs` | **SỬA** | Thêm overload `Show(ShopData data, ShopAccessMode mode)` (truyền catalog của NPC, thay mock hardcode). Thêm `IsVisible()` nếu chưa có. |
| `MerchantNPC.cs` | **SỬA nhẹ** | Thêm field `ShopDefinition shopData` (để click NPC vẫn mở đúng catalog — phương án phụ). |
| `ShopDataGenerator.cs` | **MỚI** (Editor, tùy chọn) | Menu `Generate Shop Data` tự sinh 7 asset shop điền sẵn giá (giống `Generate Animal Data`). |

### Quan hệ
```
NHÀ NPC ── BoxCollider(isTrigger) ── ShopZoneTrigger ──► đọc ShopDefinition.asset
                                                              │
                                          ServiceType quyết định mở popup nào:
                                          ShopBuy/Sell → ShopPopup.Show(catalog, mode)
                                          Workshop     → WorkshopPopup.Show()
                                          PiggyBank    → PiggyBankPopup.Show()
```

---

## 4. PHÂN LOẠI 13 CỬA HÀNG → POPUP NÀO

| # | Cửa hàng | ServiceType / Hệ | Popup |
|---|---|---|---|
| 1 | Item Shop (vật tư) | ShopBuy | ShopPopup (BuyOnly) |
| 2 | Farm Shop / YWonderLand (hạt+con giống) | ShopBuy | ShopPopup (BuyOnly) |
| 3 | Fish Shop / Bán cá | ShopBoth | ShopPopup (Both: mua mồi, bán cá) |
| 4 | Mini Garden (thu mua nông sản) | ShopSell | ShopPopup (SellOnly) |
| 5 | Hai Lúa (phân/vắc-xin/thuốc) | ShopBuy | ShopPopup (BuyOnly) |
| 6 | Verdant (bán nông sản + mua tiêu dùng) | ShopBoth | ShopPopup (Both) |
| 7 | Workshop | Workshop ✅ | WorkshopPopup |
| 8 | Heo Đất | PiggyBank ✅ | PiggyBankPopup |
| 9 | KNX (thẻ VIP) | **MỚI** | VipPopup (cần làm) |
| 10 | Maid Service | **MỚI** | MaidPopup (cần làm) |
| 11 | Pet Shop | **MỚI** | PetShopPopup (cần làm) |
| 12 | Game Center | **MỚI** | MiniGamePopup (cần làm) |
| 13 | Store/Beauty (thời trang) + Gift Post | **MỚI** | CosmeticPopup + GiftPopup (cần làm) |

➡️ **Lượt này chỉ làm nhóm ShopBuy/Sell/Both + Workshop + PiggyBank** (đã có popup). Nhóm "MỚI" (VIP/Maid/Pet/Game/Cosmetic/Gift) lên task riêng sau.

---

## 5. SPEC MUA/BÁN (giá đã có từ VatNuoi.md + 08_KinhTeGame)

### Hạt giống (Farm Shop) — buy/sell POS
| ID | Tên | Mua | Bán |
|---|---|---|---|
| carrot_seed_01 | Cà rốt | 10 | 5 |
| cabbage_seed_01 | Cải | 15 | 7 |
| watermelon_seed_01 | Dưa hấu | 30 | 15 |
| corn_seed_01 | Bắp | 20 | 10 |
| pumpkin_seed_01 | Bí ngô | 25 | 12 |
| grass_seed_01 | Cỏ voi | 5 | 2 |
| morning_glory_seed_01 | Rau muống | 8 | 4 |
| sweet_potato_seed_01 | Khoai lang | 12 | 6 |

### Con giống (Farm Shop) — giá MUA (POS) theo VatNuoi.md
| ID | Tên | Giá | Ô chuồng |
|---|---|---|---|
| chicken_01 | Gà mái | 6 | 1 |
| duck_01 | Vịt | 8 | 1 |
| goose_01 | Ngỗng | 10 | 1 |
| rabbit_01 | Thỏ | 5 | 1 |
| goat_01 | Dê | 50 | 9 |
| turtle_01 | Rùa | 90 | 1 |
| pig_01 | Heo | 100 | 9 |
| ostrich_01 | Đà điểu | 170 | 1 |
| cow_01 | Bò | 300 | 9 |
| deer_01 | Hươu | 400 | 9 |

> ⚠️ **MÂU THUẪN cần khách chốt:** giá con giống ở `VatNuoi.md` (5–400) vs `08_KinhTeGame` ("vật nuôi 500–5000 POS") vs `ItemDatabase` cũ (gà 500, bò 1500). **3 nguồn lệch nhau** → phải hỏi khách: con giống dùng thang giá nào? (Bé đề xuất theo VatNuoi.md vì có bảng tính lợi nhuận chi tiết nhất.)

### Vật tư (Item Shop / Hai Lúa) — buy POS
| ID | Tên | Mua |
|---|---|---|
| fertilizer_01 | Phân bón | 50 |
| vaccine_01 | Vắc-xin | 80 |
| medicine_01 | Thuốc trị | 100 |
| bait_01 | Mồi câu | 20 |
| mine_ticket_01 | Vé đào mỏ | 100 |

### Nông sản + sản phẩm chăn nuôi (Mini Garden / Verdant) — bán POS
| ID | Tên | Bán |
|---|---|---|
| carrot_01 | Cà rốt | 15 |
| cabbage_01 | Rau cải | 20 |
| watermelon_01 | Dưa hấu | 45 |
| corn_01 | Bắp ngô | 30 |
| pumpkin_01 | Bí ngô | 35 |
| morning_glory_01 | Rau muống | 10 |
| sweet_potato_01 | Khoai lang | 18 |
| egg_01 | Trứng gà | 25 |
| milk_01 | Sữa bò | 40 |
| pork_01 | Thịt heo | 35 |

### Cá (Fish Shop / Bán cá) — bán POS · mua mồi
| ID | Tên | Bán |
|---|---|---|
| fish_01 | Cá chép | 50 |
| fish_02 | Cá hiếm | 200 |
| (mua) bait_01 | Mồi câu | 20 (giá mua) |

---

## 6. ÁP DỤNG CHO ĐẢO RIÊNG (Hải Phú / Mộc Nhi)

Vì cơ chế là **trigger gắn lên nhà**, đảo riêng làm y hệt:
- Đặt nhà NPC trên đảo → gắn `ShopZoneTrigger` → kéo `ShopDefinition` của đảo vào.
- Đảo có hàng exclusive (cá hiếm, vật nuôi/cây theo mùa) → chỉ cần tạo asset shop RIÊNG cho đảo, KHÔNG đụng code.
- Đảo khóa theo Lv40/Lv60 + VIP/Vé là việc của hệ Map/Travel (đã có `IslandTravelManager`), không liên quan trigger shop.

---

## 7. SETUP EDITOR (sau khi code xong)
1. Tạo các `ShopDefinition.asset` (hoặc bấm generator) trong `Assets/_Project/Data/Shops/`.
2. Mỗi nhà NPC: thêm **BoxCollider** (Is Trigger ✔) bao quanh khu cửa, gắn `ShopZoneTrigger`, kéo đúng asset shop vào.
3. Player phải có tag `Player` + Rigidbody/CharacterController để trigger bắn (kiểm tra như `MapPortalTrigger`).

---

## 8. ĐIỂM CHỜ KHÁCH (chưa đủ data để làm)
- **Giá con giống** lệch 3 nguồn (mục 5) → chốt thang giá.
- **25 loại quà** Gift Post + giá.
- **Danh sách Pet** + giá từng con (mới biết khoảng 200–500 UPOS).
- **Trang phục/tóc Store** + giá; dịch vụ Spa làm gì.
- **Game Center**: bảng phần thưởng + tỉ lệ + giá 1 lượt quay.
- **Đặc quyền VIP (KNX)** đầy đủ.

---

## 9. CHECKLIST TRIỂN KHAI (khi được duyệt)
- [ ] `ShopDefinition.cs` (ScriptableObject) + `[CreateAssetMenu]`.
- [ ] `ShopPopupController.Show(ShopData, ShopAccessMode)` + `IsVisible()`.
- [ ] `ShopZoneTrigger.cs` (auto-open, giữ mở, chống mở lặp theo IsVisible).
- [ ] `MerchantNPC` thêm field `ShopDefinition` (giữ click làm phương án phụ).
- [ ] (Tùy chọn) `ShopDataGenerator` — menu sinh 7 asset shop điền sẵn giá mục 5.
- [ ] Test: chạm nhà → mở; đi ra → vẫn mở; X → đóng; vào lại → mở lại.
