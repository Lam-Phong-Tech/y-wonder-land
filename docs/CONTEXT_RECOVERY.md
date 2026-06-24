# 🔄 Context Recovery — Y WONDER GREEN FARM (Bá Chủ Khu Rừng 3D)

# Dùng khi bắt đầu conversation MỚI với AI

## Cách dùng

Copy đoạn prompt bên dưới → paste vào chat mới → AI sẽ tự đọc và hiểu dự án.

---

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

## 📌 TRẠNG THÁI MỚI NHẤT (cập nhật 24/06/2026 — PHIÊN 6)

### 🎯 Đang ở đâu
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
- **Rà soát kinh tế thú** xong (`RaSoat_SoLieu_MauThuan.md` mục 23/06): giá mua/bán khớp `VatNuoi2` **100%**; gia cầm chỉ-trứng = khách chốt; **chi phí bệnh chưa áp → lời game > bảng** tới khi làm Gói B.

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
