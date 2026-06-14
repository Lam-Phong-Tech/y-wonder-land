# 🌿 Y WONDER GREEN FARM - MEMENTO PROTOCOL (BẢN GHI NHỚ TIẾN ĐỘ)
*Dự án: BaChuKhuRung3D (Game nông trại 3D YWONDERLAND)*
*Ngày cập nhật: 14/06/2026*

## 1. Bối cảnh phiên làm việc
**Sprint Demo Gameplay** — khách yêu cầu demo (chiều 14/06): nhân vật Nam/Nữ chạy/nhảy/bơi, tương tác (câu cá, vuốt ve thú, trồng cây, chặt cây), và **đi lại giữa đảo Nông trại ↔ Thành phố**. Demo chạy trong **Unity Editor (Play Mode)**. Ưu tiên: **ỔN ĐỊNH** các tương tác.

> Trạng thái dự án: phần lớn còn **mockup**, **offline** (PlayerPrefs, chưa Cloud Save), đang ở nhánh `feat/inventory-economy`. KHÔNG có QC chính thức (chỉ giữa user & AI).

## 2. Đã hoàn thành phiên này (chi tiết xem CHANGELOG mục 14/06)
- **Đi đảo (P1):** `IslandTravelManager` (additive scene) + nối Bản đồ + cổng `MapPortalTrigger`.
- **Tương tác động vật (P2):** anim Vuốt ve/Cho ăn chạy đúng (Petting/Feed).
- **Trồng cây chọn loại (P4):** chọn hạt trong túi → **múa trồng xong mới gieo** → cây model 3D lớn dần.
- **Chặt cây:** có anim TreeCutting khi giữ chuột, tầm chỉnh được, hết xoay ngang.
- **UX:** chuột tự trả khi mở popup (`UIPopupTracker`); gỡ phím F/R toàn cục; name tag độc lập scale; anim tự đo độ dài clip + tham số speed.

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
> **Cho AI mới:** Script tương tác chính là `Assets/_Project/Scripts/Environment/FarmInteractionController.cs` (raycast tâm ngắm, xử lý cuốc/trồng/tưới/thu hoạch/chặt/câu/click NPC). Trồng trọt: `FarmTile.cs` + `CropDatabase`/`CropDefinition` (trong `Assets/Resources/`). Chuyển đảo: `IslandTravelManager.cs`. Animation hành động: `PlayerController.PlayActionAnimation()` (tự đo độ dài clip + speed). Đọc thêm `docs/MEMORY.md` mục 53–61 cho bài học phiên này.
