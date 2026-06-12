# CHANGELOG

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
