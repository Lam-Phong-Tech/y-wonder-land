# 🔄 Context Recovery — Y WONDER GREEN FARM (Bá Chủ Khu Rừng 3D)

# Dùng khi bắt đầu conversation MỚI với AI

## Cách dùng

Copy đoạn prompt bên dưới → paste vào chat mới → AI sẽ tự đọc và hiểu dự án.

---

## Cập nhật gần nhất

- **Cập nhật 07/07/2026 - workflow build mới bước 3:** đã nối ghost preview vào ô viền trắng trước mặt. Khi người chơi mở build list và chọn Ruộng/Đường đá/Chuồng, `GhostPlacementController` tự snap + pin ghost lên `FrontBuildCellSelector.CurrentCell`; người chơi chỉ cần bấm OK để xác nhận hoặc X để hủy, không cần tap màn hình để ghim như flow cũ. `ConfirmPlacement()` giờ trả kết quả thật để UI không đóng nếu thiếu vật liệu hoặc ô bị chiếm. **Cần test:** build APK/EXE, chọn item khi có ô viền trắng; ghost phải nằm đúng ô đó, OK đặt đúng ô, X hủy sạch, ô bị chiếm/thiếu vật liệu không đặt sai.
- **Cập nhật 07/07/2026 - workflow build mới bước 2:** sau khi anh test và xác nhận viền sáng ổn, đã chuyển nút búa/build xuống cụm tay phải phía trên nút Jump. `BuildModeOverlayController` mặc định không còn bật camera top-down và không ẩn GameHUD khi mở build list, để giữ góc nhìn nhân vật và vẫn điều khiển được joystick/camera. Có thêm cờ Inspector `useTopDownBuildCamera` và `hideGameHudWhileOpen` nếu cần bật lại flow cũ để debug. Bước ghost cố định đã được nối ở cập nhật bước 3 phía trên.
- **Cập nhật 07/07/2026 - workflow build mới bước 1:** theo chốt mới từ anh/khách, build sẽ chuyển dần sang kiểu nhân vật đứng camera thường, ô ngay trước mũi chân phát viền trắng theo hướng mặt nhân vật; không chuyển camera top-down và không kéo ghost tự do như build mode cũ. Đã thêm `FrontBuildCellSelector` runtime để chọn `BuildSurfaceCell` phía trước người chơi và vẽ viền trắng trên mặt ô. Anh đã test và xác nhận viền sáng ổn; các bước còn lại nằm ở cập nhật bước 2 phía trên.
- **Cập nhật 07/07/2026 - hotfix joystick mobile không xoay camera:** đã sửa hướng mobile controls theo yêu cầu khách: `InputSystem_Actions.inputactions` đổi binding `Look` từ `<Pointer>/delta` sang `<Mouse>/delta` để touch toàn màn hình không còn bị camera đọc trực tiếp; mobile touch-look chỉ đi qua `GameHUDController`/`LookZone`. `GameHUDController` cũng chặn pointer joystick khỏi vùng xoay camera, stop propagation ở down/move/up/capture-out, và vẫn cho phép dùng 2 ngón: trái di chuyển bằng joystick, phải kéo `LookZone` để xoay. Sau test đầu, anh báo trục dọc bị ngược; `ThirdPersonCamera` đã đảo touch pitch để vuốt lên = camera ngẩng lên, vuốt xuống = camera cúi xuống, không đổi mouse PC. Sau feedback tiếp theo, đã bỏ cơ chế joystick kéo mạnh/giữ lâu tự bật chạy nhanh; nút Sprint trên HUD chỉ còn là nút bật/tắt auto-run chủ động. **Cần test:** build APK/thiết bị thật, kéo joystick bên trái không được làm camera/map xoay, kéo hết biên vẫn đi tốc độ thường; kéo nửa phải vẫn xoay camera bình thường và trục dọc đúng cảm giác; bấm nút Sprint mới bật/tắt auto-run.
- **Cập nhật 06/07/2026 - tạm gác backend, quay lại chỉnh sửa game:** anh quyết định tạm dừng chuỗi backend sau khi đã có nền mock/API/dashboard/realtime. `task.md` đã được đưa mục ưu tiên mới lên đầu: chờ danh sách task chỉnh sửa game từ anh, sau đó tách checklist theo gameplay/UI/scene/data/build. Các loop backend còn lại vẫn giữ trong `task.md` nhưng chuyển sang trạng thái `[~]` để quay lại sau khi xong nhóm chỉnh sửa game.
- **Cập nhật 06/07/2026 - chốt phỏng vấn backend và roadmap yêu cầu sếp:** anh đã chốt thêm các quyết định backend quan trọng: web hiện đăng nhập bằng email/số điện thoại/password; khách phải có tài khoản trước khi chơi; 1 account = 1 nhân vật; nhiều máy cùng account dùng chung state server; account bị khóa/xóa mềm trên web thì game cũng bị chặn. `Point` vừa là tiền trong game vừa là tiền nạp từ web, nhưng scope mới chốt là MVP sắp tới **chưa cần nạp/rút**, ưu tiên online + realtime cho khách hàng; web wallet/top-up/spend chuyển sang phase sau. Yêu cầu trước mắt từ sếp được ghi rõ trong `docs/WEB_GAME_BACKEND_JOURNEY.md`: khách đăng nhập bằng account web/cấp sẵn, vào online realtime để chat/tương tác trên đảo công cộng; farm không thuộc realtime public phase này. Roadmap mới bắt đầu từ account bridge -> realtime public islands -> hạ tầng demo online -> state sync tối thiểu; shop/economy/farm server-authoritative làm sau khi lát realtime pass.
- **Cập nhật 06/07/2026 - test realtime không phụ thuộc web:** web hiện đang sập/chưa có tài khoản web thật, nên dùng `WEB_AUTH_MODE=mock` để test bằng account cấp sẵn. Đã thêm `server/realtimeSmokeTest.js` và `npm.cmd run test:realtime`. Smoke test local đã pass với `DemoRealtime01/demo`, `DemoRealtime02/demo`, `DemoRealtime03/demo`: 2 client join `city`, client thứ ba không join room vẫn nhận/gửi chat global, gửi `player_state`, và join `farm` bị chặn `ROOM_NOT_SHARED`. Unity `RealtimeClient` nay giữ WebSocket cho chat khi đang gameplay, nhưng chỉ join room `city`/`mine` để hiện remote player nên farm vẫn là đảo riêng.
- **Cập nhật 06/07/2026 - làm rõ hành trình Web account -> Game backend:** đã thêm `docs/WEB_GAME_BACKEND_JOURNEY.md` để gỡ điểm mơ hồ trong kịch bản: web hiện có là nguồn tài khoản, game backend map `web_user_id -> playerId`, Unity chỉ gọi game-server và không giữ `GAME_API_SECRET`. Tài liệu ghi rõ hiện trạng backend MVP đã có API/dashboard/data mẫu nhưng Unity shop/economy/inventory vẫn còn local `PlayerPrefs`, nên mua/bán trong game chưa tự đổi dashboard backend. Sau scope mới, loop gần nhất không phải nạp/rút hay shop hoàn chỉnh mà là account bridge + realtime public islands; bootstrap economy/inventory và shop buy/sell server-authoritative làm sau khi lát realtime pass. **Phase sau:** tiền nạp từ web để dùng trong game phải gọi API web/server-side wallet; còn cần chốt `Point`/`UPoint`, ledger cuối cùng và endpoint spend/reserve.
- **Cập nhật 06/07/2026 - backend storage adapter + daily limits server-side:** anh chọn hướng backend khi làm ở nhà, tạm gác public `api.ywonder.net`/máy case. `server/store.js` đã thành storage facade có `JsonStore` cho dev/local và chọn mode bằng `STORE_MODE=json|postgres`; thêm `server/postgresStore.js` scaffold để route API giữ interface ổn định trước khi viết query PostgreSQL thật. Có dashboard backend local tại `http://127.0.0.1:3000/admin` để xem/tạo/sửa/xóa dữ liệu demo trong JSON store. `server/schema.sql` đã có nhóm bảng tối thiểu gồm `game_players`, `player_profiles`, `player_economy`, `player_inventory`, `player_farm_state`, `player_daily_limits`, `game_transactions`. `/player/bootstrap` trả thêm `daily_limits`; thêm `GET /player/daily-limits` và `POST /player/daily-limits/consume`. `economy/apply`, `inventory/adjust`, `daily-limits/consume` đều hỗ trợ `idempotency_key` để retry không cộng đôi tiền/item/lượt. Đã smoke test Node với data file tạm: đào mỏ 10 lượt còn 0, lần 11 bị chặn, economy/inventory retry không nhân đôi. **Còn tiếp:** viết query thật cho `STORE_MODE=postgres` khi chốt driver/DB host, rồi nối Unity đọc `daily_limits`/economy/inventory từ bootstrap khi online.
- **Cập nhật 01/07/2026 - web auth thật + realtime MVP:** bên web đã bàn giao `POST https://api.ywonder.net/api/game/auth`, cần `Authorization: Bearer <GAME_API_SECRET>` và body `{ username, password }`, trả cả camelCase/snake_case `{ ok, userId/user_id, username, refCode/ref_code, fullName/full_name, gameToken/game_token, tokenType, expiresIn/expires_in }`; `GET /api/game/balance` và `POST /api/game/credit` cũng sẵn sàng phía web. `gameToken` là JWT chuẩn HS256, payload `{ sub, uid, username, iat, exp }`, `sub/uid = web userId`; game-server verify bằng `jsonwebtoken` với `WEB_AUTH_SECRET` hoặc `GAME_API_SECRET`. `GAME_API_SECRET` chỉ đặt ở env game-server, không đưa vào Unity. `server/webAuthProvider.js` đã đổi sang contract này khi `WEB_AUTH_MODE=http`, mặc định login URL là `https://api.ywonder.net/api/game/auth`; nếu SSL `api.ywonder.net` còn kẹt có thể override tạm `WEB_AUTH_LOGIN_URL=https://ywonder.net/api/game/auth`. Unity `AuthService` thử `/auth/web-login` trước rồi mới fallback `/auth/login` dev cũ. Realtime MVP đã thêm `server/realtimeServer.js` dùng WebSocket `ws` tại `/realtime` và `/game-api/realtime`, rooms chung `city,mine`, max 20 người/room, chat toàn server, presence, vị trí/yaw/Idle-Walk-Run, emote `Waving`/`Pointing`. Unity có `RealtimeClient` tự tạo runtime, giữ kết nối WebSocket để chat global trong gameplay, và chỉ join room khi đang ở `city` hoặc `mine`; remote player dùng prefab nhân vật hiện tại nhưng disable input/collider. **Cần infra:** Nginx/Caddy phải proxy WebSocket Upgrade cho `/realtime`; nếu chỉ proxy HTTP thường thì REST chạy nhưng chat/remote player không kết nối.
- **Cập nhật 01/07/2026 - điều hướng backend public + Game API MVP:** theo hướng sếp chốt, web giữ VPS riêng còn game API dùng `api.ywonder.net` trên máy vật lý. Unity đã trỏ `Assets/Resources/BackendConfig.asset` tới `https://api.ywonder.net`. DNS của `ywonder.net` và `api.ywonder.net` hiện vẫn đang trỏ `45.119.83.233`; kiểm tra từ máy này chưa kết nối được TCP `80`, `443` hoặc `3000` tới endpoint public mới. `server/index.js` đã hỗ trợ route local và route legacy `/game-api`, thêm `/health`, và thêm `server/Caddyfile.example` + `server/DEPLOY_WINDOWS.md` để cấu hình Caddy/Windows. Trong lúc chờ Web API thật, server đã có `server/webAuthProvider.js` với `WEB_AUTH_MODE=mock/http`, endpoint `POST /auth/web-login`, `GET /player/bootstrap`, economy/inventory/farm-state MVP, và `server/schema.sql` làm schema PostgreSQL mục tiêu.
- **Cập nhật 01/07/2026 - vòng quay may mắn 12 múi:** `EventPopupController` đã đổi vòng quay từ kiểu icon rải quanh vòng tối sang nền 12 múi màu tạo runtime. Mỗi múi nay chỉ hiển thị icon item đang có trong `ItemDatabase`; đã bỏ tên item và số lượng trong múi. Giữ nguyên 12 phần thưởng hiện tại và weight/tỉ lệ quay thưởng; riêng `Chúc may mắn lần sau` để ô trống, không hiện icon vòng quay. `BtnSpin` ở tâm vòng dùng icon mới `Assets/Sprites/icon/BoSungIcon/arrowforspin.png` làm nút bấm quay, không còn chữ `QUAY/HẾT/...` runtime đè lên icon; footer chỉ còn số lượt còn lại. **Cần test Unity:** mở Sự kiện -> Vòng quay, kiểm vòng không méo khi xoay, đủ 12 múi, ô may mắn trống, icon `Spin` ở tâm rõ và bấm được, bấm quay dừng đúng thưởng, trừ lượt và trao item/toast như cũ.
- **Cập nhật 01/07/2026 - Build Mode dễ bấm hơn trên mobile:** `GhostPlacementController` có assist riêng cho touch: điểm raycast được nâng lên trên ngón tay (`touchAimOffsetPixels = 90`) và nếu tap/kéo lệch khỏi collider ô nhỏ thì chọn `BuildSurfaceCell` gần nhất trong bán kính màn hình (`touchAssistRadiusPixels = 96`). `BuildModeOverlayController` truyền rõ input là touch hay mouse, nên PC/mouse vẫn giữ raycast chính xác như cũ. **Cần test mobile:** chọn Ruộng/Đường đá/Chuồng, tap/kéo hơi lệch quanh ô nhỏ và xác nhận ghost snap đúng ô mong muốn, OK/X hiện đúng.
- **Cập nhật 01/07/2026 - shop thu mua đá quý:** đã tạo nhánh `codex/gem-shop-fish-market-icons`, thêm `Shop_GemShop` dạng SellOnly whitelist 6 item `gem_*`, và cập nhật `ShopDataGenerator` để sinh/cập nhật tổng 8 shop. `ShopPopup` có thêm filter `Cá` (`food`) và `Đá quý` (`materials`); card/detail vẫn lấy icon từ `ItemDefinition.iconTexture`. **Cần Editor:** gắn `Shop_GemShop` vào `ShopZoneTrigger` hoặc `MerchantNPC` ở quầy/NPC thu mua đá quý muốn dùng, rồi test bán đá quý từ túi đồ để cộng Point đúng.
- **Cập nhật 01/07/2026 - icon gỗ/đá và toast item chung:** icon `Da`/`Go` trong `Assets/Sprites/icon/BoSungIcon/` đã được dùng cho `stone_01`/`wood_01`; `watering_water_01` dùng `NuocTuoi.png`. `ScreenToast` có helper item-icon chung và đã áp dụng cho câu cá, đào đá, chặt cây, múc nước, thu hoạch cây/thú, shop mua/bán, điểm danh và vòng quay. Build Mode hiển thị icon `Go`/`Da` ở pill vật liệu và chi phí từng ô xây.
- **Cập nhật 01/07/2026 - Farm tile dùng model đất thật:** đã tắt `FarmTileMarker` viền màu trắng/vàng/xanh/cam và tắt fallback primitive cube/sphere/cylinder trong `FarmTile` mặc định. `Soil Visual`/`Plowed Visual` nay là nguồn model đất thường/đất đã cuốc; trạng thái gieo/tưới/chín giữ `plowedVisual` dưới cây. `FarmTile` hỗ trợ visual gán prefab asset như `DatDaCuoc`: tự instantiate thành child runtime, và nếu `Soil Visual` là chính object `DatThuong` thì chỉ tắt renderer đất thường chứ không tắt cả GameObject/FarmTile. Ô trồng đặt bằng Build Mode đã có `G - Hủy ô trồng` xác nhận 2 lần như hủy chuồng, menu xóa Build Mode bắt được mesh con và clear `BuildSurfaceCell`/save ngay. Cây ưu tiên `CropDefinition.cropPrefab`; nếu crop thiếu prefab sẽ không còn fallback màu trừ khi bật `createPrimitiveFallbackVisuals` để test prototype.
- **Cập nhật 01/07/2026 - tránh bàn phím mềm che input:** thêm `MobileKeyboardAvoidance` dùng chung cho UI Toolkit. Login/Register tự dịch panel lên khi focus username/password/email trên mobile; Chat dùng cùng helper để đứng trên bàn phím mềm và vẫn giữ offset Build Mode.
- **Cập nhật 30/06/2026 - đảo đào khoáng MVP:** đã mở nền code để chọn `mine` trên bản đồ và travel tới `MineScene`. `IslandTravelManager` có fallback `MineMap -> MineScene` để dữ liệu Inspector cũ không làm vỡ runtime. `FarmInteractionController` giữ câu cá chỉ ở `city`, nhưng đào đá nay cho phép ở `city` hoặc `mine`. `ResourceSpawner` hỗ trợ gắn prefab cây/đá, snap xuống nền, random lại vị trí khi tài nguyên hồi sinh, và spawn trong nhiều vùng `Collider` kiểm soát được thay vì chỉ spawn hình tròn. **Cần Editor:** thêm `Assets/_Project/_Scenes/MineScene.unity` vào Build Settings thay entry cũ `MineMap`, set island `mine` sceneName `MineScene`, đặt `ResourceSpawner` trong `MineScene` với `spawnerID = Mine`, `treeCount = 0`, `rockCount` theo mật độ test, bật `randomizePositionOnRespawn`, gắn `rockPrefab` nếu có. Với map méo/rộng, tạo vài `BoxCollider` trigger cao phủ vùng đất hợp lệ, kéo vào `ResourceSpawner > Spawn Areas`, bật `snapSpawnToGround` với ground mask riêng, rồi dùng context menu `Clear Saved Resource State` nếu cần rải lại theo vùng mới. Cập nhật sau: giới hạn 10 lượt/ngày và shop đá quý đã làm; nâng cuốc lv2/lv3 còn chờ UI/quyết định.
- **Cập nhật 29/06/2026 - polish trước khi thêm dữ liệu cá/đá:** đã đổi toàn bộ text hiển thị tiền từ `POS` sang `Point` và `UPOS` sang `UPoint` ở UI/toast/log demo liên quan; giữ nguyên tên biến/API nội bộ `POS/UPOS` để không đụng logic kinh tế. Câu cá thành công giờ có icon cá nổi/fade cùng toast qua `ScreenToast.ShowInfoWithIcon`. Nước biển Farm/City đã chỉnh sáng hơn, xanh hơn trên `Assets/IgniteCoders/Simple Water Shader/Resources/Water_mat_01.mat` và mesh nước phụ City `Assets/Art/Environment/Materials/water.mat`.
- **Cập nhật 29/06/2026 - chăn nuôi:** khách đổi lại quyết định gia cầm. Gà/đà điểu/ngỗng/vịt vẫn lấy trứng theo chu kỳ, nhưng vụ cuối sẽ trả thịt theo Product 2 trong `VatNuoi2.md` (`chicken_meat_01`, `ostrich_meat_01`, `goose_meat_01`, `duck_meat_01`) và bán được ở Mini Garden.
- **Cập nhật 29/06/2026 - icon thịt gia cầm:** 4 item thịt gia cầm đã gắn icon mới từ `Assets/Sprites/icon/ThitGiaCam/`. Toast vụ cuối của `FarmAnimal` dùng `ScreenToast.ShowInfoWithIcon`; túi đồ và shop tự hiển thị icon qua `ItemDefinition.iconTexture`.
- **Handoff 29/06/2026 - iOS/App Store Connect:** CodeMagic exported-Xcode workflow đã build được IPA và upload lên App Store Connect. Các lỗi đã xử lý gồm Unity license bypass bằng workflow Xcode-only, signing/profile `com.ywonder.greenfarm`, app icon iOS đầy đủ, executable bit cho `process_symbols.sh`/`usymtool`, giữ `il2cpp.a`, bảo toàn binary IL2CPP qua `.gitattributes`, và bump build lên `0.1.1 (2)`. Cập nhật theo góp ý bên build: đã bỏ `submit_to_testflight: true`, nên Codemagic chỉ upload IPA; việc add build vào Internal Testing làm thủ công trong App Store Connect. Sau lỗi App Store Connect báo IPA vẫn là build `1`, đã bake trực tiếp `0.1.1 (2)` vào exported iOS project và thêm bước verify IPA version trước publish. Sau hotfix mới nhất, build cần test/publish là `0.1.1 (4)`, không phải bản cũ `0.1.0 (0)`, `0.1.1 (1)`, `0.1.1 (2)` hoặc `0.1.1 (3)`.
- **Hotfix iOS/App Store Connect mới nhất:** bên build báo cần tăng build number và bổ sung khai báo export compliance. Đã tăng bản kế tiếp lên `0.1.1 (4)`, thêm `ITSAppUsesNonExemptEncryption=false` trong `ios/Info.plist`, đồng thời cập nhật `codemagic.yaml` để sau mỗi lần Unity export CodeMagic tự ép lại `CFBundleVersion=4` và key export compliance này trước khi archive/upload.
- **Lưu ý iOS size:** TestFlight hiển thị khoảng 309 MB. Tạm chấp nhận để qua bước cài/chạy trước; tối ưu dung lượng là task riêng sau, cần audit `Payload/YWONDERGREENFARM.app/Data`, `Frameworks`, `resources.assets`, `sharedassets*.assets`.
- **Repo state cần cẩn thận:** branch chính làm việc là `dev`, main đã được merge các patch iOS gần nhất. Worktree có thể còn nhiều file Unity/iOS generated dirty (`ios/Data`, `ios/Unity-iPhone.xcodeproj`, `ProjectSettings`, `AddressableAssetsData`, `.claude/`, `_Recovery`). Không stage/revert bừa các file này nếu task không cần.
- **Cập nhật 29/06/2026 - cá mới đã implement:** đã thêm 14 `ItemDefinition` cá mới trong `Assets/Resources/Items/`, gắn icon từ `Assets/Sprites/icon/CacLoaiCa/`, đổi reward câu cá sang random theo tier Point, và whitelist toàn bộ cá mới trong Fish Shop. `ItemDatabase.GetItem` có fallback load `Resources/Items/{id}` để shop/túi đồ/toast resolve item mới trước khi generator refresh `ItemDatabase.asset`.
- **Cập nhật 29/06/2026 - cutscene thuyền:** `Assets/_Project/Scripts/Cutscenes/BoatCutscene.cs` đã đổi failsafe từ cắt cứng 35 giây sang `effectiveCutsceneTimeout`: tính theo tổng quãng đường waypoint / `movementSpeed` + `cutsceneTimeoutBuffer`, rồi lấy lớn hơn `cutsceneTimeout`. Mục tiêu là cho thuyền đủ thời gian cập bờ, nhưng vẫn tự kết thúc nếu cutscene thật sự bị kẹt.
- **Cập nhật 29/06/2026 - đá quý:** `Assets/_Project/Docs_KichBan/CacLoaiDaQuy.md` đã ghi bảng đá quý khách chốt; đã thêm 6 item `gem_*.asset` với icon trong `Assets/Sprites/icon/CacLoaiDaQuy/`. Đào đá hiện giữ đá thường 100% với 10 rock, rồi roll thêm 1 đá quý theo tỉ lệ Ruby 1%, Amethyst 2%, Fire Quartz 5%, Green Calcite 12%, Orange Calcite 30%, Kyanite 50%; toast đào trúng dùng icon qua `ScreenToast.ShowInfoWithIcon`. Cập nhật sau: shop thu mua đá quý, giới hạn đào 10 lượt/ngày ở Unity, và API daily limit server-side đều đã có; nâng cuốc lv2/lv3 còn chờ UI/quyết định.
- **Cập nhật 30/06/2026 - bảng biểu cảm:** popup biểu cảm trong chat/HUD đã bỏ 2 động tác ngoài thiết kế (`Laughing`, `Dancing`), chỉ giữ `Waving` và `Pointing`; icon nút nay lấy ảnh `Assets/Sprites/icon/BoSungIcon/VayTay.png` và `Assets/Sprites/icon/BoSungIcon/ChiTay.png` thay cho emoji text.
- **Cập nhật 29/06/2026 - hồi sinh tài nguyên:** gỗ/đá do `ResourceSpawner` quản lý giờ lưu mốc hồi sinh theo thời gian thật `respawnEndUnix`. Khi người chơi thoát app rồi mở lại, tài nguyên tự bù thời gian offline nếu đã qua đủ `respawnTimeSec`; save cũ chỉ có `respawnTimer` vẫn fallback đọc được.
- **Dữ liệu cá đang dùng trong gameplay:**
  - Cá 2 point: Cá cơm, Cá nục, Cá hồng.
  - Cá 4 point: Cá sư tử, Cá naso, Cá nhồng.
  - Cá 6 point: Cá sọc dưa, Cá khế, Cá mú.
  - Cá 10 point: Cá mặt quỷ, Cá heo biển.
  - Cá 15 point: Cá hoàng đế, Cá ngừ hoàng kim.
  - Cá 25 point: Cá rồng đỏ.
  - Tỉ lệ câu từ cá giá trị cao xuống thấp: 2%, 4%, 7%, 17%, 25%, 45%.
- **Việc tiếp theo còn lại từ khách:** chốt cách hiển thị/nâng cấp cuốc lv2/lv3 và test lại các flow đá quý/Gem Shop/daily limit trên Unity.
  - Dữ liệu đào đá đã áp vào gameplay: ảnh 1 = 2 point/viên, 4 viên, 50%; ảnh 2 = 3 point/viên, 4 viên, 30%; ảnh 3 = 6 point/viên, 3 viên, 12%; ảnh 4 = 12 point/viên, 2 viên, 5%; ảnh 5 = 500 point/viên, 1 viên, 2%, nâng cuốc lv2 tốn 250 point/lượt; ảnh 6 ruby = 3000 point/viên, 1%, nâng cuốc lv3 tốn 1500 point.
  - Unity local đã có 10 lượt đào/ngày; backend 06/07 đã có API `daily_limits` để đưa giới hạn này lên server khi online.
- **Gợi ý triển khai tiếp:** đọc `RULES.md` trước, rồi đọc file này, `task.md`, `CHANGELOG.md`; cá mới đã xong nên ưu tiên tìm hệ mining/đào đá hiện tại (`FarmInteractionController`, resource/item data/generator/shop/inventory nếu có) trước khi thêm bảng đá/gem.
- Login/profile: `player_profile` có thêm `characterCreated`; login nạp profile trước, nếu đã có nhân vật thì bỏ qua Character Select và vào game. `DemoRich01`-`DemoRich05` được coi là đã có nhân vật để tester không phải đặt tên/chọn giới tính; account mới/chưa tạo nhân vật vẫn vào màn tạo nhân vật lần đầu.
- Popup shop: các tab chế độ/danh mục đã bỏ emoji icon, chỉ giữ chữ; icon ảnh hàng hóa trong card và panel chi tiết vẫn lấy từ `ItemDefinition.iconTexture/iconSprite`; title shop dài được giới hạn/căn giữa trong header để không tràn dưới pill Point.
- Popup Tiệm rèn: dụng cụ/nguyên liệu nâng cấp chuyển sang icon ảnh; đã gắn `iconTexture` cho rìu/cuốc/cần câu/xô tưới/cuốc chim/gỗ/đá/sắt/quặng và bỏ `z-index` trong USS.
- Popup Nhiệm vụ: bỏ emoji kiếm/quà/check cũ trong danh sách; nhiệm vụ đã nhận thưởng dùng ô vuông có dấu tích visual như Hộp thư; ô phần thưởng dùng icon ảnh từ `ItemDatabase`/`BoSungIcon`.
- Hộp thư: ô thư đã đọc hiển thị dấu tích visual, badge quà dùng `Assets/Sprites/icon/SanPham/VatPham/giftbox.png`, phần thưởng đính kèm dùng icon ảnh từ `ItemDatabase`/`BoSungIcon`.
- Popup Heo đất đã bỏ emoji icon ở balance pill, tab, gói gửi, nút gửi, countdown title; icon heo ở trạng thái đang gửi/lịch sử gửi dùng ảnh `Assets/Sprites/icon/BoSungIcon/Piggy.png`.
- Popup Sự kiện & Quà tặng đã bỏ icon trang trí ở tiêu đề, icon đồng hồ ở timer pill, icon emoji trên các tab, và icon emoji trong các card gói ưu đãi.
- Bảng điểm danh của popup Sự kiện đã chuyển sang icon ảnh: Point/gỗ/ngày trống/thỏ lấy từ `Assets/Sprites/icon`, bắp ngô/bí ngô lấy theo icon `ItemDatabase`.
- Vòng quay may mắn đã chuyển các phần thưởng sang icon ảnh từ `Assets/Sprites/icon`/`ItemDatabase`; tiêu đề, hub giữa vòng và nút QUAY không còn dùng emoji text.
- Leaderboard đã thay icon ảnh cho 5 tab `EXP/Level/Fashion/Pet/Rich` từ `Assets/Sprites/icon/BoSungIcon/`; Level dùng `lv.png`; hạng 1/2/3 dùng huy chương vàng/bạc/đồng thật; Fashion/Pet/Rich hiện số thuần thay vì sao/Lv/kim cương.
- Kho đồ giờ hiển thị icon ảnh từ `ItemDefinition.iconTexture/iconSprite` cho cả card và panel chi tiết; item chưa có ảnh vẫn fallback emoji/text.
- HUD top-right now shows visible `Point` and `UPoint` labels.
- `EconomyManager` still keeps internal `POS/UPOS` events/helpers (`OnUPOSChanged`, `AddUPOS`, `SpendUPOS`) to avoid risky economy/persistence renames.

## Prompt khởi động (copy từ đây)

```
Tôi đang phát triển game Unity 3D online tên Y WONDER GREEN FARM (Bá Chủ Khu Rừng 3D).
Workspace: d:\LamGameUnity\BaChuKhuRung3D
Engine: Unity 6 (6000.3.15f1) — URP. Backend: REST API riêng (KHÔNG dùng UGS).

Hãy đọc các file sau THEO THỨ TỰ để hiểu dự án:

1. RULES.md — Quy tắc tuyệt đối + QC Pass
2. Assets/_Project/Docs_KichBan/LoTrinh_Demo_Thu2.md — ⭐ LỘ TRÌNH demo + tiến độ (ƯU TIÊN khi đang crunch)
3. task.md — backlog + việc đã làm/đang chờ Editor/chờ khách
4. CHANGELOG.md — lịch sử phát triển (entry mới nhất = trạng thái gần nhất)
5. docs/DESIGN.md — Hệ thống thiết kế UI "The Tangible Playground"
6. Assets/_Project/Docs_KichBan/ThietKe_NPCShop.md — thiết kế hệ NPC shop
7. docs/ARCHITECTURE.md + docs/TECHNICAL_DESIGN.md — kiến trúc (đọc khi đụng backend)

(MEMORY.md auto-load mỗi phiên — đã có sẵn các kinh nghiệm/quyết định đúc kết.)

Sau khi đọc xong, cho tôi biết bạn đã hiểu gì về dự án + trạng thái lộ trình.
```

---

## Prompt nâng cao (nếu cần AI hiểu workflow)

```
Sau khi đọc các file trên, đọc thêm:
- unity-ai-workflow/docs/CODING_STANDARDS.md — Chuẩn code C#
- unity-ai-workflow/docs/NAMING_CONVENTIONS.md — Quy tắc đặt tên
- docs/SECURITY.md — anti-cheat, server-authoritative
- docs/BUILD_RELEASE.md — quy trình build Android + Play Console

Async pattern: Awaitable (Unity 6) — dùng thoải mái, KHÔNG cần UniTask
Brace style: Allman (dấu { xuống dòng mới)
UI: Unity UI Toolkit (UXML + USS), manual Q<T>() binding
Design: "The Tangible Playground" — solid colors, retro shadow, không blur
```

---

## Nếu đang làm dở task cụ thể

Thêm vào prompt:

```
Task đang làm dở: [mô tả task]
File đang sửa: [danh sách files]
Trạng thái: [đã xong gì, còn gì]
```

---

## 📌 TRẠNG THÁI MỚI NHẤT (cập nhật 25/06/2026 — APK build-mode hotfix)

### 🎯 Đang ở đâu
- Sáng 25/06 test APK phát hiện 2 lỗi nghiêm trọng: tap đặt chuồng/công trình trên điện thoại không hoạt động, và một số nút close/điều khiển hiện thành ô vuông do thiếu glyph Android.
- Đã sửa `BuildModeOverlayController`: bỏ phụ thuộc bắt buộc vào `Mouse.current`/`Keyboard.current`; tap Android dùng `Touchscreen.current`, raycast ghost ngay tại điểm tap trước khi pin vị trí. Giữ mouse path cho Editor/Windows.
- Đã sửa `GhostPlacementController`: ghost placement đọc touch đang giữ trên mobile, mouse trên desktop; thêm hàm `RefreshPlacementAtScreenPosition()` để overlay ép cập nhật đúng điểm tap trong cùng frame.
- Đã đổi glyph điều khiển dễ lỗi font (`✕`, `✔`, `⌂`) sang ASCII an toàn (`X`, `OK`, `B`) cho close buttons và build placement controls. Các icon nội dung như emoji/tick sự kiện chưa đổi vì không phải nút điều khiển chính.

### ✅ Cần test lại ngay trên APK
- Vào Build Mode -> chọn `Chuồng` -> tap vùng đất hợp lệ ở giữa màn -> nút `OK/X` hiện gần ghost -> tap `OK` phải đặt được chuồng và trừ gỗ.
- Tap gần rìa màn hình vẫn phải bị chặn đặt để tránh bấm nhầm.
- Mở Settings/Login/Shop/Inventory/Map/Quest/Mailbox... kiểm tra nút close không còn hiện ô vuông.

---

## 📌 TRẠNG THÁI MỚI NHẤT (cập nhật 24/06/2026 — PHIÊN 6)

### 🎯 Đang ở đâu
- Vừa có số liệu khách đổi mới `Assets/_Project/Docs_KichBan/SuaLai4VatNuoi.xlsx/.md`: chỉ sửa 4 Product 1 của Hươu/Dê/Ngỗng/Thỏ theo công thức `Tổng Product 1 = Tổng chu kỳ thu * Số lượng Pro1`. Giá đã áp: nhung hươu 12368, sữa dê 12, trứng ngỗng 14, lông thỏ 21. Cập nhật 29/06: quyết định gia cầm chỉ-trứng đã bị đổi lại, ngỗng và các gia cầm khác có thịt ở vụ cuối.
- Icon sản phẩm mới từ `Assets/Sprites/icon/SanPham/` đã được gắn cho 34 item có tên ảnh rõ ràng: sản phẩm cây lâu năm, đồ ăn/cá, sản phẩm vật nuôi, phân bón/thuốc/mồi/vé/quà. `ItemDataGenerator.AssignIconTextures()` cũng đã map các đường dẫn này để chạy lại mock data không mất icon. Hiện chưa có icon ảnh riêng cho thịt gà/vịt/ngỗng/đà điểu, nên 4 item thịt gia cầm dùng fallback hiện có.
- QC kinh tế: NPC shop data-driven và shop mở bằng nút HUD/legacy mock đều đã tra `ItemDatabase` cho giá mua/bán/tên/icon; không còn giữ giá mock cũ trong luồng HUD.
- Hotfix shop vật nuôi: mua thú không spawn thẳng vào chuồng cũ nữa; shop trừ POS và thêm animal item vào túi, còn sức chứa chuồng chỉ kiểm khi người chơi thả thú vào chuồng build-mode.
- UI/QC mới: Confirm dialog tự bring-to-front để không bị Settings đè; Login screen có nút `✕ Thoát game` đóng app thật cho bản Windows/stop Play Mode trong Editor; khi Build Mode đang mở, `FarmInteractionController` chặn tương tác thế giới để không click xuyên vào thú/chuồng.
- Rich demo loadout tăng mạnh: 500.000 POS, vật liệu xây dựng 1000 mỗi loại, food/product 500, seed 300, consumable 300, nước tưới 500.
- Backend demo tối thiểu đã online ở `https://ywonder.net/game-api` và client có `Assets/Resources/BackendConfig.asset` trỏ đúng URL này. Hiện backend chỉ chứng minh được `auth/register`, `auth/login`, `player/profile`, cờ `tutorialCompleted`; inventory/POS/farm/build vẫn là local PlayerPrefs trong client.
- Login/Register UI đã gọi backend thật. Sau login thành công, client preload profile để tài khoản rich skip tutorial ổn định kể cả khi người test bấm Skip cutscene nhanh.
- Tài khoản test giàu cho khách: `DemoRich01`/`DemoRich01` tới `DemoRich05`/`DemoRich05`. Tất cả profile server có `tutorialCompleted=true`, level 25; client nhận diện rich account và cấp `GiveTestLoadout()` (100.000 POS + nhiều item) khi vào gameplay. Tài khoản mới sạch: `DemoNew01`/`DemoNew01`.
- Khi test đổi account trên cùng thiết bị, cần clear app data/PlayerPrefs trước vì tiền/đồ/build/farm state vẫn lưu local.
- Interaction hotfix: tâm ngắm tiếp tục là nguồn tương tác chính, nhưng guard khoảng cách gần theo hit/closest point/XZ; nước, thú trong chuồng, chuồng/ruộng/cây đã giảm lỗi UI hiện mà click không chạy hoặc đứng gần không hiện.
- Bản APK/Windows demo vẫn dùng tốc độ demo: `GameTimeConfig.SecondsPerGameDay = 60f` (1 ngày game = 60 giây thật), không đổi về 24h thật trước test chéo.
- Expected timing để test: cây ngắn ngày ~60s sau tưới; tutorial 24s; Sa Chi/Sầu Riêng ~28 phút; Chanh dây ~90 phút; vịt 60s, gà 120s, dê/ngỗng 180s, đà điểu 360s, bò 420s.
- NPC tutorial đã dùng prefab `Assets/_Project/Prefabs/ExclamationMark.prefab` thay dấu chấm than primitive; khi spawn sẽ gỡ collider để không chắn ray/click.
- Backend/VPS: client có khung REST (`BackendConfig`, `ApiClient`, `AuthService`, `PlayerProfileService`) nhưng mới đủ auth/profile/tutorialCompleted. Muốn demo với VPS thì deploy `server/` stub và tạo `Assets/Resources/BackendConfig.asset` trỏ `baseUrl` về URL public; backend online thật cho POS/inventory/farm/cây/thú/server-time/IAP là phase riêng sau demo.
- Build/chăn nuôi đã bước vào ổn định: `BuildSurfaceCell`, chuồng ghép từ hàng rào, thả thú theo đúng size chuồng.
- Hệ Sprint mobile đã chỉnh theo yêu cầu: `Sprint` bấm/tap hold đúng trạng thái; `auto-run` không nhảy vô tội vạ; đổi hướng joystick mới break sprint; có smoothing riêng cho touch và clamp pitch.
- Tutorial NPC đã chốt logic cơ bản, đang tiếp tục tinh chỉnh tốc độ thoại/hướng dẫn để không spam.
- Build mode dùng `ghost` prefab trực quan, hàng rào có auto-connect, animation búa khi đặt công trình đã gắn.

### 🔧 Việc tiếp theo ưu tiên
- Ưu tiên 19/06 theo đúng yêu cầu: effect vật bay vào túi đồ, hủy chuồng lấy lại tài nguyên, trồng theo ô từng loài.
- Sau đó quay lại các task phụ và UI polish còn lại theo `task.md`.

---

## 📌 TRẠNG THÁI MỚI NHẤT (cập nhật 23/06/2026 — PHIÊN 5)

### 🎯 Đang ở đâu
Làm xong **vòng đời CHẾT thật + PERSISTENCE real-time** cho cả CÂY và THÚ:
- **Cây:** chết thiếu nước (thanh máu **8h** chưa tưới / **20h** có tưới — khách chốt) · tất cả cây ngắn ngày chín **24h** (BA) · tutorial tua **24s** · nhãn nổi nhỏ lại vừa thanh nước.
- **Thú:** chết đói (**24h/48h** · rùa **5/10 ngày** — khách chốt) · **tách bệnh khỏi đói** · chết = **biến mất + trả ô** · thanh đói mượt mỗi frame.
- **Persistence:** đổi cây+thú sang **wall-clock** → đóng/mở app **lớn-bù/đói-bù/chết-bù** đúng. Lưu/khôi phục **công trình build mode (Ruộng/Chuồng/Đường) + cây + con vật** theo ô `BuildSurfaceCell` (`BuildPersistence.cs` + `PlacedBuilding.cs` mới).
- **Rà soát kinh tế thú** xong (`RaSoat_SoLieu_MauThuan.md` mục 23/06, cập nhật lại 29/06): giá mua/bán khớp `VatNuoi2` **100%**; gia cầm nay có trứng theo chu kỳ + thịt ở vụ cuối; **chi phí bệnh chưa áp → lời game > bảng** tới khi làm Gói B.

### 🔴 Bài học/kiến trúc QUAN TRỌNG phiên này
- **Ô TRỒNG đến từ build mode** (`GhostPlacement` đặt prefab **Dirt** trên `BuildSurfaceCell`), KHÔNG phải `TilePlacementSystem` (gõ búa — không dùng) hay lưới `FarmManager` (đã ẩn). Persistence phải bám đúng `BuildSurfaceCell`. (Lúc đầu bé phủ nhầm 2 hệ kia → mất công.)
- **`Time.timeAsDouble` reset khi đóng app** → đã đổi cây+thú sang **Unix wall-clock (`RealNow`)** để bù offline. (Chỉnh-giờ-máy còn tua được → server-time sau.)
- File QC đã sửa (có phép, báo rõ): `FarmTile`, `GhostPlacementController` (Build Mode), `TutorialManager:448`.

### ⚠️ Việc Editor (phần lớn ĐÃ làm)
- ✅ Chạy lại generator (Crop + Animal) — số chết/24h đã bake.
- ✅ Tắt `Force Run Tutorial For Testing` (ép cây tua 5s mọi lúc).
- `BuildPersistence` tự gắn (hoặc gắn tay `[BuildPrefabLibrary]`). `FarmManager.autoSpawnTiles` = TẮT mặc định (không spawn 10 ô lưới).

### 🔜 Việc tiếp theo (KHÁCH hẹn PHASE SAU)
- **Gói B — hệ BỆNH thú** (vắc-xin phòng + thuốc trị + phát bệnh theo tỉ lệ/thời điểm `VatNuoi2`). Xong → lời khớp bảng (~250-400%).
- Persistence offline **server-time** (chống tua giờ máy). Cây giàn (chanh dây) chưa persist. Phân bón. Chốt EXP "lần cuối" vs ngày×10.

---

## 📌 (PHIÊN 4 — 23/06, lịch sử)

### 🎯 Đang ở đâu
Đã **áp bộ giá Point mới (USDT×26)** từ 3 file CayTrong2/CayTrongLauNam2/VatNuoi2 + làm **cây lâu năm thu nhiều lần + số ô (chanh dây 20 ô)** + **nhãn info nổi trên cây** + **vòng quay/điểm danh 15 ngày** + **EXP/Level (250+5/cap90, ngày×10)** + nhiều **mobile UI**. Vừa **FIX bug lớn GameManager bị xoá** → game chạy lại trơn.

### 🔴 Bài học QUAN TRỌNG phiên này
Manager do `SystemsBootstrapper` tạo (**Economy/Inventory/Tool**) KHÔNG được gắn lên object CHUNG với manager khác (vd `_GameManager`): singleton trùng gọi `Destroy(gameObject)` sẽ **huỷ cả object** → mất GameManager. ĐÃ đổi 3 manager đó sang **`Destroy(this)`** (chỉ huỷ component).

### ⚠️ Việc Editor đang chờ
- Chạy generator: **Generate Mock Items → Crop Data → Shop Data** (assets đã đổi phần lớn). Kéo model chanh leo vào `Crop_passion_fruit_seed_01`.
- **TẮT `giveTestLoadoutOnStart=false` trước khi build** (đang BẬT test). Có thể gỡ component InventoryManager thừa khỏi `_GameManager`.

### 🔜 Việc tiếp theo
- **Gói B — hệ BỆNH vật nuôi** (tỉ lệ/thời điểm phát bệnh, vắc-xin phòng, thuốc trị, chết theo mốc loài) — `AnimalDefinition` chưa có field bệnh. Phân bón. Chốt EXP cột "lần cuối" vs ngày×10.

---
## 📌 (PHIÊN 3 — 22/06, lịch sử)

### 🎯 ĐANG CRUNCH DEMO — đã BUILD APK thử (đang tối ưu dung lượng + chờ khách chốt giá bán)
- **Mục tiêu:** APK chơi được vòng lặp Nông trại + Thành phố, OFFLINE. Đã build thử (1.2GB → đang giảm texture).
- **Team:** 1 dev + AI. Anh tự kiêm QC (được sửa thẳng module QC khi yêu cầu).

### Vừa hoàn thành — PHIÊN 3 (22/06) — xem CHANGELOG entry 22/06 cho đầy đủ
- **Toast** mọi hành động (thu hoạch/chặt-đào/mua-bán/câu cá). **EXP/Level tối giản** (`ExperienceManager` + HUD số thật). **Âm thanh KHUNG** (`AudioManager`, cần thả file `Resources/Audio/`).
- **Mobile #4** (camera kéo 1 ngón — `LookZone`) + **#5** (`UISafeArea`). **Resume người chơi cũ** (có save → bỏ Login+Cutscene vào thẳng game ở vị trí cũ).
- **Câu cá BẢN TẠM** (ẩn popup, 8.7s tự +1 cá). **Tưới không spam** (kiểm `IsBusy`). **Tutorial chống kẹt** (auto-nhảy bước đi-theo-NPC 90s). **Map khóa đảo Mỏ** + đổi thông báo "Chưa đủ điều kiện để di chuyển". **Ẩn nút cheat** trong Map.
- **Áp GIÁ KHÁCH CHỐT (22/06):** giá MUA con giống = cột **USDT** (+ thêm 3 con vịt/ngỗng/rùa); nông sản ngắn ngày = **THỨC ĂN không bán**. Chặt cây=**10 gỗ**/đào đá=**10 đá**.

### ⚠️ Việc Editor đang CHỜ ANH (nút thắt build)
- **Chạy lại 4 generator** (số giá mới): `Generate Mock Items` → `Crop Data` → `Animal Data` → `Shop Data`.
- **Gắn model**: cây (Crop Prefab) · 10 thú (`AnimalPrefabLibrary`). **Gắn**: `WaterSource`/`ShopZoneTrigger`/`HarvestableResource` (Tree/Rock) · `UISafeArea` lên GameHUD · City: collider+FishingSpot.
- **TỐI ƯU APK (đang làm):** GPU Instancing (xong) · Static map1/stonemap+nhà city (xong) · **GIẢM TEXTURE NPC 2048→512+ASTC** (APK 1.2GB do texture NPC `.glb` = 96%, 4 con 256MB) — dùng menu `Tools/Tối ưu Mobile/Nén Texture` hoặc chỉnh tay → **build lại đo**.
- Player Settings: Switch Platform Android · IL2CPP+ARM64 · package name · **Landscape (xong)** · thêm **CityScene vào Build Settings**. Xem `Docs_KichBan/TruocKhiBuild_Checklist.md`.

### Còn lại / Phase 2 (không chặn demo)
- **Exploit kinh tế** (phá chuồng dupe · 2 hệ chuồng AnimalPen-vs-BuildSurfaceCell · thú chết kẹt ô · đổi giờ máy reset lượt câu · POS lưu `(int)` tràn). Xem memory [[qc-audit-blindspots]].
- **Persistence DateTime offline** (cây/thú lớn-bù). **Thêm Chanh dây + dọn 7 cây lâu năm thừa** (khách chốt còn 3).
- **Settings volume/graphics chưa áp** · nhiều popup chỉ log không trao thưởng · **CHỜ KHÁCH chốt giá BÁN sản phẩm + cân bằng lời** (phiếu `PhieuHoi_Khach_GiaCa.docx`).

### 🔧 Lưu ý dev QUAN TRỌNG (đọc kỹ)
- **`giveTestLoadoutOnStart = false`** (ĐÃ TẮT cho build). Muốn test có sẵn đồ thì tạm đổi `true`, build để `false`.
- **Độ "lời":** ĐỪNG nói "kinh tế thủng/lời 300-500 lần" — SAI (bỏ qua thức ăn + 9 tháng + giới hạn lần thu). Lời thật ~250-400%. Giá BÁN chờ khách chốt.
- **`GameTimeConfig.SecondsPerGameDay = 60f`** (demo). Đổi 86400 cho bản thật + persistence DateTime.
- **Test cây/thú NGOÀI tutorial** (tutorial ép cây lớn 5s). Tắt `Force Run Tutorial For Testing`.
- **KHÔNG đụng code scale model cây** trong FarmTile (bù `cropParentLossy` đang đúng).
- **Module QC đã sửa phiên này** (báo rõ): `GameHUDController`, `TutorialManager`, `FarmInteractionController`, `FishingOverlay*`. NPC dùng glTF importer (không có nút Extract Textures).
## Update 2026-07-01: login profile cache isolation

- Hotfix Demo05/Demo02 account bleed: `PlayerProfileService` now saves local profile cache per signed-in `AuthService.UserId` or `Username` instead of one shared `YW_Profile_Cache`. `AuthService` resets the runtime profile when identity changes and lets the profile service accept `player_profile` from `/auth/web-login` if present.
- Follow-up HUD fix: `GameHUDController` refreshes the top-left profile name from the active session after gameplay starts, prioritizing `GameManager.playerName` so the corner card matches the world `FloatingNameTag`.
- Test note: after this change, test switching DemoRich02 -> DemoRich05 in the same Editor/app session, including a case where `/player/profile` is unavailable, and confirm the name/profile no longer falls back to DemoRich02.
