# CHANGELOG

## [Unreleased] - 2026-06-08
### Added
- Thêm cơ chế Tutorial động (Hướng Dẫn Tân Thủ) cho NPC GuideNPC (Anh Lâm Tốt Bụng).
- Thêm `targetAnimalPen` vào `TutorialManager` để dẫn người chơi đến chuồng thú sau khi thu hoạch.
- Thêm cơ chế tự động chuyển mốc thời gian lớn của cây trồng xuống 5 giây khi đang trong Tutorial.
- Thêm các đoạn hội thoại động (`walkDialogues`, `waitPlayerDialogues`, `actionDialogues`, `idleWarningDialogues`) để NPC spam nhắc nhở hoặc tấu hài khi người chơi AFK.
- Đăng ký event `AnimalManager.OnAnimalBought` để phát hiện khi người chơi mua thú nuôi trong Tutorial.

### Fixed
- Lỗi `AnimalManager.Instance` bị null lúc `TutorialManager.Start()` bằng cách dời phần đăng ký event sang hàm `OnNPCArrivedAtAnimalPen()`.
- Lỗi cây trồng dùng nhầm thời gian lớn mặc định thay vì thời gian ngắn của Tutorial.
- Lỗi NPC liên tục lặp thoại không đúng lúc do điều kiện kiểm tra state bị lệch pha với hành động mua sắm.

### Changed
- Rút gọn thời gian sinh trưởng của Cà Rốt trong Tutorial xuống còn 5 giây.
- Gộp các bước hướng dẫn Gieo Hạt, Tưới Nước, Đợi Thu Hoạch để trải nghiệm game liền mạch hơn.
