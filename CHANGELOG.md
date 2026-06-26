# CHANGELOG

## [Unreleased] - 2026-06-26 (iOS CI and app icon setup)

### Added
- Added a root `README.md` for clone/build handoff, backend scope, branch usage, and CodeMagic/iOS notes.
- Added `codemagic.yaml` at the repository root with a Unity iOS release workflow for CodeMagic.
- Added a CodeMagic Xcode-only TestFlight workflow that builds from a pre-exported Unity iOS Xcode project without activating Unity on CodeMagic.
- Added `Assets/_Project/Editor/BuildScript.cs` so CI can export an iOS Xcode project from Unity in batch mode.
- Added game thumbnail/icon assets under `Assets/_Project/UI/Sprites/`.
- Wired `ThumbnailGame.jpg` into Standalone app icon slots and Android adaptive icon slots in `ProjectSettings/ProjectSettings.asset`.

### Fixed
- Fixed Unity compile error in the CI build script caused by the project namespace `YWonderLand.Environment` shadowing `System.Environment`.
- Updated the CI build script to use `NamedBuildTarget.iOS` for the iOS bundle identifier API, removing the Unity 6 obsolete API warning.

### Changed Files
- `README.md`
- `codemagic.yaml`
- `Assets/_Project/Editor/BuildScript.cs`
- `Assets/_Project/Editor/BuildScript.cs.meta`
- `Assets/_Project/UI/Sprites/ThumbnailGame.jpg`
- `Assets/_Project/UI/Sprites/ThumbnailGame.jpg.meta`
- `Assets/_Project/UI/Sprites/Black.jpg`
- `Assets/_Project/UI/Sprites/Black.jpg.meta`
- `ProjectSettings/ProjectSettings.asset`

## [Unreleased] - 2026-06-26 (Interaction, enclosure, fishing, and icon polish)

### Added
- Fishing now uses a timed 8.7s action flow matching the `Fishing` animation clip, with a visible cancel/progress UI and cursor release while the action is running.
- Fishing reward logic grants a normal fish or rare fish at completion, with rare fish at 20%.
- Animal enclosure popup now supports a group view: clicking a connected pen opens all animals in that enclosure as selectable cards, then actions target the selected animal.
- Animal cards in the enclosure popup now render real animal item icons from `ItemDatabase` instead of text/emoji fallback.
- Build Mode item cards now render construction icons from `Assets/Sprites/icon/BoSungIcon/` for farm plot, stone path, and pen.

### Changed
- Fishing interaction range is tuned for shore use and currently capped around 5m.
- The floating interaction prompt is hidden while fishing is active so it no longer overlaps the cancel/progress UI.
- Timed world actions are being standardized toward cancelable clip-length flows for chopping, mining, planting, watering, and feeding.
- Enclosure popup actions are compacted into one row; disease/vaccine actions remain visible but disabled because vaccine/disease costs are not finalized.
- World hunger/water bars now use an explicit URP Unlit material template to avoid magenta bars in builds.

### Fixed
- Prevented fishing prompts from appearing while the player is swimming/underwater.
- Hardened interaction distance checks against non-convex colliders that cannot use `Collider.ClosestPoint`.
- Fixed farm tile/pen/animal interaction edge cases where prompts or click actions could disappear after range/raycast changes.
- Demolishing a connected pen now clears leftover animal interaction UI, destroys contained animal objects, returns animal items to inventory, and refunds build materials according to the existing demolish refund rules.
- Build Mode touch placement on Android remains pointer-based, while UI glyphs that became square boxes are replaced with safer text/icon assets.

### Changed Files
- `Assets/_Project/Scripts/Environment/FarmInteractionController.cs`
- `Assets/_Project/Scripts/Environment/FarmAnimal.cs`
- `Assets/_Project/Scripts/Environment/FarmTile.cs`
- `Assets/_Project/Scripts/Environment/GhostPlacementController.cs`
- `Assets/_Project/Scripts/Environment/PenEnclosure.cs`
- `Assets/_Project/UI/AnimalInteractionPopup.uxml`
- `Assets/_Project/UI/AnimalInteractionPopupController.cs`
- `Assets/_Project/UI/FishingOverlay.uxml`
- `Assets/_Project/UI/FishingOverlayController.cs`
- `Assets/_Project/UI/BuildModeOverlay.uxml`
- `Assets/_Project/UI/BuildModeOverlayController.cs`
- `Assets/_Project/UI/GameHUDController.cs`
- `Assets/_Project/UI/Styles/AnimalInteractionPopup.uss`
- `Assets/_Project/UI/Styles/BuildModeOverlay.uss`
- `Assets/Art/Environment/Material/WorldBar_Unlit.mat`
- `Assets/Resources/Materials/WorldBar_Unlit.mat`
- `Assets/Building/New/fence/Fence.prefab`
- `Assets/Sprites/icon/BoSungIcon/**`

## [Unreleased] - 2026-06-25 (Existing character login flow)

### Changed
- Added `characterCreated` to `player_profile` on the Unity client and Node server stub.
- Login now loads `/player/profile` and skips Character Select when `characterCreated=true`.
- `DemoRich01` through `DemoRich05` are treated as existing-character accounts, so testers no longer need to choose gender or name after login.
- Character Select now marks the profile as created when the player confirms name/gender.

### Changed Files
- `Assets/_Project/UI/LoginScreenController.cs`
- `Assets/_Project/Scripts/Managers/GameManager.cs`
- `Assets/_Project/Scripts/Backend/PlayerProfileService.cs`
- `server/index.js`
- `server/README.md`
- `docs/API_CONTRACTS.md`
- `docs/ARCHITECTURE.md`
- `docs/DB_SCHEMA.md`
- `docs/TECHNICAL_DESIGN.md`

## [Unreleased] - 2026-06-25 (Shop tab icon cleanup)

### Changed
- Shop popup mode/category tabs now use text-only labels (`Mua`, `Bán`, `Hạt giống`, `Vật nuôi`, `Dụng cụ`, `Vật phẩm`) without emoji icons.
- Item cards and item detail panel still keep graphic product icons from `ItemDefinition.iconTexture/iconSprite`.
- Long shop names now stay centered in the header and no longer slide underneath the POS pill or close button.
- Removed unsupported `z-index` from `ShopPopup.uss`.

### Changed Files
- `Assets/_Project/UI/ShopPopup.uxml`
- `Assets/_Project/UI/Styles/ShopPopup.uss`

## [Unreleased] - 2026-06-25 (Workshop icon rendering)

### Changed
- Workshop / Forge popup now renders tool and requirement icons with image assets instead of emoji labels.
- Added `iconTexture` links for basic tools and upgrade materials: axe, hoe, fishing rod, watering can, pickaxe, wood, stone, iron, and ore.
- Removed unsupported `z-index` from `WorkshopPopup.uss`.

### Changed Files
- `Assets/_Project/UI/WorkshopPopup.uxml`
- `Assets/_Project/UI/WorkshopPopupController.cs`
- `Assets/_Project/UI/Styles/WorkshopPopup.uss`
- `Assets/Resources/Items/axe_01.asset`
- `Assets/Resources/Items/hoe_01.asset`
- `Assets/Resources/Items/fishing_rod_01.asset`
- `Assets/Resources/Items/watering_can_01.asset`
- `Assets/Resources/Items/pickaxe_01.asset`
- `Assets/Resources/Items/wood_01.asset`
- `Assets/Resources/Items/stone_01.asset`
- `Assets/Resources/Items/iron_01.asset`
- `Assets/Resources/Items/ore_01.asset`

## [Unreleased] - 2026-06-25 (Quest popup icon cleanup)

### Changed
- Quest list no longer renders sword/gift/check emoji glyphs; in-progress and claimable quests use image icons from `Assets/Sprites/icon/`.
- Claimed quest rows now show a visual check mark inside the square, matching the mailbox read/claimed treatment.
- Quest reward slots now render icon elements from `ItemDatabase` or `Assets/Sprites/icon/BoSungIcon/` instead of emoji labels.
- Removed unsupported `z-index` properties from `QuestPopup.uss` to avoid Unity UI Toolkit warnings.

### Changed Files
- `Assets/_Project/UI/QuestPopup.uxml`
- `Assets/_Project/UI/QuestPopupController.cs`
- `Assets/_Project/UI/Styles/QuestPopup.uss`

## [Unreleased] - 2026-06-25 (Mailbox read/reward icons)

### Changed
- Mailbox read-state square now renders a visual check mark instead of emoji/file glyphs.
- Mailbox reward badge now uses `Assets/Sprites/icon/SanPham/VatPham/giftbox.png`; claimed rewards show a check mark.
- Mailbox attachment rewards now render image icons from `ItemDatabase` or `Assets/Sprites/icon/BoSungIcon/` instead of emoji labels.

### Changed Files
- `Assets/_Project/UI/MailboxPopupController.cs`
- `Assets/_Project/UI/Styles/MailboxPopup.uss`

## [Unreleased] - 2026-06-25 (Piggy bank icon cleanup)

### Changed
- Piggy Bank popup tabs, balance pill, package cards, deposit button, and countdown title no longer use emoji icons.
- Piggy Bank active/history pig markers now use `Assets/Sprites/icon/BoSungIcon/Piggy.png`.
- Removed unsupported `z-index` properties from `PiggyBankPopup.uss` to avoid Unity UI Toolkit warnings.

### Changed Files
- `Assets/_Project/UI/PiggyBankPopup.uxml`
- `Assets/_Project/UI/PiggyBankPopupController.cs`
- `Assets/_Project/UI/Styles/PiggyBankPopup.uss`

## [Unreleased] - 2026-06-25 (Event popup icon cleanup)

### Changed
- Removed the decorative gift icon from the Event & Gifts popup title.
- Removed the clock emoji from the event timer pill.
- Event popup tabs now show plain text labels without emoji icons.
- Event bundle cards no longer render package emoji icons; they keep tag, name, description, price, and purchase state only.
- Attendance reward slots now render image icons from `Assets/Sprites/icon` where available, with item database icons for crop rewards.
- Lucky wheel prize slots now render image icons from `Assets/Sprites/icon` and `ItemDatabase` instead of emoji glyphs; the wheel title, hub, and spin button no longer use emoji text.
- Removed unsupported `z-index` properties from `EventPopup.uss` to avoid Unity UI Toolkit warnings.

### Changed Files
- `Assets/_Project/UI/EventPopup.uxml`
- `Assets/_Project/UI/EventPopupController.cs`
- `Assets/_Project/UI/Styles/EventPopup.uss`

## [Unreleased] - 2026-06-25 (Leaderboard tab icons)

### Changed
- Leaderboard category tabs now use graphic icons from `Assets/Sprites/icon/BoSungIcon/` for EXP, Level, Fashion, Pet, and Rich.
- `Level` uses the dedicated `lv.png` icon.
- Leaderboard rank 1/2/3 badges now use `HuyChuongVang.png`, `HuyChuongBac.png`, and `HuyChuongDong.png` instead of emoji medals.
- Leaderboard value column now shows plain outfit counts for Fashion, plain pet counts for Pet, and numeric Gold values without the diamond glyph.

### Changed Files
- `Assets/_Project/UI/LeaderboardPopupController.cs`
- `Assets/_Project/UI/Styles/LeaderboardPopup.uss`

## [Unreleased] - 2026-06-25 (Inventory item icon rendering)

### Fixed
- `InventoryPopupController` now renders `ItemDefinition.iconTexture/iconSprite` for inventory grid cards and the detail panel, matching shop icon behavior.
- Items without assigned art still fall back to the old emoji/text icon.

### Changed Files
- `Assets/_Project/UI/InventoryPopupController.cs`
- `Assets/_Project/UI/Styles/InventoryPopup.uss`

## [Unreleased] - 2026-06-25 (HUD POS/UPOS pill)

### Added
- HUD top-right now shows an `UPOS` pill alongside `POS`.
- `EconomyManager` now exposes `OnUPOSChanged`, `AddUPOS`, and `SpendUPOS` so the premium balance can update live too.

## [Unreleased] - 2026-06-25 (APK build-mode touch + safe glyph hotfix)

### Fixed
- `BuildModeOverlayController` không còn phụ thuộc `Mouse.current`/`Keyboard.current` để đặt công trình; Android tap giờ đi qua `Touchscreen.current`, ép ghost raycast ngay tại điểm tap trước khi pin vị trí.
- `GhostPlacementController` đọc cả touch lẫn mouse để ghost cập nhật trên APK, vẫn giữ mouse cho Editor/Windows.
- Thay glyph điều khiển dễ lỗi font Android (`✕`, `✔`, `⌂`) trong các nút close/build placement bằng ASCII an toàn (`X`, `OK`, `B`) để tránh nút hiện thành ô vuông trên điện thoại.

### Changed Files
- `Assets/_Project/Scripts/Environment/GhostPlacementController.cs`
- `Assets/_Project/UI/BuildModeOverlayController.cs`
- `Assets/_Project/UI/BuildModeOverlay.uxml`
- `Assets/_Project/UI/*.uxml` (các nút close chuyển `✕` -> `X`)

## [Unreleased] - 2026-06-24 (UI quit/build-mode interaction/loadout polish)

### Added
- Thêm nút đóng app trực tiếp trên màn Login (`BtnQuitApp`) để bản Windows không bắt người test phải bấm phím Windows rồi đóng cửa sổ.

### Changed
- Tài khoản rich demo dùng `GiveTestLoadout()` được tăng mạnh tài nguyên: POS 500.000, vật liệu xây dựng 1000 mỗi loại, thức ăn/sản phẩm 500, hạt 300, vật phẩm tiêu hao 300, nước tưới 500.
- Nút thoát ở màn Login đổi từ dấu `X` nhỏ sang `✕ Thoát game` dạng pill để tester hiểu đây là nút đóng app thật.

### Fixed
- `ConfirmDialogController` tự nâng `UIDocument.sortingOrder` + sibling khi mở, tránh popup xác nhận bị Settings/popup khác đè lên.
- `FarmInteractionController` chặn toàn bộ tương tác thế giới khi Build Mode đang mở, tránh click/raycast xuyên vào chuồng có động vật trong lúc xây dựng.
- `ShopPopupController` sửa luồng mua vật nuôi: mua thú giờ thêm con giống vào túi đồ và trừ POS ngay, không còn gọi hệ chuồng cũ `AnimalPen` nên không bị chặn bởi lỗi "Chuồng đã đầy" khi chỉ muốn mua thêm thú.

### Changed Files
- `Assets/_Project/UI/ConfirmDialogController.cs`
- `Assets/_Project/UI/LoginScreen.uxml`
- `Assets/_Project/UI/Styles/LoginScreen.uss`
- `Assets/_Project/UI/LoginScreenController.cs`
- `Assets/_Project/UI/ShopPopupController.cs`
- `Assets/_Project/Scripts/Environment/FarmInteractionController.cs`
- `Assets/_Project/Scripts/Managers/InventoryManager.cs`

## [Unreleased] - 2026-06-24 (Product/shop icon pass)

### Added
- Gắn icon mới từ `Assets/Sprites/icon/SanPham/` cho 34 item có tên ảnh khớp rõ: sản phẩm cây lâu năm, đồ ăn/cá, sản phẩm vật nuôi, phân bón/thuốc/mồi/vé/quà.
- Cập nhật `ItemDataGenerator.AssignIconTextures()` để nếu chạy lại mock data thì các icon mới vẫn được gán lại đúng đường dẫn.
- Thay icon Sprint trên HUD mobile bằng `Assets/Sprites/icon/PNG/sprintIcon.png` cho cả nút Sprint và hint auto-sprint.

### Fixed
- Shop mở bằng nút HUD/legacy mock giờ cũng tra `ItemDatabase` cho giá mua/bán/tên/icon, tránh lệch số liệu so với NPC shop data-driven.

### Notes
- Không gán thịt gia cầm (`chicken_meat_01`, `duck_meat_01`, `goose_meat_01`, `ostrich_meat_01`) vì gameplay hiện đang chốt gia cầm chỉ lấy trứng.

### Changed Files
- `Assets/_Project/Scripts/Editor/ItemDataGenerator.cs`
- `Assets/Resources/Items/*.asset` (34 item có icon mới)
- `Assets/Sprites/icon/SanPham/**`

## [Unreleased] - 2026-06-24 (SuaLai4VatNuoi - 4 animal product price fix)

### Changed
- Áp file khách mới `Assets/_Project/Docs_KichBan/SuaLai4VatNuoi.xlsx/.md` cho 4 vật nuôi: Hươu, Dê, Ngỗng, Thỏ.
- Sửa giá bán Product 1 theo công thức `Tổng Product 1 = Tổng chu kỳ thu hoạch * Số lượng Pro1`:
  - `deer_velvet_01`: 24735 -> 12368
  - `goat_milk_01`: 24 -> 12
  - `goose_egg_01`: 28 -> 14
  - `rabbit_fur_01`: 172 -> 21
- Giữ nguyên quyết định cũ: gia cầm chỉ lấy trứng, không bật thịt ngỗng.

### Changed Files
- `Assets/_Project/Scripts/Editor/ItemDataGenerator.cs`
- `Assets/Resources/Items/{deer_velvet_01,goat_milk_01,goose_egg_01,rabbit_fur_01}.asset`
- `Assets/_Project/Docs_KichBan/SuaLai4VatNuoi.md`

## [Unreleased] - 2026-06-24 (Backend demo accounts + interaction hotfix)

### Added
- **Backend demo tối thiểu đã nối vào client:** thêm `Assets/Resources/BackendConfig.asset` trỏ `https://ywonder.net/game-api`; màn Login/Register gọi thật `auth/login` và `auth/register`.
- **5 tài khoản test giàu cho khách:** `DemoRich01` -> `DemoRich05` (mật khẩu trùng username) có `tutorialCompleted=true`, level 25, skip tutorial và seed loadout test khi vào gameplay.
- **Tài khoản mới sạch:** giữ `DemoNew01` để test tutorial/profile mới.

### Changed
- `AuthService` lưu và expose `Username` backend cùng token/userId để `GameManager` biết account thật đang đăng nhập.
- `LoginScreenController` preload `PlayerProfileService.LoadProfileAsync()` ngay sau login thành công để tránh bấm skip cutscene quá nhanh làm tutorial bật nhầm.
- `GameManager` ưu tiên signed-in backend account thay vì lấy tên nhân vật làm username; demo rich accounts được cấp lại `GiveTestLoadout()` sau khi clear app data.

### Fixed
- **Tương tác tâm ngắm gần vật thể:** `FarmInteractionController` dùng trigger raycast, ưu tiên animal/water, guard khoảng cách theo điểm hit/closest point và mặt phẳng XZ để giảm lỗi đứng gần nhưng không hiện/click được UI.
- **Chuồng đã có thú:** khi ngắm vào rào/chuồng có `AnimalObject`, popup ưu tiên action của thú thay vì chỉ hiện "Thả thú/Hủy chuồng".
- **Cho ăn:** popup animal cho phép bấm cho ăn khi thú đang Healthy hoặc Hungry, không chỉ lúc đã Hungry.

### Changed Files
- `Assets/_Project/UI/LoginScreenController.cs`
- `Assets/_Project/Scripts/Backend/AuthService.cs`
- `Assets/_Project/Scripts/Managers/GameManager.cs`
- `Assets/_Project/Scripts/Environment/FarmInteractionController.cs`
- `Assets/_Project/UI/AnimalInteractionPopupController.cs`
- `Assets/Resources/BackendConfig.asset`

## [Unreleased] - 2026-06-24 (Điều khiển mobile cuối phiên: Sprint, auto-run, touch feel)

### Changed

- **Điều khiển mobile tổng thể:** chỉnh feel joystick/camera để gần trải nghiệm game góc nhìn thứ 3.
  - `Sprint` giữ (`PointerDown`) giữ trạng thái chạy nhanh ổn định.
  - `Sprint` bấm 1 lần toggle đúng trạng thái.
  - Camera giật/nhấp khi điều khiển cảm ứng nhẹ hơn: touch smoothing riêng.
  - Tập trung hướng di chuyển theo joystick (đứng yên vẫn có hướng để tránh “xoay lệch”), giới hạn góc nhìn để phù hợp kiểm duyệt.

- **Điều khiển mobile (Sprint + auto-run nhất quán):**
  - Sửa luồng auto-run sát mục tiêu PUBG/FreeFire theo phản hồi.
  - Camera đổi hướng khi đang auto-run không tự hủy sprint; đổi hướng joystick mới break sprint theo đúng mong đợi.
  - Tạo trạng thái sprint thống nhất trong `PlayerController` (`IsSprintActive()`), gộp nguồn: sprint button, joystick hold, auto-run, shift (PC).
  - `Sprint` và `Jump` hoạt động ổn khi chờ animation/build mode.

- **Hệ thống Build/chuồng/nhặt thú (đang chốt):**
  - Cập nhật luồng đặt nhà/cối chuồng với preview ghost trực quan hơn, tự bù pivot prefab, và gắn logic chạy gõ búa khi đặt.
  - Chuồng: kiểm tra kích thước chuồng theo lưới khi thả thú, có rào gắn kết 1x1.

### Changed Files
- `Assets/_Project/Scripts/Player/PlayerController.cs` (gộp sprint-state, chạy nhanh/đổi hướng, đi lùi, auto-run)
- `Assets/_Project/Scripts/Camera/ThirdPersonCamera.cs` (touch smoothing + giới hạn pitch)
- `Assets/_Project/UI/GameHUDController.cs` (điều phối sprint UI/mobile)
- `Assets/_Project/UI/GameHUD.uxml` + `Assets/_Project/UI/Styles/GameHUD.uss` (nút sprint/touch feel, layout)

## [Unreleased] - 2026-06-23 (PHIÊN 5: Cây/Thú CHẾT thật + PERSISTENCE real-time (build mode + chuồng) + Rà soát kinh tế thú)

### Added
- **CÂY CHẾT THIẾU NƯỚC (khách chốt — mô hình "thanh máu"):** `CropDefinition` thêm `noWaterDeathSec` + `wateredLifeSec`. Cây ngắn ngày: gieo KHÔNG tưới → chết sau **8h**; tưới → đầy **20h**, cạn là chết; tưới lại reset. `FarmTile` đếm theo thanh máu, nhãn hiện đếm-ngược-héo. Bỏ qua chết trong Tutorial.
- **THÚ CHẾT ĐÓI (khách chốt — "thanh máu"):** `AnimalDefinition` thêm `noFeedDeathSec` + `fedLifeSec`. Đa số thú: chưa cho ăn **24h** chết / cho ăn **48h**; **rùa 5/10 ngày**; vịt cảnh báo đói từ 12h (thanh<50%). **Tách "bệnh" khỏi "đói"** (Sick để dành Gói B). Chết = **biến mất + trả ô chuồng** (hết "thú chết kẹt ô"). Thanh đói cập nhật **mỗi frame** (`RefreshHungerFill`).
- **PERSISTENCE REAL-TIME (cây + công trình + thú):** đổi `FarmTile`/`FarmAnimal` sang **wall-clock (Unix giây, `RealNow`)** → lớn-bù/đói-bù/chết-bù đúng qua đóng-mở app.
  - **`PlacedBuilding.cs` [NEW]** + `GhostPlacementController.PlaceFromSave()`: đánh dấu + dựng lại công trình build mode (Ruộng/Chuồng/Đường), KHÔNG trừ vật liệu.
  - **`BuildPersistence.cs` [NEW]:** lưu/khôi phục công trình + **cây trên ruộng** + **con vật trong chuồng** theo vị trí `BuildSurfaceCell` (khoá `YW_BuildState`). Lưu lúc đóng app + khi đặt công trình. Tự gắn qua GhostPlacementController.
  - Stage 1 (`FarmManager`): viết lại save/load dùng **timestamp thật** thay `growthPercent` (cũ load lại reset cây về 0%). Thêm công tắc **`autoSpawnTiles` (mặc định TẮT)**.

### Changed
- **Cây ngắn ngày: TẤT CẢ chín 24h** (`Days(1)` — BA xác nhận). Trước: dưa hấu/bí ngô 48h, rau muống/cỏ voi 12h → về hết 24h.
- **Tutorial tua nhanh 24s** (trước 5s) — `FarmTile.GetGrowthTime` + `TutorialManager:448`. Ngoài tutorial dùng `growthTimeSec` thật (24h tính TỪ LẦN TƯỚI ĐẦU; lớn theo thời gian thật, KHÔNG còn "gate nước").
- **Nhãn nổi trên cây nhỏ lại** (characterSize 0.085→0.035 + tự co vừa thanh nước) — hết chèn UI khác.
- **Generator:** `CropDataGenerator` set 8h/20h; `ItemDataGenerator.SetAnimalGameplay` thêm noFeed/fedLife (24h/48h · rùa 5/10 ngày).

### Docs / Audit
- **RÀ SOÁT KINH TẾ THÚ** (`RaSoat_SoLieu_MauThuan.md`, mục 23/06): đối chiếu `VatNuoi2.xlsx` (cả tab "Thuyết minh cách tính" — đọc thẳng .xlsx vì .md thiếu) ↔ generator. **Giá mua/bán/sản lượng/số lần thu = KHỚP 100%.** Công thức `maxHarvests = floor(số ngày nuôi ÷ chu kỳ thu)` đúng. Gia cầm chỉ-trứng = **khách chốt, giữ nguyên**. **Chi phí bệnh (vắc-xin/thuốc) chưa áp (Gói B) → lời game > bảng tới khi làm bệnh.** Trứng vịt 4.5→làm tròn 5.
- Thêm ghi chú "cây ngắn ngày chín 24h" vào `CayTrong2.md`. Xoá `CayTrong.md` + `CayTrongLauNam.md` (cũ, đã có bản …2).

### Fixed
- **"Thú chết kẹt ô":** thú chết đói nay tự `ClearAnimal()` + Destroy → ô chuồng trống lại, thả con mới được.
- Cảnh báo `CS0414` field `growthAccrued` thừa (đã gỡ sau khi cây lớn theo thời gian thật).

### Editor TODO (đã làm phần lớn phiên này)
- ✅ Chạy lại generator (Crop + Animal) — số chết/24h đã bake.
- ✅ Tắt `Force Run Tutorial For Testing` (đang ép cây tua 5s mọi lúc).
- `BuildPersistence` tự gắn qua GhostPlacementController (hoặc gắn tay lên `[BuildPrefabLibrary]`). `FarmManager.autoSpawnTiles` để TẮT (mặc định) nếu không muốn 10 ô lưới.

### Còn nợ (phase sau — KHÁCH hẹn)
- **Gói B — hệ BỆNH thú** (vắc-xin phòng + thuốc trị + phát bệnh ngẫu nhiên theo tỉ lệ/thời điểm VatNuoi2). Xong → chi phí khớp bảng (~250-400%).
- Persistence offline **server-time** (chỉnh-giờ-máy hiện tua được). Cây giàn nhiều ô (chanh dây) chưa persist. Phân bón. Chốt EXP "lần cuối" vs ngày×10.

### Changed Files (PHIÊN 5)
- `Scripts/Data/{CropDefinition,AnimalDefinition}.cs` · `Scripts/Editor/{CropDataGenerator,ItemDataGenerator}.cs` [MODIFIED]
- `Scripts/Environment/{FarmTile,FarmAnimal,GhostPlacementController,TilePlacementSystem}.cs` · `Scripts/Managers/FarmManager.cs` · `Scripts/Tutorial/TutorialManager.cs` [MODIFIED]
- `Scripts/Environment/{PlacedBuilding,BuildPersistence}.cs` [NEW]
- `Docs_KichBan/{RaSoat_SoLieu_MauThuan,CayTrong2}.md` [MODIFIED] · xoá `{CayTrong,CayTrongLauNam}.md`

## [Unreleased] - 2026-06-23 (PHIÊN 4: Áp giá Point ×26 · Cây lâu năm thu nhiều lần + số ô · Vòng quay + điểm danh 15 ngày · EXP/Level · Mobile UI · FIX GameManager bị xoá)

### Added
- **Vòng quay may mắn** (tab mới trong EventPopup): vòng tròn quay THẬT (emoji quanh vành + kim + ô giữa hiện quà), 12 ô weighted random, **3 lượt/ngày** (reset ngày thật, PlayerPrefs), **TRAO THƯỞNG THẬT** (AddItem/AddPOS). Tỉ lệ khách chốt + con/cây khác lấp + "chúc may mắn" 60%.
- **Điểm danh TÂN THỦ 15 ngày** (viết lại tab Attendance, dựng 15 ô động): N1=26pt · N3=4 gỗ · N5=26pt · N7=10 bắp · N10=10 bí ngô · N11=8 gỗ · N15=1 thỏ. Trao THẬT, 1 lần/ngày thật.
- **Cây LÂU NĂM thu NHIỀU LẦN** (Gói A): `CropDefinition` thêm `plotSlots/maxHarvests/reHarvestCycleSec/finalProduct*`; `FarmTile` vòng đời chín→thu→ra quả lại→VỤ CUỐI (Sa Chi 9 lần/Sầu Riêng 12/chanh dây 2). `SetPerennial` trong CropDataGenerator.
- **Cây NHIỀU Ô (giàn)**: chanh dây = 20 ô — master-slave (`PlantWithSlots`: trồng 1 ô master + chiếm 19 ô slave gần nhất, thả khi thu vụ cuối).
- **Nhãn chữ NỔI trên cây** (TextMesh billboard): % lớn / chín sau ~Xs / số lần thu — đếm ngược sống (bù info cây còn thiếu so với con vật).
- **Chanh leo (chanh dây)**: thêm crop data (lâu năm, hạt 1560, bán 57).
- **Safe Area TOÀN CỤC**: `SafeAreaInstaller` tự gắn `UISafeArea` cho MỌI UIDocument; `ToiUu_DaManHinh_Checklist.md`. 3 file `.md` so sánh số liệu mới (CayTrong2/CayTrongLauNam2/VatNuoi2).

### Changed
- **ÁP BỘ GIÁ MỚI (file …2.xlsx khách gửi):** MỌI giá = **Point = USDT × 26**; giá BÁN hạ để giữ lời ~250%. 54 mục trong `ItemDataGenerator`. Con giống: Bò 7800/Hươu 10400/Gà 156… Giá bán: sữa bò **50**, da heo 7042, mai rùa 11893, Hộp Sa Chi 194, chanh leo 57… (1 USDT = 26 Point.)
- **Gia cầm BỎ THỊT** (gà/vịt/ngỗng/đà điểu): `meatItemId` rỗng → chỉ lấy trứng, hết số lần thu thì biến mất; gỡ 4 thịt gia cầm khỏi Mini Garden.
- **EXP/Level (khách chốt):** ramp **250 + (cấp−1)×5**, Level Cap **90**; EXP thu hoạch = **số ngày × 10** (cây/thú); đào khoáng **15**.
- **Mobile UI:** nút búa Build chuyển sang PHẢI (trên Jump); phóng to nút điều khiển ~**+27%** (joystick/sidebar/settings/bag/sprint/jump/X); Sprint **giữ→bấm-toggle**; PanelSettings ref 1280×720; popup clamp max-w/h %.
- **Vắc-xin 30 / thuốc 70** (giá mua).

### Fixed
- **🔴 GAME MANAGER BỊ XOÁ khi Play (làm KẸT cả game, không spawn nhân vật/cutscene):** gốc rễ = `InventoryManager` (+ Economy/Tool) do `SystemsBootstrapper` tạo TRƯỚC (BeforeSceneLoad) → bản trùng gắn trên `_GameManager` gọi **`Destroy(gameObject)` → huỷ NHẦM cả GameManager**. **SỬA: 3 manager bootstrap đổi `Destroy(gameObject)` → `Destroy(this)`** (chỉ huỷ component trùng). Thêm GameManager singleton `!= this` + `ResetStaticInstance` (chống lỗi domain-reload-off). KHÔNG phải lỗi domain reload như nghi ban đầu.
- **Settings bị thanh chat đè:** Settings `sortingOrder=100` + đăng ký `UIPopupTracker`; ChatPanel tự ẩn khi có popup.
- **iPhone lẹm nút mép phải** (hud-root `inset:0` + Safe Area); nút búa joystick đè (chuyển phải); vòng quay title đè nội dung (layout cột + selector 2-class).

### Editor TODO
- Chạy lại generator: Generate Mock Items → Crop Data → Shop Data. Kéo model chanh leo vào `Crop_passion_fruit_seed_01`.
- **⚠️ TẮT `giveTestLoadoutOnStart = false` TRƯỚC KHI BUILD** (đang BẬT để test). Cân nhắc gỡ component InventoryManager thừa khỏi `_GameManager`.

### Còn nợ (phase tiếp)
- **Gói B — hệ BỆNH vật nuôi** (tỉ lệ/thời điểm phát bệnh, vắc-xin PHÒNG, thuốc TRỊ trừ item, chết theo mốc loài). `AnimalDefinition` chưa có field bệnh.
- Phân bón; chốt EXP "lần cuối doc" (số to) vs ngày×10.

## [Unreleased] - 2026-06-22 (PHIÊN 3: Toast · EXP/Audio · Mobile #4/#5 · Resume · QC audit · Áp giá khách chốt · Tối ưu APK · Hỏi khách)

### Added
- **Toast thông báo MỌI hành động:** thu hoạch cây/thú · chặt cây/đào đá · mua/bán shop · câu cá. `ScreenToast.ShowInfo` (xanh) thành công, `Show` (đỏ) lỗi.
- **Hệ EXP/Level TỐI GIẢN:** `ExperienceManager.cs` (singleton tự tạo, lưu PlayerPrefs, ngưỡng 100+(lv-1)*50, bắn `LevelUpOverlay`). HUD hiện Level + % EXP THẬT (hết "Level 1/0.00" cứng). Cộng EXP khi thu hoạch cây (`crop.expReward`) + chặt/đào (`resourceExp`=5).
- **Âm thanh KHUNG:** `AudioManager.cs` (tải `Resources/Audio/<tên>`, thiếu file bỏ qua êm). Nối nhạc nền `bgm` + SFX `chop`/`harvest`/`coin`. **CẦN thả file audio vào `Assets/Resources/Audio/`.**
- **Mobile #4 — camera kéo 1 ngón:** `LookZone` (nửa phải hud-root) → `GameHUDController` bắt pointer → `ThirdPersonCamera.AddTouchLook` (sensitivity touch riêng + `Instance`). PC khóa con trỏ ở tâm nên không đụng.
- **Mobile #5 — safe area:** `UISafeArea.cs` (đệm root UIDocument theo `Screen.safeArea`). Match PanelSettings đã = 0.5.
- **Resume người chơi cũ:** `GameManager` có save (`YW_HasSave`) → BỎ Login+Cutscene vào thẳng game ở vị trí cũ; lưu vị trí lúc Quit/Pause (chỉ khi ở Nông trại); cờ `alwaysStartFresh` + ContextMenu "Clear Save".
- **Câu cá BẢN TẠM:** ẩn popup, bấm F → đợi `castDuration` (8.7s) → tự +1 cá + toast (giữ code minigame căn-giờ để sửa sau).
- **Editor tool `TextureSizeReducer.cs`:** menu ép texture Max 512 + ASTC (giảm dung lượng build).
- **Map: khóa đảo Mỏ** (`LockMine` icon + `IsUnlocked("mine")=false` cứng, scene chưa có).

### Changed
- **Sản lượng tài nguyên (khách chốt):** chặt 1 cây = **10 Gỗ** · đào 1 đá = **10 Đá** (SerializeField `treeYield`/`rockYield`); `HarvestResourceTick` bù `yieldItemId` mặc định (`wood_01`/`stone_01`) nếu trống → đồ vào túi đúng + toast ra số.
- **Giá MUA con giống = cột USDT** (khách chốt): bò 300, gà 6, đà điểu 170, hươu 400, heo 100, dê 50, rùa 90, ngỗng 10, thỏ 5, vịt 8 + **thêm 3 con** vịt/ngỗng/rùa vào shop.
- **Nông sản ngắn ngày = THỨC ĂN, KHÔNG bán** (khách chốt): 8 món `sellPrice 0 + canSell false` → shop tự lọc.
- **Thông báo map khóa** → "Chưa đủ điều kiện để di chuyển" (MapPopup dialog + IslandTravel toast).
- **Câu cá:** xóa "Nhận +X POS" trong mô tả cá (câu chỉ cho CÁ; bán shop mới ra tiền).
- **`giveTestLoadoutOnStart = false`** (TẮT cho bản build demo).
- **Animation câu cá** 8.5→8.7s khớp cửa sổ căn cá.

### Fixed
- **Tưới nước SPAM:** `HandleWater` kiểm `PlayerController.IsBusy` → click khi đang múa tưới bị bỏ qua (hết tốn nước thừa + nhảy tiến độ).
- **Toast chặt/đào không hiện:** đổi từ event tĩnh `OnResourceHarvested` (dùng chung Tutorial — 1 handler throw chặn cả chuỗi) → gọi TRỰC TIẾP tại call-site.
- **Tutorial CHỐNG KẸT:** `CheckFollowAutoAdvance` tự nhảy bước "đi theo NPC" sau 90s (NPC kẹt NavMesh).
- **Ẩn nút CHEAT Level/VIP** trong Map popup (cờ `showCheatButtons=false`).
- **Warning `line-height`** trong GameHUD.uss (UI Toolkit không hỗ trợ — gỡ).
- **Lỗi biên dịch chặn cả assembly:** 2 thư mục lạ `UI-Nhien`/`BinMin-Dev` (trùng `AuthManager` của project BulletHell) — đã xóa.

### Optimization (mobile) — Editor/asset
- GPU Instancing material cube/cây/đá · Static `map1`/`stonemap` + nhà city · Far Clip ~150-200 + Fog · khóa Landscape.
- **APK 1.2GB → texture chiếm 96%** (Build Report: 4 con NPC `.glb` 256MB/con do texture nhúng quá to). Xử: **ép texture NPC 2048→512 + ASTC**.

### Decisions (khách/sếp chốt 22/06)
- **Giá MUA con giống = cột USDT** (đã áp).
- **Rau ngắn ngày = thức ăn chăn nuôi, KHÔNG bán** (đã áp).
- **Cây lâu năm = CHỈ 3 loại** (Sa Chi, Sầu Riêng, Chanh dây) — demo tạm giữ 10, dọn sau.
- **Giá BÁN sản phẩm + cân bằng lời = khách làm việc sau** (chưa chốt). ⚠️ Đính chính: "lời 300-500 lần/kinh tế thủng" là SAI (bỏ qua thức ăn + 9 tháng + giới hạn lần thu) — lời thật ~250-400%.

### Docs
- `RaSoat_SoLieu_MauThuan.md` (rà soát mâu thuẫn số liệu) · `PhieuHoi_Khach_DeHieu.md` (viết lại) + `PhieuHoi_Khach_GiaCa.docx` (gửi khách) · `ToiUu_Mobile_Checklist.md` · `TruocKhiBuild_Checklist.md`.

### Còn nợ (Phase 2 / chưa làm)
- Exploit kinh tế (phá chuồng dupe · 2 hệ chuồng AnimalPen-vs-BuildSurfaceCell · thú chết kẹt ô · đổi giờ máy reset lượt câu · POS lưu `(int)` tràn ~2,1 tỉ).
- Persistence DateTime offline (cây/thú lớn-bù) · thêm Chanh dây + dọn 7 cây lâu năm · Settings volume/graphics chưa áp · nhiều popup chỉ log không trao thưởng.

---

## [Unreleased] - 2026-06-21 (PHIÊN 2: Thời gian thực · Vật nuôi sống VatNuoi · Tưới-gate · Múc nước · Build vật liệu · Tutorial fix · Rà soát trước demo)
### Added
- **Vật nuôi SỐNG theo thời gian + thanh HP:** viết lại `FarmAnimal` — đói/ra sản phẩm tính theo MỐC THỜI GIAN (`Time.timeAsDouble`); thanh HP (no/đói) billboard trên đầu (tự đo cao theo model); `AnimalInteractionPopup` thêm dòng "Độ no / Thu hoạch" (đếm ngược vụ + X/Y lần). Gắn `FarmAnimal` cho cả con vật CÓ prefab (trước chỉ ra khối trơ).
- **Logic vật nuôi đầy đủ theo VatNuoi (10 con):** `produceItemId`/`produceAmount`/`maxHarvests` + **vụ cuối LÀM THỊT** (con biến mất, trả ô chuồng). Tạo 17 item sản phẩm/thịt (giá theo VatNuoi). `AnimalDefinition` thêm `meatItemId`/`meatAmount`.
- **Thanh NƯỚC cho cây + TƯỚI-GATE-LỚN:** thanh nước billboard tụt theo `waterIntervalSec`; cây **CHỈ lớn khi còn nước** (hết nước → ngừng lớn tới khi tưới lại). `FarmTile.growthAccrued`+`GetGrownSeconds`.
- **Hệ thời gian thực `GameTimeConfig.cs`:** 1 ngày game = 24h thực; hằng số `SecondsPerGameDay` (DEMO 60f · THẬT 86400f) + `Days()`/`Hours()` — đổi 1 chỗ là cả cây+thú đổi. Generator khai theo ngày/giờ game.
- **Khung 10 CÂY LÂU NĂM:** 10 hạt + 10 sản phẩm + 10 CropDefinition (tạm 1-lần-thu; thu-nhiều-lần Phase 2).
- **MÚC NƯỚC tưới cây:** `WaterSource.cs` (vùng ao) + item `watering_water_01`. Ngắm ao bấm/click "Múc nước" → +5 xô; tưới TỐN 1 xô.
- **Build cost = VẬT LIỆU (không POS):** Ruộng miễn phí · Đường đá 4 Đá · Chuồng 4 Gỗ/ô rào. Kiểm+trừ lúc đặt, hoàn đúng vật liệu khi phá. Chi phí ra **SerializeField** (`penWoodCost`/`pathStoneCost`). `BuildSurfaceCell` lưu `BuildMaterialId`.
- **Popup "Tính năng đang phát triển":** `ShopZoneTrigger.comingSoon` → NPC chưa làm (VIP/Maid/Pet/Gift...) hiện toast thay vì lỗi.
- **Tách tab túi đồ:** sản phẩm (trứng/sữa/thịt + SP cây) tách khỏi tab "Thú nuôi" → tab **"Sản phẩm"** (category `products`). Shop **ẩn tab filter** không có hàng.
- **Wire SHOP đầy đủ:** Farm Shop 18 hạt + 10 con giống; Mini Garden mua đủ nông sản + SP cây lâu năm + 20 SP/thịt vật nuôi.

### Changed
- **Cho ăn ĐÚNG tài liệu:** validate thức ăn theo `AnimalDefinition.foodMain/foodAlt` (so theo TÊN qua ItemDatabase) + trừ đúng số lượng; sai loại → báo, không nhận. Sửa tên item (`Cỏ khô`→**Cỏ Voi**, `Rau cải`→**Bắp cải**), chuyển 7 nông sản sang category `food` (hiện ở tab cho ăn).
- **Câu cá + đào đá CITY-ONLY:** gate bằng `IslandTravelManager.CurrentIslandId == "city"` (FarmInteractionController). Farm: ẩn nút + chặn; chặt cây vẫn mọi đảo.
- **Tutorial bỏ bước đào đá:** sau Chặt cây → thẳng bãi ruộng; đánh số lại /13.
- **Cây mọc TỪ DƯỚI LÊN:** `FarmTile.AnchorBaseToGround` neo đáy cây xuống đất (hết phình 2 đầu).
- **Khoá map khác:** chỉ Nông trại + Thành phố; Mỏ/Hải Phú/Mộc Nhi → toast "đang khoá".
- **Respawn ra SerializeField** (`HarvestableResource.respawnTimeSec`, default 60s demo). Cây + đá dùng chung.
- **Loadout test:** thêm 18 hạt + sản phẩm/thịt + 30 nước tưới; **bật mặc định** (`giveTestLoadoutOnStart=true` — NHỚ tắt khi build).

### Fixed
- **Tutorial 2 bug:** chặt/đào nhảy bước ngay (loadout đã có gỗ/đá → bỏ auto-skip); thả thú không cập nhật nhiệm vụ (nghe `FarmAnimal.OnAnimalSpawned` flow mới thay `AnimalPenSpawner` cũ).
- **Popup vật nuôi tràn chữ; warning 'Cám' & id rỗng; lỗi compile** (`ShopItem` struct so null, enum `ResourceType.Rock` không phải Stone, biến thừa `currentBalance`/`demolishRefundRate`).
- **Cây model méo/dẹp:** do ô Dirt scale lệch + model xoay Blender. Thử "holder" → DẸP hơn → ĐÃ REVERT (giữ bù `cropParentLossy` cũ — đang đúng). Kết luận: chỉnh hình qua model/wrap empty, KHÔNG đụng code scale.

### Decisions (khách chốt 21/06)
- **1 ngày game = 24h thực** (cây/thú lớn theo ngày thật). Demo tạm 60s/ngày qua `GameTimeConfig`.
- **Xây ruộng MIỄN PHÍ; xây chuồng tốn GỖ** (4/ô rào). Build dùng vật liệu, không tiền.
- **Câu cá + đào đá chỉ ở Thành phố** (đã code gate).

## [Unreleased] - 2026-06-21 (NPC Shop data-driven · Mốc thời gian cây · Economy số khách · Mobile chạm + giữ-chặt · Bơi/leo bờ · Chuẩn bị demo thứ 2)
### Added
- **Hệ NPC Shop data-driven — mở khi CHẠM NHÀ:** `ShopDefinition` (ScriptableObject, mỗi shop 1 asset chỉ lưu ID, giá tra `ItemDatabase`), `ShopZoneTrigger` (gắn nhà NPC, bước vào vùng → popup tự mở; **giữ mở, đóng bằng X**; tên shop nổi trên đầu NPC qua `nameTagTarget`; cũng route được Workshop/PiggyBank), `ShopPopupController.Show(ShopDefinition)` + lọc thu mua theo whitelist, `Editor/ShopDataGenerator.cs` (menu sinh 7 asset: Hạt giống&Vật nuôi / Vật phẩm / Siêu thị Cá / Mini Garden / Hai Lúa / Verdant / Thú Y). `MerchantNPC` thêm field `shopData`. Thiết kế: `Docs_KichBan/ThietKe_NPCShop.md`.
- **Hủy chuồng → hoàn tài nguyên:** ngắm ô rào → nút **"Hủy chuồng"** (phím G / tap) → trả con giống về túi → phá cả cụm rào (flood-fill) → hoàn **50% giá build** (`demolishRefundRate`). `BuildSurfaceCell` lưu `BuildCost` + `AnimalObject`/`AnimalItemId`.
- **Bơi: bật khỏi mặt nước + nhiều khối nước:** nhảy khi đang bơi → vọt lên **trèo lên bờ** (`swimLeapTimer`); đếm `waterVolumeCount` cho phép ghép **NHIỀU Box Collider tag "Water"** ướm hồ hình dạng lạ (rời 1 box không tụt bơi); `ResetSwimState` khi teleport.
- **Tối ưu render khi đổi đảo:** `IslandTravelManager.farmOnlyObjects` — vật thể chỉ-Nông-trại (biển 10000, địa hình) **tự ẩn** khi sang đảo khác (hết ngập + đỡ render), bật lại khi về.
- **Lộ trình + tài liệu demo:** `Docs_KichBan/LoTrinh_Demo_Thu2.md` (mục tiêu, P0/P1/P2, cắt, mobile 3 mức, phân vai). Convert 3 file Excel khách → `.md`: `CayTrong` (cây lâu năm), `CayTrongLauNam` (8 cây ngắn ngày), `CachTinh` (công thức thú).

### Changed
- **Cây lớn theo MỐC THỜI GIAN** (`FarmTile`): bỏ cộng dồn `Time.deltaTime` mỗi frame → lưu `growStartTime`, tính % từ thời gian THẬT đã trôi → đi đảo khác / tắt ô đất cho nhẹ máy thì về vẫn **lớn bù** đúng. Nền cho real-time + offline.
- **Economy số thật của khách:** giá hạt + giá bán nông sản (`ItemDataGenerator`), sản lượng + EXP 8 cây (`CropDataGenerator`) theo `CayTrongLauNam`; 10 vật nuôi đã khớp `VatNuoi`. `Shop_FishShop` đổi tên "Siêu thị Cá", thêm `Shop_ThuY`.
- **Mobile #3 — chạm tương tác:** `FarmInteractionController` đổi `Mouse.current` → `Pointer.current` (chung Mouse PC + Touchscreen) → chạm tay chặt/trồng/mua chạy.
- **Chặt cây GIỮ-ĐỂ-CHẶT:** nút "Chặt cây" trên HUD thành **giữ-liên-tục** (`InteractionAction.onHoldStart/onHoldEnd` + `HoldChopResource`) thay vì tap 1 lần (hết cảnh "thả mới hiện ~50%"); túi mặc định + loadout test thêm **rìu/cúp/cần câu**.
- **Build Mode bỏ HẲN xoay:** gỡ nút Xoay + phím R + menu ngữ cảnh Xoay; cụm đặt còn **Tích / X**.
- **Cap FPS 60 + tắt vsync** (`SystemsBootstrapper`) — cheap mobile win.

### Fixed
- **Cây bị bóp dẹp:** ô đất Dirt scale (0.15,1,0.15) bóp cây con → `FarmTile` bù `cropParentLossy` → cây đúng tỉ lệ 3D.
- **Chặt cây để lại lá:** `HarvestableResource.AttachNearbyLeaves` so khớp tên **KHÔNG phân biệt hoa/thường** ("leaf" khớp "Leaf") → tự gom lá rời vào cây → ẩn/đổ theo cây.
- **Nút đặt Build "đứng lì":** `Show/HidePlacementControls` set thẳng inline `display` (trước đó inline của `UpdatePlacementControlsPosition` đè class `.hidden`) → xây/hủy xong nút Tích biến mất; nút X tắt được.
- **Tiêu đề shop bị ghi đè:** mở qua `ShopDefinition` giữ TÊN RIÊNG shop trên popup (ApplyAccessMode không còn đè thành "CỬA HÀNG").
- **Ngập nước khi tới Thành phố:** biển 10000 trong scene nền phủ city ở X=1000 → ẩn biển qua `farmOnlyObjects` khi rời nông trại.

### Decisions (khách chốt)
- **Câu cá & đào đá CHỈ ở đảo Thành phố** (không ở đảo khởi đầu) → cần sửa tutorial (bỏ bước đào đá ở nông trại). Fish Shop đảo bỏ, chỉ còn Siêu thị Cá thành phố.
- **Demo thứ 2 = Nông trại + Thành phố, offline, model tạm.** CẮT: online/API/web, tối ưu sâu, 4 NPC feature (KNX/Game/Maid/Gift), bạn bè/chat, cosmetic/VIP/Pet, mỏ/đảo endgame.

### Changed Files (21/06)
- `Assets/_Project/Scripts/Data/ShopDefinition.cs` [NEW], `Scripts/Environment/ShopZoneTrigger.cs` [NEW], `Scripts/Editor/ShopDataGenerator.cs` [NEW]
- `Scripts/Editor/CropDataGenerator.cs`, `Scripts/Editor/ItemDataGenerator.cs` [MODIFIED]
- `UI/ShopPopupController.cs`, `UI/GameHUDController.cs`, `UI/BuildModeOverlayController.cs`, `UI/BuildModeOverlay.uxml` [MODIFIED]
- `Scripts/Environment/{FarmInteractionController,FarmTile,HarvestableResource,BuildSurfaceCell,GhostPlacementController,MerchantNPC}.cs` [MODIFIED]
- `Scripts/Player/PlayerController.cs`, `Scripts/Managers/{IslandTravelManager,InventoryManager}.cs`, `Scripts/Core/SystemsBootstrapper.cs`, `Scripts/Cutscenes/BoatCutscene.cs`, `Scripts/UI/FloatingNameTag.cs` [MODIFIED]
- Data: `Data/Shops/Shop_*.asset`, `Resources/Items/Crop_*.asset` (qua generator)
- Docs: `Docs_KichBan/{LoTrinh_Demo_Thu2,ThietKe_NPCShop,SpecShop_DraftMuaBan,CayTrong,CayTrongLauNam,CachTinh}.md`

## [Unreleased] - 2026-06-18 (Đồng bộ UI 3 màu chủ đạo, Tối ưu FloatingNameTag và Loading Screen)
### Added
- **Màu sắc thương hiệu UI:** Tích hợp 3 màu chủ đạo Cam (`#eb6b2a`), Xanh lá (`#7cb641`), Xanh biển (`#2596be`) vào các hệ thống UI chính.
- **Tính năng báo tên địa điểm:** `LoadingScreenController` giờ hỗ trợ tuỳ chọn tham số tên bản đồ đích, giúp hiển thị tên địa điểm thay vì logo khi nhảy scene.

### Changed
- **Nền NameTag nổi:** Bỏ thẻ `<mark>` (lỗi hiển thị ký tự thụt đuôi), thay bằng 3D Quad (`bgObj`). Tối ưu thuật toán co giãn bằng padding tĩnh giúp nền không bị thụt khi tên quá dài. Sửa lỗi màu tên hiển thị (trắng chuẩn).
- **Màn hình Đăng Nhập (`LoginScreen.uss`):** Nút đăng nhập/đăng ký màu Cam rực rỡ với viền hover Xanh lá. Tab đang chọn màu Xanh biển có viền Xanh lá bao quanh. Ô input focus hiện viền Xanh lá.
- **Màn hình Tạo Nhân Vật (`CharacterSelect.uss`):** Đồng bộ thiết kế màu sắc (Orange, Green, Blue) với Login (nút bấm, tiêu đề, các thẻ, popup cảnh báo). Gỡ lỗi CSS USS cảnh báo (z-index, line-height).
- **Màn hình Loading / Splash:** 
  - Gọn gàng hơn (xoá sao trang trí, version tag). 
  - Đổi chữ Logo từ "Y-WONDERLAND" thành "Y WONDER GREEN FARM".
  - Nền Splash tối lại sang `mystic-black`. Thanh tiến trình mang màu Xanh lá (`success`), màu chữ mang màu Cam.

### Changed Files (18/06)
- `Assets/_Project/Scripts/Managers/GameManager.cs` [MODIFIED]
- `Assets/_Project/Scripts/UI/FloatingNameTag.cs` [MODIFIED]
- `Assets/_Project/Scripts/UI/LoadingScreenController.cs` [MODIFIED]
- `Assets/_Project/UI/Styles/LoginScreen.uss` [MODIFIED]
- `Assets/_Project/UI/Styles/CharacterSelect.uss` [MODIFIED]
- `Assets/_Project/UI/SplashLoadingScreen.uxml/uss` [MODIFIED]
- `Assets/_Project/UI/LoadingScreen.uxml/uss` [MODIFIED]

## [Unreleased] - 2026-06-15 (Dụng cụ cầm tay · Cây đổ gục · Câu cá có dây · Animation lao động)
### Added
- **Hệ thống dụng cụ cầm tay (`EquipmentManager`):** tự sinh dụng cụ PLACEHOLDER bằng khối
  primitive (Rìu/Cúp/Cuốc/Bình tưới/Cần câu/Túi hạt/Nắm cám) gắn vào **xương bàn tay** (tự
  tìm qua Animator Humanoid) khi ô model còn trống; thêm `ToolType.Pickaxe` + `ToolType.AnimalFeed`;
  ô chỉnh `Tool Position/Rotation Offset` để canh tay; log chẩn đoán (tìm xương tay, ShowTool).
- **Dây câu + phao (`FishingLineController`) [NEW]:** dây câu = `LineRenderer` nối ngọn cần → phao;
  khi câu: phao **bay vòng cung ra mặt nước** (ném thẳng trước mặt, đúng cao độ mặt nước), nổi nhấp
  nhô, rồi **tự thu về ngọn cần** và ẩn. Timed sequence: `castDelay` (bung) → `reelDelay`/`reelDuration`
  (thu) — chỉnh khớp frame animation, khỏi cần Animation Event.
- **Cầu nối Animation Event (`AnimEventToolHider`) [NEW]:** gắn lên object có Animator. Hàm
  `HideHeldTool` (ẩn cây giống tại frame cắm xuống đất) + `CastFishingLine` (bung dây tại frame vung cần).
- **Model + prefab dụng cụ thật:** Rìu (Axe), Cúp (Pickaxe/BasicPickaxe), Cuốc (MetalHoe), Cần câu
  (fishingrod), + anim Watering/Plant. Gán vào các ô trong EquipmentManager.

### Changed
- **Cây ĐỔ GỤC xuống đất (`HarvestableResource`):** chặt/đập xong cây xoay quanh **ĐÁY cây
  (điểm chạm đất)** đổ nghiêng ~84° rồi mới ẩn — thay vì biến mất đột ngột / đổ lơ lửng.
- **Gán đúng dụng cụ theo hành động (`FarmInteractionController`):** chặt cây→Rìu, đập đá→Cúp,
  cuốc đất→Cuốc, tưới→Bình tưới, cho ăn→Nắm cám, câu→Cần câu, trồng→Cây giống. Đổi tên state
  animation lao động sang **`TreeCuttingV4`** (chặt + đào + cuốc + tưới dùng chung anim bổ xuống).
- **Câu cá:** `StartFishing` gọi `PrepareCast` (ném dây trước mặt ở cao độ FishingSpot);
  `FishingOverlayController.Hide` gọi `Reel` (thu dây khi thoát).

### Changed Files (15/06)
- `Assets/_Project/Scripts/Player/EquipmentManager.cs` [MODIFIED]
- `Assets/_Project/Scripts/Player/AnimEventToolHider.cs` [NEW]
- `Assets/_Project/Scripts/Environment/FishingLineController.cs` [NEW]
- `Assets/_Project/Scripts/Environment/HarvestableResource.cs` [MODIFIED]
- `Assets/_Project/Scripts/Environment/FarmInteractionController.cs` [MODIFIED]
- `Assets/_Project/UI/FishingOverlayController.cs` [MODIFIED]
- Prefab dụng cụ: `Axe`, `Pickaxe`, `MetalHoe`, `fishingrod`; anim `Watering`, `Plant`, `Planting`, `TreeCuttingV4`

## [Unreleased] - 2026-06-14 (Phiên chiều: NPC đa dịch vụ · UI polish · Golden Hour)
### Added
- **NPC đa dịch vụ (`MerchantNPC`):** 1 script dùng chung, enum `ServiceType {ShopBuy, ShopSell, Workshop, PiggyBank}` — click NPC mở đúng popup (Mua / Bán / Nâng cấp / Heo Đất). Tự gắn `FloatingNameTag` trên đầu (tên theo `npcName` hoặc mặc định theo dịch vụ), màu vàng gold phân biệt với Guide. Tạo prefab `NPCBuy/NPCSell/NPCWorkshop/NPCBank` + đặt vào FarmScene/CityScene.
- **Tách quầy Mua/Bán (`ShopPopupController`):** enum `ShopAccessMode {Both, BuyOnly, SellOnly}` + `Show(mode)` — NPC Mua chỉ hiện tab Mua, NPC Bán chỉ hiện tab Bán (ẩn hẳn khối "CHẾ ĐỘ"), tự đổi tiêu đề.
- **Ánh sáng xế chiều (Golden Hour):** Tạo Volume Profile `GoldenHour.asset` (URP) — WhiteBalance / ColorAdjustments / SplitToning / Tonemapping / Bloom / Vignette tông ấm, đã tinh chỉnh giảm chói (~70%: PostExposure -1, bloom gần tắt). Kèm hướng dẫn Sun (3400K góc thấp) + Ambient Gradient ấm + Fog cho FarmScene.

### Changed
- **Thanh tiến trình chặt/đập (`ResourceInteractionUI`):** Restyle Palia Cozy Dark + **ghim CỐ ĐỊNH dưới tâm ngắm** (bỏ bám theo vật thể → hết trôi/ghim mép màn hình khi cây cao).
- **Nối `UIPopupTracker` cho Workshop / PiggyBank / Fishing:** Show/Hide/OnDisable → **trả chuột ổn định** để bấm nút (trước đây thiếu → chuột kẹt / "lưỡng lự").
- **Ẩn nút Shop trên HUD** (shop giờ mở qua NPC Mua/Bán); nhãn hover NPC hiện theo loại dịch vụ.

### Fixed
- **Soft-lock đi đảo:** `IslandTravelManager.TravelToAsync` bọc `try/catch/finally` → LUÔN bật lại Player/CharacterController + ẩn loading + mở khoá `_isTraveling` dù scene load lỗi (hết kẹt đơ vĩnh viễn).
- **Cổng portal chết sau lần đầu:** `MapPortalTrigger` bỏ cờ `_playerInside` (teleport tắt CharacterController → `OnTriggerExit` không bắn → cờ kẹt true). Dùng `mapPopup.IsVisible()` chống mở lặp + sửa thứ tự check Gameplay.
- **Popup kẹt chuột khi đổi đảo:** 4 popup (Animal/Map/Shop/Inventory) thêm `OnDisable → UIPopupTracker.SetOpen(false)`.
- **Thanh chặt cây không hiện:** bar bị neo 4.5m vọt ngoài đỉnh màn hình → ghim dưới tâm ngắm + clamp trong khung nhìn + guard chia-0.
- **Chuẩn hoá `PlayerController.Instance`:** thay 6 chỗ `FindFirstObjectByType<PlayerController>` trong `FarmInteractionController` + null-guard (chống NRE khi player chưa spawn / đang teleport).
- **UI vặt:** Workshop tab lẹm trái (bỏ `scale` hover) + **ẩn thanh cuộn ngang toàn cục** (`DesignSystem.uss`, BuildMode tự bật lại); Heo Đất tràn số "12 ngày → 12 ngà" (nới thẻ 200→240px, chữ không co/cắt); Bạn bè nút X đè "Làm mới" (`padding-right` 60→96px).

### Changed Files (phiên chiều)
- `Assets/_Project/Scripts/Environment/MerchantNPC.cs` [MODIFIED]
- `Assets/_Project/Scripts/Environment/MapPortalTrigger.cs` [MODIFIED]
- `Assets/_Project/Scripts/Environment/FarmInteractionController.cs` [MODIFIED]
- `Assets/_Project/Scripts/Managers/IslandTravelManager.cs` [MODIFIED]
- `Assets/_Project/UI/ShopPopupController.cs`, `WorkshopPopupController.cs`, `PiggyBankPopupController.cs`, `FishingOverlayController.cs`, `MapPopupController.cs`, `AnimalInteractionPopupController.cs`, `InventoryPopupController.cs` [MODIFIED]
- `Assets/_Project/UI/ResourceInteractionUI.uxml` + `ResourceInteractionUIController.cs` [MODIFIED]
- `Assets/_Project/UI/Styles/{DesignSystem,WorkshopPopup,PiggyBankPopup,FriendsPopup,BuildModeOverlay}.uss`, `GameHUD.uxml`, `ShopPopup.uxml`, `WorkshopPopup.uxml` [MODIFIED]
- `Assets/_Project/Prefabs/NPC{Buy,Sell,Workshop,Bank}.prefab` [NEW]
- `Assets/_Project/Settings/GoldenHour.asset` [NEW]

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
