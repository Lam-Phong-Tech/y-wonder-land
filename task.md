# Danh sách công việc dự án (Task Backlog & Progress)

## Tiến độ đã hoàn thành (Completed)

### 1. Nâng cấp Giao diện HUD (UI/UX)
- `[x]` Thiết kế lại `GameHUD` theo phong cách **Glassmorphism** (Nền Dark Navy trong suốt `rgba(58, 71, 102, 0.5)` kết hợp viền trắng mỏng).
- `[x]` Chạy script Python tự động xóa phông nền xám, cắt grid 200x200, cạo viền 8px để bỏ line AI, và bóc tách thành công bộ icon phẳng 2D từ `FarmingIconsCollection.png`.
- `[x]` Thay thế toàn bộ text/emoji trên HUD bằng **Sprite 2D phẳng** (Flat 2D Icons).
- `[x]` Ẩn tạm thời các nút Hành động (Interact, Cancel) trên HUD (`display: none;`).
- `[x]` Thống nhất Style Guide: **HUD** dùng Sprite 2D phẳng; **Popups (Túi đồ, Shop)** dùng Sprite 2.5D/3D Render (isometric, đổ bóng).
- `[x]` Đã xây dựng các **AI Prompts** chuẩn để tạo đồ họa 2.5D isometric cho game nông trại.

### 2. Tư vấn & Gỡ lỗi 3D Pipeline (Unity & Blender)
- `[x]` Phân tích và gỡ lỗi CharacterController bị lệch tâm so với nhân vật (xử lý Pivot khác biệt giữa Model và Root Empty Object).
- `[x]` Giải thích lỗi nhảy giật trục Y do tùy chọn "Bake Into Pose" trong thẻ Animation.
- `[x]` Tư vấn sự khác biệt, ưu nhược điểm giữa Animation `Generic` và `Humanoid`.
- `[x]` Cung cấp **Quy trình chuẩn (Pipeline) xuất FBX từ Blender sang Unity Humanoid** (Bone Naming theo chuẩn Mixamo/Unity, ép T-Pose, tắt Add Leaf Bones, Apply Transforms `1, 1, 1`, trục Y-Up/-Z-Forward).

### 3. Gameplay & Hệ thống (15–19/06/2026)
- `[x]` **Backend REST đợt 1**: server stub Node/Express + client (Auth/Profile), offline-first, lưu profile + cờ tutorialCompleted thật.
- `[x]` **Tài liệu kỹ thuật**: TDD, DB_SCHEMA (ERD), SECURITY, BUILD_RELEASE; rà soát điểm mù xin khách (DiemMu_CanXinKhach, TongKet_TaiLieu_CanCo); dọn mâu thuẫn UGS.
- `[x]` **Tưới cây cầm xô**, tự gom lá vào cây, tắt rung chặt/đập, dọn Splash.
- `[x]` **Camera PUBG/Free Fire** (nhân vật quay theo yaw, hết chóng mặt); fix cuốc lệch (xoay về ô đất).
- `[x]` **Build Mode sinh prefab THẬT** (`BuildPrefabLibrary`): xây ô đất (Dirt+FarmTile) & chuồng (Nhỏ 1x1/Vừa 2x2/Lớn 3x2) + animation Hammering.
- `[x]` **Ghost preview = bản mờ prefab** (xanh/đỏ kiểu ROK), WYSIWYG; bỏ lưới hiển thị; **tự bù pivot model lệch** (MakeCenteredClone).
- `[x]` **Hàng rào tự nối liền** (`FenceAutoConnect`) — tắt cạnh giáp kiểu Minecraft.
- `[x]` **Hệ chăn nuôi cơ bản**: click chuồng → mở túi (tab Thú nuôi) chọn con vật → thả (giới hạn loài theo cỡ chuồng); **cho ăn** qua túi (Bắp ngô) + animation Feed.
- `[x]` **Tutorial viết lại** (NPC ông lão khó tính): chặt cây → đào khoáng → xây ruộng → canh tác → xây chuồng → thả thú → cho ăn. Công tắc `ForceRunTutorialForTesting`.
- `[x]` **Vật phẩm con vật**: Gà, Đà điểu, Dê, Hươu, Thỏ, Bò; fix thuyền cutscene lật.

---

## 🐄 NHÁNH HIỆN TẠI: Chăn nuôi trong lồng (animal husbandry) — ⭐ ƯU TIÊN CAO NHẤT
> Sửa & bổ sung chức năng nuôi/trồng động vật trong chuồng.
> **Đây là nhóm việc ưu tiên số 1.** Hoàn thành hết nhóm này rồi mới quay lại các task tồn đọng phía dưới.

### Build theo Ô ĐẤT (surface-cell snapping) — 19/06
- `[x]` **Sửa lệch grid**: bỏ snap theo lưới ảo (`cellSize=1` lệch khối cube `0.8` + origin nhảy theo player). Ghost giờ snap vào **TÂM MẶT TRÊN** của khối cube đất (`BuildSurfaceCell`).
  - File mới: `BuildSurfaceCell.cs` (component đánh dấu ô: SurfaceCenter/FootprintSize/IsOccupied + registry), `Editor/BuildSurfaceCellSetup.cs` (menu gắn hàng loạt).
  - Sửa `GhostPlacementController.cs`: raycast → `GetComponentInParent<BuildSurfaceCell>` → snap tâm ô; validate theo `IsOccupied`; stretch theo 0.8.
  - **CẦN làm trong Editor**: map = 4000 khối "cube" (cỏ) + 400 "stone", KHÔNG nhóm, đảo méo mó, chỉ nửa phải buildable → kiểu **"sơn vùng"**: đặt nhiều BoxCollider ướm vùng buildable (lấn ra biển vô hại vì chỗ đó không có cube) → chọn hết → menu `Gắn BuildSurfaceCell theo VÙNG`; lỡ lấn khối không muốn → `Gỡ theo VÙNG`. Tag khối tên "cube*", tự thêm collider. Còn menu đệ quy + gỡ tất cả. Nếu khối gộp chung 1 mesh → đổi sang snap lưới 0.8 + 1 collider gộp.
  - TODO sau: highlight các ô buildable khi mở Build Mode (đã có `BuildSurfaceCell.All`); nối hủy công trình → `SetOccupied(false)`.

### Nhiệm vụ 19/06
- `[ ]` **Hiệu ứng thu thập**: khi thu thập (chặt/đào/thu hoạch...) làm vật phẩm **bay vào túi đồ** (animation item bay về icon túi).
- `[ ]` **Hủy chuồng → thu lại tài nguyên**: cho phép phá chuồng và hoàn lại một phần vật liệu đã xây.
- `[x]` **Bỏ tính năng Vuốt ve** (Pet) khỏi tương tác con vật. *(Gỡ nút E + hàm PetAnimal ở FarmInteractionController; vô hiệu hóa PetInteraction.cs. Còn: gỡ component PetInteraction khỏi prefab thú trong Editor.)*
- `[x]` **Thông tin con vật**: popup hiện giá mua / số ô chuồng / thức ăn chính / thức ăn phụ / sản phẩm — restyle Cozy Dark Palia. Thêm trường vào `AnimalDefinition` + điền data 10 con qua generator. **CẦN Editor**: chạy menu `YWonderLand ▸ Generate Animal Data` để nạp dữ liệu vào các asset `Animal_*.asset` (đảm bảo con vật spawn dùng đúng asset này).
- `[ ]` **Trồng từng ô ruộng kiểu xây hàng rào**: mỗi loài thực vật tốn số ô khác nhau. *(Khách CHƯA gửi số ô/loài cây → quyết định 19/06: **tạm cho mỗi cây = 1 ô**, chỉnh lại khi có dữ liệu thật.)*
- `[x]` **Sức chứa chuồng động + validate thả thú theo số ô**: rào = hộp vuông trên 1 ô → **ô CÓ RÀO = ô chuồng**. Ngắm/click ô rào → "Thả thú" → chọn loài → validate `penSlots` vs số ô-rào liền nhau còn trống (`PenEnclosure.FindPen` BFS cụm ô-rША 4-kề; nhiều rào kề = chuồng to) → đủ thì thả (`SetAnimal`), thiếu thì `ScreenToast` báo lỗi. Click thẳng (PC) + bấm chữ (mobile) đều chạy. Gizmo hiện trạng ô.
  - File mới: `PenEnclosure.cs` (flood-fill), `AnimalPrefabLibrary.cs` (map itemId→prefab thú), `ScreenToast.cs` (toast lỗi). Sửa `BuildSurfaceCell` (Occupant/HasFence/IsFree), `GhostPlacementController` (ghi occupant), `FarmInteractionController` (luồng thả vùng quây).
  - **CẦN Editor**: thêm 1 GameObject gắn `AnimalPrefabLibrary` + điền itemId→prefab thú; hàng rào phải đặt qua Build Mode (để ghi occupant vào ô). Phụ thuộc hệ `BuildSurfaceCell` đã chạy.
  - TODO: bước "xem thông tin loài trước khi thả" (confirm dialog) — hiện đang thả ngay khi chọn; báo lỗi đang dùng OnGUI toast (nâng UI Toolkit sau).

### Nhiệm vụ 20/06 (ưu tiên mới)
- `[ ]` **Xây mặt đường đá (paving)**: thêm loại công trình "đường đá" đặt được qua Build Mode (snap theo `BuildSurfaceCell` như đất/rào). Dùng để lát lối đi trang trí.
- `[x]` **Validate ô chuồng + cho thả NHIỀU con nếu đủ ô**: đã làm — `AvailableCount` (ô-rào chưa có thú) ≥ `penSlots` thì cho thả, đánh dấu `SetAnimal`; chuồng 9 ô thả được 9 gà (mỗi con 1 ô); chuồng còn 8 ô **KHÔNG** nhét được bò (9 ô) → báo lỗi. *(Còn TODO: nếu muốn giới hạn LOÀI theo cỡ chuồng thì bổ sung sau.)*
- `[ ]` **Hiển thị thông tin con vật ở 3 nơi** (chỉ thông tin cần thiết: giá / số ô / thức ăn / sản phẩm):
  - `[x]` Khi **xem thông tin** (popup AnimalInteractionPopup) — đã làm.
  - `[ ]` Khi **mua** (Shop popup) — thêm panel info khi chọn con giống.
  - `[ ]` Khi **chọn trong túi đồ** (Inventory, tab Thú nuôi) — thêm info trước khi thả.

---

## 📱 RÀ SOÁT MOBILE (19/06) — game hướng thị trường điện thoại
> Rà soát phát hiện UI/điều khiển đang là PC-first. Sửa theo độ ưu tiên dưới.

- `[x]` **#1 Joystick ảo điều khiển di chuyển**: nối `Joystick` (GameHUD.uxml) vào `PlayerController.SetMoveInput()` qua kéo pointer (GameHUDController) + thêm style núm `.joystick-inner`. Gộp bàn phím + joystick trong PlayerController. **Cần test trên Editor/thiết bị.**
- `[x]` **#1b Nút Sprint giữ-để-chạy-nhanh (cảm ứng)**: PC bấm Shift đã chạy; sửa nút Sprint trên HUD dùng pointer capture (bỏ `PointerOutEvent` gây hủy sớm) → giữ nút là tăng tốc. Trên phone: 1 ngón giữ Sprint + 1 ngón joystick. **Cần test.**
- `[x]` **#2 Nút Jump + Hủy hoạt ảnh (X)**: Jump → `PlayerController.TriggerJump()` (1-bấm-1-nhảy). **Bỏ nút bàn tay (Interact)** — tương tác qua các **nút gợi ý nổi** quanh tâm ngắm (đã bấm được, dùng chung `ShowInteractionPrompts`). Giữ **nút X** = `PlayerController.CancelAction()` (ngắt hoạt ảnh, cất đồ, ẩn thanh tiến trình), tự hiện khi `IsBusy`. **Ngắm theo TÂM màn hình** (ổn định, không giật; gợi ý đứng yên khi rê chuột tới bấm). **Nút gợi ý "Chặt cây" bấm/tap được** (mỗi lần = 1 nhát `ClickHarvestResource`); căn giữa dưới tâm, co theo nội dung để không chặn dải ngang. Giữ chuột ở tâm vẫn chặt liên tục như cũ.
- `[ ]` **#3 Tương tác đổi `Mouse.current` → `Pointer.current`**: FarmInteractionController khóa cứng chuột (dòng 61-62) → trên mobile các **nút gợi ý nổi không xuất hiện** (vòng lặp thoát sớm). Đổi sang `Pointer.current` để hover/bấm chạy trên cảm ứng. Lưu ý multitouch: khi kéo joystick không được kích hoạt hành động ở tâm (chặn bằng kiểm tra con trỏ đang trên UI).
- `[ ]` **#4 Camera xoay bằng kéo 1 ngón** (vùng phải màn hình): ThirdPersonCamera đang đọc chuột.
- `[ ]` **#5 Safe Area + khóa Landscape + chỉnh Match**: reference 1200×800 (3:2) lệch tỉ lệ điện thoại; chưa xử lý tai thỏ.

---

## Vấn đề còn tồn đọng (Pending Issues) — ⏳ ƯU TIÊN THẤP
> Làm SAU khi xong nhóm 19/06. Phần lớn liên quan **polish UI 2.5D + asset/artist** nên còn chờ tài nguyên.
> Đánh giá nhanh: Login & Character Select **ĐÃ XONG**; các mục còn lại chủ yếu **chờ ảnh AI / artist** hoặc là việc polish dài hơi.

- `[x]` **Login UI & Validation:** Thay thế dòng chữ Y WONDER GREEN FARM bằng logo Y Wonder Hub. Cập nhật UI và Logic để validate các trường đăng nhập, đăng ký tối đa 20 ký tự, đúng định dạng.
- `[x]` **Character Select UI & Logic:** Thay thế chữ M/F bằng Avatar ảnh (Nam/Nữ) tương ứng. Đặt ảnh vừa chọn làm avatar mặc định. Validate đặt tên nhân vật tối đa 20 ký tự.
- `[ ]` **Sửa lỗi Layout Popup cũ:** Cập nhật thêm các popup khác (Inventory, Friends, Map...) theo chuẩn Flat Graphics (Dark Theme) và sửa các lỗi chồng chéo layout nếu có.
- `[ ]` **Giao diện Popup:** Các icon 2.5D trong các Popup hiện tại (Cửa hàng, Kho đồ) chưa đúng phong cách mong muốn. Đang chờ ảnh mới từ Unity Muse/AI.
- `[ ]` **3D Model/Rigging:** Cần áp dụng quy trình Blender xuất FBX Humanoid mới cho các NPC/Nhân vật sắp tới để tránh lỗi mapping xương (vàng/đỏ) trong Unity.
- `[ ]` **Thống nhất Visual:** Cần quyết định cụ thể xem tiêu đề (Title) các Popup có nên dùng icon hay bỏ đi để không bị lạm dụng icon gây rối mắt.

---

## Bước tiếp theo (Next Steps) — ⏳ ƯU TIÊN THẤP (chờ asset/artist)
- `[ ]` **Sản xuất Asset:** Chạy AI với bộ Prompt đã tạo để sinh ra bộ vật phẩm 2.5D mới (Cà chua, Hạt giống, Rìu, Cuốc, Khoáng sản...).
- `[ ]` **Tích hợp UI Popup:** Đưa các sprite 2.5D mới vào Unity, xử lý tách nền và gắn vào các slot chứa đồ trong `InventoryPopup.uxml` và `ShopPopup.uxml`.
- `[ ]` **Kiểm thử Rigging FBX:** Import thử một file FBX nhân vật mới do bạn Artist làm theo workflow chuẩn để kiểm tra thẻ Rig Humanoid trong Unity.
