# CHANGELOG

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
