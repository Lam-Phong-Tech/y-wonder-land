# Changelog — Y WONDER LAND
# Format: Theo module, có ngày và danh sách files thay đổi

---

## [2026-06-02] — Design System & AI UI Guidelines
### Added
- Tài liệu `docs/DESIGN_SYSTEM_TEMPLATE.md` tích hợp các quy chuẩn thiết kế "The Tangible Playground" và 15 câu hỏi trong quy trình tự kiểm soát chất lượng (AI Agent Self-Check Protocol) để ngăn ngừa triệt để 8 bệnh lý giao diện phổ biến của AI.
### Files changed
- docs/DESIGN_SYSTEM_TEMPLATE.md (NEW)

---

## [2026-06-02] — Module Friends Popup
### Added
- Popup Bạn Bè (Friends Popup) landscape theo phong cách "The Tangible Playground" và hình ảnh tham khảo.
- Cột bên trái gồm 3 tab dạng chữ gọn gàng, giảm thiểu icon: Bạn bè, Lời mời kết bạn, Tìm bạn.
- Khu vực hiển thị bên phải:
  - Thanh tìm kiếm theo tên và nút "Làm mới" danh sách gợi ý.
  - Danh sách người chơi dạng thẻ bo góc 14px, viền 2px và bóng đổ 3px, nền trắng.
  - Avatar đại diện dạng tròn hiển thị emoji, giới tính (♂/♀), cấp độ và trạng thái Online/Offline (chấm xanh/xám).
  - Các nút hành động dạng chữ rõ ràng: "Kết bạn" (xanh lá), "Xóa bạn" (đỏ), "Đồng ý" (xanh dương), "Từ chối" (xám).
- Mock Data phong phú cho cả 3 chế độ danh sách, tích hợp tìm kiếm lọc tên và chức năng thêm/xóa/phản hồi kết bạn cập nhật UI thời gian thực.
- Kết nối nút Bạn bè (👥) trên Game HUD để mở popup.
### Fixed
- Sửa lỗi chữ nút "Làm mới" bị khuyết thành "Làm" bằng cách rút gọn độ rộng của ô tìm kiếm (từ 170px xuống 120px), tránh tràn viền panel.
### Files changed
- Assets/UI/FriendsPopup.uxml (NEW)
- Assets/UI/Styles/FriendsPopup.uss (NEW)
- Assets/UI/FriendsPopupController.cs (NEW)
- Assets/UI/GameHUDController.cs (MODIFIED)

---

## [2026-06-02] — Module Leaderboard Popup
### Added
- Popup Bảng Xếp Hạng (Leaderboard) theo phong cách thiết kế "The Tangible Playground" và hình ảnh tham khảo.
- Hàng ngang chứa 5 tab phân loại: Diligence (EXP), Level (★), Fashion (★), Pet (★), Rich (Coin).
- Lưới danh sách sọc vằn Zebra (Hạng 1 nền kem nhạt, Hạng 2 nền xanh lơ nhạt, Hạng 3 nền hồng nhạt, các hạng sau trắng/xám lơ nhẹ xen kẽ).
- Huy chương Unicode (🥇, 🥈, 🥉) nổi bật cho top 3 và khung hình thoi màu vàng nhạt cho thứ hạng 4 trở đi.
- Nút đóng (X) màu đỏ cơ học có bóng đổ ở góc trên bên phải.
- Biểu tượng Cúp Vàng 3D đồ chơi mộc mạc (không chứa chữ) nhô lên đè trên thanh tiêu đề chính.
- Một thẻ nhỏ nổi lên ở góc dưới bên phải hiển thị thứ hạng của người chơi: "My Rank: 100+".
- Tích hợp dữ liệu giả lập (Mock Data) phong phú với tên nông trại thuần Việt (carot6868, Anhbaole, Haiau1982...) và chỉ số động thay đổi theo từng tab.
- Kết nối nút Cúp Vàng trên HUD để mở Bảng Xếp Hạng.
### Design
- Nền panel màu tím rực rỡ #5B42F3 (Hero Surface), bo góc 22px, viền 3px đen xám #3D3535.
- Hộp tiêu đề màu xanh lam capsule #4F59E3.
- Tab chưa chọn màu vàng cam #FDBE5B, tab đang chọn màu tím xám #8A7D9D.
### Fixed
- Căn giữa chữ "LEADERBOARD" trên thanh tiêu đề bằng cách loại bỏ phần padding bên trái dùng cho biểu tượng cúp vàng cũ (sau khi ẩn cúp vàng đi).
### Files changed
- Assets/UI/LeaderboardPopup.uxml (NEW)
- Assets/UI/Styles/LeaderboardPopup.uss (NEW)
- Assets/UI/LeaderboardPopupController.cs (NEW)
- Assets/UI/Textures/LeaderboardTrophy.png (NEW)
- Assets/UI/GameHUDController.cs (MODIFIED)

---

## [2026-06-02] — Module Inventory Popup
### Added
- Nâng cấp giao diện túi đồ (Inventory) thành bố cục 3 cột (Tabs -> Grid -> Detail Panel) landscape theo phong cách Tangible Playground
- Cột bên trái gồm 6 tab phân loại: Dụng cụ (Tool), Nguyên liệu (Material), Hạt giống (Seed), Thực phẩm (Food), Trang phục (Outfit), Đặc biệt (Special)
- Lưới 21 ô chứa vật phẩm mockup ở cột giữa
- Cột bên phải là Khung chi tiết vật phẩm (Item Detail Panel):
  - Hiển thị tên, icon lớn, số lượng và mô tả của vật phẩm được chọn.
  - Tự động thay đổi nút hành động động (Dynamic Button) theo loại (ví dụ: Trang bị, Ăn, Gieo hạt, Chế tạo...) và nút Vứt bỏ.
- Hỗ trợ tự động chọn vật phẩm đầu tiên khi mở túi đồ hoặc chuyển tab.
- Kết nối nút Túi đồ (Bag Button) trên HUD với Inventory Popup để mở/đóng popup.
- Hỗ trợ đóng popup qua nút đóng (X) hoặc bấm lại nút Túi đồ trên HUD.
### Design
- Header cam #E8833A, panel kem #F5F0E8
- Khung chi tiết bên phải màu nền trắng #FFFFFF, bo góc 16px, viền đậm 3px đồng bộ.
- Thẻ vật phẩm khi được chọn có viền cam rực rỡ #E8833A.
- Viền đậm 3px, góc bo tròn 16px-22px, retro shadow 6px offset.
- Các tab được bo tròn góc trái và có mechanical press khi chọn.
### Files changed
- Assets/UI/Styles/InventoryPopup.uss (NEW)
- Assets/UI/InventoryPopup.uxml (NEW)
- Assets/UI/InventoryPopupController.cs (NEW)
- Assets/UI/GameHUDController.cs (MODIFIED)

---

## [2026-06-01] — GitHub Repository
### Added
- Kết nối dự án với GitHub: `Lam-Phong-Tech/y-wonder-land`
- Tạo `.gitignore` cho Unity (ignore Library, Temp, Obj, Build, IDE, OS files)
- Initial commit + push thành công
### Files changed
- .gitignore (NEW)

---

## [2026-06-01] — Module Settings Popup
### Added
- Popup cài đặt landscape (nằm ngang) 2 cột theo Tangible Playground
- 4 section: Âm thanh (Music/SFX), Camera (Sensitivity/Zoom), Đồ họa (Quality/Shadow), Chung (Language)
- 5 sliders + 1 toggle + 1 dropdown
- Nút X ở góc panel (kiểu Gemini viewer)
- 2 nút dưới: Xóa tài khoản (đỏ) + Thoát game (xanh)
- Overlay đen 40% khi popup mở
- Kết nối nút ⚙ trên HUD → mở Settings popup
### Design
- Header tím #5B42F3, panel kem #F5F0E8
- Slider accent tím, toggle pill-shaped
- Retro shadow, mechanical press on buttons
### Fixed
- Nút X bị chìm sau panel → chuyển SAU panel trong UXML (z-order)
- Chữ "CÀI ĐẶT" lệch → dùng position absolute căn giữa
- "Tiếng Việt" bị cắt → giảm label width, thêm min-width dropdown
- Toggle hiện ô vuông xanh → style lại thành pill-shaped track
### Files changed
- Assets/UI/Styles/SettingsPopup.uss (NEW)
- Assets/UI/SettingsPopup.uxml (NEW)
- Assets/UI/SettingsPopupController.cs (NEW)
- Assets/UI/GameHUDController.cs (MODIFIED — kết nối nút Settings)

---

## [2026-06-01] — Fork & Customize unity-ai-workflow
### Changed
- Toàn bộ toolkit customize cho Unity 2022 LTS (từ Unity 6.2+)
- Awaitable → UniTask (async pattern)
- K&R braces → Allman braces (code examples)
- Assets/_Project/ → Assets/ (type-based folder)
- Assembly Definitions + GameDebug → optional
### Added
- UGS Dashboard section trong TOOLING.md
- 8 UGS packages trong ASSET_RESOURCES.md
- UGS integration patterns trong network-engineer agent
- Manual Q<T>() binding thay thế Runtime Data Binding
- UGS routing entries trong AGENTS.md
- Section 9: UGS Rules trong .agent/rules/RULES.md
### Files changed (15 files)
- unity-ai-workflow/README.md (MODIFIED)
- unity-ai-workflow/CLAUDE.md (MODIFIED)
- unity-ai-workflow/.agent/rules/RULES.md (MODIFIED)
- unity-ai-workflow/.agent/rules/AGENTS.md (MODIFIED)
- unity-ai-workflow/.agent/agents/network-engineer.md (MODIFIED)
- unity-ai-workflow/.agent/agents/ui-specialist.md (MODIFIED)
- unity-ai-workflow/.agent/skills/ui-toolkit-binder/SKILL.md (MODIFIED)
- unity-ai-workflow/docs/CODING_STANDARDS.md (MODIFIED)
- unity-ai-workflow/docs/NAMING_CONVENTIONS.md (MODIFIED)
- unity-ai-workflow/docs/DESIGN_PRINCIPLES.md (MODIFIED)
- unity-ai-workflow/docs/TOOLING.md (MODIFIED)
- unity-ai-workflow/docs/ASSET_RESOURCES.md (MODIFIED)
- unity-ai-workflow/docs/phases/03_ProjectSetup.md (MODIFIED)
- unity-ai-workflow/templates/ProjectConfig_Template.yaml (MODIFIED)
- RULES.md (MODIFIED — thêm AI WORKFLOW REFERENCE table)

---

## [2026-06-01] — Documentation bổ sung
### Added
- docs/CONTEXT_RECOVERY.md — Prompt khởi động khi mở chat mới
- docs/MEMORY.md — 14 bài học kinh nghiệm, sai lầm cần tránh
- Context Canary trong RULES.md (AI xưng "bé", gọi "anh yêu")
### Changed
- RULES.md — Thêm MEMORY.md vào session checklist (bước 2)
- RULES.md — Thêm AI WORKFLOW REFERENCE table
### Files changed
- docs/CONTEXT_RECOVERY.md (NEW)
- docs/MEMORY.md (NEW)
- RULES.md (MODIFIED)

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
