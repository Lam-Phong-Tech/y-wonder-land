# 🔄 Context Recovery — Y WONDER GREEN FARM (Bá Chủ Khu Rừng 3D)

# Dùng khi bắt đầu conversation MỚI với AI

## Cách dùng

Copy đoạn prompt bên dưới → paste vào chat mới → AI sẽ tự đọc và hiểu dự án.

---

## Cập nhật gần nhất

- **Cập nhật 30/06/2026 - đảo đào khoáng MVP:** đã mở nền code để chọn `mine` trên bản đồ và travel tới `MineScene`. `IslandTravelManager` có fallback `MineMap -> MineScene` để dữ liệu Inspector cũ không làm vỡ runtime. `FarmInteractionController` giữ câu cá chỉ ở `city`, nhưng đào đá nay cho phép ở `city` hoặc `mine`. `ResourceSpawner` hỗ trợ gắn prefab cây/đá, snap xuống nền, random lại vị trí khi tài nguyên hồi sinh, và spawn trong nhiều vùng `Collider` kiểm soát được thay vì chỉ spawn hình tròn. **Cần Editor:** thêm `Assets/_Project/_Scenes/MineScene.unity` vào Build Settings thay entry cũ `MineMap`, set island `mine` sceneName `MineScene`, đặt `ResourceSpawner` trong `MineScene` với `spawnerID = Mine`, `treeCount = 0`, `rockCount` theo mật độ test, bật `randomizePositionOnRespawn`, gắn `rockPrefab` nếu có. Với map méo/rộng, tạo vài `BoxCollider` trigger cao phủ vùng đất hợp lệ, kéo vào `ResourceSpawner > Spawn Areas`, bật `snapSpawnToGround` với ground mask riêng, rồi dùng context menu `Clear Saved Resource State` nếu cần rải lại theo vùng mới. Daily 10 lượt/nâng cuốc/shop đá quý chưa làm trong MVP này.
- **Cập nhật 29/06/2026 - polish trước khi thêm dữ liệu cá/đá:** đã đổi toàn bộ text hiển thị tiền từ `POS` sang `Point` và `UPOS` sang `UPoint` ở UI/toast/log demo liên quan; giữ nguyên tên biến/API nội bộ `POS/UPOS` để không đụng logic kinh tế. Câu cá thành công giờ có icon cá nổi/fade cùng toast qua `ScreenToast.ShowInfoWithIcon`. Nước biển Farm/City đã chỉnh sáng hơn, xanh hơn trên `Assets/IgniteCoders/Simple Water Shader/Resources/Water_mat_01.mat` và mesh nước phụ City `Assets/Art/Environment/Materials/water.mat`.
- **Cập nhật 29/06/2026 - chăn nuôi:** khách đổi lại quyết định gia cầm. Gà/đà điểu/ngỗng/vịt vẫn lấy trứng theo chu kỳ, nhưng vụ cuối sẽ trả thịt theo Product 2 trong `VatNuoi2.md` (`chicken_meat_01`, `ostrich_meat_01`, `goose_meat_01`, `duck_meat_01`) và bán được ở Mini Garden.
- **Cập nhật 29/06/2026 - icon thịt gia cầm:** 4 item thịt gia cầm đã gắn icon mới từ `Assets/Sprites/icon/ThitGiaCam/`. Toast vụ cuối của `FarmAnimal` dùng `ScreenToast.ShowInfoWithIcon`; túi đồ và shop tự hiển thị icon qua `ItemDefinition.iconTexture`.
- **Handoff 29/06/2026 - iOS/App Store Connect:** CodeMagic exported-Xcode workflow đã build được IPA và upload lên App Store Connect. Các lỗi đã xử lý gồm Unity license bypass bằng workflow Xcode-only, signing/profile `com.ywonder.greenfarm`, app icon iOS đầy đủ, executable bit cho `process_symbols.sh`/`usymtool`, giữ `il2cpp.a`, bảo toàn binary IL2CPP qua `.gitattributes`, và bump build lên `0.1.1 (2)`. Cập nhật theo góp ý bên build: đã bỏ `submit_to_testflight: true`, nên Codemagic chỉ upload IPA; việc add build vào Internal Testing làm thủ công trong App Store Connect. Sau lỗi App Store Connect báo IPA vẫn là build `1`, đã bake trực tiếp `0.1.1 (2)` vào exported iOS project và thêm bước verify IPA version trước publish. Sau hotfix mới nhất, build cần test/publish là `0.1.1 (4)`, không phải bản cũ `0.1.0 (0)`, `0.1.1 (1)`, `0.1.1 (2)` hoặc `0.1.1 (3)`.
- **Hotfix iOS/App Store Connect mới nhất:** bên build báo cần tăng build number và bổ sung khai báo export compliance. Đã tăng bản kế tiếp lên `0.1.1 (4)`, thêm `ITSAppUsesNonExemptEncryption=false` trong `ios/Info.plist`, đồng thời cập nhật `codemagic.yaml` để sau mỗi lần Unity export CodeMagic tự ép lại `CFBundleVersion=4` và key export compliance này trước khi archive/upload.
- **Lưu ý iOS size:** TestFlight hiển thị khoảng 309 MB. Tạm chấp nhận để qua bước cài/chạy trước; tối ưu dung lượng là task riêng sau, cần audit `Payload/YWONDERGREENFARM.app/Data`, `Frameworks`, `resources.assets`, `sharedassets*.assets`.
- **Repo state cần cẩn thận:** branch chính làm việc là `dev`, main đã được merge các patch iOS gần nhất. Worktree có thể còn nhiều file Unity/iOS generated dirty (`ios/Data`, `ios/Unity-iPhone.xcodeproj`, `ProjectSettings`, `AddressableAssetsData`, `.claude/`, `_Recovery`). Không stage/revert bừa các file này nếu task không cần.
- **Cập nhật 29/06/2026 - cá mới đã implement:** đã thêm 14 `ItemDefinition` cá mới trong `Assets/Resources/Items/`, gắn icon từ `Assets/Sprites/icon/CacLoaiCa/`, đổi reward câu cá sang random theo tier Point, và whitelist toàn bộ cá mới trong Fish Shop. `ItemDatabase.GetItem` có fallback load `Resources/Items/{id}` để shop/túi đồ/toast resolve item mới trước khi generator refresh `ItemDatabase.asset`.
- **Cập nhật 29/06/2026 - cutscene thuyền:** `Assets/_Project/Scripts/Cutscenes/BoatCutscene.cs` đã đổi failsafe từ cắt cứng 35 giây sang `effectiveCutsceneTimeout`: tính theo tổng quãng đường waypoint / `movementSpeed` + `cutsceneTimeoutBuffer`, rồi lấy lớn hơn `cutsceneTimeout`. Mục tiêu là cho thuyền đủ thời gian cập bờ, nhưng vẫn tự kết thúc nếu cutscene thật sự bị kẹt.
- **Cập nhật 29/06/2026 - đá quý:** `Assets/_Project/Docs_KichBan/CacLoaiDaQuy.md` đã ghi bảng đá quý khách chốt; đã thêm 6 item `gem_*.asset` với icon trong `Assets/Sprites/icon/CacLoaiDaQuy/`. Đào đá hiện giữ đá thường 100% với 10 rock, rồi roll thêm 1 đá quý theo tỉ lệ Ruby 1%, Amethyst 2%, Fire Quartz 5%, Green Calcite 12%, Orange Calcite 30%, Kyanite 50%; toast đào trúng dùng icon qua `ScreenToast.ShowInfoWithIcon`. Shop thu mua đá quý và giới hạn 10 lượt/ngày chưa implement.
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
- **Việc tiếp theo còn lại từ khách:** bảng đào đá mới chưa implement.
  - Đào đá: ảnh 1 = 2 point/viên, 4 viên, 50%; ảnh 2 = 3 point/viên, 4 viên, 30%; ảnh 3 = 6 point/viên, 3 viên, 12%; ảnh 4 = 12 point/viên, 2 viên, 5%; ảnh 5 = 500 point/viên, 1 viên, 2%, nâng cuốc lv2 tốn 250 point/lượt; ảnh 6 ruby = 3000 point/viên, 1%, nâng cuốc lv3 tốn 1500 point.
  - Mỗi ngày có 10 lượt đào.
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
