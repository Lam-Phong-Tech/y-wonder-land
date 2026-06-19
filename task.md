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

## 🐄 NHÁNH HIỆN TẠI: Chăn nuôi trong lồng (animal husbandry)
> Sửa & bổ sung chức năng nuôi/trồng động vật trong chuồng.

- `[ ]` (định hướng — điền dần khi làm)

---

## Vấn đề còn tồn đọng (Pending Issues)
- `[x]` **Login UI & Validation:** Thay thế dòng chữ Y WONDER GREEN FARM bằng logo Y Wonder Hub. Cập nhật UI và Logic để validate các trường đăng nhập, đăng ký tối đa 20 ký tự, đúng định dạng.
- `[x]` **Character Select UI & Logic:** Thay thế chữ M/F bằng Avatar ảnh (Nam/Nữ) tương ứng. Đặt ảnh vừa chọn làm avatar mặc định. Validate đặt tên nhân vật tối đa 20 ký tự.
- `[ ]` **Sửa lỗi Layout Popup cũ:** Cập nhật thêm các popup khác (Inventory, Friends, Map...) theo chuẩn Flat Graphics (Dark Theme) và sửa các lỗi chồng chéo layout nếu có.
- `[ ]` **Giao diện Popup:** Các icon 2.5D trong các Popup hiện tại (Cửa hàng, Kho đồ) chưa đúng phong cách mong muốn. Đang chờ ảnh mới từ Unity Muse/AI.
- `[ ]` **3D Model/Rigging:** Cần áp dụng quy trình Blender xuất FBX Humanoid mới cho các NPC/Nhân vật sắp tới để tránh lỗi mapping xương (vàng/đỏ) trong Unity.
- `[ ]` **Thống nhất Visual:** Cần quyết định cụ thể xem tiêu đề (Title) các Popup có nên dùng icon hay bỏ đi để không bị lạm dụng icon gây rối mắt.

---

## Bước tiếp theo (Next Steps)
- `[ ]` **Sản xuất Asset:** Chạy AI với bộ Prompt đã tạo để sinh ra bộ vật phẩm 2.5D mới (Cà chua, Hạt giống, Rìu, Cuốc, Khoáng sản...).
- `[ ]` **Tích hợp UI Popup:** Đưa các sprite 2.5D mới vào Unity, xử lý tách nền và gắn vào các slot chứa đồ trong `InventoryPopup.uxml` và `ShopPopup.uxml`.
- `[ ]` **Kiểm thử Rigging FBX:** Import thử một file FBX nhân vật mới do bạn Artist làm theo workflow chuẩn để kiểm tra thẻ Rig Humanoid trong Unity.
