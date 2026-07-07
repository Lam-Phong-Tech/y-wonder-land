# 🌿 Y WONDER GREEN FARM - MEMENTO PROTOCOL (BẢN GHI NHỚ TIẾN ĐỘ)

> Update 2026-07-07: mobile joystick/camera hotfix. `InputSystem_Actions.inputactions` now uses `<Mouse>/delta` for Player `Look` instead of `<Pointer>/delta`, so mobile touch drags are not read as camera look globally. `GameHUDController` also isolates joystick pointer capture from `LookZone`. `ThirdPersonCamera` touch pitch was inverted after phone feedback so swipe up looks up and swipe down looks down, without changing PC mouse look. Joystick-driven auto sprint/auto-run was removed; the HUD Sprint button is now the explicit auto-run toggle. Needs phone test: left joystick must not rotate camera/map and must keep normal movement speed at the edge; right-half drag must still rotate camera with the expected vertical feel; Sprint button toggles auto-run.

*Dự án: BaChuKhuRung3D (Game nông trại 3D YWONDERLAND)*
*Ngày cập nhật: 06/07/2026*

## 0. Cập nhật mới nhất

> Nhánh làm việc hiện tại: `dev`. Main đã được merge các patch iOS gần nhất để bên build iOS/CodeMagic lấy được repo ổn định. Chi tiết xem `CHANGELOG.md` + `task.md`.

- **Tạm gác backend, quay lại chỉnh sửa game 06/07:** anh quyết định tạm dừng chuỗi backend sau khi đã có nền mock/API/dashboard/realtime. `task.md` đã được đưa mục ưu tiên mới lên đầu: chờ danh sách task chỉnh sửa game từ anh, sau đó tách checklist theo gameplay/UI/scene/data/build. Các loop backend còn lại vẫn giữ trong `task.md` nhưng chuyển sang trạng thái `[~]` để quay lại sau khi xong nhóm chỉnh sửa game.
- **Backend roadmap theo yêu cầu sếp 06/07:** đã cập nhật `docs/WEB_GAME_BACKEND_JOURNEY.md`, `docs/API_CONTRACTS.md` và `task.md` theo câu trả lời phỏng vấn mới từ anh. Quyết định đã chốt: web đăng nhập bằng email/số điện thoại/password; khách phải có tài khoản trước khi chơi; 1 account = 1 nhân vật; nhiều máy cùng account dùng chung server state; account bị khóa/xóa mềm trên web thì game phải chặn. `Point` vừa là tiền game vừa là tiền nạp từ web, nhưng scope mới là MVP sắp tới **chưa cần nạp/rút**, ưu tiên online + realtime cho khách hàng; web wallet/top-up/spend chuyển sang phase sau. Roadmap bắt đầu từ account web/cấp sẵn -> game player, realtime public islands (`city`/`mine`, không gồm farm), hạ tầng demo online REST + WebSocket, rồi state sync tối thiểu; shop/economy/farm server-authoritative, dashboard online đầy đủ và web wallet làm sau khi lát realtime pass.
- **Realtime test bằng account cấp sẵn 06/07:** web thật đang sập/chưa có account từ bên web, nên dùng `WEB_AUTH_MODE=mock`. Đã thêm `server/realtimeSmokeTest.js` và npm script `test:realtime`; chạy bằng `npm.cmd run test:realtime`. Smoke test local đã pass với `DemoRealtime01/demo`, `DemoRealtime02/demo`, `DemoRealtime03/demo`: 2 client join `city`, client thứ ba không join room vẫn nhận/gửi chat global, gửi `player_state`, và join `farm` bị chặn `ROOM_NOT_SHARED`. Unity `RealtimeClient` nay giữ WebSocket cho chat khi đang gameplay, nhưng chỉ join room `city`/`mine` để hiện remote player nên farm vẫn là đảo riêng.
- **Web account -> Game backend 06/07:** đã thêm `docs/WEB_GAME_BACKEND_JOURNEY.md` để làm rõ phần kịch bản backend còn mơ hồ. Quy ước mới: web hiện có là nguồn tài khoản, game backend map `web_user_id -> playerId`, Unity chỉ gọi game-server và không giữ `GAME_API_SECRET`; game backend là nơi lưu game state. Tài liệu cũng ghi rõ hiện trạng: backend MVP đã có API/dashboard/data mẫu nhưng Unity shop/economy/inventory vẫn còn local `PlayerPrefs`, nên mua/bán trong game chưa tự đổi dashboard backend. Sau scope mới từ anh, loop tiếp theo nên làm là account bridge + realtime public islands để chứng minh online/realtime cho khách hàng; bootstrap economy/inventory và shop buy/sell server-authoritative làm sau khi lát realtime pass. Phase sau mới quay lại web wallet/nạp-rút: cần chốt `Point`/`UPoint`, ledger cuối cùng và endpoint spend/reserve.
- **Backend 06/07 khi làm ở nhà:** anh chọn ưu tiên backend, chưa phụ thuộc máy case/mạng công ty. `server/store.js` đã thành storage facade có `JsonStore` cho dev/local và chọn mode bằng `STORE_MODE=json|postgres`; thêm `server/postgresStore.js` scaffold. Có dashboard backend local tại `http://127.0.0.1:3000/admin` để xem/tạo/sửa/xóa dữ liệu demo trong JSON store. `server/schema.sql` đã có bảng tối thiểu `game_players`, `player_profiles`, `player_economy`, `player_inventory`, `player_farm_state`, `player_daily_limits`, `game_transactions`. `/player/bootstrap` trả thêm `daily_limits`; thêm `GET /player/daily-limits` và `POST /player/daily-limits/consume`. `economy/apply`, `inventory/adjust`, `daily-limits/consume` đều dùng `idempotency_key` để retry không cộng đôi. Đã smoke test Node bằng data file tạm: 10 lượt đào mỏ còn 0, lần 11 bị chặn, retry economy/inventory không nhân đôi. **Còn tiếp:** viết query PostgreSQL thật khi có driver/DB host và nối Unity đọc `daily_limits`/economy/inventory từ bootstrap khi online.
- **Vòng quay may mắn 01/07:** `EventPopupController` đã đổi vòng quay từ kiểu icon rải quanh vòng tối sang nền 12 múi màu tạo runtime. Mỗi múi nay chỉ hiển thị icon item đang có trong `ItemDatabase`; đã bỏ tên item và số lượng trong múi. Giữ nguyên 12 phần thưởng hiện tại và weight/tỉ lệ quay thưởng; riêng `Chúc may mắn lần sau` để ô trống, không hiện icon vòng quay. `BtnSpin` ở tâm vòng dùng icon mới `Assets/Sprites/icon/BoSungIcon/arrowforspin.png` làm nút bấm quay, không còn chữ `QUAY/HẾT/...` runtime đè lên icon; footer chỉ còn số lượt còn lại. Cần test Unity: Sự kiện -> Vòng quay, kiểm vòng không méo khi xoay, đủ 12 múi, ô may mắn trống, icon `Spin` ở tâm rõ và bấm được, bấm quay dừng đúng thưởng, trừ lượt và trao item/toast như cũ.
- **Build Mode mobile 01/07:** `GhostPlacementController` có assist riêng cho touch: điểm raycast được nâng lên trên ngón tay (`touchAimOffsetPixels = 90`) và nếu tap/kéo lệch khỏi collider ô nhỏ thì chọn `BuildSurfaceCell` gần nhất trong bán kính màn hình (`touchAssistRadiusPixels = 96`). `BuildModeOverlayController` truyền rõ input touch/mouse, nên PC/mouse vẫn giữ raycast chính xác như cũ. Cần test mobile với Ruộng/Đường đá/Chuồng: tap/kéo hơi lệch quanh ô nhỏ, ghost vẫn snap đúng ô mong muốn và OK/X hiện đúng.
- **Shop thu mua đá quý 01/07:** đã tạo nhánh `codex/gem-shop-fish-market-icons`, thêm `Shop_GemShop` dạng SellOnly whitelist 6 item `gem_*`, cập nhật `ShopDataGenerator` sinh/cập nhật 8 shop, và thêm filter `Cá`/`Đá quý` trong `ShopPopup`. Card/detail vẫn lấy icon từ `ItemDefinition.iconTexture`. **Cần Editor:** gắn `Shop_GemShop` vào `ShopZoneTrigger` hoặc `MerchantNPC` của quầy/NPC thu mua đá quý rồi test bán đá quý từ túi đồ để cộng Point đúng.
- **Icon gỗ/đá và toast item chung 01/07:** icon `Da`/`Go` trong `Assets/Sprites/icon/BoSungIcon/` đã được dùng cho `stone_01`/`wood_01`; `watering_water_01` dùng `NuocTuoi.png`. `ScreenToast` có helper item-icon chung và đã áp dụng cho câu cá, đào đá, chặt cây, múc nước, thu hoạch cây/thú, shop mua/bán, điểm danh và vòng quay. Build Mode hiển thị icon `Go`/`Da` ở pill vật liệu và chi phí từng ô xây.
- **Farm tile dùng model đất thật 01/07:** đã tắt `FarmTileMarker` viền màu trắng/vàng/xanh/cam và tắt fallback primitive cube/sphere/cylinder trong `FarmTile` mặc định. `Soil Visual`/`Plowed Visual` nay là nguồn model đất thường/đất đã cuốc; trạng thái gieo/tưới/chín giữ `plowedVisual` dưới cây. `FarmTile` hỗ trợ visual gán prefab asset như `DatDaCuoc`: tự instantiate thành child runtime, và nếu `Soil Visual` là chính object `DatThuong` thì chỉ tắt renderer đất thường chứ không tắt cả GameObject/FarmTile. Ô trồng đặt bằng Build Mode đã có `G - Hủy ô trồng` xác nhận 2 lần như hủy chuồng, menu xóa Build Mode bắt được mesh con và clear `BuildSurfaceCell`/save ngay. Cây ưu tiên `CropDefinition.cropPrefab`; nếu crop thiếu prefab sẽ không còn fallback màu trừ khi bật `createPrimitiveFallbackVisuals` để test prototype.
- **Tránh bàn phím mềm che input 01/07:** thêm `MobileKeyboardAvoidance` dùng chung cho UI Toolkit. Login/Register tự dịch panel lên khi focus username/password/email trên mobile; Chat dùng cùng helper để đứng trên bàn phím mềm và vẫn giữ offset Build Mode.
- **Đảo đào khoáng MVP 30/06:** đã mở nền code để chọn `mine` trên bản đồ và travel tới `MineScene`. `IslandTravelManager` có fallback `MineMap -> MineScene` để dữ liệu Inspector cũ không làm vỡ runtime. `FarmInteractionController` giữ câu cá chỉ ở `city`, nhưng đào đá nay cho phép ở `city` hoặc `mine`. `ResourceSpawner` hỗ trợ gắn prefab cây/đá, snap xuống nền, random lại vị trí khi tài nguyên hồi sinh, và spawn trong nhiều vùng `Collider` kiểm soát được thay vì chỉ spawn hình tròn. **Cần Editor:** thêm `Assets/_Project/_Scenes/MineScene.unity` vào Build Settings thay entry cũ `MineMap`, set island `mine` sceneName `MineScene`, đặt `ResourceSpawner` trong `MineScene` với `spawnerID = Mine`, `treeCount = 0`, `rockCount` theo mật độ test, bật `randomizePositionOnRespawn`, gắn `rockPrefab` nếu có. Với map méo/rộng, tạo vài `BoxCollider` trigger cao phủ vùng đất hợp lệ, kéo vào `ResourceSpawner > Spawn Areas`, bật `snapSpawnToGround` với ground mask riêng, rồi dùng context menu `Clear Saved Resource State` nếu cần rải lại theo vùng mới. Cập nhật sau: giới hạn 10 lượt/ngày và shop đá quý đã làm; nâng cuốc lv2/lv3 còn chờ UI/quyết định.
- **Polish 29/06:** đã đổi text hiển thị `POS` -> `Point`, `UPOS` -> `UPoint` ở UI/toast/log demo liên quan; giữ tên biến/API nội bộ `POS/UPOS` để không đụng logic kinh tế. Câu cá thành công có icon cá nổi/fade qua `ScreenToast.ShowInfoWithIcon`. Nước biển Farm/City đã sáng và xanh hơn trên `Assets/IgniteCoders/Simple Water Shader/Resources/Water_mat_01.mat` và mesh nước phụ City `Assets/Art/Environment/Materials/water.mat`.
- **Chăn nuôi 29/06:** khách đổi lại quyết định gia cầm. Gà/đà điểu/ngỗng/vịt vẫn lấy trứng theo chu kỳ, nhưng vụ cuối sẽ trả thịt theo Product 2 trong `VatNuoi2.md` (`chicken_meat_01`, `ostrich_meat_01`, `goose_meat_01`, `duck_meat_01`) và bán được ở Mini Garden.
- **Icon thịt gia cầm 29/06:** 4 item thịt gia cầm đã gắn icon mới từ `Assets/Sprites/icon/ThitGiaCam/`. Toast vụ cuối của `FarmAnimal` dùng `ScreenToast.ShowInfoWithIcon`; túi đồ và shop tự hiển thị icon qua `ItemDefinition.iconTexture`.
- **iOS/App Store Connect:** CodeMagic exported-Xcode workflow đã build được IPA và upload lên App Store Connect. Các vấn đề đã xử lý: tránh Unity license bằng workflow Xcode-only, signing/profile cho bundle `com.ywonder.greenfarm`, tạo đủ iOS app icon, chmod `process_symbols.sh`/`usymtool`, giữ `il2cpp.a`, bảo toàn IL2CPP binary bằng `.gitattributes`, bump build lên `0.1.1 (2)`. Cập nhật theo góp ý bên build: đã bỏ `submit_to_testflight: true`, nên Codemagic chỉ upload IPA; việc add build vào Internal Testing làm thủ công trong App Store Connect. Sau lỗi App Store Connect báo IPA vẫn là build `1`, đã bake trực tiếp `0.1.1 (2)` vào exported iOS project và thêm bước verify IPA version trước publish.
- **Hotfix iOS/App Store Connect mới nhất:** bên build báo cần tăng build number và bổ sung khai báo export compliance. Đã tăng bản kế tiếp lên `0.1.1 (4)`, thêm `ITSAppUsesNonExemptEncryption=false` trong `ios/Info.plist`, đồng thời cập nhật `codemagic.yaml` để sau mỗi lần Unity export CodeMagic tự ép lại `CFBundleVersion=4` và key export compliance này trước khi archive/upload.
- **Nếu tester báo không cài được iOS:** sau hotfix mới nhất, xác nhận họ đang cài bản TestFlight mới `0.1.1 (4)`, không phải bản cũ `0.1.0 (0)`, `0.1.1 (1)`, `0.1.1 (2)` hoặc `0.1.1 (3)`. Dung lượng TestFlight khoảng 309 MB, tạm để sau khi cài/chạy ổn mới tối ưu.
- **Cảnh báo worktree:** có thể còn file Unity/iOS generated dirty như `ios/Data`, `ios/Unity-iPhone.xcodeproj`, `ProjectSettings`, `AddressableAssetsData`, `.claude/`, `_Recovery`. Không stage/revert bừa nếu task không cần.
- **Cá mới 29/06 đã implement:** đã thêm 14 `ItemDefinition` cá mới trong `Assets/Resources/Items/`, gắn icon từ `Assets/Sprites/icon/CacLoaiCa/`, đổi reward câu cá sang random theo tier Point, và whitelist toàn bộ cá mới trong Fish Shop. `ItemDatabase.GetItem` có fallback load `Resources/Items/{id}` để shop/túi đồ/toast resolve item mới trước khi generator refresh `ItemDatabase.asset`.
- **Cutscene thuyền 29/06:** `Assets/_Project/Scripts/Cutscenes/BoatCutscene.cs` đã đổi failsafe từ cắt cứng 35 giây sang `effectiveCutsceneTimeout`: tính theo tổng quãng đường waypoint / `movementSpeed` + `cutsceneTimeoutBuffer`, rồi lấy lớn hơn `cutsceneTimeout`. Mục tiêu là cho thuyền đủ thời gian cập bờ, nhưng vẫn tự kết thúc nếu cutscene thật sự bị kẹt.
- **Đá quý 29/06:** `Assets/_Project/Docs_KichBan/CacLoaiDaQuy.md` đã ghi bảng đá quý khách chốt; đã thêm 6 item `gem_*.asset` với icon trong `Assets/Sprites/icon/CacLoaiDaQuy/`. Đào đá hiện giữ đá thường 100% với 10 rock, rồi roll thêm 1 đá quý theo tỉ lệ Ruby 1%, Amethyst 2%, Fire Quartz 5%, Green Calcite 12%, Orange Calcite 30%, Kyanite 50%; toast đào trúng dùng icon qua `ScreenToast.ShowInfoWithIcon`. Cập nhật sau: shop thu mua đá quý, giới hạn đào 10 lượt/ngày ở Unity, và API daily limit server-side đều đã có; nâng cuốc lv2/lv3 còn chờ UI/quyết định.
- **Biểu cảm 30/06:** popup biểu cảm trong chat/HUD đã bỏ 2 động tác ngoài thiết kế (`Laughing`, `Dancing`), chỉ giữ `Waving` và `Pointing`; icon nút nay lấy ảnh `Assets/Sprites/icon/BoSungIcon/VayTay.png` và `Assets/Sprites/icon/BoSungIcon/ChiTay.png` thay cho emoji text.
- **Hồi sinh tài nguyên 29/06:** gỗ/đá do `ResourceSpawner` quản lý giờ lưu mốc hồi sinh theo thời gian thật `respawnEndUnix`. Khi người chơi thoát app rồi mở lại, tài nguyên tự bù thời gian offline nếu đã qua đủ `respawnTimeSec`; save cũ chỉ có `respawnTimer` vẫn fallback đọc được.
- **Việc tiếp theo khách vừa giao còn lại:** chốt cách hiển thị/nâng cấp cuốc lv2/lv3 và test lại các flow đá quý/Gem Shop/daily limit trên Unity.

### Dữ liệu cá đang dùng trong gameplay
- 2 point: Cá cơm, Cá nục, Cá hồng.
- 4 point: Cá sư tử, Cá naso, Cá nhồng.
- 6 point: Cá sọc dưa, Cá khế, Cá mú.
- 10 point: Cá mặt quỷ, Cá heo biển.
- 15 point: Cá hoàng đế, Cá ngừ hoàng kim.
- 25 point: Cá rồng đỏ.
- Tỉ lệ câu từ cá giá trị cao xuống thấp: 2%, 4%, 7%, 17%, 25%, 45%.

### Dữ liệu đào đá đang dùng / cần chốt tiếp
- Ảnh 1: 2 point/viên, 4 viên, 50% đào trúng.
- Ảnh 2: 3 point/viên, 4 viên, 30% đào trúng.
- Ảnh 3: 6 point/viên, 3 viên, 12% đào trúng.
- Ảnh 4: 12 point/viên, 2 viên, 5% đào trúng.
- Ảnh 5: 500 point/viên, 1 viên, 2% đào trúng; nâng cấp cuốc lv2 tốn 250 point/lượt.
- Ảnh 6 ruby quý hiếm: 3000 point/viên, 1% đào trúng; nâng cấp cuốc lv3 tốn 1500 point.
- Mỗi ngày 10 lượt đào đã có local Unity và API backend server-side; nâng cuốc lv2/lv3 còn cần UI/quyết định.

### Gợi ý resume cho session mới
- Đọc `RULES.md`, `docs/CONTEXT_RECOVERY.md`, `task.md`, `CHANGELOG.md`.
- Cá mới đã xong; ưu tiên tìm hệ đào đá hiện tại trước khi sửa: `FarmInteractionController`, resource/item data/generator, shop/inventory liên quan.
- Hỏi anh nếu thiếu icon/ảnh cho đá/gem; không đoán bừa asset.

## 1. Cập nhật 24/06

> Nhánh đang dùng: `feat/animal-husbandry` (đã có thay đổi cục bộ touch-control/build-flow). Chi tiết xem `CHANGELOG.md` + `docs/CHANGELOG.md` + `task.md`.

- **Login/profile existing character:** `player_profile` thêm `characterCreated`; login nạp profile trước, account đã có nhân vật vào game luôn. `DemoRich01`-`DemoRich05` được coi là đã có nhân vật để tester không phải đặt tên/chọn giới tính.
- **Shop tab/header polish:** các tab chế độ/danh mục trong popup shop đã bỏ emoji icon, chỉ giữ chữ; card/panel chi tiết hàng hóa vẫn dùng icon ảnh từ `ItemDefinition.iconTexture/iconSprite`; title shop dài được giới hạn/căn giữa trong header để không tràn dưới pill Point.
- **Workshop icons:** popup Tiệm rèn render dụng cụ/nguyên liệu nâng cấp bằng icon ảnh; đã gắn `iconTexture` cho rìu/cuốc/cần câu/xô tưới/cuốc chim/gỗ/đá/sắt/quặng; bỏ `z-index` trong USS.
- **Quest popup icons:** danh sách nhiệm vụ bỏ emoji kiếm/quà/check; nhiệm vụ đã nhận thưởng dùng ô vuông có dấu tích visual như Hộp thư; reward slots dùng icon ảnh từ `ItemDatabase`/`BoSungIcon`.
- **Mailbox icons:** hộp thư đã đọc hiển thị dấu tích visual; badge quà dùng `Assets/Sprites/icon/SanPham/VatPham/giftbox.png`; attachment rewards dùng icon ảnh từ `ItemDatabase`/`BoSungIcon`.
- **Piggy bank icon cleanup:** popup Heo đất bỏ emoji icon ở balance/tab/gói/nút gửi/countdown; icon heo ở active/history dùng ảnh `Assets/Sprites/icon/BoSungIcon/Piggy.png`.
- **Event popup icon cleanup:** popup Sự kiện & Quà tặng bỏ icon trang trí ở tiêu đề, icon đồng hồ ở timer pill, icon emoji trên tab, và icon emoji trong card gói ưu đãi.
- **Attendance icons:** bảng điểm danh trong popup Sự kiện render icon ảnh cho Point/gỗ/ngày trống/thỏ từ `Assets/Sprites/icon`, và dùng icon `ItemDatabase` cho bắp ngô/bí ngô.
- **Lucky wheel icons:** vòng quay may mắn render icon ảnh cho phần thưởng từ `Assets/Sprites/icon`/`ItemDatabase`; tiêu đề, hub giữa vòng và nút QUAY bỏ emoji text.
- **Leaderboard icons/UI:** 5 tab `EXP/Level/Fashion/Pet/Rich` dùng icon ảnh từ `Assets/Sprites/icon/BoSungIcon/`; Level dùng `lv.png`; hạng 1/2/3 dùng huy chương vàng/bạc/đồng thật; Fashion/Pet/Rich hiện số thuần thay vì sao/Lv/kim cương.
- **Inventory icons:** `InventoryPopupController` hiển thị `ItemDefinition.iconTexture/iconSprite` trong card kho đồ và panel chi tiết; item chưa có ảnh vẫn fallback emoji/text.
- **HUD tiền tệ:** top-right giờ hiển thị `UPoint` đi cùng `Point`; tên nội bộ `POS/UPOS` trong `EconomyManager` vẫn giữ để tránh đổi logic lưu tiền.
- **Điều khiển mobile đã ổn định cho sprint/tap-hold/auto-run:** `PlayerController` có state sprint chung, `GameHUD` giữ đúng trạng thái sprint khi bấm hoặc giữ.
- **Camera touch chỉnh lại:** smoothing riêng cho touch, pitch clamp theo yêu cầu kiểm duyệt.
- **Build/chuồng đang hoàn thiện:** ghost preview là prefab mờ, rào có auto-connect; cài đặt búa animation khi đặt công trình.
- **Hệ chăn nuôi:** ô chuồng lấy theo cụm hàng rào (PenEnclosure), kiểm tra kích thước trước khi thả thú, popup thông tin con vật đã tích hợp.
- **Hỗ trợ tutorial flow mới:** NPC khó tính, đi theo hành vi và nhắc khéo (đã tránh spam thoại quá nhanh).
- **Còn dang dở ưu tiên:** hệ hiệu ứng thu thập bay vào túi đồ, hủy chuồng thu lại tài nguyên, trồng theo ô theo từng loại cây (sau khi hoàn tất phần chăn nuôi cơ bản).
- **Tốc độ demo trước build APK/Windows:** giữ `GameTimeConfig.SecondsPerGameDay = 60f` (1 ngày game = 60 giây thật), không đổi về 24h thật trước test chéo. Expected test nhanh: cây ngắn ngày ~60s sau tưới; Sa Chi/Sầu Riêng ~28 phút; Chanh dây ~90 phút; vịt 60s, gà 120s, dê/ngỗng 180s, đà điểu 360s, bò 420s.
- **NPC tutorial marker:** đã thay dấu chấm than primitive bằng prefab `Assets/_Project/Prefabs/ExclamationMark.prefab`; khi spawn gỡ collider con để không chắn ray/click.
- **Backend/VPS:** client hiện có khung REST (`BackendConfig`, `ApiClient`, `AuthService`, `PlayerProfileService`) nhưng mới phủ auth/profile/tutorialCompleted. Muốn demo VPS tối thiểu thì deploy `server/` stub và tạo `Assets/Resources/BackendConfig.asset` trỏ `baseUrl` về URL public; online thật cho POS/inventory/farm/cây/thú/server-time/IAP là phase backend riêng sau demo.

## 1. Cập nhật 20/06 (lịch sử)

> Nhánh lúc đó: `feat/animal-husbandry`. Chi tiết xem CHANGELOG mục 20/06 + task.md.

- **Điều khiển mobile (GameHUD):** joystick ảo di chuyển, nút Sprint giữ-để-chạy (fix Clickable nuốt event bằng **TrickleDown**), nút Jump, nút **X hủy hoạt ảnh**. Tương tác ngắm theo điểm chạm; nút gợi ý bấm/tap được (fix picking-mode cha Ignore).
- **Bỏ tính năng Vuốt ve.**
- **Build snap theo Ô ĐẤT THẬT** (`BuildSurfaceCell`, cube map = 0.8) thay lưới ảo. Tool Editor "sơn vùng" gắn hàng loạt. Gizmo hiện trạng ô (xanh lá=trống, đỏ=ô chuồng, xanh dương=có thú).
- **Hệ chuồng từ hàng rào (#6 XONG):** rào = hộp vuông trên 1 ô → **ô có rào = ô chuồng**. `PenEnclosure.FindPen` đếm cụm ô-rào liền nhau; ngắm/click → "Thả thú" → validate `penSlots` vs ô trống → thả hoặc báo lỗi (ScreenToast). Cần gắn `AnimalPrefabLibrary` (itemId→prefab thú).
- **Popup Thông tin con vật (#4 XONG):** giá/ô/thức ăn/sản phẩm; data 10 con từ bảng VatNuoi (chạy menu `Generate Animal Data`).
- **Thông tin con vật ở Shop + Túi đồ (XONG):** chèn "Thông tin nuôi" vào mô tả; tra qua `AnimalManager.LookupDefinition` (fallback Resources → chạy kể cả khi chưa gắn AnimalManager).
- **Dọn menu Build còn 3 mục** (Ruộng/Đường đá/Chuồng); **đường đá** map StoneSlab; **fix ghost luôn đỏ** (RaycastAll tìm BuildSurfaceCell, bỏ qua mesh nền); **loadout test** (`InventoryManager.GiveTestLoadout` + cờ).
- **Việc cần làm trong Editor:** gắn `BuildSurfaceCell` cho khối map ("sơn vùng"); 3 entry `BuildPrefabLibrary` (ruộng→Dirt, đường đá→StoneSlab, chuồng→Fence **stretch OFF**); gắn `AnimalPrefabLibrary` + prefab thú; gắn `AnimalManager`; chạy `Generate Animal Data`; gỡ component `PetInteraction` khỏi prefab thú.
- **HỆ NPC (đã lập task — xem task.md):** kịch bản "10+ NPC". ĐÃ CÓ: Guide NPC (tutorial) + 1 Merchant mẫu. CHƯA: shop keeper đa-NPC (data-driven), Maid VIP, Pet, NPC mỏ (vé/quặng), NPC câu cá, AI Chat NPC.
- **Còn dang dở (chăn nuôi):** hiệu ứng thu thập bay vào túi; hủy chuồng → hoàn tài nguyên; trồng từng ô ruộng. **Mobile còn:** #3 Mouse→Pointer, #4 camera 1 ngón, #5 safe area.

## 1. Bối cảnh phiên làm việc

**Sprint Demo Gameplay** — khách yêu cầu demo (chiều 14/06): nhân vật Nam/Nữ chạy/nhảy/bơi, tương tác (câu cá, vuốt ve thú, trồng cây, chặt cây), và **đi lại giữa đảo Nông trại ↔ Thành phố**. Demo chạy trong **Unity Editor (Play Mode)**. Ưu tiên: **ỔN ĐỊNH** các tương tác.

> Trạng thái dự án: phần lớn còn **mockup**, **offline** (PlayerPrefs, chưa Cloud Save), đang ở nhánh `feat/inventory-economy`. KHÔNG có QC chính thức (chỉ giữa user & AI).

## 2. Đã hoàn thành phiên này (chi tiết xem CHANGELOG mục 14/06)

- **Đi đảo (P1):** `IslandTravelManager` (additive scene) + nối Bản đồ + cổng `MapPortalTrigger`.
- **Tương tác động vật (P2):** anim Vuốt ve/Cho ăn chạy đúng (Petting/Feed).
- **Trồng cây chọn loại (P4):** chọn hạt trong túi → **múa trồng xong mới gieo** → cây model 3D lớn dần.
- **Chặt cây:** có anim TreeCutting khi giữ chuột, tầm chỉnh được, hết xoay ngang.
- **UX:** chuột tự trả khi mở popup (`UIPopupTracker`); gỡ phím F/R toàn cục; name tag độc lập scale; anim tự đo độ dài clip + tham số speed.

## 2b. Đã làm thêm 15/06 (chi tiết xem CHANGELOG mục 15/06)

- **Dụng cụ cầm tay (`EquipmentManager`):** tự sinh placeholder primitive gắn vào xương bàn tay; đủ Rìu/Cúp/Cuốc/Bình tưới/Cần câu/Túi hạt/Nắm cám; gán model thật vào ô là tự thay. Mỗi hành động cầm đúng đồ nghề.
- **Cây ĐỔ GỤC:** chặt/đập xong cây xoay quanh đáy đổ nghiêng rồi mới ẩn (`HarvestableResource.FallAndHide`).
- **Câu cá có DÂY + PHAO (`FishingLineController`):** dây (LineRenderer) + phao bay vòng cung ra nước → nổi → tự thu về cần → ẩn. Chỉnh `castDelay`/`reelDelay`/`reelDuration` cho khớp animation.
- **`AnimEventToolHider`:** relay cho Animation Event (ẩn cây giống lúc cắm / bung dây lúc vung cần) — gắn lên object có Animator.
- **Animation lao động:** state chặt/đào/cuốc/tưới dùng chung **`TreeCuttingV4`**; có thêm anim `Watering`, `Plant`, `Planting`.

## 3. Việc CẦN SETUP trong Editor (user làm — AI không vào Editor được)

- [ ] Tạo `CityScene` (Plane tạm) + thêm Build Settings + cấu hình `IslandTravelManager` (spawn từng đảo) + đặt cổng portal 2 bên.
- [ ] Gán `Crop Prefab` cho `Crop_cabbage_seed_01` và `Crop_corn_seed_01` (carrot xong rồi).
- [ ] Dựng vài ô `FarmTile` (GameObject rỗng + Box Collider **Is Trigger** + bật `Use Custom Crop Models`) rải trên mảnh đất; mảnh đất dài chỉ để trang trí.
- [ ] Kiểm tra Animator prefab Nam có state `Petting`/`Feed`; state `Planting` trỏ đúng clip.

## 4. Việc CÒN LẠI (code)

- [ ] **Restyle popup "Xem thông tin" động vật** cho khớp Cozy Dark Palia (#2 — chưa làm).
- [ ] **Khung `ShopkeeperNPC`** (NPC mua/bán/nâng cấp) để mai gắn model 3D — nhớ nối `UIPopupTracker` cho Workshop/popup mới.
- [ ] (Tùy chọn) Giảm `growthTimeSec` trong `Assets/Resources/Items/Crop_*.asset` cho cây lớn nhanh khi demo.
- [ ] Câu cá ở Thành phố: chỉ cần đặt vùng nước + tái dùng `FishingSpot` (script sẵn sàng).

## 5. Lưu ý kỹ thuật quan trọng

- `FarmInteractionController` KHÔNG nằm trên nhân vật → lấy nhân vật dùng **`PlayerController.Instance`**, KHÔNG dùng `GetComponent`.
- Model Nữ (artist A) đang scale 2 (mesh nhỏ); Nam (artist B) scale 1. Nên chuẩn hoá Nữ về scale 1 qua FBX Scale Factor **SAU demo** (tránh lỗi scale ngầm).
- `Current Crop` trong FarmTile là **auto-assigned lúc runtime**, set tay vô tác dụng — loại cây do hạt người chơi chọn.

> [!TIP]
> **Cho AI mới:** Script tương tác chính là `Assets/_Project/Scripts/Environment/FarmInteractionController.cs` (raycast tâm ngắm, xử lý cuốc/trồng/tưới/thu hoạch/chặt/câu/click NPC). Trồng trọt: `FarmTile.cs` + `CropDatabase`/`CropDefinition` (trong `Assets/Resources/`). Chuyển đảo: `IslandTravelManager.cs`. Animation hành động: `PlayerController.PlayActionAnimation()` (tự đo độ dài clip + speed) — gọi `EquipmentManager.ShowTool()` để hiện dụng cụ trên tay. Dây câu: `FishingLineController.cs` (timed cast/reel). Animation Event: `AnimEventToolHider.cs` (gắn trên object có Animator). Đọc thêm `docs/MEMORY.md` mục 53–61 cho bài học phiên này.
## Update 2026-07-01: login profile cache isolation

- Hotfix Demo05/Demo02 account bleed: `PlayerProfileService` now saves local profile cache per signed-in `AuthService.UserId` or `Username` instead of one shared `YW_Profile_Cache`. `AuthService` resets the runtime profile when identity changes and lets the profile service accept `player_profile` from `/auth/web-login` if present.
- Follow-up HUD fix: `GameHUDController` refreshes the top-left profile name from the active session after gameplay starts, prioritizing `GameManager.playerName` so the corner card matches the world `FloatingNameTag`.
- Test note: after this change, test switching DemoRich02 -> DemoRich05 in the same Editor/app session, including a case where `/player/profile` is unavailable, and confirm the name/profile no longer falls back to DemoRich02.
