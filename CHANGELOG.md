# CHANGELOG

## [Unreleased] - 2026-06-14
### Added
- **Hệ thống Chuyển Đảo (Island Travel):**
  - Tạo `IslandTravelManager.cs`: chuyển đảo bằng Additive Scene (Farm = scene nền luôn giữ tải → UI/Manager/Player không mất; đảo phụ load đè/unload). Cấu hình danh sách đảo + điểm spawn trong Inspector.
  - Nối Bản đồ (`MapPopupController`) vào engine (gỡ `// TODO`): bấm đảo trên Map là dịch chuyển thật + đồng bộ chấm "bạn đang ở đây".
  - Tạo `MapPortalTrigger.cs`: cổng portal vật lý — bước vào vùng trigger là tự mở Bản đồ.
- **Công tắc UI trung tâm (`UIPopupTracker.cs`):** Theo dõi "có popup nào đang mở". Khi mở popup → camera tự TRẢ CHUỘT + ngừng xoay + chặn tương tác thế giới (tránh click xuyên UI). Đã nối Map/Shop/Inventory/Animal popup.
- **Trồng cây chọn loại (P4):** Click ô đã cuốc → mở Túi đồ (tab Hạt giống) → chọn loại cây → **múa động tác trồng xong MỚI gieo hạt**. Tự tặng hạt khởi đầu (carrot/cabbage/corn) để demo có lựa chọn.
- **Model 3D thật cho cây trồng:** `CropDefinition` thêm `Crop Prefab` + `Model Ground Offset` + `Seedling Scale`. `FarmTile` thêm cờ `Use Custom Crop Models` → spawn model thật theo loại cây chọn và phóng to dần khi lớn.
- **Tầm tương tác cây/đá theo từng vật:** `HarvestableResource` thêm `Interaction Range` (mặc định 2m) + gizmo vàng; chỉ hiện gợi ý + cho chặt khi đứng đủ gần (đo tới điểm tâm ngắm).

### Changed
- **Animation hành động thông minh (`PlayerController`):** `PlayActionAnimation` tự đo ĐÚNG độ dài clip (truyền `0` = lấy độ dài clip, khỏi gõ số tay; fallback đọc clip thực tế đang phát). Thêm tham số `speed` (Trồng cây chạy x2 ~4.6s), tự trả tốc độ về thường khi xong.
- **Phím tắt:** Gỡ phím `F` toàn cục mở câu cá (giờ chỉ câu khi chĩa tâm vào nước) và phím `R` toàn cục mở Workshop (nhường `R` cho Thu hoạch động vật) trong `GameHUDController`.
- **Tài liệu `DESIGN.md` & `RULES.md`:** Làm rõ quy tắc glass — chỉ cấm LẠM DỤNG (glass cho HUD vẫn OK), thêm "không lạm dụng icon/emoji".

### Fixed
- **Anim tương tác động vật không chạy:** `FarmInteractionController` gọi nhầm anim `"Interact"` (không tồn tại) → đổi sang `"Petting"` / `"Feed"` thật.
- **Anim trồng/cuốc không chạy:** `GetComponent<PlayerController>()` trả null vì controller không nằm trên nhân vật → đổi sang `PlayerController.Instance`.
- **Chặt cây không có anim + cây xoay ngang:** Thêm anim `TreeCutting` lặp khi giữ chuột; rung cây lắc TƯƠNG ĐỐI quanh hướng gốc (không phá thế đứng model).
- **Cây trồng bị nghiêng:** `FarmTile` giữ nguyên góc xoay gốc của prefab (Blender import -90° trục X), không reset `localRotation` về identity khi spawn.
- **NullReference khi vuốt ve:** `FarmAnimal.Pet()` null-check `data` và `visualObject`.
- **Name tag Nữ bay lên trời:** `FloatingNameTag` bám toạ độ THẾ GIỚI thay vì làm con của nhân vật → độc lập hoàn toàn với scale model.

## [Unreleased] - 2026-06-13
### Added
- **Menu Tương Tác Nổi (Floating Action Menu):**
  - Ra mắt hệ thống UI tương tác động (`GameHUD.uxml` & `GameHUD.uss`), thay thế hoàn toàn dòng chữ tĩnh nhàm chán cũ. UI được thiết kế theo phong cách Kính mờ (Glassmorphism), có hiệu ứng phóng to khi hover/active.
  - Hỗ trợ **Mobile Friendly 100%**: Người chơi có thể dùng ngón tay bấm trực tiếp vào các nút nổi lơ lửng trên màn hình mà không cần dùng phím tắt PC.
  - Tích hợp cho **Thú cưng (`FarmAnimal.cs`)**: Cung cấp Nút "Vuốt ve" (Pet), tự chèn Nút "Cho ăn" (nếu đói), "Chữa bệnh" (nếu ốm), "Thu hoạch" (nếu có sản phẩm). Bổ sung hoạt ảnh trái tim khi vuốt ve và thú cưng tự động ngoảnh mặt nhìn camera.
  - Tích hợp cho **Tài nguyên (`HarvestableResource.cs`)**: Khi nhìn vào Cây cối hoặc Đá, Menu Nổi cung cấp Nút "Chặt cây" / "Đập đá". Mỗi click bằng 1 giây tiến trình, cho phép chặt cây bằng cách chạm màn hình thay vì đè phím mỏi tay.
  - Cơ chế "Auto-giving": Tự động cấp Rìu/Cúp ảo vào túi đồ nếu người chơi không có vũ khí để thuận tiện cho việc test tính năng. Mọi thiếu sót về model 3D (Cúp/Rìu) được bắt lỗi mềm, không gây crash game.
- **Kịch Bản Chào Hỏi NPC (Tutorial Waving & Pointing):**
  - Cập nhật `GuideNPC.cs` chạy chuỗi Coroutine `StartGreetingSequence` lúc khởi động game thay vì chạy đi ngay.
  - NPC tự động diễn hoạt ảnh Vẫy tay (Waving) khi người chơi ở cách xa hơn 5m, luôn hướng mặt nhìn theo người chơi.
  - Khi khoảng cách bé hơn 5m, NPC tự động chuyển sang hoạt ảnh Chỉ đường (Pointing) về phía trạm số 1, giữ tư thế 1.5 giây rồi mới bắt đầu dắt người chơi đi.
  - Công khai 2 biến Inspector `waveAnimName` và `pointAnimName` để team 3D tiện bề tinh chỉnh.
- **Bảng Chọn Cảm Xúc (Emote Popup Grid):**
  - Tận dụng icon mặt cười ☺ trên khung chat để làm nút Bật/Tắt một `EmotePopup` nằm gọn gàng ngay trên thanh chat.
  - Thiết kế tuân thủ hoàn toàn `DESIGN.md` (nền Mystic Black, bo góc mềm 16px, nút bấm không đổ bóng lồi lõm).
  - Tích hợp 4 nút cảm xúc: Vẫy tay (👋), Chỉ trỏ (👉), Cười (😂), Nhảy (💃). Bấm vào 👋 và 👉 sẽ lập tức điều khiển nhân vật diễn xuất Animation tương ứng và tự đóng khung popup.

### Changed
- **Tái Cấu Trúc Bản Đồ (Map1.prefab):** *(Thực hiện bởi Unity AI Assistant)*
  - Gom 144 vật thể rải rác vào 7 thư mục cha chuyên biệt (`1_Terrain_&_Pathways`, `2_Pond_&_Water`, `3_Farm_Plots_&_Crops`, v.v.).
  - Chuẩn hóa tên tiếng Anh (Naming Convention) với tiền tố phân loại và đánh số đuôi `00-based`.
  - Thiết lập MeshCollider cho địa hình, công trình cứng. Loại bỏ Collider khỏi lá cây, thảm cỏ, và nông sản để nhân vật không bị kẹt khi nhảy hoặc di chuyển.
  - Dọn sạch lỗi Console phát sinh từ việc ghi đè Prefab.
- **Đồng bộ UI Lò Rèn (WorkshopPopup):** Viết lại toàn bộ CSS của giao diện Lò Rèn (phím R) sang phong cách Cozy Dark Palia phẳng hoàn toàn giống hệ thống Shop.
- **Giao diện HUD (GameHUD):** Dời nút Chạy Nhanh (Sprint) tách rời khỏi Joystick, căn lề tĩnh dạng tuyệt đối (`position: absolute`) sang bên phải để nằm song song với cụm nút Mail / Leaderboard.
- **Tối ưu Trải Nghiệm Khung Chat (UX):**
  - Ở trạng thái Chat thu gọn, bấm vào icon mặt cười vẫn gọi được Bảng Cảm Xúc lên.
  - Bấm vào đoạn tin nhắn text hiển thị trước ở thanh thu gọn sẽ tự động bung khung chat và đặt sẵn con trỏ chuột nhấp nháy vào ô nhập liệu để gõ liền mạch, không cần qua bước trung gian.

### Fixed
- **Lỗi Phím F Câu Cá:** Sửa lỗi bấm F để câu cá không có tác dụng do hàm `GetComponent<PlayerController>()` trong `FarmInteractionController` bị sai bối cảnh. Đổi sang `Object.FindFirstObjectByType<PlayerController>()` để nhân vật hoạt động chính xác.
- **Chặn kẹt Phím Tắt khi Gõ Chat:**
  - Bổ sung hàm giám sát `IsTyping()` trong `ChatPanelController`.
  - Khắc phục triệt để tình trạng gõ phím trong khung chat làm kích hoạt các kỹ năng/hành động lộn xộn. Giờ đây, khi đang focus vào ô gõ chữ, mọi chức năng: Di chuyển (WASD), Chạy nhảy (Shift/Space), Hành động (Chuột trái, F, P) hay Mở Popup (M, I, B, L, E, R, 1) đều tự động bị "đóng băng" cho tới khi gõ xong.
## [Unreleased] - 2026-06-12
### Added
- **Hệ Thống Nước Thủy Động (Stylized Water URP):**
  - Tạo Shader `StylizedWaterURP.shader` chuẩn URP hỗ trợ độ sâu nước (Shallow/Deep Color), bọt trắng đánh bờ (Foam), Normal Map cuộn sóng lấp lánh phản chiếu mặt trời.
  - Tích hợp Normal Map lượn sóng tự nhiên tự động cuộn theo thời gian.
  - Expose toàn bộ tham số sóng, bọt, màu sắc ra Material để người dùng tuỳ chỉnh.

### Changed
- **Cơ chế bơi lội & Animation:**
  - Thêm hệ thống Lực đẩy Archimedes (Buoyancy) vào `PlayerController.cs`. Nhân vật sẽ tự động nổi bồng bềnh ngang ngực theo chính xác cao độ của mặt nước.
  - Công khai tên các biến Animation (`animIdle`, `animWalk`, `animRun`, `animJump`, `animSwim`) ra Inspector để tái sử dụng một script `PlayerController.cs` cho cả mô hình Nam lẫn Nữ.
  - Nới lỏng thời gian khóa hành động câu cá (Action Lock) lên 8.5 giây để khớp với hoạt ảnh.

### Fixed
- Lỗi **Câu cá xuyên thấu**: Chặn tia Laze quét đâm xuyên lòng đất. Tia Raycast giờ đây sẽ dừng lại ngay khi chạm vật cản cứng (mặt đất), ngăn ngừa việc đứng trên bờ nhìn xuống đất vẫn câu được cá ở biển ngầm.
- Lỗi **Đè phím câu cá tự do**: Chuyển quyền quản lý nút `F` câu cá từ bộ đếm chung của Player sang bộ quét thông minh của `FarmInteractionController`, ngăn chặn nhân vật gõ cần câu xuống đất.
- Lỗi **Rơi tự do xuống đáy bản đồ**: Sửa lỗi nhân vật bị rớt lọt qua khối trigger nước xuống vũ trụ bằng cơ chế tính toán lại trọng lực và thêm lực đẩy nổi.
- Lỗi **Nam/Nữ lệch khớp tên Animation**: Khắc phục lỗi đơ hoạt ảnh khi nhân vật Nam gọi nhầm tên clip của Nữ bằng biến Public Inspector.

## [Unreleased] - 2026-06-11
### Changed
- **Cập nhật Giao diện HUD (Glassmorphism):**
  - Áp dụng phong cách Glassmorphism (nền trong suốt Dark Navy kết hợp viền trắng mỏng) cho toàn bộ UI HUD (thanh chức năng, kho đồ, phím nóng).
  - Thay thế toàn bộ text/emoji trên HUD bằng Sprite phẳng 2D (Flat 2D Icons) để tăng mức độ dễ đọc và tinh gọn giao diện.
  - Ẩn các phím hành động thừa (Interact, Cancel) để tiết kiệm không gian màn hình.
- **Cập nhật Style Guide (`DESIGN.md`):**
  - Chính thức quy định: HUD sử dụng Sprite 2D phẳng; Các Popup tĩnh (Cửa hàng, Kho đồ) sử dụng Sprite 2.5D (Isometric / 3D Stylized) để mang lại cảm giác chân thực.
  - Nới lỏng quy tắc cấm Glassmorphism: Cho phép sử dụng giới hạn cho HUD để không che khuất camera 3D.
- **Cải tiến Giao diện Bản Đồ (Map Popup):**
  - Thay đổi hệ tọa độ các đảo trên bản đồ sang dùng điểm neo 0x0 (Anchor Point) kết hợp phần trăm (`%`) để hitboxes tự động scale chuẩn xác với mọi kích thước màn hình. Thu nhỏ tỷ lệ Map xuống 400x400.
  - Thêm Ngôi sao nhấp nháy (`map-player-indicator`) để hiển thị vị trí hiện tại của người chơi trên đảo.
  - Tích hợp tính năng cho nút La Bàn: Khi bấm sẽ Highlight vị trí đảo hiện tại mà không bật nhầm hộp thoại di chuyển.
- **Tinh chỉnh Hộp thoại Xác Nhận (Confirm Dialog):**
  - Thu nhỏ kích thước hộp thoại từ 480px xuống 320px để giao diện gọn gàng, phù hợp hơn với UI của game.
  - Xóa bỏ nút X ở góc phải trên cùng để giảm lộn xộn UI, người dùng sẽ dùng nút Hủy ở dưới.
  - Tự động ẩn nút Hủy và kéo giãn nút Xác nhận lên 100% chiều rộng đối với các hộp thoại chỉ có 1 lựa chọn (VD: Bảng thông báo).
- **Thanh cuộn hiện đại (Modern Scrollbar):**
  - Thêm CSS toàn cục vào `DesignSystem.uss` để thay thế thanh cuộn mặc định xấu xí của UI Toolkit thành thanh cuộn mỏng (6px), bo tròn, không nền và phản hồi khi hover/active.

### Added
- **Tài liệu 3D Pipeline:** Viết quy trình chuẩn cho họa sĩ 3D về cách xuất FBX từ Blender sang Unity chuẩn Humanoid (quy tắc đặt tên xương, T-Pose, tắt Add Leaf Bones, đồng bộ trục Y-Up).
- **Kịch bản tự động xử lý ảnh:** Chạy thành công Script Python `clean_icons.py` tự động cắt phông nền (remove bg), loại bỏ viền grid AI và crop ảnh hàng loạt cho các icon UI.
## [Unreleased] - 2026-06-10
### Changed
- **UI Lột Xác Toàn Diện (Cozy Dark Palia Flat Graphics):**
  - Gỡ bỏ hoàn toàn phong cách nút lồi lõm 3D (Tactile 3D bottom-border) cũ để chuyển sang chuẩn "Phẳng tuyệt đối" (Pure Flat Graphics).
  - Cập nhật tài liệu `DESIGN.md` và hệ thống thiết kế `DesignSystem.uss` sang chuẩn mới.
  - Sửa lỗi đổ bóng, lún không mong muốn trên các màn hình: Splash Loading, Login Screen, Character Select, Chat Panel.
  - Sửa màn hình **Settings Popup**: Làm phẳng UI, đổi màu nền sang Deep Blue / Primary Navy, đẩy nút Close vào Header, định dạng lại Slider nằm thành một hàng ngang riêng biệt để kéo thả chính xác hơn.
  - Sửa màn hình **Shop Popup**: Gọt phẳng UI, sửa lỗi dư thẻ đóng XML, thêm hệ màu Dark Theme.
  
### Fixed
- Sửa lỗi UI bị đè chữ: Chiều dài thanh label của Slider cài đặt được kéo dài để tránh đè chữ "Hiệu ứng âm thanh".
- Sửa lỗi **Click xuyên UI**: Bấm chuột trái vào nút UI (như Shop) trên màn hình bị nhận thành thao tác Tấn Công (Attack). Đã thêm kiểm tra `EventSystem.current.IsPointerOverGameObject()` vào `PlayerController.cs`.
- Thêm phím tắt số `1` để mở/đóng nhanh cửa hàng (Shop) và giải quyết lỗi null popup khi `ShopPopup` bắt đầu game ở trạng thái inactive.
- Sửa lỗi **liệt nút đóng (X)** trên Popup Bạn Bè (Friends) sau khi bấm nhảy tab do cơ chế bắt sự kiện `ClickEvent` của UI Toolkit bị xung đột. Đã chuyển sang dùng trực tiếp `.clicked +=`.
- Sửa lỗi thiếu viền vàng focus cho các ô nhập Tên đăng nhập và Email bên tab Đăng ký của màn hình Login.
- Thêm con trỏ nhấp nháy (`--unity-cursor-color`) cho ô nhập liệu ở màn hình Đặt Tên Nhân Vật do UI Toolkit tàng hình con trỏ mặc định.


## [Unreleased] - 2026-06-09
### Added
- Thêm cơ chế Tutorial động (Hướng Dẫn Tân Thủ) cho NPC GuideNPC (Anh Lâm Tốt Bụng).
- Thêm `targetAnimalPen` vào `TutorialManager` để dẫn người chơi đến chuồng thú sau khi thu hoạch.
- Thêm cơ chế tự động chuyển mốc thời gian lớn của cây trồng xuống 5 giây khi đang trong Tutorial.
- Thêm các đoạn hội thoại động (`walkDialogues`, `waitPlayerDialogues`, `actionDialogues`, `idleWarningDialogues`) để NPC spam nhắc nhở hoặc tấu hài khi người chơi AFK.
- Đăng ký event `AnimalManager.OnAnimalBought` để phát hiện khi người chơi mua thú nuôi trong Tutorial.
- **Thêm cơ chế Auto-Complete (Bảo vệ Speedrunner)** vào `TutorialManager` để chống kẹt tutorial khi người chơi thao tác quá nhanh (mua thú, bán đồ, đập đá trước khi NPC tới).
- **Thêm khối Forward Indicator** (Cục màu vàng) vào các bóng mờ và công trình 1x1 để người chơi có thể nhìn thấy hướng xoay một cách trực quan.

### Fixed
- Lỗi tàng hình công trình khi bấm Tick xây dựng do toạ độ Y bị hardcode. Giờ đây công trình bắt độ cao Y siêu chuẩn từ bóng mờ chuột.
- Lỗi `AnimalManager.Instance` bị null lúc `TutorialManager.Start()` bằng cách dời phần đăng ký event sang hàm `OnNPCArrivedAtAnimalPen()`.
- Bổ sung lệnh hủy đăng ký (unsubscribe) event `OnAnimalBought` trong `TutorialManager.cs` để dọn dẹp bộ nhớ.
- Lỗi cây trồng dùng nhầm thời gian lớn mặc định thay vì thời gian ngắn của Tutorial.
- Lỗi NPC liên tục lặp thoại không đúng lúc do điều kiện kiểm tra state bị lệch pha với hành động mua sắm.

### Changed
- Rút gọn thời gian sinh trưởng của Cà Rốt trong Tutorial xuống còn 5 giây.
- Gộp các bước hướng dẫn Gieo Hạt, Tưới Nước, Đợi Thu Hoạch để trải nghiệm game liền mạch hơn.
