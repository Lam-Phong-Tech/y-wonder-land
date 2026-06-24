# Danh sách công việc dự án (Task Backlog & Progress)

## Tiến độ đã hoàn thành (Completed)

### 1. Nâng cấp Giao diện HUD (UI/UX)
- `[x]` Thiết kế lại `GameHUD` theo phong cách **Glassmorphism** (Nền Dark Navy trong suốt `rgba(58, 71, 102, 0.5)` kết hợp viền trắng mỏng).
- `[x]` Chạy script Python tự động xóa phông nền xám, cắt grid 200x200, cạo viền 8px để bỏ line AI, và bóc tách thành công bộ icon phẳng 2D từ `FarmingIconsCollection.png`.
- `[x]` Thay thế toàn bộ text/emoji trên HUD bằng **Sprite 2D phẳng** (Flat 2D Icons).
- `[x]` Ẩn tạm thời các nút Hành động (Interact, Cancel) trên HUD (`display: none;`).
- `[x]` Thống nhất Style Guide: **HUD** dùng Sprite 2D phẳng; **Popups (Túi đồ, Shop)** dùng Sprite 2.5D/3D Render (isometric, đổ bóng).
- `[x]` Đã xây dựng các **AI Prompts** chuẩn để tạo đồ họa 2.5D isometric cho game nông trại.

### 2. Tư vấn & Gỡ lỗi 3D Pipeline (Unity & Blender)
- `[x]` Phân tích và gỡ lỗi CharacterController bị lệch tâm so với nhân vật (xử lý Pivot khác biệt giữa Model và Root Empty Object).
- `[x]` Giải thích lỗi nhảy giật trục Y do tùy chọn "Bake Into Pose" trong thẻ Animation.
- `[x]` Tư vấn sự khác biệt, ưu nhược điểm giữa Animation `Generic` và `Humanoid`.
- `[x]` Cung cấp **Quy trình chuẩn (Pipeline) xuất FBX từ Blender sang Unity Humanoid** (Bone Naming theo chuẩn Mixamo/Unity, ép T-Pose, tắt Add Leaf Bones, Apply Transforms `1, 1, 1`, trục Y-Up/-Z-Forward).

### 3. Gameplay & Hệ thống (15–19/06/2026)
- `[x]` **Backend REST đợt 1**: server stub Node/Express + client (Auth/Profile), offline-first, lưu profile + cờ tutorialCompleted thật.
- `[x]` **Tài liệu kỹ thuật**: TDD, DB_SCHEMA (ERD), SECURITY, BUILD_RELEASE; rà soát điểm mù xin khách (DiemMu_CanXinKhach, TongKet_TaiLieu_CanCo); dọn mâu thuẫn UGS.
- `[x]` **Tưới cây cầm xô**, tự gom lá vào cây, tắt rung chặt/đập, dọn Splash.
- `[x]` **Camera PUBG/Free Fire** (nhân vật quay theo yaw, hết chóng mặt); fix cuốc lệch (xoay về ô đất).
- `[x]` **Build Mode sinh prefab THẬT** (`BuildPrefabLibrary`): xây ô đất (Dirt+FarmTile) & chuồng (Nhỏ 1x1/Vừa 2x2/Lớn 3x2) + animation Hammering.
- `[x]` **Ghost preview = bản mờ prefab** (xanh/đỏ kiểu ROK), WYSIWYG; bỏ lưới hiển thị; **tự bù pivot model lệch** (MakeCenteredClone).
- `[x]` **Hàng rào tự nối liền** (`FenceAutoConnect`) — tắt cạnh giáp kiểu Minecraft.
- `[x]` **Hệ chăn nuôi cơ bản**: click chuồng → mở túi (tab Thú nuôi) chọn con vật → thả (giới hạn loài theo cỡ chuồng); **cho ăn** qua túi (Bắp ngô) + animation Feed.
- `[x]` **Tutorial viết lại** (NPC ông lão khó tính): chặt cây → đào khoáng → xây ruộng → canh tác → xây chuồng → thả thú → cho ăn. Công tắc `ForceRunTutorialForTesting`.
- `[x]` **Vật phẩm con vật**: Gà, Đà điểu, Dê, Hươu, Thỏ, Bò; fix thuyền cutscene lật.

---

## 📌 QUYẾT ĐỊNH KHÁCH (20/06) — CÂU CÁ & ĐÀO ĐÁ CHỈ Ở ĐẢO THÀNH PHỐ
> Khách chốt: **câu cá** và **đào đá** CHỈ diễn ra ở **đảo Thành phố (CityScene)**, KHÔNG có ở **đảo khởi đầu (Nông trại)**. Đảo khởi đầu tập trung trồng trọt + chăn nuôi.
- `[x]` **Gate CÂU CÁ + ĐÀO ĐÁ theo đảo (code, chắc ăn)**: `FarmInteractionController.IsOnCityIsland()` (dựa `IslandTravelManager.CurrentIslandId == "city"`). Câu cá: ẩn nút + chặn `StartFishing` nếu không ở city. Đào đá (`HarvestableResource` type Stone): ẩn nút "Đào khoáng" + chặn HandleHold/ClickHarvestResource ở đảo khác (chặt cây vẫn được mọi đảo). → KHÔNG cần đụng vị trí biển/FishingSpot/đá.
  - **CẦN Editor**: đảm bảo có FishingSpot BẬT ở khu nước thành phố (nếu nằm trong `farmOnlyObjects` thì gỡ ra/đặt riêng trong CityScene).
- `[x]` **Sửa Tutorial bỏ bước đào đá** *(module QC, đã báo)*: sau Chặt cây → sang thẳng bãi ruộng (bỏ FollowToRock + MineRock). Đánh số lại các bước thành /13. Handler đào đá cũ thành dead-code (vô hại).

---

## ✅ PHIÊN 21/06 — ĐÃ LÀM (chuẩn bị demo thứ 2)
> Lộ trình demo: `Docs_KichBan/LoTrinh_Demo_Thu2.md`. Mục tiêu: APK chơi được vòng lặp nông trại + thành phố, offline, model tạm.
- `[x]` **Toast thông báo khi nhận vật phẩm / giao dịch** (người chơi dễ nắm bắt): dùng `ScreenToast.ShowInfo` (xanh) cho thành công, `Show` (đỏ) cho thất bại.
  - **Thu hoạch cây** (`HandleHarvest`): "Thu hoạch: +N {tên}"; thiếu nước → toast đỏ kèm % giảm (gộp 1 toast, khỏi đè).
  - **Thu sản phẩm thú** (`HarvestAnimal`): "Thu hoạch: +N {tên}"; vụ cuối đã có toast "Làm thịt..." riêng → chỉ toast khi con CÒN SỐNG (tránh đè).
  - **Chặt cây / đào đá**: ~~đăng ký event `OnResourceHarvested`~~ → **ĐỔI sang gọi TRỰC TIẾP** (`HarvestResourceTick` tại 3 call-site click/hold/world-hold) vì event tĩnh dùng CHUNG với `TutorialManager`; nếu handler Tutorial ném exception thì handler toast sau bị skip → mất toast. Helper đo chênh lệch túi để ra số lượng. Vẫn KHÔNG đụng file QC `HarvestableResource.cs`. → "Chặt cây: +N Gỗ" / "Đào khoáng: +N Đá".
  - **Mua/bán shop** (`ShopPopupController.OnActionClicked`): "Đã mua/bán: Qx {tên} (∓ POS)" + toast đỏ khi thiếu POS / chuồng đầy / hết hàng.
  - **Câu cá**: thêm toast "Câu được: +1 {tên cá}" trong `FishingOverlayController.HandleQTESuccess` (module QC — sửa 1 dòng, anh đã duyệt). Panel kết quả vẫn giữ.
  - `[x]` **Bug POS câu cá — XỬ THEO HƯỚNG A** (anh chốt): câu cá CHỈ cho cá (đem shop bán mới ra tiền). Xoá chữ "Nhận +X POS!" trong mô tả 5 con cá → thay bằng "Bán được giá...". Giữ nguyên field `rewardCoins` (lỡ Phase 2 cần) + Bao Lì Xì (hứa vật phẩm sự kiện, không phải POS). ⚠️ Còn dòng `Debug.Log` "Reward coin added +X POS" thừa trong code (vô hại, chỉ dev thấy console).
- `[x]` **Sản lượng tài nguyên (khách chốt): chặt 1 cây = 10 GỖ · đào 1 đá = 10 ĐÁ**: `FarmInteractionController.HarvestResourceTick` ép `resource.minYield=maxYield=treeYield/rockYield` trước khi harvest (SerializeField `treeYield`/`rockYield`=10, chỉnh Inspector). Toast tự hiện số thật (đo chênh lệch túi). KHÔNG đụng file QC.
  - `[x]` **Fix toast chỉ báo "xong" không có số**: nhiều cây/đá trong scene để TRỐNG `yieldItemId` → đồ không vào túi + đếm = 0. Helper bù id mặc định (cây→`wood_01`, đá→`stone_01`) nếu trống → đồ vào túi đúng + toast ra "+10 Gỗ/Đá".
- `[x]` **Redesign HUD câu cá GỌN (khách 21/06)** — viết lại `FishingOverlayController.cs` + `FishingOverlay.uxml` + `Styles/FishingOverlay.uss`:
  - **BỎ:** khối chọn mồi + 3 nút, test cheat, chỉ số mồi thường/xịn, nút Thoát top-bar, **panel kết quả** (modal). Báo kết quả bằng `ScreenToast`.
  - **GIỮ + mở rộng:** 1 popup "căn thời gian" ở **góc PHẢI** (panel navy 440px, thanh căn to 34px, vùng xanh + kim, thanh giờ xanh). Tích hợp **số lượt câu/ngày** + **nút X**. Theo Cozy Dark Palia, không màu mè.
  - **Luồng + timing:** bấm F → `Show()` tự bắt đầu căn cá **8.7s** (`castDuration`, khớp animation Fishing đã chỉnh 8.5→8.7s) → giật (nút / F / Space) trúng vùng xanh → **+1 cá vào túi + toast**. Trượt/hết giờ → toast đỏ. State còn Idle↔Timing. Lượt 10/ngày (`dailyTurns`) reset theo ngày thật.
  - Số chỉnh được: SerializeField `castDuration`/`dailyTurns`/`safeZoneWidthPercent`/`pointerSpeed`. **CẦN Editor**: prefab FishingOverlay tự lấy UXML mới (cùng path) — chỉ cần Play test; nếu Inspector còn ref `confirmDialog` cũ thì kệ (đã bỏ field, vô hại).
  - ⚠️ Nợ nhỏ: re-cast bằng nút KHÔNG gọi lại `FishingLineController.PrepareCast` (dây câu cosmetic, lần đầu vẫn đúng).
- `[ ]` ⏸️ **CHỜ SẾP CHỐT — thang giá MUA con giống/cây giống** *(KHÔNG chặn build demo — demo dùng giá hiện tại được)*: VatNuoi/CayTrong cho mỗi con 3 thang số: **Định giá** (bò 44.997 — công thức lợi nhuận sếp dùng số này) / **USDT** (300, AnimalDefinition đang dùng) / **demo** (1.500, ItemDefinition = shop đang tính tiền). Trộn USDT-mua + giá-bán-game → lời ~300 lần (kinh tế thủng). Báo cáo đầy đủ: `Docs_KichBan/RaSoat_SoLieu_MauThuan.xlsx` (4 sheet). **ĐÃ XÁC NHẬN ĐÚNG y nguyên:** chu kỳ/sản lượng/thức ăn/thịt/số ô cả 10 con + giá bán SP (subagent báo lệch là đọc nhầm cột). Khi sếp chốt thang giá → bé áp + thêm Chanh dây + 3 con thiếu ItemDef (Rùa/Ngỗng/Vịt) trong 1 lần. Asset còn số demo cũ → chạy lại generator.
- `[x]` **Câu cá BẢN TẠM (khách 21/06): ẨN popup, hết giờ tự +1 cá** — anh thấy minigame chưa cần, để "sửa sau": `FishingOverlayController.Show()` đổi sang KHÔNG hiện popup → state `AutoFishing` → đợi `castDuration` (8.7s, animation câu khoá người chơi) → `HandleCatch` cộng 1 cá ngẫu nhiên + toast + thu dây. Code minigame căn-giờ (Show cũ/StartCast/AttemptPull/UXML/USS) GIỮ NGUYÊN để bật lại sau. Trừ 1 lượt/lần, hết lượt → toast.
- `[x]` **Hệ NPC Shop data-driven** (chạm nhà → popup): ShopDefinition + ShopZoneTrigger + Show(ShopDefinition) + ShopDataGenerator (7 asset) + MerchantNPC.shopData. Tên shop nổi trên đầu NPC. **CẦN Editor**: chạy `Generate Shop Data`, gắn ShopZoneTrigger + collider trigger vào nhà NPC, kéo asset + kéo NPC vào `Name Tag Target`.
- `[x]` **Hủy chuồng → hoàn 50% + trả con giống** (phím G / tap).
- `[x]` **Economy số THẬT của khách**: giá hạt + nông sản + sản lượng + EXP 8 cây (ItemDataGenerator + CropDataGenerator theo `CayTrongLauNam.md`); 10 vật nuôi khớp `VatNuoi.md`. **CẦN Editor**: chạy `Generate Mock Items` + `Generate Crop Data` + `Generate Animal Data`.
- `[x]` **Cây lớn theo MỐC THỜI GIAN** (đi đảo về vẫn lớn). *(Còn: offline thật cần lưu `growStartTime` ra đĩa/server — Bước 3 persistence.)*
- `[x]` **Cây hết bóp dẹp** (bù scale ô đất) — **CẦN Editor**: chỉnh `Model Ground Offset` từng cây nếu lún.
- `[x]` **Mobile #3 chạm tương tác** (Pointer) + **chặt cây GIỮ-ĐỂ-CHẶT** + thêm rìu/cúp/cần câu vào túi.
- `[x]` **Chặt cây hết để lại lá** (so tên lá không phân biệt hoa/thường).
- `[x]` **Bơi: nhảy leo lên bờ** + ghép nhiều Box Collider tag Water cho hồ hình dạng lạ.
- `[x]` **Đổi đảo không ngập + nhẹ máy** (`farmOnlyObjects`) — **CẦN Editor**: kéo Water (+ cảnh nông trại) vào list.
- `[x]` **Build Mode**: bỏ hẳn xoay; fix nút Tích/X "đứng lì".
- `[x]` **Ẩn name tag trong cutscene** (hiện lại khi cập bến/skip). **Cap FPS 60** (cheap mobile win).
- `[ ]` **CHỜ KHÁCH**: xác nhận "giá vốn nông sản" có phải giá BÁN không; thời gian lớn cây ngắn ngày (hiện demo 20-60s); giá con giống (đã theo VatNuoi).
- `[ ]` **CẦN model 3D** (3D gửi sáng 22/06): 4 cây ngắn ngày còn lại (cabbage/sweet_potato/morning_glory/grass) → kéo vào `Crop Prefab`; 10 thú đã đủ model → gắn `AnimalPrefabLibrary`.

---

## ✅ CẬP NHẬT 23/06 (Antigravity đã làm)
- `[x]` **Persistence real-time (wall-clock) cho cây + thú**: đóng/mở app vẫn lớn-bù/đói-bù/chết-bù đúng mốc.
- `[x]` **Vòng đời chết thật**:
  - Cây: thiếu nước quá ngưỡng sẽ chết theo luật mới.
  - Thú: đói quá ngưỡng sẽ chết, biến mất và trả ô chuồng.
- `[x]` **Build persistence**: lưu/khôi phục công trình build mode (Ruộng/Chuồng/Đường) + cây + thú theo `BuildSurfaceCell`.
- `[x]` **Áp giá Point ×26** + cập nhật economy theo bộ dữ liệu khách mới.
- `[x]` **EXP/Level bản mới** + vòng quay + điểm danh 15 ngày.
- `[x]` **Generator dữ liệu đã chạy lại** (crop/animal) cho mốc thời gian và thông số mới.
- `[x]` **Tắt `ForceRunTutorialForTesting`** (không ép tua tutorial ở bản demo chính).

---

## 🐄 NHÁNH HIỆN TẠI: Chăn nuôi trong lồng (animal husbandry) — ⭐ ƯU TIÊN CAO NHẤT
> Sửa & bổ sung chức năng nuôi/trồng động vật trong chuồng.
> **Đây là nhóm việc ưu tiên số 1.** Hoàn thành hết nhóm này rồi mới quay lại các task tồn đọng phía dưới.

### Build theo Ô ĐẤT (surface-cell snapping) — 19/06
- `[x]` **Sửa lệch grid**: bỏ snap theo lưới ảo (`cellSize=1` lệch khối cube `0.8` + origin nhảy theo player). Ghost giờ snap vào **TÂM MẶT TRÊN** của khối cube đất (`BuildSurfaceCell`).
  - File mới: `BuildSurfaceCell.cs` (component đánh dấu ô: SurfaceCenter/FootprintSize/IsOccupied + registry), `Editor/BuildSurfaceCellSetup.cs` (menu gắn hàng loạt).
  - Sửa `GhostPlacementController.cs`: raycast → `GetComponentInParent<BuildSurfaceCell>` → snap tâm ô; validate theo `IsOccupied`; stretch theo 0.8.
  - **CẦN làm trong Editor**: map = 4000 khối "cube" (cỏ) + 400 "stone", KHÔNG nhóm, đảo méo mó, chỉ nửa phải buildable → kiểu **"sơn vùng"**: đặt nhiều BoxCollider ướm vùng buildable (lấn ra biển vô hại vì chỗ đó không có cube) → chọn hết → menu `Gắn BuildSurfaceCell theo VÙNG`; lỡ lấn khối không muốn → `Gỡ theo VÙNG`. Tag khối tên "cube*", tự thêm collider. Còn menu đệ quy + gỡ tất cả. Nếu khối gộp chung 1 mesh → đổi sang snap lưới 0.8 + 1 collider gộp.
  - TODO sau: highlight các ô buildable khi mở Build Mode (đã có `BuildSurfaceCell.All`); nối hủy công trình → `SetOccupied(false)`.

### Nhiệm vụ 19/06
- `[ ]` **Hiệu ứng thu thập**: khi thu thập (chặt/đào/thu hoạch...) làm vật phẩm **bay vào túi đồ** (animation item bay về icon túi).
- `[x]` **Hủy chuồng → thu lại tài nguyên**: ngắm ô rào ngoài gameplay → nút **"Hủy chuồng"** (phím G / tap) → `DemolishEnclosure`: trả con giống về túi (`AddItem`) → phá CẢ cụm rào (flood-fill `PenEnclosure.FindPen`) → hoàn **50% giá build** vào POS (`demolishRefundRate` chỉnh được). Ô tự `Clear()` + `ClearAnimal()` nên thả lại được ngay.
  - Sửa `BuildSurfaceCell` (lưu `BuildCost` + `AnimalObject`/`AnimalItemId` ô neo), `GhostPlacementController` (ghi `SetBuildCost` lúc đặt), `FarmInteractionController` (action + `DemolishEnclosure` + lưu con vật vào ô neo lúc thả).
  - TODO(khách chốt số): khi build cost nối vật liệu/`EconomyManager` thật thì hoàn ĐÚNG loại đã tốn (hiện build cost đang là mockup overlay, refund vào ví POS). Demolish trong Build Mode (menu ngữ cảnh `DeleteBuildingAt`) vẫn chỉ free lưới ảo cũ — chưa nối `BuildSurfaceCell` (việc riêng nếu cần).
- `[x]` **Bỏ tính năng Vuốt ve** (Pet) khỏi tương tác con vật. *(Gỡ nút E + hàm PetAnimal ở FarmInteractionController; vô hiệu hóa PetInteraction.cs. Còn: gỡ component PetInteraction khỏi prefab thú trong Editor.)*
- `[x]` **Thông tin con vật**: popup hiện giá mua / số ô chuồng / thức ăn chính / thức ăn phụ / sản phẩm — restyle Cozy Dark Palia. Thêm trường vào `AnimalDefinition` + điền data 10 con qua generator. **CẦN Editor**: chạy menu `YWonderLand ▸ Generate Animal Data` để nạp dữ liệu vào các asset `Animal_*.asset` (đảm bảo con vật spawn dùng đúng asset này).
- `[ ]` **Trồng từng ô ruộng kiểu xây hàng rào**: mỗi loài thực vật tốn số ô khác nhau. *(Khách CHƯA gửi số ô/loài cây → quyết định 19/06: **tạm cho mỗi cây = 1 ô**, chỉnh lại khi có dữ liệu thật.)*
- `[x]` **Sức chứa chuồng động + validate thả thú theo số ô**: rào = hộp vuông trên 1 ô → **ô CÓ RÀO = ô chuồng**. Ngắm/click ô rào → "Thả thú" → chọn loài → validate `penSlots` vs số ô-rào liền nhau còn trống (`PenEnclosure.FindPen` BFS cụm ô-rША 4-kề; nhiều rào kề = chuồng to) → đủ thì thả (`SetAnimal`), thiếu thì `ScreenToast` báo lỗi. Click thẳng (PC) + bấm chữ (mobile) đều chạy. Gizmo hiện trạng ô.
  - File mới: `PenEnclosure.cs` (flood-fill), `AnimalPrefabLibrary.cs` (map itemId→prefab thú), `ScreenToast.cs` (toast lỗi). Sửa `BuildSurfaceCell` (Occupant/HasFence/IsFree), `GhostPlacementController` (ghi occupant), `FarmInteractionController` (luồng thả vùng quây).
  - **CẦN Editor**: thêm 1 GameObject gắn `AnimalPrefabLibrary` + điền itemId→prefab thú; hàng rào phải đặt qua Build Mode (để ghi occupant vào ô). Phụ thuộc hệ `BuildSurfaceCell` đã chạy.
  - TODO: bước "xem thông tin loài trước khi thả" (confirm dialog) — hiện đang thả ngay khi chọn; báo lỗi đang dùng OnGUI toast (nâng UI Toolkit sau).

### Nhiệm vụ 21/06 — Vật nuôi SỐNG theo thời gian + thanh HP
- `[x]` **Vật nuôi lớn/ra sản phẩm theo MỐC THỜI GIAN** (`Time.timeAsDouble`, giống cây): đói + chu kỳ sản phẩm tính từ mốc, **đi đảo thành phố về vẫn chạy bù đúng**. Viết lại `FarmAnimal.cs` (bỏ cộng dồn `deltaTime`).
- `[x]` **Gắn logic cho thú CÓ prefab**: trước đây thả thú có model chỉ ra khối trơ (không đói/không sản phẩm). Sửa `FarmInteractionController` thả thú → `AddComponent<FarmAnimal>()` + `Initialize(def, false)` (giữ model, chỉ thêm thanh HP). Nhận diện thú đổi sang `GetComponentInParent` (chắc ăn dù collider nằm ở con sâu).
- `[x]` **Thanh HP (no/đói) nổi trên đầu** — billboard tự dựng bằng code (quad Unlit 2 mặt), **tự đo chiều cao theo model**, không cần artist. No đầy = xanh, đói = đỏ, bệnh = tím; có chấm vàng "có sản phẩm". Ẩn khi chết. Field `statusBarHeight` (0 = tự đo).
- `[x]` **Popup hiện thời gian thu hoạch + tổng số lần thu** (quyết định khách: để trong popup, không nhồi lên đầu): `AnimalInteractionPopupController` thêm "No: X% · Vụ tới: 12s · Còn 37/38 lần thu" + đếm ngược SỐNG (Update 0.25s). Không sửa UXML.
- `[x]` **Fix tràn chữ popup**: tách "Độ no" + "Thu hoạch" thành 2 DÒNG riêng trong bảng (thêm `LblHunger`/`LblHarvest` vào UXML), status về ngắn gọn, cho phép xuống dòng.
- `[x]` **Cho ăn ĐÚNG tài liệu (bỏ ngô mặc định)**: `HandleFeedSelected` validate thức ăn theo `AnimalDefinition.foodMain/foodAlt` (so theo TÊN qua ItemDatabase) + trừ ĐÚNG số lượng (vd Bò sữa cần 2x Cỏ Voi hoặc 4x Khoai Lang); sai thức ăn → toast, không trừ đồ. `EnsureStarterFeed` cấp đúng thức ăn loài cho demo. **CẦN Editor**: đảm bảo ItemDatabase có item tên khớp ("Cỏ Voi", "Khoai Lang"...) — nếu thiếu sẽ có warning trong Console.
- `[x]` **Wire ĐẦY ĐỦ logic vật nuôi theo VatNuoi.md (cả 10 con)**: trước đây chỉ 3 con base (gà/bò/heo) có logic + số demo, 7 con kia không có `produceItemId`/`maxHarvests` → thu ra rỗng + "∞ lần".
  - Generator: thêm `SetAnimalGameplay` cho cả 10 con → `produceItemId`, `produceAmount` (=SL Pro1), `maxHarvests` (=Tổng lần thu VatNuoi), thịt vụ cuối. Tạo 17 item sản phẩm/thịt còn thiếu (giá bán theo cột "Giá Product 1/2" VatNuoi). Sửa giá egg/milk/pork theo VatNuoi; pig Pro1 đổi `pork_01`→`pigskin_01` (Da heo), thịt = pork_01.
  - `AnimalDefinition` thêm `meatItemId`/`meatAmount`. `FarmAnimal.HarvestProduct`: vụ CUỐI (hết số lần thu) → cộng thịt (Pro2) + **con vật biến mất** + **giải phóng ô chuồng** (`ClearAnimal`, rào vẫn còn) → thả con mới được ngay. `FarmInteractionController` gán `occupiedCells` cho con vật lúc thả.
- `[x]` **Fix tên + loại thức ăn cho khớp VatNuoi**: đổi `grass_01` "Cỏ khô"→**"Cỏ Voi"**, `cabbage_01` "Rau cải"→**"Bắp cải"**; chuyển **7 nông sản sang category "food"** để hiện trong tab cho ăn (trước đó là "items" → không chọn được). ⚠️ Nông sản giờ nằm tab "Thực phẩm" thay vì "items" — nếu Mini Garden/shop lọc theo category cần rà lại.
- `[x]` **CẦN Editor**: chạy lại `Generate Mock Items` + `Generate Animal Data` (data mới). Test thu hoạch 10 con + vụ cuối làm thịt.
- `[ ]` **CHỜ KHÁCH**: chu kỳ thu hoạch đang để giây DEMO (25s) thay vì ngày thật — chờ khách quy đổi ngày→giây.
- `[x]` **Khung CƠ BẢN cho 10 cây LÂU NĂM** (để anh gắn model): thêm 10 hạt giống + 10 sản phẩm (ItemDataGenerator) + 10 CropDefinition (CropDataGenerator, để trống `cropPrefab` cho anh kéo model). Tạm **1-lần-thu** như cây ngắn ngày. Giá Sa Chi/Sầu Riêng theo CayTrong.md, còn lại số DEMO. **CẦN Editor**: chạy `Generate Mock Items` → `Generate Crop Data`, rồi kéo model vào `Crop_<seed>.asset`.
  - TODO Phase 2: **cơ chế thu NHIỀU LẦN** cho cây lâu năm (giống vật nuôi: ra quả nhiều vụ + vụ cuối) — FarmTile hiện chỉ 1 lần thu. Số liệu thật 7/10 cây chưa có (CayTrong.md mới có Sa Chi + Sầu Riêng + chanh dây).
  - `[x]` Wire SHOP đầy đủ (ShopDataGenerator): Farm Shop bán **đủ 18 hạt** (8 ngắn + 10 lâu năm) + **đủ 10 con giống** (thêm vịt/ngỗng/rùa); Mini Garden mua **đủ nông sản + 10 SP cây lâu năm + 20 SP/thịt vật nuôi** (trước chỉ egg/milk/pork). Thêm hạt lâu năm + SP vào `GiveTestLoadout`. **CẦN Editor**: chạy lại `Generate Shop Data` + `Generate Mock Items`.
- `[x]` **Thanh "khát nước" cho CÂY (behavior B — khách chốt)**: thanh nước nổi trên cây tụt dần theo `waterIntervalSec`; cạn = khát → cây **vẫn lớn** nhưng cộng dồn thời gian khát → lúc thu **giảm sản lượng + POS** (tới tối thiểu 50%), đúng kịch bản "quên tưới → héo, mất EXP". Tưới LẠI (action "Tưới nước" khi đang lớn) đổ đầy nước. `FarmTile`: thêm `lastWaterTime`/`dryAccumSec`/`LastCareFactor` + `WaterAgain()`/`GetWaterFraction()` + thanh billboard ĐỘC LẬP (không parent ô đất để né scale lệch). `FarmInteractionController`: Watered→"Tưới nước", phạt POS + toast. **KHÔNG đụng** phần spawn/scale model cây.
  - TODO: visual "héo" trên model (đổi màu) chưa làm — hiện báo khát bằng thanh đỏ + toast khi thu (tránh tint material artist rủi ro). EXP phạt chờ hệ EXP.
- `[x]` **THỜI GIAN THỰC + TƯỚI-GATE-LỚN (khách chốt 21/06)**: 1 ngày game = 24h thực.
  - `GameTimeConfig.cs` (Core): hằng số `SecondsPerGameDay` (DEMO 60f · THẬT 86400f) + `Days()`/`Hours()` — **1 điểm chuyển demo↔thật**.
  - Generator khai thời gian theo NGÀY/GIỜ game (cây 1 ngày lớn/tưới 10h; rau muống+cỏ voi 0.5 ngày; dưa hấu+bí ngô 2 ngày; thú theo VatNuoi: gà 2/bò 7/đà điểu 6/dê+ngỗng 3/vịt 1/thỏ 40/heo+hươu 180/rùa 300 ngày).
  - **Tưới-gate-lớn**: cây CHỈ lớn khi còn nước; hết nước → NGỪNG lớn tới khi tưới lại (`FarmTile.growthAccrued`+`GetGrownSeconds`). Bỏ phạt sản lượng behavior B.
  - **CẦN Editor**: chạy lại `Generate Crop Data` + `Generate Mock Items` + `Generate Animal Data`. Test ngoài tutorial (tutorial vẫn ép 5s).
  - ⚠️ CÒN NỢ: lưu MỐC DateTime ra đĩa để offline lớn bù khi đổi sang 86400 (bản thật). `growStartTime` thành biến thừa (cảnh báo nhẹ).
- `[x]` **Fix luồng Tutorial (2 bug)** — *(module QC, sửa tối thiểu, báo rõ)*:
  - **Chặt cây/đào khoáng nhảy bước ngay**: `OnTreeArrived`/`OnRockArrived` auto-nhảy nếu túi đã có gỗ/đá — mà loadout test tặng sẵn `wood_01`/`stone_01` → bỏ đoạn auto-skip, bắt người chơi thực sự chặt 1 nhát (vẫn nghe `HarvestableResource.OnResourceHarvested`).
  - **Thả thú không cập nhật nhiệm vụ**: tutorial nghe `AnimalPenSpawner.OnAnimalPlaced` (hệ CŨ) nhưng hệ chăn nuôi đã viết lại (BuildSurfaceCell). Thêm `FarmAnimal.OnAnimalSpawned` (bắn trong `FarmInteractionController` lúc thả) → tutorial nghe sự kiện mới. `FarmAnimal`/`FarmInteractionController` cũng sửa.
- `[x]` **Chức năng MÚC NƯỚC (khách yêu cầu 21/06)** — ĐÃ LÀM:
  - `WaterSource.cs` (component đánh dấu vùng ao múc được). Item `watering_water_01` "Nước tưới". Ngắm ao + bấm **"Múc nước"** → +5 xô/lần (`amountPerScoop`). Tưới cây **TỐN 1 xô**; hết → toast "Ra ao múc nước". KHÔNG animation (khách không cần). `FarmInteractionController` (nhận diện WaterSource + ScoopWater + HandleWater trừ nước). Loadout test có sẵn 30 xô.
  - **CẦN Editor**: gắn 1 Collider(IsTrigger) + `WaterSource` lên bề mặt ao giữa đảo. KHÔNG gắn lên nước biển.
- `[x]` **#2 Tách tab túi đồ**: sản phẩm (trứng/sữa/thịt + SP cây lâu năm) tách khỏi tab "Thú nuôi" → đổi tab "Đặc biệt" thành **"Sản phẩm"** (category `products`). Live animals giữ tab "Thú nuôi". Sửa ItemDataGenerator (category) + InventoryPopup.uxml + Controller.
- `[x]` **#1 Ẩn tab filter shop không liên quan**: `ShopPopupController.UpdateFilterVisibility()` — chỉ hiện filter (Seeds/Animals/Tools/Items) có hàng trong shop đó; còn lại ẩn, giữ "Tất cả".
  - **CẦN Editor**: chạy lại `Generate Mock Items` (item nước + đổi category sản phẩm) + `Generate Shop Data`.
  - ⚠️ Lưu ý phụ: live-animal item `duck_01`/`goose_01`/`turtle_01` chưa có ItemDefinition (chỉ 7/10 con mua được ở shop) — bổ sung sau nếu cần bán 3 con này.
- `[~]` **CẦN Editor/test**: thả thú thử → chỉnh `statusBarHeight` nếu thanh lệch đầu; xác nhận prefab thú có Collider (nếu chưa, FarmAnimal tự thêm BoxCollider tạm). Đảm bảo đã chạy `Generate Animal Data` để có `produceCycleTimeSec`/`feedIntervalSec`/`maxHarvests` thật.

## 🔍 RÀ SOÁT TRƯỚC DEMO (21/06 — anh review) — đối chiếu task/lộ trình
> 13 điểm anh nêu khi chơi thử. Phần lớn TRÙNG; ➕ = GAP mới chưa có ở task/lộ trình.
- `[x]` **#11 Khoá map** (chỉ Nông trại + Thành phố) — `IslandTravelManager` gate. **(21/06 bổ sung)** Popup Map: thêm `LockMine` (icon 🔒) cho đảo **Mỏ** + `IsUnlocked("mine")=false` cứng (scene chưa có). Đổi thông báo map khóa (dialog + toast) → **"Chưa đủ điều kiện để di chuyển."** (MapPopupController + IslandTravelManager). MapPopup KHÔNG thuộc QC.
- `[ ]` ➕ **#1 Thành phố thiếu biển**: biển nằm `farmOnlyObjects` → ẩn ở city. CityScene cần **water plane RIÊNG + FishingSpot** (Editor).
- `[~]` **#2 NPC thành phố chưa đủ popup**: chỉ NPC shop mua/bán chạy. VIP/Maid/Pet/Game/Gift/Heo Đất = Phase 2 (xem mục "HỆ NPC").
- `[~]` **#3/#4 Lưu/load**: CÓ lưu local (POS/túi/ô đất/thú qua PlayerPrefs, lúc Quit/Pause). **(21/06) THÊM luồng RESUME người chơi cũ**: `GameManager` — có save → **bỏ Login+Cutscene, vào thẳng game**; lưu + thả lại **đúng vị trí** lúc thoát (chỉ lưu toạ độ khi ở Nông trại để resume an toàn). Cờ `alwaysStartFresh` (test mở đầu) + ContextMenu "Clear Save". *(GameManager là file protected — sửa theo yêu cầu anh, báo rõ.)* CÒN NỢ: (a) persistence DateTime cho offline lớn-bù (cây/thú lớn khi đóng app); (b) TẮT `giveTestLoadoutOnStart` khi build thật; (c) resume luôn về Nông trại (nếu thoát ở City thì về farm) — chấp nhận cho demo.
- `[~]` **#5/#8 Loop + công thức**: vòng lặp lõi chạy đúng; data khớp VatNuoi/CayTrong; **đã có EXP/Level**. Còn phần chốt kinh tế cuối (giá bán + anti-exploit + đồng bộ web).
- `[x]` **#6 Xây chuồng tốn GỖ + #7 Ruộng FREE + Build mode dùng VẬT LIỆU (không POS)**: `BuildModeOverlayController` đổi item sang `materialId`+amount (Ruộng=miễn phí · Đường đá=1 Đá · Chuồng=1 Gỗ/ô rào); menu hiện chi phí vật liệu + số gỗ/đá đang có. `GhostPlacementController` KIỂM + TRỪ vật liệu lúc đặt (thiếu → toast, không đặt). `BuildSurfaceCell` lưu `BuildMaterialId`+amount. Phá chuồng (`DemolishEnclosure`) HOÀN đúng vật liệu (đầy đủ) thay vì POS. Loadout test có 30 gỗ + 30 đá.
- `[x]` ➕ **#9 Tái sinh tài nguyên**: `HarvestableResource.respawnTimeSec` thành SerializeField gọn (Header "Tái sinh" + tooltip), default đổi 3600→**60s** (demo). Cây + đá dùng chung → cả hai mọc lại. **CẦN Editor**: set `respawnTimeSec` trên prefab/đối tượng cây+đá CŨ (chọn nhiều → sửa 1 lần), vì chúng đã lưu 3600.
- `[x]` ➕ **Build cost ra SerializeField**: `BuildModeOverlayController` thêm `penWoodCost`/`pathStoneCost` (Inspector) — đổi số không cần sửa code. Ruộng free.
- `[x]` ➕ **#10 Popup "Tính năng đang phát triển"** cho NPC chưa dùng được — ĐÃ CÓ (`ShopZoneTrigger.comingSoon` → `ScreenToast.ShowInfo` "🚧 ...đang phát triển").
- `[x]` ➕ **EXP/level system + HUD số (tối giản)** — `ExperienceManager` (singleton tự tạo, lưu PlayerPrefs, level ramp 100+(lv-1)*50, bắn `LevelUpOverlay`). Nối HUD (`GameHUDController` hiện Level + % EXP thật, bỏ "Level 1/0.00" cứng; sửa `SyncPlayerName` ép level 1). Cộng EXP: thu hoạch cây (`crop.expReward`) + chặt/đào (`resourceExp`=5, SerializeField). *(GameHUD module QC — sửa theo yêu cầu.)*
- `[x]` ➕ **Âm thanh/nhạc nền (khung tối giản)** — `AudioManager` (singleton tự tạo, tải clip `Resources/Audio/<tên>`, thiếu file bỏ qua êm). Nối: nhạc nền `bgm` (HUD OnEnable) · `chop` (chặt/đào) · `harvest` (thu hoạch) · `coin` (mua/bán). **CẦN: thả file audio vào `Assets/Resources/Audio/` (xem README_AUDIO.txt).** Volume có `SetMusicVolume/SetSFXVolume` — chưa nối slider Settings (2 TODO).
- `[x]` ➕ **Ẩn nút CHEAT (Tăng Level/VIP) trong Map** — cờ `showCheatButtons=false` ẩn `.map-cheat-bar`; bật lại để dev test.
- `[x]` ➕ **Tutorial CHỐNG KẸT** — `CheckFollowAutoAdvance`: bước "Đi theo NPC" quá 90s không tới → tự gọi handler tới nơi (tránh kẹt do NPC ngoài NavMesh). Chỉ áp bước đi-theo. *(TutorialManager module QC — sửa theo yêu cầu.)*
- `[ ]` **#12 Dọn data mock + tích hợp web** — Phase 2 (đã biết, nhiệm vụ lớn).

### Nhiệm vụ 20/06 (ưu tiên mới)
- `[x]` **Xây mặt đường đá (paving)**: item "Đường đá" trong menu Build → map `BuildPrefabLibrary` (nameContains "đường đá" → StoneSlab, stretch ON). Snap theo `BuildSurfaceCell`. *(Cần điền entry trong Editor.)*
- `[x]` **Dọn menu Build còn 3 mục**: 1 tab "Xây dựng" = Ruộng / Đường đá / Chuồng (rào xịn); ẩn 4 tab cũ. *(Cần BuildPrefabLibrary có 3 entry: ruộng→Dirt, đường đá→StoneSlab, chuồng→Fence (stretch Fence OFF).)*
- `[x]` **Fix ghost luôn báo đỏ**: GhostPlacementController đổi `Physics.Raycast` → `RaycastAll` + tìm `BuildSurfaceCell` gần nhất (bỏ qua collider nền/mesh đảo chắn trước).
- `[x]` **Loadout test (nhiều thức ăn + tiền)**: `InventoryManager.GiveTestLoadout()` + cờ `giveTestLoadoutOnStart` — nạp nông sản/sản phẩm/vật liệu/hạt + 100k POS để test NPC mua/bán.
- `[x]` **AnimalManager.LookupDefinition (chắc ăn)**: tra def qua Instance, fallback load thẳng Resources → info/validate chạy kể cả khi scene chưa gắn AnimalManager.
- `[x]` **Validate ô chuồng + cho thả NHIỀU con nếu đủ ô**: đã làm — `AvailableCount` (ô-rào chưa có thú) ≥ `penSlots` thì cho thả, đánh dấu `SetAnimal`; chuồng 9 ô thả được 9 gà (mỗi con 1 ô); chuồng còn 8 ô **KHÔNG** nhét được bò (9 ô) → báo lỗi. *(Còn TODO: nếu muốn giới hạn LOÀI theo cỡ chuồng thì bổ sung sau.)*
- `[x]` **Hiển thị thông tin con vật ở 3 nơi** (Tên/giá/thức ăn chính-phụ/số ô đất):
  - `[x]` Khi **xem thông tin** (popup AnimalInteractionPopup).
  - `[x]` Khi **mua** (Shop popup) — chèn "Thông tin nuôi" vào mô tả khi chọn con giống (`ShopPopupController.AnimalInfoText`).
  - `[x]` Khi **chọn trong túi đồ** (Inventory) — chèn vào mô tả khi chọn con vật (`InventoryPopupController.AnimalInfoText`).
  - *(Chèn vào mô tả nên không cần sửa UXML; nguồn data = `AnimalDefinition` — cần chạy `Generate Animal Data`.)*

---

## 👥 HỆ NPC (theo kịch bản "10+ NPC") — 20/06
> Nguồn: `Docs_KichBan/YWONDERLAND_KichBan3D_ChiTiet.md` + `DanhSachCuaHang_Game3D.md`.
> ĐÃ CÓ: **NPC Hướng dẫn** (tutorial, `GuideNPC`) + **1 Merchant NPC mẫu** (kiểu Hai Lúa). Còn lại chưa làm:

- `[~]` **Hệ Shop Keeper đa-NPC (data-driven)** — ĐÃ CODE, chờ setup Editor + test:
  - Cơ chế MỚI (sếp): **chạm NHÀ NPC → popup tự mở** (không click NPC). Tự mở · giữ mở khi đi ra · đóng bằng X. Thiết kế: `Docs_KichBan/ThietKe_NPCShop.md`.
  - File: `ShopDefinition.cs` (SO, mỗi shop 1 asset, chỉ lưu ID — giá tra ItemDatabase), `ShopZoneTrigger.cs` (gắn nhà NPC, học MapPortalTrigger), `ShopPopupController.Show(ShopDefinition)` + lọc thu mua theo whitelist, `MerchantNPC` thêm field shopData (click vẫn chạy), `Editor/ShopDataGenerator.cs` (menu `YWonderLand ▸ Generate Shop Data` sinh 6 asset).
  - **CẦN Editor**: chạy `Generate Shop Data` → 6 asset trong `Data/Shops/`; mỗi nhà NPC thêm BoxCollider(IsTrigger) + `ShopZoneTrigger` + kéo asset. Player có tag Player + CharacterController.
  - Nhóm VIP/Maid/Pet/Game/Cosmetic/Gift = feature riêng, làm sau. **CHỜ KHÁCH**: chốt giá con giống (3 nguồn lệch); duck/goose/turtle chưa có trong ItemDatabase (sẽ cảnh báo nếu thêm vào buy list).
  - Nông trại/Đảo: **Farm Shop** (hạt giống + con giống), **Item Shop** (phân/thuốc/vắc-xin), **Fish Shop** (mua cá / bán mồi).
  - Thành phố (~12 quầy): Bán cá, Workshop (nâng cấp dụng cụ), Verdant/YWonderLand, Mini Garden (mua nông sản), Hai Lúa, KNX (thẻ VIP), Maid Service, Pet Shop, Game Center, Store (thời trang), Gift Post, Heo Đất (tiết kiệm).
- `[ ]` **Maid (Hầu gái VIP)**: NPC nữ follow player ở nông trại, **tự tưới/thu hoạch** (đặc quyền VIP). Thuê tại Maid Service. Anim: Idle/Walk/Water/Harvest/Bow.
- `[ ]` **Pet (companion)**: mua ở Pet Shop → chạy theo chân (NavMesh, cách 1–2m), đứng yên→Sit, tap→Happy. Chỉ trang trí (không tham gia gameplay).
- `[ ]` **NPC khu Mỏ** (`MineScene`, mở ở Lv10): NPC **bán vé đào mỏ** + NPC **mua quặng**.
- `[ ]` **NPC Câu cá**: quầy bắt đầu câu / bán mồi (hiện chỉ có `FishingSpot` vùng nước, chưa có NPC quầy).
- `[ ]` **AI Chat NPC** (P2 — nice to have): pool câu trả lời theo từ khóa, tự nhắn vào khung chat làm sôi động khi ít người.

---

## 📱 RÀ SOÁT MOBILE (19/06) — game hướng thị trường điện thoại
> Rà soát phát hiện UI/điều khiển đang là PC-first. Sửa theo độ ưu tiên dưới.

- `[x]` **#1 Joystick ảo điều khiển di chuyển**: nối `Joystick` (GameHUD.uxml) vào `PlayerController.SetMoveInput()` qua kéo pointer (GameHUDController) + thêm style núm `.joystick-inner`. Gộp bàn phím + joystick trong PlayerController. **Cần test trên Editor/thiết bị.**
- `[x]` **#1b Nút Sprint giữ-để-chạy-nhanh (cảm ứng)**: PC bấm Shift đã chạy; sửa nút Sprint trên HUD dùng pointer capture (bỏ `PointerOutEvent` gây hủy sớm) → giữ nút là tăng tốc. Trên phone: 1 ngón giữ Sprint + 1 ngón joystick. **Cần test.**
- `[x]` **#2 Nút Jump + Hủy hoạt ảnh (X)**: Jump → `PlayerController.TriggerJump()` (1-bấm-1-nhảy). **Bỏ nút bàn tay (Interact)** — tương tác qua các **nút gợi ý nổi** quanh tâm ngắm (đã bấm được, dùng chung `ShowInteractionPrompts`). Giữ **nút X** = `PlayerController.CancelAction()` (ngắt hoạt ảnh, cất đồ, ẩn thanh tiến trình), tự hiện khi `IsBusy`. **Ngắm theo TÂM màn hình** (ổn định, không giật; gợi ý đứng yên khi rê chuột tới bấm). **Nút gợi ý "Chặt cây" bấm/tap được** (mỗi lần = 1 nhát `ClickHarvestResource`); căn giữa dưới tâm, co theo nội dung để không chặn dải ngang. Giữ chuột ở tâm vẫn chặt liên tục như cũ.
- `[x]` **#3 Tương tác đổi `Mouse.current` → `Pointer.current`**: `FarmInteractionController` giờ dùng `Pointer.current` + `pointer.press` (chung Mouse PC + Touchscreen mobile) → chạm tay chặt cây/trồng/mua chạy. PC vẫn chạy. *(Còn TODO multitouch: kéo joystick 1 ngón + tap ngón khác có thể kích hoạt hành động ở tâm — hiện chặn bằng `IsPointerOverGameObject` + `UIPopupTracker`; tinh chỉnh sau nếu lỗi.)*
- `[x]` **#4 Camera xoay bằng kéo 1 ngón** (vùng phải màn hình): thêm `LookZone` (nửa phải, con đầu của hud-root để nút đè lên) → `GameHUDController` bắt PointerDown/Move/Up (giống joystick) → `ThirdPersonCamera.AddTouchLook(delta)` cộng thẳng vào yaw/pitch (sensitivity riêng `touchHorizontalSensitivity`/`touchVerticalSensitivity`). PC khóa con trỏ ở tâm nên LookZone chỉ nhận chạm mobile (không đụng PC). Kéo-nhìn KHÔNG lỡ chặt cây (dòng 122 `IsPointerOverGameObject` chặn). `ThirdPersonCamera` thêm `Instance`.
- `[~]` **#5 Safe Area + khóa Landscape + Match**: **Match đã = 0.5** (cân ngang-dọc, ScaleMode=Scale With Screen Size) — OK sẵn. Viết `UISafeArea.cs` (đệm root UIDocument theo `Screen.safeArea`, tự cập nhật khi xoay máy; cờ `applyInEditor` để giả lập). **CẦN Editor:** (1) gắn `UISafeArea` vào GameObject có UIDocument GameHUD (và popup khác nếu muốn); (2) Player Settings ▸ Resolution ▸ khóa **Landscape**.

---

## Vấn đề còn tồn đọng (Pending Issues) — ⏳ ƯU TIÊN THẤP
> Làm SAU khi xong nhóm 19/06. Phần lớn liên quan **polish UI 2.5D + asset/artist** nên còn chờ tài nguyên.
> Đánh giá nhanh: Login & Character Select **ĐÃ XONG**; các mục còn lại chủ yếu **chờ ảnh AI / artist** hoặc là việc polish dài hơi.

- `[x]` **Login UI & Validation:** Thay thế dòng chữ Y WONDER GREEN FARM bằng logo Y Wonder Hub. Cập nhật UI và Logic để validate các trường đăng nhập, đăng ký tối đa 20 ký tự, đúng định dạng.
- `[x]` **Character Select UI & Logic:** Thay thế chữ M/F bằng Avatar ảnh (Nam/Nữ) tương ứng. Đặt ảnh vừa chọn làm avatar mặc định. Validate đặt tên nhân vật tối đa 20 ký tự.
- `[ ]` **Sửa lỗi Layout Popup cũ:** Cập nhật thêm các popup khác (Inventory, Friends, Map...) theo chuẩn Flat Graphics (Dark Theme) và sửa các lỗi chồng chéo layout nếu có.
- `[ ]` **Giao diện Popup:** Các icon 2.5D trong các Popup hiện tại (Cửa hàng, Kho đồ) chưa đúng phong cách mong muốn. Đang chờ ảnh mới từ Unity Muse/AI.
- `[ ]` **3D Model/Rigging:** Cần áp dụng quy trình Blender xuất FBX Humanoid mới cho các NPC/Nhân vật sắp tới để tránh lỗi mapping xương (vàng/đỏ) trong Unity.
- `[ ]` **Thống nhất Visual:** Cần quyết định cụ thể xem tiêu đề (Title) các Popup có nên dùng icon hay bỏ đi để không bị lạm dụng icon gây rối mắt.

---

## Bước tiếp theo (Next Steps) — ⏳ ƯU TIÊN THẤP (chờ asset/artist)
- `[ ]` **Sản xuất Asset:** Chạy AI với bộ Prompt đã tạo để sinh ra bộ vật phẩm 2.5D mới (Cà chua, Hạt giống, Rìu, Cuốc, Khoáng sản...).
- `[ ]` **Tích hợp UI Popup:** Đưa các sprite 2.5D mới vào Unity, xử lý tách nền và gắn vào các slot chứa đồ trong `InventoryPopup.uxml` và `ShopPopup.uxml`.
- `[ ]` **Kiểm thử Rigging FBX:** Import thử một file FBX nhân vật mới do bạn Artist làm theo workflow chuẩn để kiểm tra thẻ Rig Humanoid trong Unity.
