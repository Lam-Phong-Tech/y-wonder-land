# RÀ SOÁT KỊCH BẢN ↔ CODE HIỆN TẠI — YWONDERLAND

> Đối chiếu tài liệu khách (`YWONDERLAND_KichBan3D_ChiTiet.md` + `DanhSachCuaHang_Game3D.md`) với mã nguồn/UI đã dựng trong dự án.
> Cập nhật: 2026-06-16 · Nhánh: `feat/inventory-economy`
>
> **Chú giải trạng thái:**
> - ✅ **Xong** — đã chạy được trong Play Mode (dù còn offline/PlayerPrefs).
> - 🟡 **Một phần / Mockup** — có UI hoặc khung code, nhưng thiếu logic, thiếu nội dung, hoặc chỉ dữ liệu giả.
> - ❌ **Chưa làm** — chưa có code/asset.
>
> **Lưu ý nền tảng:** Toàn dự án hiện **OFFLINE** (lưu `PlayerPrefs`), **CHƯA tích hợp UGS** (Cloud Save / Auth / Economy / Friends / Leaderboard server / IAP). Mọi tính năng "online" bên dưới đang chạy bằng dữ liệu giả.

---

## A. 16 MODULE CHỨC NĂNG

| # | Module (kịch bản) | Trạng thái | Đã có gì trong code | Còn thiếu |
|---|---|:---:|---|---|
| 1 | Đăng ký tài khoản | 🟡 | `LoginScreen`, `ForgotPasswordPopup` (UI) | Backend UGS Auth thật, validate server |
| 2 | Đăng nhập + chọn giới tính/tên | ✅ | `LoginScreen`, `CharacterSelect` (chọn Nam/Nữ + đặt tên + ConfirmDialog) | Nối Auth thật |
| 3 | Vào game (cinematic + NPC hướng dẫn) | ✅ | `BoatCutscene`, `TutorialManager`, `GuideNPC`, `TutorialNode` | — |
| 4 | Màn hình game (HUD) | ✅ | `GameHUD` đầy đủ (chat, quest, rank, mail, bạn bè, túi đồ, tiền tệ, cài đặt, event, info, joystick) | — |
| 5 | Hệ thống Map (5 cảnh) | 🟡 | `IslandTravelManager`, `MapPopup`, `ScenePortal`, `MapPortalTrigger` (cơ chế đổi đảo additive) | Chỉ có **Farm + City**; **Mỏ / Hải Phú / Mộc Nhi chưa dựng scene** |
| 6 | Nông trại | ✅ | Cuốc/trồng/tưới/thu hoạch (`FarmTile`, `FarmInteractionController`), chặt cây/đào đá (`HarvestableResource`), xây dựng (`BuildMode`, `BuildGrid`, `Ghost`) | Tinh chỉnh cân bằng |
| 7 | Thành phố (13 cửa hàng) | 🟡 | Khung `ShopPopup`, `WorkshopPopup`, `PiggyBankPopup`, `MerchantNPC`, câu cá | **Chưa đủ 13 NPC shop riêng** theo đặc tả kiến trúc; mới có vài NPC |
| 8 | Khai thác mỏ | 🟡 | Đào đá (Rock) ngay trên farm qua `HarvestableResource` | **Chưa có MineScene riêng**, minigame đào, vé đào, xác suất ra quặng |
| 9 | Tiền tệ (POS + UPOS) | 🟡 | `EconomyManager` có cả POS & UPOS (offline) | IAP nạp tiền thật, validate receipt |
| 10 | Event + điểm danh | 🟡 | `EventPopup`, `RewardPopup` | Logic điểm danh chuỗi 7 ngày, đổi quà event thực |
| 11 | Level | 🟡 | UI `LevelUpOverlay` | **Logic lên cấp / mở khóa chưa có trong Manager** |
| 12 | Kinh nghiệm (EXP) | 🟡 | (gắn với Level) | **Chưa có hệ thống cộng EXP / bảng EXP theo level** |
| 13 | Bạn bè | 🟡 | `FriendsPopup` (UI, 3 tab) | Dữ liệu giả; cần UGS Friends thật |
| 14 | Pet (thú cưng) | 🟡 | `PetInteraction` | Follow theo NavMesh, animation Pet đầy đủ, mua từ Pet Shop |
| 15 | NPC AI Chat | 🟡 | `ChatPanel` (khung chat) | **Chưa có pool câu trả lời AI theo từ khóa** |
| 16 | Chính sách trò chơi (real-time) | 🟡 | Trồng/nuôi theo thời gian thực; vật nuôi có trạng thái đói/bệnh (`FarmAnimal`) | Push notification, cơ chế chết/héo hoàn chỉnh, daily quest fail |

---

## B. 24 BƯỚC KỊCH BẢN CHI TIẾT

| Bước | Nội dung | Trạng thái | Ghi chú |
|---|---|:---:|---|
| 1 | Đăng ký tài khoản | 🟡 | UI có, chưa nối server |
| 2 | Tạo nhân vật (giới tính + tên) | ✅ | `CharacterSelect` |
| 3 | Cinematic chào mừng + NPC | ✅ | `BoatCutscene` + `GuideNPC` đợi player |
| 4 | Tutorial cuốc/trồng/tưới/thu hoạch | ✅ | `TutorialManager` |
| 5 | Trồng trọt (8 loại hạt) | ✅ | Đủ 8 hạt trong `Resources/Items`; tưới/bón/thu hoạch real-time |
| 6 | Chăn nuôi (xây chuồng + nuôi) | 🟡 | `AnimalPen`, `FarmAnimal`, `AnimalManager`, vaccine có asset; cần hoàn thiện cho ăn/tiêm/thu sản phẩm |
| 7 | Khai thác (chặt cây / đào đá) | ✅ | `HarvestableResource` (đã tắt rung, cây đổ + ẩn lá) |
| 8 | Xây dựng nông trại | 🟡 | Build Mode có khung; chờ model thật, chưa trừ tài nguyên hoàn chỉnh |
| 9 | Câu cá | ✅ | `FishingSpot`, `FishingOverlay`, `FishingLineController` (dây + phao) |
| 10 | Mua sắm (12 shop) | 🟡 | `ShopPopup` khung; thiếu NPC + nội dung từng shop |
| 11 | Gửi Heo Đất (lãi suất) | 🟡 | `PiggyBankPopup` UI (12/30/180 ngày) |
| 12 | Workshop nâng cấp dụng cụ | 🟡 | `WorkshopPopup` UI; logic nâng cấp/trừ vật liệu cần hoàn thiện |
| 13 | Đào quặng tại Mỏ | 🟡 | Chưa có MineScene + minigame |
| 14 | Pet follow | 🟡 | `PetInteraction` |
| 15 | Bạn bè (kết bạn, thăm farm) | 🟡 | `FriendsPopup` mockup |
| 16 | Chat (Global + AI NPC) | 🟡 | Khung chat có; AI + global server chưa |
| 17 | Nhiệm vụ & Xếp hạng | 🟡 | `QuestPopup`, `LeaderboardPopup` UI mockup |
| 18 | Event & điểm danh | 🟡 | `EventPopup` UI; logic chuỗi chưa đủ |
| 19 | Mở khóa Đảo Hải Phú/Mộc Nhi | 🟡 | Cơ chế đổi đảo có; **chưa có scene 2 đảo** |
| 20 | Level & EXP | 🟡 | Chỉ có UI overlay |
| 21 | Tiền tệ POS & UPOS | 🟡 | `EconomyManager` offline; IAP chưa |
| 22 | Túi đồ (Inventory) | ✅ | `InventoryPopup` + `InventoryManager` (7 tab) |
| 23 | Cài đặt | ✅ | `SettingsPopup` (Audio/Graphics/Language) |
| 24 | Cosmetic & cá nhân hóa | ❌ | Chưa có hệ thống thay tóc/áo/quần lên model |

---

## C. 13 CỬA HÀNG (theo DanhSachCuaHang)

| Cửa hàng | NPC/Model | Trạng thái |
|---|---|:---:|
| Item Shop (Nông trại/Đảo) | — | 🟡 khung ShopPopup |
| Farm Shop (Hạt giống & Vật nuôi) | — | 🟡 |
| Fish Shop (Bán cá & Mồi) | `FishNPC` (vừa import) | 🟡 |
| Siêu thị Cá (Thành phố) | — | ❌ |
| Workshop (Nâng cấp dụng cụ) | `BlacksmithNPC` (vừa import) | 🟡 WorkshopPopup |
| Verdant Farm & Sukimoko | — | ❌ |
| Mini Garden & Hai Lúa | `HaiLuaNPC` | 🟡 |
| Spa Cecilia / KNX / Nguyễn Phương | — | ❌ |
| Hai Lúa (Nhà Nông Vàng) & Cơm Gà | `HaiLuaNPC` | 🟡 |
| Maid Service | `MaidNPC` | 🟡 |
| Pet Shop & Arcade | — | ❌ |
| Store (Thời trang) & Beauty | — | ❌ (gắn Cosmetic) |
| Heo Đất & Gift Post | `BankNPC1` (vừa import) | 🟡 PiggyBankPopup |

> Đã import sẵn NPC: `BankNPC1`, `BlacksmithNPC`, `FishNPC`, `SeedNPC` → sẵn sàng gắn vào khung `ShopkeeperNPC`.

---

## D. 32 ANIMATION TRIGGER (sheet 06)

| Nhóm | Trạng thái | Ghi chú |
|---|:---:|---|
| Player di chuyển (Idle/Walk/Run/Jump/Swim) | ✅ | `PlayerController` code-driven |
| Player lao động (Hoe/Plant/Water/Chop/Mine/Fish/Feed/Pet) | ✅ | Qua `PlayActionAnimation` + `EquipmentManager` (cầm đúng dụng cụ) |
| Water (tưới) | ✅ | Vừa nối clip riêng + cầm xô |
| Feed (cho ăn) | 🟡 | Có cầm nắm cám; **chờ VFX rắc thức ăn** |
| Vaccinate / Fertilize / Sit / Wave / LevelUp | 🟡 | Chưa có clip/đấu nối riêng |
| NPC (Idle/Wave/Point/Walk) | 🟡 | GuideNPC có; shop NPC chưa đủ |
| Pet (Idle/Follow/Happy) | 🟡 | Một phần |
| Vật nuôi (Eat/Sick/Sleep/Produce) | 🟡 | `FarmAnimal` có trạng thái; animation chưa đủ |
| Cá (Swim/Bite/Caught) | 🟡 | Câu cá có hiệu ứng cơ bản |

---

## E. KHUYẾN NGHỊ ƯU TIÊN (đề xuất cho khách)

**Nhóm "đã chạy được, demo tốt":** Onboarding (đăng nhập→tạo nhân vật→tutorial), Nông trại (trồng/tưới/thu hoạch/chặt/đào), Câu cá, Túi đồ, HUD, Đổi đảo Farm↔City.

**Nhóm cần ưu tiên làm tiếp (gần xong, đem lại giá trị nhanh):**
1. **Khung `ShopkeeperNPC`** — đã có NPC + nhiều popup shop, chỉ cần đấu nối → mở được phần lớn Module 7 & 10.
2. **Hệ thống Level/EXP** — hiện chỉ có UI, thiếu logic lõi; nhiều module phụ thuộc (mở khóa map/chức năng).
3. **Hoàn thiện chăn nuôi** (cho ăn + VFX, tiêm vaccine, thu sản phẩm) — Module 6.

**Nhóm việc lớn cần lên kế hoạch riêng:**
- Dựng 3 scene còn thiếu: **Mỏ, Hải Phú, Mộc Nhi**.
- **Tích hợp UGS** (Auth, Cloud Save, Economy, Friends, Leaderboard) — chuyển từ offline sang online.
- **~46 model 3D** theo sheet 11 (vật nuôi, cây lâu năm, sản phẩm).
- Hệ thống **Cosmetic** (Module 24) + **AI Chat pool** (Module 15).

> ⚠️ Các con số trạng thái dựa trên rà soát code/UI. Một vài module mức 🟡 cần mở Editor kiểm tra mức độ đấu nối thực tế (vd Pet follow, logic Heo Đất, Workshop trừ vật liệu) trước khi chốt với khách.
