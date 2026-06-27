# Changelog — Y WONDER GREEN FARM
# Format: Theo module, có ngày và danh sách files thay đổi

> **⚠️ TRẠNG THÁI QC:** Tất cả các module UI hiện tại đều **CHƯA được QC review** bởi khách hàng.
> Nếu QC/khách hàng không duyệt → sẽ sửa lại theo feedback.

---
## [2026-06-27] — Fix bundle id cho CodeMagic iOS signing

### Fixed
- Thêm bước trong workflow CodeMagic Xcode-only để ép bundle id của Xcode project đã export sẵn về `com.ywonder.greenfarm` trước khi chạy `xcode-project use-profiles`.
- Mục tiêu là để CodeMagic match đúng provisioning profile App Store `ywonderland_greenfarm_appstore` mà bên build đã tạo cho bundle id `com.ywonder.greenfarm`.

### Changed Files
- `codemagic.yaml`

---
## [2026-06-27] — Dọn dữ liệu thịt gia cầm trong loadout demo

### Fixed
- Bỏ các item thịt gia cầm khỏi loadout rich/demo để tester không còn được cấp sẵn "Thịt gà", "Thịt vịt", "Thịt ngỗng", "Thịt đà điểu" rồi hiểu nhầm là hàng phải bán được.
- Đánh dấu các item thịt gia cầm là không bán được (`canSell=false`, `sellPrice=0`), khớp quyết định hiện tại: gia cầm chỉ lấy trứng.
- Xóa dữ liệu hiển thị sản phẩm phụ dạng thịt khỏi 4 definition gia cầm, vẫn giữ sản phẩm chính là trứng.

### Changed Files
- `Assets/_Project/Scripts/Managers/InventoryManager.cs`
- `Assets/_Project/Scripts/Editor/ItemDataGenerator.cs`
- `Assets/Resources/Items/Animal_chicken_01.asset`
- `Assets/Resources/Items/Animal_ostrich_01.asset`
- `Assets/Resources/Items/Animal_goose_01.asset`
- `Assets/Resources/Items/Animal_duck_01.asset`
- `Assets/Resources/Items/chicken_meat_01.asset`
- `Assets/Resources/Items/ostrich_meat_01.asset`
- `Assets/Resources/Items/goose_meat_01.asset`
- `Assets/Resources/Items/duck_meat_01.asset`

---
## [2026-06-26] — iOS CI, CodeMagic và icon game

### Added
- Thêm `README.md` ở root repo để người clone `main` nắm cách mở project, build, trạng thái backend và nhánh làm việc.
- Thêm `codemagic.yaml` ở root repo để CodeMagic nhận workflow build Unity iOS.
- Thêm workflow CodeMagic Xcode-only/TestFlight: build từ Xcode project iOS đã export sẵn, không cần activate Unity trên CodeMagic.
- Thêm Xcode project iOS đã export sẵn trong `ios/` để phục vụ workflow TestFlight không chạy Unity.
- Thêm rule Git LFS cho binary lớn của iOS export (`.a`, `.resS`, `usymtool`, `usymtoolarm64`).
- Thêm `Assets/_Project/Editor/BuildScript.cs` để CI có thể export Xcode project iOS từ Unity bằng batch mode.
- Thêm asset thumbnail/icon game trong `Assets/_Project/UI/Sprites/`.
- Gắn `ThumbnailGame.jpg` vào icon Standalone và Android adaptive icon trong `ProjectSettings/ProjectSettings.asset`.

### Fixed
- Sửa lỗi compile trong build script do namespace `YWonderLand.Environment` che mất `System.Environment`.
- Đổi API set bundle id iOS sang `NamedBuildTarget.iOS`, bỏ warning obsolete của Unity 6.

### Changed Files
- `README.md`
- `.gitattributes`
- `codemagic.yaml`
- `ios/**`
- `Assets/_Project/Editor/BuildScript.cs`
- `Assets/_Project/Editor/BuildScript.cs.meta`
- `Assets/_Project/UI/Sprites/ThumbnailGame.jpg`
- `Assets/_Project/UI/Sprites/ThumbnailGame.jpg.meta`
- `Assets/_Project/UI/Sprites/Black.jpg`
- `Assets/_Project/UI/Sprites/Black.jpg.meta`
- `ProjectSettings/ProjectSettings.asset`

---
## [2026-06-26] — Interaction, chuồng, câu cá và icon build/popup

### Added
- Câu cá chuyển sang luồng hành động có thời lượng 8.7s khớp clip `Fishing`, có UI hủy/progress và nhả chuột trong lúc hành động đang chạy.
- Kết thúc câu cá mới cộng cá vào túi: cá thường hoặc cá hiếm, tỉ lệ cá hiếm 20%.
- Popup chuồng hỗ trợ xem theo nhóm: click vào vùng chuồng liền kề sẽ hiện danh sách toàn bộ thú trong chuồng dưới dạng card, chọn card nào thì hành động đúng con đó.
- Card thú trong popup chuồng dùng icon thật từ `ItemDatabase`, không còn dùng chữ cái/emoji fallback nếu item đã có ảnh.
- Build Mode dùng icon công trình từ `Assets/Sprites/icon/BoSungIcon/` cho Ruộng, Đường đá và Chuồng.

### Changed
- Tầm tương tác câu cá được chỉnh để đứng trên bờ vẫn câu được, hiện giới hạn khoảng 5m.
- Khi đang câu cá, prompt `F Câu cá` được ẩn để không đè lên nút hủy/progress.
- Các hành động thế giới đang được chuẩn hóa theo hướng có thể hủy, chạy theo độ dài clip: chặt cây, đào khoáng, cuốc/trồng/tưới, cho ăn.
- Popup chuồng gom nút hành động vào một hàng; `Chữa bệnh` và `Vaccine` vẫn hiển thị nhưng bị khóa/mờ vì dữ liệu vaccine/bệnh chưa được khách chốt.
- Thanh đói/thanh nước trên world bar dùng material URP Unlit riêng để tránh lỗi màu tím trong build.

### Fixed
- Không còn hiện UI câu cá khi nhân vật đang bơi/dưới nước.
- Chặn lỗi `Collider.ClosestPoint` với collider không hỗ trợ/non-convex khi tính khoảng cách tương tác.
- Sửa các lỗi prompt/click của ô đất, chuồng, con vật sau các lần chỉnh raycast/tầm tương tác.
- Hủy chuồng liền kề giờ dọn sạch UI tương tác còn sót, xóa object thú trong chuồng, trả con giống về túi và hoàn vật liệu theo rule hoàn tài nguyên hiện có.
- Build Mode trên Android tiếp tục dùng touch/pointer để đặt công trình; các glyph dễ thành ô vuông được thay bằng chữ/icon an toàn hơn.

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

---
## [2026-06-25] — Existing character login flow

### Changed
- Thêm `characterCreated` vào `player_profile` ở Unity client và Node server stub.
- Login giờ nạp `/player/profile` trước; nếu `characterCreated=true` thì bỏ qua màn tạo nhân vật.
- `DemoRich01` đến `DemoRich05` được coi là tài khoản đã có nhân vật, nên tester đăng nhập là vào game, không phải chọn giới tính/đặt tên.
- Màn tạo nhân vật sẽ đánh dấu profile là đã tạo nhân vật khi người chơi xác nhận tên/giới tính.

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

---
## [2026-06-25] — Shop tab icon cleanup

### Changed
- Popup shop bỏ emoji icon trong các tab chế độ/danh mục: `Mua`, `Bán`, `Hạt giống`, `Vật nuôi`, `Dụng cụ`, `Vật phẩm`.
- Icon ảnh của hàng hóa trong card item và panel chi tiết vẫn giữ theo `ItemDefinition.iconTexture/iconSprite`.
- Tên shop dài được căn giữa trong vùng header còn trống, không còn tràn xuống dưới pill POS hoặc nút đóng.
- Bỏ thuộc tính `z-index` không được UI Toolkit hỗ trợ trong `ShopPopup.uss`.

### Changed Files
- `Assets/_Project/UI/ShopPopup.uxml`
- `Assets/_Project/UI/Styles/ShopPopup.uss`

---
## [2026-06-25] — Workshop icon rendering

### Changed
- Popup Tiệm rèn hiển thị icon dụng cụ và nguyên liệu nâng cấp bằng ảnh asset thay vì emoji label.
- Gắn `iconTexture` cho nhóm dụng cụ/vật liệu: rìu, cuốc, cần câu, xô tưới, cuốc chim, gỗ, đá, sắt, quặng.
- Bỏ thuộc tính `z-index` không được UI Toolkit hỗ trợ trong `WorkshopPopup.uss`.

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

---
## [2026-06-25] — Quest popup icon cleanup

### Changed
- Popup Nhiệm vụ bỏ emoji kiếm/quà/check trong danh sách nhiệm vụ; nhiệm vụ đang làm/đợi nhận dùng icon ảnh từ `Assets/Sprites/icon/`.
- Nhiệm vụ đã nhận thưởng hiển thị dấu tích visual trong ô vuông, đồng bộ cách nhìn với Hộp thư.
- Ô phần thưởng nhiệm vụ dùng icon từ `ItemDatabase` hoặc `Assets/Sprites/icon/BoSungIcon/`, không còn render emoji cũ.
- Bỏ các thuộc tính `z-index` không được UI Toolkit hỗ trợ trong `QuestPopup.uss` để tránh warning.

### Changed Files
- `Assets/_Project/UI/QuestPopup.uxml`
- `Assets/_Project/UI/QuestPopupController.cs`
- `Assets/_Project/UI/Styles/QuestPopup.uss`

---
## [2026-06-25] — Mailbox read/reward icons

### Changed
- Hộp thư: ô trạng thái thư đã đọc hiển thị dấu tích vẽ bằng UI thay vì emoji/glyph dễ thành ô vuông.
- Badge quà trong danh sách thư dùng icon mới `Assets/Sprites/icon/SanPham/VatPham/giftbox.png`; quà đã nhận hiển thị dấu tích.
- Phần thưởng đính kèm trong chi tiết thư dùng icon ảnh từ `ItemDatabase` hoặc `Assets/Sprites/icon/BoSungIcon/`, không còn render emoji cũ.

### Changed Files
- `Assets/_Project/UI/MailboxPopupController.cs`
- `Assets/_Project/UI/Styles/MailboxPopup.uss`

---
## [2026-06-25] — Piggy bank icon cleanup

### Changed
- Popup Heo đất bỏ emoji icon ở balance pill, tab, gói gửi, nút gửi và title thời gian còn lại.
- Icon heo ở trạng thái đang gửi/lịch sử gửi chuyển sang ảnh `Assets/Sprites/icon/BoSungIcon/Piggy.png`.
- Bỏ các thuộc tính `z-index` không được UI Toolkit hỗ trợ trong `PiggyBankPopup.uss` để tránh warning.

### Changed Files
- `Assets/_Project/UI/PiggyBankPopup.uxml`
- `Assets/_Project/UI/PiggyBankPopupController.cs`
- `Assets/_Project/UI/Styles/PiggyBankPopup.uss`

---
## [2026-06-25] — Event popup icon cleanup

### Changed
- Popup Sự kiện & Quà tặng bỏ icon trang trí ở tiêu đề.
- Pill thời gian sự kiện bỏ emoji đồng hồ, chỉ giữ chữ thời gian.
- Các tab trong popup Sự kiện giờ chỉ hiển thị chữ, không còn emoji icon.
- Các card gói ưu đãi không còn render emoji icon của gói; vẫn giữ tag, tên gói, mô tả, giá và trạng thái mua/hết hàng.
- Bảng điểm danh hiển thị icon ảnh từ `Assets/Sprites/icon` khi có, riêng quà cây trồng dùng icon đang gắn trong `ItemDatabase`.
- Vòng quay may mắn hiển thị icon ảnh cho các phần thưởng từ `Assets/Sprites/icon` và `ItemDatabase`; tiêu đề, hub giữa vòng, nút QUAY bỏ emoji text.
- Bỏ các thuộc tính `z-index` không được UI Toolkit hỗ trợ trong `EventPopup.uss` để tránh warning.

### Changed Files
- `Assets/_Project/UI/EventPopup.uxml`
- `Assets/_Project/UI/EventPopupController.cs`
- `Assets/_Project/UI/Styles/EventPopup.uss`

---
## [2026-06-25] — Leaderboard tab icons

### Changed
- Popup Leaderboard đổi 5 tab `EXP`, `Level`, `Fashion`, `Pet`, `Rich` sang icon ảnh từ `Assets/Sprites/icon/BoSungIcon/`.
- Tab `Level` dùng icon riêng `lv.png`.
- Hạng 1/2/3 trong bảng Leaderboard dùng icon huy chương vàng/bạc/đồng thật thay cho emoji.
- Cột giá trị Leaderboard: Fashion hiện số bộ trang phục, Pet hiện số lượng pet, Rich bỏ glyph kim cương và chỉ còn số Gold.

### Changed Files
- `Assets/_Project/UI/LeaderboardPopupController.cs`
- `Assets/_Project/UI/Styles/LeaderboardPopup.uss`

---
## [2026-06-25] — Inventory item icon rendering

### Fixed
- Kho đồ giờ hiển thị `ItemDefinition.iconTexture/iconSprite` cho card vật phẩm và panel chi tiết, đồng bộ với cách cửa hàng đang hiển thị icon.
- Vật phẩm chưa có ảnh được gán vẫn fallback về emoji/text như cũ.

### Changed Files
- `Assets/_Project/UI/InventoryPopupController.cs`
- `Assets/_Project/UI/Styles/InventoryPopup.uss`

---
## [2026-06-25] — HUD POS/UPOS pill

### Added
- Thêm pill `UPOS` ở HUD top-right để hiển thị premium currency song song với `POS`.
- `EconomyManager` có event `OnUPOSChanged` và helper `AddUPOS`/`SpendUPOS` để số dư premium cập nhật live.

---
## [2026-06-25] — APK build-mode touch + safe glyph hotfix

### Fixed
- `BuildModeOverlayController` không còn phụ thuộc `Mouse.current`/`Keyboard.current` để đặt công trình; Android tap giờ đi qua `Touchscreen.current`, ép ghost raycast ngay tại điểm tap trước khi pin vị trí.
- `GhostPlacementController` đọc cả touch lẫn mouse để ghost cập nhật trên APK, vẫn giữ mouse cho Editor/Windows.
- Thay glyph điều khiển dễ lỗi font Android (`✕`, `✔`, `⌂`) trong các nút close/build placement bằng ASCII an toàn (`X`, `OK`, `B`) để tránh nút hiện thành ô vuông trên điện thoại.

### Changed Files
- `Assets/_Project/Scripts/Environment/GhostPlacementController.cs`
- `Assets/_Project/UI/BuildModeOverlayController.cs`
- `Assets/_Project/UI/BuildModeOverlay.uxml`
- `Assets/_Project/UI/*.uxml` (các nút close chuyển `✕` -> `X`)

---
## [2026-06-24] — Tối ưu cảm ứng Mobile + Sprint theo hướng PUBG/FreeFire

### Changed
- **Chốt trạng thái demo build:** giữ `GameTimeConfig.SecondsPerGameDay = 60f` để cây/thú chạy nhanh cho APK/Windows test chéo; không đổi về 24h thật trước demo.
- **Ghi expected timing cho tester:** cây ngắn ngày ~60s sau tưới; tutorial 24s; Sa Chi/Sầu Riêng ~28 phút; Chanh dây ~90 phút; thú nhanh gồm vịt 60s, gà 120s, dê/ngỗng 180s, đà điểu 360s, bò 420s.
- **NPC tutorial marker:** thay dấu chấm than primitive bằng prefab `Assets/_Project/Prefabs/ExclamationMark.prefab`, vẫn giữ fallback primitive nếu scene chưa gán prefab.
- **VPS/backend:** xác nhận client có khung REST nhưng mới phủ `auth/profile/tutorialCompleted`; deploy VPS chỉ đủ cho online tối thiểu nếu chạy `server/` stub + cấu hình `BackendConfig.baseUrl`. Backend thật cho POS/inventory/farm/cây/thú/server-time/IAP là phase riêng sau demo.
- **Điều khiển mobile:** hoàn thiện luồng `Sprint` hold + tap, auto-run không bị auto-dừng khi xoay camera; đổi hướng bằng joystick mới break sprint.
- **Camera cảm ứng:** smoothing riêng cho touch, khóa góc nhìn phù hợp kiểm duyệt; giảm tối thiểu bắn ngang.
- **Đi lùi / đổi hướng:** sửa lại hành vi khi kéo joystick lùi để nhân vật quay đầu trước rồi chạy theo hướng mới.
- **Build/chuồng:** trạng thái gõ búa + preview ghost build tiếp tục cập nhật theo hướng trực quan.
- **Popup/flow học chơi:** NPC tutorial giữ nhịp chậm hơn, không spam thoại liên tiếp.

### Changed Files
- `Assets/_Project/Scripts/Player/PlayerController.cs`
- `Assets/_Project/Scripts/Camera/ThirdPersonCamera.cs`
- `Assets/_Project/UI/GameHUD.uxml`
- `Assets/_Project/UI/GameHUDController.cs`
- `Assets/_Project/UI/Styles/GameHUD.uss`
- `Assets/_Project/Scripts/Environment/GhostPlacementController.cs`
- `Assets/_Project/Scripts/Environment/FenceAutoConnect.cs`

## [2026-06-20] — Điều khiển mobile + Build snap theo ô đất thật + Hệ chuồng từ hàng rào + Thông tin con vật

### Added
- **Điều khiển cảm ứng (GameHUD):** joystick ảo điều khiển di chuyển (`PlayerController.SetMoveInput`); nút **Sprint giữ-để-chạy** (fix `Clickable` nuốt event bằng `TrickleDown`); nút **Jump** (`TriggerJump`); nút **X hủy hoạt ảnh** (`CancelAction`, tự hiện khi `IsBusy`).
- **Build snap theo Ô ĐẤT THẬT:** `BuildSurfaceCell` (gắn lên khối cube map, cube=0.8) thay lưới ảo lệch; ghost ướm vào tâm mặt trên khối. Tool Editor **"sơn vùng"** (`BuildSurfaceCellSetup`): kéo BoxCollider trùm vùng → gắn hàng loạt khối `cube*` trong vùng (+ collider). Gizmo hiện trạng ô.
- **Hệ chuồng từ hàng rào (task #6):** rào = hộp vuông trên 1 ô → **ô có rào = ô chuồng**. `PenEnclosure.FindPen` BFS cụm ô-rào liền nhau (nhiều rào kề = chuồng to). Ngắm/click ô rào → "Thả thú" → chọn loài → validate `penSlots` vs số ô còn trống → thả (`SetAnimal`) hoặc `ScreenToast` báo lỗi. `AnimalPrefabLibrary` (itemId→prefab thú + spawnHeightOffset). Click thẳng (PC) + bấm chữ (mobile) đều chạy.
- **Popup Thông tin con vật:** hiện giá mua / số ô chuồng / thức ăn chính-phụ / sản phẩm. Thêm trường vào `AnimalDefinition`; điền data 10 con qua generator (nguồn: bảng VatNuoi khách). Restyle popup theo Cozy Dark Palia.
- **Thông tin con vật ở Shop + Túi đồ:** chèn "Thông tin nuôi" (giá/ô/thức ăn) vào mô tả khi chọn con vật. `AnimalManager.LookupDefinition` (tra Instance, fallback Resources → chạy kể cả khi scene chưa gắn AnimalManager).
- **Mặt đường đá (paving):** item "Đường đá" trong Build Mode → map `BuildPrefabLibrary` (StoneSlab).
- **Loadout test:** `InventoryManager.GiveTestLoadout()` + cờ `giveTestLoadoutOnStart` — nạp nông sản/sản phẩm/vật liệu/hạt + tiền để test NPC mua/bán.

### Changed
- **Tương tác ngắm theo điểm chạm/tâm** cho ổn định; **nút gợi ý "Chặt cây"... bấm/tap được** (fix picking-mode cha Ignore); tia ngắm **xuyên qua hàng rào + ô bị chiếm** để tới ô chuồng.
- **Bỏ tính năng Vuốt ve** (Pet) khỏi tương tác con vật.
- **Dọn menu Build còn 3 mục** (Ruộng / Đường đá / Chuồng); ẩn 4 tab cũ.
- **Fix ghost Build luôn báo đỏ:** `GhostPlacementController` đổi `Physics.Raycast` → `RaycastAll` + tìm `BuildSurfaceCell` gần nhất (bỏ qua collider nền/mesh đảo chắn trước).
- **Lập task Hệ NPC** (theo kịch bản "10+ NPC"): shop keeper đa-NPC, Maid, Pet, NPC mỏ, NPC câu cá, AI chat (xem task.md).

### Changed Files
- `Scripts/Player/PlayerController.cs`, `UI/GameHUD.uxml`, `UI/GameHUDController.cs`, `UI/Styles/GameHUD.uss`
- `Scripts/Environment/{BuildSurfaceCell,PenEnclosure,AnimalPrefabLibrary,ScreenToast}.cs` [NEW], `GhostPlacementController.cs`, `FarmInteractionController.cs`, `PetInteraction.cs`
- `Scripts/Editor/BuildSurfaceCellSetup.cs` [NEW], `Scripts/Editor/ItemDataGenerator.cs`, `Scripts/Data/AnimalDefinition.cs`
- `UI/AnimalInteractionPopup.uxml`, `UI/AnimalInteractionPopupController.cs`, `UI/Styles/AnimalInteractionPopup.uss` [NEW]
- `UI/ShopPopupController.cs`, `UI/InventoryPopupController.cs`, `UI/BuildModeOverlayController.cs`, `UI/BuildModeOverlay.uxml`
- `Scripts/Managers/{AnimalManager,InventoryManager}.cs`, `task.md`

---
## [2026-06-19] — Build Mode trực quan: ghost mờ kiểu ROK + bù pivot + hàng rào tự nối

### Changed
- **Bỏ lưới hiển thị** trong Build Mode theo yêu cầu khách (giữ logic snap ô, chỉ tắt phần vẽ lưới).
- **Ghost preview = bản MỜ của chính prefab** (kiểu ROK/Hay Day): chọn item thấy luôn hình công trình mờ **xanh lá** (đặt được) / **đỏ** (không), theo chuột + snap lưới + xoay. Đặt = clone y hệt ghost (WYSIWYG). Item chưa khai báo prefab vẫn fallback khối Cube.
- Stretch prefab (đất/hàng rào) lên **đúng 1 ô (100%)** để đặt liền kề khít sát.

### Added
- **Tự bù pivot lệch** (`MakeCenteredClone`): bọc prefab vào wrapper căn tâm cụm mesh → model artist export pivot lệch (vd Fence lệch 88m) vẫn hiện/đặt đúng ngay vị trí nhắm, xoay quanh tâm. Không cần sửa prefab.
- **`FenceAutoConnect`**: hàng rào kề nhau (trực giao) tự **tắt cạnh tiếp giáp** ở cả hai → nối liền thành vùng quây (Minecraft-style), không cần nhiều prefab biến thể. Chọn cạnh tắt theo **vị trí thực** (không phụ thuộc tên), refresh sau 1 frame để vị trí ổn định.

### Fixed
- Ghost prefab "tàng hình"/đặt văng xa do pivot model lệch tâm → đã bù tự động.
- Hàng rào tắt cạnh lung tung khi đặt nhiều ô → do tính tâm khi vị trí chưa ổn định + đo bounds khi cạnh đã tắt; đã chụp tâm 1 lần lúc đủ cạnh + delay 1 frame.

### Changed Files
- `Scripts/Environment/GhostPlacementController.cs` *(refactor ghost = prefab mờ + bù pivot)*, `FenceAutoConnect.cs` [NEW]
- `UI/BuildModeOverlayController.cs` (tắt lưới)

---
## [2026-06-18] — Tutorial mới (NPC ông lão khó tính) + Cho ăn qua túi + sửa thuyền cutscene

### Added
- **Viết lại TutorialManager theo flow mới** (Giai đoạn 1 — đảo nông trại): Lên đảo (chào) → tới Cây → **chặt cây** → tới Mỏ → **đào khoáng** → tới Bãi ruộng → **xây ruộng** (Build) → cuốc → trồng → tưới → thu hoạch → tới Bãi chuồng → **xây chuồng** → **thả thú** → **cho ăn** → hoàn thành. (Bỏ bán chợ/workshop khỏi flow tân thủ; câu cá + sang đảo = Giai đoạn 2.)
- **NPC dẫn 4 trạm** (Cây/Mỏ/Bãi ruộng/Bãi chuồng) — kéo Empty waypoint vào Inspector. Tự bắt ô đất người chơi vừa xây để theo dõi cuốc/trồng/tưới/thu hoạch.
- **Giọng NPC ông lão ~70 tuổi khó tính** (xưng tôi–cậu): câu chào, giao việc, giục khi afk, càu nhàu. Thoại NPC cập nhật theo từng bước (hết lặp câu cũ).
- **2 hook mới cho tutorial**: `AnimalPenSpawner.OnAnimalPlaced` (thả thú), `FarmAnimal.OnAnimalFed` (cho ăn).
- **Công tắc `Force Run Tutorial For Testing`**: ép chạy lại tutorial dù hồ sơ đã hoàn thành (tiện dev; nhớ tắt khi release).
- **Cho ăn động vật qua túi đồ**: click thú đói → mở túi (tab Thực phẩm) → chọn **Bắp ngô** → animation Feed (cầm `oat` tượng trưng) → trừ đồ. Cả nút "Cho Ăn" trong popup Thú nuôi cũng đi cùng luồng này.
- Thêm vật phẩm **Đà điểu, Dê, Hươu, Thỏ** vào database; **Bắp ngô** chuyển category `items` → `food` (hiện ở túi để cho ăn).

### Fixed
- **Thuyền cutscene lật ngang khi cập bến**: tự suy "góc bù model" từ rotation đã căn sẵn → giữ tư thế đúng khi xoay, nhân vật không rơi nước.
- **Animation gõ búa khi xây**: gọi đúng state `Hammering2` (trước gọi sai tên "Hammering" → nhân vật cầm búa nhưng không gõ).
- **NPC lặp lại thoại cũ / câu thoại dồn dập / câu chào mất nhanh**: thoại chuyển bước hiện trễ 2.5s, câu chào kéo 9s.

### Changed Files
- `Scripts/Tutorial/TutorialManager.cs` *(viết lại)*, `Scripts/Environment/AnimalPenSpawner.cs`, `FarmAnimal.cs` [MODIFIED]
- `Scripts/Environment/FarmInteractionController.cs`, `UI/AnimalInteractionPopupController.cs`, `UI/BuildModeOverlayController.cs` [MODIFIED]
- `Scripts/Cutscenes/BoatCutscene.cs`, `Scripts/Editor/ItemDataGenerator.cs`, `Scripts/Managers/InventoryManager.cs` [MODIFIED]

---
## [2026-06-18] — Build Mode sinh prefab thật + Chuồng & thả thú từ túi đồ

### Added
- **`BuildPrefabLibrary`**: bảng ánh xạ "tên item Build → prefab THẬT" (kéo thả Inspector). Build Mode đặt ô đất/chuồng giờ sinh **prefab thật** (có FarmTile/collider…) thay khối Cube placeholder. Hỗ trợ `stretchToFootprint` (đất co vừa ô) + `yOffset` (chỉnh chìm/nổi) + khớp từ khóa **cụ thể nhất** (tránh nhầm chuồng).
- **Hệ chuồng trại**: thanh Build có **Chuồng nhỏ (1x1) / vừa (2x2) / lớn (3x2)**. Đặt chuồng chạy animation Hammering. Mỗi loại chỉ nuôi loài đúng cỡ.
- **`AnimalPenSpawner`**: click chuồng → mở **túi đồ (tab Thú nuôi)** chọn con vật → thả vào chuồng. Giới hạn loài theo `allowedAnimals` (map itemId→prefab) — không cho bò vào chuồng gà. `maxCapacity` (demo=1), `spawnHeightOffset` chỉnh độ cao con vật.
- **Tab "Thú nuôi"** trong túi đồ (UXML + controller, lọc category `animals`).
- **Vật phẩm con vật mới**: Đà điểu, Dê, Hươu, Thỏ (`ostrich_01/goat_01/deer_01/rabbit_01`) trong ItemDataGenerator; cấp sẵn để test (`EnsureStarterAnimals`).
- **Ruộng 1x1** trên thanh Build (đặt từng ô đất nhỏ).

### Changed
- **Nhân vật xoay thẳng về ô đất** khi cuốc/gieo/tưới/thu hoạch (`PlayerController.FaceTowards`) — hết lệch do camera lệch vai GTA.
- **Camera Build Mode** dốc hơn (góc ≥84°) để nhân vật ở giữa màn hình, không bị thanh chat che.

### Fixed
- **Prefab Build bị dựng đứng/lật**: giữ rotation gốc prefab (Blender xoay) + chỉ thêm yaw, không ghi đè.
- **Thuyền cutscene lật ngang khi cập bến** (`BoatCutscene`): tự suy "góc bù model" từ rotation đã căn sẵn (`autoOffsetFromInitialRotation`) → thuyền giữ tư thế đúng khi xoay, nhân vật không rơi nước.

### Changed Files
- `Scripts/Environment/BuildPrefabLibrary.cs`, `AnimalPenSpawner.cs` [NEW]
- `Scripts/Environment/GhostPlacementController.cs`, `FarmInteractionController.cs` [MODIFIED]
- `Scripts/Player/PlayerController.cs`, `Scripts/Camera/BuildCameraController.cs`, `Scripts/Cutscenes/BoatCutscene.cs` [MODIFIED]
- `UI/BuildModeOverlayController.cs`, `UI/InventoryPopup.uxml`, `UI/InventoryPopupController.cs` [MODIFIED]
- `Scripts/Editor/ItemDataGenerator.cs`, `Scripts/Managers/InventoryManager.cs` [MODIFIED]

---
## [2026-06-17] — Thiết kế lại UI Onboarding + Gameplay tile/búa/camera

> ⚠️ Phần **UI Onboarding** bên dưới có **sự hỗ trợ của Gemini Pro 3.1** (làm khi phiên Claude đạt giới hạn). Toàn bộ thay đổi phiên này **CHƯA commit** tại thời điểm ghi.

### Changed — UI Onboarding (Gemini Pro hỗ trợ)
- **Màn Login redesign**: bỏ chữ "Y WONDER GREEN FARM" + "CUỘC PHIÊU LƯU BẮT ĐẦU", thay bằng **logo Ywonder Hub** (`Y_Wonder_Hub_Logo2.png`, đã tách nền). Thêm **validate ≤ 20 ký tự** cho username/password ở cả form Đăng nhập lẫn Đăng ký (`max-length="20"` + check code).
- **Character Select redesign**: thay ký tự ♂/♀ bằng **avatar ảnh** (`Male_Avatar.jpg`, `Female_avatar2.jpg`); lưu `PlayerGender` (static) để đặt avatar mặc định; validate tên nhân vật **2–20 ký tự** (trước 2–16).
- **Đồng bộ 3 tông màu chủ đạo** (Cam `#eb6b2a` · Xanh lá `#7cb641` · Xanh biển `#2596be`) vào toàn bộ onboarding:
  - Login: nút Cam viền hover Xanh lá; ô input focus viền Xanh lá; tab active Xanh biển viền Xanh lá.
  - Character Select: nút/tiêu đề/thẻ giới tính/popup cảnh báo theo 3 tông; sửa USS (z-index, line-height).
- **`FloatingNameTag` nâng cấp**: xóa thẻ `<mark>` bị lỗi khoảng cách ký tự dài; thêm **3D Quad làm nền** căn giữa tự động; sửa công thức tính chiều rộng nền (dùng padding tĩnh thay vì ×0.8) → tên dài không tràn.
- **`GameManager`**: sửa lỗi đè màu tên nhân vật → khôi phục **màu trắng chuẩn** (thay vì vàng).
- **Splash & Loading**: gỡ trang trí dư (ngôi sao, version tag); nền Splash đổi **mystic-black**; thanh tiến trình **Xanh lá**, logo text **Cam**.
- **`LoadingScreenController`**: thêm tham số `destinationName` cho `ShowLoadingAsync` (hiện tên địa điểm đích khi chuyển scene).
- Tinh chỉnh kèm theo: `GameHUD`, `ProfilePopup`, `BuildModeOverlay`, `BuildCameraController`, `GhostPlacementController`, `MapPopupController`.

> ✅ **Chốt tên game chính thức = "Y WONDER GREEN FARM"** (18/06): tên loading mặc định Gemini đặt là đúng. Splash vẫn dùng **logo Ywonder Hub** (logo hình, không hiện chữ tên). Tài liệu kỹ thuật đã đồng bộ lại tên này (trước đó vài file ghi tạm "YWONDERLAND").

### Added — Gameplay (phiên Claude 16/06, gộp ghi ở đây)
- **`FarmTileMarker`**: ô vuông viền màu theo trạng thái đất (vàng=sẵn gieo, xanh=đang lớn, cam=chín), tự gắn vào mọi FarmTile.
- **Hammer Build (kiểu Minecraft)**: `TilePlacementSystem` + `HammerBuildController` — cầm búa gõ ô trước mặt để lát (tốn 4 đá + 4 gỗ), ô preview sáng/đỏ. *(Phím G tạm — Bước 2 sẽ thay nút HUD.)*
- **`NpcProximityInteract`**: bước vào vùng quanh NPC dịch vụ tự mở Shop/Workshop/Heo đất (không cần bấm).
- **Camera PUBG/Free Fire**: nhân vật luôn quay lưng về người chơi theo yaw camera; bỏ camera trôi; giảm độ nhạy (0.8/0.6); Free-Look giữ Alt.

### Changed Files (chính)
- UI: `LoginScreen.uxml/.uss`, `LoginScreenController.cs`, `CharacterSelect.uxml/.uss`, `CharacterSelectController.cs` [MODIFIED]
- Gameplay: `FarmTileMarker.cs`, `TilePlacementSystem.cs`, `HammerBuildController.cs`, `NpcProximityInteract.cs` [NEW]
- `ThirdPersonCamera.cs`, `PlayerController.cs`, `EquipmentManager.cs` [MODIFIED]

---
## [2026-06-16] — Rà soát điểm mù tài liệu + Bộ tài liệu kỹ thuật

### Added (tài liệu cho khách/BA)
- **`Docs_KichBan/DiemMu_CanXinKhach.md`** [NEW]: audit 3 lớp (kịch bản khách + docs nội bộ + code thật) → liệt kê toàn bộ điểm mù cần khách làm rõ, nhóm theo hệ thống, đánh dấu mức 🔴 chặn code / 🟡 chặn cân bằng / ⚪ chặn bàn giao. Kèm 4 mục GATING (backend, API web, định danh đăng nhập, publish Android) + bảng mâu thuẫn nội bộ.
- **`Docs_KichBan/TongKet_TaiLieu_CanCo.md`** [NEW]: tổng kết toàn bộ tài liệu dự án cần có, chia **Nhóm A (khách/BA cung cấp)** vs **Nhóm B (team tự viết)** vs **Nhóm C (viết lại)**; có câu nhắn mẫu gửi BA.

### Added (tài liệu kỹ thuật — team tự viết)
- **`docs/TECHNICAL_DESIGN.md`** [NEW] (TDD): kiến trúc backend REST offline-first, stack hiện tại vs cần chốt, luồng đăng nhập/tutorial đợt 1, lộ trình đợt 2–4, 8 rủi ro kỹ thuật đã biết.
- **`docs/DB_SCHEMA.md`** [NEW] (ERD): lược đồ DB thật theo REST — bảng `users`/`profiles` (đã có) + đề xuất economy/inventory/transactions/farm/animal/piggy_bank/quests + danh mục tĩnh (item/crop/animal/shop catalog).
- **`docs/SECURITY.md`** [NEW]: threat model, nguyên tắc **server-authoritative**, chống chỉnh giờ/double-spend/IAP giả, checklist anti-cheat đợt 2–3.
- **`docs/BUILD_RELEASE.md`** [NEW]: runbook build Android (keystore → AAB → Play Console), checklist phát hành, versioning.

### Changed (dọn mâu thuẫn UGS/Unity 2022 — Nhóm C)
- `docs/ARCHITECTURE.md`: **viết lại theo REST** — bỏ bảng "UGS Services" + sơ đồ "UGS Cloud", thay bằng Backend Services thật, cấu trúc thư mục `_Project/` đúng thực tế, trỏ sang TDD/DB_SCHEMA/SECURITY/BUILD.
- `docs/CONTEXT_RECOVERY.md`: prompt khởi động sửa **Unity 2022 + UGS + UniTask → Unity 6 + REST + Awaitable**; cập nhật danh sách file đọc.
- `docs/DATA_SCHEMA.md`: gắn banner **LỖI THỜI**, trỏ sang `DB_SCHEMA.md` (tránh implement nhầm theo UGS).

### Notes
- Phát hiện cần xử lý ở đợt 2–3 (ghi trong TDD/SECURITY): POS đang `int` sẽ tràn → đổi `long`; PiggyBank tách rời EconomyManager (gửi/rút chưa trừ tiền thật); lãi Heo Đất phải tính giờ server (chống chỉnh giờ máy).

### Changed Files
- `Assets/_Project/Docs_KichBan/DiemMu_CanXinKhach.md`, `TongKet_TaiLieu_CanCo.md` [NEW]
- `docs/TECHNICAL_DESIGN.md`, `docs/DB_SCHEMA.md`, `docs/SECURITY.md`, `docs/BUILD_RELEASE.md` [NEW]
- `docs/ARCHITECTURE.md`, `docs/CONTEXT_RECOVERY.md`, `docs/DATA_SCHEMA.md` [MODIFIED]

---
## [2026-06-16] — Lưu trữ THẬT (REST API) — Đợt 1: Profile + Tutorial

### Added
- **Backend REST đợt 1** (theo kịch bản khách, KHÔNG dùng UGS): chuyển từ mock/PlayerPrefs sang lưu thật cho `player_profile` + cờ `tutorialCompleted`.
- **Server stub** `server/` (Node/Express, lưu `data.json`): `/auth/register`, `/auth/login`, `GET|PUT /player/profile`. Token JWT đơn giản (chỉ dev/test, không production). Đã smoke-test end-to-end OK.
- **Client Unity** `Assets/_Project/Scripts/Backend/`: `BackendConfig` (ScriptableObject URL/timeout), `ApiClient` (UnityWebRequest + Newtonsoft, try/catch + timeout), `AuthService` (login/register, cache token), `PlayerProfileService` (load/save profile, **offline-first** fallback cache PlayerPrefs).

### Changed
- `SystemsBootstrapper`: khởi tạo thêm `AuthService` + `PlayerProfileService`.
- `GameManager.StartGame()` *(PROTECTED)*: đăng nhập + nạp profile chạy nền song song cutscene (không chặn UX, offline tự fallback).
- `TutorialManager` *(PROTECTED)*: bỏ qua tutorial nếu hồ sơ đã `tutorialCompleted`; khi hoàn thành thì ghi cờ lên hồ sơ thật.
- `docs/ARCHITECTURE.md` + `docs/API_CONTRACTS.md`: đính chính backend UGS → **REST API riêng**.

### Notes
- Auth đợt 1 dùng username = tên nhân vật + mật khẩu sinh/lưu local (CHƯA nối UI Login — để đợt 2).
- KHÔNG cài package Unity mới (dùng UnityWebRequest + Newtonsoft sẵn có).

### Changed Files
- `server/*` [NEW] · `Assets/_Project/Scripts/Backend/*.cs` [NEW]
- `Assets/_Project/Scripts/Core/SystemsBootstrapper.cs` [MODIFIED]
- `Assets/_Project/Scripts/Managers/GameManager.cs` [MODIFIED]
- `Assets/_Project/Scripts/Tutorial/TutorialManager.cs` [MODIFIED]
- `docs/ARCHITECTURE.md`, `docs/API_CONTRACTS.md` [MODIFIED]

---
## [2026-06-15] — Tưới cây (cầm xô), tự gom lá vào cây, tắt rung, dọn Splash

### Added
- **Hoạt ảnh tưới cây riêng**: động tác tưới gọi clip `Watering` riêng (tự đo độ dài clip), nhân vật cầm **bình tưới/xô** qua `EquipmentManager` (đúng pattern các nông cụ khác). Placeholder bình tưới được dựng lại có thân + quai xách + vòi + bông sen cho ra dáng.
- **Tự gắn lá rời vào cây lúc runtime** (`HarvestableResource`): cây tự tìm các object lá ở gần theo tên (`leafNameContains`) + bán kính (`leafAttachRadius`) rồi `SetParent` vào thân — khỏi phải parent tay từng cây. Lá tự ẩn + tự đổ theo cây khi bị chặt. Dùng cache tĩnh để chỉ quét scene 1 lần.

### Changed
- **Tắt hẳn rung lắc cây/đá** khi chặt/đập (trước giảm còn 10%, nay bỏ luôn) — cây/đá đứng yên, chỉ chạy thanh tiến trình.
- **Ẩn toàn bộ phần con (thân + lá)** khi cây bị chặt qua `SetVisualsActive`, thay cho việc chỉ ẩn con đầu tiên (tránh lá lơ lửng còn sót).
- **Màn Splash**: nền đổi sang **đen thuần** (hòa với nền logo JPG), **xóa tiêu đề "YWONDER GREEN FARM"** và **dòng kẻ vàng** trang trí; giữ logo YWonderHub + dòng "CUỘC PHIÊU LƯU BẮT ĐẦU" + thanh tải.

### Changed Files
- `Assets/_Project/Scripts/Environment/FarmInteractionController.cs` [MODIFIED]
- `Assets/_Project/Scripts/Environment/HarvestableResource.cs` [MODIFIED]
- `Assets/_Project/Scripts/Player/EquipmentManager.cs` [MODIFIED]
- `Assets/_Project/UI/SplashLoadingScreen.uxml` [MODIFIED]
- `Assets/_Project/UI/Styles/SplashLoadingScreen.uss` [MODIFIED]

---
## [2026-06-07] — Phase 5 & 6: Khai Thác Tài Nguyên và Câu Cá

### Added
- **Phase 5 (Khai Thác Tài Nguyên)**:
  - Hệ thống `HarvestableResource.cs` xử lý tương tác nhấn giữ (hold 3s) để chặt cây, đập đá. Rơi ra `wood_01` và `stone_01`.
  - `ResourceSpawner.cs` sinh ngẫu nhiên tài nguyên trên bản đồ, theo dõi đếm ngược thời gian hồi sinh (Respawn Timer) và lưu qua `PlayerPrefs`.
  - Giao diện thanh tiến trình lơ lửng (`ResourceInteractionUI.uxml` và Controller) theo dõi tiến độ nhấn giữ.
- **Phase 6 (Câu Cá - Đấu nối lõi)**:
  - Tích hợp `FishingOverlayController.cs` với `InventoryManager`. Đọc số lượng `bait_01` thật từ túi đồ.
  - Vượt qua QTE thành công, cá (`fish_01`, `fish_02`, `gift_box_01`) tự động thêm vào túi đồ.
  - Hệ thống 10 lượt câu miễn phí mỗi ngày, lưu và reset bằng `PlayerPrefs` theo ngày thực.
- **Item Database**: Thêm `pickaxe_01`, `fish_01`, `fish_02`, `gift_box_01` vào `ItemDataGenerator.cs`.

### Fixed
- **Obsolete API Cleanup (bởi Unity Assistant)**: Thay `enableWordWrapping` bằng `textWrappingMode = TextWrappingModes.NoWrap` trong `FloatingNameTag.cs` và `FishingSpot.cs`.
- **Code Cleanup**: Dọn dẹp biến thừa `premiumBait` trong `FishingOverlayController.cs`.

### Changed Files
- `Assets/Scripts/Environment/HarvestableResource.cs` [NEW]
- `Assets/Scripts/Managers/ResourceSpawner.cs` [NEW]
- `Assets/UI/ResourceInteractionUI.uxml` [NEW]
- `Assets/UI/ResourceInteractionUIController.cs` [NEW]
- `Assets/Scripts/Environment/FarmInteractionController.cs` [MODIFIED]
- `Assets/Scripts/Editor/ItemDataGenerator.cs` [MODIFIED]
- `Assets/UI/FishingOverlayController.cs` [MODIFIED]
- `Assets/Scripts/UI/FloatingNameTag.cs` [MODIFIED]
- `Assets/Scripts/Environment/FishingSpot.cs` [MODIFIED]

---
## [2026-06-06] — Part B (Fix Phase): Fishing 3D & Build Mode Redesign

### Added
- **Fishing Spot 3D Interaction** (`FishingSpot.cs`): Chuyển từ bấm nút UI sang trigger 3D. Lại gần hiện TextMeshPro nổi "Nhấp F để Câu", bấm F mới mở UI câu cá. Tránh đụng phím E của sự kiện.
- **Contextual Build Mode UX** (`BuildModeOverlayController.cs`):
  - Ghim (Pin) vị trí nhà trên map khi click trái thay vì xây ngay, giải phóng chuột.
  - Các nút Xây/Xoay/Hủy nổi cạnh ngôi nhà 3D.
  - Context menu Xoay/Nhấc/Xóa nổi cạnh nhà khi click vào nhà đã xây.
- **URP Grid Renderer** (`BuildGridRenderer.cs`): Dùng `RenderPipelineManager.endCameraRendering` để vẽ lưới bằng lệnh `Graphics.DrawMeshNow` thay cho `GL.Lines` (không chạy trên URP).

### Fixed
- **Build Mode Bugs**:
  - Khối Ghost không trong suốt: Sửa shader URP.
  - Lỗi click UI xuyên xuống game: Tự động ẩn `GameHUD` khi bật chế độ xây dựng.
  - Ghost bị dính chuột: Đổi logic sang Pin position.

### Changed Files
- `Assets/UI/BuildModeOverlayController.cs`
- `Assets/UI/BuildModeOverlay.uxml`
- `Assets/UI/Styles/BuildModeOverlay.uss`
- `Assets/Scripts/Environment/GhostPlacementController.cs`
- `Assets/Scripts/Environment/BuildGridRenderer.cs` [NEW]
- `Assets/Scripts/Environment/BuildGridManager.cs`
- `Assets/Scripts/Environment/FishingSpot.cs` [NEW]

---
## [2026-06-06] — Part A Hoàn thành: Onboarding Flow + Tutorial UX + Name Tags

### Added
- **Character Select UI Toolkit** (`CharacterSelect.uxml`, `CharacterSelectController.cs`):
  - Chọn giới tính (♂/♀ cards), đặt tên (2-16 ký tự, validate), popup cảnh báo xác nhận.
  - Vietnamese text set từ C# code (không gõ trực tiếp UXML).
  - GameManager tự Show/Hide theo state.
- **Tutorial UX cải thiện cho đối tượng nhỏ tuổi** (`TutorialManager.cs`):
  - Instruction Banner lớn (nền xanh) hiện mỗi bước quan trọng.
  - Countdown Timer to (48px) giữa màn hình khi chờ cây lớn, đổi màu xanh→vàng→đỏ.
  - Dấu chấm than (!) vàng nhấp nhô+xoay trên đầu NPC.
  - NPC chào ngay khi tutorial bắt đầu.
- **Floating Name Tags** (`FloatingNameTag.cs`):
  - TextMeshPro 3D + billboard rotation — chữ phẳng sắc nét kiểu Minecraft.
  - Outline đen dày, màu theo Design System (Player=Gold #FFC107, NPC=Hero #5B42F3).
  - Anti-frustum culling: overflow mode, disable occlusion, force mesh update.
  - Fade opacity khi xa, ẩn khi >30m.
  - Auto-attach cho Player (GameManager) và NPC (GuideNPC).

### Fixed
- **Legacy Input bug** (`TutorialManager.cs`): `Input.GetMouseButtonDown(0)` → `Mouse.current.leftButton.wasPressedThisFrame`.
- **URP Shader tím** (8 files): `Shader.Find("Standard")` → `Shader.Find("Universal Render Pipeline/Lit")` trong FarmTile, GhostPlacement, GuideNPC, TutorialManager.
- **CharacterSelect không ẩn sau xác nhận**: Thêm Hide() + GameManager quản lý visibility.

### Changed Files
- `Assets/UI/CharacterSelect.uxml` [NEW]
- `Assets/UI/CharacterSelectController.cs` [NEW]
- `Assets/Scripts/UI/FloatingNameTag.cs` [NEW]
- `Assets/Scripts/Tutorial/TutorialManager.cs`
- `Assets/Scripts/Tutorial/GuideNPC.cs`
- `Assets/Scripts/Managers/GameManager.cs`
- `Assets/Scripts/Environment/FarmTile.cs`
- `Assets/Scripts/Environment/GhostPlacementController.cs`

---
## [2026-06-05] — Module Build Mode / Chế độ Xây dựng (WIP)

### Added
- **Build Mode Overlay UI** — Giao diện xây dựng/trang trí nông trại:
  - **Control Bar** (cạnh trên): Pill số dư POS, tiêu đề "CHẾ ĐỘ XÂY DỰNG", 5 nút công cụ (Hoàn tác, Di chuyển, Xoay, Xóa, Lưu) + nút thoát (✕ đỏ).
  - **Category Sidebar** (cạnh trái): 5 tab dọc — Nhà cửa, Nông trại, Hàng rào, Trang trí, Đường đi. Tab active nền vàng `#FFC107`.
  - **Item Bar** (cạnh dưới): ScrollView ngang chứa card vật phẩm (80×96px) với icon, tên, giá POS. Card được chọn viền vàng 3px.
  - **Detail Tooltip** (nổi phía trên item): Panel kem `#F5F0E8` với retro shadow, hiển thị icon + tên + kích thước + giá + mô tả + nút "ĐẶT XUỐNG".
  - **Status Label**: Nhãn trung tâm mờ dần (fade-out 2s) thông báo kết quả thao tác.
  - **Màu chủ đạo**: Nâu gỗ `#8B5E3C` cho nút Xoay và sidebar button trên HUD.
  - **Mock Data**: 5 danh mục × ~5 vật phẩm = ~25 item mẫu với emoji, giá, kích thước.
- **Hệ thống 3D Placement**:
  - **BuildGridManager**: Lưới 50×50 ô (1 unit/ô), world↔grid conversion, occupancy validation (CanPlace/OccupyCells/FreeCells), Gizmos debug, **follow target** (grid bám theo nhân vật liên tục mỗi frame).
  - **BuildGridRenderer**: Vẽ lưới ô vuông runtime bằng GL.Lines trong Game View (không chỉ Scene View). Viền nâu gỗ. Bật/tắt theo Build Mode.
  - **GhostPlacementController**: Cube bán trong suốt theo chuột qua Raycast → snap grid → xanh lá (hợp lệ) / đỏ (trùng hoặc ngoài grid). Click trái đặt, click phải hủy. Hỗ trợ xoay + multi-cell (2x2, 3x3...).
  - **BuildCameraController**: Camera Top-Down 75°, smooth transition từ/về ThirdPersonCamera, WASD pan, scroll zoom. Unlock cursor cho Build Mode.
- **HUD Integration**:
  - Nút 🔨 (`BtnBuild`) trên sidebar trái, nền nâu gỗ `#8B5E3C`.
  - Phím **B** toggle Build Mode.

### ⚠️ WIP — Chưa hoàn thiện
- Ghost dùng Primitive Cube placeholder — chờ model 3D thật.
- Chưa test kỹ ghost xanh/đỏ, cho xây/hủy/di chuyển.
- Chưa có logic trừ POS qua ghost system (chỉ có mockup UI).
- Chưa có lưu/load bố cục nông trại.

### Files changed
- Assets/UI/BuildModeOverlay.uxml (NEW)
- Assets/UI/Styles/BuildModeOverlay.uss (NEW)
- Assets/UI/BuildModeOverlayController.cs (NEW)
- Assets/Editor/SetupBuildModeUI.cs (NEW)
- Assets/Scripts/Environment/BuildGridManager.cs (NEW)
- Assets/Scripts/Environment/BuildGridRenderer.cs (NEW)
- Assets/Scripts/Environment/GhostPlacementController.cs (NEW)
- Assets/Scripts/Camera/BuildCameraController.cs (NEW)
- Assets/UI/GameHUD.uxml (MODIFIED — thêm BtnBuild)
- Assets/UI/Styles/GameHUD.uss (MODIFIED — thêm .sidebar-btn-build)
- Assets/UI/GameHUDController.cs (MODIFIED — thêm reference + callback + phím B)
- docs/MEMORY.md (MODIFIED — thêm bài học #46, #47)

---
## [2026-06-05] — Module Splash/Loading Screen (Màn hình Chào/Tải game)

### Added
- **Splash Loading Screen** — Màn hình khởi động game hiển thị đầu tiên khi Play Mode:
  - **Logo thương hiệu**: Chữ "Y WONDER GREEN FARM" cỡ 48px nét đậm trắng trên nền tối `#1E1E23`, có bóng đổ retro cứng `#3D3535` lệch 4px tạo hiệu ứng khắc chữ nổi.
  - **Phụ đề**: "CUỘC PHIÊU LƯU BẮT ĐẦU" màu vàng `#FFC107`, letter-spacing 6px tạo cảm giác trang trọng.
  - **Thanh tiến trình retro**: Chiều rộng 400px, chiều cao 24px, viền dày 3px `#3D3535`, nền xám `#3A3A42`. Vệt nạp màu vàng `#FFC107` bo góc 8px, chiều rộng thay đổi mượt mà từ 0% → 100% theo eased curve (smoothstep).
  - **Nhãn trạng thái động**: Thay đổi theo 5 mốc phần trăm — "Đang tải cấu hình nông trại..." → "Đang kết nối đến máy chủ Cloud..." → "Đang đồng bộ dữ liệu thế giới 3D..." → "Đang chuẩn bị giao diện..." → "Tải hoàn tất!".
  - **Nhãn phần trăm**: Hiển thị `0%` → `100%` đồng bộ với thanh tiến trình.
  - **Trang trí**: 4 ngôi sao Unicode ✦✧ ở 4 góc màn hình tạo bầu không khí, gạch phân cách vàng mờ dưới phụ đề, nhãn phiên bản `v0.1.0-alpha` góc dưới phải.
- **Tính năng mở rộng**:
  - **Sort Order = 10**: UIDocument đặt Sort Order cao hơn Login Screen (mặc định 0) để tự động đè lên mà không cần thay đổi GameManager.
  - **Click to Skip**: Nhấp chuột vào bất kỳ đâu trên màn hình splash trong lúc tải sẽ nhảy nhanh đến 100% và chuyển cảnh.
  - **Fade-out Transition**: Khi tải hoàn tất, toàn bộ màn hình splash mờ dần (opacity 1 → 0) trong 0.5 giây, rồi GameObject tự động deactivate để lộ Login Screen bên dưới.
  - **Phím nóng P**: Bấm phím P (New Input System) bất kỳ lúc nào để bật lại Splash Screen và chạy lại mô phỏng từ 0% — tiện cho nhà phát triển kiểm thử và quay video demo.
  - **Simulated Loading**: Coroutine giả lập tiến trình từ 2 đến 3 giây ngẫu nhiên với đường cong smoothstep tạo cảm giác tải tự nhiên.

### Files changed
- Assets/UI/SplashLoadingScreen.uxml (NEW)
- Assets/UI/Styles/SplashLoadingScreen.uss (NEW)
- Assets/UI/SplashLoadingController.cs (NEW)
- Assets/Editor/SetupSplashUI.cs (NEW)

---
## [2026-06-04] — Module Fishing UI (Mini-game Câu cá)

### Added
- **Fishing Overlay** — Giao diện mini-game câu cá tương tác đầy đủ với các trạng thái:
  - **Chuẩn bị (Ready Panel)**: Chọn mồi câu (Không mồi / Mồi thường / Mồi xịn) và nút "QUĂNG CẦN" (🎣).
  - **Chờ đợi (Waiting Panel)**: Biểu tượng phao câu nhấp nhô theo nhịp sóng (sine wave animation trong Update) và nút "Thu cần" để hủy câu sớm.
  - **Giật cần (QTE Panel)**: Xuất hiện khi cá cắn câu sau 3-6 giây ngẫu nhiên.
    - Thanh đo lực chứa **Vùng Xanh (Safe Zone)** thay đổi độ rộng theo loại mồi.
    - Kim đỏ dao động liên tục qua lại bên trong thanh đo.
    - Thanh thời gian cạn dần biểu thị giới hạn **1.5 giây QTE**.
    - Nút "GIẬT CẦN!" và phím nóng `Space` để câu.
  - **Kết quả (Result Panel)**: Bảng thông báo thành công/hụt dạng modal bo góc viền đen.
    - Hiển thị tên cá, biểu tượng emoji, độ hiếm (Thường/Hiếm/Sử Thi/Sự Kiện), và mô tả phần thưởng.
    - Liên kết thưởng POS trực tiếp khi câu thành công. Có 5% cơ hội câu được Bao Lì Xì Event 🎁.
- **Tính năng mở rộng**:
  - **Bait Mechanics**: Sử dụng mồi thường tăng tỉ lệ cá hiếm, mồi xịn tăng tỉ lệ cá sử thi và mở rộng vùng xanh QTE lên 40%, giảm tốc độ kim.
  - **Bait Shop Fallback**: Tích hợp ConfirmDialog mời mua thêm 5 mồi thường giá 50 POS khi hết lượt câu miễn phí.
  - **Cheat Panel**: Thanh hỗ trợ nhà phát triển ở góc dưới bên trái gồm các nút hồi 10 lượt, mua 10 mồi và công tắc "Auto-Win QTE" giúp kiểm thử chính xác nhanh chóng.
  - **HUD Integration**: Nút 🎣 ở sidebar và phím nóng `F` trên bàn phím để mở/đóng chế độ câu cá.

### Files changed
- Assets/UI/FishingOverlay.uxml (NEW)
- Assets/UI/Styles/FishingOverlay.uss (NEW)
- Assets/UI/FishingOverlayController.cs (NEW)
- Assets/Editor/SetupFishingUI.cs (NEW)
- Assets/UI/GameHUD.uxml (MODIFIED — thêm nút 🎣)
- Assets/UI/Styles/GameHUD.uss (MODIFIED — thêm css nút 🎣)
- Assets/UI/GameHUDController.cs (MODIFIED — thêm tích hợp nút & phím F)

---
## [2026-06-04] — Module Chat UI (Khung chat thu/mở)

### Added
- **Chat Panel** — Hệ thống chat kênh thế giới đặt tại cạnh dưới giữa màn hình với 2 trạng thái:
  - **Trạng thái thu gọn (Collapsed)**: Thanh pill mờ đen đồng bộ HUD hiển thị tin nhắn mới nhất, có nút emoji nhanh và nút mở rộng (▲).
  - **Trạng thái mở rộng (Expanded)**: Khung chat 420x260px nền tối bán trong suốt (Dark Translucent — `rgba(30, 30, 35, 0.88)`) không che khuất thế giới 3D, viền tối 3px chuẩn design system.
    - **Header**: Thanh tiêu đề "KÊNH THẾ GIỚI" nền đen mờ kèm nút thu nhỏ (▼).
    - **History scroll**: Cuộn lịch sử tin nhắn nền tối mờ, chữ trắng/sáng màu dễ đọc, tự động cuộn xuống đáy khi có tin nhắn mới (On GeometryChangedEvent).
    - **Footer**: Input nhập tin nhắn màu trắng nổi bật, nút gửi ("Gửi") màu xanh blue retro, nút emoji nhanh (☺) màu kem sáng.
  - **Nút bấm Tactile đồng bộ**: Cả nút mở rộng (▲), thu nhỏ (▼) và emoji nhanh (☺) đều được thiết kế dạng phím cơ bo góc tròn, màu tím thương hiệu (`#5B42F3`), viền dày 2px `#3D3535`, có phản hồi vật lý lún 1px khi click.
- **Tính năng mở rộng**:
  - **Profanity Filter**: Tự động lọc các từ tục tĩu tiếng Việt/Anh ("ngu", "fuck", "đm", "vl"...) thành dấu `***`.
  - **Rate Limit**: Giới hạn tần suất chat (tối đa 5 tin nhắn trong 30 giây). Nếu vượt quá, hiển thị cảnh báo đỏ từ hệ thống.
  - **Mock AI Chatbot**: Tự động trả lời theo từ khóa tin nhắn ("hello", "nông trại", "shop", "bản đồ", "heo đất"...) sau 2 giây delay để mô phỏng tính năng AI NPC.
  - **Enter Hotkey**: Bấm phím `Enter` để mở rộng chat và tự động focus vào input field; bấm tiếp để gửi tin; bấm khi rỗng sẽ tắt focus/thu nhỏ.
  - **Settings Integration**: Thêm toggle "Hiện chat" vào Cài đặt (SettingsPopup) để bật/tắt hiển thị toàn bộ khung chat.

### Files changed
- Assets/UI/ChatPanel.uxml (NEW)
- Assets/UI/Styles/ChatPanel.uss (NEW)
- Assets/UI/ChatPanelController.cs (NEW)
- Assets/Editor/SetupChatUI.cs (NEW)
- Assets/UI/SettingsPopup.uxml (MODIFIED — thêm toggle Hiện chat)
- Assets/UI/SettingsPopupController.cs (MODIFIED)
- Assets/UI/GameHUD.uxml (MODIFIED — xóa MessagesBar cũ)
- Assets/UI/GameHUDController.cs (MODIFIED — xóa dọn dẹp MessagesBar)

### Fixed & Refactored
- **Xóa giao diện đè chồng trong Editor (Edit Mode)**: Loại bỏ hoàn toàn `MessagesBar` cũ trong `GameHUD.uxml` và dọn dẹp các C# bindings liên quan trong `GameHUDController.cs` để tránh đè chồng lên Chat Panel mới lúc chưa chạy game trong Unity Editor.
- **Đồng bộ hóa nút bấm**: Thay đổi style các nút tam giác, emoji phẳng không viền thành các nút đặc có khối đế màu tím viền đen dày để đúng tinh thần giao diện cơ học.
- **Lỗi biên dịch Setup script**: Sửa lỗi tham chiếu sai thuộc tính `sourceAsset` thành `visualTreeAsset` trên `UIDocument` trong C# Editor setup script.

---
## [2026-06-04] — Module Event / Exchange UI (Sự kiện mùa)

### Added
- **Event Popup** — UI sự kiện theo mùa với 2 tab:
  - **Tab Đổi quà**: Grid đổi vật phẩm event (cá, quặng, vé) lấy reward hiếm (V2 items, pet, cosmetic)
  - **Tab Gói ưu đãi**: Bundle UPOS giảm giá giới hạn thời gian, có tag "-50%"/"HOT", trạng thái "ĐÃ HẾT"
  - **Sidebar Vật phẩm**: Hiển thị số lượng 🐟 Cá event / 💎 Quặng hiếm / 🎫 Vé sự kiện
  - **Timer pill**: Đếm ngược thời gian sự kiện còn lại
  - **Header**: Festival Purple #9C27B0
  - **Close button**: Wrapper pattern chuẩn (Lessons #33 #34)
  - **Mock data**: 6 exchange items + 3 bundles

### Files changed
- Assets/UI/EventPopup.uxml (NEW)
- Assets/UI/Styles/EventPopup.uss (NEW)
- Assets/UI/EventPopupController.cs (NEW)
- Assets/UI/GameHUD.uxml (MODIFIED — thêm BtnEvent 🎁)
- Assets/UI/Styles/GameHUD.uss (MODIFIED — thêm sidebar-btn-event styles)
- Assets/UI/GameHUDController.cs (MODIFIED — thêm eventPopup reference + BtnEvent callback + E key test)

### Fixed
- **UXML comment Unicode**: Comment `<!-- ═══ ... ═══ -->` chứa ký tự Unicode `═` khiến UI Builder không mở được file. Đã đổi thành ASCII thuần.
- **Header bị co rúm khi đổi tab**: Header và tab bar thiếu `flex-shrink: 0`, bị body content ép nhỏ khi tab Đổi quà có nhiều item.
- **Bundle cards cao thấp khác nhau**: Dùng `min-height` chỉ đặt mức tối thiểu, card có description dài vẫn cao hơn. Fix: dùng `height: 280px` cố định + spacer `flex-grow: 1` đẩy nút xuống đáy.
- **Legacy Input API**: `Input.GetKeyDown` gây lỗi vì project dùng New Input System. Fix: dùng `Keyboard.current.eKey.wasPressedThisFrame`.

---
## [2026-06-04] — Module Level Up VFX/UI

### Added
- **Level Up Overlay** — Fullscreen golden VFX khi người chơi thăng cấp:
  - Background: Overlay tối + vùng glow vàng tròn ở giữa
  - **Badge** ⭐ scale animation (0.5→1)
  - **"LEVEL UP!"** text scale animation (0.6→1)
  - **Level mới** hiển thị trong pill viền vàng
  - **Mở khóa** section (xanh lá, chỉ hiện khi level có unlock): Lv.5 Câu cá, Lv.10 Mỏ đá, Lv.40 Đảo Hải Phú...
  - **Nút "TIẾP TỤC"** màu vàng gold để đóng
  - Star decorations ✦✧ trang trí xung quanh
  - Fade in/out via CSS opacity transition
- **Keyboard Test** — Bấm phím **L** trong Play Mode để test Level Up liên tục

### Files changed
- Assets/UI/LevelUpOverlay.uxml (NEW)
- Assets/UI/Styles/LevelUpOverlay.uss (NEW)
- Assets/UI/LevelUpOverlayController.cs (NEW)
- Assets/UI/GameHUDController.cs (MODIFIED — thêm levelUpOverlay reference + L key test)

---
## [2026-06-04] — Module Heo Đất UI (Piggy Bank Savings)

### Added
- **Heo Đất Popup** — Gửi tiết kiệm POS với 3 gói lãi suất:
  - **3 gói**: 12 ngày (+2%), 30 ngày (+6%), 180 ngày (+45%)
  - **Tab Gửi tiết kiệm**: Chọn gói → nhập số tiền → preview (gốc/lãi/tổng) → xác nhận
  - **Validation**: Kiểm tra số dư, chỉ cho phép 1 gói active, không rút sớm
  - **Countdown**: Đếm ngược real-time (test mode: 1 ngày = 5 giây)
  - **Đáo hạn**: Tự động cộng gốc + lãi vào balance, thêm entry lịch sử
  - **Tab Lịch sử**: Hiển thị các giao dịch đã hoàn thành + mock data
  - **Header**: Warm Gold #E8833A, balance pill góc trái (Lessons #30 #32 applied)
  - **Close button**: Wrapper pattern chuẩn (Lessons #33 #34 applied)
- **HUD Piggy Button** — Nút 🐷 trên sidebar HUD, màu #E8833A

### Files changed
- Assets/UI/PiggyBankPopup.uxml (NEW)
- Assets/UI/Styles/PiggyBankPopup.uss (NEW)
- Assets/UI/PiggyBankPopupController.cs (NEW)
- Assets/UI/GameHUD.uxml (MODIFIED — thêm BtnPiggy)
- Assets/UI/Styles/GameHUD.uss (MODIFIED — thêm sidebar-btn-piggy styles)
- Assets/UI/GameHUDController.cs (MODIFIED — thêm piggyBankPopup reference + callback)

### Fixed
- **Package card tràn nội dung**: Layout dọc (icon→tên→rate→label) xếp 4 tầng quá cao, rate bị tràn ra ngoài viền card. Sửa bằng cách chuyển sang layout **ngang** (icon ← tên → rate), ẩn label dư thừa.
- **Preview rows đè chồng**: Các dòng Gốc/Lãi/Nhận về bị overlap do thiếu `min-height`, `align-items`, `flex-shrink`. Thêm `min-height: 18px` + `flex-shrink: 0` cho label/value.

---
## [2026-06-04] — Module Map UI (Visual World Map)

### Added
- **Map Popup** — Bản đồ thế giới dạng visual map (biển + đảo), không phải danh sách:
  - Nền đại dương xanh #1A8FBF với sóng trang trí `〰〰〰` + la bàn 🧭
  - **5 đảo positioned** trên bản đồ, mỗi đảo có vùng đất riêng (hình/màu khác nhau):
    - 🏡 Nông trại (xanh lá, center-left, luôn mở)
    - 🏙️ Thành phố (xám bạc, center-right, cần tutorial)
    - ⛏️ Mỏ đá (nâu, top-center, Lv.10)
    - 🏝️ Đảo Hải Phú (vàng cát, bottom-left, Lv.40 + VIP/Vé, có 🔒 overlay)
    - 🌲 Đảo Mộc Nhi (xanh đậm, bottom-right, Lv.60 + VIP/Vé, có 🔒 overlay)
  - **Interaction**: Bấm đảo → pin viền vàng gold + floating info card hiện ở dưới → bấm "🚀 DI CHUYỂN"
  - **Info Card**: Icon, tên, status badge (ĐÃ MỞ KHÓA/ĐANG KHÓA), mô tả, yêu cầu ✅/❌, nút travel
  - **Cheat Bar**: 2 nút test — cycle level (1→5→15→45→65), cycle VIP/Vé
  - **Top bar**: Semi-transparent dark, level pill góc trái, tiêu đề "BẢN ĐỒ THẾ GIỚI"
- **HUD Map Button** — Nút tạm "🗺️ Map" trên sidebar HUD, màu #00B4D8

### Fixed
- **Close button bị cắt (clip)**: Nút X nằm bên trong `map-container` có `overflow: hidden` → bị lẹm góc. Sửa bằng cách thêm `map-wrapper` bọc ngoài (không có overflow), đặt close button ở wrapper level.
- **Close button khó thấy**: Ban đầu nút X nằm trong top bar tối màu → lẫn vào nền. Chuyển ra góc phải trên, nhô ra ngoài viền (pattern chuẩn `right: -8px; top: -8px`).

### Files changed
- Assets/UI/MapPopup.uxml (NEW — restructured: map-wrapper → map-container + close)
- Assets/UI/Styles/MapPopup.uss (NEW — ocean bg, islands, info card, wrapper)
- Assets/UI/MapPopupController.cs (NEW — visual map, dictionary data, island clicks)
- Assets/UI/GameHUD.uxml (MODIFIED — thêm BtnMap)
- Assets/UI/Styles/GameHUD.uss (MODIFIED — thêm sidebar-btn-map styles)
- Assets/UI/GameHUDController.cs (MODIFIED — thêm mapPopup reference + callback)

---
## [2026-06-04] — Shop UI Polish & Sell Mode Testing

### Fixed
- **Số dư dính header**: Pill số dư (`🪙 5,000 POS`) bị nằm giữa header đè lên tiêu đề → sửa bằng `position: absolute; left: 12px` để ghim góc trái.
- **Tiêu đề tràn viền**: Chữ "HAI LÚA — VẬT TƯ NÔNG TRẠI" quá dài, sắp chìa ra ngoài → giảm font `20px → 18px`, thêm `padding: 0 120px` + `text-overflow: ellipsis`.
- **Bottom bar thừa thông tin**: Dòng "Chế độ: Mua" và "Số dư" ở cạnh dưới bị lệch nhau giữa các tab → xóa hoàn toàn bottom info bar vì tab Mua/Bán trên sidebar đã thể hiện rõ chế độ, số dư chuyển lên header.

### Added
- **Sell Mode mock data**: Bật tab Bán (`hasSellTab = true`) với 8 item nông sản có thể bán (Cà rốt 15 POS, Rau cải 20 POS, Dưa hấu 50 POS, Trứng gà 25 POS, Sữa bò 40 POS, Gỗ 8 POS, Đá 12 POS...) để test chuyển đổi Mua/Bán.

### Files changed
- Assets/UI/ShopPopup.uxml (MODIFIED — xóa bottom bar, thêm balance pill vào header)
- Assets/UI/Styles/ShopPopup.uss (MODIFIED — thêm balance pill styles, xóa info-bar styles, sửa header title)
- Assets/UI/ShopPopupController.cs (MODIFIED — xóa lblMode, cập nhật UpdateBalance format, thêm sell mock data)

---
## [2026-06-04] — HUD Shop Test Button Integration

### Added
- **HUD Shop Button** — Tích hợp nút tạm "🛒 Shop" trên HUD để test nhanh Shop Popup:
  - Màu nền xanh lá #4CAF50 đồng bộ với header shop, viền 3px #3D3535.
  - Sử dụng bố cục ngang (flex-direction: row) gồm emoji 🛒 và nhãn chữ "Shop".
  - Hiệu ứng cơ học đầy đủ: hover phóng to/đổi màu nhẹ, active lún xuống 3px.
  - Tích hợp callback click mở ShopPopup với mock data mặc định ("Hai Lúa").
  - Cơ chế dự phòng (fallback) tự động tìm kiếm `ShopPopupController` và các popup controller khác trong `OnEnable()` nếu chưa kéo thả trong Inspector.

### Files changed
- Assets/UI/GameHUD.uxml (MODIFIED)
- Assets/UI/Styles/GameHUD.uss (MODIFIED)
- Assets/UI/GameHUDController.cs (MODIFIED)

---
## [2026-06-04] — Module Shop UI (Template chung 12 shop)

### Added
- **Shop Popup** — Template UI dùng chung cho tất cả 12 cửa hàng trong Thành phố:
  - Layout landscape 2 cột giống Inventory: Sidebar + Grid + Detail Panel
  - **Sidebar trái**: 2 tab chế độ (🛒 Mua / 💰 Bán) + 5 filter danh mục (Tất cả / Hạt giống / Vật nuôi / Dụng cụ / Vật phẩm)
  - **Grid giữa**: Item cards (icon + tên + giá POS) với hover/active/selected states
  - **Detail phải**: Icon lớn + tên + giá + mô tả + bộ chọn số lượng (−/+) + tổng tiền + nút MUA/BÁN
  - **Tab Bán**: tự ẩn nếu shop không hỗ trợ bán (cấu hình qua `ShopData.hasSellTab`)
  - Reusable API: `Show(ShopData data)` — mỗi NPC shop truyền data riêng
  - Mock data mặc định: "Hai Lúa — Vật tư nông trại" (10 items, giá theo kịch bản)
  - Header xanh lá #4CAF50, border #388E3C, style khớp popup cũ (22px radius, 3px border)
  - Nút Mua màu xanh lá, nút Bán màu cam #E8833A
  - Footer hiển thị số dư POS + chế độ hiện tại

### Files changed
- Assets/UI/ShopPopup.uxml (NEW)
- Assets/UI/Styles/ShopPopup.uss (NEW)
- Assets/UI/ShopPopupController.cs (NEW)

---

## [2026-06-04] — Forgot Password Popup + UI Consistency Fix

### Added
- **Forgot Password Popup** — Popup riêng cho luồng quên mật khẩu:
  - 1 input Email (có icon ✉, focus highlight border xanh)
  - Nút "Gửi mã xác nhận" luôn bấm được, hiện lỗi nếu email sai
  - Validate email real-time (regex), status thành công/lỗi
  - Header xanh #2D7BFF, overlay click-to-dismiss, nút X đỏ
  - Mockup flow: validate → hiện thông báo gửi mã thành công
- Tích hợp vào **LoginScreenController**: nút "Quên mật khẩu?" gọi `ForgotPasswordPopupController.Show()`

### Fixed
- **UI Consistency** — Sửa toàn bộ ConfirmDialog.uss và RewardPopup.uss cho khớp phong cách popup cũ:
  - Panel: `border-radius: 22px`, `border-width: 3px`, `border-color: #3D3535`
  - Shadow wrapper: `transparent` + `padding 6px` (không tô màu)
  - Close button: `border-radius: 10px`, `3px #3D3535`, `:active → #CC3333`
  - Nút action: `3px border`, `translate: 1px 1px`, `transition: 0.08s`
  - Bỏ shadow wrapper phía sau các nút bấm (nút phẳng)

### Files changed
- Assets/UI/ForgotPasswordPopup.uxml (NEW)
- Assets/UI/Styles/ForgotPasswordPopup.uss (NEW)
- Assets/UI/ForgotPasswordPopupController.cs (NEW)
- Assets/UI/LoginScreenController.cs (MODIFIED — thêm SerializeField + gọi Show)
- Assets/UI/Styles/ConfirmDialog.uss (MODIFIED — khớp popup cũ)
- Assets/UI/Styles/RewardPopup.uss (MODIFIED — khớp popup cũ)
- docs/MEMORY.md (MODIFIED — thêm bài học #29 UI Consistency)

---

## [2026-06-04] — Module Confirm Dialog & Reward Popup
### Added
- **Confirm Dialog** — Component reusable dạng modal nhỏ trung tâm cho toàn game:
  - 3 loại dialog: Warning (⚠ vàng #FFC107), Danger (✕ đỏ #FF4B4B), Info (i xanh #2D7BFF)
  - API: `Show(title, message, confirmText, cancelText, onConfirm, dialogType)`
  - 2 nút: Hủy bỏ (xám) + Xác nhận (màu theo type)
  - Icon Unicode trong vòng tròn màu theo type
  - Overlay click-to-dismiss, nút X đỏ góc trên phải
- **Reward Popup** — Component reusable hiển thị phần thưởng:
  - API: `Show(title, rewards, buttonText, onClaim)`
  - Lưới reward items tự động tạo từ `List<RewardItemData>`
  - Mỗi item: icon + tên + số lượng trong khung trắng bo góc 16px
  - Header vàng #FFC107, nút "Nhận thưởng" mechanical press
  - Empty state "Không có phần thưởng" khi danh sách rỗng
- Cả 2 component tuân thủ đầy đủ The Tangible Playground:
  - Retro shadow 6px offset, 0px blur
  - Spacing bội 4/8px, border 2-3px #3D3535
  - Đủ trạng thái :hover, :active, :disabled
  - Không glassmorphism, không gradient, không icon thừa
  - Callbacks đăng ký 1 lần, unregister khi disable
### Files changed
- Assets/UI/ConfirmDialog.uxml (NEW)
- Assets/UI/Styles/ConfirmDialog.uss (NEW)
- Assets/UI/ConfirmDialogController.cs (NEW)
- Assets/UI/RewardPopup.uxml (NEW)
- Assets/UI/Styles/RewardPopup.uss (NEW)
- Assets/UI/RewardPopupController.cs (NEW)

---

## [2026-06-03] — Onboarding Cinematic Skip & Tutorial Fallback
### Added
- Tích hợp Cinematic UI cho màn hình thuyền cập bến (`BoatCutscene.cs`): Bao gồm nút **Bỏ qua (Skip)** xuất hiện sau 3 giây và hội thoại dẫn truyện chạy dọc ở đáy màn hình. Bấm "Bỏ qua" sẽ dịch chuyển tức thời thuyền và camera đến vị trí kết thúc.
- Triển khai cơ chế **Tự động Tìm kiếm & Khởi tạo (Bulletproof Fallbacks)** trong `TutorialManager.cs`: Nếu mảnh đất `FarmTile` hoặc `GuideNPC` bị thiếu trong Scene, script sẽ tự động sinh các GameObject placeholder tương thích kèm BoxCollider và các thành phần logic để tutorial chạy trơn tru không lỗi.
- Triển khai **mô phỏng visual bằng hình 3D hình học (Primitives Mockup)** cho `FarmTile.cs`: Tự động vẽ Soil (Khối nâu), Plowed (Khối nâu đậm), Seed (Sprout nhỏ), Watered (Sprout vừa), Ripe (Củ cà rốt cam) khi thiếu tài nguyên 3D art từ Artist.
- Triển khai **model Capsule tạm thời** cho `GuideNPC.cs`: Vẽ Capsule màu tím cao 2m cùng mũi kim chỉ hướng màu vàng để người chơi định vị NPC. Tự động sinh 3 Waypoints dẫn đường đến mảnh đất nếu waypoints bị trống.
### Changed
- Quản lý đồng bộ hiển thị HUD (`GameManager.cs`): Tự động ẩn HUD trong các trạng thái Login, Menu, Cutscene và hiển thị lại khi vào Gameplay.
### Files changed
- Assets/Scripts/Cutscenes/BoatCutscene.cs (MODIFIED)
- Assets/Scripts/Managers/GameManager.cs (MODIFIED)
- Assets/Scripts/Tutorial/TutorialManager.cs (MODIFIED)
- Assets/Scripts/Tutorial/GuideNPC.cs (MODIFIED)
- Assets/Scripts/Environment/FarmTile.cs (MODIFIED)

## [2026-06-03] — UI/UX Layout Polish (Friends, Quest, Attendance, Settings Popups)
### Fixed
- Popup Cài đặt (Settings): Polish và hoàn thiện layout ngày 03/06.
- Popup Bạn bè (Friends): Thêm khoảng đệm an toàn `margin-right: 16px` cho cụm tìm kiếm và thu nhỏ kích thước của TextField nhập tên cùng các nút bấm để tránh đè lấn lên nút đóng X ở góc trên bên phải.
- Popup Nhiệm vụ (Quest) & Điểm danh (Attendance): Khắc phục triệt để lỗi ô vật phẩm phần thưởng bị chòi ra ngoài viền khung chứa bằng cách thiết lập `flex-shrink: 0` cho các container/grid phần thưởng và các slot con cố định, giữ nguyên layout cân đối khi kích thước màn hình thay đổi.
- Popup Nhiệm vụ (Quest) & Thông tin nhân vật (Profile): Sửa lỗi text chỉ số tiến trình (`10 / 10`) và EXP bị lệch sát đáy dưới thanh bằng cách reset `margin` và `padding` về `0` cho `.quest-progress-text` và `.profile-exp-text`.
- Popup Nhiệm vụ (Quest) & Thông tin nhân vật (Profile): Khắc phục lỗi thanh tiến trình khi đầy 100% bị khuyết vệt đen ở đầu bên phải do lỗi render bo góc bằng cách thiết lập `border-radius` cho `.quest-progress-fill` và `.profile-exp-fill` tương thích với khung track của chúng.
- Popup Điểm danh (Attendance): Reset margin và padding về 0 cho emoji và chữ số lượng phần thưởng ngày để tránh lệch tâm hiển thị.
### Files changed
- Assets/UI/Styles/FriendsPopup.uss (MODIFIED)
- Assets/UI/Styles/QuestPopup.uss (MODIFIED)
- Assets/UI/Styles/AttendancePopup.uss (MODIFIED)
- Assets/UI/Styles/ProfilePopup.uss (MODIFIED)

## [2026-06-02] — HUD Sidebar & 3 New Popups (Profile, Attendance, Quest)
### Added
- Tái cấu trúc HUD Sidebar (GameHUD.uxml) theo đúng thứ tự từ trên xuống: Leaderboard (🏆 - vàng), Điểm danh (📅 - tím, nút mới), Hòm thư (✉ - xanh dương), Bạn bè (👥 - xanh lơ). Loại bỏ nút Character cũ.
- Thiết lập Avatar (PlayerInfo) và Quest Bubble (QuestBubble) thành các phần tử tương tác bấm được (Clickable) với đầy đủ hiệu ứng phóng to/thu nhỏ (hover/active scale).
- Popup Thông tin nhân vật (Profile Popup) dạng landscape nền kem `#F5F0E8`: Hiển thị Avatar lớn, thanh tiến trình EXP lớn, và lưới chỉ số nông trại mockup (Cây đã trồng, Nông sản đã bán, Số bạn bè) tự động tải dữ liệu từ HUD.
- Popup Điểm danh 7 ngày (Attendance Popup) dạng lưới 7 ô slot quà: Hiển thị quà đính kèm và trạng thái (Đã nhận / Chưa nhận). Tích hợp nút Điểm danh nhận thưởng và cập nhật trạng thái ô lưới thời gian thực.
- Popup Nhật ký nhiệm vụ (Quest Journal Popup) dạng landscape 2 cột: Danh sách nhiệm vụ đang làm/đã xong bên trái, chi tiết yêu cầu và quà đính kèm bên phải. Cho phép nhận thưởng và đổi trạng thái khi nhiệm vụ hoàn thành.
### Changed
- Cập nhật `GameHUDController.cs` để query các phần tử mới, đăng ký callback click mở 3 popup mới (Profile, Attendance, Quest) và truyền dữ liệu động từ HUD sang Profile.
### Files changed
- Assets/UI/GameHUD.uxml (MODIFIED)
- Assets/UI/GameHUDController.cs (MODIFIED)
- Assets/UI/Styles/GameHUD.uss (MODIFIED)
- Assets/UI/ProfilePopup.uxml (NEW)
- Assets/UI/ProfilePopupController.cs (NEW)
- Assets/UI/Styles/ProfilePopup.uss (NEW)
- Assets/UI/AttendancePopup.uxml (NEW)
- Assets/UI/AttendancePopupController.cs (NEW)
- Assets/UI/Styles/AttendancePopup.uss (NEW)
- Assets/UI/QuestPopup.uxml (NEW)
- Assets/UI/QuestPopupController.cs (NEW)
- Assets/UI/Styles/QuestPopup.uss (NEW)

---

## [2026-06-02] — Module Mailbox Popup
### Added
- Giao diện Hòm thư (Mailbox Popup) dạng landscape chuẩn phong cách "The Tangible Playground" đè lên cảnh game 3D.
- Cột bên trái: Danh sách các thư cuộn mượt mà. Mỗi card thư hiển thị trạng thái động (Đã đọc/Chưa đọc bằng phong bì đóng/mở và chấm xanh dương), ngày gửi, người gửi, và huy hiệu hộp quà nếu có phần thưởng.
- Cột bên phải: Thẻ chi tiết thư nền trắng đặc bo góc, viền tối dày. Khi có thư sẽ hiện tiêu đề, nội dung, lưới ô slot quà đính kèm và nút hành động.
- Nút "Nhận tất cả" ở chân cột danh sách trái hỗ trợ claim nhanh mọi phần thưởng chưa nhận. Nút "Xóa đã đọc" hỗ trợ dọn dẹp hòm thư tự động.
- Nút "Nhận quà" và "Xóa thư" riêng lẻ cho từng thư, cập nhật trạng thái "Đã nhận" thời gian thực.
- Kết nối nút Hòm thư (phong bì) trên HUD sidebar để mở popup.
### Changed
- Cập nhật GameHUDController.cs để tích hợp liên kết gọi MailboxPopupController.Show().
### Files changed
- Assets/UI/MailboxPopup.uxml (NEW)
- Assets/UI/Styles/MailboxPopup.uss (NEW)
- Assets/UI/MailboxPopupController.cs (NEW)
- Assets/UI/GameHUDController.cs (MODIFIED)

---

## [2026-06-02] — Universal Design System & AI UI Guidelines
### Added
- Thiết lập và nâng cấp tài liệu `docs/DESIGN_SYSTEM_TEMPLATE.md` thành **Universal Design System Template** dùng chung cho cả 3 nền tảng: **Web**, **Mobile App**, và **Game**.
- Cấu trúc lại file thành **bản song ngữ (bản tiếng Anh và bản tiếng Việt)** chia làm 2 mục lớn rõ ràng, phục vụ cả các AI Agent quốc tế lẫn các nhà phát triển Việt Nam.
- Tích hợp chi tiết phân tích biểu hiện, tác hại và cách phòng tránh cụ thể cho **8 bệnh lý giao diện kinh điển của AI Agent** (lạm dụng glassmorphism/icon, loạn đơn vị, chột trạng thái, mù tương phản, cụt chữ...) trên từng nền tảng bằng cả hai ngôn ngữ.
- Cung cấp **AI Agent Self-Check Protocol** với 15 câu hỏi kiểm tra chất lượng tự động giúp AI tự kiểm duyệt UI trước khi bàn giao.
### Changed
- Cập nhật tài liệu thiết kế của dự án [DESIGN.md](file:///d:/LamGameUnity/BaChuKhuRung3D/docs/DESIGN.md) áp dụng các thông số thực tế của game **Y WONDER GREEN FARM** theo đúng khung chuẩn Universal và tích hợp các nguyên tắc ngăn ngừa bệnh UI để dự án trực tiếp áp dụng.
### Files changed
- docs/DESIGN_SYSTEM_TEMPLATE.md (NEW)
- docs/DESIGN.md (MODIFIED)

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
- Unity project setup (URP, Unity 6.3 LTS)
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
