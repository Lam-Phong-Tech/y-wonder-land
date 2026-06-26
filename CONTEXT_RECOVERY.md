# 🌿 Y WONDER GREEN FARM - MEMENTO PROTOCOL (BẢN GHI NHỚ TIẾN ĐỘ)

*Dự án: BaChuKhuRung3D (Game nông trại 3D YWONDERLAND)*
*Ngày cập nhật: 24/06/2026*

## 0. Cập nhật 24/06 (mới nhất)

> Nhánh đang dùng: `feat/animal-husbandry` (đã có thay đổi cục bộ touch-control/build-flow). Chi tiết xem `CHANGELOG.md` + `docs/CHANGELOG.md` + `task.md`.

- **Điều khiển mobile đã ổn định cho sprint/tap-hold/auto-run:** `PlayerController` có state sprint chung, `GameHUD` giữ đúng trạng thái sprint khi bấm hoặc giữ.
- **Camera touch chỉnh lại:** smoothing riêng cho touch, pitch clamp theo yêu cầu kiểm duyệt.
- **Build/chuồng đang hoàn thiện:** ghost preview là prefab mờ, rào có auto-connect; cài đặt búa animation khi đặt công trình.
- **Hệ chăn nuôi:** ô chuồng lấy theo cụm hàng rào (PenEnclosure), kiểm tra kích thước trước khi thả thú, popup thông tin con vật đã tích hợp.
- **Hỗ trợ tutorial flow mới:** NPC khó tính, đi theo hành vi và nhắc khéo (đã tránh spam thoại quá nhanh).
- **Còn dang dở ưu tiên:** hệ hiệu ứng thu thập bay vào túi đồ, hủy chuồng thu lại tài nguyên, trồng theo ô theo từng loại cây (sau khi hoàn tất phần chăn nuôi cơ bản).
- **Tốc độ demo trước build APK/Windows:** giữ `GameTimeConfig.SecondsPerGameDay = 60f` (1 ngày game = 60 giây thật), không đổi về 24h thật trước test chéo. Expected test nhanh: cây ngắn ngày ~60s sau tưới; Sa Chi/Sầu Riêng ~28 phút; Chanh dây ~90 phút; vịt 60s, gà 120s, dê/ngỗng 180s, đà điểu 360s, bò 420s.
- **NPC tutorial marker:** đã thay dấu chấm than primitive bằng prefab `Assets/_Project/Prefabs/ExclamationMark.prefab`; khi spawn gỡ collider con để không chắn ray/click.
- **Backend/VPS:** client hiện có khung REST (`BackendConfig`, `ApiClient`, `AuthService`, `PlayerProfileService`) nhưng mới phủ auth/profile/tutorialCompleted. Muốn demo VPS tối thiểu thì deploy `server/` stub và tạo `Assets/Resources/BackendConfig.asset` trỏ `baseUrl` về URL public; online thật cho POS/inventory/farm/cây/thú/server-time/IAP là phase backend riêng sau demo.

## 1. Cập nhật 20/06 (lịch sử)

> Nhánh lúc đó: `feat/animal-husbandry`. Chi tiết xem CHANGELOG mục 20/06 + task.md.

- **Điều khiển mobile (GameHUD):** joystick ảo di chuyển, nút Sprint giữ-để-chạy (fix Clickable nuốt event bằng **TrickleDown**), nút Jump, nút **X hủy hoạt ảnh**. Tương tác ngắm theo điểm chạm; nút gợi ý bấm/tap được (fix picking-mode cha Ignore).
- **Bỏ tính năng Vuốt ve.**
- **Build snap theo Ô ĐẤT THẬT** (`BuildSurfaceCell`, cube map = 0.8) thay lưới ảo. Tool Editor "sơn vùng" gắn hàng loạt. Gizmo hiện trạng ô (xanh lá=trống, đỏ=ô chuồng, xanh dương=có thú).
- **Hệ chuồng từ hàng rào (#6 XONG):** rào = hộp vuông trên 1 ô → **ô có rào = ô chuồng**. `PenEnclosure.FindPen` đếm cụm ô-rào liền nhau; ngắm/click → "Thả thú" → validate `penSlots` vs ô trống → thả hoặc báo lỗi (ScreenToast). Cần gắn `AnimalPrefabLibrary` (itemId→prefab thú).
- **Popup Thông tin con vật (#4 XONG):** giá/ô/thức ăn/sản phẩm; data 10 con từ bảng VatNuoi (chạy menu `Generate Animal Data`).
- **Thông tin con vật ở Shop + Túi đồ (XONG):** chèn "Thông tin nuôi" vào mô tả; tra qua `AnimalManager.LookupDefinition` (fallback Resources → chạy kể cả khi chưa gắn AnimalManager).
- **Dọn menu Build còn 3 mục** (Ruộng/Đường đá/Chuồng); **đường đá** map StoneSlab; **fix ghost luôn đỏ** (RaycastAll tìm BuildSurfaceCell, bỏ qua mesh nền); **loadout test** (`InventoryManager.GiveTestLoadout` + cờ).
- **Việc cần làm trong Editor:** gắn `BuildSurfaceCell` cho khối map ("sơn vùng"); 3 entry `BuildPrefabLibrary` (ruộng→Dirt, đường đá→StoneSlab, chuồng→Fence **stretch OFF**); gắn `AnimalPrefabLibrary` + prefab thú; gắn `AnimalManager`; chạy `Generate Animal Data`; gỡ component `PetInteraction` khỏi prefab thú.
- **HỆ NPC (đã lập task — xem task.md):** kịch bản "10+ NPC". ĐÃ CÓ: Guide NPC (tutorial) + 1 Merchant mẫu. CHƯA: shop keeper đa-NPC (data-driven), Maid VIP, Pet, NPC mỏ (vé/quặng), NPC câu cá, AI Chat NPC.
- **Còn dang dở (chăn nuôi):** hiệu ứng thu thập bay vào túi; hủy chuồng → hoàn tài nguyên; trồng từng ô ruộng. **Mobile còn:** #3 Mouse→Pointer, #4 camera 1 ngón, #5 safe area.

## 1. Bối cảnh phiên làm việc

**Sprint Demo Gameplay** — khách yêu cầu demo (chiều 14/06): nhân vật Nam/Nữ chạy/nhảy/bơi, tương tác (câu cá, vuốt ve thú, trồng cây, chặt cây), và **đi lại giữa đảo Nông trại ↔ Thành phố**. Demo chạy trong **Unity Editor (Play Mode)**. Ưu tiên: **ỔN ĐỊNH** các tương tác.

> Trạng thái dự án: phần lớn còn **mockup**, **offline** (PlayerPrefs, chưa Cloud Save), đang ở nhánh `feat/inventory-economy`. KHÔNG có QC chính thức (chỉ giữa user & AI).

## 2. Đã hoàn thành phiên này (chi tiết xem CHANGELOG mục 14/06)

- **Đi đảo (P1):** `IslandTravelManager` (additive scene) + nối Bản đồ + cổng `MapPortalTrigger`.
- **Tương tác động vật (P2):** anim Vuốt ve/Cho ăn chạy đúng (Petting/Feed).
- **Trồng cây chọn loại (P4):** chọn hạt trong túi → **múa trồng xong mới gieo** → cây model 3D lớn dần.
- **Chặt cây:** có anim TreeCutting khi giữ chuột, tầm chỉnh được, hết xoay ngang.
- **UX:** chuột tự trả khi mở popup (`UIPopupTracker`); gỡ phím F/R toàn cục; name tag độc lập scale; anim tự đo độ dài clip + tham số speed.

## 2b. Đã làm thêm 15/06 (chi tiết xem CHANGELOG mục 15/06)

- **Dụng cụ cầm tay (`EquipmentManager`):** tự sinh placeholder primitive gắn vào xương bàn tay; đủ Rìu/Cúp/Cuốc/Bình tưới/Cần câu/Túi hạt/Nắm cám; gán model thật vào ô là tự thay. Mỗi hành động cầm đúng đồ nghề.
- **Cây ĐỔ GỤC:** chặt/đập xong cây xoay quanh đáy đổ nghiêng rồi mới ẩn (`HarvestableResource.FallAndHide`).
- **Câu cá có DÂY + PHAO (`FishingLineController`):** dây (LineRenderer) + phao bay vòng cung ra nước → nổi → tự thu về cần → ẩn. Chỉnh `castDelay`/`reelDelay`/`reelDuration` cho khớp animation.
- **`AnimEventToolHider`:** relay cho Animation Event (ẩn cây giống lúc cắm / bung dây lúc vung cần) — gắn lên object có Animator.
- **Animation lao động:** state chặt/đào/cuốc/tưới dùng chung **`TreeCuttingV4`**; có thêm anim `Watering`, `Plant`, `Planting`.

## 3. Việc CẦN SETUP trong Editor (user làm — AI không vào Editor được)

- [ ] Tạo `CityScene` (Plane tạm) + thêm Build Settings + cấu hình `IslandTravelManager` (spawn từng đảo) + đặt cổng portal 2 bên.
- [ ] Gán `Crop Prefab` cho `Crop_cabbage_seed_01` và `Crop_corn_seed_01` (carrot xong rồi).
- [ ] Dựng vài ô `FarmTile` (GameObject rỗng + Box Collider **Is Trigger** + bật `Use Custom Crop Models`) rải trên mảnh đất; mảnh đất dài chỉ để trang trí.
- [ ] Kiểm tra Animator prefab Nam có state `Petting`/`Feed`; state `Planting` trỏ đúng clip.

## 4. Việc CÒN LẠI (code)

- [ ] **Restyle popup "Xem thông tin" động vật** cho khớp Cozy Dark Palia (#2 — chưa làm).
- [ ] **Khung `ShopkeeperNPC`** (NPC mua/bán/nâng cấp) để mai gắn model 3D — nhớ nối `UIPopupTracker` cho Workshop/popup mới.
- [ ] (Tùy chọn) Giảm `growthTimeSec` trong `Assets/Resources/Items/Crop_*.asset` cho cây lớn nhanh khi demo.
- [ ] Câu cá ở Thành phố: chỉ cần đặt vùng nước + tái dùng `FishingSpot` (script sẵn sàng).

## 5. Lưu ý kỹ thuật quan trọng

- `FarmInteractionController` KHÔNG nằm trên nhân vật → lấy nhân vật dùng **`PlayerController.Instance`**, KHÔNG dùng `GetComponent`.
- Model Nữ (artist A) đang scale 2 (mesh nhỏ); Nam (artist B) scale 1. Nên chuẩn hoá Nữ về scale 1 qua FBX Scale Factor **SAU demo** (tránh lỗi scale ngầm).
- `Current Crop` trong FarmTile là **auto-assigned lúc runtime**, set tay vô tác dụng — loại cây do hạt người chơi chọn.

> [!TIP]
> **Cho AI mới:** Script tương tác chính là `Assets/_Project/Scripts/Environment/FarmInteractionController.cs` (raycast tâm ngắm, xử lý cuốc/trồng/tưới/thu hoạch/chặt/câu/click NPC). Trồng trọt: `FarmTile.cs` + `CropDatabase`/`CropDefinition` (trong `Assets/Resources/`). Chuyển đảo: `IslandTravelManager.cs`. Animation hành động: `PlayerController.PlayActionAnimation()` (tự đo độ dài clip + speed) — gọi `EquipmentManager.ShowTool()` để hiện dụng cụ trên tay. Dây câu: `FishingLineController.cs` (timed cast/reel). Animation Event: `AnimEventToolHider.cs` (gắn trên object có Animator). Đọc thêm `docs/MEMORY.md` mục 53–61 cho bài học phiên này.
