# Changelog — Y WONDER LAND
# Format: Theo module, có ngày và danh sách files thay đổi

---

## [2026-06-01] — Module Game HUD
### Added
- HUD layout 8 thành phần (Player Info, Currency, Quest, Sidebar, Joystick, Action Buttons, Messages, Jump)
- HUD styles theo Tangible Playground (solid colors, retro shadow, mechanical press)
- HUD controller mockup với public API (SetPlayerInfo, SetCurrency, SetQuest, SetPlayerEXP)
- Nút Settings (tròn, đen) và Bag/Inventory (xanh dương, vuông bo góc)
### Design
- Action Buttons: hình tròn hoàn hảo, viền 3px, retro shadow
- Sidebar: solid white buttons (không dùng nền trong suốt)
- Layout cột trái: Player Info → Quest Bubble → Sidebar Buttons (flow tự nhiên, không overlap)
### Files changed
- Assets/UI/GameHUD.uxml (NEW)
- Assets/UI/Styles/GameHUD.uss (NEW)
- Assets/UI/GameHUDController.cs (NEW)

---

## [2026-06-01] — Bộ Documentation
### Added
- RULES.md — Quy tắc dự án + QC Pass system + quy tắc UGS
- docs/ARCHITECTURE.md — Kiến trúc Unity + UGS (viết lại từ template web)
- docs/DATA_SCHEMA.md — Cấu trúc Cloud Save, Economy, Leaderboards
- docs/API_CONTRACTS.md — Blueprint tích hợp 8 UGS services
- docs/CHANGELOG.md (NEW — file này)
### Files changed
- RULES.md (MODIFIED)
- docs/ARCHITECTURE.md (MODIFIED — viết lại)
- docs/DATA_SCHEMA.md (NEW)
- docs/API_CONTRACTS.md (NEW)
- docs/CHANGELOG.md (NEW)

---

## [2026-05-29] — Module Character Selection
### Added
- Màn hình chọn giới tính theo Tangible Playground style
- Card nhân vật: nền trắng, viền đậm, retro shadow
- Highlight card selected: viền xanh #2D7BFF
- Nút xác nhận với mechanical press effect
### Files changed
- Assets/UI/Styles/CharacterSelect.uss (NEW)
- Assets/UI/MainMenuUI.uxml (MODIFIED — redesign)
- Assets/UI/MainMenuUIToolkit.cs (MODIFIED — class name update)

---

## [2026-05-27] — Module Login/Register UI
### Added
- Màn hình đăng nhập/đăng ký với design Tangible Playground
- Form fields: Username, Password, Remember Me checkbox
- Nút Đăng nhập/Đăng ký với retro shadow + mechanical press
- Design System tokens (DesignSystem.uss)
### Changed
- GameManager.cs — Thêm Login state vào state machine
### Files changed
- Assets/UI/Styles/DesignSystem.uss (NEW) ⚠️ PROTECTED
- Assets/UI/Styles/LoginScreen.uss (NEW)
- Assets/UI/LoginScreen.uxml (NEW)
- Assets/UI/LoginScreenController.cs (NEW)
- Assets/GameManager.cs (MODIFIED) ⚠️ PROTECTED

---

## [2026-05-27] — Khởi tạo dự án
### Added
- Unity project setup (URP, Unity 2022 LTS)
- 3D environment cơ bản (terrain, trees, house)
- Character model + animations
- Camera controller
- docs/DESIGN.md — Hệ thống thiết kế "The Tangible Playground"
### Files changed
- docs/DESIGN.md (NEW)
- Assets/* (Initial setup)

---

<!-- Template cho module mới:

## [YYYY-MM-DD] — Module [Tên Module]
### Added
- Mô tả tính năng mới
### Changed
- Mô tả thay đổi
### Fixed
- Mô tả bug fix
### Security
- Mô tả cải thiện bảo mật
### Files changed
- path/to/file.cs (NEW/MODIFIED/DELETED)

-->
